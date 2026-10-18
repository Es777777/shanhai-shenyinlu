using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ShanHai
{
    [Serializable] public class ArtFile { public string key,file; public long bytes; }
    [Serializable] public class ArtCatalog { public ArtFile[] files; }
    // Only the current scene and requested collection pages are downloaded.
    // The catalog and hashed URLs are shared by the browser and the local launcher.
    public sealed class ArtStream : MonoBehaviour
    {
        readonly Dictionary<string,Texture2D> loaded=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,ArtFile> catalog=new Dictionary<string,ArtFile>();
        readonly Dictionary<string,int> touched=new Dictionary<string,int>();
        readonly HashSet<string> pending=new HashSet<string>(),wanted=new HashSet<string>();
        readonly Queue<string> queue=new Queue<string>();
        int active;
        public string Error { get; private set; }
        public bool Busy { get { return pending.Count>0; } }
        public int Remaining { get { return pending.Count; } }
        void Awake()
        {
            var manifest=Resources.Load<TextAsset>("ArtCatalog");
            if(manifest!=null)foreach(var entry in JsonUtility.FromJson<ArtCatalog>(manifest.text).files)catalog[entry.key]=entry;
        }
        public void BeginFrame(){wanted.Clear();}
        public Texture2D Get(string key)
        {
            wanted.Add(key);touched[key]=Time.frameCount;
            Texture2D texture;if(loaded.TryGetValue(key,out texture))return texture;
#if UNITY_EDITOR
            texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/"+key+".png");
            if(texture!=null){loaded[key]=texture;return texture;}
#endif
            if(!pending.Contains(key)&&Error==null)
            {
                if(!catalog.ContainsKey(key)){Error="这幅卷页尚未准备好，请重新解压完整游戏包。";return null;}
                pending.Add(key);queue.Enqueue(key);
            }
            return null;
        }
        void Update()
        {
            while(active<3&&queue.Count>0){active++;StartCoroutine(Load(queue.Dequeue()));}
            if(Time.frameCount%180==0)Trim();
        }
        IEnumerator Load(string key)
        {
            string baseUrl=Application.streamingAssetsPath;
            if(!baseUrl.Contains("://"))baseUrl=new Uri(baseUrl+"/").AbsoluteUri.TrimEnd('/');
            using(var request=UnityWebRequestTexture.GetTexture(baseUrl+"/Art/"+catalog[key].file,key!="Investigations/workshop-paper"))
            {
                request.timeout=35;yield return request.SendWebRequest();
                if(request.result==UnityWebRequest.Result.Success)
                {
                    var texture=DownloadHandlerTexture.GetContent(request);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
                    loaded[key]=texture;
                }
                else Error="卷页未能加载，请检查连接后重试。已保存的旅程仍在。";
            }
            pending.Remove(key);active--;
        }
        public void Retry(){Error=null;}
        void Trim()
        {
            long bytes=loaded.Values.Sum(t=>(long)t.width*t.height*4);
            foreach(var key in loaded.Keys.Where(k=>!wanted.Contains(k)).OrderBy(k=>touched.ContainsKey(k)?touched[k]:0).ToArray())
            {
                if(bytes<=72L*1024*1024)break;
                var texture=loaded[key];bytes-=(long)texture.width*texture.height*4;loaded.Remove(key);
#if !UNITY_EDITOR
                Destroy(texture);
#endif
            }
        }
    }
}
