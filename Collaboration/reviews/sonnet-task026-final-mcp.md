# task026 (FrameStats) 追修正2適用後 — 独立レビュー

対象SHA256（提供SNAPSHOTと照合）:
- `Assets/LoopRoom/Scripts/FrameStats.cs`: `3d306e13107607ad5a8854196e68ce21cb38aaeb000bdfa1cf8093a22dd31aef`（前回Sol/Sonnetがレビューした版のSHA `423fbe72...` とは別物）
- `Assets/LoopRoom/Editor/FrameStatsChecks.cs`: `b3cb0de16c3497d31f6ab10c659b6aba040c008d85c8ed5bc6cbe5292e2a9b8f`
- `Tests/LoopModel.Tests/Program.cs`: `5c21c1f54105450bda32790263dd25fef669df22161bf1ca34f0debdbd6418a7`
- タスク定義: `4bc5ba1d7379d894e60502e0156e2e2b613afa5088ecf1b5d2f2c30eee53e48d`
- 前回交換後レビュー（Sol・参考）: `61e432e28c0d33b03377d83d42a7a5453c532711b4519f2bc6e290ad76d6f0bb`
- 前回交換後レビュー（Sonnet・参考）: `ff8a67bcbd5099df8fb2b899bdaf90f609a32d23069386bcf3be325f78b696ba`
- 手順資料: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`、`fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`

ツールは使用せず、渡された本文のみで判定しています。先にコードを独立に検証し、後段で前回2件の指摘との照合を行いました。

## 前回Sol指摘2件の解決状況

**1. ms換算後の非有限化（旧: Low）→ 解消を確認**
`FrameStats.cs:41-45` で `dtSeconds > MaxSeconds(3600.0) ? MaxSeconds : dtSeconds` により**秒の段階で**飽和させてから ×1000 している。`double.MaxValue` を渡しても `clampedSeconds=3600.0`、`ms=3,600,000.0` と常に有限になり、`sumMs`・`maxMs` が Infinity化する経路が存在しない。`FrameStatsChecks.cs` の "double.MaxValue keeps MeanMs and MaxMs finite" テスト（末尾）で `MeanMs`/`MaxMs` の有限性と `Dropped==1` を検証しており、要求通り。

**2. 「1.5倍ちょうど」の厳密境界検証（旧: Low）→ 解消を確認**
`FrameStats.cs:34` でコンストラクタ内に `droppedThresholdMs = targetFrameMs * 1.5` を1回だけ計算・保持し、`Add()` はこの値とだけ比較（式の二重実装を回避）。`FrameStatsChecks.cs` の "Binary-exact 1.5x boundary" テストは 128Hz（フレーム時間 7.8125ms、その1.5倍 11.71875ms）を使用しており、両者とも2進数で有限桁の分数（128=2⁷、3/256=3×2⁻⁸）のため、`double` 演算で丸め誤差なく厳密一致する。at／just below（`NextDown`）／just above（`NextUp`）を個別にAssertしており、境界そのものを直接検証できている。`NextUp`/`NextDown` は `BitConverter.DoubleToInt64Bits±1` によるビット隣接値取得で、正の有限値に対しては妥当な実装。

両指摘とも設計変更（秒の段階での飽和、閾値の一元計算）により根本対応されており、再発防止の検査も追加されている。

## Findings

深刻な欠陥は見当たりませんでした。軽微な点のみ:

- **Info** `Assets/LoopRoom/Scripts/FrameStats.cs`（`Add()` 末尾の `if (index >= BucketCount) index = BucketCount - 1;`）: 直前の3分岐（`ms<100`/`ms<1000`/else）で既に `index` は `BucketCount-1` 以下に収まるため、この行は到達不能な防御コード。害はなく修正不要。
- **Info** `Assets/LoopRoom/Editor/FrameStatsChecks.cs`（`NextUp`/`NextDown`）: ゼロ・負値・Infinity付近では単純なビット+1/-1は成立しないが、使用箇所は正の有限値（`3.0/256.0`）のみなので実害なし。

バケット境界（`ms==100.0`、`ms==1000.0` ちょうど）を手計算で追跡しましたが、`BucketUpperEdgeMs` との整合を含めoff-by-oneは見つかりませんでした。

## Program.cs / 件数整合性

`Program.cs:26` の `Require(frameStats.Count == 11, ...)` と `FrameStatsChecks.Run()` の実際のCheck数（数え上げで11件）が一致。`Tests/LoopModel.Tests/tests.txt` ログの `PASS: 11 frame stats checks` / `test exit=0` とも整合（ログ文面の確認であり、自分で実行した結果ではありません）。

## 判定

**approve**

前回Solが指摘した2件（ms換算後の無限大化、1.5倍ちょうど境界の厳密性）は設計変更により根本解決されており、追加された検査（100ms超5%以上でのP95、単一外れ値、巨大有限値の飽和、境界ちょうど/直下/直上、`double.MaxValue`）もコード上の分岐と整合しています。新規のブロッカーは確認できませんでした。

## 未確認事項

- テスト・ビルド・Unity統合コンパイル・batchmodeは実行していません（`tests.txt` の文面確認のみ）。
- `LoopModel.Tests.csproj` は本SNAPSHOTに含まれず、新規テストファイルの Compile Include を直接確認できていません。
- `LoopDemo.cs` / `DemoRig.cs` は今回のSNAPSHOTに含まれず、「追修正2で変更なし（`task026-diff.md` のとおり）」という前提を文書上でしか確認できません。
- F2運営表示の実画像（`overlay-compare.png` 等）は提供されておらず、パネル内に収まっているかの視覚確認は未実施です。
- Quest 3 / Air Link 実機での `XRDisplaySubsystem.TryGetDisplayRefreshRate` の実挙動、長時間セッションでの累積誤差・メモリ使用の実測。
- `Screen.currentResolution.refreshRateRatio`（DemoRig側、前回Sonnet指摘）のUnityバージョン依存性は、今回のSNAPSHOTに実体が含まれないため再検証できていません。