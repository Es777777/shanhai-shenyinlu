using System;
using UnityEngine;
using P=ShanHai.PaintUI;
namespace ShanHai
{
    public sealed partial class StoryGame
    {
        void DrawCompactTitle()
        {
            P.Image(new Rect(0,0,Width,Height),Art("Backgrounds/tianque"));
            P.Fill(new Rect(0,0,760,900),new Color(.015f,.045f,.05f,.88f));
            P.Label(new Rect(80,80,660,100),"山海神隐录",76,P.Paper);
            P.Label(new Rect(85,195,600,65),"以名观物　以忘容生",36,P.Gold);
            if(P.Button(new Rect(85,290,590,105),"启卷 · 开始旅程",true,true,40))
            {if(LocalSaves.Read(0)!=null)Ask("开始新旅程将更新自动存档。手动存档与已收集内容会保留。",NewGame);else NewGame();}
            if(P.Button(new Rect(85,410,590,105),"续卷 · 继续旅程",false,LocalSaves.Read(0)!=null,40))LoadGame(LocalSaves.Read(0));
            if(P.Button(new Rect(85,540,280,105),"山海图鉴",false,true,36))OpenPanel("codex");
            if(P.Button(new Rect(395,540,280,105),"结局收藏",false,true,36))OpenPanel("endings");
            if(P.Button(new Rect(85,670,180,105),"章节",false,true,36))OpenPanel("chapters");
            if(P.Button(new Rect(290,670,180,105),"读档",false,true,36))OpenPanel("load");
            if(P.Button(new Rect(495,670,180,105),"设置",false,true,36))OpenPanel("settings");
            P.Label(new Rect(850,650,660,130),"名字若被遗忘，\n山海可还存在？",40,P.Paper,TextAnchor.MiddleRight);
        }
        void DrawPortraitEdges(Rect target,Texture2D texture,Rect uv)
        {
            // Feather the hard atlas crop during rendering; preserve the source art.
            Color old=GUI.color;Matrix4x4 matrix=GUI.matrix;
            Vector3 a=matrix.MultiplyPoint3x4(new Vector3(target.x,target.y,0));
            Vector3 b=matrix.MultiplyPoint3x4(new Vector3(target.xMax,target.yMax,0));
            int x=Mathf.RoundToInt(a.x),y=Mathf.RoundToInt(a.y);
            int width=Mathf.Max(4,Mathf.RoundToInt(b.x)-x),height=Mathf.Max(1,Mathf.RoundToInt(b.y)-y);
            int edge=Mathf.Clamp(Mathf.RoundToInt(width*.065f),1,32);
            // Draw the body once. Feather only the outer pixels, aligned in screen
            // space so IMGUI rounding cannot open seams at fractional canvas scales.
            GUI.matrix=Matrix4x4.identity;
            GUI.DrawTextureWithTexCoords(new Rect(x+edge,y,width-2*edge,height),texture,new Rect(uv.x+uv.width*edge/width,uv.y,uv.width*(width-2*edge)/width,uv.height));
            for(int i=0;i<edge;i++)
            {
                float alpha=Mathf.SmoothStep(0,1,(i+.5f)/edge);
                GUI.color=new Color(old.r,old.g,old.b,old.a*alpha);
                for(int side=0;side<2;side++)
                {
                    int column=side==0?i:width-1-i;
                    GUI.DrawTextureWithTexCoords(new Rect(x+column,y,1,height),texture,new Rect(uv.x+uv.width*column/width,uv.y,uv.width/width,uv.height));
                }
            }
            GUI.color=old;GUI.matrix=matrix;
        }
        void DrawEndingClose()
        {
            // The CG already contains the actors. Leave its storytelling area clear.
            P.Fill(new Rect(0,0,Width,100),new Color(.015f,.04f,.05f,.78f));
            P.Label(new Rect(45,25,900,65),engine.Node.title,Compact?40:34,P.Gold);
            if(P.Button(new Rect(1180,2,180,96),"图鉴",false,true,Compact?36:28))OpenPanel("codex");
            if(P.Button(new Rect(1375,2,210,96),"行卷",false,true,Compact?36:28))OpenPanel("menu");
            if(P.Button(new Rect(1180,Compact?675:730,370,Compact?110:90),"收卷 · 记录结局",true,true,Compact?36:28))Next();
        }
        void DrawReadableDialogue(DialogueLine line)
        {
            int font=Mathf.RoundToInt((Compact?38:31)*profile.textScale);
            string text=line.text.Substring(0,Math.Min(line.text.Length,(int)shownCharacters));
            float body=Mathf.Max(100,P.Style(font).CalcHeight(new GUIContent(line.text),1370));
            float bottom=Compact?790:840,top=bottom-body-100;
            P.Panel(new Rect(35,top,1530,body+100));
            P.Label(new Rect(72,top+15,1000,55),line.speaker,Compact?36:29,P.Gold);
            P.Label(new Rect(75,top+73,1370,body),text,font,P.Paper);
            if(GUI.Button(new Rect(35,top,1530,body+100),GUIContent.none,GUIStyle.none)){audioBus.Resume();Next();}
        }
        void DrawCompactHUD()
        {
            P.Fill(new Rect(0,0,1600,100),new Color(.015f,.035f,.04f,.9f));
            P.Label(new Rect(35,10,680,80),engine.Node.title,36,P.Paper,TextAnchor.MiddleLeft);
            P.Label(new Rect(730,15,420,75),"明昧 "+engine.Get("mcv")+"  ·  碎片 "+engine.Get("fragments"),32,P.Gold,TextAnchor.MiddleCenter);
            if(P.Button(new Rect(1180,2,180,96),"图鉴",false,true,36))OpenPanel("codex");
            if(P.Button(new Rect(1375,2,210,96),"行卷",false,true,36))OpenPanel("menu");
        }
        void DrawCompactToolbar()
        {
            P.Fill(new Rect(310,795,1290,105),new Color(.015f,.04f,.05f,.98f));
            if(P.Button(new Rect(640,800,280,98),autoPlay?"自动 · 开":"自动",false,true,36)){autoPlay=!autoPlay;skipping=false;}
            if(P.Button(new Rect(940,800,280,98),"回看",false,true,36))OpenPanel("history");
            if(P.Button(new Rect(1240,800,320,98),"存档与设置",false,true,36))OpenPanel("menu");
        }
        void DrawTextScale()
        {
            P.Label(new Rect(200,679,290,60),"文字大小",30,P.Gold);
            if(P.Button(new Rect(520,672,240,Compact?100:78),"标准",profile.textScale<1.1f,true,30))profile.textScale=1f;
            if(P.Button(new Rect(790,672,240,Compact?100:78),"较大",profile.textScale>=1.1f&&profile.textScale<1.25f,true,30))profile.textScale=1.15f;
            if(P.Button(new Rect(1060,672,280,Compact?100:78),"大字",profile.textScale>=1.25f,true,30))profile.textScale=1.3f;
        }
        void EvidenceChoice(string key,string label,Rect rect)
        {
            bool selected=engine.State.deductions.Contains(key);
            if(P.Button(rect,(selected?"✓ ":"")+label,selected,true,Compact?33:24))
            {if(selected)engine.State.deductions.Remove(key);else engine.State.deductions.Add(key);Persist();}
        }
        void DrawReasoning(bool memory)
        {
            MiniPanel(memory?"残 页 · 见 闻 印 证":"双 卷 · 规 律 推 演",memory?"画面拼合了。选出能辨认两位执卷者的两处证据，再判断这页记忆为何值得留下。":"名与形已经相照。结合相柳与罔象的经历，判断双卷应如何相处。");
            if(memory)
            {
                P.Image(new Rect(260,315,545,295),Art("Investigations/memory"));
                string[] keys={"red-brush","blue-scroll","lantern","leaves"},labels={"朱衣之人随身携笔","青衣之人带着卷轴","远处点亮的灯火","树上落下的秋叶"};
                for(int i=0;i<4;i++)EvidenceChoice(keys[i],labels[i],new Rect(840,310+i*82,500,74));
            }
            else
            {
                P.Label(new Rect(300,320,1000,200),"相柳：只剩恶名，便掩去了它的困惑。\n罔象：不辨善恶，遗忘也会带走无辜。\n两卷合看，才能既留下见证，也容纳改变。",Compact?36:29,P.Paper);
            }
            string[] answers=memory?new[]{"用灯火的多少判定年代","以两人的器物印证共同旅途","只记下山川，抹去人物"}:new[]{"只留下名字，形影不再重要","记名与存形相互印证","让所有见闻都被遗忘"};
            for(int i=0;i<3;i++)if(P.Button(new Rect(235+i*382,650,365,Compact?92:76),answers[i],false,true,Compact?33:24))
            {
                bool correct=memory?WorkshopRules.MemoryConclusion(engine.State,i):WorkshopRules.MirrorConclusion(engine.State,i);
                if(correct){engine.State.deductions.Add(memory?"memory-understood":"duality-understood");DoneMechanic(memory?"你认出了朱笔与青卷，散失的共同记忆重新相接。":"记与忘相互制约，也彼此成全。双卷映照完成。");}
                else {miniMessage=memory?"先找出两位执卷者各自的器物。景物相似，还不足以认出人。":"只有名字或只有遗忘，都无法留下完整的见闻。再比较两段经历。";audioBus.Sound("error");}
            }
        }
    }
}
