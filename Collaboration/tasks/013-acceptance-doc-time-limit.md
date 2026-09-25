# 013 — UNITY_ACCEPTANCE.md を180秒上限の撤廃と Quest 3/3S に合わせる（計画 5b）

状態: **受入済み（2026-09-24）**。結果は `reviews/tasks010-011-013-exchange-20260924.md`。計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 Claude Sonnet 5。レビュー Sol と別セッションの Sonnet。

## 目的

2026-09-24 の段階1文書最終確認で Sonnet が指摘: `Docs/UNITY_ACCEPTANCE.md` に180秒の前提（10行目）と第8章「180 秒以内の確認」（107-116行目、超過を不合格）が残り、2026-09-23 のユーザー決定（上限を当面外す）と食い違う。あわせて実機が Quest 3S になる可能性を反映する。

## 設計

- 10行目の前提: 180秒目標を削除し、「時間上限は当面なし（`LoopRules.enforcePlayLimit=false`）。再導入はユーザー判断」と書く。
- 第8章: 「セッション時間の記録」に改める。合否基準は「脱出または運営の中断で終わること、TimedOut が出ないこと、セッションログの elapsed を記録すること」。上限を再導入したときの確認手順（`enforcePlayLimit=true` で172秒 TimedOut＋8秒）は参考として残す。
- 対象機種: Quest 3 / Quest 3S（Link による PCVR、Touch Plus）。3S で追加する確認: 視野角・解像度・レンズの違いによる机・時計・取っ手の視認性と文字サイズ。desktop で案内パネルの文字が Game ビューからはみ出す件（evidence/20260924-editor-play）を、HMD 内での見え方として確認項目に入れる。
- 段階1当時の記述を変えた箇所は、本文中に変更日を書く。

### 追修正（2026-09-24、独立レビュー指摘への対応。計画担当 Opus 5.5 の決定）

Sol・Sonnet とも request_changes（`sol-task013-independent-mcp.md`、`sonnet-task013-independent-mcp.md`）。すべて採用する。
1. 第4章手順2の「脱出またはタイムアウトまたは中断」から、タイムアウトを外す（既定では起きない）。第5章でタイムアウトを通常の終了として書いている箇所も同様に外すか、「第8章の参考手順で `enforcePlayLimit=true` にした場合のみ」と限定する。
2. 第8章の手順4・期待される証拠・不合格基準に、セッションログの `timings.enforcePlayLimit` が `false` であることを加える（シーンに `playLimit=172` が保存されていても使われない点も一文で書く）。上限が誤って有効になった回帰を検出するため。
3. 第9章のログ内容の説明に `timings.enforcePlayLimit` を明記する。
4. 見送り: 証拠画像と変更前後の照合は、照合者が `evidence/20260924-task013/` と `git diff` で行う（レビュアーのスナップショットに含まれないのは手順上の制約）。

## 許可ファイル

- `Docs/UNITY_ACCEPTANCE.md`

## 受入条件

1. 180秒を合否基準にする記述が残っていない（参考手順を除く）。
2. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
3. 証拠: 変更前後の該当箇所の差分画像。
