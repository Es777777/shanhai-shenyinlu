using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace ShanHai
{
    [Serializable] public class SaveArchive
    {
        public int version;
        public string format,createdAt,payload,sha256;
    }
    [Serializable] public class SaveBundle
    {
        public int version;
        public PlayerProfile profile;
        public string[] slots;
    }
    public static class SaveArchiveCodec
    {
        public const int MaxBytes=4*1024*1024;
        static string Hash(string text)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        public static string Encode(SaveBundle bundle)
        {
            string payload=JsonUtility.ToJson(bundle);
            string result=JsonUtility.ToJson(new SaveArchive{version=1,format="shanhai-save",createdAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm"),payload=payload,sha256=Hash(payload)});
            if(Encoding.UTF8.GetByteCount(result)>MaxBytes)throw new ArgumentException("旅程备份超过大小限制");
            return result;
        }
        public static SaveBundle Decode(string json,StoryData story)
        {
            if(string.IsNullOrEmpty(json)||Encoding.UTF8.GetByteCount(json)>MaxBytes)throw new ArgumentException("备份为空或超过 4 MiB");
            SaveArchive archive;
            try{archive=JsonUtility.FromJson<SaveArchive>(json);}catch{throw new ArgumentException("文件不是有效的旅程备份");}
            if(archive==null||archive.format!="shanhai-save"||archive.version!=1||string.IsNullOrEmpty(archive.payload)||archive.sha256!=Hash(archive.payload))
                throw new ArgumentException("备份版本不支持或文件已损坏，原存档未改变");
            var bundle=JsonUtility.FromJson<SaveBundle>(archive.payload);
            if(bundle==null||bundle.version!=1||bundle.slots==null||bundle.slots.Length!=7)throw new ArgumentException("备份存档位不完整");
            Action<string> check=s=>{new StoryEngine(story,new PlayerProfile()).Restore(JsonUtility.FromJson<SaveState>(s));};
            SaveValidation.Profile(story,bundle.profile,check);
            foreach(string slot in bundle.slots)if(!string.IsNullOrEmpty(slot))check(slot);
            return bundle;
        }
    }
}
