# 再開位置

正本C:/deathback/deathback-exp。AGENTS.md、STATE.md、Docs/Plans/STAGE_PLAN.md、最新reviewsとValidationを読む。ユーザー変更と実際のremote HEADを確認する。

2026-09-23: PAUSE は解除済み（ユーザー明示指示、Codex枠 残92%）。最新の優先順位は STATE.md「現在の計画」表を正とする。以下の項目0〜5は過去の再開位置として残す。利用枠が10%未満になったら再び PAUSE.json を作成して停止する。

0. 2026-09-19: task005（Fable実装、未レビュー）が作業ツリーにある。次はEditor PlayでLatch警告なし・3秒足音を確認し、同SHAでSol/Sonnetレビューへ渡すか判断する。
   [2026-09-22追記] Claude Sonnet 5がユーザー指示(Claude単独範囲、Codex/Sol/Astra未呼出し)でtask005の独立コードレビューとSHA256再照合・Unity batchmodeコンパイル再確認を実施(Collaboration/reviews/sonnet-task005-independent.json、verdict: approve_with_open_items、error CS 0/warning CS 0で前回と一致)。さらにcomputer-use経由でUnity Editorをインタラクティブ起動しdesktopモードでPlay実行(約14秒、t=3秒のLatchタイミングを含む)。Console件数は開始時のerror0/warning4/info1から変化なく、`Can not play a disabled audio source`警告は発生しなかった(受入条件2のうち警告非発生は確認、実際の可聴音は未確認)。Play停止・Editor終了済み(停止状態)。Quest3実機・Sol側レビューは未実施のまま。Solレビューが揃うまで相互レビュー完了とは扱わない。PAUSE.jsonはCodex/Sol/Astra呼出しについて維持(claude_scoped_resumeフィールド参照)。
   [2026-09-22追記2] ローカルGit同期を実施。Sync-PublishedBranch.ps1はWindows PowerShell 5.1で`$ErrorActionPreference='Stop'`と`git rev-parse --verify HEAD 2>$null`(unborn HEAD時のfatalがredirectで抑制されずNativeCommandErrorとして終了する)の組み合わせで必ず失敗するバグを確認(9行目)。スクリプト自体は変更せず、同じ手順をgitコマンドで手動実行し、ローカルbranch `codex/stage-01-looproom`をorigin追従で作成・reset --mixed・upstream設定まで完了(push/commitはしていない、作業ファイルは無変更)。`git status`はtask005変更(LoopDemo.cs, RoomVisuals.cs)とCollaboration配下の新規ファイルのみを表示。Sync-PublishedBranch.ps1のバグはCodex/Solへの提案事項として報告(別タスクでの修正が必要、本セッションの許可範囲外のため未修正)。
1. ローカルGit履歴は未同期。通常PowerShellで `.\Sync-PublishedBranch.ps1` を実行して公開ブランチに同期する。スクリプトは既存履歴・staged変更がある場合拒否する。拒否なら差分を確認し、強制リセットしない。
2. Sonnet最終文書レビューと交換を完了する。修正済みF1/F2はsol-stage01-doc-exchange.json、以前のSonnet request_changesはsonnet-stage01-doc-corrections.jsonに保持。タイムアウトをapprove扱いしない。task004設定の最新Sonnet判定も確認し同じSHAで交換。
3. URP観客カメラのstereoTargetEye警告を独立タスクで扱う。現行render pipelineのXR描画設定を公式API/実ソースで確認してから修正し、同じSHAをSol/Sonnetがレビュー。実機結果を代用しない。
4. Quest3が利用可能になったらLink接続、Floor/頭/両手/操作到達、3回死亡、3回Play、外部計測180秒、F2無効の観客表示をDocs/UNITY_ACCEPTANCE.mdで確認。現在ユーザーは接続不可。
5. 依存候補はIssue3でUnity Client APIの実際の互換性を確認。可変RulesはIssue4で別仕様/検証。段階2受入前に5シナリオやマルチプレイへ範囲を広げない。

CLIは必ず正本cwdで実行。旧cwdのstatus未検出で二重Editor起動しない。Editorはtask004終了時停止状態。Library/PackageCacheとCLI生ログ/認証情報をpushしない。

Start-Automation run-001は旧task001(1.12→1.17)限定ハーネスであり、既に1.17の現在版へ再実行して一般的な実装再開とはしない。新タスクに目的/許可範囲/受入/SHAを設定してSolとSonnetへ限定packetを渡す。

停止前の最新settings判定: Sol approve / Sonnet request_changes。次はsol-task004-settings.jsonのコード/meta照合をSonnetへ渡し、必要な参照ソースを補足して交換する。ソース修正案はまだ不要な可能性があるが、本人の最終判定を取得するまで相互承認としない。
