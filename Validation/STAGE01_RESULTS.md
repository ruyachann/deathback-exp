# 段階1の検証結果

2026-09-17。移行先C:/deathback/deathback-exp。
- 管理基盤: Python3.12 unittest 26項目PASS（Windows）。
- モデル: PowerShell Add-Typeで実ソース+runnerをコンパイル、12項目PASS、終了0。最初はinternal Programの型解決で呼出しが失敗し、Assembly.GetTypeで呼出しを修正。製品ソース変更なし。
- SDKによるdotnet run: ローカルSDKなし。GitHub Ubuntu CIでは実ソースをSDKビルドし12チェックPASS。対象f2db664cf53db7e29a4eb1c1200a605cc0223053、push run35194249768 / PR run35194253419は両方success。
- Unity: 通常Hubで新正本の統合コンパイル成功。SolがPrepare/Configure/Validateを実行して11チェックPASS。一回の短いPlay/Stopで実行時オブジェクト生成とStop後の消去を確認、エラー0・警告4。詳細TASK004_EDITOR.md。Quest3/セッション開始/死亡復帰/180秒/観客秘匿は未実施。
- 依存: check_dependencies.pyはmanifest/lock一致を全7件で確認。公開レジストリ一覧にURP17.3.0/Test Framework1.6.0/uGUI2.0.0が出ずmanual review扱い。パッケージ欠落・非互換と断定しない。latestタグはUnity互換性を保証しない。依存変更なし。
- CIの公式Actions SHA: upstream GitHub tag APIのobject.type=commitを確認して固定。初回run35193929431は手書きcsprojがgitignore対象だったため失敗。例外を追加し、リモートtreeへの掲載を確認して修正した。過去の失敗を合格扱いしない。
- .gitの書込拒否でローカルcommit未実施。認証済みGitHub APIで作業ブランチを公開しdraft PR #5を作成。通常PowerShellのSync-PublishedBranch.ps1でローカル履歴を同期する手順を用意した（未実行）。CLI生ログ/キャッシュ/認証情報は除外。
- Unity起動試行: CLI起動はIPC接続失敗。通常Hubでの最初の起動もpipelineのEPERM renameで失敗したが、ユーザーRetry後に正常解決・統合コンパイル・CLI接続へ到達。詳細UPM_RECOVERY.md。原因確定や再発防止済みとは扱わない。
