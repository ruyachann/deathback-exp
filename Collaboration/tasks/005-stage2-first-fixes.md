# 005 — 段階2の最初の修正（B-2 / B-3 / B-1）

状態: 2026-09-19、ユーザーの「作業を始めて」指示に基づき Claude Fable 5.1 が実装（CLAUDE.md の指定 claude-sonnet-5 ではない。役割の扱いはユーザー判断）。根拠は Collaboration/reviews/fable-plan-and-code-review-20260918.md の Part D 手順1。Sol/Sonnet への新規呼出しは行っていない。

## 目的

Quest 3 実機受入（STAGE_PLAN 段階2）の前に、ソースから確定できた3件を直す。

| 指摘 | 内容 | 修正 |
| --- | --- | --- |
| B-2（高） | ビルド版で VR セッション中に運営者が別ウィンドウへフォーカスを移すと `Application.isFocused` 判定で Interrupted になる | `LoopDemo.Update` の条件に `!rig.IsVR` を追加。VR 中の中断は Esc と追跡喪失のみ |
| B-3（高） | 3秒の足音（Latch）は `enemyAudio` が Enemy 上にあり、Enemy が `t>=3` で有効化される前に `PlayOneShot` が走るため無音 | AudioSource を Root 直下の常時アクティブな `Enemy audio` オブジェクトへ移し、`RefreshWorld` で Enemy の位置 +1.4m に追従させる |
| B-1（高） | URP は `stereoTargetEye` を無視するため、観客カメラ（depth 10）が HMD にも描画されうる | `RoomVisuals.BuildSpectator` で `GetUniversalAdditionalCameraData().allowXRRendering=false` |

## 許可範囲

`Assets/LoopRoom/Scripts/LoopDemo.cs`、`Assets/LoopRoom/Scripts/RoomVisuals.cs` のみ。LoopModel、DemoRig、Editor スクリプト、依存、設定は変更しない。

## 受入条件

1. Unity 統合コンパイルでエラー 0（警告は task004 と同じ4件を上限に記録）。
2. Editor Play（HMD なし・desktop モード）で `Can not play a disabled audio source` 警告が出ず、3秒で Latch が鳴る。
3. 実機: VR 中に PC 側で別ウィンドウをクリックしてもセッションが続く。HMD 内に俯瞰視点が混入しない。PC ウィンドウの観客表示が従来どおり出る。
4. 同じ SHA256 の Sol/Sonnet 独立レビュー（ユーザーが Fable 実装を片側と扱わない場合）。

## 追修正（2026-09-23、相互レビュー指摘への対応）

根拠: `reviews/task005-exchange-20260923.md`（Sol request_changes を採用）。実装は Claude Opus 5.5（`claude-opus-5-5`）。ユーザーの「1から進めて」指示による。CLAUDE.md の実装優先モデル（Sonnet 5）ではない。再レビューは Sonnet 5 と Sol の双方に依頼する。許可範囲は変えない（`LoopDemo.cs` のみ変更）。

| 指摘 | 修正 |
| --- | --- |
| R-1（高）`OnApplicationPause` が VR 中も Interrupt | `!rig.IsVR` のときだけ中断する。VR 中は Esc と追跡喪失（0.3秒）の2経路のみ |
| R-2（中）長いフレームで t=3 と射撃をまとめて越えると Latch が鳴らない | Latch の判定を `Phase==Playing` の外に出し、`Playing` または `Blackout` で同じ周回の t=3 通過を検出する（LoopModel は Blackout 中に LoopTime を進めない）。射撃音より先に鳴る順序は維持する |

追加受入: 変更後の SHA256 で Sonnet/Sol が再レビューする。batchmode コンパイルでエラー 0。

結果（2026-09-23）: LoopDemo.cs `9d6925b4e3dcd36edeb2d338f1f39bb9c479bebf0a2d2bfb4caa1380d0e39097`。batchmode でエラー0・警告0。Sonnet/Sol とも交換後に approve（`reviews/task005-rereview-exchange-20260923.md`）。周回をまたぐ長フレームで Latch が鳴らない件は既知の制限。**残り: 受入条件2の可聴確認、受入条件3（Quest3 実機）。**

## 非対象

B-4 運営再準備、A-1 壁時計上限、B-5 机位置、B-6 Rules 複製、B-7 Touch Plus プロファイルは別タスク。
