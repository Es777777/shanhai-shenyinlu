using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using ShanHai;
// Deterministic storage fault injection; actual browser file transfer has separate tests.
namespace UnityEngine {
    public static class JsonUtility {
        static JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=10000000};
        public static string ToJson(object value){return json.Serialize(value);}
        public static T FromJson<T>(string value){return json.Deserialize<T>(value);}
    }
    public static class Mathf {
        public static float Clamp(float x,float a,float b){return Math.Max(a,Math.Min(b,x));}
        public static float Clamp01(float x){return Clamp(x,0,1);}
    }
    public static class PlayerPrefs {
        public static Dictionary<string,string> values=new Dictionary<string,string>();
        public static bool failNextSave;
        public static string failSetKey="";
        public static string GetString(string key,string fallback){string v;return values.TryGetValue(key,out v)?v:fallback;}
        public static void SetString(string key,string value){if(key==failSetKey){failSetKey="";throw new IOException("Injected quota rejection");}values[key]=value;}
        public static void DeleteKey(string key){values.Remove(key);}
        public static void Save(){if(failNextSave){failNextSave=false;throw new IOException("Injected storage failure");}}
    }
}
static class SaveTests {
    static string Snapshot(){return string.Join("\n",UnityEngine.PlayerPrefs.values.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value).ToArray());}
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static void Rejected(Action action,string label){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,label);}
    static PlayerProfile capacityProfile=new PlayerProfile();
    static string longestSave="";
    static void ExploreCapacity(StoryData data,SaveState state,int depth){
        if(depth>40)throw new Exception("Capacity graph loop");
        var engine=new StoryEngine(data,capacityProfile);engine.Restore(UnityEngine.JsonUtility.FromJson<SaveState>(UnityEngine.JsonUtility.ToJson(state)));
        if(Array.IndexOf(new[]{"N001","N101","N201","N301","N401","N501","N601","N701","N801","N901"},state.nodeId)>=0){
            string key=state.route+":"+state.nodeId;
            if(!capacityProfile.checkpoints.Exists(c=>c.nodeId==key))capacityProfile.checkpoints.Add(new Checkpoint{nodeId=key,title=engine.Node.title,json=UnityEngine.JsonUtility.ToJson(engine.State)});
        }
        while(!engine.DialogueFinished)engine.ReadLine();if(engine.NeedsMechanic)engine.FinishMechanic();
        if(engine.IsEnding){engine.Complete();string value=UnityEngine.JsonUtility.ToJson(engine.State);if(value.Length>longestSave.Length)longestSave=value;return;}
        var choices=engine.AvailableChoices();
        if(choices.Length==0){engine.Advance();ExploreCapacity(data,engine.State,depth+1);}
        else{string before=UnityEngine.JsonUtility.ToJson(engine.State);foreach(var choice in choices){engine.Restore(UnityEngine.JsonUtility.FromJson<SaveState>(before));engine.Select(choice.id);ExploreCapacity(data,engine.State,depth+1);}}
    }
    public static void Main(string[] args){try{Run(args);}catch(Exception e){Console.Error.WriteLine("FAIL: "+e.Message);Environment.Exit(1);}}
    static void Run(string[] args){
        var data=UnityEngine.JsonUtility.FromJson<StoryData>(File.ReadAllText(args[0]));LocalSaves.Configure(data);
        var engine=new StoryEngine(data,new PlayerProfile());
        Check(LocalSaves.Read(0)==null,"Empty slot stays empty");
        Check(LocalSaves.Save(0,engine.State),"Initial save");engine.ReadLine();Check(LocalSaves.Save(0,engine.State),"Second save");
        Check(LocalSaves.Read(0).line==1,"Current progress read");
        UnityEngine.PlayerPrefs.values["shanhai.v1.slot0"]="broken";
        Check(LocalSaves.Read(0).line==0,"Corrupt primary recovers previous valid state");
        Check(LocalSaves.RecoveryNotice!="","Recovery is disclosed");
        LocalSaves.Save(0,engine.State);LocalSaves.Save(2,engine.State);
        var p=new PlayerProfile{textScale=1.3f,musicVolume=.2f};p.unlocked.Add(data.codex[0].id);LocalSaves.SaveProfile(p);
        string archive=LocalSaves.Export();var bundle=LocalSaves.Inspect(archive);
        Check(bundle.slots.Length==7&&bundle.profile.textScale==1.3f&&bundle.slots[2]!="","Archive includes profile and manual slots");
        string rawBefore=UnityEngine.PlayerPrefs.GetString("shanhai.v1.slot0","");
        Rejected(()=>LocalSaves.Import(archive.Replace("shanhai-save","wrong-format")),"Wrong format rejected");
        var envelope=UnityEngine.JsonUtility.FromJson<SaveArchive>(archive);envelope.payload+=" ";
        Rejected(()=>LocalSaves.Import(UnityEngine.JsonUtility.ToJson(envelope)),"Checksum mismatch rejected");
        Rejected(()=>LocalSaves.Import("{}"),"Invalid object rejected");
        Rejected(()=>LocalSaves.Import(new string('x',SaveArchiveCodec.MaxBytes+1)),"Oversized file rejected");
        Check(rawBefore==UnityEngine.PlayerPrefs.GetString("shanhai.v1.slot0",""),"Invalid imports never overwrite progress");
        bundle.slots[0]="{}";Rejected(()=>LocalSaves.Import(SaveArchiveCodec.Encode(bundle)),"Invalid nested slot rejected");
        bundle=LocalSaves.Inspect(archive);bundle.profile.checkpoints.Add(new Checkpoint{nodeId="bad",title="bad",json="{}"});
        Rejected(()=>LocalSaves.Import(SaveArchiveCodec.Encode(bundle)),"Invalid nested chapter rejected");
        engine.ReadLine();LocalSaves.Save(0,engine.State);p.textScale=1;LocalSaves.SaveProfile(p);
        Check(LocalSaves.Import(archive),"Valid archive imports");Check(LocalSaves.Read(0).line==1&&LocalSaves.LoadProfile().textScale==1.3f,"Progress and settings restored together");
        Check(LocalSaves.HasRestorePoint,"Pre-import snapshot exists");
        Check(LocalSaves.Import(LocalSaves.RestorePoint())&&LocalSaves.Read(0).line==2&&LocalSaves.LoadProfile().textScale==1,"Restore pre-import snapshot");
        string before=Snapshot();
        UnityEngine.PlayerPrefs.failNextSave=true;Check(!LocalSaves.Import(archive),"Storage failure reports import failure");
        Check(before==Snapshot(),"Failed import rolls all keys back");
        UnityEngine.PlayerPrefs.failSetKey="shanhai.v1.slot2";Check(!LocalSaves.Import(archive),"Partial write rejection is reported");
        Check(before==Snapshot(),"Partially written values are removed before restoring old records");
        UnityEngine.PlayerPrefs.failNextSave=true;engine.ReadLine();Check(!LocalSaves.Save(0,engine.State)&&LocalSaves.Read(0).line==2,"Failed regular save keeps working state");
        p.textScale=1.15f;LocalSaves.SaveProfile(p);UnityEngine.PlayerPrefs.values["shanhai.v1.profile"]="broken";
        Check(LocalSaves.LoadProfile().textScale==1,"Profile corruption also recovers");
        UnityEngine.PlayerPrefs.values.Clear();LocalSaves.Configure(data);ExploreCapacity(data,new StoryEngine(data,new PlayerProfile()).State,0);
        LocalSaves.SaveProfile(capacityProfile);LocalSaves.SaveProfile(capacityProfile);
        for(int i=0;i<7;i++){var s=UnityEngine.JsonUtility.FromJson<SaveState>(longestSave);LocalSaves.Save(i,s);s.savedAt="capacity backup";LocalSaves.Save(i,s);}
        string fullArchive=LocalSaves.Export();LocalSaves.Import(fullArchive);LocalSaves.Import(fullArchive);
        long bytes=0;foreach(var pair in UnityEngine.PlayerPrefs.values)bytes+=System.Text.Encoding.UTF8.GetByteCount(pair.Key+pair.Value)+32;
        Console.WriteLine("Full collection + seven endgame slots + recovery keys: "+bytes+" bytes");
        Check(bytes<950000,"Full legitimate archive and rolling backups fit PlayerPrefs capacity");
        Console.WriteLine("PASS: "+checks+" save integrity, recovery and rollback checks.");
    }
}
