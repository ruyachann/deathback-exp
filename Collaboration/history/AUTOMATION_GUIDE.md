# Sol/Sonnetの自動実行

制作は現在PAUSE.jsonにより停止中。再開指示と利用枠確認後に解除します。今回の設定変更で制作は再開していません。

```powershell
.\Start-Automation.cmd check --timeout 90
.\Start-Automation.cmd run-001
```

通常checkはSol/Sonnetのみ。run-001はSol設計→Sol実装候補→独立Sol/Sonnetレビュー→双方の報告交換→Sol受入→限定適用→Sol結果報告/次計画。各段階は新しいCLIセッションです。2案目はSonnetが修正候補を担当します。
run-001はmanifestのInput System1.12.0→1.17.0専用で、既に適用済みのため再実行不要。002/003を処理する汎用実装コマンドではありません。

重要判断・行き詰まりに関する理由を記録した場合に限り、`--escalate '判断が必要な理由'`でその実行の管理役をAstraへ変更できます。通常実行ではAstraを呼びません。レビュー不一致が最大2案で残る場合は未適用で停止し、相談資料を残します。自動でAstraへ切り替えません。

モデルはSol=gpt-5.6-sol、Sonnet=claude-sonnet-5、相談用Astra=gpt-6-astra。黙ったモデル切替なし。Claude回答のmodelと最終本文を照合し、Codexは明示CLI -mを記録します。CLIの指定は既存Desktop会話のモデルを変更しません。

正常適用はapplied_pending_unity_validation。適用後の報告失敗はapplied_pending_supervisor_report。旧applied_pending_astra_reportは過去ログの名称です。結果はautomation-runsへ保存。SHA照合、範囲制限、二重起動ロック、バックアップ、タイムアウトと子CLI終了、PAUSEによる新規呼出し防止を維持します。
通常は最大16呼出し（接続2、設計1、6段階×最大2案、報告1）。Astra相談ありは接続3で最大17。管理スクリプトにアカウント残量の自動取得は内蔵していません。
Unity内のコンパイル・Play・Quest3確認は別の検証として記録します。
