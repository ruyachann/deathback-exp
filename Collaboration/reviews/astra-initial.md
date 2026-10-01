# Astra 初期設計・実APIレビュー

レビュー時刻: 2026-09-17 00:14 JST。担当: GPT-6 Astra の独立レビューエージェント。

対象: Unity 6000.3.15f1 (c1aa84e375f6)、Quest 3 PC接続、2m×2m、通常体験3分以内。周回中のXR原点移動禁止。Assets/LoopRoom の6 C#を全件読み、インストール済みPackageCacheの実ソースと照合した。アプリのコード・設定・PackageCacheは変更していない。ユーザーが開いているUnityとは別にEditorを起動していない。

## 判定と検証範囲

**現版はコンパイル阻害あり。PCVR動作承認には未到達。** 下記T1で依存パッケージのエラーを先に解消し、T2でXR初期化順序を修正する。パッケージAPIが存在することと、プロジェクト全体のコンパイル成功は別の判定。

- 実ログ確認済み: 現プロジェクトを開いた通常の `%LOCALAPPDATA%/Unity/Editor/Editor.log` に InputSystem のCS0117がある。ログのプロジェクトパスとEditor版も照合した。
- 実行済み: PowerShell `Add-Type -Path` で現在の `LoopModel.cs` と `LoopModelChecks.cs` を独立コンパイルし、既存10チェックを実行。全件PASS。Unity API・Mono・描画・HMD入力の検証ではない。
- 実行済み: `firstShot=double.NaN` を与える追加のメモリ上の試験で、不正設定が通り `Advance(1)` 後のTotalTime/LoopTimeがNaNになることを確認。テストファイルは変更していない。
- 静的レビュー済み: 残り4ファイル、XR起動・選択・原点・時刻・表示制御、実パッケージAPI。
- 未実行: Unity全体コンパイル、Play、メニューPrepare/Configure/Validate、Windowsビルド、Quest 3実機、観客画面、2m×2mの到達範囲、フレーム時間。

独立モデル試験のコマンド・観測結果・対象SHA256は `Collaboration/reviews/astra-model-checks-20260917.txt` に保存。限定実装タスクは `Collaboration/tasks/002-xr-startup.md` と `003-finite-timings.md`。役割設定・送信補助のレビューは `Collaboration/reviews/astra-workflow-initial.md`。

## 優先指摘とSonnet向け最小タスク

### T1 / P1 / 観測済み: InputSystem 1.12.0がEditorコンパイルを止めている

対象: `Packages/manifest.json` の `com.unity.inputsystem`。ログ2330/2335/2337行は以下の同一エラー。

```text
Library\PackageCache\com.unity.inputsystem@920b46832575\InputSystem\Editor\InputSystemPluginControl.cs(47,25): error CS0117: 'BuildTarget' does not contain a definition for 'ReservedCFE'
```

キャッシュ実ソース47行にも `BuildTarget.ReservedCFE` がある。Unity 6000.3.15f1と旧InputSystemの組合せによる実際のコンパイル障害で、6 C#の構文テストだけでは検出できない。

最小変更: 主担当が公式根拠を確認した6000.3互換のInputSystem版へmanifestの1項目を更新し、起動済みEditorのPackage Managerに解決させる。PackageCacheを直接編集しない。他のXR/URP版を同時に変更しない。`packages-lock.json` の変更はPackage Manager生成結果を記録する。

版選定の注意: このレビューで取得した[Unity 6000.3.0f1公式リリースノート](https://unity.com/releases/editor/whats-new/6000.3.0f1)の「New ... Package Changes」はInputSystem **1.15.0→1.16.0** と記載していた。1.17.0を選ぶ場合は別の公式互換資料/変更履歴も確認する。1.17.0の実ソースは本レビュー時点でローカル未導入のため未照合。

追記: 主担当は[InputSystem 1.17公式変更履歴](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/changelog/CHANGELOG.html)を確認し、タスク001の候補を1.17.0に限定した。現版1.12.0の既知コンパイル障害を解く最小依存更新案として妥当。ただし1.17の取得・全体コンパイル・XR実入力は引き続き受入条件であり、リリースノートだけで合格にしない。

受入条件: manifestと実解決版が一致し、新しいコンパイル周期のログ/ConsoleでCS0117が消える。他の赤いコンパイルエラーも0件。古いログ行が残っているだけか、新しい失敗かを時刻・コンパイル周期で区別する。LoopRoomメニューが出る。成功した6 C#のSHA256と解決版を後続レビューに記録。

### T2 / P1 / 実APIに基づく起動不具合候補: XR開始とFloor原点の確定が競合する

対象: `DemoRig.cs:56,83,193–203`、`LoopDemo.cs:38–44`。

`LoopDemo.Start()` → `rig.Initialize()` → `StartCoroutine(StartXR())` の経路で、最初の明示的なフレーム待機なしに `InitializeLoader()` に進む。導入済みXR Management 4.5.4の `Runtime/XRManagerSettings.cs:206–207` は、手動初期化ではStart完了前にこのAPIを呼べない旨を明記している。Coroutineの最初のyieldまでの実行に頼らず、明示的にStart完了後へ送る必要がある。

同時に、XROriginのFloor要求はloader作成前に設定される。Core Utils 2.6.0 `Runtime/XROrigin.cs:275–305` は、入力Subsystemが0件でも `SetupCamera()` がtrueを返す。その後にSubsystemができても、自動再試行を保証しない。`XROrigin.Start()` は1回だけ初期化を試みるため、loaderが遅延した経路ではFloor設定とtrackingOriginUpdated購読を逃す可能性がある。現在のコードはXR起動完了後にFloorを再適用・確認せず、HMD追跡だけで開始可能になる。実機で床基準・高さが不正になるリスクで、再現の実機検証は未実施。

最小変更: DemoRigだけを中心に、(1) XR初期化前に明示的な1フレーム待機、(2) 起動済み入力Subsystemの取得後に、準備状態でFloor要求を適用し実際のorigin modeを確認、(3) Floor/追跡の準備完了までVR開始を許可しない、の順序を作る。遅延起動でも確認できるようXROrigin参照を保持する。周回開始・死亡復帰では原点/offset/再センタリングAPIを操作しない。Floor要求を毎フレーム無条件に書き戻す修正は禁止。

受入条件: 正常起動と意図的な1フレーム以上の遅延起動の双方で、開始許可より前にFloorが確認される。Quest 3の実床と仮想床、HMDと左右コントローラーの高さが一致する。死亡前後でXR OriginとTracking Offsetの姿勢にアプリ由来の変化がない。3回のPlay開始/停止でSubsystemの二重開始・未解放によるエラーがない。`--desktop` の既存操作が保たれる。

### T3 / P2 / 独立実行で確認済み: 不正なtimingsが状態モデルを壊す

対象: `LoopModel.cs:18–23`。

Validateは大小比較だけなのでNaNを拒否できない。`new LoopRules { firstShot = double.NaN }` は受理され、開始後 `Advance(1)` でTotalTimeとLoopTimeがNaNになる。以後、通常終了時刻を保証できない。現行デフォルト値では起こらないが、timingsを公開して調整する設計なので境界検査が不足する。

最小変更: 全7 timing値のNaN/Infinityを最初に拒否する。既存の順序制約は保持する。LoopModelChecksに各timingのNaN/正負Infinity拒否を確認する1項目を追加する。

受入条件: 既存10項目PASS、不正値拒否チェックPASS。正常なデフォルトで無操作のTotalTime=180を維持。Unity Inspectorから通常の有効値を設定した動作を変えない。

## 設計上の確認事項（今回の即時修正と分離）

- 周回原点: `LoopDemo.Begin()` はセッション開始時だけRoomを頭の位置/向きへ合わせ、死亡後は状態を戻す。現版に周回ごとのXR Origin移動呼出しは見つからなかった。この方針は維持する。パッケージ自身のFloor/Device設定やユーザーによるランタイム再センターの影響は別途実機確認が必要。
- 3分: 純粋モデルの通常進行は172秒+終了8秒=180秒で10チェックもPASS。一方 `LoopDemo.cs:86–93` は0.3秒以内の追跡喪失中にModel.Advanceを呼ばず、短い喪失が反復すると壁時計で180秒を超え得る。READMEは安全中断を通常進行と分けている。3分を展示の厳密な壁時計上限にする場合は、ゲーム内時計とは別にセッションdeadlineを設計する。凍結したLoopTimeへ失われた時間を一括加算して即死させない。
- 2m×2m:操作位置は開始点基準 x=±0.32m,z=0.36m、操作半径0.16mなので大きな移動を要求しない。ただしy=1.01m固定で、座位・身長差を吸収する調整は未実装。身体に合わせた到達範囲は実機試験で決める。仮想室5m幅をそのまま物理移動範囲と扱わない。
- 暗転・観客表示: 0.16秒暗転が低FPSでも知覚できるか、URP+OpenXRのHMD/PC表示先、両眼の文字/黒Quad、観客側への私用表示漏れは静的API照合だけでは承認できない。コンパイル修正後の実機確認項目にする。

## 導入済みAPI照合

| 使用箇所/API | 実ソースで確認した状態 |
| --- | --- |
| XRI 3.3.2 `XRDirectInteractor`, `XRSimpleInteractable` | Interactors/Interactables名前空間は現コードと一致 |
| `StartManualInteraction(IXRSelectInteractable)` / `isPerformingManualInteraction` / `EndManualInteraction` | XRBaseInteractor.cs:1020/286/1045に存在。現コードは廃止されたXRBaseInteractable引数を回避している |
| `XRInteractionManager` | UnityEngine.XR.Interaction.Toolkit名前空間に存在 |
| Input System 1.12 `TrackedPoseDriver.positionInput`, `rotationInput`, `trackingStateInput` | Plugins/XR/TrackedPoseDriver.csに存在。更新先版は更新後に再照合 |
| `OpenXRSettings.GetSettingsForBuildTargetGroup` | OpenXR 1.16.1 Runtime/Settings/OpenXRSettings.cs:68に存在（Editor条件下） |
| `OpenXRSettings.RenderMode.SinglePassInstanced` | Runtime/Settings/OpenXRRenderSettings.csに存在 |
| `XRGeneralSettingsPerBuildTarget.SettingsForBuildTarget`, SetSettingsForBuildTarget, XRGeneralSettingsForBuildTarget | XR Management 4.5.4 Editorソースに存在 |
| `XRGeneralSettings.InitManagerOnStart` | XR Management 4.5.4 Runtimeソースでsetterを確認 |

これらの主要APIに明白な欠落は見つからないが、現時点の全体コンパイル合格を意味しない。

## レビュー対象版 SHA256

以下は変更前の本レビュー対象。パスはプロジェクトルート相対。

```text
Assets/LoopRoom/Editor/DemoSetup.cs
B8A42CB4698BC3BEA7773832ED2BD9BE1B52FDCE3F0EEB222410109EC04873FB
Assets/LoopRoom/Editor/LoopModelChecks.cs
943045A198C37B5D9AA4B365BADD9FB8F0C010ECAD9686271715B105A2B4AAD0
Assets/LoopRoom/Scripts/DemoRig.cs
EA1AD7ED591ED9EF2DB772E8152C6C94DD8FE687D0450FE0D8A7C9CE0E27A837
Assets/LoopRoom/Scripts/LoopDemo.cs
1B04F7CAAD9E5AE7D711FE5003CF8FE5A0D6C3D61C04F6E3E5E0721CE745761E
Assets/LoopRoom/Scripts/LoopModel.cs
741043710EF81DA2120EAE08AE9F8F4D1DC98D4CBF62787B3127DDDC2ACE3224
Assets/LoopRoom/Scripts/RoomVisuals.cs
A9CB72A4E78A2B83F76A62B15434B93F433CAB975BAC29B6A475A655153D332F
Packages/manifest.json
A984627E25BAD0B69F9EB184BAC1EC5134B60F46D3D501D6C89708E2BF60CE62
```

解決版: InputSystem 1.12.0 / OpenXR 1.16.1 / XRI 3.3.2 / XR Management 4.5.4 / Core Utils 2.6.0 / URP 17.3.0。
