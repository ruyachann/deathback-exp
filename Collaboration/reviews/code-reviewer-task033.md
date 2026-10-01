# code-reviewer（claude-sonnet-5-5）task033 独立レビュー

# task033 独立レビュー結果

**判定: request-changes**（P2 が 1 件。修正は小さく、既定値のままなら実害はありません）。
**Opus 判断が必要な点**: 指摘 1 の直し方（Validate で拒否するか、ExitAvailable で窓の処理を待つか）の選択。

## 対象ファイルと SHA256

SHA256 は **自分では計算できませんでした**。Bash が無効で、PowerShell 相当のツールも権限が付与されていません。下の値は差分ファイル冒頭の記載をそのまま書き写したものです。

| ファイル | SHA256（task033-diff.md 記載、未検証） |
|---|---|
| Assets/LoopRoom/Scripts/LoopModel.cs | dcc375db920c8752ab07c401f45331cc9f46020fa6f8928478cc02f01120471d |
| Assets/LoopRoom/Editor/LoopModelChecks.cs | 2b74356322497d5a37ed1d015253dafa148808dbd1050a07f7ac9c5119202dfc |
| Tests/LoopModel.Tests/Program.cs | c1514fa0fe718a015e5d2249f6194446c9957fb12b068ab59926ea06b3da68ed |
| Assets/LoopRoom/Scripts/LoopDemo.cs | 81560a15c2af8539199f28b763394d0b30098a8d1507bf98d1eaa6217543ba7e |
| Assets/LoopRoom/Scenes/LoopRoom.unity | 2f3b7c99f86994ea085a501103888c0c7199fc672bcc65c29856af89e6accbb7 |

読んだソースは差分と内容が一致しています。ただし、読んだ時点のハッシュが差分記載のものと同じかどうかは確認できていません。

## 指摘

### P2: 出口が窓の狙撃に依存しない（LoopModel.cs:79-80、Validate 36-49 行）

- **再現条件**: 出口の開く時刻を窓の狙撃より前に設定する。例として `new LoopRules{ exitOpens=6.5, exitCloses=7 }` を使う（Validate は通ります）。
- **何が起きるか**: t=6.5〜7 に、ブラインドを下ろさずに脱出できます。`ExitAvailable` が `ShotResolved` だけを条件にしているためです。窓の脅威が飛ばされます。
- **既存テストとの関係**: LoopModelChecks.cs:66 と 68（`exitOpens=6.0/6.5` と `windowShot=6.05/9`）は、この組み合わせが有効だと確認する形になっています。
- **既定値への影響**: 既定の 9.5 と、シーンの 9.5（`LoopRoom.unity:51`）は正しいため、現状の実害は出ません。`exitOpens` を含むのは Assets 内ではこのシーンだけで、旧値の 6.5 は他に残っていません。
- **UI の不整合**: LoopDemo.cs:364 の開錠音は `ExitOpens` を基準に鳴ります。ランプ（492 行）は `ExitAvailable` に従います。`exitOpens` が窓より前だと、窓の前に開錠音が鳴ります。
- **修正案 A（推奨）**: `Validate` に `exitOpens >= windowShot - IntervalTolerance` を追加します。出口の時刻、開錠音、ランプが一貫して保たれます。
  - 影響するテスト: LoopModelChecks.cs:59-60（`exitOpens=MinInterval`、`windowShot=2*MinInterval`）、66、68、71-73 の `exitOpens` を窓以降に直す必要があります。`windowShot` と `exitCloses` の大小も合わせて見直してください。
  - 追加するテスト: 「exitOpens が windowShot より前の Rules を拒否」。
- **修正案 B**: `ExitAvailable` に `windowResolved` を加えます。
  - ただし `exitCloses <= windowShot` の Rules だと、出口が永遠に使えない（クリア不能）設定になります。`exitCloses > windowShot` も Validate に必要です。
  - 開錠音の基準を `max(exitOpens, windowShot)` にする必要もあります。
  - A のほうが小さい変更で済みます。
- **補足**: 既定値では、窓の処理後に Playing のままなら遮蔽もブラインドも成立済みです。出口の判定に ShieldRaised や BlindsClosed を足す必要はありません。

### P2（既知の予定）: VR で窓を防ぐ手段がまだない（LoopDemo.cs:130-132、344-349）

- ブラインドの操作は desktop の B キーだけです。VR は `ShieldHandle` と `ExitHandle` しか配線されていません。
- **再現条件**: VR で遮蔽を上げて t=9 まで待つ。
- **何が起きるか**: 必ず `window_shot` で死亡し、VR では脱出できません。
- task034 の範囲なので 033 の欠陥とは扱いません。ただし、033 と 034 の間に実機（VR）確認を挟むと、必ず失敗する版になります。

### P3: 死因の表示は内部名のままで、運営の F2 表示と同じ置き場所にある（LoopDemo.cs:601）

- 「直前の死因 window_shot ×2」と英語の内部名がそのまま出ます。運営用なので実害はありません。
- **観客への漏れ（確認事項 5）**: `showCauseStreak` は `!rig.IsVR || privateOverlay` で、既存の遮蔽・出口のキー案内（577 行）と同じ条件です。
  - desktop は観客カメラが無効（RoomVisuals.cs:598）なので漏れません。
  - VR では F2 が既定で OFF のときだけ隠れます。F2 を ON にした運営画面を観客が見ると、死因が見えます。
- 既存の方針の範囲内で、新しい漏れ方ではありません。気になるなら、死因を日本語の短い名前にするか、F2 を観客の前で押さない運用を Docs に書いてください。

### P3: `Kill(id, null)` で連続回数が更新されない（LoopModel.cs:136）

- 公開 API の `Kill` に null を渡すと `pendingDeathCause=null` になり、直前の死因が古いまま残ります。
- 内部の呼び出しは固定文字列なので実害はありません。気になるなら `cause ?? "unknown"` にします。

### P3: window_shot の銃声が敵の位置から鳴る（LoopDemo.cs:371-372、488）

- `enemyAudio` は敵の人影の位置にあります。窓から撃たれる音が、部屋の中の男の方向から聞こえます。
- 033 では未実装でも問題ありません。task034 で窓の位置から鳴らしてください。
- `window_blocked` は音も振動もなく、画面に何も起きません。計画の「何も起きない」どおりです。ただし、遮断できたことが分かる合図は 034 で検討が必要です。

## 確認項目への回答

- **(1) 因果順と長いフレーム**: 問題なしと判断しました。
  - `next` は `!ShotResolved ? firstShot : !windowResolved ? windowShot : searchShot` で、LoopModel.cs:181 のスライス方式により 6→9→12 が必ず順に処理されます。
  - 6 で死亡すると Blackout になり、同じ `Advance` 内で 9 は処理されません。
  - 死亡処理は `Kill` が Playing かつ同じ周回のときだけ通るので一意です（二重死亡の余地なし）。
  - 旧周回の入力は、`CloseBlinds(expectedLoop)` が `Phase`、`LoopId`、既に下ろした場合を拒否します。
  - LoopDemo では `Advance` が先で、その後に入力を処理します（342-349 行）。9 をまたぐフレームで B が押されても、死亡後なので拒否されます。既存の「損害判定が先、新しい入力は後」の方針と同じです。
  - `exitOpens`（9.5）や `exitCloses`（12）の境界は、スライス末尾で `LoopTime >= next - 1e-7` を判定するため隙間がありません。12 では `ExitAvailable` が false になるのと同時に flanked が処理されます。
  - `lastRecords` のループで、9 での死亡も銃声と振動を鳴らします（371 行）。
- **(2) ExitAvailable の依存**: 上の P2 のとおりです。
- **(3) SameCauseStreak / LastDeathCause**: 設計どおりです。
  - 更新は `BeginLoop` の冒頭、つまり次の周回の始めです。
  - 脱出（`End(Escaped)`）と中断は `pendingDeathCause` に触れません。
  - 再開始の `Start()` で null と 0 に初期化されます。Blackout 中に中断しても pending は `Start()` で消えます。
  - 死因が違えば 1 に戻ります。
  - 机上でテスト 7 を追って、期待値どおりになることを確認しました。
- **(4) 既存テストの修正**: 意図は変わっていません。
  - 「Shield grants…」と「No escape before…」は、遮蔽に加えて `CloseBlinds` と時刻 9.5 を足しただけです。
  - 「Missing escape window…」は `CloseBlinds(1)` を足しただけで、12 での flanked は保たれます。
  - 「Minimum interval」は 2 点が気になります。
    - 「search interval」の項目は、実際には `searchShot-windowShot` の下限違反を検査します。`searchShot-firstShot` の判定は窓の 2 制約が成り立つと冗長になり、単独では当たらなくなりました。名前の変更は必要ありません。
    - 先に述べたとおり、`exitOpens=6.0/6.5` の組み合わせが有効だと確認する形になっています（P2 の修正で直す対象です）。
  - テスト件数は、Program.cs の期待値 27（旧 19 に新規 8 件）と 28（全体）が、追加分と一致しています。
- **(5) 運営表示から観客への漏れ**: 上の P3 のとおりで、新しい漏れ方はありません。`window_shot` などの内部名は運営表示以外には出ません。死亡の効果音と暗転は死因に依存せず、死因の文字も出ません。

## テスト上の補足（P3）

- 小さな刻み（0.01 秒）でも、遮蔽とブラインドあり → 出口まで通る道筋が同じ結果になるテストはありません。既存の「大きい刻み/小さい刻み」の検査は入力なしの死亡だけです。9.5 付近の浮動小数点誤差は無害だと考えますが、追加すると安心です。

## 実行した検証

- **ここでは何も実行していません**。Bash が無効で、desktop-commander の `start_process` も権限未付与でした。SHA256 とテストの再実行は未実施です。
- 代わりに行ったこと:
  - ソース全文の通読。
  - テスト 7 件（9 で死亡、ブラインドで遮断、出口の 9.5 と 12、6 での死亡、5→13 の長いフレーム、`SameCauseStreak`、`CloseBlinds` の拒否）の浮動小数点を含む机上追跡。すべて期待値どおりでした。
  - Opus が保存した `model-run.txt` の確認（テスト 28 件 PASS、exit=0、1 周目 t=6 first_shot、2 周目 shield_raised と blinds_closed が t=1、shot_blocked が 6、window_blocked が 9、Escaped が 9.5）。この出力が自分の読んだ内容と矛盾しないことは確認しました。

## 確認できなかった事項

- 上記 SHA256 の検証。
- テスト、コンパイル、Unity の実行。
- Unity 上でのシーン読み込み後の `timings`。ただし YAML の値（windowShot 9、exitOpens 9.5）は読んで確認しています。
- VR と実機の挙動。
- 小さな刻みでの境界（9.5）の確認。

ファイルは変更していません。