using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using ShanHai;

static class CoreTests
{
    static int assertions, paths;
    static JavaScriptSerializer json=new JavaScriptSerializer {MaxJsonLength=4000000};
    static HashSet<string> reached=new HashSet<string>();
    static int[] endingCounts=new int[4];
    static void Check(bool value,string label) {assertions++;if(!value)throw new Exception(label);}
    static SaveState Copy(SaveState s) {return json.Deserialize<SaveState>(json.Serialize(s));}
    static void FinishNode(StoryEngine engine)
    {
        while(!engine.DialogueFinished)engine.ReadLine();
        if(engine.NeedsMechanic)engine.FinishMechanic();
    }
    static void Walk(StoryData data,SaveState state,int depth)
    {
        Check(depth<45,"Graph must terminate");
        var engine=new StoryEngine(data,new PlayerProfile());engine.Restore(Copy(state));
        reached.Add(engine.Node.id);
        FinishNode(engine);
        if(engine.IsEnding)
        {
            engine.Complete();int ending=engine.Get("ending");
            Check(ending==(engine.Get("mcv")>=60?1:engine.Get("mcv")<=40?2:3),"Ending threshold");
            Check(engine.Profile.endings.Contains(ending),"Collection only after ending completed");
            int fragments=engine.Get("fragments");
            Check(fragments==(engine.State.route=="玄曜"?2:4),"Route fragment count");
            Check(engine.Get("pacts")==1,"No duplicated pacts");
            var before=json.Serialize(engine.State.values);engine.Complete();
            Check(before==json.Serialize(engine.State.values),"Completion is idempotent");
            paths++;endingCounts[ending]++;return;
        }
        var choices=engine.AvailableChoices();
        if(choices.Length>0)
        {
            var before=Copy(engine.State);
            foreach(var choice in choices)
            {
                engine.Restore(Copy(before));engine.Select(choice.id);Walk(data,engine.State,depth+1);
            }
        }
        else {engine.Advance();Walk(data,engine.State,depth+1);}
    }
    static void TestInvestigations()
    {
        foreach(var def in InvestigationRules.All)
        {
            var state=new SaveState();InvestigationRules.Prepare(state,def.id);
            var inv=state.investigation;
            Check(!InvestigationRules.IsSolved(inv),def.id+" starts gated");
            state.items.Add("wrong-tool");Check(!InvestigationRules.UseTool(state,"wrong-tool"),"Wrong tool rejected");
            Check(state.items.Contains("wrong-tool")&&!inv.prepared,"Wrong tool not consumed");
            if(!state.items.Contains(def.tool))state.items.Add(def.tool);
            if(def.id=="N401")
            {
                Check(!InvestigationRules.UseTool(state,"fan"),"Fan alone cannot bypass clearing each vent");
                Check(!InvestigationRules.ClearVent(state,-1)&&!InvestigationRules.ClearVent(state,3),"Invalid mouth is rejected");
                InvestigationRules.ToggleVent(inv,0);Check(inv.dials[0]==0,"Ash blocks shutter operation");
                Check(InvestigationRules.ClearVent(state,0),"First mouth clears");
                Check(!InvestigationRules.ClearVent(state,0),"Clearing same mouth does not count twice");
                var partial=Copy(state);InvestigationRules.Prepare(partial,"N401");
                Check(InvestigationRules.VentCleared(partial.investigation,0)&&!InvestigationRules.VentCleared(partial.investigation,1),"Partial ash mask survives save and prepare");
                Check(partial.items.Contains("fan")&&!partial.investigation.prepared,"Fan retained until all mouths clear");
                Check(InvestigationRules.ClearVent(state,2)&&!inv.prepared,"Two mouths cannot unlock shutters");
                Check(InvestigationRules.ClearVent(state,1)&&inv.prepared&&!state.items.Contains("fan"),"All three mouths unlock shutters and put fan away");
                var oldSave=Copy(state);oldSave.investigation.progress=0;
                for(int i=0;i<3;i++)Check(InvestigationRules.VentCleared(oldSave.investigation,i),"Version 0.2 prepared save remains playable");
            }
            else Check(InvestigationRules.UseTool(state,def.tool),"Correct tool prepares mechanism");
            Check(!InvestigationRules.UseTool(state,def.tool),"Cannot consume twice");
            if(def.id=="N101")
            {inv.dials[0]=0;var loaded=Copy(state);InvestigationRules.Prepare(loaded,def.id);Check(loaded.investigation.dials[0]==0,"Reload keeps partial mirror");inv.dials[1]=0;inv.dials[2]=1;}
            else if(def.id=="N201"){inv.dials[0]=2;inv.dials[1]=0;inv.dials[2]=3;}
            else if(def.id=="N301")
            {InvestigationRules.PressLamp(inv,2);InvestigationRules.PressLamp(inv,1);Check(inv.progress==0,"Wrong sequence resets recoverably");foreach(int lamp in new[]{2,0,3,1})InvestigationRules.PressLamp(inv,lamp);}
            else if(def.id=="N401"){InvestigationRules.ToggleVent(inv,0);Check(!InvestigationRules.IsSolved(inv),"Single vent leaves center blowing");InvestigationRules.ToggleVent(inv,2);}
            else if(def.id=="N403"){foreach(int i in new[]{0,3,6,7,8})inv.dials[i]=0;Check(InvestigationRules.RiverPath(inv.dials).Contains(9),"Connected river reaches outlet");}
            else if(def.id=="N501"){InteractionRules.Swap(inv.dials,0,2);Check(InvestigationRules.Torque(inv.dials)==0,"Moment arms balance");}
            else if(def.id=="N601"){inv.dials[0]=0;inv.dials[1]=2;inv.dials[2]=1;}
            else if(def.id=="N801")
            {Check(!InvestigationRules.BridgeStep(inv,3,0),"Cannot skip to summit");Check(!InvestigationRules.BridgeStep(inv,8,1),"Wrong scroll cannot repair crack");for(int i=0;i<6;i++)Check(InvestigationRules.BridgeStep(inv,InvestigationRules.BridgeCells[i],i%2),"Alternating bridge step");}
            Check(InvestigationRules.IsSolved(inv),def.id+" solvable by real rules");
            inv.solved=true;state.evidence.Add(def.id);
            var roundtrip=Copy(state);InvestigationRules.Prepare(roundtrip,def.id);
            Check(roundtrip.investigation.solved&&roundtrip.evidence.Contains(def.id),"Puzzle and evidence survive save");
        }
    }
    public static void Main(string[] args)
    {
        TestInvestigations();
        var data=json.Deserialize<StoryData>(File.ReadAllText(args[0]));
        Check(data.nodes.Length==36,"All source nodes imported");Check(data.edges.Length==38,"All source edges imported");
        var e=new StoryEngine(data,new PlayerProfile());
        Check(e.Get("mcv")==50,"Initial MCV");Check(e.Profile.endings.Count==0,"No ending on start");
        bool blocked=false;try{e.Advance();}catch(InvalidOperationException){blocked=true;}Check(blocked,"Cannot skip unread dialogue");
        Walk(data,e.State,0);
        Check(paths==24,"All 24 choice paths");Check(reached.Count==36,"Every node reachable");
        Check(endingCounts[1]==10&&endingCounts[2]==4&&endingCounts[3]==10,"Expected ending distribution");
        // Save during a partially completed interaction, restore, and award once.
        var test=Copy(e.State);test.nodeId="N102";test.line=data.nodes.First(n=>n.id=="N102").lines.Length;test.miniStep=7;
        e.Restore(test);Check(e.NeedsMechanic,"Restored unfinished writing still gates progression");
        blocked=false;try{e.Advance();}catch(InvalidOperationException){blocked=true;}Check(blocked,"Minigame cannot be bypassed");
        e.FinishMechanic();e.Complete();Check(e.Get("mcv")==58,"Writing rewards once");
        var save=Copy(e.State);e.Restore(save);e.Complete();Check(e.Get("mcv")==58,"Loading never re-applies reward");
        Check(e.State.miniStep==7,"Stroke progress roundtrip");
        e.Set("mcv",-99);Check(e.Get("mcv")==0,"Lower clamp");e.Set("mcv",199);Check(e.Get("mcv")==100,"Upper clamp");
        foreach(int value in new[]{0,40,41,59,60,100})
        {
            e.NewGame();var s=Copy(e.State);s.nodeId="N901";s.line=data.nodes.First(n=>n.id=="N901").lines.Length;e.Restore(s);e.Set("mcv",value);
            string next=e.Advance();Check(next==(value>=60?"N911":value<=40?"N912":"N913"),"Exact threshold "+value);
            Check(e.Profile.endings.Count==0,"Routing does not unlock an unread ending");
        }
        foreach(int value in new[]{0,26,42,50,66,82,100})
        {
            e.NewGame();var s=Copy(e.State);s.nodeId="N803";s.line=data.nodes.First(n=>n.id=="N803").lines.Length;e.Restore(s);e.Set("mcv",value);e.Select("final_balance");
            Check(e.Get("mcv")==value+Math.Sign(50-value)*Math.Min(20,Math.Abs(50-value)),"Balance must not overshoot midpoint");
        }
        var guide=new[]{new Point2(0,0),new Point2(100,0),new Point2(100,100)};
        Check(InteractionRules.JudgeStroke(guide,guide,12),"Correct stroke");
        Check(!InteractionRules.JudgeStroke(new[]{new Point2(0,0),new Point2(100,100)},guide,12),"Reject diagonal shortcut");
        Check(!InteractionRules.JudgeStroke(guide.Reverse().ToArray(),guide,12),"Reject backwards stroke");
        Check(!InteractionRules.JudgeStroke(new[]{new Point2(0,0)},guide,12),"Reject tap");
        var tiles=new List<int>{4,2,0,5,1,3};Check(!InteractionRules.IsSolved(tiles),"Puzzle starts unsolved");
        for(int i=0;i<6;i++)InteractionRules.Swap(tiles,i,tiles.IndexOf(i));Check(InteractionRules.IsSolved(tiles),"Puzzle can be solved by swaps");
        var invalid=Copy(e.State);invalid.nodeId="missing";blocked=false;try{e.Restore(invalid);}catch(ArgumentException){blocked=true;}Check(blocked,"Reject invalid saves");
        Console.WriteLine("PASS: "+assertions+" assertions; "+paths+" complete paths; "+reached.Count+" reachable nodes; endings="+string.Join(",",endingCounts.Skip(1)));
    }
}
