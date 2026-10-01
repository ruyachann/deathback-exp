using System;
using System.Collections.Generic;
using LoopRoom;

public static class LoopModelChecks
{
    public static List<string> Run()
    {
        var passed=new List<string>();
        Check(passed,"Unprotected attack returns immediately and resets only world state",()=>{
            var m=new LoopModel(); m.Start(); m.Advance(6);
            Need(m.Phase==SessionPhase.Blackout,"death boundary");
            m.Advance(.16);
            Need(m.LoopId==2 && m.Phase==SessionPhase.Playing,"new loop");
            Near(m.TotalTime,6.16); Near(m.LoopTime,0);
            Need(!m.ShieldRaised && !m.ShotResolved,"world reset");
        });
        Check(passed,"Shield grants a real escape window",()=>{
            var m=new LoopModel();m.Start();m.Advance(2);m.RaiseShield(m.LoopId);m.CloseBlinds(m.LoopId);m.Advance(7.5);
            Need(m.ExitAvailable && m.TryExit(m.LoopId),"escape");
            Need(!m.Kill(m.LoopId,"late"),"no death after escape");
            m.Advance(8);Need(m.Phase==SessionPhase.Finished && m.Outcome==SessionPhase.Escaped,"ending");
        });
        Check(passed,"No escape before unlocking or with a stale loop id",()=>{
            var m=new LoopModel();m.Start();Need(!m.TryExit(1),"locked");m.Advance(6.16);
            Need(!m.RaiseShield(1) && !m.Kill(1,"stale"),"stale ignored");
            m.RaiseShield(2);m.CloseBlinds(2);m.Advance(9.5);Need(!m.TryExit(1) && m.TryExit(2),"correct loop only");
        });
        Check(passed,"Repeated death callbacks produce only one reset",()=>{
            var m=new LoopModel();m.Start();Need(m.Kill(1,"one"),"first accepted");
            Need(!m.Kill(1,"two"),"duplicate rejected");m.Advance(.16);Need(m.LoopId==2,"once");
        });
        Check(passed,"Deadline includes blackouts and ends within 180 seconds",()=>{
            var m=new LoopModel(new LoopRules{enforcePlayLimit=true});m.Start();m.Advance(1000);
            Need(m.Phase==SessionPhase.Finished && m.Outcome==SessionPhase.TimedOut,"timeout");Near(m.TotalTime,180);
        });
        Check(passed,"Default rules keep looping without a session deadline",()=>{
            var m=new LoopModel();m.Start();m.Advance(1000);
            Need(m.Phase==SessionPhase.Playing || m.Phase==SessionPhase.Blackout,"session continues");
            Need(m.Outcome==SessionPhase.Ready && m.LoopId>1,"loops continue");
            Near(m.TotalTime,1000);
        });
        Check(passed,"Model owns a copy of its loop rules",()=>{
            var rules=new LoopRules();var m=new LoopModel(rules);rules.firstShot=1;
            Need(!ReferenceEquals(rules,m.Rules),"rules shared with caller");
            Near(m.Rules.firstShot,6);
        });
        Check(passed,"Changing returned rules cannot alter model behavior",()=>{
            var m=new LoopModel();var exposed=m.Rules;exposed.firstShot=1;
            Need(!ReferenceEquals(exposed,m.Rules),"same rules copy returned twice");
            Near(m.Rules.firstShot,6);
            m.Start();m.Advance(1);
            Need(m.Phase==SessionPhase.Playing && !m.ShotResolved,"returned rules changed first shot");
            m.Advance(5);
            Need(m.Phase==SessionPhase.Blackout,"first shot did not occur at original time");
        });
        Check(passed,"Minimum interval boundaries are enforced",()=>{
            var boundary=new LoopRules{
                firstShot=LoopRules.MinInterval,windowShot=LoopRules.MinInterval*2,searchShot=LoopRules.MinInterval*3,
                exitOpens=LoopRules.MinInterval*2,exitCloses=LoopRules.MinInterval*3,
                blackout=LoopRules.MinInterval,playLimit=LoopRules.MinInterval,
                endingLength=LoopRules.MinInterval
            };
            boundary.Validate();
            new LoopRules{
                firstShot=6.0,windowShot=6.05,searchShot=6.1,exitOpens=6.05,exitCloses=6.1
            }.Validate();
            new LoopRules{exitOpens=11.95,exitCloses=12.0}.Validate();
            Action<LoopRules>[] makeTooSmall={
                r=>r.firstShot=LoopRules.MinInterval/2,
                r=>{r.firstShot=LoopRules.MinInterval;r.windowShot=LoopRules.MinInterval*2;
                    r.searchShot=LoopRules.MinInterval*2.5;
                    r.exitOpens=r.windowShot;r.exitCloses=r.searchShot;},
                r=>{r.exitOpens=11.9;r.exitCloses=11.9+LoopRules.MinInterval/2;},
                r=>r.blackout=LoopRules.MinInterval/2,
                r=>r.endingLength=LoopRules.MinInterval/2,
                r=>r.playLimit=LoopRules.MinInterval/2
            };
            string[] names={"firstShot","search interval","exit interval","blackout","endingLength","playLimit"};
            for(int i=0;i<makeTooSmall.Length;i++){
                var rules=new LoopRules();makeTooSmall[i](rules);
                bool bad=false;try{rules.Validate();}catch(ArgumentException){bad=true;}
                Need(bad,names[i]+" accepted below MinInterval");
            }
        });
        Check(passed,"Extremely small loop timings are rejected",()=>{
            var rules=new LoopRules{
                firstShot=1e-200,windowShot=1.5e-200,searchShot=2e-200,exitOpens=1.5e-200,exitCloses=2e-200,
                blackout=1e-200,playLimit=1e-200,endingLength=1e-200
            };
            bool bad=false;try{rules.Validate();}catch(ArgumentException){bad=true;}
            Need(bad,"extremely small timings accepted");
        });
        Check(passed,"Minimum intervals remain bounded at MaxStep",()=>{
            var rules=new LoopRules{
                firstShot=LoopRules.MinInterval,windowShot=LoopRules.MinInterval*2,searchShot=LoopRules.MinInterval*3,
                exitOpens=LoopRules.MinInterval*2,exitCloses=LoopRules.MinInterval*3,
                blackout=LoopRules.MinInterval,playLimit=LoopRules.MinInterval,
                endingLength=LoopRules.MinInterval
            };
            var m=new LoopModel(rules);m.Start();m.Advance(LoopModel.MaxStep);
            Near(m.TotalTime,LoopModel.MaxStep);
        });
        Check(passed,"Advance accepts MaxStep and rejects larger finite deltas without mutation",()=>{
            var m=new LoopModel();m.Start();m.Advance(LoopModel.MaxStep);
            Need(m.Phase==SessionPhase.Playing || m.Phase==SessionPhase.Blackout,"session continues at MaxStep");
            Need(m.Outcome==SessionPhase.Ready && m.LoopId>1,"loops continue at MaxStep");
            Near(m.TotalTime,LoopModel.MaxStep);
            int loop=m.LoopId;double total=m.TotalTime;int records=m.Records.Count;
            bool bad=false;try{m.Advance(LoopModel.MaxStep*2);}catch(ArgumentOutOfRangeException){bad=true;}
            Need(bad,"delta over MaxStep accepted");
            Need(m.LoopId==loop && m.TotalTime==total && m.Records.Count==records,"state changed after MaxStep rejection");
            bad=false;try{m.Advance(double.MaxValue);}catch(ArgumentOutOfRangeException){bad=true;}
            Need(bad,"double.MaxValue accepted");
            Need(m.LoopId==loop && m.TotalTime==total && m.Records.Count==records,"state changed after double.MaxValue rejection");
        });
        Check(passed,"Session deadline validation is conditional",()=>{
            new LoopRules{playLimit=180}.Validate();
            bool bad=false;try{new LoopRules{playLimit=180,enforcePlayLimit=true}.Validate();}catch(ArgumentException){bad=true;}
            Need(bad,"enabled deadline accepted over 180 seconds");
        });
        Check(passed,"Missing escape window allows the enemy to flank",()=>{
            var m=new LoopModel();m.Start();m.RaiseShield(1);m.CloseBlinds(1);m.Advance(12);
            Need(m.Phase==SessionPhase.Blackout && !m.TryExit(1),"flank before late input");
            Need(m.Records[m.Records.Count-1].kind=="flanked","cause");
        });
        Check(passed,"Large and small time steps yield the same no-input outcome",()=>{
            var a=new LoopModel();var b=new LoopModel();a.Start();b.Start();a.Advance(80);
            for(int i=0;i<8000;i++)b.Advance(.01);
            Need(a.LoopId==b.LoopId && a.Phase==b.Phase,"same phase");Near(a.LoopTime,b.LoopTime);
            Need(a.Records.Count==b.Records.Count,"same event count");
        });
        Check(passed,"100 resets reject old events and clear shield state",()=>{
            var m=new LoopModel();m.Start();
            for(int i=0;i<100;i++){
                int old=m.LoopId;m.RaiseShield(old);Need(m.Kill(old,"test"),"kill");m.Advance(.16);
                Need(m.LoopId==old+1 && !m.ShieldRaised && !m.ShotResolved,"clear state");
                Need(!m.Kill(old,"delayed"),"old callback ignored");
            }
            Near(m.TotalTime,16);
        });
        Check(passed,"Stop is distinct from death and does not resume the same session",()=>{
            var m=new LoopModel();m.Start();m.Advance(1);m.Interrupt();m.Advance(8);
            Need(m.Outcome==SessionPhase.Interrupted && m.LoopId==1,"interrupted");
            m.Start();Need(m.LoopId==1 && m.TotalTime==0 && m.Outcome==SessionPhase.Ready,"fresh session");
        });
        Check(passed,"Invalid timing and non-finite delta are rejected",()=>{
            bool bad=false;try{new LoopModel(new LoopRules{blackout=0});}catch(ArgumentException){bad=true;}
            Need(bad,"bad rules");bad=false;try{new LoopModel().Advance(double.NaN);}catch(ArgumentException){bad=true;}
            Need(bad,"bad delta");
        });
        Check(passed,"All eight timings reject NaN and both infinities",()=>{
            string[] names={"firstShot","windowShot","searchShot","exitOpens","exitCloses","blackout","playLimit","endingLength"};
            Action<LoopRules,double>[] setters={
                (r,v)=>r.firstShot=v,(r,v)=>r.windowShot=v,(r,v)=>r.searchShot=v,(r,v)=>r.exitOpens=v,
                (r,v)=>r.exitCloses=v,(r,v)=>r.blackout=v,(r,v)=>r.playLimit=v,(r,v)=>r.endingLength=v
            };
            double[] values={double.NaN,double.PositiveInfinity,double.NegativeInfinity};
            for(int i=0;i<setters.Length;i++)foreach(double value in values){
                var rules=new LoopRules();setters[i](rules,value);
                bool bad=false;try{rules.Validate();}catch(ArgumentException){bad=true;}
                Need(bad,names[i]+" accepted "+value);
            }
        });
        Check(passed,"Window shot kills at t=9 when the shield is up but the blinds are open",()=>{
            var m=new LoopModel();m.Start();m.RaiseShield(1);
            m.Advance(8.5);Need(m.Phase==SessionPhase.Playing && !m.TryExit(1),"alive before the window shot");
            m.Advance(.5);
            Need(m.Phase==SessionPhase.Blackout,"window death");
            var last=m.Records[m.Records.Count-1];
            Need(last.kind=="window_shot" && last.loop==1,"cause "+last.kind);Near(last.loopTime,9);
            m.Advance(.16);Need(m.LoopId==2 && !m.BlindsClosed && !m.ShieldRaised,"world reset after the window death");
        });
        Check(passed,"Closed blinds block the window shot and the flank still comes at t=12",()=>{
            var m=new LoopModel();m.Start();m.Advance(2);
            Need(m.RaiseShield(1) && m.CloseBlinds(1) && m.BlindsClosed,"inputs accepted");
            m.Advance(7);
            Need(m.Phase==SessionPhase.Playing && Near2(m.LoopTime,9),"survives t=9");
            Need(m.Records[m.Records.Count-1].kind=="window_blocked","window_blocked");
            m.Advance(3);
            Need(m.Phase==SessionPhase.Blackout && m.Records[m.Records.Count-1].kind=="flanked","flanked at t=12");
            Near(m.Records[m.Records.Count-1].loopTime,12);
        });
        Check(passed,"Exit opens at t=9.5 after the window shot and stays open until t=12",()=>{
            var m=new LoopModel();m.Start();m.RaiseShield(1);m.CloseBlinds(1);
            m.Advance(9.25);
            Need(!m.ExitAvailable && !m.TryExit(1) && m.Phase==SessionPhase.Playing,"exit before 9.5 must fail");
            m.Advance(.25);Need(m.ExitAvailable,"exit at 9.5");
            m.Advance(2.25);
            Need(m.ExitAvailable && m.TryExit(1) && m.Outcome==SessionPhase.Escaped,"escape late in the window");
            var late=new LoopModel();late.Start();late.RaiseShield(1);late.CloseBlinds(1);late.Advance(9.5);
            Need(late.TryExit(1) && late.Outcome==SessionPhase.Escaped,"escape right at 9.5");
        });
        Check(passed,"Dying to the first shot skips the window shot (blinds do not stop the first shot)",()=>{
            var m=new LoopModel();m.Start();m.Advance(10);
            Need(Kinds(m,1)=="loop_started,first_shot","no input: "+Kinds(m,1));
            Need(m.LoopId==2 && Kinds(m,2)=="loop_started","next loop clean");
            var b=new LoopModel();b.Start();Need(b.CloseBlinds(1),"blinds");b.Advance(10);
            Need(Kinds(b,1)=="loop_started,blinds_closed,first_shot","blinds only: "+Kinds(b,1));
        });
        Check(passed,"One long step from t=5 to t=13 resolves 6, 9, 12 in order",()=>{
            string[] expected={
                "loop_started,first_shot",
                "loop_started,shield_raised,shot_blocked,window_shot",
                "loop_started,blinds_closed,first_shot",
                "loop_started,shield_raised,blinds_closed,shot_blocked,window_blocked,flanked"
            };
            for(int i=0;i<4;i++){
                bool shield=(i&1)!=0,blinds=(i&2)!=0;
                var m=new LoopModel();m.Start();m.Advance(5);
                if(shield)Need(m.RaiseShield(1),"shield");
                if(blinds)Need(m.CloseBlinds(1),"blinds");
                m.Advance(8);
                int index=(shield?1:0)+(blinds?2:0);
                Need(Kinds(m,1)==expected[index],"case "+index+": "+Kinds(m,1));
                Near(m.TotalTime,13);
            }
        });
        Check(passed,"Stale or out-of-phase CloseBlinds is rejected and blinds reset each loop",()=>{
            var idle=new LoopModel();Need(!idle.CloseBlinds(0) && !idle.CloseBlinds(1),"rejected before Start");
            var m=new LoopModel();m.Start();
            Need(!m.CloseBlinds(2),"future loop id rejected");
            Need(m.CloseBlinds(1) && !m.CloseBlinds(1),"accepted once per loop");
            m.Advance(6);Need(m.Phase==SessionPhase.Blackout,"first shot kills despite the blinds");
            Need(!m.CloseBlinds(1),"rejected during blackout");
            m.Advance(.16);
            Need(m.LoopId==2 && !m.BlindsClosed,"blinds back up in the new loop");
            Need(!m.CloseBlinds(1) && !m.BlindsClosed,"stale loop id rejected");
            Need(m.CloseBlinds(2) && m.BlindsClosed,"current loop accepted");
        });
        Check(passed,"SameCauseStreak counts repeats, restarts on a new cause, and ignores escape/interrupt",()=>{
            var m=new LoopModel();m.Start();
            Need(m.LastDeathCause==null && m.SameCauseStreak==0,"clean start");
            m.Advance(6);Need(m.LastDeathCause==null && m.SameCauseStreak==0,"updated only when the next loop begins");
            m.Advance(.16);Need(m.LastDeathCause=="first_shot" && m.SameCauseStreak==1,"first death");
            m.Advance(6.16);Need(m.LastDeathCause=="first_shot" && m.SameCauseStreak==2,"second same death");
            m.Advance(6.16);Need(m.LastDeathCause=="first_shot" && m.SameCauseStreak==3,"third same death");
            m.RaiseShield(m.LoopId);m.Advance(9.16);
            Need(m.LastDeathCause=="window_shot" && m.SameCauseStreak==1,"different cause resets to 1");
            m.RaiseShield(m.LoopId);m.Advance(9.16);
            Need(m.LastDeathCause=="window_shot" && m.SameCauseStreak==2,"window repeat");
            m.RaiseShield(m.LoopId);m.CloseBlinds(m.LoopId);m.Advance(9.5);
            Need(m.TryExit(m.LoopId),"escape");
            Need(m.LastDeathCause=="window_shot" && m.SameCauseStreak==2,"escape leaves the streak alone");
            m.Advance(8);m.Start();
            Need(m.LastDeathCause==null && m.SameCauseStreak==0,"fresh session clears the streak");
            var n=new LoopModel();n.Start();n.Advance(6.16);n.Interrupt();
            Need(n.LastDeathCause=="first_shot" && n.SameCauseStreak==1,"interrupt leaves the streak alone");
        });
        Check(passed,"windowShot must sit at least MinInterval inside (firstShot, searchShot)",()=>{
            Near(new LoopRules().windowShot,9);Near(new LoopRules().exitOpens,9.5);
            new LoopRules{windowShot=6.0+LoopRules.MinInterval}.Validate();
            new LoopRules{windowShot=12.0-LoopRules.MinInterval,exitOpens=12.0-LoopRules.MinInterval}.Validate();
            Action<LoopRules>[] bad={
                r=>r.windowShot=r.firstShot,
                r=>r.windowShot=5.0,
                r=>r.windowShot=r.firstShot+LoopRules.MinInterval/2,
                r=>r.windowShot=r.searchShot,
                r=>r.windowShot=13.0,
                r=>r.windowShot=r.searchShot-LoopRules.MinInterval/2
            };
            string[] names={"windowShot==firstShot","windowShot<firstShot","windowShot just after firstShot",
                "windowShot==searchShot","windowShot>searchShot","windowShot just before searchShot"};
            for(int i=0;i<bad.Length;i++){
                var rules=new LoopRules();bad[i](rules);
                bool threw=false;try{rules.Validate();}catch(ArgumentException){threw=true;}
                Need(threw,names[i]+" accepted");
            }
        });
        Check(passed,"exitOpens before windowShot is rejected so the window threat cannot be skipped",()=>{
            new LoopRules{exitOpens=9.0}.Validate();
            Action<LoopRules>[] bad={
                r=>r.exitOpens=8.99,
                r=>{r.exitOpens=6.5;r.exitCloses=7.0;},
                r=>{r.exitOpens=r.firstShot;r.exitCloses=r.windowShot;},
                r=>r.exitOpens=r.windowShot-LoopRules.MinInterval/2
            };
            string[] names={"exitOpens=8.99","exitOpens=6.5/exitCloses=7","exit window entirely before the window shot","exitOpens just before windowShot"};
            for(int i=0;i<bad.Length;i++){
                var rules=new LoopRules();bad[i](rules);
                bool threw=false;try{new LoopModel(rules);}catch(ArgumentException){threw=true;}
                Need(threw,names[i]+" accepted");
            }
        });
        Check(passed,"Kill with a null cause is recorded as unknown and still counts toward the streak",()=>{
            var m=new LoopModel();m.Start();
            Need(m.Kill(1,null),"kill accepted");
            Need(m.Records[m.Records.Count-1].kind=="unknown","recorded as unknown");
            m.Advance(.16);
            Need(m.LastDeathCause=="unknown" && m.SameCauseStreak==1,"first unknown death");
            Need(m.Kill(2,null),"second kill accepted");m.Advance(.16);
            Need(m.LastDeathCause=="unknown" && m.SameCauseStreak==2,"second unknown death");
            Need(m.Kill(3,"first_shot"),"third kill accepted");m.Advance(.16);
            Need(m.LastDeathCause=="first_shot" && m.SameCauseStreak==1,"named cause resets the streak");
        });
        return passed;
    }

    static string Kinds(LoopModel m,int loop){
        string s="";
        foreach(var r in m.Records)if(r.loop==loop)s+=(s.Length>0?",":"")+r.kind;
        return s;
    }
    static bool Near2(double a,double b)=>Math.Abs(a-b)<.00001;
    static void Check(List<string> list,string name,Action test){test();list.Add("PASS: "+name);}
    static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Near(double a,double b){Need(Math.Abs(a-b)<.00001,"Expected "+a+" ~ "+b);}
}
