## 独立レビュー（交換後の再レビュー）：Task025 手続き生成テクスチャ

対象SHA256（本レビューで解析したソース）
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `04d9aea3855900d5e69da836965cb071f3f372b7c15c2e7df56ee88cccf44a4b`
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `240a043975c167832f18e1e84d48360fe6de864ca1ae411bbc946b737cf910f2`
- `Collaboration/tasks/025-procedural-textures.md`: `9172f503daf585fedf80bab4182915773209ca2cf73ef112332c6a70f1ee9cc0`
- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`

Sol／Sonnetの独立レビュー（`sol-task025-independent-mcp.md`、`sonnet-task025-independent-mcp.md`、対象SHA `f919e050dfc2bc3d229572d87da0f5dcc622211229b34e76bd1206a3a4ff361b`）が指摘した中の項目と、`025-procedural-textures.md` の「追修正1〜5」を照合しながら、現行コードを検証しました。

### 追修正1（周期性）の検証 — 解消を確認
- `FloorTexture()`：`grain` は `Sin(px*15f+...)` のように引数の係数が整数（15,4）で、`2π·n·x/size` 形式のため x=0とx=size-1の境界で位相が一致します（`RoomVisuals.cs` 106〜113行目付近）。板の継ぎ目（`across`/`along`）も、テクスチャ端（board 3→0、plank end→start）がいずれも「継ぎ目色（216/222）」で塗りつぶされる境界に一致するよう設計されており、端で不連続になりません。
- `WoodTexture()`：`bend`（`px*2f`、`px+1.3f`）、`grain`（`py*9f`、`py*3f`）とも整数係数で、x・y双方向に周期size で連続します。
- `WallTexture()`、`FabricTexture()`：同様に整数係数の`Sin`のみで、`FabricTexture`の`weave=8`は`size=256`の約数（256%8=0）のため境界で連続します。
- `PeriodicNoise()` はセル数`cells=size/cellSize`で全て整数割り切れ（512/8, 256/32, 256/8）、`(x0+1)%cells`でセル境界を折り返しており、格子ノイズ自体もシームレスです。

→ コード上は Sol/Sonnet が指摘した「境界不連続」は解消されていると判断します（実機・HMDでのモアレは別途未確認）。

### 追修正2（マテリアル共有）の検証 — 解消を確認
- `Material()`（`RoomVisuals.cs:25-45`頃）が `MaterialKey`（shader, color, smoothness, metallic, emission, texture, textureScale）でキャッシュし、同一パラメータの`Shape`/`Detail`呼び出し（左右壁、机脚4本、椅子脚4本、公開用ドア複製等）が同一`Material`インスタンスを共有するようになっています。
- 実行中に色を書き換える出口ランプ（`ExitLamp`）は `UpdatePublic()` で `ExitLamp.material`（`.sharedMaterial`ではない）を参照しており、Unity側の仕様上ここで自動的にインスタンス化されるため、キャッシュ共有から自然に切り離されます。要求どおり「実行中に書き換える物だけ個別」という設計意図に整合しています。

### 追修正3（壁のムラ増強）の検証
- `WallTexture()` の変動幅は `Sin(px)*Sin(py)*3f + Sin(px*3f+py*2f)*1.5f`（理論上限±4.5）＋周期ノイズ±1.25で、合計理論最大±5.75。仕様「±5〜6/255」に整合しており、コード上の矛盾は見当たりません（実際に近くで見て分かる程度かは画像未確認）。

### 追修正4（額縁への木目適用）の検証 — 反映を確認
- `Picture frame` に `texture:woodTexture, textureScale:new Vector2(1.64f,1.36f)` が付与されており（0.82m/0.5m=1.64, 0.68m/0.5m=1.36で実寸と整合）、反映済みです。

### 追修正5（--desktop-pitch）の検証 — 概ね妥当、軽微な注記あり

1. **[低] 追加した引数解析ロジック自体は妥当** — `DemoRig.cs` の `Initialize()` 内で `forceDesktop` が真の場合のみ `--desktop-pitch` を読み、`NumberStyles.Float` + `InvariantCulture` でパースし、NaN/Infinityを弾いた上で `Mathf.Clamp(-70,70)` しています。範囲・前提条件（`--desktop`併用時のみ）とも仕様と一致し、欠陥は見当たりません。

2. **[情報] `desktopPitch` の適用箇所が `LookAtFloorForCalibration(false)` 経由のみ** — `pitch = active ? 60f : desktopPitch;`（該当行）はキャリブレーション終了時にのみ`desktopPitch`を反映します。キャリブレーション画面を経由しない/呼び出されない経路がある場合、指定した角度が一切適用されない可能性がありますが、その呼び出し元（キャリブレーション制御スクリプト）は本スナップショットに含まれておらず、配線の妥当性は確認できません。**未確認**として扱います。

### 新規に見つけた軽微な指摘

**[低] `materialCache` が静的かつ `Build()` 間で解放されない**
`RoomVisuals.cs`（`static readonly Dictionary<MaterialKey, Material> materialCache` 宣言部、および `Material()` 内のキャッシュ登録処理）
- トリガー: 同一プロセス内で `RoomVisuals.Build()` が複数回呼ばれる場合（例: batchmodeテストが複数のテストケースでルームを構築・破棄する、あるいはドメインリロード無効化設定下でのPlay再入）。
- 各`Build()`呼び出しごとに `FloorTexture()` 等が新規`Texture2D`を生成するため、テクスチャの`GetInstanceID()`が変わり`MaterialKey`も変わります。その結果、古い`Build()`のマテリアル・テクスチャがキャッシュに残ったまま再利用されず、単調に蓄積します（誤動作はしませんが、テストを多数回回す環境ではメモリリークになり得ます）。
- 修正案: `Build()` 冒頭で `materialCache.Clear()` するか、インスタンスフィールド化する。

## 判定

**approve**

理由: Sol/Sonnet 両者が request_changes とした2つの中程度の欠陥（周期境界の不連続、マテリアル未共有）は、コード解析上いずれも妥当に解消されていることを確認しました。壁のムラ拡大・額縁への木目適用も反映を確認しました。`--desktop-pitch` の追加コードは仕様どおりで安全に実装されています。新たに見つけた `materialCache` の静的リーク懸念は低重要度であり、受入をブロックする欠陥とは判断しません。

## 未確認事項

- batchmode 0/0、テスト全件PASS、ビルド0/0、例外0 — 本レビューではテスト・ビルドを実行しておらず、実行ログ等の証跡データもスナップショットに含まれていないため未確認です。
- `Collaboration/evidence/20260925-textures/` の各画像（`compare.png`、`room.png`、`after-pitch40.png`、`floor-zoom.png`）— 画像データ自体は本レビューに提供されておらず、記載された内容（全体の明るさ・色合いの一致、机・壁の質感、`--desktop-pitch 40`時の床の見え方）は目視確認できていません。特に「床の継ぎ目・端の食い違い」という記載については、コード上は周期性が保たれていることを確認済みですが、実際のレンダリング結果（フィルタリング・ミップマップ・視野角による見え方）は画像なしに断定できません。
- Quest 3実機でのモアレ・ちらつき、異方性フィルタの効果。
- `LookAtFloorForCalibration` の呼び出し元（キャリブレーション制御コード）がどの経路で`false`を呼ぶか、`--desktop-pitch`が実際に全ての desktop 起動経路で反映されるか。
- Cube各面のUV実測値（面ごとの伸び方）はUnity上での実機検証が必要。
- `WoodTexture` が `FloorTexture` より「控えめ」に見えるかの主観的判断。