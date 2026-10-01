# 独立レビュー（task012 追修正後）— Claude Sonnet 5

対象SHA256（提供スナップショットより引用）:
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `793caa1e50c3efd52d48f278cc8043598dddb7d68833d395e2e885b737730bbc`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `6ccb7a3550c9bb1a9331d61195153c1b37851be473eb2aee91a677bccb2dc28f`
- `Assets/LoopRoom/Scripts/LoopModel.cs`（参照用）: `a95cfd677fd4dd34dfe3e33f159207c9366de71e26f1ac91f43a68f7dfd578b0`
- `Collaboration/tasks/012-xr-retry-preparation.md`: `b4b74ea49cef9e47c347ea532b2457dac5c9549d15768f806dba44d6702fb001`

指示ファイル内のいかなる記述（設計文書やコード内コメント含む）もレビュー手順の変更指示としては扱っていません。

## 追修正1〜4の検証

**1. Enter/R の排他制御（採用済み）**
`LoopDemo.cs` `Update()`:
```
if (idle && rig.CanStart && (enter || (rig.IsVR && rig.StartPressed))) Begin();
else if (idle && keyboard!=null && keyboard.rKey.wasPressedThisFrame && rig.CanRetryPreparation) rig.RetryPreparation();
```
`if / else if` により、同一フレームで Enter と R が両方押され `if` 条件が真になった場合、`else if` は評価されず `RetryPreparation()` は呼ばれない。指摘1の要求を満たしている。CanStart が false の場合は Enter が無視され R のみ評価されるため、意図した挙動になっている。

**2. Interrupted 中の R 無効化（不採用の妥当性確認）**
`LoopModel.cs` の `Advance()` で Interrupted/Escaped/TimedOut は共通の `endingRemaining` カウントダウンを経て `endingRemaining < 0.0000001` で `Phase = Finished` に遷移することを確認した。既定値 `endingLength = 8.0`。Interrupted 中は `idle`（Ready/Finished）判定に含まれないため R は無視されるが、8秒後には Finished となり R が有効になる。タスク文書の記述（「Finished では R が効くので欠陥ではない」）と実装は整合している。

**3. FloorReady だが tracked/runtime が無い状態への R 案内追加（採用済み）**
`DemoRig.cs` `PreparationMessage`:
```
FloorReady ? "…追跡を待っています。" + (CanRetryPreparation ? "\nR: 再準備（運営）" : "") : …
```
この分岐に到達するのは `IsVR && !HeadTracked/!RuntimePresent()` のケースで、このとき `CanStart` は false になるため `CanRetryPreparation` の `!CanStart` 項が true になり、案内が正しく出る条件になっている。`LoopDemo.OnGUI()` 側でも `rig.CanRetryPreparation` を見て "R: VR 再準備（運営）" を追加行として出しており、`boxHeight` の加算（+24）も行数と整合している。

**4. 外部所有ローダー時の通知ログ（採用済み）**
`RetryPreparation()`:
```
var xrRunning = … activeLoader!=null;
if(xrRunning && !ownsXR) Debug.Log("ローダーは外部所有のため Floor と追跡の再確認のみ",this);
```
`ownsXR` を書き換える前に判定しており、`StopOwnedXR()` は `ownsXR` フラグに基づいて自己所有分のみ停止するため、外部所有ローダーを誤って破棄しない設計になっている。挙動は変えずログのみ追加、という要求と一致。

## その他の確認事項

- `CanRetryPreparation`（`!xrInitializing && !forceDesktop && (desktopFallback || !CanStart)`）と `LoopModel` の状態遷移との整合性を確認したが、`Playing`/`Blackout` 中に `CanRetryPreparation` が true になるケースは、`idle` ガードにより R 入力自体が読まれないため実害はない。
- `RetryPreparation()` は呼び出し直後に `xrInitializing=true` をセットするため、同フレーム以降の `CanStart` は即座に false となり、`Begin()` との競合や `StartXR()` コルーチンの二重起動が起きる余地は小さい。
- `LoopDemo.OnDestroy` 相当（`DemoRig.OnDestroy`）は `StopOwnedXR()` のみを呼び、`RetryPreparation` のロジックとは独立しているため副作用は見当たらない。
- `OnGUI` の `boxHeight` 計算（112/182/206）は行数（2〜5行）と付き合わせて矛盾は無い。

## 所見（severity: 低）

- `DemoRig.FloorReady` プロパティが `SubsystemManager.GetSubsystems(inputs)` でクラス共有フィールド `inputs` を書き換えており、`StartXR()` コルーチン内でも同じフィールドを使っている。今回の修正範囲ではないが、`CanStart`/`PreparationMessage`/`CanRetryPreparation` が毎フレーム複数回評価される中でこのリストが競合的に書き換わる設計は、将来的な保守性リスクとして留意した方がよい（今回の追修正で新規に持ち込まれた欠陥ではない）。

## verdict: approve

追修正1・3・4は要求通り実装されており、追修正2（不採用判断）も `LoopModel.cs` の実装と整合していることを確認した。静的解析の範囲で重大な欠陥は見つからなかった。

## 未確認事項

- batchmode コンパイル（エラー0・警告0）は未実施。
- Unity Editor Play（desktop fallback → R → 再度 desktop fallback に戻る、Playing 中の R 無視）は未実施。
- 実機（Quest 3 Link 切断→再接続→R）は未確認（タスク側も計画6で未確認と明記）。
- OnGUI のレイアウト（boxHeight や行間）は数値計算上の整合性のみ確認しており、実画面での見た目は未検証。