# Git移行と開発体制変更の引継ぎ

2026-09-17。ユーザーが新しい開発体制とC:\deathbackへの移行・ブランチ作成・pushを指示。確認時点でCodexの5時間枠100%使用、ordinaryUsageAllowed=falseのため実行を停止。

## 最新のユーザー指示

- 既存のAstra作成企画書を判断の根拠とし、Solが実行計画・機能アーキテクチャ・複雑なロジックのレビュー・品質チェックを担当。
- 大きな企画を段階へ分け、段階ごとに実装・テスト・レビュー。
- 方針確定後、Sol（Unity操作など）とClaude Sonnet5がサブエージェントとして並列に実装・意味のある自動テスト・ドキュメントを担当。ファイルの担当を分ける。
- 新しいSol/Sonnetセッションで同一SHAを独立レビューし、双方へ報告を渡す。
- Issue対応のPRドラフト、依存更新のチェック、CI/CDの改善を段階的に行う。ユーザーはGit pushとドラフトPR作成を許可。mergeは今回の依頼に含まれない。
- 重要判断・行き詰まりはOpus5優先、次にAstra。実際に利用可能な正確なモデルIDを確認し、勝手に別モデルへ切り替えない。
- 各AIの5時間枠を管理し、残り10%未満で報告・引継ぎ書保存・停止。Codex枠はモデル別に分けられずアカウント共通として扱う。Claude残量はまだ取得しておらず、推定やAPI費用から残量を捏造しない。取得方法を確認し、取得不可の場合は不明と記録しユーザーの利用表示を確認する。

## 調査結果と未実施

- 指定先C:\deathbackの読書きとネットワークをセッション範囲で許可取得済み。将来のターンで必要なら許可を再確認。
- C:\deathbackを作業ディレクトリとしてgit status/remote/branch実行はいずれも「not a git repository」。子ディレクトリや実際のclone先は未確認。新規git initやremoteの推測は行わない。
- コピー・ブランチ作成・commit・push・Issue/PR作成は未実施。
- 旧プロジェクトのPAUSE.jsonは維持。新しいモデル呼出しなし。
- 旧正本: C:\Users\PC_User\Documents\Codex\2026-09-16\hmd-unity-vr-x20-x20-x20\outputs\LoopRoomUnity。
- 企画書: outputs/VR_demo_plan_v0.1.md、VR_demo_first_milestone.md、VR_demo_implementation_status.md。
- 002/003の最新進捗はRESUME.md。003Sonnet側の交換は承認、Sol側交換未完。002Sonnetレビューは180秒タイムアウト。UnityのRefreshはエラーなし、Pipeline起動記録あり。Play/Quest3未確認。

## 再開時の順序

1. 明示的再開指示とCodex/Claudeの利用枠を確認。停止後のユーザー編集とSHAを確認。
2. C:\deathbackのディレクトリ構成と.gitを確認。必要ならユーザーへ正しいclone先を問い合わせる。remote/branch/worktree/AGENTS/CLAUDE/CIを読み、既存変更を保護。
3. 新規作業ブランチ名を決め、既存企画書とUnityのAssets/Packages/ProjectSettings、管理ツール・検証報告を移行。Library/Temp/Logs/UserSettings、認証ログ、CLI生ストリームをpush対象にしない。gitignoreと差分・機密情報を確認。
4. Solが段階計画と境界・受入基準を記録。設定を最新の指示へ揃え、重要相談先にOpus優先を追加。正確なOpusモデルIDと残量取得手段を確認する。
5. 初段階は既存002/003のレビュー完了とUnity/Quest3確認。独立に進められるUnity検証準備とCI/ドキュメントをSol/Sonnetへ分ける。未完了の実機確認をCI成功で代替しない。
6. 実際のIssueがあればIssueに結びつけ、なければ初段階のIssueを作成する。必要な検証後commit/pushしドラフトPRを作成。CIはリポジトリ既存構成とUnityライセンス/runnerの実態を確認して最小構成を設計する。資格情報が必要な段階だけユーザーへ依頼。
7. 各段階の結果・未検証・対象SHA・利用枠を保存し、次の段階へ進む。

## クローン先訂正とClaude連携注意事項の確認

ユーザー指定の正しいクローン先は C:\deathback\deathback-exp。
git status: No commits yet on main...origin/main [gone]。remote origin: https://github.com/ruyachann/deathback-exp.git。ローカル履歴はまだなく、remoteの実際の状態は未照会。対象clone内には今回のrg検索で文書が見つからなかった。
旧正本のCollaboration/SONNET_401_ROOT_CAUSE.md、CLAUDE_CONNECTION_CHECK.md、AUTOMATION_GUIDE.mdを読んだ。
- 連携は既存Desktop会話のAPI化ではなく、新しいClaude CLIセッション。
- 401 OAuth期限切れとネットワークのECONNREFUSEDを区別する。
- Claude側報告は、.claude.jsonの書込み/.lock取得と.claude/.oauth_refresh.lockがEPERMとなり自動更新できないことを原因としている。このターンでは生ログを再検証していない。設定書込み失敗から認証情報の保存先まで断定せず、再開時に公式仕様と実ログで確認。
- 401は未実施として扱い、別モデルへ代替しない。認証の恒久対策は許可された対象パスへの限定アクセス又はユーザーの通常CLI運用を検討。APIキー方式への無断変更はしない。
- プロキシ/サンドボックスを迂回しない。認証情報をログ・commit・pushへ含めない。
- 回答modelと最終本文を照合し、modelUsage集計のみでモデルを判定しない。
この確認は読み取りと引継ぎ訂正のみ。停止状態は維持、ブランチ作成・コピー・push未実施。