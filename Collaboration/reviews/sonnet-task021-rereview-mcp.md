# task021 独立レビュー（追修正3の1〜4フォーカス）

対象SHA256（snapshotより引用）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs` — `613fc54d3e786299ecd69f09040980348e6e1d9710a4323905a6d7d149b3ace5`
- `Assets/LoopRoom/Scripts/CalibrationView.cs` — `c1edb18c98e2c2bde5308fa8f097ff94d66caeb566e9f9fdb73cf81ddcb02ce5`
- `Collaboration/tasks/021-calibration-mode.md` — `2f9d9c9a1eadbfb90769924874ed10eb404e25703e3b804f1eeb2966bb53a3c5`

## 追修正3-1（境界外で決定→警告）

`CommitCalibration()` が毎回 `rig.TryGetBoundaryPoints` → `calibration.Refresh` を呼び、決定瞬間の頭位置/yawで `Fit` を再評価してから `calibrationOutside` を確定している（`RefreshWorld` 時点の古い `Fit` を使い回していない）。`Debug.LogWarning` も出る。OnGUIの `showOutsideWarning` は `calibrationOutside` を参照し、次の `CommitCalibration()`/`ResetAlignment()` まで表示が残る。要求どおり実装されている。問題なし。

## 追修正3-2（凹形境界の辺交差判定）

`CalibrationView.FitsBoundary` は四隅の内包チェックに加え、四角の4辺と境界ポリゴンの全辺を `SegmentsIntersect` で総当たりし、交差・接触ともOutside扱いにしている。ロジック（標準的な向き判定＋共線接触の特別扱い）自体は妥当に見える。単体テストは走らせていないため境界値（`OnSegment` の `1e-5f` イプシロン等）の実挙動は未検証。

## 追修正3-3（`calibrating && idle` で部屋を隠す）

```
bool roomHidden = calibrating && idle;
...
calibrationVisible = calibrating && idle && rig.CanStart;
```
部屋の非表示条件は `CanStart` を含まない `calibrating && idle` で、枠の表示条件のみ `CanStart` が付いている。要求どおり。問題なし。

## 追修正3-4（Cを処理したフレームは開始判定をしない）

C起因（`cKey.wasPressedThisFrame`）と、1秒長押し／`--auto-calibrate` 起因（いずれも `CommitCalibration()` 内で `operatorCommandConsumed=true`）は、いずれも開始判定のif文より**前**のブロックで評価されるため、`!operatorCommandConsumed` により正しく同フレーム開始を抑止できている。ここは良い。

一方Rキー処理はコード順序が逆:
```csharp
if (idle && !calibrating && !operatorCommandConsumed && rig.CanStart && (enter || (rig.IsVR && rig.StartPressed) || autoTrigger)) { ...; Begin(); }
else if (idle && keyboard!=null && keyboard.rKey.wasPressedThisFrame && rig.CanRetryPreparation) { rig.RetryPreparation(); ResetAlignment(); operatorCommandConsumed = true; }
```
- **[中] LoopDemo.cs（開始判定の直後の `else if (... rKey ...)` ブロック）**: `operatorCommandConsumed` を参照する唯一の箇所（開始判定のif文）はこの `else if` より前に既に評価済みのため、R処理内で `operatorCommandConsumed = true` をセットしても同フレーム内で何の効果も持たない、実質no-opになっている。さらに、同一フレームで `enter`/`rig.StartPressed`/`autoTrigger` のいずれかとRキーが同時に真であった場合、開始判定側が先に評価されて `Begin()` が実行され、`else if` 自体が評価されずRの処理（`RetryPreparation`/`ResetAlignment`）がまるごとスキップされてしまう。追修正3-4の意図（CまたはRを処理したフレームでは開始しない）と逆方向の抜け穴。
  - **トリガー**: RキーとEnter（またはVRのStartPressed、あるいはdesktop `--autostart` のautoTrigger）が同一フレームで真になった場合。運用上は稀（Rはオペレータの手動キー、autoTriggerは自動シナリオ用）だが、要求仕様を厳密には満たしていない。
  - **修正案**: R判定を開始判定より前に移し、Cと同様に`operatorCommandConsumed`をセットしてから開始判定側で`!operatorCommandConsumed`により抑止する形に順序を入れ替える。

- **[要確認・中]** 1秒長押し（A/X）で `CommitCalibration()` が実行された直後、まだ物理ボタンを押し続けている場合に次フレームで `rig.StartPressed` がtrueのままだと、`calibrating=false` かつ `operatorCommandConsumed` は毎フレーム先頭でリセットされるため、指を離す前に `Begin()` が即座に呼ばれてしまう可能性がある。これは `DemoRig.StartPressed`（`wasPressedThisFrame` 相当の一瞬だけtrueなのか、押している間ずっとtrueなのか）の実装に依存するが、`DemoRig.cs` がSNAPSHOTに含まれておらず判定できない。もし後者であれば、追修正3-4の「決定操作と開始操作の分離」という趣旨に反する抜け穴になる。

## その他の気づき（フォーカス外・軽微）

- **[低] CalibrationView.FitsBoundary**: `poly.Count<3` の場合 `return false` となり `Fit=Outside`（赤）になる。`boundaryAvailable=true` だが点数が異常（0〜2点）というケースでは、本来「判定不能」に近いはずが赤表示になる。安全側ではあるが、`showBoundaryHint`（`!lastBoundaryAvailable` で「境界情報なし」表示）とは別経路のため、境界データが壊れている場合のUI整合性はやや不明瞭。

## 未確認事項（ツール不使用・SNAPSHOT外のため）

- `DemoRig.cs`：`CanStart`／`StartHeld`／`StartPressed`／`TryGetBoundaryPoints`／`LookAtFloorForCalibration`／`PreparationMessage`／`CanRetryPreparation`／`RetryPreparation` の実装未確認（特に `StartPressed` の意味論、上記「長押し直後の即開始」懸念の裏付けに必須）。
- `RoomAnchor.cs`／`PlayAreaSettings.cs`（task020担当作成中、AGENTS.md記載どおり本レビューでは未確認）。
- `RoomVisuals.cs`：`PrivateLayer`／`Material`／`TextMaterial` の実装未確認。
- `Assets/LoopRoom/Scripts/PlayAreaSettingsFile.cs`（git statusで変更ありだがSNAPSHOTに含まれず未確認）。
- コンパイル・テスト（batchmode 0/0）・実機/Editor実行・スクリーンショット証拠はいずれも未実施（ツール不使用の書面レビューのため）。

## 判定: **request_changes**

理由: 追修正3-4は「Cを起因とする同フレーム開始抑止」は正しく実装されているが、「Rを起因とする同フレーム開始抑止」はコード順序の誤りにより実質機能しておらず（no-op）、要求を完全には満たしていない。加えて、1秒長押し決定直後の即時開始可能性が `DemoRig.StartPressed` の意味論次第で懸念として残る（未確認のため断定不可だが、要求の趣旨に関わるため確認・修正を推奨）。他の追修正3-1〜3-3は書面上問題なし。