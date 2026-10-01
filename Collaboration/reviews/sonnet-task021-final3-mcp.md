# 独立レビュー（task021 追修正6 後の最終スナップショット）

対象SHA256:
- `Collaboration/tasks/021-calibration-mode.md` = `3c547b13bb6353d6381fc185be13774d49892e8f16f1dbc6f898eadecc9eff1e`
- `Assets/LoopRoom/Scripts/LoopDemo.cs` = `f564af51053bfc954de987078f97829c0b00595951daf6ba207502e3ee1ae787`
- `Assets/LoopRoom/Scripts/CalibrationView.cs` = `43da0c7f41853f277eee6fde8a965acf2ff85ce5668047b54bddb39518dee9b1`

## 前回Solの指摘（境界点3個未満で運営表示が出ない）の検証

`LoopDemo.cs` 内で `TryGetBoundaryPoints` を呼んでいるのは2箇所のみで、両方とも確認できました。

- `CommitCalibration()`（207行目付近）: `lastBoundaryAvailable = rig.TryGetBoundaryPoints(boundaryPoints) && boundaryPoints.Count >= 3;`
- `RefreshWorld()`（277行目付近）: `lastBoundaryAvailable=rig.TryGetBoundaryPoints(boundaryPoints) && boundaryPoints.Count>=3;`

両者とも `Count>=3` で正規化済みで、`OnGUI()` の `showBoundaryHint = (!rig.IsVR || privateOverlay) && calibrationVisible && !lastBoundaryAvailable;` が `lastBoundaryAvailable` を直接参照しているため、境界点0〜2個のケースでも「境界情報なし（目視で確認）」が表示されます。追修正6-1の指摘は解消されています。

## 追修正1〜6の各項目の再確認

- 追修正3-1（赤で決定しても警告なし）: `CalibrationView.Fit` を公開し、`CommitCalibration()` が `calibrationOutside` をセットして `Debug.LogWarning` を出し、`OnGUI` の `showOutsideWarning` が次のcommitまで残る（`ResetAlignment()`は`calibrationOutside`に触れない）。実装確認できました。
- 追修正3-2（凹形境界の辺交差）: `CalibrationView.FitsBoundary` が四隅の内包判定に加えて正方形4辺×境界全辺の交差判定を行っています。確認できました。
- 追修正3-3（追跡不能で部屋再表示）: `roomHidden = calibrating && idle;` が `rig.CanStart` を条件に含めていないため、追跡不能で枠(`calibrationVisible`)が消えても部屋は隠れたままです。意図通りです。
- 追修正3-4/4-2（C・R・開始入力の同一フレーム競合）: `operatorCommandConsumed` を毎フレーム冒頭でリセットし、C→Rの順で開始判定より前に評価、消費フラグがあれば `Begin()` をスキップする構造になっています。確認できました。
- 追修正5-1（epsilon判定）: `PointSegmentDistance`（double）による点・線分間の実距離判定に置き換わっており、以前の「cross積に直接epsilonを当てる」問題は解消されています。
- 追修正5-2/6-1（境界点0〜2個の例外・警告漏れ）: 上記のとおり解消済み。

## 軽微な指摘（severity: 低、機能への影響なし）

1. `CalibrationView.Refresh()` 内の `boundaryAvailable = boundaryAvailable && boundaryWorldPoints.Count>=3;`（LoopDemo呼び出し側で既に`Count>=3`を確認済みの値を渡しているため常に冗長）。バグではありませんが、今後別の呼び出し元が増えた際の安全策として残しておく分には問題ありません。
2. `LoopDemo.cs` の `OnGUI()` の `boxHeight` 計算（`showOutsideWarning`/`showBoundaryHint`で+24ずつ加算）は、実際のラベル行の積み上げ（`y+=24`の回数）とほぼ一致していますが、厳密なpixel単位の検証はコードだけでは断定できません。見た目のみの問題で機能には影響しません。

## 未確認事項（コード上判断できない範囲）

- `DemoRig.cs`（`TryGetBoundaryPoints`, `LookAtFloorForCalibration`, `StartHeld` 等の実装）は本スナップショットに含まれておらず、`boundaryPoints`リストが呼び出しごとにClearされているか等は確認できません。
- Unity Editor/実機でのコンパイル、batchmodeテスト、`--desktop --auto-calibrate --autostart` の実行、境界線・扇形・枠の視認性は未実施（ツール不使用のため）。
- `BuildSector()` のCullオフ処理はコード内コメントで「Editorなしでは確認不可」と自己申告されており、未検証のままです。

## 判定

**approve**

前回Solが指摘した「境界点3個未満で運営表示が出ない」問題はコード上解消されており、追修正1〜6の他の指摘もすべてコードに反映され、新たな重大な回帰は見当たりません。ただし上記の未確認事項（特にUnity実機/Editorでの動作確認）は引き続き必要です。