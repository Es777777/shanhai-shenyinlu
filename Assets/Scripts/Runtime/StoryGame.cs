using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using P = ShanHai.PaintUI;

namespace ShanHai
{
    public sealed partial class StoryGame : MonoBehaviour
    {
        StoryEngine engine;
        PlayerProfile profile;
        GameAudio audioBus;
        ArtStream artStream;
        bool Compact { get { return Screen.height < 650 || Application.isMobilePlatform; } }
        bool playing, autoPlay, skipping;
        string panel = "", notice = "", confirmText = "";
        Action confirmed;
        float shownCharacters, lineTimer, transitionTime, noticeUntil;
        Vector2 scroll;
        int codexPage;
        string selectedCodex = "";
        const float Width = 1600, Height = 900;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (FindFirstObjectByType<StoryGame>() == null)
            {
                var go = new GameObject("山海神隐录");
                DontDestroyOnLoad(go); go.AddComponent<StoryGame>();
            }
        }
        void Awake()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black; camera.orthographic = true;
                cameraObject.AddComponent<AudioListener>();
            }
            Application.targetFrameRate = 60; Application.runInBackground = true;
            // Browsers may reject orientation locking, even when the API exists.
            // WebGL uses the rotation prompt and resizable landscape layout instead.
#if !UNITY_WEBGL || UNITY_EDITOR
            Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
            profile = LocalSaves.LoadProfile();
            artStream = gameObject.AddComponent<ArtStream>();
            var text = Resources.Load<TextAsset>("Story/story");
            if (text == null) { Debug.LogError("缺少剧情数据，请运行 Tools/import_story.py"); enabled = false; return; }
            engine = new StoryEngine(JsonUtility.FromJson<StoryData>(text.text),profile);
            P.Font = Resources.Load<Font>("Fonts/StorySerif");
            if (P.Font == null) P.Font = Font.CreateDynamicFontFromOSFont(new[] {"Microsoft YaHei","Noto Serif CJK SC","Arial"},28);
            audioBus = gameObject.AddComponent<GameAudio>(); audioBus.profile = profile;
            audioBus.PlayMusic("prologue");
        }
        void Update()
        {
            if (engine == null) return;
            transitionTime = Mathf.Max(0,transitionTime-Time.unscaledDeltaTime);
            if (!playing || panel != "" || confirmed != null || Screen.height > Screen.width || artStream.Busy || artStream.Error != null) return;
            if (!engine.DialogueFinished)
            {
                shownCharacters += Time.unscaledDeltaTime * profile.textSpeed;
                var line = engine.Node.lines[engine.State.line];
                if (shownCharacters >= line.text.Length)
                {
                    lineTimer += Time.unscaledDeltaTime;
                    if ((autoPlay && lineTimer >= profile.autoDelay) || (skipping && (profile.skipUnread || engine.CurrentLineRead) && lineTimer >= .08f)) Next();
                }
                if (skipping && !profile.skipUnread && !engine.CurrentLineRead) skipping = false;
            }
            else if (!engine.NeedsMechanic && engine.AvailableChoices().Length == 0 && !engine.IsEnding)
            {
                lineTimer += Time.unscaledDeltaTime;
                if (autoPlay && lineTimer > .9f || skipping && lineTimer > .15f) Next();
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Next();
            if (Input.GetKeyDown(KeyCode.Escape)) OpenPanel("menu");
        }
        void CancelPointerGesture()
        {
            tracing=draggingMount=false;drawnStroke.Clear();dragTile=-1;
            CancelFanStroke();
        }
        void OnApplicationPause(bool paused) { if (paused && playing) { CancelPointerGesture();Persist(); } }
        void OnApplicationFocus(bool focused) { if (!focused && playing) { CancelPointerGesture();Persist(); } }
        void OnApplicationQuit() { if (playing) Persist(); LocalSaves.SaveProfile(profile); }
        void Persist() { LocalSaves.Save(0,engine.State); LocalSaves.SaveProfile(profile); }
        Texture2D Art(string path) { return artStream.Get(path); }
        void NewGame()
        {
            engine.NewGame(); playing = true; panel = ""; autoPlay = skipping = false;
            OnNodeChanged();
        }
        void LoadGame(SaveState state)
        {
            if (state == null) { Notify("尚无这份存档"); return; }
            try { engine.Restore(state); playing = true; panel = ""; autoPlay = skipping = false; OnNodeChanged(false); }
            catch (Exception e) { Notify(e.Message); }
        }
        void OnNodeChanged(bool checkpoint = true)
        {
            noticeUntil = 0;
            shownCharacters = 0; lineTimer = 0; transitionTime = profile.reducedMotion ? 0 : .55f;
            ResetTransientMechanic();
            int ending = engine.Node.id == "N911" ? 1 : engine.Node.id == "N912" ? 2 : engine.Node.id == "N913" ? 3 : 0;
            audioBus.PlayMusic(engine.Node.music,ending);
            if (checkpoint && new[] {"N001","N101","N201","N301","N401","N501","N601","N701","N801","N901"}.Contains(engine.Node.id))
            {
                string key = engine.State.route + ":" + engine.Node.id;
                if (!profile.checkpoints.Any(c => c.nodeId == key))
                    profile.checkpoints.Add(new Checkpoint { nodeId=key, title=engine.Node.title, json=JsonUtility.ToJson(engine.State) });
            }

            Persist();
            if (!string.IsNullOrEmpty(engine.State.choiceReply)) Notify(engine.State.choiceReply,6);
        }
        void Next()
        {
            if (!playing || panel != "" || confirmed != null) return;
            if (!engine.DialogueFinished)
            {
                int len = engine.Node.lines[engine.State.line].text.Length;
                if (shownCharacters < len) { shownCharacters = len; return; }
                engine.ReadLine(); shownCharacters = 0; lineTimer = 0;
                Persist(); return;
            }
            if (engine.NeedsMechanic || engine.AvailableChoices().Length > 0) return;
            try
            {
                if (engine.Advance() == null) { Persist(); OpenPanel("credits"); }
                else OnNodeChanged();
            }
            catch (Exception e) { Notify(e.Message); Debug.LogError(e); }
        }
        void Choose(StoryChoice choice)
        {
            audioBus.Sound("confirm"); engine.Select(choice.id); OnNodeChanged();
        }
        void Notify(string text, float seconds = 3) { notice = text; noticeUntil = Time.unscaledTime + seconds; }
        void OpenPanel(string name)
        {
            panel = name; scroll = Vector2.zero; skipping = false;
            CancelPointerGesture();
            if (playing) Persist();
        }
        void Ask(string text, Action action) { confirmText = text; confirmed = action; }
        void OnGUI()
        {
            if (engine == null) return;
            artStream.BeginFrame();
            P.SmallScreen = Compact;
            GUI.depth = 0;
            GUI.matrix = Matrix4x4.identity;
            P.Fill(new Rect(0,0,Screen.width,Screen.height),Color.black);
            if (Screen.height > Screen.width)
            {
                CancelPointerGesture();
                GUI.matrix = Matrix4x4.identity;
                P.Fill(new Rect(0,0,Screen.width,Screen.height),P.Ink);
                P.Label(new Rect(20,Screen.height/2-60,Screen.width-40,120),"请将设备横屏\n展开这一卷山海",24,P.Paper,TextAnchor.MiddleCenter);
                return;
            }
            float scale = Mathf.Min(Screen.width/Width,Screen.height/Height);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-Width*scale)/2,(Screen.height-Height*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            bool locked = panel != "" || confirmed != null || artStream.Busy || artStream.Error != null;
            GUI.enabled = !locked;
            if (playing) DrawGame(); else DrawTitle();
            GUI.enabled = true;
            if (panel != "") { GUI.enabled = confirmed == null && !artStream.Busy && artStream.Error == null; DrawPanel(); }
            GUI.enabled = true;
            if (confirmed != null) DrawConfirmation();
            if (Time.unscaledTime < noticeUntil)
            {
                P.Panel(new Rect(320,104,960,100),false);
                P.Label(new Rect(350,121,900,68),notice,23,P.Paper,TextAnchor.MiddleCenter);
            }
            if (!string.IsNullOrEmpty(LocalSaves.LastError)) P.Label(new Rect(60,850,1480,36),LocalSaves.LastError,20,P.Red);
            if (artStream.Busy || artStream.Error != null)
            {
                CancelPointerGesture();GUI.enabled=true;
                P.Fill(new Rect(0,0,Width,Height),new Color(.02f,.05f,.06f,.94f));
                P.Label(new Rect(280,345,1040,100),artStream.Error ?? "正在展开这一幕 · 余下 "+artStream.Remaining+" 幅卷页",36,P.Paper,TextAnchor.MiddleCenter);
                if(artStream.Error!=null && P.Button(new Rect(600,495,400,100),"重新加载",true,true,34))artStream.Retry();
            }
            GUI.matrix = Matrix4x4.identity;
        }
        void DrawTitle()
        {
            if(Compact){DrawCompactTitle();return;}
            P.Image(new Rect(0,0,Width,Height),Art("Backgrounds/tianque"));
            for (int i=0;i<18;i++) P.Fill(new Rect(0,i*50,Width,50),new Color(.015f,.055f,.065f,.08f+ i*.012f));
            P.Fill(new Rect(0,0,630,Height),new Color(.015f,.045f,.05f,.70f));
            P.Line(new Vector2(625,75),new Vector2(625,825),new Color(P.Gold.r,P.Gold.g,P.Gold.b,.5f));
            P.Label(new Rect(100,80,470,35),"SHAN HAI  ·  A CHRONICLE OF NAMES",18,P.Gold);
            P.Label(new Rect(90,145,500,220),"山海\n神隐录",82,P.Paper);
            P.Label(new Rect(102,393,460,55),"以名观物　以忘容生",26,P.Gold);
            P.Line(new Vector2(102,468),new Vector2(520,468),P.Gold);
            if (P.Button(new Rect(102,505,418,62),"启 卷  ·  开始旅程",true, true,28))
            {
                if (LocalSaves.Read(0) != null) Ask("开始新旅程将更新自动存档。手动存档与已收集内容会保留。",NewGame); else NewGame();
            }
            if (P.Button(new Rect(102,583,418,62),"续 卷  ·  继续旅程",false,LocalSaves.Read(0)!=null,28)) LoadGame(LocalSaves.Read(0));
            if (P.Button(new Rect(102,668,196,52),"山海图鉴")) OpenPanel("codex");
            if (P.Button(new Rect(324,668,196,52),"结局收藏")) OpenPanel("endings");
            if (P.Button(new Rect(102,736,126,48),"章节")) OpenPanel("chapters");
            if (P.Button(new Rect(248,736,126,48),"读档")) OpenPanel("load");
            if (P.Button(new Rect(394,736,126,48),"设置")) OpenPanel("settings");
            P.Label(new Rect(1040,770,460,50),"名字若被遗忘，山海可还存在？",24,P.Paper,TextAnchor.MiddleRight);
            P.Label(new Rect(1050,825,450,28),"双卷分途  /  三卷终章",18,P.Gold,TextAnchor.MiddleRight);
        }
        void DrawGame()
        {
            var n=engine.Node;
            bool danxue=n.id=="N401"||n.id=="N402";
            if(danxue)DrawDanxueBackground();
            else if(CurrentWorld!=null)DrawWorldBackground();
            else if(engine.IsEnding)P.Image(new Rect(0,0,Width,Height),Art("Investigations/"+EndingArt(n.id=="N911"?1:n.id=="N912"?2:3)));
            else {P.Image(new Rect(0,0,Width,Height),Art("Backgrounds/"+n.background));DrawAtmosphere();}
            bool exploring = engine.DialogueFinished && engine.NeedsMechanic && (n.mechanic=="explore" || n.mechanic=="climb");
            if (!exploring && !engine.IsEnding) DrawCharacters();
            if (!(engine.IsEnding && engine.DialogueFinished)) DrawHUD();
            if (!engine.DialogueFinished) DrawDialogue();
            else if (engine.NeedsMechanic) DrawMechanic();
            else
            {
                var choices = engine.AvailableChoices();
                if (choices.Length>0) DrawChoices(choices);
                else
                {
                    if(engine.IsEnding){DrawEndingClose();DrawToolbar();return;}
                    P.Panel(new Rect(60,710,1480,134));
                    P.Label(new Rect(100,730,1050,70),engine.IsEnding ? "此卷已成。山海因你的抉择，留下了新的篇章。" : "这一幕已经完成。",28,P.Paper,TextAnchor.MiddleLeft);
                    if (P.Button(new Rect(1215,749,265,56),engine.IsEnding?"收卷 · 记录结局":"继续行卷",true)) Next();
                }
            }
            DrawToolbar();
            if (transitionTime>0 && Event.current.type==EventType.Repaint) P.Fill(new Rect(0,0,Width,Height),new Color(.025f,.05f,.055f,Mathf.Clamp01(transitionTime/.55f)));
        }
        void DrawAtmosphere()
        {
            if (profile.reducedMotion) return;
            float time=Time.unscaledTime;
            for (int i=0;i<32;i++)
            {
                float x=(i*157.31f+time*(4+i%5))%Width;
                float y=(i*83.7f-time*(5+i%7))%Height; if (y<0)y+=Height;
                var c=engine.Node.background=="zhongshan" ? new Color(.88f,.96f,1,.45f) : new Color(.91f,.78f,.49f,.15f+.2f*Mathf.Sin(time+i));
                P.Fill(new Rect(x,y,i%3+2,i%3+2),c);
            }
        }
        void DrawCharacters()
        {
            string[] ids=engine.Node.characters ?? new string[0];
            string speaker=engine.DialogueFinished ? "" : engine.Node.lines[engine.State.line].speaker;
            int count=Math.Min(ids.Length,3);
            for(int i=0;i<count;i++)
            {
                float x=count==1 ? 915 : count==2 ? (i==0?130:990) : 90+i*510;
                bool active=NameFor(ids[i])==speaker || speaker=="旁白" || speaker=="";
                DrawCharacter(ids[i],new Rect(x,180,460,580),active?1:.61f,ExpressionFor(speaker));
            }
        }
        static string NameFor(string id)
        {
            switch(id){case "xuanyao":return "玄曜";case "lingxi":return "灵曦";case "baize":return "白泽";case "zhuyin":return "烛阴";case "xiangliu":return "相柳";case "dinv":return "帝女";case "wuxian":return "巫咸";case "wuxian2":return "巫彭";case "gonggong":return "共工";default:return "罔象";}
        }
        int ExpressionFor(string speaker)
        { return engine.DialogueFinished ? 0 : Mathf.Clamp(engine.Node.lines[engine.State.line].expression,0,4); }
        void DrawCharacter(string id,Rect rect,float opacity,int frame=0)
        {
            var tex=Art("Characters/"+id); if(tex==null)return;
            int frames=id=="xuanyao"||id=="lingxi"?5:1;
            float aspect=(float)tex.width/frames/tex.height;
            float w=rect.height*aspect;
            Rect target=new Rect(rect.center.x-w/2,rect.y,w,rect.height);
            Color old=GUI.color; GUI.color=new Color(opacity,opacity,opacity,1);
            if(frames>1)DrawPortraitEdges(target,tex,new Rect((Mathf.Clamp(frame,0,4)+.008f)/5f,0,.984f/5,1));
            else GUI.DrawTexture(target,tex,ScaleMode.ScaleToFit,true);
            GUI.color=old;
        }
        void DrawHUD()
        {
            if(Compact){DrawCompactHUD();return;}
            if(engine.Node.id=="N401"||engine.Node.id=="N402"||CurrentWorld!=null){DrawDanxueHUD();return;}
            P.Fill(new Rect(0,0,Width,94),new Color(.015f,.045f,.055f,.88f));
            P.Label(new Rect(46,20,650,50),engine.Node.title,28,P.Paper,TextAnchor.MiddleLeft);
            P.Label(new Rect(720,28,230,38),"碎片 "+engine.Get("fragments")+"    烛火 "+engine.Get("candle"),20,P.Muted,TextAnchor.MiddleRight);
            P.Label(new Rect(985,17,190,35),"明昧  "+engine.Get("mcv"),23,P.Gold,TextAnchor.MiddleCenter);
            P.Fill(new Rect(982,63,202,4),P.Muted);
            P.Fill(new Rect(982,63,202*engine.Get("mcv")/100f,4),P.Red);
            P.Diamond(new Vector2(982+202*engine.Get("mcv")/100f,65),8,P.Gold);
            if(P.Button(new Rect(1240,23,135,48),"图鉴",false,true,23))OpenPanel("codex");
            if(P.Button(new Rect(1395,23,155,48),"行卷菜单",false,true,23))OpenPanel("menu");
        }
        void DrawDialogue()
        {
            var line=engine.Node.lines[engine.State.line];
            if(Compact || profile.textScale>1.02f){DrawReadableDialogue(line);return;}
            P.Panel(new Rect(60,625,1480,217));
            P.Fill(new Rect(89,646,4,39),line.speaker=="灵曦"?P.Jade:P.Gold);
            P.Label(new Rect(111,645,700,44),line.speaker,29,P.Gold);
            string text=line.text.Substring(0,Math.Min(line.text.Length,(int)shownCharacters));
            P.Label(new Rect(110,702,1265,102),text,31,P.Paper);
            P.Label(new Rect(1380,776,110,35),"继续  ›",23,P.Gold,TextAnchor.MiddleRight);
            if(GUI.Button(new Rect(65,689,1470,150),GUIContent.none,GUIStyle.none)){audioBus.Resume();Next();}
        }
        void DrawChoices(StoryChoice[] choices)
        {
            float h=100+choices.Length*90;
            Rect box=new Rect(330,420-h*.3f,940,h);
            P.Panel(box);
            P.Label(new Rect(box.x+40,box.y+20,860,50),engine.Node.id=="N803"?"落下最后一笔":"此刻，你将如何回应？",28,P.Gold,TextAnchor.MiddleCenter);
            for(int i=0;i<choices.Length;i++)
            {
                var c=choices[i]; string text=c.text;
                if(engine.Node.id=="N803")text+="  ·  "+(c.toward?"向中和靠近20":c.delta>0?"明昧 +20":"明昧 −20");
                if(P.Button(new Rect(box.x+55,box.y+83+i*90,830,68),text,i==0,true,27)) {Choose(c);break;}
            }
            autoPlay=false;skipping=false;
        }
        void DrawToolbar()
        {
            if(Compact){DrawCompactToolbar();return;}
            P.Fill(new Rect(0,850,Width,50),new Color(.015f,.04f,.05f,.96f));
            P.Label(new Rect(45,859,550,32),engine.State.route==""?"山海初卷":"执卷者 · "+engine.State.route,19,P.Muted);
            var names=new[]{autoPlay?"自动 · 开":"自动",skipping?"快进 · 开":"快进","回看","存档","读档","设置"};
            for(int i=0;i<names.Length;i++)if(P.Button(new Rect(800+i*125,856,115,37),names[i],false,true,20))
            {
                if(i==0){autoPlay=!autoPlay;skipping=false;}
                else if(i==1){skipping=!skipping;autoPlay=false;}
                else OpenPanel(i==2?"history":i==3?"save":i==4?"load":"settings");
            }
        }
    }
}
