# 独立レビュー（task015 追修正1〜4、実装: 別セッションの claude-sonnet-5）

対象:
- `Assets/LoopRoom/Scripts/LoopDemo.cs`（SHA256: `540eddf28a19f69b34e572070014a8e7faccc3d1293dbcf2d7a13e9cbf1f077b`）
- `Assets/LoopRoom/Scripts/ProceduralAudio.cs`（SHA256: `6e020145a1e1877d5275539d12fd32eb5617ef9446800dd7fab225abb1e47b0f`）
- `Collaboration/tasks/015-quality-audio-and-panel.md`（SHA256: `bcc8f83b454b76d972af2dea70865b37e09eb4be0fedbf64dd51060d954f71ce`）

## 指摘事項

### 1. 【高】BuildMessage() で `color=Color.clear` が直後の material 差し替えで打ち消され、仮の文字が見えてしまう（追修正4の要求に違反）
- 場所: `LoopDemo.cs` `BuildMessage()` 内
  ```csharp
  message.color=Color.clear; text.GetComponent<MeshRenderer>().material=RoomVisuals.TextMaterial(font);
  ```
- 原因: `TextMesh.color` は現在の Renderer マテリアルのカラーを書き換えるだけの実装。この行では **先に** `color=Color.clear` を設定し、**直後に** `renderer.material` を `RoomVisuals.TextMaterial(font)` へ丸ごと差し替えているため、透明化した効果は捨てられ、新しく割り当てたマテリアルの既定色（不透明である可能性が高い）がそのまま使われる。
- トリガ: アプリ起動時、`Start()`→`BuildMessage()` 直後から `LateUpdate()` が計測に成功して `ApplyMessageSize()` が呼ばれるまでの間（過去の追修正1/2のコメントにもある通り「TextMeshは描画時までメッシュを作らない」ため、最低でも1フレームはこの区間が発生する）。この間 `RefreshWorld()` は `Model.Phase==Ready` のとき `message.gameObject.SetActive(show)` を **`messageSized` の条件なしに** 実行するため、仮の文字列（「国」×20文字×4行）がパネル背景なしで視界に浮いて見える。これは追修正4がSol指摘（「仮の文字が1フレーム見える」）を受けて明示的に潰そうとした不具合そのものであり、今回の実装でその修正が無効化されている。
- 修正案: 2文を入れ替える。
  ```csharp
  text.GetComponent<MeshRenderer>().material=RoomVisuals.TextMaterial(font);
  message.color=Color.clear;
  ```
  これで `color` セッターが最終的なマテリアルに対して実行され、透明化が有効になる。

### 2. 【中】固定フォールバック（0.62×0.26）時に実文言の文字サイズが未検証で、旧不具合が再発する可能性
- 場所: `LoopDemo.cs` `ApplyMessageSize()`
  ```csharp
  if(!fixedFallback) { ... message.characterSize*=shrink; ... }
  ```
- 原因: `shrink`（＝`characterSize` の縮小）は「計測成功」経路でのみ適用される。30フレーム計測できず固定サイズへ落ちた場合、`characterSize` は縮小されないまま `0.62×0.26` の固定パネルに実文言（VRの3行メッセージ等）を表示するため、パネル・画面からはみ出す元の不具合（task015の目的2）が再発しうる。
- トリガ: TextMesh のメッシュ生成が30フレーム以内に完了しない環境（低スペック機やビルド直後の初回フレームなど）。
- 修正案: 固定フォールバック時も、仮テキストの `characterSize` に対する既知の縮小率（例えば計測不能時は安全側の固定倍率を掛ける、または固定パネルサイズに合わせた `characterSize` 定数を別途用意する）を適用する。

## 検証（設計仕様との数値整合性）
- `追修正3` に記載の計測値 `(1.86, 0.53)` を前提にすると、`shrink = min(1, 0.56/1.86) ≈ 0.30`、縮小後 `(0.56, 0.159≈0.16)` となり、レビュー対象文中の「measured (0.56,0.16) shrink=0.30」と数式上一致する。
- `Normalize()` は全音声で peak を明示値に固定しており、クリップ（|sample|>1）は理論上発生しない（`RoomTone` も含め確認済み）。「波形 clipped 0」の主張はコード上矛盾しない。
- `LateUpdate()` の計測経路は `renderer!=null` と `size.x>0 && size.y>0` を必ずチェックしており、追修正2で問題になった `NullReferenceException`（`GetComponent<MeshFilter>()`）は解消されている。例外0件は現在のコード構造と整合する。
- `固定フォールバック時は 0.62×0.26 のまま（余白0.05は計測時のみ）` は `fixedFallback ? bounds : bounds+0.05` の分岐で正しく実装されている。

以上はコード読解による静的な整合性確認であり、実際のビルド実行・ログではない。

## 判定

**request_changes**

指摘1（仮の文字が見える）は task015 の目的2および追修正4の受入条件に直接反する具体的な不具合であり、修正必須。指摘2は低頻度だが再発防止のため対応を推奨。

## 未確認事項
- Unity batchmode コンパイルは実施していない（エラー0・警告0は未確認）。
- 実際の音の聞こえ方（音量バランス・不快さ）は未確認（タスク自体もユーザー確認待ちと明記）。
- 波形/スペクトル画像、パネルのスクリーンショットは本レビューでは受領しておらず未確認。
- `RoomVisuals.cs`（`TextMaterial(font)` の既定色、`Chime`/`Shot`/`Latch`/`Open`/`Sound` フィールドの可変性・初期設定）は本スナップショットに含まれず未確認（指摘1の実際の見え方の深刻度に影響）。
- `DemoRig.cs`（`PreparationMessage` の内容・最大文字数、`rig.View`/`IsVR`/`CanStart` 等の実装）は含まれず未確認。
- 実機・Editor実行でのログ出力（`Debug.Log` の実測値、例外0件の実行時確認）は未確認。