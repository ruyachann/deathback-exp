# task026 (FrameStats) 追修正後レビュー（独立検証）

対象SHA256（提供スナップショットと照合）:
- `Assets/LoopRoom/Scripts/FrameStats.cs`: `423fbe72fca574d18edcc33ff9869404a6245543688c5e5bf6c017606ec68f63`
- `Assets/LoopRoom/Editor/FrameStatsChecks.cs`: `bdf987d09d54e1f976442928e1f95ecd9df08e63ef45afe339043c202a4fc538`
- `Tests/LoopModel.Tests/Program.cs`: `942a3c839c96c30f067299722ccc6f5a51654642f829f1c0fabbada02437a877`
- `Collaboration/reviews/task026-diff.md`（LoopDemo.cs/DemoRig.csの差分）: `1e12937a8cc91c5b352e449aa5e0359e49b6f556ff6f5ac0007d991496911231`
  - 差分内記載の対象: LoopDemo.cs `89e39fbb8c0a19ea6e818173239d04bd2bd53ee9d3e2536d9a08bdaa61fd52d9`、DemoRig.cs `6f26cc933cb920d12f9d81aacb93aa62357b591950dc2e6fc7132cc9fe38c855`
- `Collaboration/evidence/20260925-frame-stats/session-frames.txt`: `8cd5f883290d5215322256f20342bcdbafeb323c142844a2a1196508203a73cf`
- 前回独立レビュー（参照用・追修正前）: Sol `94534767fae250ede0613f104a95159d45f200b60b2922cdb69793c426055bff`、Sonnet `dfc8f4644faf03fa05d400a6438877c61f7a9564c7f2aa7c942c3d76ded67259`

ツールは使用していません。以下は提示された本文のみによる判定です。

## 追修正1〜7の反映状況

1. **P95のオーバーフロー区間（Sol指摘1, Medium）→ 解消**
   `FrameStats.cs` 47〜53行目で `ms<100`/`ms<1000`/それ以外の3分岐に変更され、100〜1000msを10ms刻み（`CoarseBucketCount=90`）で確保。`ComputeP95()`（65〜77行目）は該当バケットの上端を返す実装で、`maxMs` を返す分岐はコード上に存在しません。`BucketUpperEdgeMs`（79〜81行目）で最終バケットの上端が1000msに固定されているため、100ms超が5%を占めるケースでもP95が青天井の最大値に張り付くことはありません。

2. **Begin直後の最初のdt混入（Sol指摘2, Medium）→ 解消**
   `task026-diff.md` の `Begin()` 差分で `frameStatsSkipFirst=true` をセットし、`Update()` 差分側で `wasStatsPhase` が真になった最初の1回だけ `Add()` をスキップしています。証跡 `session-frames.txt` の `maxMs` が 743.74ms→36.44ms、`dropped` が2→1に改善しており、症状と整合しています。

3. **Advance後の状態だけで判定（Sol指摘3, Low）→ 解消**
   `Update()` 差分で `Model.Advance()` 呼び出し**前**に `wasStatsPhase` を確定させてから `Add()` の可否を判定する順序に変更済みです。

4. **巨大だが有限な値でのバケット添字（Sol指摘4, Low）→ 解消**
   `Add()` 48〜51行目で、値そのものを見て `ms<100`/`ms<1000`/それ以外の3分岐に振り分けてから `BucketCount-1` へ飽和させ、除算結果を直接添字に使っていません。`FrameStatsChecks.cs` の「huge finite dt」検査（`1.0e10` 秒）でも例外なく完走する前提のテストが追加されています。

5. **F2表示の折り返し（撮影指摘5）→ テキスト上は短縮済み**
   `task026-diff.md` の該当ハンクで「フレーム 0.9 / P95 1.5 ms・落ち 2・165Hz」の形に短縮され、`boxHeight` に24加算されています。ただし `overlay-compare.png` の画像自体はSNAPSHOTに含まれておらず、実際にパネル内へ収まったかは文面上の推測に留まります（後述）。

6. **検査4件の追加（Sol指摘）→ 反映済み**
   `FrameStatsChecks.cs` に「100ms超5%以上でのP95」「極端な単一外れ値でのP95」「巨大有限値の飽和」「1.5倍ちょうどの境界」の4検査が追加され、`Program.cs` の `Require(frameStats.Count == 10, ...)` と一致しています。

7. **バケット数コメントの不一致（Sonnet指摘, Minor）→ 解消**
   11・13・14〜16行目のコメントが実装（`BucketCount=490`、100ms超も有限幅バケット）と整合する内容に更新されています。

## 残る軽微な指摘

### [低] `DemoRig.GetTargetHz()` の `Screen.currentResolution.refreshRateRatio` はUnity 2022.2以降のAPI
- 該当: `task026-diff.md` DemoRig.cs差分（`@@ -199,6 +199,24 @@` ハンク内）
- 発生条件: プロジェクトのUnityバージョンがこれ未満の場合コンパイルエラーになる。
- 修正案不要（前回Sonnet指摘の再掲、対応不要とされたまま）。Unity Editor側の実コンパイル確認とバージョン明記が必要。

### [情報] `frameStats` 再生成条件がdoubleの厳密等価比較
- 該当: `task026-diff.md` LoopDemo.cs差分の `Begin()`（`if(frameStats==null || frameStats.TargetHz!=targetHz) frameStats=new FrameStats(targetHz); else frameStats.Reset();`）
- 発生条件: `GetTargetHz()` の戻り値がフレームごとにわずかに変動する場合、`Reset()` の代わりに毎回 `new` される。
- 実害なし（状態のクリアという意味では同等の結果になるため、修正不要）。

## テスト・証跡の確認（文面のみ）

- `session-frames.txt` に `PASS: 20 model checks` / `PASS: 12 room anchor checks` / `PASS: 10 frame stats checks` / `test exit=0` / `csc exit=0` の記載があり、`Program.cs` の `Require(frameStats.Count == 10, ...)` および3ブロック構成と数が一致します。
- `frames.dropped` が2→1、`maxMs` が743.74ms→36.44msに改善した記録は、追修正2（最初のフレームスキップ）の効果と整合します。
- これらはあくまで提示されたログ文字列の内容確認であり、私自身がビルド・実行して得た結果ではありません。

## 判定

**approve**

Solが指摘した4件（P95のオーバーフロー、開始直後の混入、Advance前後の判定順序、巨大値での添字破綻）およびSonnetの軽微な指摘（コメント不一致）はコード上で解消を確認できました。残る指摘（refreshRateRatioのAPIバージョン依存、frameStats再生成の等価比較）はブロッカーではありません。

## 未確認事項

- `Tests/LoopModel.Tests.csproj` の内容が今回のSNAPSHOTに含まれておらず、新規テストファイルの参照が正しく組み込まれているかをファイル内容で直接確認できていません（ビルド成功ログの文面のみ）。
- `LoopDemo.cs` / `DemoRig.cs` の全文が提供されておらず、`task026-diff.md` の差分のみで判断しています。1フレーム中に `Advance` が複数回呼ばれる分岐の有無など、差分外の文脈は確認できません。
- Unity Editorでの統合コンパイル、および `refreshRateRatio` APIとプロジェクトのUnityバージョンの整合性。
- `overlay-compare.png` の実画像（パネル内に収まっているかの視覚的確認）はSNAPSHOTに含まれていません。
- Quest 3・Air Link実機での `XRDisplaySubsystem.TryGetDisplayRefreshRate` の実挙動、および長時間セッションでのメモリ使用の実測。
- `session-frames.txt` に記載されたテスト実行・ビルドを私自身は実行していません（記載文面の確認のみ）。