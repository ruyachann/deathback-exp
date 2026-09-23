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

## 非対象

B-4 運営再準備、A-1 壁時計上限、B-5 机位置、B-6 Rules 複製、B-7 Touch Plus プロファイルは別タスク。
