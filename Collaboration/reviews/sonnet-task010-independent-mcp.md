# task010 独立レビュー

対象ファイル（SHA256を引用して確認済み）:
- `Assets/LoopRoom/Scripts/LoopModel.cs`（sha256: `ec04e9dca75a15641d0c2d4eec35dff0211f6340ca26e48617819852ffce1d96`）
- `Assets/LoopRoom/Editor/LoopModelChecks.cs`（sha256: `3cc1aca7db8a5a0d2d331ad82df263dd2e14745dfc929acbfada0e4687450777`）
- `Tests/LoopModel.Tests/Program.cs`（sha256: `f59bf9e766f983865f76532ca635d8b2647ddae00df489f078952f81a0733eb3`）
- `Assets/LoopRoom/Scripts/LoopDemo.cs`（sha256: `9d6925b4e3dcd36edeb2d338f1f39bb9c479bebf0a2d2bfb4caa1380d0e39097`、参照のみ）
- `Collaboration/tasks/010-rules-copy-and-minimums.md`（sha256: `e2e35725ed70e397e5f0e5d757013e6b0f86a1136b0a729155d587e7866b385a`）

## 指摘

### [Medium] `LoopModelChecks.cs` の境界テスト（"Minimum interval boundaries are enforced"）が searchShot 側の条件を実質検証していない
- **場所**: `LoopModelChecks.cs` の `makeTooSmall` 配列2番目（`r=>{r.firstShot=LoopRules.MinInterval;r.searchShot=LoopRules.MinInterval*1.5;}`）
- **発生条件**: この2番目のケースは、他フィールドをデフォルト値（`exitCloses=12`）のままにして `firstShot=0.05, searchShot=0.075` だけ変更する。この場合 `LoopRules.Validate()` 内の**既存の順序チェック** `exitCloses > searchShot`（12 > 0.075）が先に真になり `ArgumentException` を投げる。テストは例外が出たことしか確認していないため（`bad=true` で PASS 扱い）、目的である「`searchShot - firstShot < MinInterval` の新規チェック」が実際に発火したかどうかを区別できていない。
- **fix案**: 該当ケースで `exitOpens` / `exitCloses` も同時に調整し（例: `r.exitOpens=6; r.exitCloses=6.05f...` のように `searchShot` に収まる値にする）、既存の順序チェックを迂回してから MinInterval チェックのみを踏ませる。あるいは例外メッセージ／型をチェックごとに分けて、どの条件で失敗したかをアサートする。
- 実装コード側（`LoopModel.cs` の `Validate()`）自体のロジックは仕様通り6条件を正しく実装しているため、これはテストの信頼性の問題であり実装バグではない。

### [Low] `LoopRules` のフィールドが public mutable なため、Clone+Validate 後も無効値を注入できる
- **場所**: `LoopModel.cs` の `LoopRules` フィールド定義（`public double firstShot = 6.0;` 等）と `LoopModel` コンストラクタ（`Rules = (rules ?? new LoopRules()).Clone(); Rules.Validate();`）
- **発生条件**: コンストラクタで複製・検証した後でも、`model.Rules.firstShot = 1e-200;` のように外部コードが直接フィールドを書き換えれば、`Advance()` の反復回数上限（MinInterval導入で期待している有界性）が破られる。現状 `LoopDemo.cs` はこれを行っていないため実害はないが、`Advance()` 自体には反復回数のガードがなく、Validate 済みという設計上の保証だけに依存している。
- **fix案**（今回のスコープ外でも良い設計メモ）: `Rules` を不変にする（読み取り専用プロパティ＋コンストラクタでのみ設定可能な構造体/レコードにする）か、`Advance()` に安全弁（最大反復回数での例外）を追加する。

### [Info] `Program.cs` の "PASS: 18 model checks" 表示
- `LoopModelChecks.Run()` の17件＋`Program.cs` 自身の1件で合計18件、という意図と実カウント（`existing.Count==17` の Require）は一致しており、バグではない。表示文字列だけ見ると内訳が分かりにくいが修正必須ではない。

## 確認できた正しい点
- `LoopRules.Clone()`（`MemberwiseClone`）は全フィールドが値型のため shallow copy で十分、正しい実装。
- `LoopModel` コンストラクタで複製後に `Validate()` する順序は仕様通り。
- `Validate()` の6条件（firstShot、searchShot-firstShot、exitCloses-exitOpens、blackout、endingLength、playLimit、いずれも `>= MinInterval=0.05`）は実装として正しく、既定値（6/12/6.5/12/0.16/172/8）すべて満たす。
- 既存チェック（NaN/Infinity、順序、`enforcePlayLimit` 時の180秒）、`MaxStep`（3600秒）関連のテストは変更されておらず壊れていない。
- `LoopDemo.SaveLog()` は `timings=Model.Rules` を記録しており、Inspector側の生 `timings` ではなく検証済み複製を保存する設計がタスク通り反映されている。
- `Advance()` の1周期最小時間（firstShot分岐で最短0.05秒、shot_blocked経由でも0.05+0.05+0.05秒程度）は MinInterval導入により実用上有界になっている。ただしコード上の明示的なガードではなく Validate 依存（上記Low指摘参照）。
- `LoopModelChecks.Run()` の件数（17件）と `Program.cs` の `Require(existing.Count == 17, ...)` は一致。

## 未確認事項
- `Assets/LoopRoom/Editor/LoopModelChecks.cs` / `Tests/LoopModel.Tests/Program.cs` の実行結果（Unity付属Roslyn、dotnet CI）は未実施（レビュー手順上テストは実行していない）。
- `LoopDemo.cs` が本当に「変更なし」であるかは、変更前バージョンの差分が提供されていないため直接確認できない（内容から見て `Model = new LoopModel(timings);` のみで設計と整合するが、diffベースの検証ではない）。
- `Docs/UNITY_ACCEPTANCE.md` の変更内容はスナップショットに含まれておらず未確認。
- Unity Editor での実機/Play確認、コンパイル警告0件の確認は本レビューの対象外（実施していない）。

## 判定

**request_changes**

理由: `LoopModel.cs` の実装自体（Clone、6条件のMinInterval検証）はタスク仕様通り正しいと判断できるが、`LoopModelChecks.cs` の境界テストの1ケース（searchShot側）が意図した検証パスを通っておらず、受入条件「6つの最小値の境界をそれぞれ検査する」を実質的に満たしていない可能性がある。テスト修正（既存の順序チェックを迂回する値設定）を要求する。