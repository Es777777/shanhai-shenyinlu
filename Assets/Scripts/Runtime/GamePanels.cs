using System;
using System.Linq;
using UnityEngine;
using P=ShanHai.PaintUI;

namespace ShanHai
{
    public sealed partial class StoryGame
    {
        Vector2 readingScroll;
        int sliderControl;
        bool readingDrag;
        Vector2 readingPointer;
        void ReadingScroll(Rect view,ref Vector2 position,float total)
        {
            Event e=Event.current;
            if(GUI.enabled&&total>view.height&&e.type==EventType.MouseDown&&view.Contains(e.mousePosition)){readingDrag=true;readingPointer=e.mousePosition;}
            if(readingDrag&&e.type==EventType.MouseDrag){position.y+=readingPointer.y-e.mousePosition.y;readingPointer=e.mousePosition;e.Use();}
            if(e.rawType==EventType.MouseUp)readingDrag=false;
            position.y=Mathf.Clamp(position.y,0,Mathf.Max(0,total-view.height));
        }
        void DrawPanel()
        {
            P.Fill(new Rect(0,0,Width,Height),new Color(.01f,.035f,.04f,.82f));
            P.Panel(new Rect(120,80,1360,740));
            string title=panel=="menu"?"行 卷":panel=="history"?"前 尘 · 对话回看":panel=="save"?"留 卷 · 存档":panel=="load"?"续 卷 · 读档":panel=="settings"?"调 弦 · 设置":panel=="chapters"?"山 海 行 迹":panel=="codex"?"山 海 图 鉴":panel=="endings"?"终 卷 收 藏":"此 卷 已 成";
            if(panel=="backup")title="旅 程 备 份";
            P.Label(new Rect(170,112,1040,64),title,36,P.Gold);
            if(P.Button(new Rect(1310,Compact?98:112,112,Compact?90:48),"返回")){panel="";LocalSaves.SaveProfile(profile);return;}
            P.Line(new Vector2(170,191),new Vector2(1420,191),new Color(P.Gold.r,P.Gold.g,P.Gold.b,.4f));
            if(panel=="menu")DrawMenuPanel();
            else if(panel=="settings")DrawSettings();
            else if(panel=="load"||panel=="save")DrawSlots();
            else if(panel=="history")DrawHistory();
            else if(panel=="chapters")DrawChapters();
            else if(panel=="codex")DrawCodex();
            else if(panel=="endings")DrawEndings();
            else if(panel=="credits")DrawCredits();
            else if(panel=="backup")DrawSaveTransfer();
        }
        void DrawConfirmation()
        {
            P.Fill(new Rect(0,0,Width,Height),new Color(0,.02f,.025f,.86f));
            P.Panel(new Rect(350,245,900,415));
            P.Label(new Rect(405,280,790,215),confirmText,Compact?35:29,P.Paper,TextAnchor.MiddleCenter);
            if(P.Button(new Rect(425,505,320,110),"取消")){confirmed=null;return;}
            if(P.Button(new Rect(845,505,320,110),"确认",true)){var action=confirmed;confirmed=null;action();}
        }
        void DrawMenuPanel()
        {
            P.Label(new Rect(180,220,1140,70),engine.Node.title+"\n当前明昧 "+engine.Get("mcv")+" · 记忆碎片 "+engine.Get("fragments"),26,P.Paper);
            string[] labels={"保存旅程","读取存档","对话回看","章节选择","山海图鉴","结局收藏","声音与文字","返回主菜单"};
            string[] panels={"save","load","history","chapters","codex","endings","settings","title"};
            for(int i=0;i<labels.Length;i++)
            {
                int index=i;
                if(P.Button(new Rect(220+i%2*610,Compact?300+i/2*122:335+i/2*94,550,Compact?108:68),labels[i]))
                {
                    if(index==7)Ask("返回主菜单？当前旅程会自动保存。",()=>{Persist();playing=false;panel="";autoPlay=skipping=false;audioBus.PlayMusic("prologue");});
                    else OpenPanel(panels[index]);
                }
            }
        }
        void DrawSettings()
        {
            string[] names={"音乐音量","音效音量","文字速度","自动阅读停顿"};
            for(int i=0;i<4;i++){
                float y=Compact?200+i*98:220+i*85;
                P.Label(new Rect(200,y,330,80),names[i],27,P.Paper,TextAnchor.MiddleLeft);
                float value=i==0?profile.musicVolume:i==1?profile.soundVolume:i==2?profile.textSpeed:profile.autoDelay;
                value=Slider(new Rect(550,y,650,Compact?94:70),value,i==2?10:i==3?1:0,i==2?80:i==3?6:1);
                if(i==0)profile.musicVolume=value;else if(i==1)profile.soundVolume=value;else if(i==2)profile.textSpeed=value;else profile.autoDelay=value;
                P.Label(new Rect(1230,y,150,80),i<2?Mathf.RoundToInt(value*100)+"%":i==2?((int)value)+" 字/秒":value.ToString("0.0")+" 秒",23,P.Gold,TextAnchor.MiddleLeft);
            }
            if(P.Button(new Rect(200,590,560,Compact?100:62),profile.skipUnread?"快进范围：全部对白":"快进范围：仅已读对白"))profile.skipUnread=!profile.skipUnread;
            if(P.Button(new Rect(810,590,560,Compact?100:62),profile.reducedMotion?"减少动态效果：开":"减少动态效果：关"))profile.reducedMotion=!profile.reducedMotion;
            DrawTextScale();
            if(!Compact) P.Label(new Rect(200,770,1170,32),"存档保存在当前浏览器中。清理站点数据会移除本地存档。",20,P.Muted);
        }
        float Slider(Rect rect,float value,float min,float max)
        {
            int id=GUIUtility.GetControlID(FocusType.Passive);float cy=rect.center.y;
            P.Fill(new Rect(rect.x,cy-2,rect.width,4),new Color(.3f,.4f,.37f,1));
            float ratio=(value-min)/(max-min);
            P.Fill(new Rect(rect.x,cy-2,rect.width*ratio,4),P.Gold);
            P.Diamond(new Vector2(rect.x+rect.width*ratio,cy),Compact?20:12,P.Gold);
            Event e=Event.current;
            if(GUI.enabled&&e.type==EventType.MouseDown&&rect.Contains(e.mousePosition)){sliderControl=id;GUIUtility.hotControl=id;}
            if(GUI.enabled&&sliderControl==id&&(e.type==EventType.MouseDown||e.type==EventType.MouseDrag))
            {value=Mathf.Lerp(min,max,Mathf.Clamp01((e.mousePosition.x-rect.x)/rect.width));e.Use();}
            if(e.rawType==EventType.MouseUp&&sliderControl==id){sliderControl=0;GUIUtility.hotControl=0;}
            return value;
        }
        void DrawSlots()
        {
            for(int slot=0;slot<7;slot++)
            {
                int captured=slot;
                var state=LocalSaves.Read(slot);
                string node=state==null?"空白卷页":engine.Data.nodes.FirstOrDefault(n=>n.id==state.nodeId)?.title ?? "未知章节";
                float x=190+slot%2*625,y=225+slot/2*132;
                P.Panel(new Rect(x,y,590,113),false);
                P.Label(new Rect(x+20,y+12,550,34),(slot==0?"自动存档":"卷页 "+slot)+"  ·  "+node,23,P.Paper);
                P.Label(new Rect(x+20,y+62,360,32),state==null?"尚未写入":state.savedAt+"  "+state.route,19,P.Muted);
                bool save=panel=="save";
                if(P.Button(new Rect(x+412,Compact?y+15:y+57,156,Compact?90:40),save?"写入":"读取",false,save?playing&&slot!=0:state!=null,21))
                {
                    if(save)
                    {
                        Action action=()=>{bool ok=LocalSaves.Save(captured,engine.State);ok=LocalSaves.SaveProfile(profile)&&ok;Notify(ok?"已存入卷页 "+captured:LocalSaves.LastError);};
                        if(state!=null)Ask("覆盖卷页 "+captured+" 中的旧存档？",action);else action();
                    }
                    else if(playing)Ask("读取这份存档？当前进度已保存在自动存档中。",()=>LoadGame(state));
                    else LoadGame(state);
                }
            }
            if(P.Button(new Rect(815,621,590,108),"导出、导入与恢复备份",false,true,27)){transferMessage="";OpenPanel("backup");}
        }
        void DrawHistory()
        {
            var entries=engine.State.history;
            if(entries.Count==0){P.Label(new Rect(200,270,1150,100),"尚无已读对白。",28,P.Muted);return;}
            float total=entries.Sum(h=>Mathf.Max(100,P.ReadingStyle(27).CalcHeight(new GUIContent(h.text),1120)+65));
            Rect view=new Rect(185,220,1225,Compact?460:510);ReadingScroll(view,ref scroll,total);
            scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,1185,total));
            float y=0;
            foreach(var h in entries)
            {
                float height=Mathf.Max(100,P.ReadingStyle(27).CalcHeight(new GUIContent(h.text),1120)+65);
                P.Label(new Rect(15,y+5,1120,48),h.speaker,24,P.Gold);
                P.ReadingLabel(new Rect(15,y+55,1120,height-55),h.text,27,P.Paper);
                y+=height;
            }
            GUI.EndScrollView();
            if(P.Button(new Rect(1100,Compact?705:750,300,Compact?100:55),"查看最新对白",false,true,24))scroll.y=total;
        }
        void DrawChapters()
        {
            if(profile.checkpoints.Count==0){P.Label(new Rect(210,270,1150,90),"初次抵达章节后，这里会保存当时的旅程快照。",28,P.Muted);return;}
            P.Label(new Rect(200,216,1200,50),"从已到达的章节继续，保留当时的路线、数值与物品。",23,P.Muted);
            scroll=GUI.BeginScrollView(new Rect(190,280,1220,465),scroll,new Rect(0,0,1180,Mathf.Ceil(profile.checkpoints.Count/2f)*105));
            for(int i=0;i<profile.checkpoints.Count;i++)
            {
                var cp=profile.checkpoints[i];
                if(P.Button(new Rect(i%2*590,i/2*105,565,80),cp.title+"\n"+(cp.nodeId.StartsWith(":")?"序卷":cp.nodeId.Split(':')[0]),false,true,23))
                    Ask("从“"+cp.title+"”继续？当前旅程将切换到此章节的快照。",()=>LoadGame(JsonUtility.FromJson<SaveState>(cp.json)));
            }
            GUI.EndScrollView();
        }
        void DrawCodex()
        {
            var entries=engine.Data.codex;
            int pageCount=Mathf.CeilToInt(entries.Length/8f);
            codexPage=Mathf.Clamp(codexPage,0,pageCount-1);
            P.Label(new Rect(190,218,500,42),"已记 "+profile.unlocked.Count+" / "+entries.Length+" 篇",23,P.Muted);
            for(int index=codexPage*8;index<Math.Min(entries.Length,codexPage*8+8);index++)
            {
                int i=index-codexPage*8;var entry=entries[index];bool unlocked=profile.unlocked.Contains(entry.id);
                if(P.Button(new Rect(190+i%2*292,278+i/2*100,272,Compact?96:77),unlocked?entry.title+"  /  "+entry.category:"未遇 · 待录",selectedCodex==entry.id,unlocked,23)){selectedCodex=entry.id;readingScroll=Vector2.zero;}
            }
            if(P.Button(new Rect(190,700,170,Compact?100:60),"上一页",false,codexPage>0))codexPage--;
            P.Label(new Rect(355,704,200,40),(codexPage+1)+" / "+pageCount,21,P.Gold,TextAnchor.MiddleCenter);
            if(P.Button(new Rect(570,700,170,Compact?100:60),"下一页",false,codexPage<pageCount-1))codexPage++;
            var chosen=entries.FirstOrDefault(c=>c.id==selectedCodex&&profile.unlocked.Contains(c.id));
            if(chosen==null)chosen=entries.FirstOrDefault(c=>profile.unlocked.Contains(c.id));
            if(chosen!=null)
            {
                Rect art=new Rect(815,225,550,270);
                DrawCodexArt(chosen,art);
                P.Label(new Rect(815,510,550,60),chosen.title,34,P.Gold);
                float total=Mathf.Max(200,P.ReadingStyle(27).CalcHeight(new GUIContent(chosen.text),510));
                Rect view=new Rect(815,580,550,215);ReadingScroll(view,ref readingScroll,total);
                readingScroll=GUI.BeginScrollView(view,readingScroll,new Rect(0,0,510,total));
                P.ReadingLabel(new Rect(0,0,510,total),chosen.text,27,P.Paper);GUI.EndScrollView();
            }
            else P.Label(new Rect(840,365,490,170),"每一次相遇\n都将成为山海的一页。",30,P.Muted,TextAnchor.MiddleCenter);
        }
        static string EndingArt(int ending){return new[]{"ending-everbright","ending-coexist","ending-balance"}[Mathf.Clamp(ending-1,0,2)];}
        void DrawCodexArt(CodexEntry entry,Rect r)
        {
            int item=entry.id=="feather"?0:entry.id=="candle"?1:entry.id=="mulberry"?2:entry.id=="scroll"?3:-1;
            if(item>=0)
            {
                P.Image(r,Art("Investigations/worktable"));
                Atlas(new Rect(r.center.x-165,r.y+7,330,330),"Investigations/codex-items",item,2,2,Color.white);
                return;
            }
            string scene=entry.id=="fusang"?"fusang-room":entry.id=="jinwu"?"fusang-awake":entry.id=="phoenix"?"danxue-awake":entry.id=="memory"?"memory":entry.id=="truth"?"ending-balance":"";
            if(scene!="")P.Image(r,Art("Investigations/"+scene));
            else if(entry.art.StartsWith("bg:"))P.Image(r,Art("Backgrounds/"+entry.art.Substring(3)));
            else DrawCharacter(entry.art,r,1);
        }
        void DrawEndings()
        {
            string[] titles={"永明卷","共生卷","调和卷"};
            string[] subtitles={"灯火不息，见证相传。","记忘有序，聚散从容。","记有所托，忘有所安。"};
            for(int i=0;i<3;i++)
            {
                bool unlocked=profile.endings.Contains(i+1);float x=190+i*410;
                P.Panel(new Rect(x,250,380,440));
                if(unlocked)P.Image(new Rect(x+17,267,346,200),Art("Investigations/"+EndingArt(i+1)),new Color(1,1,1,.8f));
                else P.Diamond(new Vector2(x+190,365),60,P.Muted);
                P.Label(new Rect(x+30,490,320,60),unlocked?titles[i]:"尚未成卷",36,unlocked?P.Gold:P.Muted,TextAnchor.MiddleCenter);
                P.Label(new Rect(x+30,575,320,90),unlocked?subtitles[i]:"让旅途与抉择写下答案。",24,P.Paper,TextAnchor.MiddleCenter);
            }
            P.Label(new Rect(190,730,1200,45),"已收集 "+profile.endings.Count+" / 3  ·  结局在收卷时记录，开启新旅程仍会保留。",23,P.Muted,TextAnchor.MiddleCenter);
        }
        void DrawCredits()
        {
            string name=engine.Get("ending")==1?"永明卷":engine.Get("ending")==2?"共生卷":"调和卷";
            P.Label(new Rect(250,230,1100,75),name+" · 已收入结局册",43,P.Gold,TextAnchor.MiddleCenter);
            P.Label(new Rect(340,345,920,190),"原作剧情 · 山海神隐录分支剧情填写模板\n对白整理、游戏开发与原创视觉 · Codex\n音乐与音效 · 原创程序合成\n字体 · Noto Serif CJK / SIL Open Font License",26,P.Paper,TextAnchor.MiddleCenter);
            P.Label(new Rect(320,565,960,70),"感谢你为这一卷山海，留下名字。",30,P.Gold,TextAnchor.MiddleCenter);
            if(P.Button(new Rect(580,682,440,65),"归卷 · 返回主菜单",true))
            {Persist();playing=false;panel="";autoPlay=skipping=false;audioBus.PlayMusic("prologue");}
        }
    }
}
