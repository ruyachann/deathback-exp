# Codex と Claude の相互レビュー

## 担当

企画・計画は GPT-6 Astra または利用可能なことを確認した Claude Opus を優先する。実装は GPT-5.6 Sol または Claude Sonnet 5 を優先する。両製品とも企画・計画・実装・レビューのファイルを編集できる。担当モデルと編集範囲はタスクごとに記録する。同じファイルは同時に編集しない。

`.codex/config.toml` は通常作業の既定値を Sol とし、`.claude/settings.json` は Sonnet とする。Astra で計画する場合は `Start-Astra.ps1`、Opus で計画する場合は正確なモデル ID を確認して `Start-Opus.ps1` を使う。Opus の正確なモデル ID と利用権が未確認なら呼び出さない。モデル名を指定しただけでは実行を証明しない。

## MCP 接続

このリポジトリを信頼した Codex は `.codex/config.toml` から `peer_claude` を読み、`review_with_claude` を使える。Claude Code はルートの `.mcp.json` から `peer_codex` を読み、`review_with_codex` を使える。両方とも `Tools/peer_review_mcp.py` を標準入出力の MCP サーバーとして起動する。設定はこのマシンの正本パス `C:/deathback/deathback-exp` を指す。移動した場合は両設定のスクリプトパスを更新する。

接続確認は正本のルートで `codex mcp list` と `claude mcp list` を実行する。2026-09-23、ユーザーの明示承認後、Codex の正本リポジトリだけを信頼済みに登録し、`peer_claude` が有効として表示された。Claude の `peer_codex` も初回承認待ちは解消したが、Codex の制限付きシェルからのヘルスチェックは `uv_spawn 'python'` の `EPERM` で失敗した。Claude の通常セッションからの接続と実際のモデル応答は未確認。通信・プロセス制限を迂回して接続を成立させない。

2026-09-24 追記: Claude Code の通常セッションで `claude mcp list` が `peer_codex` を Connected と表示。`review_with_codex` の Codex 呼出し不具合（PATH、`--ask-for-approval` の位置）は task009 で修正し、MCP サーバーを stdio で起動した `review_with_codex`（Sol）と `review_with_claude`（Sonnet、modelUsage で確認）の両方が実際のレビューを返すことを確認した。codex.exe が PATH に無い環境では `CODEX_CLI_PATH`、無ければ `%LOCALAPPDATA%\OpenAI\Codex\bin\*\codex.exe` の最新を使う。Codex 側の制限付きシェルから `peer_claude` を起動する経路（`EPERM`）は未確認のまま。接続確認とモデル応答確認は別であり、レビュー成功まで認証・ネットワーク・モデル利用権を証明しない。

## レビュー手順

1. タスク文書を確定し、対象ファイルの編集を止める。実装者の報告は独立レビューの代用にしない。
2. Codex 側では `review_with_claude`、Claude 側では `review_with_codex` に同じ `task` と `files` を渡す。`task` は `Collaboration/tasks/...` または計画文書、`files` は相対パスの配列。`focus` は任意。
3. サーバーは `AGENTS.md`、`CLAUDE.md`、タスク、対象ファイルの内容と SHA256 を固定して別 CLI セッションに渡す。レビュアーは読み取り専用で、他方のレビューを先に読まない。
4. `Collaboration/runs/<時刻>-mcp-<モデル>-review/` の `status.json` と `review.md` を確認する。両レビューの SHA256 が一致した場合に限り報告を交換し、指摘への対応と結論を `Collaboration/reviews/` に記録する。修正後は変更範囲を再レビューする。

MCP サーバーはレビューのみを依頼する。実装はタスクで担当を指定した Codex/Claude の通常セッションが共有リポジトリへ直接行う。`Collaboration/PAUSE.json` がある間は MCP のモデル呼出しを拒否する。再開条件は `Collaboration/RESUME.md` と `AGENTS.md` に従う。現時点では接続設定を用意しただけで、相互レビューの実行・受入を意味しない。
