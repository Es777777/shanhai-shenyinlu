using System;
using System.Linq;
using UnityEngine;
using P=ShanHai.PaintUI;

namespace ShanHai
{
    public sealed partial class StoryGame
    {
        string investigationFocus="",selectedItem="",investigationMessage="";
        int selectedStone=-1,activeScroll;
        Vector2 notesScroll;
        float clueGlintUntil,worldSolvedAt=-100,worldPreparedAt=-100;
        InvestigationState Investigation { get { return engine.State.investigation; } }
        void ResetInvestigationUI()
        {investigationFocus=selectedItem=investigationMessage="";selectedStone=-1;activeScroll=0;notesScroll=Vector2.zero;clueGlintUntil=0;worldSolvedAt=worldPreparedAt=-100;ResetDanxue();ResetWorldVisuals();}
        void Prop(Rect r,int sprite,Color? color=null){Atlas(r,"Props/investigation-atlas",sprite,4,2,color??Color.white);}
        void RubbingArt(Rect r,int sprite){Atlas(r,"Props/rubbing-atlas",sprite,2,1,Color.white);}
        void Atlas(Rect r,string path,int cell,int cols,int rows,Color color,float rotation=0)
        {
            var tex=Art(path);if(tex==null)return;
            var matrix=GUI.matrix;var old=GUI.color;
            try
            {
                GUI.color=color;
                if(Mathf.Abs(rotation)>.01f)GUI.matrix=matrix*Matrix4x4.TRS(r.center,Quaternion.Euler(0,0,rotation),Vector3.one)*Matrix4x4.Translate(-r.center);
                float w=1f/cols,h=1f/rows,inset=.001f;
                GUI.DrawTextureWithTexCoords(r,tex,new Rect(cell%cols*w+inset,1-(cell/cols+1)*h+inset,w-2*inset,h-2*inset));
            }
            finally{GUI.matrix=matrix;GUI.color=old;}
        }
        void Stone(Rect r,int cell,float rotation=0,float alpha=1)
        {Atlas(r,"Investigations/mechanism-stone-dry",cell,4,2,new Color(1,1,1,alpha),rotation);}
        void JourneyItem(Rect r,int cell,float alpha=1)
        {Atlas(r,"Investigations/journey-items",cell,4,2,new Color(1,1,1,alpha));}
        void PaperArt(Rect r,int cell)
        {Atlas(r,"Investigations/workshop-paper",cell,2,2,Color.white);}
        void ObserveClue(InvestigationDefinition d)
        {
            investigationFocus="clue";
            if((d.id=="N201"||d.id=="N403")&&!Investigation.prepared)
            {investigationMessage=d.id=="N201"?"冰霜遮住了铭文。石台上的余烬提灯能让它重新清晰。":"河碑被浮沙盖住。先找碑脚的拂尘，轻轻清理石面。";return;}
            if(!engine.State.evidence.Contains(d.id))
            {
                engine.State.evidence.Add(d.id);
                engine.State.history.Add(new HistoryEntry{node=d.id,speaker="线索 · "+d.clueTitle,text=d.clue});
                audioBus.Sound("puzzle");Persist();
            }
            investigationMessage=d.clue;
        }
        void CheckInvestigationSolved(InvestigationDefinition d)
        {
            if(Investigation.solved||!InvestigationRules.IsSolved(Investigation))return;
            Investigation.solved=true;worldSolvedAt=Time.unscaledTime;
            investigationMessage=d.success;audioBus.Sound("reveal");
            engine.State.history.Add(new HistoryEntry{node=d.id,speaker="解谜 · "+d.title,text=d.success});Persist();
        }
        void FinishInvestigation(InvestigationDefinition d)
        {
            if(!Investigation.solved){investigationMessage="前路尚未打开。先观察场景里的痕迹，再尝试合适的器物。";return;}
            foreach(var target in engine.Node.targets)
                if(!engine.State.inspected.Contains(target.id)){engine.State.inspected.Add(target.id);engine.State.history.Add(new HistoryEntry{node=d.id,speaker="见闻 · "+target.title,text=target.text});}
            investigationFocus="";DoneMechanic(d.success);
        }
        void DrawInvestigation()
        {
            var d=InvestigationRules.Find(engine.Node.id);if(d==null)return;
            InvestigationRules.Prepare(engine.State,d.id);
            if(d.id=="N401")DrawDanxue(d);else DrawWorldInvestigation(d);
        }
        // Only the explicit notebook is a reading overlay. World clues and mechanisms stay in the scene.
        void DrawInvestigationDetail(InvestigationDefinition d)
        {
            P.Fill(new Rect(0,62,1600,788),new Color(.015f,.025f,.025f,.75f));
            P.Image(new Rect(155,145,1290,650),Art("Investigations/worktable"));
            P.Label(new Rect(215,180,1000,50),"山 海 线 索 手 记",30,P.Gold);
            if(P.Button(new Rect(1245,155,155,95),"收起",false,true,27)){investigationFocus="";return;}
            Rect view=new Rect(215,257,1165,490);float total=0;
            P.Fill(new Rect(205,247,1190,510),new Color(.025f,.02f,.015f,.45f));
            foreach(string id in engine.State.evidence){var note=InvestigationRules.Find(id);if(note!=null)total+=P.ReadingStyle(27).CalcHeight(new GUIContent(note.clue),1100)+88;}
            ReadingScroll(view,ref notesScroll,total);
            notesScroll=GUI.BeginScrollView(view,notesScroll,new Rect(0,0,1120,Math.Max(455,total)));
            int index=0;float y=0;
            foreach(string id in engine.State.evidence)
            {
                var note=InvestigationRules.Find(id);if(note==null)continue;
                float body=P.ReadingStyle(27).CalcHeight(new GUIContent(note.clue),1100);
                P.ReadingLabel(new Rect(6,y,1100,60),note.clueTitle,27,P.Gold);
                P.ReadingLabel(new Rect(6,y+68,1100,body),note.clue,27,P.Paper);index++;y+=body+88;
            }
            if(index==0)P.Label(new Rect(15,35,1080,130),"尚未发现线索。回到场景，观察岩壁、器物和它们留下的痕迹。",26,P.Paper);
            GUI.EndScrollView();
        }
    }
}
