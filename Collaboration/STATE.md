# 現在の状態

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

## 停止

ユーザーの「制限直前で停止」に従い13%で新規AI呼出し停止。ローカルPAUSE.jsonあり。未完了はSonnet最終文書再確認（最後もtimeout）、settings交換、段階1最終受入、実機/警告確認。停止は受入完了を意味しない。
