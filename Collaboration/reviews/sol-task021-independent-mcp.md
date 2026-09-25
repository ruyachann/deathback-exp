## 独立レビュー結果

**判定: `request_changes`**

対象スナップショットのみを静的に確認した。テスト・Unity・実機は実行しておらず、他レビューも参照していない。

### Findings

1. **High — 境界外で決定しても、必須の運営警告とログ警告が出ない**

   - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:283-288`（`CommitCalibration`）、同 `402-419`（`OnGUI`）
   - SHA256: `475fb4f21ce6d127cd95baa338b63623e6212a5ca1e62a0510d90a8729372e87`
   - トリガー: Guardian 境界を取得でき、体験空間の枠が赤い状態で C または A/X 長押しにより決定する。
   - 問題: `CommitCalibration()` は境界への収まり状態を参照せず、通常どおり確定するだけ。`OnGUI()` にも「境界情報なし」はあるが、境界外で決定したことを示す警告がない。`fitsWarned` と `PlaceRoom()` の警告は RoomAnchor の到達範囲判定であり、Guardian 境界外の警告ではない。
   - 修正: `CalibrationView` から `Unknown/Fits/Outside` を公開し、確定時に `Outside` なら `Debug.LogWarning` を出す。決定後も運営表示に警告を保持する。確定直前の頭位置・yawで判定を更新すること。

2. **High — 凹形の境界で、枠がはみ出していても緑になる**

   - 場所: `Assets/LoopRoom/Scripts/CalibrationView.cs:158-170`（`FitsBoundary`）
   - SHA256: `3e895b7f2fe51ea6f364c82e08197f658c601da9f51971a82284d1ac252d90be`
   - トリガー: Guardian 境界に内向きの凹みがあり、正方形の四隅は境界内だが、正方形の辺または内部が凹みをまたぐ配置。
   - 問題: 四隅の `PointInPolygon` だけで「正方形全体が境界内」と判定している。凹多角形では四隅が内側でも辺が境界外を通るため、要件に反して危険側の緑表示になり得る。
   - 修正: 四隅の内包判定に加え、正方形の各辺と境界辺の交差を検査する。境界上の扱いも明示し、安全側に赤とする。

3. **Medium — キャリブレーション状態のまま追跡不能になると部屋が再表示される**

   - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:186-192`
   - SHA256: `475fb4f21ce6d127cd95baa338b63623e6212a5ca1e62a0510d90a8729372e87`
   - トリガー: キャリブレーション表示中に頭部追跡や Floor 準備が失われ、`rig.CanStart` が false になる。または起動後、準備完了前で `calibrating == true` の状態。
   - 問題: 部屋を隠す条件が `calibrating` ではなく `calibrationVisible = calibrating && idle && rig.CanStart` に連動している。追跡不能時にはキャリブレーション自体は継続中なのに `SetRoomVisible(true)` が呼ばれ、部屋や仕掛けが見える。追修正の「キャリブレーションの間は部屋を表示しない」を満たさない。
   - 修正: 部屋の非表示条件を `calibrating && idle` に分離する。追跡不能時は床表示を止めても、部屋は非表示のままにする。R による `ResetAlignment()` 後も同一フレームで反映する。

4. **Medium — C と開始入力が同一フレームなら、そのままセッションが始まる**

   - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:182-208`
   - SHA256: `475fb4f21ce6d127cd95baa338b63623e6212a5ca1e62a0510d90a8729372e87`
   - トリガー: キャリブレーション中に C と Enter、または C と A/X の新規押下が同一フレームに入る。
   - 問題: C で `calibrating=false` にした後、同じ `Update()` 内の開始条件が成立して `Begin()` まで進む。コードコメントの意図と異なり、Enter/R/C の排他が保証されていない。desktop fallback では C と R の同時入力も同一フレームに処理され得る。
   - 修正: `operatorCommandConsumed` のようなフレーム単位の消費フラグを設け、C または R を処理したフレームでは開始判定を行わない。入力の優先順位を一か所で決定する。

### 未確認事項

- batchmode のコンパイル結果、テスト全件 PASS、例外 0 の出力はスナップショットに含まれず未確認。
- `evidence/20260924-calibration/calibration-screen.png` は内容が提示されていないため、白枠・60度視点・決定後画面を未確認。
- Quest 3/Air Link での境界取得、床位置、ステレオ表示、A/X 1秒長押しは未確認。
- `RoomVisuals.PrivateLayer == 8`、観客カメラの culling mask、`RoomVisuals.UpdatePublic()` が非表示中の Renderer を再度有効化しないことは、該当ソース未提示のため未確認。
- `PlayAreaSettings` の値検証と `RoomAnchor.ChooseFrontYaw` の契約、Finished からの `LoopModel.Start()` の挙動も未確認。

### 確認したスナップショットのSHA256

- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`
- `Collaboration/tasks/021-calibration-mode.md`: `0086f44ba98373d6a28805c90335be9910f8b10795d1eb076632aa49a42cb124`
- `LoopDemo.cs`: `475fb4f21ce6d127cd95baa338b63623e6212a5ca1e62a0510d90a8729372e87`
- `CalibrationView.cs`: `3e895b7f2fe51ea6f364c82e08197f658c601da9f51971a82284d1ac252d90be`
- `DemoRig.cs`: `9e5faf86421f94746fd428d238eae9e8d891a4a6241af8603e7f1d87f4cdf671`
