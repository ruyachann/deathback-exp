# 009 — peer_review_mcp.py の Codex 呼出し不具合の修正

状態: **受入済み（2026-09-24）**。結果は `reviews/task009-exchange-20260924.md`。計画確定（2026-09-24）。計画は Claude Opus 5.5（`claude-opus-5-5`）。実装は GPT-5.6 Sol（`codex exec` の別セッション）。レビューは実装とは別セッションの Sol と Claude Sonnet 5。

## 背景

2026-09-23 に判明（STATE.md 計画 3b）。Claude Code 側の MCP ツール `review_with_codex` は、次の2点のため必ず失敗する。task005/007/008 のレビューでは、呼出し側でこの2点だけを補正して回避した。

1. `shutil.which("codex")` が見つからない。このマシンでは codex.exe が PATH に無く、実体は `%LOCALAPPDATA%\OpenAI\Codex\bin\<ハッシュ>\codex.exe`（Codex デスクトップの更新でハッシュ部分が変わりうる）。
2. `--ask-for-approval never` を `exec` の後ろに置いている。codex-cli 0.155 ではトップレベル引数なので、`exec` の後ろだと exit 2（unexpected argument）。

## 修正方針

- Codex 実行ファイルの解決順:
  1. 環境変数 `CODEX_CLI_PATH`（存在するファイルなら使う）
  2. `shutil.which("codex")`
  3. Windows のみ: `%LOCALAPPDATA%\OpenAI\Codex\bin\*\codex.exe` のうち更新日時が最新のもの
  いずれも無ければ従来どおり `RuntimeError(f"{reviewer} CLI is unavailable")`。Claude 側（`shutil.which("claude")`）は変えない。
- Codex のコマンドを `[codex, "--ask-for-approval", "never", "exec", "--model", model, "--sandbox", "read-only", "--ephemeral", "-c", "mcp_servers.peer_claude.enabled=false", "-C", ROOT, "-"]` の順にする。
- `status.json` に、使った Codex 実行ファイルのパスを `reviewer_executable` として記録する（Claude 側も同じキーで記録してよい）。
- レビューのプロンプト、スナップショット、SHA256 記録、PAUSE 判定、出力先、ツール定義は変えない。

## 許可ファイル

- `Tools/peer_review_mcp.py` のみ。

## 受入条件

1. 修正後の MCP サーバーを stdio で起動し、`initialize` → `tools/list` → `tools/call review_with_codex`（小さなタスクとファイル）が、呼出し側の補正なしで成功し、`Collaboration/runs/` に `review.md` と `status.json` ができる。
2. `CODEX_CLI_PATH` に存在しないパスを入れても、2〜3番目の方法で見つかる。
3. `--reviewer claude` 側の動作が変わらない（`tools/list` が `review_with_claude` を返す）。
4. 実装とは別セッションの Sol と Sonnet が同じ SHA256 で独立レビューし、交換後に両方 approve。

## リスク

- 受入1は実際に Codex を1回呼ぶ（Codex の利用枠を少し使う）。
- 起動中の Claude Code セッションは MCP サーバーを起動時に読むため、この修正を使うには Claude Code の再起動が必要。
