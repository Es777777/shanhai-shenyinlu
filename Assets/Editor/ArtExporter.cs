using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ShanHai.Editor
{
    public static class ArtExporter
    {
        public static void Export()
        {
            const string output="Assets/StreamingAssets/Art";
            Directory.CreateDirectory(output);
            var records=new List<ArtFile>();
            foreach(string path in Directory.GetFiles("Assets/Art","*.png",SearchOption.AllDirectories))
            {
                string normalized=path.Replace('\\','/');
                // This obsolete atlas was replaced by the dry sandstone edition.
                if(normalized.EndsWith("/mechanism-stone.png"))continue;
                string key=normalized.Substring("Assets/Art/".Length);key=key.Substring(0,key.Length-4);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path)))throw new Exception("Cannot decode "+path);
                bool alpha=key.StartsWith("Characters/")||key.StartsWith("Props/")||Array.IndexOf(new[]{"danxue-layers","mechanism-stone-dry","leaf-rings","ferry","bridge-layers","journey-items","workshop-paper","codex-items"},Path.GetFileNameWithoutExtension(path))>=0;
                // Preserve original resolution. Opaque painted backgrounds use JPEG;
                // sprites keep alpha and remain lossless, without platform texture assumptions.
                byte[] bytes=alpha?texture.EncodeToPNG():texture.EncodeToJPG(88);
                UnityEngine.Object.DestroyImmediate(texture);
                string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,16).ToLowerInvariant();
                string name=key.Replace('/','_')+"-"+hash+(alpha?".png":".jpg");
                File.WriteAllBytes(output+"/"+name,bytes);records.Add(new ArtFile{key=key,file=name,bytes=bytes.Length});
            }
            // Delete only obsolete generated files inside this exact output directory.
            var retained=new HashSet<string>();foreach(var r in records)retained.Add(r.file);
            foreach(var file in Directory.GetFiles(output))
                if((file.EndsWith(".png")||file.EndsWith(".jpg"))&&!retained.Contains(Path.GetFileName(file))){File.Delete(file);if(File.Exists(file+".meta"))File.Delete(file+".meta");}
            File.WriteAllText("Assets/Resources/ArtCatalog.json",JsonUtility.ToJson(new ArtCatalog{files=records.ToArray()},true));
            AssetDatabase.Refresh();Debug.Log("ART_STREAM_EXPORT: "+records.Count+" independently loaded images");
        }
        public static Texture2D Get(string key){return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/"+key+".png");}
    }
}
