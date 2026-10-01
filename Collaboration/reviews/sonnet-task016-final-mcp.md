# task016 独立レビュー（最終）

対象: task016全体 — (a) `LoopDemo.cs` の `--desktop` 必須化、(c) `Docs/DEVICE_QUICKCHECK.md` #12 の手順追記と `Start-DeviceCheck.ps1`。

参照SHA256:
- `Collaboration/tasks/016-device-check-readiness.md`: `305baacbb1ed6d66356dc8342db60ff001c31ba8c0d4c4c70af9587ad58da0c0`
- `Docs/DEVICE_QUICKCHECK.md`: `14c08670538db9db5c29554b39779d0905a8152e94c3d96047124ee4832eb7cc`
- `Start-DeviceCheck.ps1`: `fee188babb3741d7890689918d9459ab1ca41ab126336a860d8acd39841eae7a`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `12de37174914c99e60248fc4d02fb3ed6377a6420cadf95122c99f95dc0d29f3`

## 指摘

### 1. [Low] Docs/DEVICE_QUICKCHECK.md #12 — F2トグルの前提が未確認
- 場所: `Docs/DEVICE_QUICKCHECK.md` 表12行目
- 再現条件: F2はトグル動作（`LoopDemo.cs` `Update()` 内 `if (keyboard.f2Key.wasPressedThisFrame) privateOverlay=!privateOverlay;`）。#11末尾で「F2をOFFに戻す」を実施し忘れた場合、または#12を単独実行した場合、「F2で運営表示をONにしてから」の指示どおりF2を1回押すと、実際にはON→OFFに切り替わってしまい、意図と逆の状態になる。
- 修正案: 「現在PC画面の運営表示が消えていることを確認し、出ていなければF2を押してONにする（既に出ていればそのまま）」のように、現状確認を含む一文に変更する。
- 重大度は低い。運営者はPC画面を見て表示有無を確認できるため、誤操作しても目視で気づいて再度F2を押せば復旧できる。ブロッキングではない。

### 2. [Info/検証不能] `(a)` の `--desktop` 一致性
- 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs` `Start()` 内 `desktopArg = Array.IndexOf(args, "--desktop") >= 0; autostart = desktopArg && ...; autoescape = autostart && ...;`
- 前回の独立レビュー指摘（Sol高）「`--autostart` が `rig.IsVR` だけで判定されており、`--desktop` なしで起動してdesktop fallbackになった場合にも自動開始する」に対しては、`desktopArg` を明示的な `--desktop` 引数の有無で判定するよう修正されており、要求どおりの対応になっている。`autoTrigger` にも `!rig.IsVR` を残しているため、VR成立時は自動開始しないという元の安全策も維持されている。ロジック自体は妥当。
- ただし、コード内コメント「Match DemoRig's own `--desktop` reading」が実際に `DemoRig.cs` の読み取り方法と一致しているかは、`DemoRig.cs` が本スナップショットに含まれていないため確認できない。

### 3. [Info/検証不能] #12 のLink切断フローの実装的裏付け
- `DEVICE_QUICKCHECK.md` #12 が想定する「Linkケーブルを抜く→0.3秒ほどで中断→つなぎ直す→Rで再準備」というフローのうち、
  - 「0.3秒ほどで中断」は `LoopDemo.cs` `Update()` の `if(trackingLost>.3f) { Model.Interrupt(); trackingLost=0; }` と一致しており、これは確認できた。
  - 一方、`rig.HeadTracked`・`rig.RuntimePresent()`・`rig.CanRetryPreparation`・`rig.PreparationMessage` の実装は `DemoRig.cs` に依存し、本スナップショットには含まれていないため、実際にLink切断でこれらが期待どおりの値になるか、また `PreparationMessage` の文言に「R: 再準備」相当の案内が含まれ「HMDの案内」でも確認可能になっているかは検証不能。

### 4. [問題なし] `Start-DeviceCheck.ps1`
- 手順書#0章の起動コマンド（`-Device` のみ、`-Desktop` 無指定）は通常VR起動と一致し、`--autostart`/`--autoescape` を渡さないため(a)の変更による副作用はない。スクリプト自体に構文・パス処理上の欠陥は見当たらない。

## 検証済み事項
- `LoopDemo.cs` の `--desktop` 必須化ロジック（`desktopArg`/`autostart`/`autoescape`）と、周回2でのt≈1秒遮蔽→出口開放時脱出という `autoescape` の挙動は、タスク文書の要求と前回指摘への対応と一致している。
- Link切断→0.3秒での中断タイミングは `LoopDemo.cs` 内で確認できた。

## 未検証事項
- `DemoRig.cs`（`IsVR`, `CanRetryPreparation`, `PreparationMessage`, `RuntimePresent`, `HeadTracked` の実装）が未提供のため、(a)のdesktop引数一致性、および#12のLink切断→再準備フローの実装的正しさは確認できていない。
- タスク受入条件1（batchmodeコンパイル エラー0・警告0）は本レビューで実行していない。
- タスク受入条件2（ビルドのdesktop自動実行スクリーンショット・セッションログ証拠）は本レビューで確認していない。
- Unity上での実際の動作・実機確認は未実施（本レビューはソース精査のみ）。

## 判定

**approve**（指摘1は非ブロッキングな改善提案。実施は必須としない）