# task024 交換照合（2026-09-25、Opus 5.5）

- 独立: Sol approve／Sonnet request_changes（LoopDemo.cs の差分がレビュー対象に含まれていない。計画担当の渡し漏れ）。低2件（最初の検査が Advance まで見ていない＝直後の検査で担保、コード内のタスク番号コメント＝既存の慣習）は対応しない。
- 交換: LoopDemo.cs の差分、Rules の参照一覧、テスト出力、実行ログを `task024-loopdemo-diff.md` にまとめて両者に渡した。コードは変更なし。
- 交換後: Sol・Sonnet とも approve（`sol-task024-exchange-mcp.md`、`sonnet-task024-exchange-mcp.md`）。
- 検証: テスト 20+12 PASS、ビルド0/0、desktop 自動実行20秒で周回・暗転・置き直し4回、例外0（evidence/20260925-cleanup/）。観客カメラは VR 時だけ有効なので、desktop の撮影では観客表示は確認していない（挙動の変更なし）。
- 手順の教訓: 呼び出し側の変更が小さくても、変更したファイルはすべてレビュー対象に含める。
