using System;
using System.Collections.Generic;
using UnityEngine;
namespace ShanHai
{
    public static class LocalSaves
    {
        const string Prefix="shanhai.v1.";
        static StoryData story;
        static readonly Dictionary<string,string> validated=new Dictionary<string,string>();
        public static string LastError="",RecoveryNotice="";
        public static void Configure(StoryData data){story=data;validated.Clear();}
        static string Get(string key){return PlayerPrefs.GetString(Prefix+key,"");}
        static void Put(string key,string value){if(string.IsNullOrEmpty(value))PlayerPrefs.DeleteKey(Prefix+key);else PlayerPrefs.SetString(Prefix+key,value);}
        static void Validate(string key,string raw)
        {
            string known;if(validated.TryGetValue(key,out known)&&known==raw)return;
            if(key=="profile")SaveValidation.Profile(story,JsonUtility.FromJson<PlayerProfile>(raw),s=>new StoryEngine(story,new PlayerProfile()).Restore(JsonUtility.FromJson<SaveState>(s)));
            else new StoryEngine(story,new PlayerProfile()).Restore(JsonUtility.FromJson<SaveState>(raw));
            validated[key]=raw;
        }
        static string ReadRaw(string key)
        {
            string raw=Get(key);if(raw=="")return "";
            try{Validate(key,raw);return raw;}
            catch {
                string backup=Get(key+".backup");
                try {
                    if(backup=="")throw new ArgumentException();
                    Validate(key,backup);Put(key,backup);PlayerPrefs.Save();
                    RecoveryNotice="发现损坏记录，已恢复上一份有效备份。请在存档页导出旅程备份。";
                    return backup;
                } catch {LastError="本地记录已损坏。可在存档页导入备份，或选择其他卷页。";return "";}
            }
        }
        static bool Store(string key,string json)
        {
            string previous=Get(key),previousBackup=Get(key+".backup");
            try {
                Validate(key,json);
                if(previous!=""&&previous!=json){
                    try{Validate(key,previous);Put(key+".backup",previous);}catch{}
                }
                Put(key,json);PlayerPrefs.Save();validated[key]=json;LastError="";return true;
            } catch(Exception e){
                // Keep the last working values if storage rejects this write.
                try{Put(key,"");Put(key+".backup","");Put(key,previous);Put(key+".backup",previousBackup);PlayerPrefs.Save();}catch{}
                validated.Remove(key);LastError="保存未完成，请检查浏览器存储权限并导出备份："+e.Message;return false;
            }
        }
        public static bool Save(int slot,SaveState state)
        {state.savedAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm");return Store("slot"+slot,JsonUtility.ToJson(state));}
        public static SaveState Read(int slot)
        {try{string raw=ReadRaw("slot"+slot);return raw==""?null:JsonUtility.FromJson<SaveState>(raw);}catch(Exception e){LastError="无法读取本地存档："+e.Message;return null;}}
        public static PlayerProfile LoadProfile()
        {
            try{
                string raw=ReadRaw("profile");var p=raw==""?new PlayerProfile():JsonUtility.FromJson<PlayerProfile>(raw);
                p.musicVolume=Mathf.Clamp01(p.musicVolume);p.soundVolume=Mathf.Clamp01(p.soundVolume);
                p.textSpeed=Mathf.Clamp(p.textSpeed,10,80);p.autoDelay=Mathf.Clamp(p.autoDelay,1,6);
                p.textScale=Mathf.Clamp(p.textScale<=0?1:p.textScale,1,1.3f);return p;
            }catch(Exception e){LastError="无法读取收藏："+e.Message;return new PlayerProfile();}
        }
        public static bool SaveProfile(PlayerProfile p){return Store("profile",JsonUtility.ToJson(p));}
        public static string Export(SaveState current=null,PlayerProfile currentProfile=null)
        {
            var bundle=new SaveBundle{version=1,profile=currentProfile??LoadProfile(),slots=new string[7]};
            for(int i=0;i<7;i++)bundle.slots[i]=ReadRaw("slot"+i);
            if(current!=null)bundle.slots[0]=JsonUtility.ToJson(current);
            return SaveArchiveCodec.Encode(bundle);
        }
        public static SaveBundle Inspect(string archive){return SaveArchiveCodec.Decode(archive,story);}
        public static bool HasRestorePoint {get{return Get("restorepoint")!="";}}
        public static string RestorePoint(){return Get("restorepoint");}
        public static bool Import(string archive)
        {
            // Parse and validate every slot and chapter before touching local data.
            var bundle=Inspect(archive);
            var before=new Dictionary<string,string>();
            foreach(string key in new[]{"profile","slot0","slot1","slot2","slot3","slot4","slot5","slot6","restorepoint"}){
                before[key]=Get(key);if(key!="restorepoint")before[key+".backup"]=Get(key+".backup");
            }
            string restore=Export();
            try{
                Put("restorepoint",restore);
                Put("profile",JsonUtility.ToJson(bundle.profile));Put("profile.backup",before["profile"]);
                for(int i=0;i<7;i++){Put("slot"+i,bundle.slots[i]);Put("slot"+i+".backup",before["slot"+i]);}
                PlayerPrefs.Save();validated.Clear();LastError="";return true;
            }catch(Exception e){
                // Free partially written values first, so restoring larger old
                // values cannot itself exceed the remaining storage quota.
                foreach(var pair in before){try{Put(pair.Key,"");}catch{}}
                foreach(var pair in before){try{Put(pair.Key,pair.Value);}catch{}}
                try{PlayerPrefs.Save();}catch{}
                validated.Clear();LastError="导入未能保存，已保留原记录："+e.Message;return false;
            }
        }
    }
}
