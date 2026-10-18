using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using P=ShanHai.PaintUI;

namespace ShanHai
{
    public sealed partial class StoryGame
    {
        bool tracing, draggingMount;
        readonly List<Point2> drawnStroke=new List<Point2>();
        int selectedTile=-1,dragTile=-1,selectedMirror=-1;
        Vector2 dragStart;
        string miniMessage="";
        bool showHint;
        Texture2D rubbingVeil;
        Color32[] rubbingPaperPixels;
        int rubbingVeilCount=-1;
        void ResetTransientMechanic()
        {
            tracing=draggingMount=false;drawnStroke.Clear();
            selectedTile=dragTile=selectedMirror=-1;miniMessage="";showHint=false;
            ResetInvestigationUI();
            if(rubbingVeil!=null)Destroy(rubbingVeil);rubbingVeil=null;rubbingVeilCount=-1;
        }
        void DoneMechanic(string message)
        {
            engine.FinishMechanic();engine.Complete();audioBus.Sound("pact");Notify(message,3);lineTimer=0;Persist();
        }
        void DrawMechanic()
        {
            switch(engine.Node.mechanic)
            {
                case "explore":case "climb":DrawInvestigation();break;
                case "trace":DrawTrace();break;
                case "rubbing":DrawRubbing();break;
                case "mount":DrawMount();break;
                case "puzzle":DrawPuzzle();break;
                case "mirror":DrawMirror();break;
            }
        }
        void MiniPanel(string title,string instruction)
        {
            P.Fill(new Rect(0,94,Width,756),new Color(.015f,.035f,.04f,.45f));
            P.Image(new Rect(180,132,1240,680),Art("Investigations/worktable"));
            P.Fill(new Rect(220,154,1160,133),new Color(.025f,.02f,.015f,.65f));
            P.Label(new Rect(235,164,1130,55),title,34,P.Gold);
            P.Label(new Rect(235,226,1130,58),instruction,24,P.Paper);
            if(miniMessage!="")P.Label(new Rect(235,741,1130,46),miniMessage,23,P.Gold,TextAnchor.MiddleCenter);
        }
        // Coordinates are local to a square calligraphy grid; the first eight are 金, last four 乌.
        static readonly Point2[][] BrushGuides={
            new[]{new Point2(.50f,.13f),new Point2(.34f,.30f),new Point2(.16f,.39f)},
            new[]{new Point2(.50f,.13f),new Point2(.66f,.29f),new Point2(.85f,.38f)},
            new[]{new Point2(.32f,.37f),new Point2(.68f,.37f)},
            new[]{new Point2(.23f,.52f),new Point2(.77f,.52f)},
            new[]{new Point2(.50f,.38f),new Point2(.50f,.84f)},
            new[]{new Point2(.28f,.63f),new Point2(.36f,.74f)},
            new[]{new Point2(.73f,.62f),new Point2(.63f,.75f)},
            new[]{new Point2(.17f,.85f),new Point2(.83f,.85f)},
            new[]{new Point2(.48f,.13f),new Point2(.37f,.28f)},
            new[]{new Point2(.35f,.29f),new Point2(.69f,.29f),new Point2(.68f,.53f),new Point2(.59f,.57f)},
            new[]{new Point2(.31f,.29f),new Point2(.29f,.66f),new Point2(.77f,.66f),new Point2(.73f,.85f),new Point2(.61f,.87f)},
            new[]{new Point2(.15f,.77f),new Point2(.61f,.77f)}
        };
        Vector2 BrushPoint(Point2 point,int stroke)
        {
            return new Vector2((stroke<8?350:825)+point.x*380,320+point.y*380);
        }
        void DrawTrace()
        {
            MiniPanel("墨 韵 落 笔","按亮起的笔画描写“金乌”。从金色起点落笔，沿引导线写到末端，再抬笔。可用鼠标或手指。");
            int step=engine.State.miniStep;
            for(int k=0;k<2;k++)
            {
                Rect grid=new Rect(k==0?350:825,320,380,380);
                PaperSurface(grid);P.Border(grid,P.Gold,2);
                P.Line(new Vector2(grid.x,grid.center.y),new Vector2(grid.xMax,grid.center.y),new Color(.45f,.42f,.33f,.3f));
                P.Line(new Vector2(grid.center.x,grid.y),new Vector2(grid.center.x,grid.yMax),new Color(.45f,.42f,.33f,.3f));
                P.Line(grid.position,new Vector2(grid.xMax,grid.yMax),new Color(.45f,.42f,.33f,.18f));
                P.Line(new Vector2(grid.xMax,grid.y),new Vector2(grid.x,grid.yMax),new Color(.45f,.42f,.33f,.18f));
            }
            for(int i=0;i<BrushGuides.Length;i++)
            {
                Color c=i<step?P.Red:i==step?new Color(.46f,.30f,.1f,.65f):new Color(.38f,.34f,.25f,.14f);
                for(int j=1;j<BrushGuides[i].Length;j++)P.Line(BrushPoint(BrushGuides[i][j-1],i),BrushPoint(BrushGuides[i][j],i),c,i<step?12:7);
            }
            if(step<BrushGuides.Length)P.Diamond(BrushPoint(BrushGuides[step][0],step),13,P.Red);
            for(int i=1;i<drawnStroke.Count;i++)P.Line(new Vector2(drawnStroke[i-1].x,drawnStroke[i-1].y),new Vector2(drawnStroke[i].x,drawnStroke[i].y),P.Red,10);
            P.Label(new Rect(350,284,380,30),"金",21,P.Gold,TextAnchor.MiddleCenter);
            P.Label(new Rect(825,284,380,30),"乌",21,P.Gold,TextAnchor.MiddleCenter);
            if(P.Button(new Rect(1220,410,144,54),"重新落笔",false,true,22)){engine.State.miniStep=0;tracing=false;drawnStroke.Clear();miniMessage="已清空，可以重新开始。";Persist();}
            P.Label(new Rect(1220,500,144,90),"笔画\n"+step+" / 12",25,P.Gold,TextAnchor.MiddleCenter);
            Event e=Event.current;
            if(GUI.enabled&&step<BrushGuides.Length)
            {
                Rect writingArea=new Rect(step<8?356:831,326,368,368);
                if(tracing&&!writingArea.Contains(e.mousePosition))
                {tracing=false;drawnStroke.Clear();miniMessage="笔已离开纸面。这一笔请从起点重写。";}
                if(e.type==EventType.MouseDown&&Vector2.Distance(e.mousePosition,BrushPoint(BrushGuides[step][0],step))<36)
                {tracing=true;miniMessage="";drawnStroke.Clear();drawnStroke.Add(new Point2(e.mousePosition.x,e.mousePosition.y));audioBus.Sound("brush");e.Use();}
                else if(e.type==EventType.MouseDrag&&tracing)
                {if(drawnStroke.Count<1500)drawnStroke.Add(new Point2(e.mousePosition.x,e.mousePosition.y));e.Use();}
                else if(e.type==EventType.MouseUp&&tracing)
                {
                    drawnStroke.Add(new Point2(e.mousePosition.x,e.mousePosition.y));tracing=false;
                    var guide=BrushGuides[step].Select(p=>{var v=BrushPoint(p,step);return new Point2(v.x,v.y);}).ToArray();
                    bool passed=InteractionRules.JudgeStroke(drawnStroke,guide,31);
                    drawnStroke.Clear();
                    if(passed){engine.State.miniStep++;audioBus.Sound("brush");miniMessage="这一笔已落定。";Persist();}
                    else miniMessage="请从起点顺着笔画写到末端。重写没有惩罚。";
                    e.Use();
                    if(engine.State.miniStep==12)DoneMechanic("金乌之名已成，形影应声而现。");
                }
            }
        }
        void DrawRubbing()
        {
            MiniPanel("绘 异 传 拓","按住并擦拭拓纸，让凤凰形影逐渐显现。覆盖八成纸面即可落印入册。");
            Rect board=new Rect(340,310,920,390);
            PaperSurface(board);
            RubbingArt(new Rect(board.center.x-195,board.y,390,390),1);
            const int cols=20,rows=9;float cw=board.width/cols,ch=board.height/rows;
            var touched=engine.State.rubbed;
            const int paperWidth=460,paperHeight=195;
            if(rubbingVeil==null)
            {
                rubbingVeil=new Texture2D(paperWidth,paperHeight,TextureFormat.RGBA32,false);rubbingVeil.filterMode=FilterMode.Bilinear;rubbingVeil.wrapMode=TextureWrapMode.Clamp;
                if(rubbingPaperPixels==null)
                {
                    var paper=Art("Investigations/workshop-paper");var source=paper.GetPixels32();
                    rubbingPaperPixels=new Color32[paperWidth*paperHeight];
                    for(int y=0;y<paperHeight;y++)for(int x=0;x<paperWidth;x++)
                    {
                        int sx=(int)((.09f+.32f*x/(paperWidth-1))*paper.width),sy=(int)((.59f+.32f*y/(paperHeight-1))*paper.height);
                        rubbingPaperPixels[y*paperWidth+x]=source[sy*paper.width+sx];
                    }
                }
            }
            if(rubbingVeilCount!=touched.Count)
            {
                var mask=new float[cols*rows];for(int i=0;i<mask.Length;i++)mask[(rows-1-i/cols)*cols+i%cols]=touched.Contains(i)?0:1;
                var pixels=(Color32[])rubbingPaperPixels.Clone();
                for(int y=0;y<paperHeight;y++)for(int x=0;x<paperWidth;x++)
                {
                    float gx=Mathf.Clamp((x+.5f)*cols/paperWidth-.5f,0,cols-1),gy=Mathf.Clamp((y+.5f)*rows/paperHeight-.5f,0,rows-1);
                    int x0=(int)gx,y0=(int)gy,x1=Mathf.Min(cols-1,x0+1),y1=Mathf.Min(rows-1,y0+1);
                    float a=Mathf.Lerp(Mathf.Lerp(mask[y0*cols+x0],mask[y0*cols+x1],gx-x0),Mathf.Lerp(mask[y1*cols+x0],mask[y1*cols+x1],gx-x0),gy-y0);
                    pixels[y*paperWidth+x].a=(byte)(255*a);
                }
                rubbingVeil.SetPixels32(pixels);rubbingVeil.Apply(false);rubbingVeilCount=touched.Count;
            }
            GUI.DrawTexture(board,rubbingVeil,ScaleMode.StretchToFill,true);
            P.Border(board,P.Gold,3);
            var e=Event.current;
            if(GUI.enabled&&(e.type==EventType.MouseDown||e.type==EventType.MouseDrag)&&board.Contains(e.mousePosition))
            {
                int oldCount=touched.Count;
                for(int i=0;i<cols*rows;i++)
                {
                    Vector2 p=new Vector2(board.x+(i%cols+.5f)*cw,board.y+(i/cols+.5f)*ch);
                    if(Vector2.Distance(p,e.mousePosition)<65&&!touched.Contains(i))touched.Add(i);
                }
                if(touched.Count>oldCount&&touched.Count/20!=oldCount/20)audioBus.Sound("tuopian");
                e.Use();
            }
            if(e.type==EventType.MouseUp)Persist();
            float percent=touched.Count/(float)(cols*rows);
            miniMessage="形影显现 "+Mathf.RoundToInt(percent*100)+"%";
            if(P.Button(new Rect(1275,470,112,70),"落印",true,percent>=.8f,25))DoneMechanic("凤凰拓影已收入山海图鉴，记忆碎片 +1。");
        }
        void DrawMount()
        {
            MiniPanel("拓 片 装 裱","将左侧金乌拓片拖入右侧装裱框，然后落印。图鉴将保存这次相遇。");
            Rect source=new Rect(290,350,425,285),target=new Rect(870,330,430,325);
            PaperSurface(new Rect(target.x+50,target.y+28,target.width-100,target.height-58));PaperArt(new Rect(target.x-10,target.y-14,target.width+20,target.height+28),2);
            bool mounted=engine.State.miniStep>0;
            if(mounted)RubbingArt(new Rect(target.center.x-105,target.center.y-105,210,210),0);
            else
            {
                P.Label(target,"将拓片置于此处",25,new Color(.29f,.29f,.22f,1),TextAnchor.MiddleCenter);
                if(!draggingMount){PaperSurface(source);RubbingArt(new Rect(source.center.x-142,source.y,285,285),0);}
            }
            var e=Event.current;
            if(GUI.enabled&&!mounted&&e.type==EventType.MouseDown&&source.Contains(e.mousePosition)){draggingMount=true;e.Use();}
            if(draggingMount)
            {
                PaperSurface(new Rect(e.mousePosition.x-190,e.mousePosition.y-125,380,250));
                RubbingArt(new Rect(e.mousePosition.x-125,e.mousePosition.y-125,250,250),0);
                if(GUI.enabled&&e.type==EventType.MouseUp)
                {
                    draggingMount=false;
                    if(target.Contains(e.mousePosition)){engine.State.miniStep=1;audioBus.Sound("puzzle");miniMessage="拓片已就位，请落印。";Persist();}
                    else miniMessage="将拓片放进右侧装裱框。";
                    e.Use();
                }
            }
            if(P.Button(new Rect(1060,684,240,52),"落印入册",true,mounted))DoneMechanic("金乌拓片已装裱入册。");
        }
        void DrawPuzzle()
        {
            MiniPanel("拾 遗 之 页","拖动一块碎片到另一块上交换位置；也可依次点击两块。拼回玄曜遗落的记忆。");
            var tiles=engine.State.puzzle;
            if(tiles.Count==0){tiles.AddRange(new[]{4,2,0,5,1,3});Persist();}
            Rect board=new Rect(320,310,840,390);float w=board.width/3,h=board.height/2;
            Texture2D tex=Art("Investigations/memory");
            for(int slot=0;slot<6;slot++)
            {
                Rect r=new Rect(board.x+slot%3*w,board.y+slot/3*h,w-4,h-4);int piece=tiles[slot];
                GUI.DrawTextureWithTexCoords(r,tex,new Rect(piece%3/3f,1-(piece/3+1)/2f,1/3f,.5f));
                P.Border(r,selectedTile==slot?P.Gold:Color.black,selectedTile==slot?4:1);
                if(showHint){P.Fill(new Rect(r.x+10,r.y+10,55,38),P.Ink);P.Label(new Rect(r.x+10,r.y+10,55,38),(piece+1).ToString(),25,P.Gold,TextAnchor.MiddleCenter);}
                Event e=Event.current;
                if(GUI.enabled&&e.type==EventType.MouseDown&&r.Contains(e.mousePosition)){dragTile=slot;dragStart=e.mousePosition;e.Use();}
            }
            Event ev=Event.current;
            if(GUI.enabled&&ev.type==EventType.MouseUp&&dragTile>=0)
            {
                int target=board.Contains(ev.mousePosition)?Mathf.Clamp((int)((ev.mousePosition.y-board.y)/h)*3+(int)((ev.mousePosition.x-board.x)/w),0,5):-1;
                if(target>=0)
                {
                    if(target!=dragTile){InteractionRules.Swap(tiles,dragTile,target);selectedTile=-1;audioBus.Sound("puzzle");}
                    else if(selectedTile>=0&&selectedTile!=target){InteractionRules.Swap(tiles,selectedTile,target);selectedTile=-1;audioBus.Sound("puzzle");}
                    else selectedTile=target;
                }
                dragTile=-1;Persist();ev.Use();
                if(InteractionRules.IsSolved(tiles))DoneMechanic("散落的记忆重新相接，记忆碎片 +1。");
            }
            P.Label(new Rect(1200,350,170,85),"完整图景",24,P.Gold,TextAnchor.MiddleCenter);
            P.Image(new Rect(1188,427,190,105),tex);
            if(P.Button(new Rect(1200,575,165,60),showHint?"收起序号":"显示提示",false,true,22))showHint=!showHint;
            miniMessage=showHint?"碎片按 1、2、3 / 4、5、6 的顺序排列。":"每片记忆，都有它的位置。";
        }
        void DrawMirror()
        {
            MiniPanel("双 卷 映 照","先选择左卷的一段记名，再选择右卷对应的形影。让四段见闻互相补全。");
            string[] left={"金乌之名","凤凰之影","相柳之问","罔象之行"};
            string[] right={"烈焰中新生的羽","不辨善恶的遗忘","托起晨曦的三足","九重回声中的困惑"};
            int[] matches={2,0,3,1};var done=engine.State.matched;
            ScrollSurface(new Rect(260,304,480,430));ScrollSurface(new Rect(860,304,480,430));
            P.Label(new Rect(300,290,400,45),"记名之卷",27,P.Red,TextAnchor.MiddleCenter);
            P.Label(new Rect(900,290,400,45),"存形之卷",27,P.Jade,TextAnchor.MiddleCenter);
            for(int i=0;i<4;i++)
            {
                bool matched=done.Contains(i);
                if(ScrollChoice(new Rect(300,350+i*87,400,66),matched?"已照 · "+left[i]:left[i],selectedMirror==i,!matched))selectedMirror=i;
                bool rightDone=done.Any(k=>matches[k]==i);
                if(ScrollChoice(new Rect(900,350+i*87,400,66),right[i],false,!rightDone))
                {
                    if(selectedMirror<0)miniMessage="先从左卷选择一段记名。";
                    else if(matches[selectedMirror]==i)
                    {done.Add(selectedMirror);selectedMirror=-1;audioBus.Sound("puzzle");miniMessage="两卷的纹路相接了。";Persist();}
                    else{miniMessage="形与名尚未相合，想想旅途中的见闻。";engine.State.miniStep++;}
                }
                if(matched)P.Line(new Vector2(715,383+i*87),new Vector2(885,383+matches[i]*87),P.Gold,3);
            }
            if(engine.State.miniStep>=3)miniMessage="提示：金乌—三足；凤凰—新生；相柳—九重回声；罔象—遗忘。";
            if(done.Count==4)DoneMechanic("双卷映照完成，记忆碎片 +2。相柳与罔象的关联已记入图鉴。");
        }
        void PaperSurface(Rect r)
        {
            var tex=Art("Investigations/workshop-paper");
            if(tex!=null)GUI.DrawTextureWithTexCoords(r,tex,new Rect(.09f,.59f,.32f,.32f));
        }
        void ScrollSurface(Rect r)
        {
            var tex=Art("Investigations/workshop-paper");
            if(tex!=null)GUI.DrawTextureWithTexCoords(r,tex,new Rect(.58f,.53f,.34f,.46f));
        }
        bool ScrollChoice(Rect r,string label,bool selected,bool enabled)
        {
            P.Label(r,label,24,enabled?new Color(.19f,.15f,.1f,1):new Color(.52f,.31f,.23f,1),TextAnchor.MiddleCenter);
            if(selected)P.Line(new Vector2(r.x+85,r.yMax-6),new Vector2(r.xMax-85,r.yMax-6),P.Red,3);
            return enabled&&WorldButton(r);
        }
    }
}
