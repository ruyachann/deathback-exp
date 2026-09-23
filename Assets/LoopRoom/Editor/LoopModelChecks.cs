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
            var m=new LoopModel();m.Start();m.Advance(2);m.RaiseShield(m.LoopId);m.Advance(4.5);
            Need(m.ExitAvailable && m.TryExit(m.LoopId),"escape");
            Need(!m.Kill(m.LoopId,"late"),"no death after escape");
            m.Advance(8);Need(m.Phase==SessionPhase.Finished && m.Outcome==SessionPhase.Escaped,"ending");
        });
        Check(passed,"No escape before unlocking or with a stale loop id",()=>{
            var m=new LoopModel();m.Start();Need(!m.TryExit(1),"locked");m.Advance(6.16);
            Need(!m.RaiseShield(1) && !m.Kill(1,"stale"),"stale ignored");
            m.RaiseShield(2);m.Advance(6.5);Need(!m.TryExit(1) && m.TryExit(2),"correct loop only");
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
        Check(passed,"Minimum interval boundaries are enforced",()=>{
            var boundary=new LoopRules{
                firstShot=LoopRules.MinInterval,searchShot=LoopRules.MinInterval*2,
                exitOpens=LoopRules.MinInterval,exitCloses=LoopRules.MinInterval*2,
                blackout=LoopRules.MinInterval,playLimit=LoopRules.MinInterval,
                endingLength=LoopRules.MinInterval
            };
            boundary.Validate();
            new LoopRules{
                firstShot=6.0,searchShot=6.05,exitOpens=6.0,exitCloses=6.05
            }.Validate();
            new LoopRules{exitOpens=6.5,exitCloses=6.55}.Validate();
            Action<LoopRules>[] makeTooSmall={
                r=>r.firstShot=LoopRules.MinInterval/2,
                r=>{r.firstShot=LoopRules.MinInterval;r.searchShot=LoopRules.MinInterval*1.5;
                    r.exitOpens=r.firstShot;r.exitCloses=r.searchShot;},
                r=>{r.exitOpens=6.5;r.exitCloses=6.5+LoopRules.MinInterval/2;},
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
                firstShot=1e-200,searchShot=2e-200,exitOpens=1e-200,exitCloses=2e-200,
                blackout=1e-200,playLimit=1e-200,endingLength=1e-200
            };
            bool bad=false;try{rules.Validate();}catch(ArgumentException){bad=true;}
            Need(bad,"extremely small timings accepted");
        });
        Check(passed,"Minimum intervals remain bounded at MaxStep",()=>{
            var rules=new LoopRules{
                firstShot=LoopRules.MinInterval,searchShot=LoopRules.MinInterval*2,
                exitOpens=LoopRules.MinInterval,exitCloses=LoopRules.MinInterval*2,
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
            var m=new LoopModel();m.Start();m.RaiseShield(1);m.Advance(12);
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
        Check(passed,"All seven timings reject NaN and both infinities",()=>{
            string[] names={"firstShot","searchShot","exitOpens","exitCloses","blackout","playLimit","endingLength"};
            Action<LoopRules,double>[] setters={
                (r,v)=>r.firstShot=v,(r,v)=>r.searchShot=v,(r,v)=>r.exitOpens=v,
                (r,v)=>r.exitCloses=v,(r,v)=>r.blackout=v,(r,v)=>r.playLimit=v,(r,v)=>r.endingLength=v
            };
            double[] values={double.NaN,double.PositiveInfinity,double.NegativeInfinity};
            for(int i=0;i<setters.Length;i++)foreach(double value in values){
                var rules=new LoopRules();setters[i](rules,value);
                bool bad=false;try{rules.Validate();}catch(ArgumentException){bad=true;}
                Need(bad,names[i]+" accepted "+value);
            }
        });
        return passed;
    }

    static void Check(List<string> list,string name,Action test){test();list.Add("PASS: "+name);}
    static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Near(double a,double b){Need(Math.Abs(a-b)<.00001,"Expected "+a+" ~ "+b);}
}
