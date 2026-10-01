# 002 — Sol実装報告

担当: `sol_timing_implementation`。本報告は実装者の報告であり、独立レビューではない。

検証日時: 2026-09-17T01:03:09.6592569Z（JST 2026-09-17 10:03）。状態は実装完了・独立レビューとUnity内/実機検証待ち。

## 実装内容

- `StartXR()` の最初に `yield return null` を置き、InputAction生成・有効化と `LoopDemo.Start()` 完了後にloader初期化へ進む。
- `XROrigin` をフィールドで保持し、準備前は無効にする。XROriginの自動Start処理だけにFloor要求を任せず、running XRInputSubsystemが存在し、対応モードが既知でFloor対応であることを調べ、準備処理内で各running inputへFloorを1回要求する。
- 要求後に実際の `GetTrackingOriginMode()==Floor` を確認してから、保持したXROriginへFloor要求を設定して有効化する。この時点を `floorPrepared` とし、開始時には保存済みinputがrunningかつFloorであることと、その時点の全running inputもFloorであることを再確認する。
- HMD追跡は `isTracked` に加え、同じTrackedPoseDriverの `trackingStateInput` にPositionとRotationの両フラグが立っていることを要求する。いずれかを失った場合は既存のtrackingLost判定もこの厳しい条件を用いる。
- `CanStart` で初期化中の全開始を止める。VRではFloor実確認・HMD位置/回転追跡・running displayを要求し、EnterとA/Xの両方を同じゲートへ通す。`Begin()` 自体にも同じガードを置いた。
- 準備中、床準備済みで頭追跡待ち、未準備、開始可能の表示を分ける。Desktop開始可能時のEnter/Space/Eは維持する。
- running display/input、対応モードの確定、Floor確認、頭追跡の待機は、loader初期化後から実時間10秒を上限としてフレームをyieldする。未対応、Floor拒否、期限切れは1回だけ警告する。毎フレームの警告やTryRecenterは追加していない。

## Desktop fallbackと所有権

`--desktop` はXR準備を開始せず、従来どおりEnterで開始できる。XR settings/Managerがない場合、または `InitializeLoader()` が完了してもactiveLoaderがない場合のみ、非VRのDesktop fallback開始を許可する。初期化がpendingの間にはDesktopセッションを開始できない。

activeLoaderが得られた後でdisplay/inputがrunningにならない場合は、Desktopへ自動開始許可せず未準備のままとする。この判断はAstraが承認した範囲に従う。loaderを持つがHMDが未接続のEditor状態では準備未完になる可能性があり、明示的Desktop経路と実際のHMDなしloader失敗経路を別々に確認する必要がある。

既存activeLoaderを検出した経路ではStartSubsystemsを呼ばず `ownsXR` を立てない。自分でloaderを初期化してStartSubsystemsを呼んだ経路のみ既存どおり `ownsXR=true` とし、OnDestroyのStopSubsystems/DeinitializeLoaderも従来の所有権条件を維持した。

Floor要求はStartXRの準備処理だけに置き、PollMode、BeginLoop、死亡、Playing、Blackoutへ原点設定を追加していない。`xrInitializing` がfalseになる前にはセッションを開始できないため、準備CoroutineがPlayingへ移行した後でFloorを設定する経路はない。既存のDesktopカメラ処理とBegin内の部屋配置は変更していない。

## 期限切れ後の制限

Floor準備済みで頭追跡だけが期限内に得られなかった場合は、後から追跡が戻れば `CanStart` が成立する。Floor設定まで完了していない期限切れ・未対応・拒否の場合は自動再試行を行わず、そのPlay起動ではVR開始を止める。接続を直してPlayを再起動するか、別設計で明示的再準備を検討する。この実装で10秒を超える全遅延機器を対応済みとは扱わない。

## 対象版とバックアップ

| 相対パス | 変更前SHA256 | 変更後SHA256 |
| --- | --- | --- |
| `Assets/LoopRoom/Scripts/DemoRig.cs` | `EA1AD7ED591ED9EF2DB772E8152C6C94DD8FE687D0450FE0D8A7C9CE0E27A837` | `EB753E6B0685B92A771918BD7756086A486EA140ECFB3532DD164F666F421474` |
| `Assets/LoopRoom/Scripts/LoopDemo.cs` | `1B04F7CAAD9E5AE7D711FE5003CF8FE5A0D6C3D61C04F6E3E5E0721CE745761E` | `449B19BD9694AFA8CB5EAB145A70B46DADBAB41A362404900923E893F172D39F` |

編集前に `Copy-Item` により `Collaboration/changes/002/before/<同相対パス>` へバイナリコピーし、原本とバックアップのSHA256一致を確認した。書き込みは許可された2C#、そのバックアップ2ファイル、本報告だけ。PackageCacheは読み取りのみ。Scene、ProjectSettings、Packages、LoopModel、部屋配置ソースへ書き込んでいない。別Unity Editorも起動していない。

## API確認とコンパイル検証

packages-lockの解決版を読み取りで確認: Input System 1.17.0、XR Management 4.5.4、XR Core Utils 2.6.0、XR Interaction Toolkit 3.3.2。

読み取りで確認した実ソース:

- `Library/PackageCache/com.unity.xr.management@e3a3882b360a/Runtime/XRManagerSettings.cs`: InitializeLoaderのStart完了後要件、activeLoader/isInitializationComplete、StartSubsystemsの条件。
- `Library/PackageCache/com.unity.xr.core-utils@a8b900321199/Runtime/XROrigin.cs`: RequestedTrackingOriginMode setter、SubsystemなしSetupCamera成功、対応モード/実モード/設定API、Floorのoffset処理、RepeatInitializeCameraの挙動。
- `Library/PackageCache/com.unity.inputsystem@02433b2481ab/InputSystem/Plugins/XR/TrackedPoseDriver.cs`: public trackingStateInput、int読取り、Position=1/Rotation=2のフラグ。

Unity 6000.3.15f1の既存Bee response `Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp` を読み、現在の4runtime C#すべてを291個の実参照アセンブリと同じpreprocessor defineで、PowerShellのRoslyn 5.0.0.0によりメモリ内semantic compilationした。`CSharpSyntaxTree.ParseText`、`MetadataReference.CreateFromFile`、`CSharpCompilation.Create`、`GetDiagnostics` を使用。OutputKindはDynamicallyLinkedLibrary、unsafeは許可。アセンブリ/スクリプト検証ファイルをプロジェクトへ生成せず、Unityのコンパイル操作も実行していない。

```text
Roslyn in-memory semantic compilation: Sources=4; References=291; Errors=0; Diagnostics=0
Compiler assembly=5.0.0.0
UTC=2026-09-17T01:03:09.6592569Z
Exit code=0
```

参照に使われたパッケージDLLのSHA256（Library内ファイルは読み取りのみ）:

| パス | SHA256 |
| --- | --- |
| `Library/ScriptAssemblies/Unity.InputSystem.dll` | `2BF8EA90C31A457700B07F65D6EDBCFADE6D979DD478E8696F496109BC26D8C7` |
| `Library/ScriptAssemblies/Unity.XR.Management.dll` | `A4D2CA3B9BA6907DA47699775BD7AB60B44AE9460D44300953A76A8037D11FCF` |
| `Library/ScriptAssemblies/Unity.XR.CoreUtils.dll` | `A61C21A733A854E85545FFB4DF912BA06FB7ECBFE140CC71A3FA75915C6EC7E8` |

この結果は現在ファイルのC# semantic compilationであり、既存Editorでの最新ソース統合コンパイルや実機起動の合格ではない。rootからのEditor.log情報も現版の2ファイルがUnityで再コンパイルされた証拠として流用していない。

## 未実施とレビュー引き継ぎ

- 現在EditorでのAssets Refresh、最新ソースUnity統合コンパイル、Play実行: 本担当では未実行。
- 通常/遅延起動中のEnter・A/X入力、Floor未対応/拒否/Subsystemなし実行、Desktop fallbackと `--desktop` 実操作: 未実行。
- Quest 3床・頭・両手の高さ、取っ手到達、死亡3回後のXR Origin/Tracking Offset不変、Play開始/停止3回の所有権・入力例外: 未実行。
- Sonnetレビューは認証保留で未実施。Solの新セッション独立レビュアーへは上記変更後SHA256とソース安定通知だけを送付した。実装確定後、rootから「同一SHAでSol独立レビュー完了・指摘なし」の連絡を受けた。レビューの正本は独立担当の別報告とし、本報告はその代用ではない。Sonnetとの相互指摘交換・最終受入判断は未完了。

以下にバイナリバックアップからの実差分を保存する。以降のソース変更は別SHA256の再検証対象とする。

## バックアップとの実差分

```diff
diff --git a/Collaboration/changes/002/before/Assets/LoopRoom/Scripts/DemoRig.cs b/Assets/LoopRoom/Scripts/DemoRig.cs
index ab9ee6c..c44740b 100644
--- a/Collaboration/changes/002/before/Assets/LoopRoom/Scripts/DemoRig.cs
+++ b/Assets/LoopRoom/Scripts/DemoRig.cs
@@ -17,12 +17,22 @@ namespace LoopRoom
     {
         public Camera View { get; private set; }
         public bool IsVR { get; private set; }
-        public bool HeadTracked => tracked.ReadValue<float>() > 0.5f;
+        public bool HeadTracked => tracked.ReadValue<float>() > 0.5f &&
+            (drivers[0].trackingStateInput.action.ReadValue<int>() &
+                (int)(InputTrackingState.Position | InputTrackingState.Rotation)) ==
+                (int)(InputTrackingState.Position | InputTrackingState.Rotation);
+        public bool CanStart => !xrInitializing && (IsVR ? FloorReady && HeadTracked && RuntimePresent() :
+            forceDesktop || desktopFallback);
+        public string PreparationMessage => xrInitializing ? "第零室\nVRを準備しています。\n接続とプレイエリアを確認してください。" :
+            FloorReady ? "第零室\n頭の位置と向きの追跡を待っています。" :
+            "第零室\nVRの準備ができませんでした。\n接続とプレイエリアを確認してください。";
         public Transform[] Hands { get; private set; }
         public bool StartPressed => startAction.WasPressedThisFrame();
         public bool NeedsRelease => needsRelease[0] || needsRelease[1];
         readonly List<InputAction> actions = new List<InputAction>();
         readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
+        readonly List<XRInputSubsystem> inputs = new List<XRInputSubsystem>();
+        readonly List<XRInputSubsystem> floorInputs = new List<XRInputSubsystem>();
         readonly bool[] wasGrip = new bool[2];
         readonly bool[] needsRelease = new bool[2];
         readonly bool[] manual = new bool[2];
@@ -32,16 +42,34 @@ namespace LoopRoom
         readonly TrackedPoseDriver[] drivers = new TrackedPoseDriver[3];
         InputAction tracked, startAction;
         XRInteractionManager manager;
+        XROrigin origin;
         bool forceDesktop;
         bool ownsXR;
+        bool xrInitializing, desktopFallback, floorPrepared, preparationReported;
         float yaw, pitch;
 
+        bool FloorReady
+        {
+            get
+            {
+                if (!floorPrepared || floorInputs.Count == 0) return false;
+                foreach (var input in floorInputs)
+                    if (!input.running || input.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor) return false;
+                SubsystemManager.GetSubsystems(inputs);
+                foreach (var input in inputs)
+                    if (input.running && input.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor) return false;
+                return true;
+            }
+        }
+
         public void Initialize()
         {
             forceDesktop = Array.IndexOf(Environment.GetCommandLineArgs(), "--desktop") >= 0;
             manager = new GameObject("Interaction Manager").AddComponent<XRInteractionManager>();
             manager.transform.SetParent(transform);
-            var origin = gameObject.AddComponent<XROrigin>();
+            origin = gameObject.AddComponent<XROrigin>();
+            // Apply origin configuration only after running input subsystems confirm Floor.
+            origin.enabled = false;
             var offset = new GameObject("Tracking Offset");
             offset.transform.SetParent(transform, false);
             var cameraObject = new GameObject("HMD Camera", typeof(Camera), typeof(AudioListener));
@@ -53,7 +81,6 @@ namespace LoopRoom
             View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(.012f,.02f,.03f);
             origin.Camera = View; origin.CameraFloorOffsetObject = offset;
             origin.CameraYOffset = 1.65f;
-            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
             drivers[0] = AddPose(cameraObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");
             tracked = Action("Head tracked", "<XRHMD>/isTracked");
             startAction = Action("Start", "<XRController>{RightHand}/primaryButton", InputActionType.Button);
@@ -80,7 +107,7 @@ namespace LoopRoom
             }
             SetVR(false);
             foreach(var input in actions) input.Enable();
-            if(!forceDesktop) StartCoroutine(StartXR());
+            if(!forceDesktop) { xrInitializing=true; StartCoroutine(StartXR()); }
         }
 
         InputAction Action(string name, string binding, InputActionType type = InputActionType.Value)
@@ -193,14 +220,74 @@ namespace LoopRoom
         IEnumerator StartXR()
         {
             // Register bindings before OpenXR creates its action sets.
+            // XR Management requires manual loader initialization after Start completes.
+            yield return null;
             var settings=XRGeneralSettings.Instance;
-            if(settings==null || settings.Manager==null) yield break;
-            if(settings.Manager.activeLoader!=null) yield break;
-            yield return settings.Manager.InitializeLoader();
-            if(settings.Manager.activeLoader!=null)
+            if(settings==null || settings.Manager==null)
             {
+                desktopFallback=true; xrInitializing=false;
+                ReportPreparation("XR settings unavailable; using desktop fallback."); yield break;
+            }
+            if(settings.Manager.activeLoader==null)
+            {
+                yield return settings.Manager.InitializeLoader();
+                if(settings.Manager.activeLoader==null)
+                {
+                    desktopFallback=true; xrInitializing=false;
+                    ReportPreparation("XR loader initialization completed without a loader; using desktop fallback."); yield break;
+                }
                 settings.Manager.StartSubsystems(); ownsXR=true;
             }
+            // An existing loader remains externally owned; do not start or release it here.
+            float deadline=Time.realtimeSinceStartup+10f;
+            bool requested=false;
+            while(Time.realtimeSinceStartup<deadline)
+            {
+                if(!requested)
+                {
+                    SubsystemManager.GetSubsystems(inputs);
+                    floorInputs.Clear();
+                    foreach(var input in inputs) if(input.running) floorInputs.Add(input);
+                    if(!RuntimePresent() || floorInputs.Count==0) { yield return null; continue; }
+                    bool known=true;
+                    foreach(var input in floorInputs)
+                    {
+                        var supported=input.GetSupportedTrackingOriginModes();
+                        if(supported==TrackingOriginModeFlags.Unknown) { known=false; continue; }
+                        if((supported & TrackingOriginModeFlags.Floor)==0)
+                        {
+                            xrInitializing=false; ReportPreparation("Running XR input subsystem does not support Floor; VR start blocked."); yield break;
+                        }
+                    }
+                    if(!known) { yield return null; continue; }
+                    foreach(var input in floorInputs)
+                        if(!input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
+                        {
+                            xrInitializing=false; ReportPreparation("XR input subsystem rejected Floor; VR start blocked."); yield break;
+                        }
+                    requested=true;
+                }
+                bool confirmed=true;
+                foreach(var input in floorInputs)
+                    if(!input.running || input.GetTrackingOriginMode()!=TrackingOriginModeFlags.Floor) confirmed=false;
+                if(confirmed && !floorPrepared)
+                {
+                    origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;
+                    origin.enabled=true;
+                    floorPrepared=true;
+                }
+                if(FloorReady && RuntimePresent() && HeadTracked) { xrInitializing=false; yield break; }
+                yield return null;
+            }
+            xrInitializing=false;
+            ReportPreparation("XR preparation timed out waiting for running input, confirmed Floor, or head position/rotation tracking; VR start blocked until ready.");
+        }
+
+        void ReportPreparation(string reason)
+        {
+            if(preparationReported) return;
+            preparationReported=true;
+            Debug.LogWarning(reason,this);
         }
 
         void OnDestroy()
diff --git a/Collaboration/changes/002/before/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index 603c35f..fbf1d19 100644
--- a/Collaboration/changes/002/before/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -77,7 +77,7 @@ namespace LoopRoom
             rig.PollMode(idle);
             var keyboard=Keyboard.current;
             bool enter=keyboard!=null && keyboard.enterKey.wasPressedThisFrame;
-            if (idle && (!rig.IsVR || rig.HeadTracked) && (enter || (rig.IsVR && rig.StartPressed))) Begin();
+            if (idle && rig.CanStart && (enter || (rig.IsVR && rig.StartPressed))) Begin();
             if (keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) Model.Interrupt();
             if (keyboard!=null && keyboard.f2Key.wasPressedThisFrame) privateOverlay=!privateOverlay;
             if (!Application.isFocused && !Application.isEditor && !idle) Model.Interrupt();
@@ -128,6 +128,7 @@ namespace LoopRoom
 
         void Begin()
         {
+            if(!rig.CanStart) return;
             // Do not re-center tracking: the user's real position remains unchanged.
             rig.ClearSelection(); trackingLost=0;
             if(rig.IsVR)
@@ -169,7 +170,7 @@ namespace LoopRoom
             bool show=Model.Phase!=SessionPhase.Playing && Model.Phase!=SessionPhase.Blackout;
             messagePanel.SetActive(show); message.gameObject.SetActive(show);
             if(Model.Phase==SessionPhase.Ready || Model.Phase==SessionPhase.Finished)
-                message.text=rig.IsVR ? "第零室\n手元の取っ手に手を近づけ、グリップで操作\nA または X ボタンで開始" : "第零室  /  操作確認\nEnter：開始　Space：遮蔽　E：出口\n右ドラッグ：見回す";
+                message.text=!rig.CanStart ? rig.PreparationMessage : rig.IsVR ? "第零室\n手元の取っ手に手を近づけ、グリップで操作\nA または X ボタンで開始" : "第零室  /  操作確認\nEnter：開始　Space：遮蔽　E：出口\n右ドラッグ：見回す";
             else if(Model.Phase==SessionPhase.Escaped) message.text="脱出した。\n今度は、時間が進んでいる。";
             else if(Model.Phase==SessionPhase.TimedOut) message.text="今回は、脱出できなかった。\n見つけた手がかりは、あなたの記憶に。";
             else if(Model.Phase==SessionPhase.Interrupted) message.text="体験を中断しました。\n接続と周囲を確認してください。";
@@ -206,7 +207,7 @@ namespace LoopRoom
             GUI.Label(new Rect(32,72,340,28),"LOOP "+Model.LoopId.ToString("00")+"  ·  "+PublicState(),body);
             if(!rig.IsVR || privateOverlay)
             {
-                GUI.Label(new Rect(32,107,340,26),"Enter 開始 / Space 遮蔽 / E 出口",small);
+                GUI.Label(new Rect(32,107,340,26),rig.CanStart ? "Enter 開始 / Space 遮蔽 / E 出口" : "開始前の接続と追跡を確認中",small);
                 GUI.Label(new Rect(32,131,340,26),"右ドラッグ 視点 / Esc 中断 / F2 運営表示",small);
                 GUI.Label(new Rect(32,155,340,26),"S01  "+Model.TotalTime.ToString("F1")+"s  "+logMessage,small);
             }
@@ -217,7 +218,7 @@ namespace LoopRoom
             if(Model.Outcome==SessionPhase.Escaped) return "ESCAPED";
             if(Model.Outcome==SessionPhase.TimedOut) return "SESSION ENDED";
             if(Model.Outcome==SessionPhase.Interrupted) return "PAUSED";
-            if(Model.Phase==SessionPhase.Ready) return "READY";
+            if(Model.Phase==SessionPhase.Ready) return rig.CanStart ? "READY" : "PREPARING";
             return "IN PROGRESS";
         }
     }
```
