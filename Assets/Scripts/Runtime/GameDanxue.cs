using System;
using UnityEngine;
using P=ShanHai.PaintUI;

namespace ShanHai
{
    public sealed partial class StoryGame
    {
        // All hit areas and layers are registered to features in danxue-room.png.
        static readonly float[] VentX={808,1045,1286};
        static readonly Rect FanOnLedge=new Rect(320,565,155,28);
        static readonly Rect MuralHit=new Rect(160,135,450,375);
        static readonly Rect StairHit=new Rect(1408,307,186,236);
        readonly float[] ventOpening=new float[3],ashRemaining=new float[3],dustStart={-100,-100,-100};
        bool danxueVisualReady;
        int fanVent=-1,fanDirection,fanTurns;
        float fanDistance,fanLastX,phoenixStart=-100;
        Vector2 fanPointer;
        void CancelFanStroke(){fanVent=-1;fanDistance=0;fanDirection=fanTurns=0;CancelWorldGesture();}
        void ResetDanxue(){CancelFanStroke();danxueVisualReady=false;phoenixStart=-100;for(int i=0;i<3;i++)dustStart[i]=-100;}
        Rect VentHit(int i){return new Rect(VentX[i]-80,505,160,160);}
        void DanxueLayer(Rect r,int cell,float alpha=1,float shade=1)
        {
            var tex=Art("Investigations/danxue-layers");if(tex==null)return;
            Color old=GUI.color;GUI.color=new Color(shade,shade,shade,alpha);
            // Inset UVs prevent filtering a neighboring sprite at atlas cell boundaries.
            GUI.DrawTextureWithTexCoords(r,tex,new Rect((cell%2)*.5f+.002f,cell<2?.502f:.002f,.496f,.496f));GUI.color=old;
        }
        void DrawDanxueHUD()
        {
            P.Fill(new Rect(0,0,Width,62),new Color(.015f,.035f,.04f,.84f));
            P.Label(new Rect(42,9,530,42),engine.Node.title,24,P.Paper,TextAnchor.MiddleLeft);
            P.Label(new Rect(620,16,280,32),"碎片 "+engine.Get("fragments")+"    烛火 "+engine.Get("candle"),18,P.Muted,TextAnchor.MiddleRight);
            P.Label(new Rect(985,6,190,34),"明昧  "+engine.Get("mcv"),21,P.Gold,TextAnchor.MiddleCenter);
            P.Fill(new Rect(992,48,180,3),P.Muted);P.Fill(new Rect(992,48,180*engine.Get("mcv")/100f,3),P.Red);
            if(P.Button(new Rect(1240,11,135,39),"图鉴",false,true,21))OpenPanel("codex");
            if(P.Button(new Rect(1395,11,155,39),"行卷菜单",false,true,21))OpenPanel("menu");
        }
        void DrawDanxueBackground()
        {
            bool nextScene=engine.Node.id=="N402";
            bool solved=nextScene||(Investigation!=null&&Investigation.node=="N401"&&Investigation.solved);
            P.Image(new Rect(0,0,Width,Height),Art("Investigations/danxue-room"));
            if(solved)
            {
                float blend=profile.reducedMotion||phoenixStart<0?1:Mathf.SmoothStep(0,1,(Time.unscaledTime-phoenixStart)/3f);
                Color old=GUI.color;GUI.color=new Color(1,1,1,blend);
                P.Image(new Rect(0,0,Width,Height),Art("Investigations/danxue-awake"));GUI.color=old;
            }
            if(nextScene)return;
            bool prepared=Investigation!=null&&Investigation.node=="N401"&&Investigation.prepared;
            bool sameNode=Investigation!=null&&Investigation.node=="N401"&&Investigation.dials.Count>=3;
            if(!danxueVisualReady)
            {
                for(int i=0;i<3;i++)
                {ventOpening[i]=sameNode?Investigation.dials[i]:0;ashRemaining[i]=sameNode&&InvestigationRules.VentCleared(Investigation,i)?0:1;}
                danxueVisualReady=true;
            }
            if(!prepared&&!engine.State.items.Contains("fan"))DanxueLayer(FanOnLedge,1);
            for(int i=0;i<3;i++)
            {
                float target=sameNode?Investigation.dials[i]:0;
                float ash=sameNode&&InvestigationRules.VentCleared(Investigation,i)?0:1;
                if(fanVent==i)ash*=1-Mathf.Clamp01(fanDistance/340f)*.65f;
                if(Event.current.type==EventType.Repaint)
                {
                    ventOpening[i]=profile.reducedMotion?target:Mathf.MoveTowards(ventOpening[i],target,Time.unscaledDeltaTime*2);
                    ashRemaining[i]=profile.reducedMotion?ash:Mathf.MoveTowards(ashRemaining[i],ash,Time.unscaledDeltaTime*2.5f);
                }
                Rect aperture=new Rect(VentX[i]-68,514,136,146);
                // A physical shutter withdraws into the stone lintel, exposing the painted hole.
                GUI.BeginGroup(aperture);
                DanxueLayer(new Rect(0,-151*ventOpening[i],136,146),0,1,.80f);
                GUI.EndGroup();
                if(ashRemaining[i]>.005f)DanxueLayer(new Rect(VentX[i]-68,516,136,140),2,ashRemaining[i],.66f);
                float dustAge=Time.unscaledTime-dustStart[i];
                if(!profile.reducedMotion&&dustAge<1.4f)
                    DanxueLayer(new Rect(VentX[i]-85-dustAge*20,500-dustAge*50,170+dustAge*40,165+dustAge*20),3,(1-dustAge/1.4f)*.38f);
                if(ventOpening[i]>.2f&&!profile.reducedMotion)DrawVentEmbers(i,ventOpening[i]);
            }
        }
        void DrawVentEmbers(int vent,float amount)
        {
            // Small embers follow air from each actual mouth up to the nest.
            for(int p=0;p<10;p++)
            {
                float t=Mathf.Repeat(Time.unscaledTime*.32f+p*.101f+vent*.17f,1);
                float x=Mathf.Lerp(VentX[vent],1050,t)+Mathf.Sin(t*8+p)*12*(1-t);
                float y=560-t*280;
                P.Fill(new Rect(x,y,2+(p%2),2+(p%2)),new Color(1,.48f,.12f,Mathf.Sin(t*Mathf.PI)*.68f*amount));
            }
        }
        string DanxueDefaultMessage()
        {
            if(Investigation.solved)return "双翼托起新生的凤凰。右侧石阶已经可以通行。";
            if(Investigation.prepared)return "风道已经清开。试着拨动风闸，观察哪些洞口同时开合。";
            if(engine.State.items.Contains("fan"))return "在积灰的风口前按住并左右扫动，逐个拂去余烬。";
            return "火羽在巢中颤动，风却进不去。石壁、石台与炉口之间，似乎藏着关联。";
        }
        void DrawDanxue(InvestigationDefinition d)
        {
            bool interactive=GUI.enabled&&investigationFocus!="notes";
            if(!interactive)CancelFanStroke();
            string hover="";
            Vector2 pointer=Event.current.mousePosition;
            if(interactive)
            {
                if(MuralHit.Contains(pointer))hover="石壁上的凤凰展着双翼，胸前的羽纹却静静收拢。点击细看。";
                if(StairHit.Contains(pointer))hover=Investigation.solved?"沿右侧石阶继续行卷。":"热流封着石阶。先让凤巢中的火稳定下来。";
                bool hasFan=engine.State.items.Contains("fan");
                Rect fanHit=new Rect(300,553,194,65);
                if(!hasFan&&!Investigation.prepared&&fanHit.Contains(pointer))hover="石台上搁着一柄蒲叶扇。点击拾取。";
                if(GUI.Button(MuralHit,GUIContent.none,GUIStyle.none))
                {
                    ObserveClue(d);investigationFocus="";
                    investigationMessage="壁画旁刻着：两翼同振，腹息无风。左右闸各连腹闸，腹闸牵动三处。";
                }
                if(!hasFan&&!Investigation.prepared&&GUI.Button(fanHit,GUIContent.none,GUIStyle.none))
                {
                    engine.State.items.Add("fan");selectedItem="fan";audioBus.Sound("puzzle");
                    investigationMessage="拿起蒲叶扇。在三个炉口前按住并左右扫动，把堵住风道的灰拂开。";Persist();
                }
                if(GUI.Button(StairHit,GUIContent.none,GUIStyle.none))
                {
                    if(Investigation.solved)FinishInvestigation(d);
                    else investigationMessage="石阶被翻涌的热流封住。先想办法让凤巢两侧的气流平稳下来。";
                }
                for(int i=0;i<3;i++)
                {
                    Rect r=VentHit(i);bool clear=InvestigationRules.VentCleared(Investigation,i);
                    if(r.Contains(pointer))hover=Investigation.solved?"两翼生风，腹闸静闭。火羽终于舒展开来。":!clear?(hasFan?"按住左右扫动，让灰从这个风口散开。":"旧羽与灰封住了炉口。徒手碰不得，要借一件器物。"):Investigation.prepared?"点击拨动风闸，观察相连的洞口。":"这里已经清开。还有其他风口被灰封住。";
                    if(!clear&&hasFan&&selectedItem=="fan")HandleFanStroke(i,r);
                    else if(GUI.Button(r,GUIContent.none,GUIStyle.none))
                    {
                        if(!Investigation.prepared)investigationMessage=clear?"先把其余风口里的灰也清开，风闸才能活动。":"灰里还藏着余火。石台上的蒲叶扇或许能派上用场。";
                        else if(!Investigation.solved)
                        {
                            InvestigationRules.ToggleVent(Investigation,i);audioBus.Sound("puzzle");
                            investigationMessage=Investigation.dials[1]==1?"腹闸也被带开了，巢中的火羽摇晃起来。壁画上的胸前，却是静止的。":"中间的气流停下了。再看两翼，是否都已接住风？";
                            bool wasSolved=Investigation.solved;CheckInvestigationSolved(d);
                            if(!wasSolved&&Investigation.solved){phoenixStart=Time.unscaledTime;investigationMessage="两翼的风托起凤凰！沿右侧石阶，去看它留下的新羽。";}
                            Persist();
                        }
                    }
                }
                // Releasing outside a mouth cancels the unfinished gesture, never clears another mouth.
                if(Event.current.rawType==EventType.MouseUp&&fanVent>=0)CancelFanStroke();
                if(fanVent>=0)
                {
                    DanxueLayer(new Rect(fanPointer.x-62,fanPointer.y-36,118,82),1,.92f);
                    hover="轻轻扫动……灰正从羽纹间散开。";
                }
            }
            // Text sits on the dark floor, leaving the painting and objects unobstructed.
            for(int j=0;j<8;j++)P.Fill(new Rect(0,724+j*16,1600,16),new Color(.015f,.026f,.026f,.12f+j*.075f));
            P.Label(new Rect(50,747,245,34),"丹穴 · 两翼生风",24,P.Gold);
            string status=Investigation.solved?"风道已通  ·  石阶可行":Investigation.prepared?"拨动风闸  ·  观察联动":"观察  ·  拾取  ·  拂灰";
            P.Label(new Rect(50,790,250,34),status,17,P.Muted);
            string message=investigationMessage!=""?investigationMessage:DanxueDefaultMessage();
            P.Label(new Rect(350,744,1000,54),message,23,P.Paper,TextAnchor.MiddleLeft);
            P.Label(new Rect(350,807,1000,28),hover!=""?hover:Investigation.prepared?"线索已记在手记中，可随时回看。":"留意左侧石壁与石台，右侧三个炉口可以直接操作。",18,P.Gold);
            if(engine.State.items.Contains("fan")&&!Investigation.prepared)
            {
                DanxueLayer(new Rect(1390,748,124,79),1);
                if(GUI.Button(new Rect(1365,735,180,102),GUIContent.none,GUIStyle.none))
                {selectedItem="fan";investigationMessage="扇子已拿在手中。在积灰的风口前按住并左右扫动。";}
                P.Label(new Rect(1365,817,180,26),"蒲叶扇 · 已持有",17,P.Paper,TextAnchor.MiddleCenter);
                // A restored in-progress save immediately resumes with the owned fan available.
                selectedItem="fan";
            }
            if(P.Button(new Rect(1280,79,125,42),"线索手记",false,true,19))
            {investigationFocus=investigationFocus=="notes"?"":"notes";notesScroll=Vector2.zero;CancelFanStroke();}
            if(P.Button(new Rect(1420,79,125,42),"一点提示",false,true,19))
            {
                Investigation.hints=Math.Min(3,Investigation.hints+1);clueGlintUntil=Time.unscaledTime+5;
                investigationMessage=!engine.State.items.Contains("fan")&&!Investigation.prepared?"壁画下方的石台上放着扇子。拾起它，可以拂开风口里的余烬。":!Investigation.prepared?"按住风口并左右扫动，三个风口都要清开。每清开一处都会自动记录。":d.hints[Math.Max(1,Investigation.hints-1)];Persist();
            }
            if(Time.unscaledTime<clueGlintUntil&&investigationFocus=="")
            {
                // A pinprick of reflected firelight on actual handles; no rings or nameplates.
                if(!Investigation.prepared&&!engine.State.items.Contains("fan"))DanxueGlint(new Vector2(413,582));
                else for(int i=0;i<3;i++)DanxueGlint(new Vector2(VentX[i],580));
            }
            if(investigationFocus=="notes")DrawInvestigationDetail(d);
        }
        void HandleFanStroke(int index,Rect r)
        {
            Event e=Event.current;Vector2 pos=e.mousePosition;
            if(e.type==EventType.MouseDown&&e.button==0&&r.Contains(pos))
            {fanVent=index;fanLastX=pos.x;fanPointer=pos;fanDistance=0;fanDirection=fanTurns=0;e.Use();}
            else if(e.type==EventType.MouseDrag&&fanVent==index)
            {
                if(!r.Contains(pos)){CancelFanStroke();e.Use();return;}
                float delta=pos.x-fanLastX;fanLastX=pos.x;fanPointer=pos;
                if(Mathf.Abs(delta)>1)
                {
                    int direction=delta>0?1:-1;
                    if(fanDirection!=0&&direction!=fanDirection)fanTurns++;
                    fanDirection=direction;fanDistance+=Mathf.Abs(delta);
                    if(!profile.reducedMotion&&Time.unscaledTime-dustStart[index]>.5f)dustStart[index]=Time.unscaledTime;
                    if(fanDistance>=280&&fanTurns>=2)
                    {
                        InvestigationRules.ClearVent(engine.State,index);CancelFanStroke();dustStart[index]=Time.unscaledTime;audioBus.Sound("puzzle");
                        investigationMessage=Investigation.prepared?"三个风口都清开了。现在拨动风闸，按壁画的提示调通两翼。":"灰散开，露出了青铜风闸。继续拂开其余风口。";Persist();
                    }
                }
                e.Use();
            }
        }
        void DanxueGlint(Vector2 p)
        {
            float a=profile.reducedMotion?.7f:.45f+.35f*Mathf.Sin(Time.unscaledTime*3);
            Color c=new Color(1,.8f,.4f,a);P.Line(p-new Vector2(4,0),p+new Vector2(4,0),c,1);P.Line(p-new Vector2(0,4),p+new Vector2(0,4),c,1);
        }
    }
}
