## Findings

- **中 — Playing 開始と同じフレームに再準備を実行できる**
  - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:79-84`
  - SHA256: `fccd609817de6128f12fe4f4a7dc5936aa19074175650609ef9fa80a6eb9ffb0`
  - 発生条件: desktop fallback の Ready/Finished 状態で、Enter と R を同一フレームに押す。
  - 原因: `idle` を `Begin()` 前に一度だけ計算しているため、83行目で `Model.Phase` が Playing に変わっても、84行目は古い `idle == true` を使って `RetryPreparation()` を実行する。
  - 影響: 「Playing / Blackout 中は再準備しない」という受入条件に反し、周回開始直後に XR の停止・破棄・再初期化が走る。
  - 修正案: 開始処理と再準備処理を相互排他的にする。例えば83行目を実行した場合は `return` する、84行目を `else if` にする、または再準備直前に現在の `Model.Phase` を再確認する。

## Verdict

**request_changes**

上記の状態遷移不具合を修正後、再レビューが必要です。`DemoRig.cs` の `CanRetryPreparation`、所有 XR のみを停止する処理、フラグ・origin・floorInputs の初期化、`OnDestroy` の停止処理については、提示ソース上で追加の具体的欠陥は確認できませんでした。

## 対象SHA256

- `AGENTS.md`: `c7a53997e17be6079c1f3c8f675fbca3ebf74757f14665036a7b019b4656f731`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`
- `Collaboration/tasks/012-xr-retry-preparation.md`: `61e729170467eaaa9518d902b5d408629df463683bd83d8310461f0ba555f74b`
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `5f29a20f751ed95b6ac0e47b7fcbacc123191c9eea3ee2a9341042380edba250`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `fccd609817de6128f12fe4f4a7dc5936aa19074175650609ef9fa80a6eb9ffb0`

## 未確認事項

- batchmode コンパイルのエラー0・警告0。
- HMDなしの Editor Play における fallback → R → 再fallback。
- Playing/Blackout中の通常のR、およびEnter+R同時入力の実動作。
- OnGUI の実画面上の位置・折り返し・Box高さ。
- Quest 3でのLink切断→再接続→再準備。
- XR Management の停止・破棄・再初期化ライフサイクルの実機挙動。

テストやUnity実行は行っていません。
