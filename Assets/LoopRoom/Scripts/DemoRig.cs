using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace LoopRoom
{
    public sealed class DemoRig : MonoBehaviour
    {
        public Camera View { get; private set; }
        public bool IsVR { get; private set; }
        public bool HeadTracked => tracked.ReadValue<float>() > 0.5f &&
            (drivers[0].trackingStateInput.action.ReadValue<int>() &
                (int)(InputTrackingState.Position | InputTrackingState.Rotation)) ==
                (int)(InputTrackingState.Position | InputTrackingState.Rotation);
        public bool CanStart => !xrInitializing && (IsVR ? FloorReady && HeadTracked && RuntimePresent() :
            forceDesktop || desktopFallback);
        public bool CanRetryPreparation => !xrInitializing && !forceDesktop && (desktopFallback || !CanStart);
        public string PreparationMessage => xrInitializing ? "第零室\nVRを準備しています。\n接続とプレイエリアを確認してください。" :
            FloorReady ? "第零室\n頭の位置と向きの追跡を待っています。" + (CanRetryPreparation ? "\nR: 再準備（運営）" : "") :
            "第零室\nVRの準備ができませんでした。\n接続とプレイエリアを確認してください。\nR: 再準備（運営）";
        public Transform[] Hands { get; private set; }
        public bool StartPressed => startAction.WasPressedThisFrame();
        // Same physical input as StartPressed (A/X on either hand), exposed as a held state so the
        // calibration screen can require a 1-second hold instead of a tap (task021).
        public bool StartHeld => startAction.IsPressed();
        public bool NeedsRelease => needsRelease[0] || needsRelease[1];
        readonly List<InputAction> actions = new List<InputAction>();
        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        // Separate lists for FloorReady's per-frame poll and StartXR's coroutine poll (task024-b):
        // both used to share `inputs`, risking a same-frame mix-up between the two call sites.
        readonly List<XRInputSubsystem> pollInputs = new List<XRInputSubsystem>();
        readonly List<XRInputSubsystem> xrPrepInputs = new List<XRInputSubsystem>();
        readonly List<XRInputSubsystem> floorInputs = new List<XRInputSubsystem>();
        readonly bool[] wasGrip = new bool[2];
        readonly bool[] needsRelease = new bool[2];
        readonly bool[] manual = new bool[2];
        readonly XRDirectInteractor[] interactors = new XRDirectInteractor[2];
        readonly InputAction[] grips = new InputAction[2];
        readonly InputAction[] handTracked = new InputAction[2];
        readonly TrackedPoseDriver[] drivers = new TrackedPoseDriver[3];
        InputAction tracked, startAction;
        XRInteractionManager manager;
        XROrigin origin;
        bool forceDesktop;
        bool ownsXR;
        bool xrInitializing, desktopFallback, floorPrepared, preparationReported;
        float yaw, pitch, desktopPitch;
        Vector3 driftOffset;
        // Fixed, deterministic per-loop offsets/yaws for --simulate-drift evidence screenshots (desktop only).
        // Alternating "no correction" (small drift, stays inside the safe area) and
        // "correction" (large drift, forces RoomAnchor to pick a different front yaw) entries,
        // per task018's 追修正 (the first, all-large-drift list always triggered correction).
        static readonly Vector3[] DriftOffsets = { new Vector3(.15f,0,.10f), new Vector3(.55f,0,.35f), new Vector3(-.20f,0,-.05f), new Vector3(-.60f,0,-.40f) };
        static readonly float[] DriftYaws = { 20f, 40f, -30f, -120f };

        bool FloorReady
        {
            get
            {
                if (!floorPrepared || floorInputs.Count == 0) return false;
                foreach (var input in floorInputs)
                    if (!input.running || input.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor) return false;
                SubsystemManager.GetSubsystems(pollInputs);
                foreach (var input in pollInputs)
                    if (input.running && input.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor) return false;
                return true;
            }
        }

        public void Initialize()
        {
            var args=Environment.GetCommandLineArgs();
            forceDesktop = Array.IndexOf(args, "--desktop") >= 0;
            if(forceDesktop)
            {
                int pitchArgument=Array.IndexOf(args,"--desktop-pitch");
                if(pitchArgument>=0 && pitchArgument+1<args.Length &&
                    float.TryParse(args[pitchArgument+1],NumberStyles.Float,CultureInfo.InvariantCulture,out var requestedPitch) &&
                    !float.IsNaN(requestedPitch) && !float.IsInfinity(requestedPitch))
                    desktopPitch=Mathf.Clamp(requestedPitch,-70f,70f);
            }
            manager = new GameObject("Interaction Manager").AddComponent<XRInteractionManager>();
            manager.transform.SetParent(transform);
            origin = gameObject.AddComponent<XROrigin>();
            // Apply origin configuration only after running input subsystems confirm Floor.
            origin.enabled = false;
            var offset = new GameObject("Tracking Offset");
            offset.transform.SetParent(transform, false);
            var cameraObject = new GameObject("HMD Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(offset.transform, false);
            View = cameraObject.GetComponent<Camera>();
            View.cullingMask = ~(1 << 9);
            View.nearClipPlane = 0.035f; View.farClipPlane = 30;
            View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(.012f,.02f,.03f);
            origin.Camera = View; origin.CameraFloorOffsetObject = offset;
            origin.CameraYOffset = 1.65f;
            drivers[0] = AddPose(cameraObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");
            tracked = Action("Head tracked", "<XRHMD>/isTracked");
            startAction = Action("Start", "<XRController>{RightHand}/primaryButton", InputActionType.Button);
            startAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            Hands = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                string usage = i == 0 ? "LeftHand" : "RightHand";
                string binding = "<XRController>{" + usage + "}";
                var hand = new GameObject(usage); hand.transform.SetParent(offset.transform, false);
                Hands[i] = hand.transform;
                var body = hand.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                var collider = hand.AddComponent<SphereCollider>(); collider.radius = .045f; collider.isTrigger = true;
                interactors[i] = hand.AddComponent<XRDirectInteractor>(); interactors[i].interactionManager = manager;
                drivers[i+1] = AddPose(hand, binding + "/devicePosition", binding + "/deviceRotation", binding + "/trackingState");
                grips[i] = Action(usage + " grip", binding + "/grip");
                handTracked[i] = Action(usage + " tracked", binding + "/isTracked");
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Controller proxy"; visual.transform.SetParent(hand.transform, false);
                visual.layer = RoomVisuals.PrivateLayer;
                visual.transform.localScale = new Vector3(.075f,.055f,.11f);
                Destroy(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().material = RoomVisuals.Material(new Color(.5f,.82f,.84f));
            }
            SetVR(false);
            foreach(var input in actions) input.Enable();
            if(!forceDesktop) { xrInitializing=true; StartCoroutine(StartXR()); }
        }

        InputAction Action(string name, string binding, InputActionType type = InputActionType.Value)
        {
            var action = new InputAction(name, type, binding); actions.Add(action); return action;
        }

        TrackedPoseDriver AddPose(GameObject target, string position, string rotation, string state)
        {
            var driver = target.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(Action(target.name + " position", position));
            driver.rotationInput = new InputActionProperty(Action(target.name + " rotation", rotation));
            driver.trackingStateInput = new InputActionProperty(Action(target.name + " state", state));
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            return driver;
        }

        void SetVR(bool value)
        {
            IsVR = value;
            foreach (var driver in drivers) driver.enabled = value;
            foreach (var hand in Hands) hand.gameObject.SetActive(value);
            if (!value)
            {
                View.transform.parent.localPosition = Vector3.zero;
                View.transform.localPosition = new Vector3(0,1.65f,0);
                View.transform.localRotation = Quaternion.identity;
            }
        }

        public void PollMode(bool allowSwitch)
        {
            SubsystemManager.GetSubsystems(displays);
            bool available = !forceDesktop && displays.Exists(d => d.running);
            if (allowSwitch && available != IsVR) SetVR(available);
            if(!IsVR)
            {
                View.transform.parent.localPosition=driftOffset;
                View.transform.localPosition=new Vector3(0,1.65f,0);
            }
            if (!IsVR && Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * .10f; pitch = Mathf.Clamp(pitch - delta.y * .10f, -70,70);
                View.transform.localRotation = Quaternion.Euler(pitch,yaw,0);
            }
        }

        // --simulate-drift evidence aid: before each loop's reposition, nudge the desktop
        // "head" by a fixed, deterministic offset/yaw so screenshots prove the room follows.
        public void SimulateDesktopDrift(int loopId)
        {
            if (IsVR) return;
            int i = (loopId - 1) % DriftOffsets.Length;
            if (i < 0) i += DriftOffsets.Length;
            driftOffset = DriftOffsets[i]; yaw = DriftYaws[i];
            View.transform.parent.localPosition = driftOffset;
            View.transform.localPosition = new Vector3(0,1.65f,0);
            View.transform.localRotation = Quaternion.Euler(pitch,yaw,0);
        }

        public bool RuntimePresent()
        {
            SubsystemManager.GetSubsystems(displays);
            return displays.Exists(d => d.running);
        }

        // Frame-time target for FrameStats (task026): the running XR display's reported
        // refresh rate in VR, the desktop monitor's refresh rate otherwise. Falls back to a
        // sane default (matches the design's 72/60 Hz) when the rate cannot be read.
        public double GetTargetHz()
        {
            if (IsVR)
            {
                SubsystemManager.GetSubsystems(displays);
                foreach (var display in displays)
                    if (display.running && display.TryGetDisplayRefreshRate(out float hz) &&
                        !float.IsNaN(hz) && !float.IsInfinity(hz) && hz >= 1 && hz <= 1000)
                        return hz;
                return 72;
            }
            var rate = Screen.currentResolution.refreshRateRatio.value;
            return !double.IsNaN(rate) && !double.IsInfinity(rate) && rate >= 1 && rate <= 1000 ? rate : 60;
        }

        // Guardian/play-area boundary, if the running Floor-mode input subsystem can report one
        // (task021 calibration screen). Points come back in world space: XRInputSubsystem returns
        // them relative to the tracking origin, which for this rig is the "XR Origin" GameObject's
        // own transform (Origin defaults to the attached GameObject; this component is on it).
        public bool TryGetBoundaryPoints(List<Vector3> worldPoints)
        {
            worldPoints.Clear();
            foreach (var input in floorInputs)
            {
                var local = new List<Vector3>();
                if (input.TryGetBoundaryPoints(local) && local.Count >= 3)
                {
                    foreach (var point in local) worldPoints.Add(transform.TransformPoint(point));
                    return true;
                }
            }
            return false;
        }

        // Desktop-only: pitches the free-look camera down by default so the calibration floor
        // overlay is visible without requiring a mouse drag (task021). No effect in VR (the real
        // head controls the view) and does not lock out the existing right-drag look.
        // 60° (追修正2, was 45°): the shallower angle left the outline outside the frame.
        public void LookAtFloorForCalibration(bool active)
        {
            if (IsVR) return;
            pitch = active ? 60f : desktopPitch;
            View.transform.localRotation = Quaternion.Euler(pitch,yaw,0);
        }

        public void Operate(XRSimpleInteractable[] controls, bool enabled)
        {
            if (!IsVR) return;
            for (int i=0; i<2; i++)
            {
                bool down = grips[i].ReadValue<float>() > .65f;
                bool valid = handTracked[i].ReadValue<float>() > .5f;
                if (!down) needsRelease[i] = false;
                if ((!down || !enabled || !valid) && manual[i]) EndSelection(i);
                if (enabled && valid && down && !wasGrip[i] && !needsRelease[i])
                {
                    XRSimpleInteractable nearest = null;
                    float distance = .16f;
                    foreach (var control in controls)
                    {
                        float d = Vector3.Distance(Hands[i].position, control.transform.position);
                        if (d < distance) { distance = d; nearest = control; }
                    }
                    if (nearest != null)
                    {
                        interactors[i].StartManualInteraction((IXRSelectInteractable)nearest);
                        manual[i] = interactors[i].isPerformingManualInteraction;
                    }
                }
                wasGrip[i] = down;
            }
        }

        void EndSelection(int i)
        {
            if (interactors[i] != null && interactors[i].isPerformingManualInteraction)
                interactors[i].EndManualInteraction();
            manual[i] = false;
        }

        public void ClearSelection()
        {
            for (int i=0; i<2; i++)
            {
                EndSelection(i);
                wasGrip[i] = grips[i].ReadValue<float>() > .65f;
                needsRelease[i] = wasGrip[i];
            }
        }

        public void Haptic(float amplitude)
        {
            for (int i=0; i<2; i++)
            {
                var device = InputDevices.GetDeviceAtXRNode(i == 0 ? XRNode.LeftHand : XRNode.RightHand);
                if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    device.SendHapticImpulse(0, amplitude, .065f);
            }
        }

        IEnumerator StartXR()
        {
            // Register bindings before OpenXR creates its action sets.
            // XR Management requires manual loader initialization after Start completes.
            yield return null;
            var settings=XRGeneralSettings.Instance;
            if(settings==null || settings.Manager==null)
            {
                desktopFallback=true; xrInitializing=false;
                ReportPreparation("XR settings unavailable; using desktop fallback."); yield break;
            }
            if(settings.Manager.activeLoader==null)
            {
                yield return settings.Manager.InitializeLoader();
                if(settings.Manager.activeLoader==null)
                {
                    desktopFallback=true; xrInitializing=false;
                    ReportPreparation("XR loader initialization completed without a loader; using desktop fallback."); yield break;
                }
                settings.Manager.StartSubsystems(); ownsXR=true;
            }
            // An existing loader remains externally owned; do not start or release it here.
            float deadline=Time.realtimeSinceStartup+10f;
            bool requested=false;
            while(Time.realtimeSinceStartup<deadline)
            {
                if(!requested)
                {
                    SubsystemManager.GetSubsystems(xrPrepInputs);
                    floorInputs.Clear();
                    foreach(var input in xrPrepInputs) if(input.running) floorInputs.Add(input);
                    if(!RuntimePresent() || floorInputs.Count==0) { yield return null; continue; }
                    bool known=true;
                    foreach(var input in floorInputs)
                    {
                        var supported=input.GetSupportedTrackingOriginModes();
                        if(supported==TrackingOriginModeFlags.Unknown) { known=false; continue; }
                        if((supported & TrackingOriginModeFlags.Floor)==0)
                        {
                            xrInitializing=false; ReportPreparation("Running XR input subsystem does not support Floor; VR start blocked."); yield break;
                        }
                    }
                    if(!known) { yield return null; continue; }
                    foreach(var input in floorInputs)
                        if(!input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                        {
                            xrInitializing=false; ReportPreparation("XR input subsystem rejected Floor; VR start blocked."); yield break;
                        }
                    requested=true;
                }
                bool confirmed=true;
                foreach(var input in floorInputs)
                    if(!input.running || input.GetTrackingOriginMode()!=TrackingOriginModeFlags.Floor) confirmed=false;
                if(confirmed && !floorPrepared)
                {
                    origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;
                    origin.enabled=true;
                    floorPrepared=true;
                }
                if(FloorReady && RuntimePresent() && HeadTracked) { xrInitializing=false; yield break; }
                yield return null;
            }
            xrInitializing=false;
            ReportPreparation("XR preparation timed out waiting for running input, confirmed Floor, or head position/rotation tracking; VR start blocked until ready.");
        }

        void ReportPreparation(string reason)
        {
            if(preparationReported) return;
            preparationReported=true;
            Debug.LogWarning(reason,this);
        }

        void StopOwnedXR()
        {
            if(ownsXR && XRGeneralSettings.Instance!=null && XRGeneralSettings.Instance.Manager!=null)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
        }

        public void RetryPreparation()
        {
            if(!CanRetryPreparation) return;
            var xrRunning=XRGeneralSettings.Instance!=null && XRGeneralSettings.Instance.Manager!=null && XRGeneralSettings.Instance.Manager.activeLoader!=null;
            if(xrRunning && !ownsXR) Debug.Log("ローダーは外部所有のため Floor と追跡の再確認のみ",this);
            StopOwnedXR();
            ownsXR=false; desktopFallback=false; floorPrepared=false; preparationReported=false;
            origin.enabled=false;
            floorInputs.Clear();
            xrInitializing=true;
            StartCoroutine(StartXR());
        }

        void OnDestroy()
        {
            StopOwnedXR();
            foreach (var action in actions) action.Dispose();
        }
    }
}
