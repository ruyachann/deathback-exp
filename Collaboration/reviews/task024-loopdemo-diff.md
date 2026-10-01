# task024 LoopDemo.cs の差分（計画担当が作成、2026-09-25）

LoopDemo.cs SHA256: 8de1a132266c898876a60d69dd6af849f58df416cf84a5dfee2cde5abd81e85d

```diff
diff --git a/Assets/LoopRoom/Scripts/LoopDemo.cs b/Assets/LoopRoom/Scripts/LoopDemo.cs
index aa74c7c..009f64d 100644
--- a/Assets/LoopRoom/Scripts/LoopDemo.cs
+++ b/Assets/LoopRoom/Scripts/LoopDemo.cs
@@ -279,7 +279,7 @@ namespace LoopRoom
             // A long frame can cross t=3 and the shot together; LoopTime is frozen in Blackout, so the latch still plays.
             if((Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout) && lastLoopTime<3 && Model.LoopTime>=3)
                 enemyAudio.PlayOneShot(room.Latch);
-            if(Model.Phase==SessionPhase.Playing && lastLoopTime<Model.Rules.exitOpens && Model.LoopTime>=Model.Rules.exitOpens)
+            if(Model.Phase==SessionPhase.Playing && lastLoopTime<Model.ExitOpens && Model.LoopTime>=Model.ExitOpens)
                 room.Sound.PlayOneShot(room.Open,.6f);
             if(lastRecords<Model.Records.Count)
             {
@@ -383,7 +383,7 @@ namespace LoopRoom
             room.ExitLamp.material.color=lamp;
             room.ExitLabel.text=Model.ExitAvailable?"脱出可能":"施錠中";
             room.Blackout.SetActive(Model.Phase==SessionPhase.Blackout || trackingLost>0);
-            room.UpdatePublic(rig.IsVR,playing);
+            room.UpdatePublic(rig.IsVR);
             calibration.SetVisible(calibrationVisible);
             if(calibrationVisible)
             {
```

リポジトリ内の Model.Rules の参照（grep）:
```
Assets/LoopRoom/Scripts/LoopDemo.cs:421:                    outcome=Model.Outcome.ToString(),elapsed=Model.TotalTime,timings=Model.Rules,events=Model.Records.ToArray() };
Assets/LoopRoom/Editor/LoopModelChecks.cs:45:            Need(!ReferenceEquals(rules,m.Rules),"rules shared with caller");
Assets/LoopRoom/Editor/LoopModelChecks.cs:46:            Near(m.Rules.firstShot,6);
Assets/LoopRoom/Editor/LoopModelChecks.cs:49:            var m=new LoopModel();var exposed=m.Rules;exposed.firstShot=1;
Assets/LoopRoom/Editor/LoopModelChecks.cs:50:            Need(!ReferenceEquals(exposed,m.Rules),"same rules copy returned twice");
Assets/LoopRoom/Editor/LoopModelChecks.cs:51:            Near(m.Rules.firstShot,6);
```

テスト出力（evidence/20260925-cleanup/tests.txt 抜粋）:
```
csc exit=0
PASS: 100 resets reject old events and clear shield state
PASS: 20 model checks
PASS: 12 room anchor checks
test exit=0
```

desktop 自動実行（evidence/20260925-cleanup/player-log-lines.txt）:
```
LoopRoom: --autostart is active; ignoring focus loss.
LoopRoom: PlaceRoom pos=(0.00,0.00) headYaw=0.0 frontYaw=0.0 corrected=False diff=0.0 fits=True
LoopRoom: PlaceRoom pos=(0.00,0.00) headYaw=0.0 frontYaw=0.0 corrected=False diff=0.0 fits=True
LoopRoom: PlaceRoom pos=(0.00,0.00) headYaw=0.0 frontYaw=0.0 corrected=False diff=0.0 fits=True
LoopRoom: PlaceRoom pos=(0.00,0.00) headYaw=0.0 frontYaw=0.0 corrected=False diff=0.0 fits=True
```
