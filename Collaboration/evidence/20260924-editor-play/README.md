# Editor Play 証拠（2026-09-24 02:03–02:07 JST）

撮影者: Claude Opus 5.5（computer-use、ユーザー許可済み）。Unity 6000.3.15f1、LoopRoom シーン、desktop モード（HMD なし、OpenXR ローダーなしで desktop fallback）。対象コミット `68a78f6`（LoopDemo.cs `9d6925b4…`、LoopModel.cs `2aeda54c…`）。画像は PrintWindow で Unity のウィンドウだけを描画したもの（他のウィンドウは写らない）。

| ファイル | 内容 | 対応する受入 |
| --- | --- | --- |
| `01-early-loops-no-new-warnings.png` | LOOP 08、S01 49.3s。7周（毎周 t=3 の Latch と t=6 の射撃）を経ても Console はエラー0・警告4・情報1のまま（Play 開始時と同じ既知の4件） | task005 受入2（`Can not play a disabled audio source` 警告なし） |
| `02-past-180s-still-playing.png` | LOOP 35、S01 210.5s、IN PROGRESS。180秒を超えても TimedOut にならない。Console 変化なし | task007 受入4（Editor Play で180秒超の継続） |
| `03-escaped-then-ending-finished.png` | 左上に LOOP 37・ESCAPED、S01 237.0s、「周回記録を保存しました」。撮影時点でエンディング（8秒）は終わり、開始待ちの案内に戻っている | 180秒超でも遊べる（遮蔽→脱出） |
| `session-log-escaped.json` / `.png` | LoopDemo が保存したセッションログ。outcome=Escaped、elapsed=228.96s、enforcePlayLimit=False（シーンの playLimit=172 は保存されたままだが使われない）、37周、TimedOut 0件。最後の周回: 0.13s 遮蔽 → 6.0s shot_blocked → 7.2s 脱出 | task007 設計（シーン未編集で既定 false） |

## 未確認

- Latch・射撃の**実際の可聴音**（画面とログでは確認できない。警告が出ないことだけを確認）。
- Quest 3 / 3S 実機（VR 中のフォーカス切替・ダッシュボード、HMD への観客視点混入、Touch/Touch Plus 操作）。

## 気づいた点（修正は未実施、計画担当に報告）

- desktop モードで、開始待ち・終了後の案内パネル（`Ready and ending panel`、カメラ前 0.82m）の文字が Game ビューからはみ出す（`03` の中央）。Play 開始直後にも同じ表示。VR では見え方が違う可能性があり、Quest 3/3S の見え方確認（STATE 計画6）と合わせて扱う。
