# 独立レビュー（task022 — autostart時のフォーカス喪失非中断）

対象SHA256（提供値をそのまま引用）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `78c61e27e48c616e9ef04b2e2570961a0e9dcf494be78b09214af47a6de672ff`
- `Collaboration/tasks/022-autostart-focus.md`: `37e4d1479bc9df0078e5d5e7cd70658b0dd50bb0494d22233ea273ea273ed2d475b20`（参照用、レビュー対象コードではない）

※ `Collaboration/evidence/20260925-light-focus/` および `RoomVisuals.cs` の変更はスナップショットに本文が含まれていないため、本レビューの対象外（未確認）とする。

## 検証した要求とコード対応

- 中断条件 `if (!autostart && !rig.IsVR && !Application.isFocused && !Application.isEditor && !idle) Model.Interrupt();` （LoopDemo.cs）: `autostart` が真の場合は条件全体が偽になり、フォーカス喪失で `Interrupt()` されない。desktop以外・VR・idle中は元の分岐がそのまま生きており、要求どおり他挙動は不変。
- Esc（`keyboard.escapeKey.wasPressedThisFrame` → `Model.Interrupt()`）、追跡喪失（`rig.IsVR && (Playing || Blackout)` ブロック）、`--autoescape`（`autoescape && !rig.IsVR && ...`）は本タスクで一切変更されておらず、`autostart` フラグと独立に動作する。要求「これらの挙動を変えない」を満たす。
- ログ1行: `Start()` 冒頭で `if(autostart) Debug.Log("LoopRoom: --autostart is active; ignoring focus loss.");` を1回だけ出力。`Start()` はコンポーネントのライフサイクル上1回のみ呼ばれるため、受入条件の「ログ1行」を満たす。

## 指摘事項

**重大度: 中（要確認・未検証）** — `LoopDemo.cs` 内 `void OnApplicationPause(bool paused) { if(paused && Model!=null && !rig.IsVR) Model.Interrupt(); }`
今回の修正は `Update()` 内のフォーカス判定にのみ `!autostart` を追加しており、この `OnApplicationPause` の経路には反映されていない。desktop実行中に評価対象ウィンドウが「最小化」に相当する状態遷移を起こした場合（証拠撮影ツールの挙動によっては、単なる前面化ではなく最小化を伴う可能性がある）、Unityが `OnApplicationPause(true)` を発火させると、`autostart` 中でも `Model.Interrupt()` が呼ばれセッションが中断し得る。`Application.runInBackground = true` が設定されているためWindows Standaloneでは通常発火しないと考えられるが、これはビルド設定・OS挙動に依存し、コードからは断定できない。
修正案: この行にも `!autostart` を追加するか（`if(paused && Model!=null && !rig.IsVR && !autostart) Model.Interrupt();`）、実機で「最小化を伴う操作」でも中断しないことを確認してコメントで根拠を残す。

**その他は具体的欠陥なし** — 差分範囲がUpdate内の1条件追加とStart内のログ1行に限定されており、既存ロジック（PlaceRoom、CommitCalibration、calibration関連、trackingLost処理等）への波及は確認できない。

## 未確認事項（本文のみでは判定不可）

- batchmode 0/0、テスト全件PASSの実行結果
- 「`--desktop --auto-calibrate --autostart` 実行中に別ウィンドウを前面に出してもLOOP 02まで継続」の実機証拠（ログ1行含む）
- `OnApplicationPause` が対象の実行環境・撮影手順で実際に発火するかどうか
- `RoomVisuals.cs` の変更内容（本レビュー範囲外）

## 判定

**request_changes**（`OnApplicationPause` 経路の未対応をリスクとして明記。修正または実機確認による安全確認のいずれかを求める。Update内の主要修正自体には具体的な欠陥は見当たらない）