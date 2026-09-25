# 独立レビュー（タスク024: A. Rules private化 / B. inputs一覧分離・UpdatePublic整理）

## 対象と参照SHA256
- `Assets/LoopRoom/Scripts/LoopModel.cs` — `fddd6568ac4bed02fdaf0a1c50f60183d80e3249bb9bbe064d41eb6cf4f9f4f0`
- `Assets/LoopRoom/Editor/LoopModelChecks.cs` — `444e835391c201b957e9b3a5ac975af762d6799d64e1b18bba46956ea1ab74c2`
- `Assets/LoopRoom/Scripts/DemoRig.cs` — `120db643cea494fa1819c1e6bbabe2574fd8d89efe98273a977978d923865cfe`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs` — `a146e206f96a4befea18897759f46bdabc74f51b3f73e7562f511703cd033f30`
- `Tests/LoopModel.Tests/Program.cs` — `243897501acf3769e5fb1f1a9780fbc59372669e7ff74d18c0322bf55a5fb9e3`
- `Collaboration/tasks/024-deferred-cleanup.md` — `9b2c5375667a5803d1e25fbcdcd927973db5b5505f4ea32c78cc6f8ca012f28b`

## 指摘事項

### [高] LoopDemo.cs がレビュー対象に含まれていない
- 対象: `Assets/LoopRoom/Scripts/LoopDemo.cs`（git status上は変更済み `M` だが、SNAPSHOTに本文が渡されていない）
- トリガー: プロンプト本文は「LoopDemo.cs の変更は2行だけ（`Model.Rules.exitOpens`→`Model.ExitOpens`、`UpdatePublic(rig.IsVR,playing)`→`UpdatePublic(rig.IsVR)`）」と主張しているが、その差分自体は本レビューの入力に一切含まれていない。
- 影響: `RoomVisuals.UpdatePublic` はシグネチャが `(bool vr, bool active)` → `(bool vr)` に変わっており、呼び出し側を直していなければ即ビルドエラーになる。また `LoopModel.Rules` は `rules.Clone()` を毎回返す設計に変わったため、LoopDemo側が毎フレーム `Model.Rules.exitOpens` のような形で参照し続けていれば、タスクが意図した「複製の毎フレーム生成回避」（GC負荷回避）が実現されていないことになる。いずれも本文なしには判定不能。
- 修正: LoopDemo.cs の実差分をレビューに提示すること。特に (1) UpdatePublic呼び出しが1引数になっているか、(2) 毎フレームパスで `Model.Rules.xxx` ではなく `Model.ExitOpens` 等の専用プロパティ／開始時1回のキャッシュを使っているか、の2点の確認が必須。

### [中] RoomAnchorChecks の内容が未提示
- 対象: `Tests/LoopModel.Tests/Program.cs` 内 `RoomAnchorChecks.Run()`（`RoomAnchorChecks.cs` 自体がSNAPSHOT外）
- git statusにも変更対象として現れていないため今回のタスクでは不変更と推測できるが、受入条件に書かれた「20+12 PASS」のうち12件側の中身は本レビューでは確認できない。

### [低] 新規テストの検証範囲がやや非対称
- 対象: `Assets/LoopRoom/Editor/LoopModelChecks.cs` の `"Model owns a copy of its loop rules"` テスト
- コンストラクタに渡した `rules` を書き換えた後、`m.Rules.firstShot` の値比較のみで終わっており、`Advance()` を通した実動作確認をしていない（直後の `"Changing returned rules cannot alter model behavior"` テストは `Advance` まで検証していて対照的）。実害はないが一貫性の観点で軽微。

### [低] タスク番号のコード内埋め込み
- 対象: `Assets/LoopRoom/Scripts/DemoRig.cs` のコメント（`// ... (task024-b) ...` 等）
- 既存コードの慣習（task021, task018 等）を踏襲しているため許容範囲だが、AGENTS.mdの「タスク参照はレビュー記録側に残す」方針とはやや外れる。ブロッカーにはしない。

## コードリーディングで確認できた点（欠陥なし）
- `LoopModel.cs`: `rules` が `readonly` な private フィールドになり、`Rules` プロパティは `rules.Clone()` を返す。`ExitOpens` 読み取り専用プロパティも追加済み。`LoopRules` の既存公開API（フィールド・`Clone`・`Validate`・`MinInterval`）は変更されておらず、`JsonUtility` でのシリアライズ結果はクローンでも元と同一値になるはずで矛盾はない。
- `LoopModelChecks.cs`: `Check` 呼び出しを数えると19件で、`Program.cs` の `existing.Count == 19` と一致。
- `DemoRig.cs`: `pollInputs`（`FloorReady` 用一時バッファ）、`xrPrepInputs`（`StartXR` コルーチン用一時バッファ）、`floorInputs`（Floor確定済みの永続リスト、`StartXR` のみが書き込み `FloorReady`/`TryGetBoundaryPoints` は読み取り専用）に分離されており、両呼び出し元が同一の一時リストを共用する構造にはなっていない。Unityはシングルスレッド実行のため、コルーチンの `Clear→Add` 途中で `FloorReady` が割り込む余地もない。挙動自体（Floor判定ロジック）は変更されていないように見える。
- `RoomVisuals.cs`: `UpdatePublic(bool vr)` から未使用の `active` 引数が削除され、本体ロジックは変更なし（`Spectator.enabled=vr` 等)。9-eの「暗転中も観客表示を継続する」判断とも整合。
- `Program.cs`: `existing.Count == 19` の後にインラインで1テストを追加実行し `"PASS: 20 model checks"` を出力しており、19+1=20の数字は一致している。

## 判定
**request_changes**

理由: 提供された範囲（LoopModel.cs / LoopModelChecks.cs / DemoRig.cs / RoomVisuals.cs / Program.cs）自体には機能的な欠陥は見当たらないが、タスクの中核である「呼び出し側（LoopDemo.cs）の整合性」を検証できるソースが提供されておらず、UpdatePublicのシグネチャ変更や `Rules`→`ExitOpens` 移行が実際にビルド可能な形で反映されているか独立に確認できない。この点を解消（LoopDemo.csの差分提示）してから再レビューすべき。

## 未確認事項（このレビューでは検証していない）
- `Assets/LoopRoom/Scripts/LoopDemo.cs` の実差分（本体未提示）
- `RoomAnchorChecks.cs` の中身（12件のテスト内容）
- Unity Editor batchmode 0/0 コンパイルの実行結果
- dotnet相当のテスト実行結果（20+12 PASSという主張の実行証跡）
- desktop自動実行20秒・4周回・drift 4パターンのスクリーンショット等の実機/実行証拠
- `JsonUtility` によるセッションログの `timings=Model.Rules` シリアライズが複製でも同一値になることの実行時確認（コード上は妥当と判断できるが実測はしていない）