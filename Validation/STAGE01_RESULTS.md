# 段階1の検証結果

2026-09-17。移行先C:/deathback/deathback-exp。
- 管理基盤: Python3.12 unittest 26項目PASS（Windows）。
- モデル: PowerShell Add-Typeで実ソース+runnerをコンパイル、12項目PASS、終了0。最初はinternal Programの型解決で呼出しが失敗し、Assembly.GetTypeで呼出しを修正。製品ソース変更なし。
- SDKによるdotnet run: ローカルSDKなしのため未実施。GitHub CIで確認予定。
- Unity/Play/Quest3: 新正本では未実施。旧正本のアセンブリ更新を代用しない。
- 依存: check_dependencies.pyはmanifest/lock一致を全7件で確認。公開レジストリ一覧にURP17.3.0/Test Framework1.6.0/uGUI2.0.0が出ずmanual review扱い。パッケージ欠落・非互換と断定しない。latestタグはUnity互換性を保証しない。依存変更なし。
- CIの公式Actions SHA: upstream GitHub tag APIのobject.type=commitを確認して固定。
- .gitの書込拒否でローカルcommit未実施。公開前dry-runは133ファイル/約473KB（その後の文書/レビュー追加で変動）。CLI生ログ/キャッシュ/認証情報は除外。