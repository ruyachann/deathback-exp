using System;
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
        AudioSource enemyAudio;

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
            Model = new LoopModel(timings);
            rig = new GameObject("XR Origin").AddComponent<DemoRig>(); rig.transform.SetParent(transform,false);
            rig.Initialize();
            room = new RoomVisuals(); room.Build(transform,rig);
            controls = new[]{room.ShieldHandle,room.ExitHandle};
            room.ShieldHandle.selectEntered.AddListener(_ => RaiseShield());
            room.ExitHandle.selectEntered.AddListener(_ => TryExit());
            // Enemy is inactive until LoopTime>=3, and a disabled AudioSource plays nothing;
            // keep the source on an always-active object that follows the enemy instead.
            enemyAudio = new GameObject("Enemy audio").AddComponent<AudioSource>();
            enemyAudio.transform.SetParent(room.Root,false); enemyAudio.playOnAwake=false;
            enemyAudio.spatialBlend=1; enemyAudio.minDistance=.5f; enemyAudio.maxDistance=10; enemyAudio.volume=.22f;
            room.Sound.transform.position = new Vector3(0,1.4f,.7f);
            room.Sound.spatialBlend=1; room.Sound.minDistance=.4f; room.Sound.maxDistance=8;
            BuildMessage();
            RefreshWorld();
        }

        void BuildMessage()
        {
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic","Meiryo","Arial"},64);
            messagePanel=GameObject.CreatePrimitive(PrimitiveType.Quad);
            messagePanel.name="Ready and ending panel"; messagePanel.layer=RoomVisuals.PrivateLayer;
            messagePanel.transform.SetParent(rig.View.transform,false);
            messagePanel.transform.localPosition=new Vector3(0,.02f,.82f);
            messagePanel.transform.localScale=new Vector3(1.18f,.48f,1);
            Destroy(messagePanel.GetComponent<Collider>());
            messagePanel.GetComponent<Renderer>().material=RoomVisuals.Material(new Color(.02f,.035f,.045f),true);
            var text=new GameObject("Session instructions"); text.layer=RoomVisuals.PrivateLayer;
            text.transform.SetParent(rig.View.transform,false); text.transform.localPosition=new Vector3(0,.02f,.80f);
            message=text.AddComponent<TextMesh>(); message.font=font; message.fontSize=64;
            message.characterSize=.020f; message.anchor=TextAnchor.MiddleCenter; message.alignment=TextAlignment.Center;
            message.color=new Color(.88f,.88f,.77f); text.GetComponent<MeshRenderer>().material=font.material;
        }

        void Update()
        {
            if (Model == null) return;
            bool idle=Model.Phase==SessionPhase.Ready || Model.Phase==SessionPhase.Finished;
            rig.PollMode(idle);
            var keyboard=Keyboard.current;
            bool enter=keyboard!=null && keyboard.enterKey.wasPressedThisFrame;
            if (idle && rig.CanStart && (enter || (rig.IsVR && rig.StartPressed))) Begin();
            else if (idle && keyboard!=null && keyboard.rKey.wasPressedThisFrame && rig.CanRetryPreparation) rig.RetryPreparation();
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
                rig.ClearSelection(); room.Sound.Stop(); enemyAudio.Stop();
                room.Sound.PlayOneShot(room.Chime); lastLoop=Model.LoopId; lastLoopTime=0;
            }
            // Damage/time boundaries are resolved before this frame's fresh input.
            rig.Operate(controls,Model.Phase==SessionPhase.Playing);
            if(!rig.IsVR && Model.Phase==SessionPhase.Playing && keyboard!=null)
            {
                if(keyboard.spaceKey.wasPressedThisFrame) RaiseShield();
                if(keyboard.eKey.wasPressedThisFrame) TryExit();
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

        void Begin()
        {
            if(!rig.CanStart) return;
            // Do not re-center tracking: the user's real position remains unchanged.
            rig.ClearSelection(); trackingLost=0;
            if(rig.IsVR)
            {
                var p=rig.View.transform.position;
                room.Root.position=new Vector3(p.x,0,p.z);
                room.Root.rotation=Quaternion.Euler(0,rig.View.transform.eulerAngles.y,0);
            }
            sessionId=Guid.NewGuid().ToString("N"); saved=false; lastLoop=0; lastRecords=0;
            Model.Start();
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
            bool show=Model.Phase!=SessionPhase.Playing && Model.Phase!=SessionPhase.Blackout;
            messagePanel.SetActive(show); message.gameObject.SetActive(show);
            if(Model.Phase==SessionPhase.Ready || Model.Phase==SessionPhase.Finished)
                message.text=!rig.CanStart ? rig.PreparationMessage : rig.IsVR ? "第零室\n手元の取っ手に手を近づけ、グリップで操作\nA または X ボタンで開始" : "第零室  /  操作確認\nEnter：開始　Space：遮蔽　E：出口\n右ドラッグ：見回す";
            else if(Model.Phase==SessionPhase.Escaped) message.text="脱出した。\n今度は、時間が進んでいる。";
            else if(Model.Phase==SessionPhase.TimedOut) message.text="今回は、脱出できなかった。\n見つけた手がかりは、あなたの記憶に。";
            else if(Model.Phase==SessionPhase.Interrupted) message.text="体験を中断しました。\n接続と周囲を確認してください。";
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
            float boxHeight=rig.IsVR&&!privateOverlay?112:showRetryHint?206:182;
            GUI.Box(new Rect(16,16,360,boxHeight),GUIContent.none);
            GUI.Label(new Rect(32,28,340,40),"第零室 / THE ROOM BEFORE",title);
            GUI.Label(new Rect(32,72,340,28),"LOOP "+Model.LoopId.ToString("00")+"  ·  "+PublicState(),body);
            if(!rig.IsVR || privateOverlay)
            {
                float y=107;
                GUI.Label(new Rect(32,y,340,26),rig.CanStart ? "Enter 開始 / Space 遮蔽 / E 出口" : "開始前の接続と追跡を確認中",small);
                y+=24;
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
