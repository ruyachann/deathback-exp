# 現在の状態

2026-09-23 運用更新: 企画・計画・実装・レビューは Codex/Claude の双方が編集可能。計画は GPT-6 Astra または確認済み Claude Opus、実装は GPT-5.6 Sol または Claude Sonnet 5 を優先する。Codex は正本リポジトリの信頼登録後 `peer_claude` が有効。Claude の `peer_codex` は初回承認待ちが解消したが、この Codex シェルからのヘルスチェックは `uv_spawn 'python'` の EPERM で失敗。通常 Claude セッションでの接続と実レビューは未確認。詳細は AGENTS.md と PEER_REVIEW_MCP.md。以下の過去の分担記録は当時の証跡として残す。

2026-09-23 19:50 再開: ユーザーの明示指示（「Codexと作業を進める」）で PAUSE.json を解除し `history/PAUSE-cleared-20260923.json` へ保存。Codex 5時間枠は使用8%（残92%、10:47Z の rollout rate_limits、週枠使用1%）。Claude 枠は機械検証なし。Claude の `peer_codex` MCP は承認済み・Connected。Codex 側 `peer_claude` は未接続（deathback-exp の信頼設定待ち）のため、現状の相互レビューは Claude→Codex 方向の MCP のみ。

2026-09-24 運用ルール追加（ユーザー指定、AGENTS.md「計画の立て直しと証拠の共有」）: 計画の動的な立て直しは Opus 5.5 または GPT-6 Astra だけが行う。完成したもの（部分でも）には証拠画像・スクリーンショットを `Collaboration/evidence/` に保存し、ユーザーにも送る。コマンド出力の画像化は `Tools/Capture-Evidence.ps1`。既存の証拠: evidence/20260924-task007（モデル15チェック、Unity コンパイル）、20260924-task008（Sync 受入4件）、20260924-task009（MCP 受入）、20260924-item5-reviews。

## 現在の計画（2026-09-23 更新、Claude Opus 5.5 `claude-opus-5-5` が整理）

| 順 | 項目 | 担当 | 状態 / 次の一手 |
| --- | --- | --- | --- |
| 1 | task005 相互レビュー | レビュー: Sol(MCP) / Sonnet(済) | Sonnet `approve_with_open_items`（2026-09-22）。Sol独立レビュー（同SHA、MCP）は **request_changes**: 高 OnApplicationPause が VR 中も中断（LoopDemo.cs:199）、中 長フレームで Latch 欠落。照合で両方採用（reviews/task005-exchange-20260923.md）。2026-09-23 追修正 R-1/R-2 を Opus 5.5 が実装（LoopDemo.cs `9d6925b4…`、RoomVisuals.cs 不変）。batchmode コンパイル エラー0/警告0（local-logs/unity-batchmode-20260923.log）。再レビュー: Sonnet approve / Sol request_changes（周回をまたぐ長フレームで Latch 欠落）。交換で既知の制限として両者 **approve**（reviews/task005-rereview-exchange-20260923.md）。**相互レビュー完了。最終受入は Quest3 実機（条件3）と Latch 可聴確認待ち** |
| 2 | task006 正式化 | 計画: Astra or Opus | A-1 は2026-09-23ユーザー決定「180秒制限は当面なくし、まずしっかり遊べるように」→ AGENTS/STAGE_PLAN/task006 に反映。コード側は **task007**（`LoopRules.enforcePlayLimit` 既定 false、計画 Opus 5.5／実装 Sol）。レビュー指摘で `LoopModel.MaxStep=3600` を追加。モデル15チェック PASS（Unity 付属 Roslyn）、batchmode エラー0/警告0。**Sonnet/Sol とも交換後 approve、相互レビュー完了**（reviews/task007-exchange-20260923.md）。残課題: Issue4 で Rules の極小値を拒否、テスト補強、Editor Play で180秒超の継続確認（任意）。Quest3不要のB-6（Rules複製）・B-4（再準備）を先に正式タスク化。B-5はOpus相談条件（身体位置）に該当、B-7は実機待ち |
| 3 | Sync-PublishedBranch.ps1 修正 | 計画: Opus 5.5 / 実装: Sol | unborn HEAD で必ず失敗するバグ（9行目）。**task008 受入済み（2026-09-24）**: Sol が修正（`82b58822…`）、PS5.1 の一時リポジトリで受入テスト4件 OK、Sol/Sonnet とも approve（reviews/task008-exchange-20260924.md） |
| 3b | peer_review_mcp.py の Codex 呼出し修正 | **task009 受入済み（2026-09-24）**: Sol 実装（`20ecfd09…`）、MCP stdio で補正なしの受入3件 OK、Sol/Sonnet approve（reviews/task009-exchange-20260924.md）。以下は当初の記録 → 実装: Sol or Sonnet（Codexセッションが作成者） | 2026-09-23 判明: (1) `shutil.which("codex")` が PATH 上に codex.exe が無く失敗（実体は `%LOCALAPPDATA%\OpenAI\Codex\bin\<hash>\codex.exe`）、(2) `--ask-for-approval` は codex-cli 0.155 ではトップレベル引数で `exec` の後ろだと exit 2。今回の task005 Sol レビューは呼出し側でこの2点のみ補正して実行（スナップショット・プロンプトは同一） |
| 4 | 未コミット変更の保存 | ユーザー承認後 | MCP設定・task005・文書類が作業ツリーのみ。作業ブランチへ commit/push は承認待ち |
| 5 | task004 settings 交換 / 段階1 Sonnet最終文書確認 | Sonnet + Sol | **完了（2026-09-24）**: task004 設定は交換後 Sonnet も approve、段階1文書は Sonnet 最終 approve。**段階1の相互レビュー完了**（reviews/task004-settings-exchange-20260924.md、証拠 evidence/20260924-item5-reviews/） |
| 5c | Editor Play 証拠（2026-09-24） | Opus 5.5（computer-use） | **完了**: desktop Play で37周・228.96秒、Console は既知の警告4件から増えず（task005 受入2の警告部分）、180秒超でも継続（task007 受入4）、遮蔽→脱出で Escaped、TimedOut 0件（evidence/20260924-editor-play/）。可聴音と実機は未確認。気づき: desktop で案内パネルの文字が Game ビューからはみ出す（計画6の見え方確認で扱う） |
| 5b | UNITY_ACCEPTANCE.md の180秒節を現行方針に合わせる | 計画: Opus 5.5 | **task013 受入（2026-09-24）**: 実装 Sonnet、1回の追修正後 Sol/Sonnet approve |
| 7a | task010 Rules 複製＋区間最小値 | 実装 Sol | **受入（2026-09-24）**: 1回の追修正後 Sol/Sonnet approve、モデル19チェック PASS、batchmode 0/0。残り: 生成後の Rules 書き換え防止（Issue4） |
| 7b | task011 Touch Plus プロファイル | 実装 Sol | **受入（2026-09-24）**: Sol approve、Sonnet は証拠の交換後 approve。asset は Touch Plus の m_enabled 1行のみ。実機入力は計画6 |
| 7c | task012 XR 再準備キー R | 実装 Sonnet | **相互レビュー完了（Sol/Sonnet approve、2026-09-24）**、batchmode 0/0。**受入待ち: Editor Play で R の証拠**（見えない textinputhost.exe が前面扱いで computer-use が拒否。ユーザーの協力待ち）。reviews/task012-exchange-20260924.md |
| 9 | 後の課題（非ブロッキング） | 計画: Opus 5.5 | (a) 生成後の `Model.Rules` 書き換え防止（Issue4 残り、task010 Sonnet 低）、(b) DemoRig の `inputs` 一覧を FloorReady と StartXR が共用（task012 Sonnet 低）、(c) MCP クライアントで一度だけ出た UnicodeDecodeError（review は正常保存。原因未特定）、(d) desktop の案内パネル文字のはみ出し（計画6・8） |
| 7 | task006 の正式化（2026-09-24、Opus 5.5） | 計画: Opus 5.5 | DRAFT を分割: **task010** B-6 Rules 複製＋区間の最小値（実装 Sol）→ **task011** B-7 Touch Plus プロファイル（実装 Sol）→ **task012** B-4 XR 再準備キー R（実装 Sonnet）→ **task013** 文書（実装 Sonnet）。確認しやすい順。B-5（机の位置、身体位置の判断）は Quest 3/3S 実機で見え方を確かめてからユーザーと決める |
| 8 | **今後: 体験のクオリティ向上**（2026-09-24 ユーザー指定） | 計画: Opus 5.5 / Astra | 仮想空間内の見た目（素材・照明・部屋の作り込み、敵の造形とアニメーション）、音（足音・射撃・環境音）、演出（暗転・脱出の余韻）、案内表示の読みやすさ。STAGE_PLAN 段階3と合わせ、task010〜013 と実機受入のあとに計画を立てる |
| 6 | Quest3 実機受入 | ユーザー | 接続可能になり次第（task005受入条件3を含む）。**2026-09-24 ユーザー報告: 実機が Meta Quest 3S になる可能性がある。** 決まるまでは Quest 3 / 3S の両方を想定する。3S も Link による PCVR と Touch Plus コントローラーなので、task006 B-7（Touch Plus プロファイル有効化）の優先度を上げる。視野角・解像度・レンズの違いによる見え方（机・時計・取っ手の視認性、文字サイズ）は実機受入の確認項目に加える。機種が決まったら AGENTS.md の不変条件と STAGE_PLAN の対象機種を更新する |

2026-09-17。正本C:/deathback/deathback-exp。移行元119ファイルのコピー時SHAはmigration-manifest.jsonに保持（後の正当な編集は別レビュー）。

## Git保存

- remote: https://github.com/ruyachann/deathback-exp.git
- 公開作業ブランチ: codex/stage-01-looproom。draft PR: https://github.com/ruyachann/deathback-exp/pull/5。Issue1移行/CI、Issue2実機、Issue3依存、Issue4可変Rules。merge未実施。
- ローカル.gitはindex.lock/HEAD.lock書込拒否が続き、ローカルHEADはunborn main。認証済みGitHub APIで履歴を保存し、更新は既知HEADを確認してforce=false。ローカル履歴同期には通常PowerShellのSync-PublishedBranch.ps1を使用（未実行、作業ファイルを保持）。
- 初回CIは手書きcsprojが*.csproj除外で未公開となり失敗。対象だけnegation追加、bin/obj除外を維持。修正f2db664cf53db7e29a4eb1c1200a605cc0223053のpush35194249768/PR35194253419はsuccess。以後の文書・設定保存の最新CIは再確認する。

## 検証

- Windows管理26テストPASS。ローカルモデルAdd-Type12チェックPASS。GitHub Ubuntu SDKによるモデル12/管理26PASS。
- 通常HubのEPERM pipeline renameはユーザーRetry後に解消。原因は未確定。キャッシュ/ACL変更なし。UPM_RECOVERY.md参照。
- 新正本のUnity統合コンパイル、既存EditorでPrepare/Configure/Validate成功（11モデルチェック）。一回の短いPlay/Stop、生成/消去確認、エラー0・警告4。Validation/TASK004_EDITOR.md参照。
- Editorは停止状態で残す。Quest3は現在接続できないとユーザー確認。実機、セッション開始/Readyゲート、3死亡/3Play、180秒、観客秘匿、Windowsビルドは未実施。
- 警告: XROriginの初期Camera/offset未設定、SRP stereoTargetEye非対応、loaderなしdesktop fallback。特にURP観客カメラのXR描画を別タスクで確認する。
- 依存7件manifest/lock一致。公開registryに一部pinnedが載らないためIssue3の手動確認。自動更新なし。

## AIレビュー・受入

- 通常作業Sol、SonnetはCLI別セッション。短いnonce接続テストとprimary model/final-answer照合を確認済み。Desktop既存会話をAPIサーバーとしては使わない。
- Solは独立コア・追加文書/運用・交換・task004設定を承認。具体的証拠とSHAをreviews/に保存。
- Sonnetは初回コアをapprove、交換後request_changes。運用4ファイルをapprove。修正文書の限定レビューで2指摘を受け修正し、Solは再承認。最終Sonnet再確認はtimeoutで未完了。広い文書レビューもtimeout。段階1の最終相互承認は未完了で、受入済みと扱わない。
- task004の5設定ファイルはSol独立レビューapprove、Sonnetはrequest_changes（Windowed/D3D11/バックグラウンド/入力profile/layers/shaderの意図とGUID確認）。Solは実際のPrepareコード/metaで照合済み、その証拠をSonnetへ渡す交換は未完了。PCVRでAndroidビルド要件は範囲外。
- 原task002のFloor失敗ブロック、task003のRules不変化対象外、段階1の継承ソースレビュー範囲を保存。未解決判定を隠して別モデルの承認に置換しない。
- Opus5優先→Astra相談。Opus正確ID/利用権は未確認。Start-OpusのregexはID形の検査だけで利用可能性の証明ではない。Sonnet運用報告中の「正式IDと整合」は未確認として扱う。

## 利用枠

Codex5時間枠はアカウント共通、停止時13%残（ツール）。Claudeは開始時100%とユーザー報告、最新は「5時間枠は大丈夫」と回答（正確な現在%は未取得、機械検証なし）。usage.local.jsonは非公開ローカル記録。残量10%未満で新規呼出しを止めPAUSEと引き継ぎを書く。現在の数値を次回へ持ち越して推定しない。

## 2026-09-19 追記（Fable 実装）

ユーザー指示でClaude Fable 5.1がtask005（B-2/B-3/B-1、詳細はtasks/005とreviews/fable-task005-implementation.md）を実装。LoopDemo.cs 4554d309…、RoomVisuals.cs 2ba19368…。batchmodeコンパイルはエラー0・警告0。Editor Play/実機/Sol・Sonnetレビューは未実施。指定モデルclaude-sonnet-5ではないため相互レビューの片側と数えるかはユーザー判断。PAUSE.jsonは据え置き。ローカルGitは依然unborn main、Sync未実行。

## 停止

ユーザーの「制限直前で停止」に従い13%で新規AI呼出し停止。ローカルPAUSE.jsonあり。未完了はSonnet最終文書再確認（最後もtimeout）、settings交換、段階1最終受入、実機/警告確認。停止は受入完了を意味しない。
