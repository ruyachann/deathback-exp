using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace LoopRoom
{
    public sealed class LoopDemo : MonoBehaviour
    {
        public LoopRules timings = new LoopRules();
        public LoopModel Model { get; private set; }
        DemoRig rig;
        RoomVisuals room;
        XRSimpleInteractable[] controls;
        TextMesh message;
        GameObject messagePanel;
        GUIStyle title, body, small;
        int lastLoop, lastRecords;
        double lastLoopTime;
        bool saved, privateOverlay;
        float trackingLost;
        string sessionId, logMessage = "";
        AudioSource enemyAudio, ambience;
        bool desktopArg, autostart, autoescape, autostartUsed, autoShieldDone, autoExitDone;
        bool simulateDrift;
        // Calibration (task021): shown before the first Ready screen and again whenever the
        // operator asks to redo it. LoopModel.Phase stays Ready throughout; this is LoopDemo-only.
        PlayAreaSettings settings;
        CalibrationView calibration;
        bool calibrating = true;
        float calibrationHold;
        bool calibrationVisible, wasCalibrationVisible, wasRoomHidden, lastBoundaryAvailable;
        // Set by CommitCalibration() so the operator overlay can keep showing "境界外で決定"
        // until the *next* commit, even after the calibration screen itself closes (task021 追修正3-1).
        bool calibrationOutside;
        // Reset at the top of every Update(); true once this frame has processed C or R, so the
        // same frame's Enter/A-X/auto-trigger can never also start the session (task021 追修正3-4).
        bool operatorCommandConsumed;
        // Renderers under room.Root (追修正 2026-09-24: the desk was hiding the calibration outline).
        // Only Renderer.enabled is toggled, never room.Root.SetActive/its children's active state,
        // so the spectator camera and every audio source parented under Root (Sound/ambience) keep
        // working exactly as before while the room is hidden.
        Renderer[] roomRenderers;
        bool autoCalibrate, autoCalibrateDone;
        float autoCalibrateTimer;
        readonly List<Vector3> boundaryPoints = new List<Vector3>();
        bool messageSized;
        int messageMeasureAttempts;
        Color messageColor;
        // Operator alignment (key C): head floor position -> area center, head yaw -> area orientation.
        // Left at (0,0,0) until the operator aligns once; RoomAnchor then just uses the head yaw as-is.
        double alignCx, alignCz, alignAreaYaw;
        bool aligned;
        // Limits the "reach doesn't fit" warning to once per alignment (task018 追修正2-3).
        bool fitsWarned;

        [Serializable] sealed class SessionLog
        {
            public string sessionId;
            public string scenario = "S01_Shield";
            public string version = "0.1.0-prototype";
            public string mode;
            public string outcome;
            public double elapsed;
            public LoopRules timings;
            public LoopRecord[] events;
        }

        void Start()
        {
            Application.runInBackground = true;
            var args = Environment.GetCommandLineArgs();
            // Match DemoRig's own --desktop reading: --autostart/--autoescape must never fire on a
            // desktop *fallback* (failed XR init without --desktop), only on an explicit --desktop launch.
            desktopArg = Array.IndexOf(args, "--desktop") >= 0;
            autostart = desktopArg && Array.IndexOf(args, "--autostart") >= 0;
            autoescape = autostart && Array.IndexOf(args, "--autoescape") >= 0;
            simulateDrift = desktopArg && Array.IndexOf(args, "--simulate-drift") >= 0;
            autoCalibrate = desktopArg && Array.IndexOf(args, "--auto-calibrate") >= 0;
            Model = new LoopModel(timings);
            rig = new GameObject("XR Origin").AddComponent<DemoRig>(); rig.transform.SetParent(transform,false);
            rig.Initialize();
            settings = PlayAreaSettings.LoadOrCreate(Path.Combine(Application.persistentDataPath,"play-area.json"));
            calibration = new CalibrationView(); calibration.Build(transform,settings);
            room = new RoomVisuals(); room.Build(transform,rig);
            roomRenderers = room.Root.GetComponentsInChildren<Renderer>(true);
            room.Chime=ProceduralAudio.Chime(); room.Shot=ProceduralAudio.Shot();
            room.Latch=ProceduralAudio.Latch(); room.Open=ProceduralAudio.Open();
            controls = new[]{room.ShieldHandle,room.ExitHandle};
            room.ShieldHandle.selectEntered.AddListener(_ => RaiseShield());
            room.ExitHandle.selectEntered.AddListener(_ => TryExit());
            // Enemy is inactive until LoopTime>=3, and a disabled AudioSource plays nothing;
            // keep the source on an always-active object that follows the enemy instead.
            enemyAudio = new GameObject("Enemy audio").AddComponent<AudioSource>();
            enemyAudio.transform.SetParent(room.Root,false); enemyAudio.playOnAwake=false;
            enemyAudio.spatialBlend=1; enemyAudio.minDistance=.5f; enemyAudio.maxDistance=10; enemyAudio.volume=.22f;
            // Local, not world, position: room.Sound is a child of room.Root, so this keeps the
            // chime coming from the room's front after Begin()/loop-change repositions Root.
            room.Sound.transform.localPosition = new Vector3(0,1.4f,.7f);
            room.Sound.spatialBlend=1; room.Sound.minDistance=.4f; room.Sound.maxDistance=8;
            // Separate source so the per-loop "room.Sound.Stop(); enemyAudio.Stop();" in Update() never cuts the ambience.
            ambience = new GameObject("Room tone").AddComponent<AudioSource>();
            ambience.transform.SetParent(room.Root,false); ambience.playOnAwake=false;
            ambience.loop=true; ambience.spatialBlend=0; ambience.volume=.12f;
            ambience.clip=ProceduralAudio.RoomTone(); ambience.Play();
            BuildMessage();
            RefreshWorld();
        }

        void BuildMessage()
        {
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},64);
            messagePanel=GameObject.CreatePrimitive(PrimitiveType.Quad);
            messagePanel.name="Ready and ending panel"; messagePanel.layer=RoomVisuals.PrivateLayer;
            messagePanel.transform.SetParent(rig.View.transform,false);
            Destroy(messagePanel.GetComponent<Collider>());
            messagePanel.GetComponent<Renderer>().material=RoomVisuals.Material(new Color(.02f,.035f,.045f),true);
            messagePanel.SetActive(false);
            var text=new GameObject("Session instructions"); text.layer=RoomVisuals.PrivateLayer;
            text.transform.SetParent(rig.View.transform,false);
            message=text.AddComponent<TextMesh>(); message.font=font; message.fontSize=64;
            message.characterSize=.0145f; message.anchor=TextAnchor.MiddleCenter; message.alignment=TextAlignment.Center;
            messageColor=new Color(.88f,.88f,.77f);
            message.color=Color.clear; text.GetComponent<MeshRenderer>().material=RoomVisuals.TextMaterial(font);
            // Size the panel from the actual worst-case guidance text instead of a guessed
            // constant: among every Ready/Finished message (RefreshWorld, DemoRig.PreparationMessage)
            // the widest single line has 20 full-width characters and the tallest message has 4
            // lines. TextMesh has no MeshFilter (only a MeshRenderer), so keep this probe text
            // invisible and measure it from LateUpdate via MeshRenderer.localBounds.
            string probeLine=new string('国',20);
            message.text=probeLine+"\n"+probeLine+"\n"+probeLine+"\n"+probeLine;
        }

        void LateUpdate()
        {
            if(messageSized || message==null) return;
            messageMeasureAttempts++;
            var renderer=message.GetComponent<MeshRenderer>();
            var size=renderer!=null ? renderer.localBounds.size : Vector3.zero;
            if(size.x>0 && size.y>0) { ApplyMessageSize(size,false); return; }
            if(messageMeasureAttempts>=30) ApplyMessageSize(new Vector3(.62f,.26f,0),true);
        }

        void ApplyMessageSize(Vector3 bounds, bool fixedFallback)
        {
            float z=.82f;
            float shrink=1;
            if(!fixedFallback)
            {
                // Worst-case line must fit within ~0.56m at 0.82m distance (comfortable reading arc).
                const float maxLineWidth=.56f;
                shrink=Mathf.Min(1f,maxLineWidth/bounds.x);
                message.characterSize*=shrink;
                bounds=new Vector3(bounds.x*shrink,bounds.y*shrink,bounds.z);
            }
            else
            {
                // Fixed fallback text is never measured, so it would otherwise overflow the
                // .62x.26 panel; use the shrink measured on a dev machine (追修正5).
                const float fixedCharacterSize=.0044f;
                shrink=fixedCharacterSize/message.characterSize;
                message.characterSize=fixedCharacterSize;
            }
            messagePanel.transform.localPosition=new Vector3(0,.02f,z);
            // Padding only applies to a measured size; the fixed fallback (.62x.26) is used as-is.
            messagePanel.transform.localScale=fixedFallback?bounds:new Vector3(bounds.x+.05f,bounds.y+.05f,1);
            message.transform.localPosition=new Vector3(0,.02f,z-.02f);
            messageSized=true;
            // Color/text stay deferred to the next RefreshWorld (see there) so the probe glyphs
            // never flash on screen once sizing is decided.
            Debug.Log("LoopDemo: message panel sized "+(fixedFallback?"(fixed fallback)":"(measured)")+" bounds="+bounds+" shrink="+shrink);
        }

        void Update()
        {
            if (Model == null) return;
            bool idle=Model.Phase==SessionPhase.Ready || Model.Phase==SessionPhase.Finished;
            // Detect desktop<->VR switches around PollMode (which is what actually flips rig.IsVR)
            // so a stale alignment taken in the other mode can't silently carry over (task018 追修正2-1).
            bool wasVR=rig.IsVR;
            rig.PollMode(idle);
            if(rig.IsVR!=wasVR) ResetAlignment();
            var keyboard=Keyboard.current;
            operatorCommandConsumed = false;
            // Checked before Enter/Start so a same-frame Enter+C press cannot slip past idle
            // (Begin() would already have advanced Phase) and still commit an alignment (task018 追修正3-2).
            // Tracking must be live and VR (when applicable) fully ready before C can commit an
            // alignment, otherwise it could capture a meaningless head pose (task018 追修正2-1).
            // C confirms the calibration while it is shown; otherwise it now sends idle back to the
            // calibration screen instead of aligning immediately (task021 replaces the old behavior).
            if (idle && rig.CanStart && keyboard!=null && keyboard.cKey.wasPressedThisFrame)
            {
                if (calibrating) CommitCalibration(); else calibrating = true;
                operatorCommandConsumed = true;
            }
            // Moved ahead of the Enter/Start check below (task021 追修正4-2): R used to be an
            // "else if" hanging off that check, so a same-frame R+Enter/StartPressed/autoTrigger
            // let Begin() run first and skipped R's RetryPreparation/ResetAlignment entirely. C and
            // R are now both resolved (exclusively, via operatorCommandConsumed) before Begin() is
            // even considered.
            else if (idle && !operatorCommandConsumed && keyboard!=null && keyboard.rKey.wasPressedThisFrame && rig.CanRetryPreparation)
            {
                rig.RetryPreparation(); ResetAlignment(); operatorCommandConsumed = true;
            }
            // Hides the room whenever calibration is the active idle screen, regardless of
            // rig.CanStart: if tracking/Floor drops out mid-calibration the room must stay hidden
            // too, not reappear just because the floor overlay itself can no longer be shown
            // (task021 追修正3-3). The floor overlay's own visibility keeps requiring CanStart below.
            bool roomHidden = calibrating && idle;
            if (roomHidden != wasRoomHidden) { SetRoomVisible(!roomHidden); wasRoomHidden = roomHidden; }
            calibrationVisible = calibrating && idle && rig.CanStart;
            if (calibrationVisible != wasCalibrationVisible)
            {
                rig.LookAtFloorForCalibration(calibrationVisible);
                wasCalibrationVisible = calibrationVisible;
            }
            if (calibrationVisible)
            {
                // Same input as DemoRig's start button (A/X); a short press must not confirm.
                calibrationHold = rig.StartHeld ? calibrationHold + Time.unscaledDeltaTime : 0f;
                if (calibrationHold >= 1f) CommitCalibration();
                else if (autoCalibrate && !autoCalibrateDone)
                {
                    autoCalibrateTimer += Time.unscaledDeltaTime;
                    if (autoCalibrateTimer >= 2f) { autoCalibrateDone = true; CommitCalibration(); }
                }
            }
            else calibrationHold = 0f;
            bool enter=keyboard!=null && keyboard.enterKey.wasPressedThisFrame;
            bool autoTrigger = autostart && !autostartUsed && !rig.IsVR;
            // operatorCommandConsumed (set by C or R above, or inside CommitCalibration for the 1s
            // hold/--auto-calibrate paths) blocks this same frame from also starting the session
            // (task021 追修正3-4, 追修正4-2).
            if (idle && !calibrating && !operatorCommandConsumed && rig.CanStart && (enter || (rig.IsVR && rig.StartPressed) || autoTrigger)) { if(autoTrigger) autostartUsed=true; Begin(); }
            if (keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) Model.Interrupt();
            if (keyboard!=null && keyboard.f2Key.wasPressedThisFrame) privateOverlay=!privateOverlay;
            // Focus loss ends only the desktop check mode; in VR the HMD keeps running (runInBackground) while the operator uses other windows.
            if (!rig.IsVR && !Application.isFocused && !Application.isEditor && !idle) Model.Interrupt();
            if (rig.IsVR && (Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout))
            {
                trackingLost = !rig.HeadTracked || !rig.RuntimePresent() ? trackingLost+Time.unscaledDeltaTime : 0;
                // Freeze gameplay immediately on lost tracking; end the session if loss persists.
                if(trackingLost>0)
                {
                    rig.ClearSelection();
                    if(trackingLost>.3f) { Model.Interrupt(); trackingLost=0; }
                    RefreshWorld(); return;
                }
            }
            Model.Advance(Time.unscaledDeltaTime);
            if(Model.LoopId!=lastLoop)
            {
                // Reposition happens before RefreshWorld() below turns the blackout off this same
                // frame, so the move itself is never seen (see task018 design note 3).
                if(simulateDrift) rig.SimulateDesktopDrift(Model.LoopId);
                PlaceRoom();
                rig.ClearSelection(); room.Sound.Stop(); enemyAudio.Stop();
                room.Sound.PlayOneShot(room.Chime); lastLoop=Model.LoopId; lastLoopTime=0;
                autoShieldDone=false; autoExitDone=false;
            }
            // Damage/time boundaries are resolved before this frame's fresh input.
            rig.Operate(controls,Model.Phase==SessionPhase.Playing);
            if(!rig.IsVR && Model.Phase==SessionPhase.Playing && keyboard!=null)
            {
                if(keyboard.spaceKey.wasPressedThisFrame) RaiseShield();
                if(keyboard.eKey.wasPressedThisFrame) TryExit();
            }
            if(autoescape && !rig.IsVR && Model.Phase==SessionPhase.Playing && Model.LoopId==2)
            {
                if(!autoShieldDone && Model.LoopTime>=1) { autoShieldDone=true; RaiseShield(); }
                if(!autoExitDone && Model.ExitAvailable) { autoExitDone=true; TryExit(); }
            }
            // A long frame can cross t=3 and the shot together; LoopTime is frozen in Blackout, so the latch still plays.
            if((Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout) && lastLoopTime<3 && Model.LoopTime>=3)
                enemyAudio.PlayOneShot(room.Latch);
            if(Model.Phase==SessionPhase.Playing && lastLoopTime<Model.Rules.exitOpens && Model.LoopTime>=Model.Rules.exitOpens)
                room.Sound.PlayOneShot(room.Open,.6f);
            if(lastRecords<Model.Records.Count)
            {
                for(int i=lastRecords;i<Model.Records.Count;i++)
                {
                    var record=Model.Records[i];
                    if(record.kind=="first_shot" || record.kind=="shot_blocked" || record.kind=="flanked")
                    { enemyAudio.PlayOneShot(room.Shot,.8f); rig.Haptic(.18f); }
                }
                lastRecords=Model.Records.Count;
            }
            lastLoopTime=Model.LoopTime;
            if(!saved && Model.Outcome!=SessionPhase.Ready) SaveLog();
            RefreshWorld();
        }

        // Hides/shows the room's own renderers only (追修正 2026-09-24): room.Root.SetActive would
        // also disable the spectator camera and every AudioSource parented under Root, so those
        // must stay untouched and keep being driven by UpdatePublic()/normal playback as before.
        void SetRoomVisible(bool visible)
        {
            foreach(var r in roomRenderers) if(r!=null) r.enabled=visible;
        }

        // Clears the alignment flag together with the 3 stored values so PlaceRoom can never
        // reuse a stale center/orientation after the operator's alignment is invalidated (task018 追修正3-1).
        // task021: also sends idle back to the calibration screen, since a mode switch or VR
        // re-preparation makes the previous head pose meaningless. calibrationOutside is left
        // untouched here (task021 追修正4-3): the "境界外で決定" warning must persist until the
        // next CommitCalibration() actually determines a fresh Fit, not disappear just because a
        // retry/reset happened before the operator has re-decided.
        void ResetAlignment() { aligned=false; alignCx=0; alignCz=0; alignAreaYaw=0; calibrating=true; calibrationHold=0; }

        // Commits the calibration: the current head floor position/yaw become the alignment
        // center/orientation (same 2 values PlaceRoom/RoomAnchor use), and the calibration screen closes.
        void CommitCalibration()
        {
            var head=rig.View.transform;
            var headFloor=new Vector3(head.position.x,0,head.position.z);
            // Re-checked against the boundary right now, at the exact head pose being committed,
            // instead of reusing calibration.Fit from the last RefreshWorld() frame: a commit can
            // happen earlier in this same Update() than RefreshWorld() runs (task021 追修正3-1).
            lastBoundaryAvailable = rig.TryGetBoundaryPoints(boundaryPoints) && boundaryPoints.Count >= 3;
            calibration.Refresh(headFloor, head.eulerAngles.y, boundaryPoints, lastBoundaryAvailable);
            calibrationOutside = calibration.Fit == CalibrationView.BoundaryFit.Outside;
            if(calibrationOutside) Debug.LogWarning("LoopRoom: calibration committed outside the reported guardian boundary.");
            alignCx=head.position.x; alignCz=head.position.z; alignAreaYaw=head.eulerAngles.y;
            aligned=true; fitsWarned=false; calibrating=false; calibrationHold=0;
            operatorCommandConsumed = true;
        }

        void Begin()
        {
            if(!rig.CanStart) return;
            // Do not re-center tracking: the user's real position remains unchanged.
            rig.ClearSelection(); trackingLost=0;
            PlaceRoom();
            sessionId=Guid.NewGuid().ToString("N"); saved=false; lastLoop=0; lastRecords=0;
            Model.Start();
        }

        // Places room.Root at the player's current head-floor position, with the front (RoomAnchor's
        // yaw) chosen so the forward reach fits the aligned safe area. Same call for VR and desktop.
        void PlaceRoom()
        {
            var head=rig.View.transform;
            double px=head.position.x, pz=head.position.z, headYaw=head.eulerAngles.y;
            double frontYaw=RoomAnchor.ChooseFrontYaw(px,pz,headYaw,alignCx,alignCz,alignAreaYaw,settings,out bool fits);
            room.Root.position=new Vector3((float)px,0,(float)pz);
            room.Root.rotation=Quaternion.Euler(0,(float)frontYaw,0);
            double yawDiff=((frontYaw-headYaw)%360+540)%360-180;
            bool corrected=Math.Abs(yawDiff)>0.001;
            Debug.Log("LoopRoom: PlaceRoom pos=("+px.ToString("F2")+","+pz.ToString("F2")+") headYaw="+headYaw.ToString("F1")+
                " frontYaw="+frontYaw.ToString("F1")+" corrected="+corrected+" diff="+yawDiff.ToString("F1")+" fits="+fits);
            if(!fits && !fitsWarned) { fitsWarned=true; Debug.LogWarning("LoopRoom: no orientation keeps the forward reach inside the safe area; using head yaw as-is."); }
        }

        void RaiseShield()
        {
            if(Model.RaiseShield(Model.LoopId)) { room.Sound.PlayOneShot(room.Latch); rig.Haptic(.08f); }
        }

        void TryExit()
        {
            if(Model.TryExit(Model.LoopId)) room.Sound.PlayOneShot(room.Open);
        }

        void RefreshWorld()
        {
            float t=(float)Model.LoopTime;
            bool playing=Model.Phase==SessionPhase.Playing;
            room.Barrier.localPosition=new Vector3(0,Model.ShieldRaised?1.65f:.35f,1.08f);
            room.Door.localPosition=new Vector3(.8f+Mathf.Clamp01((t-3)/.65f)*1.05f,1.18f,2.86f);
            room.Enemy.gameObject.SetActive(t>=3 && (playing || Model.Phase==SessionPhase.Blackout));
            float move=Mathf.Clamp01((t-8)/3.5f);
            room.Enemy.localPosition=Vector3.Lerp(new Vector3(.8f,0,2.5f),new Vector3(1.25f,0,.38f),move);
            room.Enemy.localRotation=Quaternion.Euler(0,move*75,0);
            enemyAudio.transform.localPosition=room.Enemy.localPosition+new Vector3(0,1.4f,0);
            room.Clock.text="00 : "+Mathf.FloorToInt(t).ToString("00");
            Color lamp=Model.ExitAvailable?new Color(.25f,1,.66f):new Color(.7f,.13f,.09f);
            room.ExitLamp.material.color=lamp;
            room.ExitLabel.text=Model.ExitAvailable?"脱出可能":"施錠中";
            room.Blackout.SetActive(Model.Phase==SessionPhase.Blackout || trackingLost>0);
            room.UpdatePublic(rig.IsVR,playing);
            calibration.SetVisible(calibrationVisible);
            if(calibrationVisible)
            {
                var head=rig.View.transform;
                lastBoundaryAvailable=rig.TryGetBoundaryPoints(boundaryPoints) && boundaryPoints.Count>=3;
                calibration.Refresh(new Vector3(head.position.x,0,head.position.z),head.eulerAngles.y,boundaryPoints,lastBoundaryAvailable);
            }
            bool show=Model.Phase!=SessionPhase.Playing && Model.Phase!=SessionPhase.Blackout;
            messagePanel.SetActive(show && messageSized); message.gameObject.SetActive(show);
            // Keep the probe text (see BuildMessage) until LateUpdate has measured and sized the
            // panel; otherwise the real, shorter guidance text would be measured instead of the
            // intended worst case. Text and color are restored together here (the frame after
            // sizing) so the probe glyphs never render with real color.
            if(messageSized)
            {
                message.color=messageColor;
                if(calibrationVisible)
                    message.text="足元の枠が体験の空間です\nA か X を長押しで決定（運営: C）";
                else if(Model.Phase==SessionPhase.Ready || Model.Phase==SessionPhase.Finished)
                    message.text=!rig.CanStart ? rig.PreparationMessage : rig.IsVR ? "第零室\n手元の取っ手に手を近づけ、グリップで操作\nA または X ボタンで開始" : "第零室  /  操作確認\nEnter：開始　Space：遮蔽　E：出口\n右ドラッグ：見回す";
                else if(Model.Phase==SessionPhase.Escaped) message.text="脱出した。\n今度は、時間が進んでいる。";
                else if(Model.Phase==SessionPhase.TimedOut) message.text="今回は、脱出できなかった。\n見つけた手がかりは、あなたの記憶に。";
                else if(Model.Phase==SessionPhase.Interrupted) message.text="体験を中断しました。\n接続と周囲を確認してください。";
            }
        }

        void SaveLog()
        {
            saved=true;
            try
            {
                string directory=Path.Combine(Application.persistentDataPath,"Sessions");
                Directory.CreateDirectory(directory);
                var record=new SessionLog { sessionId=sessionId, mode=rig.IsVR?"PCVR":"Desktop",
                    outcome=Model.Outcome.ToString(),elapsed=Model.TotalTime,timings=Model.Rules,events=Model.Records.ToArray() };
                File.WriteAllText(Path.Combine(directory,sessionId+".json"),JsonUtility.ToJson(record,true));
                logMessage="周回記録を保存しました";
            }
            catch(Exception e) { logMessage="記録保存に失敗: "+e.GetType().Name; Debug.LogWarning(logMessage); }
        }

        // In VR, only Esc and lost tracking interrupt; runtime pause notices (dashboard, focus) must not end the session.
        void OnApplicationPause(bool paused) { if(paused && Model!=null && !rig.IsVR) Model.Interrupt(); }

        void OnGUI()
        {
            if(Model==null) return;
            if(title==null)
            {
                var font=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},24);
                title=new GUIStyle(GUI.skin.label){font=font,fontSize=28}; title.normal.textColor=new Color(.9f,.85f,.7f);
                body=new GUIStyle(title){fontSize=18}; small=new GUIStyle(title){fontSize=13};
            }
            bool showRetryHint=(!rig.IsVR || privateOverlay) && rig.CanRetryPreparation;
            bool showBoundaryHint=(!rig.IsVR || privateOverlay) && calibrationVisible && !lastBoundaryAvailable;
            // Stays visible from the moment of commit until the *next* commit, even once the
            // calibration screen itself has closed (task021 追修正3-1).
            bool showOutsideWarning=(!rig.IsVR || privateOverlay) && calibrationOutside;
            float boxHeight=(rig.IsVR&&!privateOverlay?112:showRetryHint?230:206)+(showBoundaryHint?24:0)+(showOutsideWarning?24:0);
            GUI.Box(new Rect(16,16,360,boxHeight),GUIContent.none);
            GUI.Label(new Rect(32,28,340,40),"第零室 / THE ROOM BEFORE",title);
            GUI.Label(new Rect(32,72,340,28),"LOOP "+Model.LoopId.ToString("00")+"  ·  "+PublicState(),body);
            if(!rig.IsVR || privateOverlay)
            {
                float y=107;
                GUI.Label(new Rect(32,y,340,26),rig.CanStart ? "Enter 開始 / Space 遮蔽 / E 出口" : "開始前の接続と追跡を確認中",small);
                y+=24;
                GUI.Label(new Rect(32,y,340,26),calibrating ? "C: キャリブレーションを決定" : "C: キャリブレーションをやり直す（"+(aligned?"済":"未")+"）",small);
                y+=24;
                if(showOutsideWarning) { GUI.Label(new Rect(32,y,340,26),"境界外で決定（要確認）",small); y+=24; }
                if(showBoundaryHint) { GUI.Label(new Rect(32,y,340,26),"境界情報なし（目視で確認）",small); y+=24; }
                if(rig.CanRetryPreparation) { GUI.Label(new Rect(32,y,340,26),"R: VR 再準備（運営）",small); y+=24; }
                GUI.Label(new Rect(32,y,340,26),"右ドラッグ 視点 / Esc 中断 / F2 運営表示",small);
                y+=24;
                GUI.Label(new Rect(32,y,340,26),"S01  "+Model.TotalTime.ToString("F1")+"s  "+logMessage,small);
            }
        }

        string PublicState()
        {
            if(Model.Outcome==SessionPhase.Escaped) return "ESCAPED";
            if(Model.Outcome==SessionPhase.TimedOut) return "SESSION ENDED";
            if(Model.Outcome==SessionPhase.Interrupted) return "PAUSED";
            if(Model.Phase==SessionPhase.Ready) return rig.CanStart ? "READY" : "PREPARING";
            return "IN PROGRESS";
        }
    }
}
