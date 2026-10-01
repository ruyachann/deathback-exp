# task025 交換照合（2026-09-25、Opus 5.5）

- 独立: Sol・Sonnet とも request_changes（Repeat の端で模様が不連続、同じ素材のマテリアルが共有されていない、床の証拠がない）→ 追修正（周期ノイズと整数周期の波、マテリアルのキャッシュ、壁のムラを強める、額縁に木目、証拠用 `--desktop-pitch`）。
- 交換後: Sonnet approve／Sol request_changes（壁の模様が 255 で上側クリップ）→ 追修正2（基準 249、クリップなし。Build() でキャッシュを空に）。
- 最終: Sol・Sonnet とも approve（`sol-task025-final-mcp.md`、`sonnet-task025-final-mcp.md`、RoomVisuals.cs `bdc4fd3e…e5fb`、DemoRig.cs `240a0439…10f2`）。Sonnet の低2件（布が極めてまれにクリップし得る、static キャッシュ）は影響が小さく対応しない。
- 証拠 `evidence/20260925-textures/`: compare.png（全体の明るさ・色合いはほぼ同じ）、zoom.png（扉の木目・壁のムラ）、after-pitch40.png・floor-zoom.png（床板の継ぎ目と互い違いの端、机の天板の木目）。テスト PASS、ビルド0/0、例外0。
- 見送り: RGB24 非圧縮（数 MB 以内）。未確認: 実機でのちらつき・モアレ、質感が十分か（ユーザーの Air Link 確認で判断）。
