# task015 独立レビュー（Claude Sonnet 5、実装セッションとは別セッション）

対象SHA256（引用）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs` = `12de37174914c99e60248fc4d02fb3ed6377a6420cadf95122c99f95dc0d29f3`
- `Assets/LoopRoom/Scripts/ProceduralAudio.cs` = `6e020145a1e1877d5275539d12fd32eb5617ef9446800dd7fab225abb1e47b0f`
- `Collaboration/tasks/015-quality-audio-and-panel.md` = `4c2d33fabb10040bc104f87340423cd28da8f4066df489ba6682da0561a67c3c`

他のレビュー報告は未参照。以下は本スナップショットのみに基づく独立判断。

---

## 指摘事項

### [Medium] `LoopDemo.cs` `ApplyMessageSize()`（fixedFallback分岐）— 固定 characterSize=0.0044 の妥当性はコード構造では保証されない
```csharp
const float fixedCharacterSize=.0044f;
shrink=fixedCharacterSize/message.characterSize;
message.characterSize=fixedCharacterSize;
```
- 発生条件: `LateUpdate` の計測が30フレーム以内に成功しない場合（開発機以外の環境、フォント読み込み遅延など）。
- 内容: `0.0044` は開発機1回の実測（`0.0145×0.30`）に基づくハードコード値であり、実際に固定パネル(0.62×0.26m)へ収まる保証はコード上ない。プローブ文字列（後述）の想定と実際の実行環境のフォントメトリクスが変われば破綻しうる。
- 修正案: 致命的ではないため必須修正ではないが、フォールバック発生時にも実測ベースの検算（例: 固定文字列でもう一度 `localBounds` を測って警告ログを出す等）を用意すると堅牢性が上がる。task側も「開発機で測った値」と明記しており計画上は許容範囲。

### [Medium] `TextMaterial(font)` のシェーダー実装が本スナップショットに含まれず、追修正5の技術的主張が検証不能
- `Collaboration/tasks/015-quality-audio-and-panel.md` 追修正5に「TextMesh の color は頂点カラーで保持され、material 差し替えの影響を受けない」との記述があるが、これは `RoomVisuals.TextMaterial(font)`（task014担当、本レビュー対象外）のシェーダーが頂点カラーを描画に反映する実装であることが前提。
- `LoopDemo.cs` 90行目付近: `message.color=Color.clear; text.GetComponent<MeshRenderer>().material=RoomVisuals.TextMaterial(font);` の順序自体（color設定→material差し替え→text設定）はUnityのTextMesh実装から見て妥当（textセット時に現在のcolorが頂点色として焼き込まれる）だが、シェーダー側の対応は未確認。
- 修正案不要（コード側に欠陥なし）。ただしRoomVisuals.csのシェーダー実装を合わせて確認する必要あり。

### [Low] `BuildMessage()` のプローブ文字列（'国'×20、4行）の妥当性はフォントメトリクス依存
- 全角「国」20文字を最悪ケースとして仮定しているが、実際の最長行（例: `"Enter：開始　Space：遮蔽　E：出口"`）は半角英字を含むため通常は全角換算で20文字より短くなる想定で、安全側に振られている。ただし正確な確認にはUnity実行が必要。

### [Low] `ApplyMessageSize()` のログ計算がBuildMessage初期値にハードコード依存
- `shrink=fixedCharacterSize/message.characterSize;` は `message.characterSize` がBuildMessage設定値(0.0145)のままである前提。将来的に初期値だけ変更されるとログの意味がずれる（動作自体には影響しない、informationalレベル）。

### 検証した項目（欠陥なし）
- `ProceduralAudio.cs` 各関数（Chime/Latch/Shot/Open/RoomTone）はすべて `Normalize()` で最大振幅を指定ピーク以下にスケーリングしており、`AudioClip.SetData` に渡る波形は[-1,1]内に収まる → **クリップは静的に発生しない**（受入条件の「clipped 0」と整合）。
- `RoomTone()`: `humA=55, humB=58.5`, `loopSeconds=4` はいずれも `freq*loopSeconds` が整数になり、48本の倍音（`k=800+h*32` が `fundamental=1/4` の整数倍）も含めてバッファ境界で位相が一致する → ループ点のクリック/不連続は理論上発生しない。
- `LoopDemo.cs`: `LateUpdate` → `ApplyMessageSize` → 次フレームの `RefreshWorld` という順序により、`messageSized` が立った瞬間のフレームでは表示更新が行われず（`SetActive`/`color` 復元は次のUpdateサイクル）、プローブ文字が実際に画面に描かれる経路は見当たらない（追修正4の指摘は解消されている）。
- 固定フォールバック時 `shrink=0.0044/0.0145≈0.3034≈0.30`、これは追修正3のログ実測値（`shrink≈0.56/1.86≈0.30`）と一致し、task015が要求する数値（`0.0145×0.30≈0.0044`）とコードが一致している。
- `enemyAudio`/`room.Sound`/`ambience` を別AudioSourceに分離しており、`room.Sound.Stop(); enemyAudio.Stop();`（周回切替時）が環境音を止めない設計になっている。

---

## 判定

**task015 全体（追修正1〜5）: approve（条件付き）**

コード上の論理矛盾・明白なバグは見当たらず、各追修正の意図（NullReferenceException回避、仮の文字のちらつき防止、パネルはみ出し対策、固定サイズ時のcharacterSize調整）はすべてLoopDemo.csに整合的に反映されている。ただし上記Medium2件（固定フォールバックの実測値依存、TextMaterialのシェーダー前提）は実行時検証が必要な事項であり、コードレビューだけでは完全な保証はできない。

---

## 未検証事項

1. Unity batchmodeコンパイル（エラー0・警告0）は未実施。
2. Editor Play / ビルド実機での画面表示（文字がパネル内に収まっているか、HMD含む）は未確認。
3. 各合成音の実際の聞こえ方（音量バランス、不快さ）は未確認（task015自身も「人の耳で確認」を受入条件として保留中と明記）。
4. `RoomVisuals.TextMaterial(font)` のシェーダー実装（頂点カラー対応の有無）は本スナップショットに含まれず未確認。
5. `DemoRig.PreparationMessage` の実際の文字列・行数（VR準備メッセージ4行との記述の裏付け）は本スナップショットに含まれず未確認。
6. 波形の実際のスペクトル・波形画像（受入条件2）は未確認、Normalize関数からの静的推論のみ。