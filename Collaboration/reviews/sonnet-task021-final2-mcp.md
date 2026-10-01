# task021 最終レビュー（独立判定）

対象SHA256（提示されたスナップショットのもの）
- `Assets/LoopRoom/Scripts/CalibrationView.cs`: `43da0c7f41853f277eee6fde8a965acf2ff85ce5668047b54bddb39518dee9b1`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `66bbc36959c120c5413e1001056ae3062543ce75b7421eafe1ea74cbb07dd8e0`
- `Collaboration/tasks/021-calibration-mode.md`: `97386da25769aa928770e4124fca2a6b764a3ace2c3a563090f3231fc21b6bfb`

## 検証結果（追修正1〜5、線と接触判定・空境界を中心に）

| 指摘 | 実装箇所 | 判定 |
|---|---|---|
| epsilonを外積ではなく点-線分距離（double, 1e-4m）で判定 | `CalibrationView.cs` `PointSegmentDistance`/`SegmentsIntersect`（Epsilon=1e-4, `<=`比較、double計算、長さ0の辺は点扱い） | 妥当。標準的な点-線分距離式で、`lenSq<=0`のとき点として収束する実装になっている |
| 境界点0〜2個で例外にならない | `CalibrationView.Refresh`で`boundaryWorldPoints.Count>=3`を確認してから使用、`FitsBoundary`側も`poly.Count<3`で二重ガード。`boundaryLine`描画部も`boundaryAvailable`（count>=3保証済）を確認してから`[0]`参照 | 妥当。`[0]`への無条件アクセスは残っていない |
| 凹境界の辺交差判定 | `FitsBoundary`が4隅の内包チェックに加え、正方形4辺×境界n辺の総当たり交差判定を実施 | 妥当 |
| 境界外で決定時の警告 | `LoopDemo.CommitCalibration`が確定直前に`calibration.Refresh`を再実行し`Fit`を確定、`calibrationOutside`をOnGUIで次のCommitまで保持（`ResetAlignment`では意図的にリセットしない） | 妥当。コメント通りの動作 |
| C/Rと開始入力の同フレーム競合 | `operatorCommandConsumed`をUpdate冒頭で毎フレームfalseにし、C→R→開始判定の順で消費フラグを見て開始をブロック | 妥当 |
| キャリブレーション中の部屋非表示 | `roomHidden = calibrating && idle`（`CanStart`に依存しない）でトラッキング喪失時も部屋非表示を維持 | 妥当 |

ロジック上の明確な欠陥は見当たらなかった。

## 未確認事項（本スナップショットに含まれず判定できない）

- `DemoRig.cs`（`TryGetBoundaryPoints`, `LookAtFloorForCalibration`, `StartHeld`, `CanStart`, `CanRetryPreparation`等）の実装
- `PlayAreaSettings.cs` / `RoomAnchor.cs`（task020側、`areaSize`/`margin`/`reach`/`halfAngleDeg`, `ChooseFrontYaw`）の実装
- `RoomVisuals.cs`, `ProceduralAudio.cs`
- **テスト実行（batchmode 0/0）**: 未実施、本レビューでは実行していない
- 証拠画像（`--desktop --auto-calibrate --autostart`の枠表示・Player.log例外0件）
- 実機（Air Link）確認

## 判定: approve

（提示ソースの範囲内では追修正1〜5の意図した修正が正しく実装されている。ただし上記未確認事項、特にbatchmode 0/0の実測とDemoRig.cs側の実装確認は別途必要）