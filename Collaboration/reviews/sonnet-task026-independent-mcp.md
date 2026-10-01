# task026 (FrameStats) 独立レビュー

対象SHA256（すべて提供されたスナップショットの値と照合済み）:
- `Assets/LoopRoom/Scripts/FrameStats.cs`: `49718dcd70affc8e1be663e42a998a25dab3e7335a48c631ed65c48b0a57e8e6`
- `Assets/LoopRoom/Editor/FrameStatsChecks.cs`: `ecdd3d561d472da355b8f39abaaec34df819d03329424ff7bddf180f6fc83abb`
- `Tests/LoopModel.Tests/Program.cs`: `38c18123e002366e116ce8324187c57570dab37f6b057843643c66d06c0f9060`
- `Tests/LoopModel.Tests/LoopModel.Tests.csproj`: `2b1531f45af36eb4cdcd5e1061fff33610eb1c596cc774f800dabc6b52752db7`
- `Collaboration/reviews/task026-diff.md`（LoopDemo.cs/DemoRig.csの差分）: `a8465af736dca3b1512f2721d7f59a07c1be901a85d947b6cc59ca37165d40ab`
- 実行証跡 `Collaboration/evidence/20260925-frame-stats/session-frames.txt`: `8d6ca21303b3ebaf8324bb3380e6b0b7bee0ffcc462b7e09d5fc7ab433082039`

## 検出事項

### [Minor] FrameStats.cs:11-13 バケット数のコメントと実装の食い違い（実害なし）
- 該当: `// 400 buckets cover [0,100)ms in 0.25ms steps` のコメントと `const int BucketCount = 401;`
- トリガー: 読んだ人が「400個の配列で100ms全体をカバーし、超過分は最後の要素で兼用」と誤解しうる。実装自体は401要素（index 0〜399が通常区間、400番目がoverflow）で数値的には正しい。
- 修正案: コメントを「401個の配列（0〜399が0〜100msの区間、400番目がoverflow）」のように書き換える。動作に影響しないため必須ではない。

### [Minor] DemoRig.cs差分（task026-diff.md 該当ハンク `@@ -199,6 +199,24 @@`） `Screen.currentResolution.refreshRateRatio` のAPI要件
- 該当: `var rate = Screen.currentResolution.refreshRateRatio.value;`
- トリガー: `refreshRateRatio` はUnity 2022.2以降で追加されたAPI。プロジェクトのUnityバージョンがこれより古いとコンパイルエラーになる。
- 確認: 提供された証跡ログでは `csc exit=0` かつ desktop autorun でのビルド `0/0` が示されており、少なくとも今回のビルド環境ではコンパイルが通っている。ただしこれはEditor統合ビルドではなくcsc単体テストのビルドである可能性があり（LoopDemo.cs/DemoRig.csはcsprojに含まれていない＝Tests側では検証対象外）、Unity Editor側での実コンパイルは本レビュー範囲のスナップショットからは確認できない。
- 修正案不要（バージョンさえ満たしていれば問題なし）。Unity Editor側のコンパイル確認をUnityバージョンとあわせて明記すること。

### [Minor] LoopDemo.cs差分（`@@ -461,6 +486,9 @@`付近） F2表示の「セッション中だけ更新」の解釈
- 該当: `showFrameStats=(!rig.IsVR || privateOverlay) && frameStats!=null;`
- トリガー: `frameStats` は最初の`Begin()`以降ずっとnullでなくなるため、セッション終了後（Finished/TimedOut等）もF2表示に前回セッションの数値が残ったまま表示され続ける可能性がある。タスク仕様は「値の更新をPlaying/Blackout中に限定する」ことであり、これは`Add()`呼び出し条件で満たされている。しかし文言「セッション中だけ更新」を「表示自体もセッション中だけ」と読む場合は不足。
- 修正案（必要なら）: `showFrameStats` の条件に `(Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout)` を追加する。ただし仕様の読み方次第で必須ではないため、計画担当への確認事項として指摘するに留める。

### [Info] 証跡ログ `session-frames.txt` の `maxMs=743.7431216239929ms`（dropped 2件のうち1件）
- タスク自身も「開始直後や撮影の影響が未特定」と記載している通り、原因未特定。コード上の欠陥ではなく観測事実。今後の実機（Air Link）確認時に再発するか要注視。

## 検証済み事項
- `FrameStats.cs` のヒストグラムP95近似・Dropped判定（`ms > targetFrameMs*1.5`）・例外送出・Reset・非有限/負値の無視は、静的にコードを追った限りすべてタスク仕様通りに実装されている。
- `FrameStatsChecks.cs` の6件はタスクが要求する6項目（一定dtでの平均/P95/最大、1.5倍超のみDropped、非有限/負の無視、Reset、不正Hzで例外、フレーム0で全て0）と一対一で対応している。
- `Program.cs` の `Require(frameStats.Count == 6, ...)` および3ブロック（20+12+6）の合計はタスク・診断で言及された「20+12+6 PASS」と整合する。
- `LoopModel.Tests.csproj` の新規Compile Includeパスは相対パス的に正しい。
- 提供された`session-frames.txt`では `csc exit=0`／`test exit=0`／全PASS行が確認できる。

## 未確認事項（本レビューでは検証不可）
- Unity Editor上でのLoopDemo.cs/DemoRig.cs実コンパイル（Tests/LoopModel.Tests.csprojの対象外）およびUnityバージョンの`refreshRateRatio`対応可否。
- F2運営表示のスクリーンショット（タスク受入条件2で要求されているが、本スナップショットには含まれていない）。
- Quest 3実機・Air Linkでの`XRDisplaySubsystem.TryGetDisplayRefreshRate`の実際の戻り値・動作。
- 743msスパイクの原因切り分け（開始直後/撮影影響/フォーカス関連）。
- 長時間セッションでのメモリ使用実測（設計上は固定サイズだが実測はしていない）。

## 判定
**approve**

コード自体（FrameStats.cs、FrameStatsChecks.cs、テスト配線、LoopDemo/DemoRigの差分）に機能上の重大な欠陥は見当たらない。指摘は軽微なドキュメント/仕様解釈レベルであり、ブロッカーではない。ただしタスク受入条件2の「F2表示の画面」証拠は本スナップショットに含まれておらず、実機（Air Link）検証も未実施であるため、最終受入前にこの2点の確認を担当者に依頼すること。