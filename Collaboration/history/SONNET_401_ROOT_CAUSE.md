# Sonnetの HTTP 401 OAuth期限切れ: 原因と対処 (Claude Code Desktop調査)

2026-09-17。ユーザーの依頼により、サンドボックス外の通常のClaude Code Desktop会話（このファイルを書いている本セッション）が調査しました。ゲームコード・設定・認証情報は一切変更していません。Astra/Solはこのファイルを事実として読み、対処の実施可否はユーザー判断に委ねてください。

## 結論

- 401はAnthropic API側が正規に返しているエラーで、サービス障害ではありません。メッセージは `API Error: 401 OAuth access token has expired. Re-authenticate to continue.`
- 高頻度に再発する理由: このリポジトリの自動化パイプラインが起動するSonnet用 `claude.exe` は毎回使い捨てプロセスで、ディスク上の `C:\Users\PC_User\.claude.json` を読むだけです。この実行環境（Codexシェル経由のサンドボックス）はそのファイルへの書き戻し・ロック取得を許可していないため、トークンが自然に期限切れになるたびに自己修復できず、次のSonnet呼び出しが必ず401になります。

## 証拠（ファイル・行）

1. 401の実例
   - `Collaboration/automation-runs/20260917-095905-review003/01-sonnet-review-003/stdout.log` (2〜5行目)
   - `Collaboration/automation-runs/20260917-004340-f4e06bda/02-sonnet-check/stdout.log`
2. 書き戻し不能の証拠（同時刻の別プロセスによるデバッグログ） `work/claude-link-check.log`
   - 15行目: `EPERM: operation not permitted, mkdir 'C:\Users\PC_User\.claude.json.lock'`
   - 19行目・23行目: `.claude.json` へのアトミック書込み・非アトミック書込みが両方EPERMで失敗
   - 34行目・103行目: `EPERM: operation not permitted, mkdir 'C:\Users\PC_User\.claude\.oauth_refresh.lock'`（トークン更新用の調整ロックすら取得できない）
   - 同ログでは `git.exe` / `rg` / `cmd.exe` の起動、`C:\Users\PC_User\.claude\sessions` の `chmod` も同様にEPERM。このCodex自動化環境がユーザープロファイル配下への書込み・任意プロセス起動を広く禁止していることと整合します。
3. 時系列的な裏付け
   - 09:59 (review003) で401失敗 → 10:08〜10:10 (review003再実行・review002・exchange003) はいずれも正常成功し、usage/costも記録されました。コード側は無変更のため、この間にサンドボックス外で人がインタラクティブに再ログインし `.claude.json` が更新されたとしか説明できません。`Collaboration/RESUME.md` 7行目の「Claude再ログイン成功」とも一致します。

## 対処

このセッションでは何も変更していません。以下はユーザー判断・実施が必要です。

1. 短期（既にAUTOMATION_GUIDE.md 85行目に記載済みの運用）: 401が出るたびに、サンドボックス外の通常のClaude Codeで `claude auth login --claudeai` を実行し `.claude.json` を更新する。都度手動対応が必要で、再発を止めるものではありません。
2. 恒久対応の候補（いずれか、ユーザー実施）:
   - このCodex自動化サンドボックスに `C:\Users\PC_User\.claude\` と `C:\Users\PC_User\.claude.json` への書込み・ロック取得を許可し、CLI組み込みのバックグラウンド・トークン更新を機能させる。
   - 短命なOAuthアクセストークンへ依存する対話ログインの代わりに、この自動化専用の長期間有効な認証情報（例: `ANTHROPIC_API_KEY` によるAPIキー運用）に切り替える。書込み禁止のサンドボックスとは根本的に相性が悪い設計のため。

## 明記事項（AGENTS.md順守の確認）

- ネットワーク制限・サンドボックスの回避、認証情報や個人設定の書換えはこの調査中に一切行っていません。
- 401発生時にSonnetを別モデルへ無断代替することはしていません。今後も401は「未実施」として記録し、代替実装モデルにしないでください。
