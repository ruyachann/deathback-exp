# 再開位置

正本C:/deathback/deathback-exp。AGENTS.md、STATE.md、Docs/Plans/STAGE_PLAN.md、最新reviewsとValidationを読む。ローカルPAUSEあり。ユーザーの明示再開後に利用枠を新たに取得し、10%未満なら新規呼出し停止。十分な枠を確認してからPAUSEを解除する。ユーザー変更と実際のremote HEADを確認する。

1. ローカルGit履歴は未同期。通常PowerShellで `.\Sync-PublishedBranch.ps1` を実行して公開ブランチに同期する。スクリプトは既存履歴・staged変更がある場合拒否する。拒否なら差分を確認し、強制リセットしない。
2. Sonnet最終文書レビューと交換を完了する。修正済みF1/F2はsol-stage01-doc-exchange.json、以前のSonnet request_changesはsonnet-stage01-doc-corrections.jsonに保持。タイムアウトをapprove扱いしない。task004設定の最新Sonnet判定も確認し同じSHAで交換。
3. URP観客カメラのstereoTargetEye警告を独立タスクで扱う。現行render pipelineのXR描画設定を公式API/実ソースで確認してから修正し、同じSHAをSol/Sonnetがレビュー。実機結果を代用しない。
4. Quest3が利用可能になったらLink接続、Floor/頭/両手/操作到達、3回死亡、3回Play、外部計測180秒、F2無効の観客表示をDocs/UNITY_ACCEPTANCE.mdで確認。現在ユーザーは接続不可。
5. 依存候補はIssue3でUnity Client APIの実際の互換性を確認。可変RulesはIssue4で別仕様/検証。段階2受入前に5シナリオやマルチプレイへ範囲を広げない。

CLIは必ず正本cwdで実行。旧cwdのstatus未検出で二重Editor起動しない。Editorはtask004終了時停止状態。Library/PackageCacheとCLI生ログ/認証情報をpushしない。

Start-Automation run-001は旧task001(1.12→1.17)限定ハーネスであり、既に1.17の現在版へ再実行して一般的な実装再開とはしない。新タスクに目的/許可範囲/受入/SHAを設定してSolとSonnetへ限定packetを渡す。

停止前の最新settings判定: Sol approve / Sonnet request_changes。次はsol-task004-settings.jsonのコード/meta照合をSonnetへ渡し、必要な参照ソースを補足して交換する。ソース修正案はまだ不要な可能性があるが、本人の最終判定を取得するまで相互承認としない。
