# 管理基盤の独立コードレビュー

実施日: 2026-09-17（Asia/Tokyo）
担当: Codexの独立レビューセッション（設計担当側）
判定: 静的コードレビューとオフライン試験の範囲で承認。未解決のP1/P2は検出していない。

## 対象版（SHA256）

| ファイル | SHA256 |
| --- | --- |
| `Tools/automation.py` | `55380183f178331a309cafd37b9950f49576d8682b31716e1fe674993518b66d` |
| `Tools/windows_job.py` | `5041aae843c9aa2eac2063f511128cb58f56804c5d8336cbdd302dbda4ed5021` |
| `Tools/test_automation.py` | `e014442adadb322269eea0fb323d8c4368077f7d12f2c74c71e0efc01dbbecb7` |
| `Start-Automation.cmd` | `aa6664736e46fa7e3ebca9d8368dd7e5c7e6379b7e233093494c1186fa4ed9b7` |

## 指摘と修正確認

- P1: original読込と初回snapshotの間のmanifest変更が上書きされる問題。旧版で再現済み。`baseline[TARGET] == sha(original)`の検査により、設計/実装の呼出前に停止することを修正後のテストで確認。
- P2: timeout後にCLI子プロセスが残る問題。旧版で子プロセスの遅延書込みを再現済み。WindowsではCLIをsuspendedで作成し、KILL_ON_JOB_CLOSEのJob Objectに所属させた後に再開する実装へ変更。親子timeout試験で遅延書込みが行われないことを確認。breakaway、プロキシ変更、CLIサンドボックス解除は追加されていない。
- P3（任意改善）: `run_task`冒頭の元manifest読込みは`json.loads`であり、既に1.17.0と判定される重複キー付きJSONはno-op経路で`strict_json`を通らない。この経路によるファイル変更はない。通常経路の候補とモデル回答は重複キーを拒否する。

## 確認した境界

候補SHA256・baseline SHA256・設計SHA256・review phaseの一致を検査する。独立レビューには相手の報告を含めず、その後の交換で両者の報告を渡す。同一候補に対する両者の最終approveだけで適用する。修正案は再レビューされる。許可されたmanifestのInput System 1.12.0→1.17.0だけを意味的に許可し、単一ファイルをbackup後にatomic置換する。モデルの別名への自動切替は実装されていない。

## 実行した検証

`python -B -m unittest discover -s outputs/LoopRoomUnity/Tools -p test_automation.py -v`

16件すべて成功（この独立レビューセッションで実行）。競合、古い承認、範囲外変更、レビュー不一致、再レビュー、重複キー、置換失敗、排他ロック、モデル不一致、親子timeoutを含む。ゲームコード、manifest、実CLI接続は変更・実行していない。

## 未検証と承認範囲

実際のAstra/Sonnet API呼出、認証、指定モデルの提供状況、CLIの実出力との接続互換性は未検証。Codex側は明示的な`-m`指定の記録であり、出力から実ランタイムモデルを検証できたとは扱わない。Unity依存解決、統合コンパイル、Play、Quest 3実機受入も未実行。本報告は管理スクリプトのレビューであり、実モデル間の相互レビュー実施済み・ゲーム受入済みを意味しない。

利用者変更の検査はsnapshotによる検出であり、最後のsnapshotと置換の間の任意の外部writerをOSレベルで排他するものではない。
