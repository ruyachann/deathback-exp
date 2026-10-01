# task026 (FrameStats) 追修正3適用後 — 独立レビュー

他のレビュー報告（sol-task026-final-mcp.md、sonnet-task026-final-mcp.md 等）は先に読まず、まずSNAPSHOT本文のみでコードを独立に検証しました。以下は自分の分析結果です（後段で既存レビューとの照合を付記）。

## 対象SHA256（引用のみ、自分では再計算していません）

- `Assets/LoopRoom/Scripts/FrameStats.cs`: `59174398010fed949be68cf94145e85a41ac320d6203db248b89a692bb036ce8`
- `Assets/LoopRoom/Editor/FrameStatsChecks.cs`: `aa24ea083167cb1d19bd1c3132b3e9d1dc79ada3b8ba1a2d06dd45ecd0f32f2c`
- `Tests/LoopModel.Tests/Program.cs`: `cfe180a6bd5b8721a9dd3f4315c08d09d100a45861eb6c55d61bbe24e5b02d28`
- `Collaboration/tasks/026-frame-stats.md`: `b2c0ab8804d95dacfd7fa522bf8a8a8a4cdcffab4b29ec5380cf2d65c7f4ddd0`
- `Collaboration/reviews/task026-diff.md`（LoopDemo.cs/DemoRig.csの差分本体）: `5b36894142c9e74430e13d85bb77adaeba0fda16593800ba313a1729892c1c9a`
- `Collaboration/evidence/20260925-frame-stats/session-frames.txt`: `ba88bc610a296850d29061f387a266123d4be417ca934ef3d62f494ed7ae1c25`

## 前回Sol指摘（追修正2適用後のコードに対して）2件の解決状況

**1. 3600秒飽和によるDropped過少計上 → 追修正3（targetHz範囲を[1,1000]に制限）で構造的に解消を確認**

`FrameStats.cs`のコンストラクタで `targetHz < 1.0 || targetHz > 1000.0` を例外にしたことで、`targetFrameMs`（最大1000ms、targetHz=1のとき）と`droppedThresholdMs`（最大1500ms）は必ず `MaxSeconds`飽和後の値（3600.0秒＝3,600,000ms）より大幅に小さくなります。手計算で確認: targetHz=1 → threshold=1500ms、dtSeconds=6000秒でも `clampedSeconds=3600` → `ms=3,600,000` > 1500 → Dropped++。Solが挙げた `1.0/3600.0` Hzのような極端な入力自体がコンストラクタで例外になるため、指摘された過少計上のシナリオは発生し得ません。解消済みと判断します。

**2. int overflow → long化で解消を確認**

`Frames`、`Dropped`、`buckets`（`long[]`）がすべて`long`。165Hzの連続稼働でも`long`の範囲に到達することは現実的にないため妥当です。

## 追加で自分が検証した点（新規欠陥の有無）

- `FrameStatsChecks.cs`の実チェック数を数え上げ → 12件（Program.csの`Require(frameStats.Count == 12, ...)`と一致）。追修正3で「Target Hz outside [1, 1000] throws ArgumentException」チェックが追加され11→12になったことも整合しています。
- 境界値の手計算: `ms==100.0`は`else if(ms<1000.0)`分岐に入り`index=400`（Coarseの先頭）、`BucketUpperEdgeMs(400)=110.0`。`ms==1000.0`は`else`分岐で`index=489`、`BucketUpperEdgeMs(489)=1000.0`。off-by-oneなし。
- 128Hz境界テスト: `targetFrameMs=7.8125ms`、`1.5倍=11.71875ms`はいずれも2進で正確に表現できる値（分母が2の冪）。`atBoundarySeconds=3.0/256.0`も同様に正確な2進小数であり、`×1000`の乗算結果も表現可能範囲内のためIEEE754で丸め誤差なく一致します。テスト設計は妥当です。
- `DemoRig.GetTargetHz()`（`task026-diff.md`より）はVR/desktopともに`hz >= 1 && hz <= 1000`の範囲外なら既定値（72/60）を返す実装になっており、`FrameStats`コンストラクタの許容範囲と一致。`Begin()`側でも`GetTargetHz()`の戻り値をそのまま`new FrameStats(targetHz)`に渡しており、例外発生の余地はありません（追修正3-3の要求を満たしている）。
- `Collaboration/evidence/20260925-frame-stats/session-frames.txt`の数値整合性: `targetHz=164.917` → `threshold≈9.1ms`、`maxMs=36.44ms`で`dropped=1`は辻褄が合います。また「最初の版: max=743.74ms dropped=2」→修正後「max=36.44ms dropped=1」という記述は、Begin直後の最初のdtスキップ（追修正2-2）が効いていることを裏付けています。ただしこれは提供テキストの内容確認であり、自分で実行した結果ではありません。

## Findings（いずれも軽微、ブロッカーではない）

- **Info** `Assets/LoopRoom/Scripts/FrameStats.cs`の`Add()`末尾 `if (index >= BucketCount) index = BucketCount - 1;`: 直前の3分岐で既に`index`は範囲内に収まるため到達不能な防御コード。実害なし、修正不要。
- **Info** `Assets/LoopRoom/Editor/FrameStatsChecks.cs`の`NextUp`/`NextDown`（`BitConverter`のビット±1）: ゼロ・負値・Infinity付近では成立しないが、使用箇所は正の有限値（`3.0/256.0`）のみなので実害なし。
- **Info** `sumMs`をdoubleで単純累積しているため、極めて長時間（数週間〜）のセッションでは浮動小数点の桁落ちにより`MeanMs`にわずかな誤差が蓄積し得る。実用上のQuest 3セッション時間では問題にならない水準。

## 未確認事項（実行・実機系）

- テスト・ビルド・batchmode・Unity統合コンパイルは実行していません。`session-frames.txt`記載の`csc exit=0` / `test exit=0` / `PASS: 12 frame stats checks`は文面の確認にとどまります。
- `LoopDemo.cs`・`DemoRig.cs`の完全な現物ファイルは提供されておらず、`task026-diff.md`の差分のみで判断しています。Model.Phase遷移の全体像、F2表示トグルの完全なロジック、SessionLog全体のシリアライズは差分から推測した範囲でのみ確認できています。
- F2運営表示のスクリーンショット、行がパネル幅に収まっているかの視覚確認は未実施（画像は本SNAPSHOTに含まれていません）。
- Quest 3/Air Link実機での`XRDisplaySubsystem.TryGetDisplayRefreshRate`の実挙動、可変リフレッシュレート下でのセッション中の`targetHz`固定の妥当性。
- `LoopModel.Tests.csproj`の内容（新規テストファイルのCompile Include）は今回未提供のため未確認。

## 判定

**approve**

追修正3で対応された前回Sol指摘2件（Dropped過少計上、int overflow）は、ロジックを独立に追跡した結果いずれも根本的に解消されていると判断しました。新たなブロッカーとなる欠陥は見つかっていません。残る未確認事項は実行・実機系の検証のみです。