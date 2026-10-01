# task027 交換照合（2026-09-25、Opus 5.5）

- 撮影で判明（計画担当）: `--auto-calibrate` がスプラッシュ中に終わりキャリブレーション画面が映らない → スプラッシュ終了後に計時、待ちを3秒に延長（証拠専用）。
- 独立: Sol request_changes（リングが扇形に隠れる、起動直後の長いフレームで計時が一気に進む。計画担当の撮影の気づきと一致）。Sonnet は時間切れ（300秒）。
- 追修正2: リングを .024 に上げ renderQueue +1、計時の dt を1フレーム最大0.1秒。
- 再レビュー: Sol approve（`sol-task027-rereview-mcp.md`）、Sonnet の独立レビュー approve（`sonnet-task027-independent-mcp.md`、差分資料で渡して時間切れを回避）。
- 交換: 互いのレビューを読み、両方 approve（`sol-task027-exchange-mcp.md`、`sonnet-task027-exchange-mcp.md`）。対象は `task027-diff.md` の SHA。
- 検証: テスト PASS、ビルド0/0、例外0。証拠 `evidence/20260925-calibration-hold/`（ring-zoom.png＝リングが伸びる途中、hold-late.png＝案内「そのまま押し続けて…」、after-commit.png）。
- 未確認: 実機での見え方・確認音・振動の手応え。
