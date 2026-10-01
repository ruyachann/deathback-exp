# 独立レビュー（task024・追加提出資料の検証）

## 検証範囲と対象SHA256

- `Assets/LoopRoom/Scripts/LoopModel.cs`: `fddd6568ac4bed02fdaf0a1c50f60183d80e3249bb9bbe064d41eb6cf4f9f4f0`
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `120db643cea494fa1819c1e6bbabe2574fd8d89efe98273a977978d923865cfe`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `a146e206f96a4befea18897759f46bdabc74f51b3f73e7562f511703cd033f30`
- `Collaboration/tasks/024-deferred-cleanup.md`: `9b2c5375667a5803d1e25fbcdcd927973db5b5505f4ea32c78cc6f8ca012f28b`
- `Collaboration/reviews/task024-loopdemo-diff.md`: `64dec8321510065461c6ada6016ff40b093bd86e3b7540463e27e24cbd3b3481`（本文中で `LoopDemo.cs` のSHA256を `8de1a132266c898876a60d69dd6af849f58df416cf84a5dfee2cde5abd81e85d` と主張）
- Sonnet先行レビュー: `e9ca9964e0c4a00e3bd5c3711b48537e84044df22f14e6b609937ecfd687de4f`
- Sol先行レビュー: `964d20fe3c50f767626ec0f6dc6ec877bf49ebae7aa51d387bcdc243f0c8fbc7`

`LoopModel.cs` / `DemoRig.cs` / `RoomVisuals.cs` 本文はコード面で前回から変更されていない前提で再確認したが、読解上の欠陥は見当たらない（`rules` は private readonly、`Rules` は `Clone()` を返す、`ExitOpens` は代入を伴わない読み取り専用プロパティ、`pollInputs`/`xrPrepInputs`/`floorInputs` は用途別に分離済み、`UpdatePublic(bool vr)` は未使用引数を削除済み）。

## 指摘事項

### [中] `LoopDemo.cs` の完全な独立検証ができない
- 対象: `Assets/LoopRoom/Scripts/LoopDemo.cs`（本文未提供、`task024-loopdemo-diff.md` に示された2箇所のdiffのみ）
- トリガー: 提示された diff は279行目付近（`Model.Rules.exitOpens` → `Model.ExitOpens`）と383行目付近（`UpdatePublic(rig.IsVR,playing)` → `UpdatePublic(rig.IsVR)`）の2箇所のみ。この2箇所以外にも旧APIへの参照が残っていないかは、ファイル全文なしには断定できない。
- 補足評価: 同資料に添付された「リポジトリ内の `Model.Rules` 参照」のgrep結果では、`LoopDemo.cs` 内の `Model.Rules` 参照は421行目（`timings=Model.Rules`、ログ出力用の単発参照）のみであり、毎フレーム経路（279行目）は `ExitOpens` に置換済みと読める。これは Sonnet が[高]として挙げた「Cloneが毎フレーム生成され続ける」「ビルドエラーの可能性」という懸念に対する具体的な裏付けになっている。`ExitOpens` の定義（`LoopModel.cs`: `public double ExitOpens => rules.exitOpens;`）や `RoomVisuals.UpdatePublic(bool vr)` のシグネチャとも矛盾しない。
- 修正/今後: 次回同種のレビューでは、部分diffではなくファイル全文（または完全なunified diff）を提示すること。grep結果が「ファイル全体の走査」であることを明記する。

### [中] `RoomAnchorChecks` の内容が今回も未提示（Sonnet先行指摘の再掲・未解消）
- 対象: `Tests/LoopModel.Tests/Program.cs` 内 `RoomAnchorChecks.Run()`（当該ファイル自体はスナップショット外）
- `git status` 上も変更対象に含まれておらず、本タスクでの新規変更ではないと推測できるが、「PASS: 12 room anchor checks」の中身は本レビューでも検証不能。タスク範囲外のためブロッカーとはしない。

### [低] 実行証跡・SHA256の再現性
- `evidence/20260925-cleanup/tests.txt` や `player-log-lines.txt` の抜粋、および各ファイルのSHA256値は、ツールを持たない本レビューでは再計算・再実行による検証ができない。提示された値・出力をそのまま情報として受理したのみ。

## 判定

**approve**

理由: コード本体（`LoopModel.cs`／`DemoRig.cs`／`RoomVisuals.cs`）はSonnetの先行レビュー時から不変で機能的欠陥は見当たらない。Sonnetが[高]としていた「LoopDemo.cs側の整合性が確認できない」という懸念に対し、`task024-loopdemo-diff.md` のdiffとgrep結果は具体的かつ実装（`ExitOpens`／`UpdatePublic`のシグネチャ）と矛盾なく整合しており、ビルドエラーおよびClone毎フレーム生成のリスクは実質的に解消されたと判断できる。残る指摘（RoomAnchorChecks未提示、実行証跡未再現）はいずれもタスク本体の欠陥ではなく、次回以降の確認事項として記録するに留める。

## 未確認事項

- `Assets/LoopRoom/Scripts/LoopDemo.cs` の全文（提示されたのは2箇所のdiffとgrep結果のみ）
- `Tests/LoopModel.Tests/Program.cs` 内 `RoomAnchorChecks` の実装内容（12件の中身）
- Unity Editor batchmode 0/0 コンパイル、dotnet相当のテスト実行、desktop自動実行の再実行検証（すべて提示されたテキスト出力を情報として受理したのみで、本レビューでは未実施）
- Quest 3実機でのXRサブシステム列挙、暗転中の観客表示、到達範囲の確認
- 各ファイルのSHA256値の独立再計算による真正性確認