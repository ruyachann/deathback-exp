# 010 — LoopRules の複製と区間の最小値（task006 B-6 + Issue4 の一部）

状態: **受入済み（2026-09-24）**。結果は `reviews/tasks010-011-013-exchange-20260924.md`。計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 GPT-5.6 Sol（`codex exec` 別セッション）。レビュー 実装とは別セッションの Sol と Claude Sonnet 5。

## 目的

1. **B-6**: `LoopDemo.timings`（Inspector で編集される）と `LoopModel.Rules` が同じオブジェクトを共有しているため、Play 中に Inspector で値を変えると、検証済みのルールが検証なしで書き換わる。モデルは自分用の複製を持つ。
2. **Issue4 の一部**: `Validate()` が極端に小さい区間（例 1e-200 秒）を受け入れ、`Advance()` が終わらなくなる（task007 の照合で判明、変更前から存在）。区間ごとに最小値を設ける。

## 設計

- `LoopRules` に `public LoopRules Clone()` を追加する（全8フィールドを複製。`MemberwiseClone` でよい）。
- `LoopModel` のコンストラクタで `Rules = (rules ?? new LoopRules()).Clone(); Rules.Validate();` とする。呼び出し元の `LoopRules` をモデルが持たないことをテストで確認する。
- `Validate()` に最小区間 `public const double MinInterval = 0.05;`（秒）を加え、次をすべて満たさなければ ArgumentException:
  - `firstShot >= MinInterval`
  - `searchShot - firstShot >= MinInterval`
  - `exitCloses - exitOpens >= MinInterval`
  - `blackout >= MinInterval`
  - `endingLength >= MinInterval`
  - `playLimit >= MinInterval`
  既存の検査（NaN・無限大、順序、`enforcePlayLimit` 時の180秒）は変えない。既定値（6 / 12 / 6.5 / 12 / 0.16 / 172 / 8）はすべて満たす。
- これにより `Advance(MaxStep)` の反復回数は 3600 / 0.05 程度で有界になる（1周に最低 firstShot + blackout ≥ 0.1 秒）。
- LoopDemo は変更しない（`new LoopModel(timings)` のままで複製される）。

### 追修正（2026-09-24、独立レビュー指摘への対応。計画担当 Opus 5.5 の決定）

Sol・Sonnet とも request_changes（`sol-task010-independent-mcp.md`、`sonnet-task010-independent-mcp.md`）。
1. 採用（Sol 中）: `searchShot - firstShot` などの差を `< MinInterval` と比べると、`6.05 - 6.0 = 0.0499999…` でちょうど0.05秒の区間が拒否される。差の比較に許容誤差 `1e-9` を持たせる（`diff < MinInterval - 1e-9` で拒否）。単独の値（firstShot、blackout、endingLength、playLimit）も同じ形にそろえる。境界テストに `6.0/6.05`（searchShot）と `6.5/6.55`（exitCloses）を追加し、どちらも通ることを確認する。
2. 採用（Sol・Sonnet）: 検索区間の「半分は拒否」テストが既存の順序検査 `exitCloses > searchShot` で先に例外になり、新しい検査を通っていない。出口区間を `[firstShot, searchShot]` 内に収める値にして、最小区間の検査だけで拒否されるケースにする。
3. 採用（Sol 低）: 最小許容ルール（全区間 MinInterval）で `Advance(LoopModel.MaxStep)` が戻り、`TotalTime == MaxStep` になる回帰テストを追加する。
4. 見送り（Sonnet 低）: 生成後に `Model.Rules` のフィールドを外部から書き換えられる。LoopDemo は書き換えていない。不変化は Issue4 の残りとして後で扱う（STATE に記録）。

## 許可ファイル

- `Assets/LoopRoom/Scripts/LoopModel.cs`
- `Assets/LoopRoom/Editor/LoopModelChecks.cs`
- `Tests/LoopModel.Tests/Program.cs`

## テスト（LoopModelChecks に追加）

1. モデル作成後に元の `LoopRules` の値を変えても `Model.Rules` は変わらない（例: 元の firstShot を 1 にしても Model.Rules.firstShot は 6）。
2. 各最小値の境界: ちょうど MinInterval は通る、MinInterval の半分は拒否（6条件それぞれ）。
3. task007 の Sol 指摘の再現ケース（firstShot=1e-200 など）は Validate で拒否される。
4. task007 レビューの軽微指摘の補強: 既定ルールの1000秒チェックで `TotalTime == 1000` を確認する。
Program.cs の件数を合わせる。

## 受入条件

1. モデルテスト全件 PASS（Unity 付属 Roslyn）。GitHub CI（dotnet）も success。
2. Unity batchmode コンパイル エラー0・警告0。
3. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
4. 証拠: テスト出力とコンパイル結果の画像を `Collaboration/evidence/<日付>-task010/` に保存し、ユーザーに送る。

## リスク

- Inspector でシーンの timings に 0.05 秒未満の値を入れていると Play 開始時に例外になる。現在のシーン値はすべて既定値で問題ない。
