# 014 — クオリティ第1弾: 部屋の見た目と雰囲気（計画8）

状態: **受入済み（2026-09-24、実機の見え方・聞こえ方は計画6で確認）**。結果は `reviews/tasks014-016-exchange-20260924.md`。計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 GPT-5.6 Sol（`codex exec` 別セッション）。レビュー 実装とは別セッションの Sol と Claude Sonnet 5。

## 目的

2026-09-24 ユーザー指定「仮想空間内などのクオリティを上げていく」の第1弾。現在の部屋はプリミティブに単色マテリアルを貼っただけで、照明も点光源2つと平坦な環境光。外部アセットや依存パッケージを追加せず、コード（URP の標準機能）だけで「古い密室」の雰囲気と奥行きを出す。

## 守ること（変えない）

- 取っ手（遮蔽・出口）、机、時計、指示カード、敵の移動経路、観客カメラの位置と向き。身体の位置に関わる配置（task006 B-5）は実機の後で決めるため動かさない。
- レイヤー: HMD 専用（PrivateLayer=8）と観客専用（9）の分け方。観客カメラに攻略の手がかり（取っ手・ランプ・時計・カードなど）が写らないこと。新しく足す物体は、攻略に関わるなら 8、ただの背景なら既定レイヤー。
- 暗転（Eye blackout）の即時性。周回・死亡・脱出のロジック（LoopDemo、LoopModel）には触れない。
- PCVR の負荷: リアルタイムの影は1灯まで、ポストプロセスは軽いもの（Bloom、Vignette、Tonemapping、Color Adjustments 程度）。

## 設計（RoomVisuals.cs の中で完結させる）

1. **素材**: `Material()` に粗さ・金属感・発光を指定できる引数を足す（既存の呼び出しは同じ見た目のまま動くよう既定値を持たせる）。石壁は粗く、真鍮は金属感、床は少し艶。出口ランプ・敵のバイザー・時計の文字盤の背景はわずかに発光させる（Bloom で光る）。
2. **部屋の細部**（既定レイヤー、当たり判定は外す）: 天井、幅木、壁の腰板または柱、天井の吊り照明の器具（光源の位置に合わせる）、扉の枠。床の目地は現状を活かす。
3. **照明**: 暖色の吊り照明をスポットまたはポイントで、影を有効化（ソフトシャドウ、解像度は中）。冷たい補助光は影なし。環境光を Trilight（上・横・下）にして、床と天井で明るさの差を出す。
4. **空気感**: `RenderSettings.fog` を Exponential で薄く（部屋の奥が少しかすむ程度、色は環境光に合わせる）。
5. **ポストプロセス**: 実行時に `VolumeProfile` を作り、Global の `Volume` に Bloom（弱め、閾値高め）・Vignette（弱め）・Tonemapping（ACES）・Color Adjustments（彩度をやや下げる）を入れる。HMD のカメラ（rig.View）と観客カメラの `UniversalAdditionalCameraData.renderPostProcessing` を true にする。
6. **敵のシルエット**: 既存の Coat/Head/Visor/Weapon を残したまま、肩・帽子のつばなどを足して人影らしくする（layer 8、影は既存どおり Off）。当たり判定は外す。
7. 物体を足すときは `Shape()` を使い、Collider を外す（取っ手の XRSimpleInteractable を邪魔しない）。

### 追修正（2026-09-24、計画担当 Opus 5.5。ビルドの自動実行の画面で判明）

変更後の desktop 画面（evidence/20260924-quality1/frames/f02）が変更前（evidence/20260924-quality-before/ready-screen.png）より大幅に暗く、床・机・左右の壁がほぼ見えない。「古い密室の雰囲気」は保ちつつ、プレイヤーが部屋の形と取っ手を読み取れる明るさに戻す。
- 環境光（Trilight）の空・赤道の色を明るく（目安: 変更前の Flat (.20,.23,.26) と同程度の平均）。
- 吊り照明の Spot の角度を広げる（部屋の中央と机全体を照らす）か強さを上げる。冷たい補助光も少し上げる。
- 霧の密度を下げる（目安 .012）。霧の色を環境光に近づける。
- Vignette は弱いまま、Color Adjustments の露出（postExposure）を使う場合は小さく。
- 変更前の画面と見比べて、壁・床・机・取っ手の輪郭が見えること。

### 追修正2（2026-09-24、計画担当 Opus 5.5）

追修正1の後もまだ暗い（evidence/20260924-quality1/frames/f02、右側の壁と床がほぼ黒）。環境光は変更前と同程度に戻したので、ACES トーンマッピングが中間調を沈めているのが主因と判断する。
- Tonemapping を ACES から **Neutral** に変える。
- `postExposure` を +0.25 → +0.4。
- 冷たい補助光の強さを上げ、右側の壁にも届く位置・範囲にする（影なしのまま）。
- 目標: 変更前の画面と同じくらいか少し暗い程度で、壁・床・机の輪郭がすべて見えること。

### 追修正3（2026-09-24、計画担当 Opus 5.5。ビルドの画面で判明）

明るさは改善（evidence/20260924-quality1/frames/f02）。一方で **TextMesh の文字が奥行きを無視して最前面に描かれている**: 奥の壁の「第零室 / THE ROOM BEFORE」や時計の数字が、手前の案内パネル（カメラ前 0.82m）の上に描かれている（frames/f06）。`font.material`（GUI/Text Shader）は ZTest が `unity_GUIZTestMode` で、3D では常に手前に出る。VR では壁や敵の向こうの文字が透けて見える。
- `RoomVisuals` に `public static Material TextMaterial(Font font)` を追加する。`font.material` を複製し、奥行きを考慮して描く（`unity_GUIZTestMode` を `CompareFunction.LessEqual` に設定する。効かない場合は理由を報告）。
- `RoomVisuals.Text()` はこの素材を使う。LoopDemo の案内パネルの文字は task015 の担当が同じ関数を使う。
- 文字の読みやすさ（大きさ・位置）は変えない。

### 追修正4（2026-09-24、計画担当 Opus 5.5）

追修正3のビルドで確認（frames/f12 を拡大）: 奥の壁の「THE ROOM BEFORE」が手前の案内パネルの上に描かれたまま。**素材ごとの `unity_GUIZTestMode` は効かない**。URP 同梱の `3DText.shader` はサンプル（未インポート）で固定機能シェーダーのため、VR の Single Pass Instanced に対応せず使えない。
- 新規 `Assets/LoopRoom/Shaders/LoopRoomText.shader`（名前 `LoopRoom/Text`）: URP の Unlit・Transparent。`_MainTex` のアルファ × 頂点カラーで描く（TextMesh は文字色を頂点カラーで渡す）。`ZTest LEqual`、`ZWrite Off`、`Cull Off`、`Blend SrcAlpha OneMinusSrcAlpha`、Queue は Transparent。Single Pass Instanced のステレオに対応する（`UNITY_VERTEX_INPUT_INSTANCE_ID`、`UNITY_VERTEX_OUTPUT_STEREO`、`UNITY_SETUP_INSTANCE_ID`、`UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO`）。URP の Core.hlsl を使う。書き方は PackageCache の URP の Unlit シェーダーや `Shaders/Utils` を参照して確かめる。
- `RoomVisuals.TextMaterial(font)`: `font.material` を複製し、`Shader.Find("LoopRoom/Text")` が見つかればシェーダーを差し替える（見つからなければ今の複製のまま＝従来の見た目、警告を1回出す）。`unity_GUIZTestMode` の設定は削除してよい。
- `DemoSetup.Prepare` の常時同梱シェーダーの一覧に `"LoopRoom/Text"` を加える（見つからなければ既存どおり例外）。これにより GraphicsSettings.asset に1件増える（設定の差分として記録する）。
- 許可ファイルにこの節だけ `Assets/LoopRoom/Shaders/LoopRoomText.shader`（新規）と `Assets/LoopRoom/Editor/DemoSetup.cs`（一覧に1件足すだけ）を加える。

### 追修正5（2026-09-24、独立レビュー指摘。計画担当 Opus 5.5）

Sol request_changes（`sol-task014-independent-mcp.md`）: (1) 採用: 吊り照明に影を付けたため、観客専用の頭・両手（layer 9）の影が床に落ち、HMD から見える。`publicHead/Left/Right` の `shadowCastingMode=Off`、`receiveShadows=false`。(2) 採用: 許可ファイル欄が追修正4と食い違っていた（計画担当の書き漏れ）→ 下の欄を更新。

### 追修正6（2026-09-24、再レビュー指摘。計画担当 Opus 5.5）

再レビュー: Sol approve、Sonnet request_changes（`sonnet-task014-rereview-mcp.md`）。
- 採用（Sonnet 高）: `LoopRoomText.shader` の `#pragma target 2.0` はインスタンシング／Single Pass Instanced に足りない（3.5 以上が必要）。`#pragma target 3.5` にする。
- 不採用（Sonnet 中「font を再代入すると素材が戻る」）: `.font=` は LoopDemo.cs:87 と RoomVisuals.cs:78 の生成時1回だけで、その後に素材を設定している（計画担当が grep で確認）。
- 見送り（Sonnet 低2件: Pendant shade の影設定の書き方、VolumeProfile の破棄）: 動作に影響なし。

## 許可ファイル

- `Assets/LoopRoom/Scripts/RoomVisuals.cs`
- `Assets/LoopRoom/Shaders/LoopRoomText.shader`（追修正4で新規）
- `Assets/LoopRoom/Editor/DemoSetup.cs`（追修正4、常時同梱シェーダーの一覧に1件足すだけ）

## 受入条件

1. Unity batchmode コンパイル エラー0・警告0。モデルテストは影響なし（念のため全件 PASS）。
2. 画面の証拠: 変更前後のゲーム画面（desktop モード）のスクリーンショット（task016 の自動開始オプションを使って撮る。撮れない場合は Editor で撮る）。観客カメラの画面（F2 運営表示なし）で攻略の手がかりが写らないこと。
3. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
4. HMD 内の見え方・フレームレートは実機（計画6）で確認（未確認と明記）。
