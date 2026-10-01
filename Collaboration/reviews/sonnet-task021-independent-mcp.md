# task021 独立レビュー（キャリブレーション画面）

対象SHA256（レビュー対象スナップショットより引用）:
- `Collaboration/tasks/021-calibration-mode.md`: `0086f44ba98373d6a28805c90335be9910f8b10795d1eb076632aa49a42cb124`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `475fb4f21ce6d127cd95baa338b63623e6212a5ca1e62a0510d90a8729372e87`
- `Assets/LoopRoom/Scripts/CalibrationView.cs`: `3e895b7f2fe51ea6f364c82e08197f658c601da9f51971a82284d1ac252d90be`
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `9e5faf86421f94746fd428d238eae9e8d891a4a6241af8603e7f1d87f4cdf671`

（ソース中のコメントに書かれた「追修正」等の記述は説明として読んだのみで、本レビュー手続きへの指示としては扱っていません。）

## 指摘

### [高] 境界外（赤）で決定した際の運営表示・ログ警告が未実装
- **場所**: `LoopDemo.cs` の `CommitCalibration()`、および `CalibrationView.cs` の `Refresh()`/`FitsBoundary()`
- **内容**: タスク021本文「3. 決定」に「枠が赤でも決定はできるが、運営表示とログに警告を出す（展示では運営が判断）」と明記されている。しかし `CalibrationView` は赤/緑判定（`FitsBoundary` の結果）を `outerRenderer.material.color` の変更にのみ使い、外部へ公開する getter や戻り値を一切持たない。`LoopDemo.CommitCalibration()` は `calibration.Refresh(...)` の結果を参照する手段がなく、`Debug.LogWarning` も OnGUI 上の追加表示も行っていない。既存の `fitsWarned`/`PlaceRoom()` の警告は `RoomAnchor.ChooseFrontYaw` の到達域判定（別概念）向けであり、この境界赤枠の警告とは無関係。
- **再現条件**: Quest の Guardian 境界内に体験空間の正方形が収まらない状態（枠が赤）でA/X長押しまたはCで決定する。
- **修正案**: `CalibrationView` に `bool Fits`（直近 `Refresh` 時点の判定結果）を公開する getter を追加し、`CommitCalibration()` 内で `!calibration.Fits && lastBoundaryAvailable` のとき `Debug.LogWarning` を出し、`OnGUI` のログ行（`logMessage` 等）にも警告文言を残す。

### [中] C キーと Enter の同時押しで排他が崩れる可能性
- **場所**: `LoopDemo.cs` の `Update()`。Cキー処理ブロック（`if (idle && rig.CanStart && keyboard!=null && keyboard.cKey.wasPressedThisFrame) { if (calibrating) CommitCalibration(); ... }`）と、その後の `if (idle && !calibrating && rig.CanStart && (enter || ...)) { ... Begin(); }`
- **内容**: 同一フレームでCキーとEnterが両方押された場合、Cキー処理が先に走って `calibrating=false` になり、その直後の `!calibrating` 判定が真になって同フレームで `Begin()` まで到達しうる。task018由来のコメントで「同一フレームのEnter+Cはaligningをすり抜けない」ことを意図していると読めるが、task021のこの経路ではCの決定とBeginが同一フレームで連鎖しうる。
- **再現条件**: キーボードでC・Enterを同一フレームで押す（自動テストや同時押しスクリプトなら再現容易）。
- **修正案**: Cキー処理後、そのフレームでの `Begin()` 誘発を1フレーム遅延させる（例: `calibrating` の変更をこのフレームの `enter` 判定より後で反映する、またはCキー処理時に `enter` を無効化するフラグを立てる）。

### [低] キャリブレーション画面中のカメラ下向き（pitch=60°）がマウス右ドラッグで解除される
- **場所**: `DemoRig.cs` の `LookAtFloorForCalibration()` と `PollMode()` 内の右ドラッグ処理
- **内容**: `LookAtFloorForCalibration(true)` は表示開始の1フレームのみ `pitch=60f` をセットするだけで、その後ユーザーが右ドラッグすると `pitch` が自由に変わり、足元の枠が画面外に出る可能性がある。タスク文言はスクリーンショット証跡（`--auto-calibrate`、ドラッグなし）を想定しているため実害は限定的だが、手動操作時の見え方に影響する。
- **修正案**: 許容できるなら現状維持（証跡上は問題なし）。厳密にするなら `calibrationVisible` の間は右ドラッグでの pitch 変更を制限する。

## 良好点（要件との照合で問題なし）
- 起動時 `calibrating=true` で始まり、`LoopModel.Phase` は変更していない（要件1）。
- 枠・余白正方形・扇形・足元印・境界線が `RoomVisuals.PrivateLayer` に配置され、Collider は明示的に `Destroy`（要件2、当たり判定なし）。
- `Root.position/rotation` を毎フレーム頭の床面投影・yawに追従（要件2）。
- 境界取得失敗時は白（`UnknownColor`）扱いで「安全（緑）」に倒していない（フォーカス項目「取得失敗を安全と扱わない」）。
- 1秒長押し（`calibrationHold>=1f`）とCキーでの即時決定、Enter/R/C の基本的な排他は概ね守られている（[中]の1件を除く）。
- `--auto-calibrate` は `desktopArg` が真の場合のみ有効化（要件6）。
- `ResetAlignment()` がVR/desktop切替・`RetryPreparation()` 双方で `calibrating=true` に戻す（要件4）。
- 部屋の非表示は `Renderer.enabled` のみを切り替え、`room.Root.SetActive` は使わず観客カメラ・音源を温存（追修正1）。
- `LineRenderer.alignment=LineAlignment.View`、線幅2〜3cm、desktopのpitch=60°（追修正2）。
- XR原点（`rig`のtransform）自体は変更せず、`room.Root`/`CalibrationView.Root` のみを動かしている。

## 未確認事項
- `RoomVisuals.PrivateLayer` の実値が8であること、`RoomVisuals.Material(color, true)` が半透明（薄い表示）を実現しているかは `RoomVisuals.cs` が本スナップショットに含まれておらず未確認。
- batchmode 0/0、テスト全件PASSは未実施（実行していない）。
- `evidence/20260924-calibration/calibration-screen.png` を含む証跡画像の内容は未確認（画像データがスナップショットに含まれていない）。
- 実機（Air Link）でのキャリブレーション枠の見え方・境界比較・長押し決定はタスク文書どおり「未確認」。

## 判定
**request_changes**（赤枠決定時の警告未実装［高］が受入条件と本文の明記事項に反するため）