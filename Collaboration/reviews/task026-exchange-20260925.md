# task026 交換照合（2026-09-25、Opus 5.5）

- 独立: Sonnet approve／Sol request_changes（P95 が 100ms 超の区間で最大値を返す、開始前の時間が最初の dt に入る＝証拠の max 743ms、Advance 後の状態で判定、巨大値の添字）。撮影で運営表示の行がはみ出すことも判明 → 追修正（100〜1000ms の区間、開始直後の1フレームを捨てる、Advance 前の状態、飽和、表示の短縮、検査4件）。再実行で max 743→36ms。
- 交換後: Sonnet approve／Sol request_changes（低: ms 換算後の無限大、1.5倍ちょうどの検査）→ 追修正2（3600 秒で飽和、閾値を保持、境界ちょうど・直下・直上の検査。Math.BitIncrement は .NET Framework 4 に無いので BitConverter で置き換え）。
- 最終: Sonnet approve／Sol request_changes（低: 目標 Hz が極端だと飽和と矛盾、int のあふれ）→ 追修正3（目標 Hz 1〜1000、long、GetTargetHz は範囲外なら既定値＝計画担当の追加。これが無いと Begin で例外になる）。
- 最終2: Sonnet approve／Sol request_changes（低: 2^53 フレームを超えると P95 の順位がずれる）→ 計画担当が根拠付きで見送り（165Hz で約170万年）。Sol が再判定で approve（`sol-task026-rejudge-mcp.md`）。
- 検証: テスト 20+12+12 PASS、ビルド0/0、例外0。desktop 自動実行でセッションログに frames（mean 0.80ms、p95 1.5ms、max 48.8ms、dropped 1、目標 164.9Hz）。証拠 `evidence/20260925-frame-stats/`（session-frames.png、overlay-compare.png＝表示のはみ出しの修正前後）。
- 手順の気づき: 自動実行が遅いと `--autoescape` まで到達しないことがある（撮影の間隔を広げて再実行で解決）。
- 未確認: Air Link の実機での目標 Hz（72/90/120）の取得と値。
