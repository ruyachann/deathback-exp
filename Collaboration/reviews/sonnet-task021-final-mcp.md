# task021 独立レビュー（最終版）

対象SHA256（ツール不使用・本文照合のみ）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `66bbc36959c120c5413e1001056ae3062543ce75b7421eafe1ea74cbb07dd8e0`
- `Assets/LoopRoom/Scripts/CalibrationView.cs`: `39fa9f8161fa0afb9d028625755659f5e72d6dda2362a7206e905f031f9e783a`
- `Collaboration/tasks/021-calibration-mode.md`: `1dedeb2e04dcc9178b6b3906dd8bade76692a8d322bbd3fb67afd4dce22e5455`

## 追修正4（3点）の検証結果

**4-1（epsilon/double）: 満たしている**
`CalibrationView.cs` の `SegmentsIntersect`/`Cross`/`Sign`/`OnSegment` は `const double Epsilon = 1e-4` を用い、`Cross` は `double` 変数で乗減算している（float入力→double昇格ではあるが、丸め誤差低減という文意は満たす）。`Sign` は `d>Epsilon?1:d<-Epsilon?-1:0` で接触/境界上を確実に0（=交差扱い＝Outside側）に倒しており、要求どおり「誤差内はすべて交差扱い」になっている。

**4-2（C/RをBegin判定より前に処理、operatorCommandConsumedで開始を止める）: 満たしている**
`LoopDemo.Update()` で、フレーム冒頭に `operatorCommandConsumed=false` → C分岐（`if`）→ R分岐（`else if`、`!operatorCommandConsumed` 条件付き）→ …（キャリブレーション長押し/`--auto-calibrate` 経路でも `CommitCalibration()` 内で `operatorCommandConsumed=true` を設定）→ 最後にBegin判定 `if (idle && !calibrating && !operatorCommandConsumed && ...)` という順で並んでおり、同フレームのC/RとEnter/A-X/autoTriggerの競合はブロックされる。

**4-3（calibrationOutsideはCommitCalibrationのみで更新）: 満たしている**
`ResetAlignment()` は `aligned/alignCx/alignCz/alignAreaYaw/calibrating/calibrationHold` のみリセットし、`calibrationOutside` には触れていない。更新箇所は `CommitCalibration()` 内の1箇所のみ。

## 新規に確認した欠陥

**[中] `CalibrationView.cs` の `Refresh()` — 境界点リストが空のとき `IndexOutOfRangeException` の可能性**

該当箇所（`Refresh` メソッド末尾）:
```csharp
if(boundaryAvailable)
{
    int count=boundaryWorldPoints.Count;
    boundaryLine.positionCount=count+1;
    for(int i=0;i<count;i++) boundaryLine.SetPosition(i,boundaryWorldPoints[i]+Vector3.up*BoundaryHeight);
    boundaryLine.SetPosition(count,boundaryWorldPoints[0]+Vector3.up*BoundaryHeight);
}
```
- **トリガー**: `boundaryAvailable=true` かつ `boundaryWorldPoints.Count==0`（`rig.TryGetBoundaryPoints` が真を返しつつ空リストを返すケース）。`count=0` のとき `for` は実行されず、直後の `boundaryLine.SetPosition(count, boundaryWorldPoints[0] ...)` が `boundaryWorldPoints[0]` にアクセスして例外を投げる。
- **根拠**: `FitsBoundary()` 側には `if(poly.Count<3) return false;` という安全策があるのに、同じ `Refresh()` 内の描画コードには対応するガードがなく非対称。`DemoRig.cs`（`TryGetBoundaryPoints` の実装）は本レビューのスナップショットに含まれておらず、trueかつ空配列を返す挙動が実際に起き得るかは未確認だが、防御していない点自体が欠陥。
- **影響**: 発生すればUpdateループ内で例外→受入条件2「Player.log の例外0」に抵触しうる。
- **修正案**: `boundaryAvailable = boundaryAvailable && boundaryWorldPoints.Count>=3` のように描画前にもガードするか、`if(boundaryAvailable && count>0)` で分岐する。

**[低] `PointInPolygon`（`FitsBoundary` 内）は epsilon 保護なし**
追修正4-1が対象とした「向き判定・接触判定」（`SegmentsIntersect`系）には epsilon が入ったが、コーナー内包判定 `PointInPolygon` は float のままレイキャスト判定のみで epsilon 未適用。頂点がちょうど境界線上に乗るケースで内外判定が不安定化しうるが、後続の辺交差判定（epsilon付き）でOutside側に捕捉される可能性が高く、実害は限定的と判断する。

**[軽微] `CommitCalibration()` 内 `operatorCommandConsumed=true` の重複設定**
Cキー分岐から呼ばれた場合、呼び出し元で既に`true`が立っているため冗長だが実害なし。

## batchmode 0/0・自動実行証拠・実機受入

スナップショットにテスト実行結果・Player.log・スクリーンショットは含まれておらず、本文のみでは検証不可能。**未確認**として扱う。

## 未確認事項一覧

- batchmode テスト実行結果（0/0、全件PASS）
- `--desktop --auto-calibrate --autostart` の自動実行証拠画像・Player.log
- `DemoRig.cs`（`LookAtFloorForCalibration` の60°実装、`TryGetBoundaryPoints` の座標系・空リスト返却有無、`CanStart`/`StartHeld`/`StartPressed`/`RetryPreparation` のロジック）の内容
- `RoomAnchor.cs`／`PlayAreaSettings.cs`（task020側、本レビュー対象外）とのAPI整合性の実行時検証
- 実機（Air Link）での枠表示・境界比較・長押し決定の確認

## 判定

追修正4の3点（epsilon/double化、C/R処理順、calibrationOutsideの保持）はいずれもソース上で正しく実装されていることを確認した。一方で、`CalibrationView.Refresh()` の境界線描画に空リスト時の未ガード配列アクセスという新規欠陥（中）を検出し、これは受入条件「Player.log の例外0」に抵触しうるため見送れない。

**判定: request_changes**