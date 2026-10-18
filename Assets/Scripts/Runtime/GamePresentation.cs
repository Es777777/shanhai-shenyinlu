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
            float y=Compact?704:672;
            P.Label(new Rect(200,y+5,290,60),"阅读字号",30,P.Gold);
            if(P.Button(new Rect(520,y,240,Compact?100:78),"标准",profile.textScale<1.1f,true,30))profile.textScale=1f;
            if(P.Button(new Rect(790,y,240,Compact?100:78),"较大",profile.textScale>=1.1f&&profile.textScale<1.25f,true,30))profile.textScale=1.15f;
            if(P.Button(new Rect(1060,y,280,Compact?100:78),"大字",profile.textScale>=1.25f,true,30))profile.textScale=1.3f;
        }
        void EvidenceChoice(string key,string label,Rect rect)
        {
            bool selected=engine.State.deductions.Contains(key);
            if(P.Button(rect,(selected?"✓ ":"")+label,selected,true,P.ReadingSize(27)))
            {if(selected)engine.State.deductions.Remove(key);else engine.State.deductions.Add(key);Persist();}
        }
        void DrawReasoning(bool memory)
        {
            if(engine.State.reasoningOrder.Count==0){
                engine.State.reasoningOrder.AddRange(new[]{0,1,2});
                for(int i=2;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);InteractionRules.Swap(engine.State.reasoningOrder,i,j);}Persist();
            }
            P.Fill(new Rect(0,94,Width,756),new Color(.015f,.035f,.04f,.45f));
            P.Image(new Rect(180,132,1240,680),Art("Investigations/worktable"));
            P.Fill(new Rect(220,150,1160,645),new Color(.025f,.02f,.015f,.7f));
            P.Label(new Rect(235,160,900,65),memory?"残 页 · 见 闻 印 证":"双 卷 · 规 律 推 演",34,P.Gold);
            P.ReadingLabel(new Rect(235,232,1120,62),memory?"哪些痕迹能证明两人曾同行？选两份证据，再作推论。":"哪些见闻暴露了双卷的局限？选两份证据，再作推论。",27);
            if(P.Button(new Rect(1140,150,230,85),"提示 "+engine.State.reasoningHints+" / 3",false,true,27)){
                engine.State.reasoningHints=Math.Min(3,engine.State.reasoningHints+1);miniMessage="";Persist();
            }
            if(memory)
            {
                P.Image(new Rect(250,305,530,240),Art("Investigations/memory"));
                string[] keys={"red-brush","blue-scroll","lantern","leaves"},labels={"朱衣随身携笔","青衣怀中卷轴","远处灯火数量","秋叶的颜色"};
                for(int i=0;i<4;i++)EvidenceChoice(keys[i],labels[i],new Rect(830,286+i*98,520,94));
            }
            else
            {
                P.ReadingLabel(new Rect(265,310,510,245),"相柳困在名字里。\n罔象困在遗忘里。\n双卷各自遗漏了什么？",27);
                string[] keys={"xiangliu-testimony","wangxiang-testimony","vent-testimony","boat-testimony"},labels={"相柳经历被恶名遮蔽","罔象遗忘波及无辜","丹穴风口已疏通","石舟重新获得平衡"};
                for(int i=0;i<4;i++)EvidenceChoice(keys[i],labels[i],new Rect(830,286+i*98,520,94));
            }
            string[] hints=memory?new[]{"先观察两个人，不急着看景物。","两人的随身之物，能与哪段旅途对应？","朱笔与青卷分别属于谁？两者要一起印证。"}:new[]{"回想记名与遗忘分别造成过什么后果。","两份证据应各自对应一种局限。","相柳与罔象的遭遇，分别缺少了什么？"};
            string feedback=miniMessage!=""?miniMessage:engine.State.reasoningHints>0?hints[engine.State.reasoningHints-1]:"证据不足时，可逐级展开提示。";
            P.ReadingLabel(new Rect(250,552,535,130),feedback,25,P.Gold);
            string[] answers=memory?new[]{"以灯火推断年代","以器物印证同行","以景物代替人物"}:new[]{"只留下名字","记名与存形互证","遗忘全部见闻"};
            for(int i=0;i<3;i++)if(P.Button(new Rect(235+i*382,685,365,104),answers[engine.State.reasoningOrder[i]],false,true,P.ReadingSize(28)))
            {
                int answer=engine.State.reasoningOrder[i];
                bool correct=memory?WorkshopRules.MemoryConclusion(engine.State,answer):WorkshopRules.MirrorConclusion(engine.State,answer);
                if(correct){engine.State.deductions.Add(memory?"memory-understood":"duality-understood");DoneMechanic(memory?"你认出了朱笔与青卷，散失的共同记忆重新相接。":"记与忘相互制约，也彼此成全。双卷映照完成。");}
                else {miniMessage=(memory?WorkshopRules.MemoryEvidence(engine.State.deductions):WorkshopRules.MirrorEvidence(engine.State.deductions))?"这条推论解释不了所选见闻，再比较一次。":"还需要两份相关证据，景物和结果未必能作证。";audioBus.Sound("error");}
            }
        }
    }
}
