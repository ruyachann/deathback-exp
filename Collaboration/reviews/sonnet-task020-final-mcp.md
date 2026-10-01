# 独立レビュー: task020 追修正（PlayAreaSettingsFile.cs の CreateNew 対応）

対象ファイルは以下（SHA256はスナップショットに記載のものをそのまま引用）。
- `Assets/LoopRoom/Scripts/PlayAreaSettingsFile.cs` (sha256: 742fbf95effe6a7297bde34c807e0cd291673290a0cead5b45fff1c420ebb9722)
- `Assets/LoopRoom/Scripts/PlayAreaSettings.cs` (sha256: 9fb475cc77ab33e40448a535d210047e70fb7f6069513a1eff69958eed482217)
- `Collaboration/tasks/020-play-area-settings.md` (sha256: e3a7994743f4e4db08d27f5d45bbeecfee64c741f558d0afc877bf75be2af603)

ツールは使用せず、本文のみで判断。

## 指摘事項

1. **[軽微・保守性] コード重複**
   `PlayAreaSettingsFile.cs` 内で「`JsonUtility.FromJson` → null チェック → `Validate()`」の3行が、`File.Exists` 分岐時と `CreateNew` 失敗時の再読込時の2箇所にほぼ同一のまま重複している。動作には影響しないが、ローカル関数（例: `ReadAndValidate(path)`）へ抽出すると保守性が上がる。修正必須ではない。

2. **[低〜中・未確認] `JsonUtility.FromJson` が空文字列/壊れたJSONに対して例外ではなくデフォルト値オブジェクトを返す可能性**
   Unityの `JsonUtility.FromJson` は入力が空文字列や一部破損した場合でも `null` を返さず、フィールド未設定のオブジェクトを返すことがある（バージョン依存）。この場合 `loaded == null` のチェックをすり抜け、`Validate()` も既定値と同じ値のため成功し、「読めない・不正なら既定値を使い、警告を1回出す」という要件の“警告”が出ないまま静かに通ってしまう可能性がある。実害は「既定値相当になるだけ」で小さいが、要求仕様の“警告を出す”という観点では取りこぼしうる。実機/Unity上での挙動確認が必要（本レビューでは検証不可）。

3. **CreateNewによる上書き防止ロジック自体は要件を満たしている**
   `File.Exists` → `FileMode.CreateNew` という流れは、OSレベルの排他的生成に委ねているため、Exists確認後の競合（TOCTOU）にも耐性がある設計になっている。`CreateNew` が `IOException` で失敗した場合に `File.Exists(path)` を再確認してから読み込み直し、`Validate()` する分岐、さらにそれも失敗すれば外側の `catch` で警告＋既定値にフォールバックする流れは、タスクの追修正要求（「既に存在して失敗した場合は読み込み直して検証、それも失敗したら既定値と警告」）通りに実装されている。既存ファイルへの上書き経路は存在しないため、「上書きしない」という保証も満たされている。

4. **例外フィルタの網羅性は妥当**
   外側の `catch` が `ArgumentException / IOException / NotSupportedException / SecurityException / UnauthorizedAccessException` を捕捉しており、`Directory.CreateDirectory` や `File.ReadAllText`、`JsonUtility.FromJson` から想定される例外系はおおむねカバーされている。ここに重大な欠陥は見当たらない。

## 未確認事項（本スナップショットに含まれず判断不可）

- `Tests/LoopModel.Tests/LoopModel.Tests.csproj` の変更内容（`PlayAreaSettingsFile.cs` を誤って含めていないか）
- `Assets/LoopRoom/Scripts/RoomAnchor.cs`、`Assets/LoopRoom/Editor/RoomAnchorChecks.cs`、`Tests/LoopModel.Tests/Program.cs` の変更内容（新オーバーロードの追加、既定値版の委譲、競合ケースのテスト追加）
- 実際のテスト実行結果（batchmode 0/0 等）は未実施・未確認
- Unity実機/エディタ上での `JsonUtility.FromJson` の空/不正入力時の挙動

## 判定

上記の通り、渡された `PlayAreaSettingsFile.cs` 単体としては追修正の要求（`FileMode.CreateNew` による上書き防止、競合時の再読込と検証、失敗時の既定値と警告）を満たしており、重大な欠陥は見当たらない。指摘した2点は軽微または追加確認事項であり、修正必須のブロッカーではない。

**判定: approve**

ただし、上記「未確認事項」（csproj、RoomAnchor.cs、テストコード、実際のテスト実行結果）はこのレビュー範囲外のため、タスク担当が別途確認することを前提とする。