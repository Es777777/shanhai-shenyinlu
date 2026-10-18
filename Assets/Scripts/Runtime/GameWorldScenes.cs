using System;
using System.Linq;
using UnityEngine;
using P=ShanHai.PaintUI;

namespace ShanHai
{
    public sealed partial class StoryGame
    {
        sealed class WorldScene
        {
            public string id,art;public Rect clue,tool,device,exit;
            public WorldScene(string id,string art,Rect clue,Rect tool,Rect device,Rect exit)
            {this.id=id;this.art=art;this.clue=clue;this.tool=tool;this.device=device;this.exit=exit;}
        }
        // Coordinates are registered to the actual painted landmarks, in a 1600x900 canvas.
        static readonly WorldScene[] WorldScenes={
            new WorldScene("N101","fusang",new Rect(190,135,365,345),new Rect(332,516,115,34),new Rect(624,437,120,120),new Rect(1400,240,185,460)),
            new WorldScene("N201","zhongshan",new Rect(135,130,440,320),new Rect(335,467,90,90),new Rect(775,180,530,135),new Rect(900,350,300,320)),
            new WorldScene("N301","youdu-lamps",new Rect(80,135,455,325),new Rect(),new Rect(647,465,795,95),new Rect(850,150,320,260)),
            new WorldScene("N403","xiongli",new Rect(150,160,350,375),new Rect(355,565,120,35),new Rect(752,145,445,440),new Rect(1320,285,250,360)),
            new WorldScene("N501","wangchuan",new Rect(448,275,130,205),new Rect(365,560,150,115),new Rect(720,435,790,260),new Rect(720,435,790,260)),
            new WorldScene("N601","youdu-leaf",new Rect(90,120,510,360),new Rect(),new Rect(895,215,300,300),new Rect(875,550,430,180)),
            new WorldScene("N801","buzhou",new Rect(60,180,360,235),new Rect(330,476,125,65),new Rect(425,432,90,90),new Rect(1320,155,260,140))
        };
        WorldScene CurrentWorld {get{return WorldScenes.FirstOrDefault(s=>s.id==engine.Node.id);}}
        string worldHover="";
        bool worldBrushing;
        Vector2 worldBrushPoint,worldWalker;
        float worldBrushDistance,boatAngle;
        readonly float[] bridgeRaisedAt={-100,-100,-100,-100,-100,-100};
        readonly float[] leafAngle=new float[3];
        bool leafAnglesReady;
        void CancelWorldGesture(){worldBrushing=false;worldBrushDistance=0;}
        void ResetWorldVisuals(){CancelWorldGesture();worldWalker=Vector2.zero;boatAngle=0;leafAnglesReady=false;for(int i=0;i<6;i++)bridgeRaisedAt[i]=-100;}
        float WorldReveal {get{return profile.reducedMotion||worldSolvedAt<0?1:Mathf.SmoothStep(0,1,(Time.unscaledTime-worldSolvedAt)/2f);}}
        void SceneImage(string path,float alpha=1)
        {
            Color old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            P.Image(new Rect(0,0,Width,Height),Art("Investigations/"+path));GUI.color=old;
        }
        void DrawWorldBackground()
        {
            var scene=CurrentWorld;if(scene==null)return;
            InvestigationRules.Prepare(engine.State,scene.id);
            string initial=scene.art+"-room";
            if(scene.id=="N403"&&!Investigation.prepared)initial="xiongli-covered";
            if(scene.id=="N201"&&Investigation.prepared)initial="zhongshan-thawed";
            SceneImage(initial);
            if((scene.id=="N201"||scene.id=="N403")&&Investigation.prepared&&!profile.reducedMotion&&Time.unscaledTime-worldPreparedAt<1.2f)
                SceneImage(scene.id=="N201"?"zhongshan-room":"xiongli-covered",1-Mathf.SmoothStep(0,1,(Time.unscaledTime-worldPreparedAt)/1.2f));
            if(Investigation.solved&&scene.id!="N501"&&scene.id!="N801")SceneImage(scene.art+"-awake",WorldReveal);
            var d=InvestigationRules.Find(scene.id);
            if(scene.tool.width>0&&!Investigation.prepared&&!engine.State.items.Contains(d.tool))Prop(scene.tool,d.sprite);
            if(scene.id=="N201"&&Investigation.prepared)
                Prop(new Rect(335,467,90,90),2);
            DrawWorldMechanism(false);
        }
        bool WorldButton(Rect r){return GUI.Button(r,GUIContent.none,GUIStyle.none);}
        bool TryWorldTool(InvestigationDefinition d)
        {
            if(!engine.State.items.Contains(d.tool))
            {investigationMessage="这里还缺一件器物。观察近处的石台、岸边或行囊。";return false;}
            if(!InvestigationRules.UseTool(engine.State,d.tool))return false;
            selectedItem="";worldPreparedAt=Time.unscaledTime;audioBus.Sound(d.id=="N403"?"sand":d.id=="N501"?"wood":"stone");
            investigationMessage=d.id=="N201"?"暖光融开冰霜，日月星辰的浮雕露了出来。":d.id=="N403"?"浮沙落下，河纹清晰了。转动石渠，让沙沿旧河而出。":d.id=="N801"?"绳索系牢了。选择朱卷或青卷，逐步修复相邻的残阶。":d.instruction;
            if(d.id=="N201"||d.id=="N403"){ObserveClue(d);investigationFocus="";}
            Persist();return true;
        }
        void DrawWorldInvestigation(InvestigationDefinition d)
        {
            var scene=CurrentWorld;if(scene==null)return;
            worldHover="";bool controls=GUI.enabled;
            bool input=controls&&investigationFocus=="";
            if(!input)CancelWorldGesture();
            GUI.enabled=input;
            if(input)
            {
                bool owns=engine.State.items.Contains(d.tool);
                if(owns&&!Investigation.prepared)selectedItem=d.tool;
                if(scene.clue.Contains(Event.current.mousePosition))worldHover="观察 · "+d.clueTitle;
                if(d.id=="N403"&&owns&&!Investigation.prepared)HandleRiverBrush(scene.clue,d);
                else if(WorldButton(scene.clue))
                {
                    if(d.id=="N201"&&!Investigation.prepared&&owns)TryWorldTool(d);
                    else {ObserveClue(d);investigationFocus="";}
                }
                if(scene.tool.width>0&&!owns&&!Investigation.prepared)
                {
                    Rect hit=scene.tool;hit.xMin-=18;hit.xMax+=18;hit.yMin-=12;hit.yMax+=12;
                    if(hit.Contains(Event.current.mousePosition))worldHover="拾取 · "+d.toolName;
                    if(WorldButton(hit))
                    {engine.State.items.Add(d.tool);selectedItem=d.tool;investigationMessage="拿起了"+d.toolName+"。"+(d.id=="N403"?"在河碑表面按住并来回轻扫。":d.id=="N201"?"把暖光移到左侧的冰碑上。":"观察它能与场景中的哪处痕迹相合。");audioBus.Sound("stone");Persist();}
                }
                if(!Investigation.prepared&&d.id!="N201"&&d.id!="N403")
                {
                    if(scene.device.Contains(Event.current.mousePosition))worldHover="使用器物 · "+d.title;
                    if(WorldButton(scene.device))TryWorldTool(d);
                }
                else if(Investigation.prepared&&!Investigation.solved)DrawWorldMechanism(true);
                if(d.id!="N501"||Investigation.solved)
                {
                    if(scene.exit.Contains(Event.current.mousePosition))worldHover=Investigation.solved?"前路已开 · 点击继续":"观察前路";
                    if(WorldButton(scene.exit))FinishInvestigation(d);
                }
                CheckInvestigationSolved(d);
                if(Event.current.rawType==EventType.MouseUp)CancelWorldGesture();
                if(worldBrushing)Prop(new Rect(worldBrushPoint.x-18,worldBrushPoint.y-40,75,75),3);
            }
            GUI.enabled=controls;
            if(Compact)
            {
                P.Fill(new Rect(0,675,1600,120),new Color(.012f,.025f,.029f,.94f));
                string message=investigationMessage!=""?investigationMessage:Investigation.solved?d.success:d.goal;
                if(message.Length>76)message=message.Substring(0,73)+"…";
                P.Label(new Rect(35,688,1530,100),message,36,P.Paper);
            }
            else
            {
            for(int j=0;j<8;j++)P.Fill(new Rect(0,722+j*16,1600,16),new Color(.012f,.025f,.029f,.12f+j*.075f));
            P.Label(new Rect(42,741,300,39),d.title,25,P.Gold);
            P.Label(new Rect(350,735,1030,75),investigationMessage!=""?investigationMessage:Investigation.solved?d.success:d.goal,25,P.Paper,TextAnchor.MiddleLeft);
            P.Label(new Rect(350,815,1030,28),worldHover!=""?worldHover:Investigation.solved?"观察场景中的变化，然后点击前路继续。":Investigation.prepared?"机关就在场景里。点击器物，观察它们的回应。":"可点实物调查；拾取器物后，直接用于场景。",20,P.Gold);
            }
            if(d.id=="N801"&&Investigation.prepared&&!Investigation.solved)
            {
                for(int s=0;s<2;s++)
                {
                    Rect r=new Rect(45+s*135,782,120,60);JourneyItem(r,4+s,activeScroll==s?1:.5f);
                    if(WorldButton(r)){activeScroll=s;investigationMessage=s==0?"朱卷镇住有名的裂隙。选择相邻的裂纹石阶。":"青卷映出无形的空缺。选择相邻的虚影石阶。";}
                    P.Label(new Rect(r.x+50,r.y+17,65,30),s==0?"朱卷":"青卷",19,P.Paper);
                }
            }
            else if(engine.State.items.Contains(d.tool)&&!Investigation.prepared)
            {
                Prop(new Rect(45,783,55,55),d.sprite);
                P.Label(new Rect(110,793,225,35),"持有 · "+d.toolName,19,P.Paper);
            }
            else if(!Compact) P.Label(new Rect(45,796,285,34),Investigation.solved?"见闻已成 · 前路可行":Investigation.prepared?"器物已就位":"留意近处的痕迹与器物",18,P.Muted);
            if(P.Button(new Rect(Compact?1070:1280,Compact?105:79,Compact?220:125,Compact?90:42),"线索手记",false,true,19))
            {investigationFocus=investigationFocus=="notes"?"":"notes";notesScroll=Vector2.zero;CancelWorldGesture();}
            if(P.Button(new Rect(Compact?1310:1420,Compact?105:79,Compact?250:125,Compact?90:42),"一点提示",false,true,19))
            {
                Investigation.hints=Math.Min(3,Investigation.hints+1);clueGlintUntil=Time.unscaledTime+5;
                investigationMessage=d.hints[Investigation.hints-1];Persist();
            }
            if(Time.unscaledTime<clueGlintUntil&&investigationFocus=="")
            {
                Vector2 p=Investigation.solved?scene.exit.center:!Investigation.prepared&&!engine.State.items.Contains(d.tool)&&scene.tool.width>0?scene.tool.center:!Investigation.prepared&&(d.id=="N201"||d.id=="N403")?scene.clue.center:scene.device.center;
                DanxueGlint(p);
            }
            if(investigationFocus=="notes")DrawInvestigationDetail(d);
        }
        void HandleRiverBrush(Rect area,InvestigationDefinition d)
        {
            Event e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0&&area.Contains(e.mousePosition))
            {worldBrushing=true;worldBrushDistance=0;worldBrushPoint=e.mousePosition;investigationMessage="顺着石纹轻扫，让浮沙落下。";e.Use();}
            else if(e.type==EventType.MouseDrag&&worldBrushing)
            {
                if(!area.Contains(e.mousePosition)){CancelWorldGesture();e.Use();return;}
                worldBrushDistance+=Vector2.Distance(worldBrushPoint,e.mousePosition);worldBrushPoint=e.mousePosition;
                if(worldBrushDistance>=230){TryWorldTool(d);CancelWorldGesture();}
                e.Use();
            }
        }
        void DrawWorldMechanism(bool input)
        {
            switch(engine.Node.id)
            {
                case "N101":WorldMirrors(input);break;
                case "N201":WorldEyes(input);break;
                case "N301":WorldLamps(input);break;
                case "N403":WorldRiver(input);break;
                case "N501":WorldFerry(input);break;
                case "N601":WorldLeafGate(input);break;
                case "N801":WorldBridge(input);break;
            }
        }
        Vector2 LightPoint(Point2 p){return p.x==5&&p.y==3?new Vector2(1100,510):new Vector2(580+p.x*104,222+p.y*137.5f);}
        void WorldMirrors(bool input)
        {
            Vector2[] centers={new Vector2(684,497),new Vector2(684,222),new Vector2(1100,222)};
            if(!input)
            {
                var path=InvestigationRules.LightPath(Investigation.dials,Investigation.prepared);
                for(int j=1;j<path.Count;j++)
                {Vector2 a=LightPoint(path[j-1]),b=LightPoint(path[j]);P.Line(a,b,new Color(1,.76f,.3f,.09f),17);P.Line(a,b,new Color(1,.84f,.49f,.5f),3);}
            }
            for(int i=0;i<3;i++)
            {
                var c=centers[i];Rect r=new Rect(c.x-48,c.y-48,96,96);
                if(!input&&(i>0||Investigation.prepared))Stone(r,6+Investigation.dials[i]);
                if(input&&WorldButton(new Rect(c.x-57,c.y-57,114,114)))
                {Investigation.dials[i]=1-Investigation.dials[i];investigationMessage="镜面转过，晨光换了方向。让它接着走到下一面镜。";audioBus.Sound("stone");Persist();}
            }
        }
        void WorldEyes(bool input)
        {
            for(int i=0;i<3;i++)
            {
                Rect r=new Rect(779+i*201,185,117,117);
                if(!input)Stone(r,Investigation.dials[i]);
                else if(WorldButton(r))
                {Investigation.dials[i]=(Investigation.dials[i]+1)%4;investigationMessage="石眼映出"+new[]{"日","云","月","星"}[Investigation.dials[i]]+"。再想想铭文里的昼夜次序。";audioBus.Sound("stone");Persist();}
            }
        }
        void WorldLamps(bool input)
        {
            float[] xs={704,929,1153,1378};int[] order={2,0,3,1};string[] names={"木","土","水","火"};
            for(int i=0;i<4;i++)
            {
                Rect r=new Rect(xs[i]-37,478,74,69);bool lit=Array.IndexOf(order,i)<Investigation.progress;
                if(!input&&lit)
                {
                    float f=profile.reducedMotion?1:.94f+.06f*Mathf.Sin(Time.unscaledTime*3+i);
                    Atlas(new Rect(r.x-6,r.y-10,86,82),"Investigations/leaf-rings",3,2,2,new Color(1,1,1,f));
                }
                if(input&&WorldButton(new Rect(xs[i]-64,457,128,120)))
                {
                    int before=Investigation.progress;InvestigationRules.PressLamp(Investigation,i);
                    investigationMessage=Investigation.progress>before?names[i]+"灯应答了，火光沿石阶靠近城门。":"灯影熄灭，次序尚未相合。按残刻的关系重新点灯即可。";
                    audioBus.Sound("lamp");Persist();
                }
                if(input&&new Rect(xs[i]-64,457,128,120).Contains(Event.current.mousePosition))worldHover="名灯 · "+names[i];
            }
        }
        static int TurnMask(int mask,int turn){for(int i=0;i<turn;i++)mask=((mask<<1)&15)|(mask>>3);return mask;}
        void WorldRiver(bool input)
        {
            var path=InvestigationRules.RiverPath(Investigation.dials);
            for(int i=0;i<9;i++)
            {
                Vector2 c=new Vector2(827+i%3*147,218+i/3*146);Rect r=new Rect(c.x-72,c.y-72,144,144);
                int mask=InvestigationRules.RiverMask(i,Investigation.dials[i]);bool straight=mask==5||mask==10;
                int angle=0;while(angle<4&&TurnMask(straight?10:12,angle)!=mask)angle++;
                if(!input)
                {
                    Stone(r,straight?5:4,angle*90);
                    if(Investigation.prepared&&path.Contains(i))
                    {
                        for(int dir=0;dir<4;dir++)if((mask&(1<<dir))!=0)
                        {
                            Vector2 delta=new Vector2(new[]{0,1,0,-1}[dir],new[]{-1,0,1,0}[dir]);
                            P.Line(c,c+delta*74,new Color(.96f,.74f,.35f,.25f),9);
                            for(int p=0;p<5;p++)
                            {
                                float t=profile.reducedMotion?(p+.5f)/5:Mathf.Repeat(Time.unscaledTime*.23f+p*.2f,1);
                                Vector2 pos=c+delta*(t*70);P.Fill(new Rect(pos.x,pos.y,2,2),new Color(1,.82f,.49f,.82f));
                            }
                        }
                    }
                }
                else if(WorldButton(r))
                {Investigation.dials[i]=(Investigation.dials[i]+1)%4;audioBus.Sound("stone");investigationMessage="河纹石转动了。看流沙停在哪里，把下一段石渠接上。";Persist();}
            }
        }
        void WorldFerry(bool input)
        {
            int torque=InvestigationRules.Torque(Investigation.dials);
            float target=Investigation.prepared?Mathf.Clamp(torque*1.3f,-9,9):-5;
            if(!input&&Event.current.type==EventType.Repaint)boatAngle=profile.reducedMotion?target:Mathf.MoveTowards(boatAngle,target,Time.unscaledDeltaTime*16);
            float departure=Investigation.solved?WorldReveal*65:0;
            var matrix=GUI.matrix;Vector2 pivot=new Vector2(1115+departure,590);
            try
            {
                GUI.matrix=matrix*Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,boatAngle),Vector3.one)*Matrix4x4.Translate(new Vector3(-1115,-590,0));
                if(!input)
                {
                    JourneyItem(new Rect(770,635,700,110),7,.45f);
                    var tex=Art("Investigations/ferry");if(tex!=null)GUI.DrawTexture(new Rect(695,355,840,420),tex,ScaleMode.StretchToFill,true);
                }
                float[] x={921,1049,1171,1294};
                for(int i=0;i<4;i++)
                {
                    Rect r=new Rect(x[i]-42,488,84,90);
                    if(!input&&Investigation.prepared)JourneyItem(r,Investigation.dials[i]-1,selectedStone==i?.7f:1);
                    if(input&&r.Contains(Event.current.mousePosition))worldHover="镇舟石 · "+Investigation.dials[i]+" 重；点击两块交换位置。";
                    if(input&&WorldButton(r))
                    {
                        if(selectedStone<0){selectedStone=i;investigationMessage="选中这块镇舟石，再点击另一块交换。";}
                        else {InteractionRules.Swap(Investigation.dials,selectedStone,i);selectedStone=-1;int t=InvestigationRules.Torque(Investigation.dials);investigationMessage=t==0?"舟身平稳了，可以登舟过河。":t>0?"右侧仍在下沉。离船心越远，同样的石头压得越重。":"左侧仍在下沉。试着调整远端与近端石头的位置。";audioBus.Sound("wood");Persist();}
                    }
                }
            }
            finally{GUI.matrix=matrix;}
        }
        void WorldLeafGate(bool input)
        {
            Vector2 c=new Vector2(1046,366);float[] size={282,195,116};int[] correct={0,2,1};
            float alpha=Investigation.solved?1-WorldReveal:1;
            for(int i=0;i<3;i++)
            {
                Rect r=new Rect(c.x-size[i]/2,c.y-size[i]/2,size[i],size[i]);
                if(!input&&alpha>.005f)
                {
                    if(!leafAnglesReady){for(int j=0;j<3;j++)leafAngle[j]=Investigation.dials[j]*90;leafAnglesReady=true;}
                    if(Event.current.type==EventType.Repaint)leafAngle[i]=profile.reducedMotion?Investigation.dials[i]*90:Mathf.MoveTowardsAngle(leafAngle[i],Investigation.dials[i]*90,Time.unscaledDeltaTime*240);
                    Atlas(new Rect(r.x+3,r.y+6,r.width,r.height),"Investigations/leaf-rings",i,2,2,new Color(.015f,.025f,.02f,alpha*.65f),leafAngle[i]);
                    Atlas(r,"Investigations/leaf-rings",i,2,2,new Color(.72f,.83f,.78f,alpha),leafAngle[i]);
                    float a=(correct[i]*90-90)*Mathf.Deg2Rad;
                    Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    P.Line(c+direction*(size[i]*.47f),c+direction*(size[i]*.53f),new Color(.95f,.74f,.3f,alpha),5);
                }
            }
            if(input&&Event.current.type==EventType.MouseDown)
            {
                float radius=Vector2.Distance(Event.current.mousePosition,c);int ring=radius>107&&radius<151?0:radius>67&&radius<=107?1:radius<=67?2:-1;
                if(ring>=0){Investigation.dials[ring]=(Investigation.dials[ring]+1)%4;audioBus.Sound("stone");investigationMessage="青色叶尖随圆环转动，须与石门上同一圈的金色叶脉接合。";Persist();Event.current.Use();}
            }
        }
        Vector2 BridgePlace(int cell){return new Vector2(610+cell%4*180,300+cell/4*150);}
        void WorldBridge(bool input)
        {
            for(int step=0;step<6;step++)
            {
                int cell=InvestigationRules.BridgeCells[step];Vector2 c=BridgePlace(cell);bool repaired=step<Investigation.progress;
                float depth=(1f-(2-cell/4)*.12f)*(1+(step%3-1)*.045f);
                Rect r=new Rect(c.x-110*depth,c.y-125*depth,220*depth,220*depth);
                if(!input)
                {
                    float reveal=profile.reducedMotion?1:Mathf.SmoothStep(0,1,(Time.unscaledTime-bridgeRaisedAt[step])/.75f);
                    float angle=(step%3-1)*3;
                    Color tint=new Color(.80f+.17f*depth,.87f+.1f*depth,.94f,1);
                    Atlas(new Rect(r.x+3,r.y+7*depth,r.width,r.height),"Investigations/bridge-layers",(repaired?2:0)+step%2,2,2,new Color(.02f,.03f,.04f,.38f),angle);
                    if(!repaired||reveal<1)Atlas(r,"Investigations/bridge-layers",step%2,2,2,tint,angle);
                    if(repaired){r.y+=(1-reveal)*14; tint.a=reveal;Atlas(r,"Investigations/bridge-layers",2+step%2,2,2,tint,angle);}
                }
                if(input&&WorldButton(new Rect(c.x-106*depth,c.y-55*depth,212*depth,100*depth)))
                {
                    if(InvestigationRules.BridgeStep(Investigation,cell,activeScroll))
                    {bridgeRaisedAt[Investigation.progress-1]=Time.unscaledTime;investigationMessage="残阶稳住了，两人向前一步。接着辨认相邻的裂纹或虚影。";audioBus.Sound("stone");Persist();}
                    else investigationMessage="这一步还够不到，或卷别不合。朱卷镇裂纹，青卷映虚影，要从近处逐步前行。";
                }
            }
            if(!input)
            {
                Vector2 target=Investigation.progress==0?new Vector2(490,545):BridgePlace(InvestigationRules.BridgeCells[Math.Min(5,Investigation.progress-1)])+new Vector2(0,-20);
                if(worldWalker==Vector2.zero)worldWalker=target;
                if(Event.current.type==EventType.Repaint)worldWalker=profile.reducedMotion?target:Vector2.MoveTowards(worldWalker,target,Time.unscaledDeltaTime*260);
                if(Investigation.prepared)
                {
                    Vector2 tether=new Vector2(470,470);
                    for(int i=0;i<18;i++)
                    {float a=i/18f,b=(i+1)/18f;Vector2 p=Vector2.Lerp(tether,worldWalker,a)+Vector2.up*Mathf.Sin(a*Mathf.PI)*22,q=Vector2.Lerp(tether,worldWalker,b)+Vector2.up*Mathf.Sin(b*Mathf.PI)*22;P.Line(p,q,new Color(.68f,.57f,.39f,.7f),2);}
                }
                JourneyItem(new Rect(worldWalker.x-36,worldWalker.y-107,72,104),6);
            }
        }
    }
}
