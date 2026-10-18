using System.Runtime.InteropServices;
using UnityEngine;
namespace ShanHai
{
    public static class WebPlatform
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int ShanHai_Compact();
        [DllImport("__Internal")] static extern void ShanHai_Export(string name,string json);
        [DllImport("__Internal")] static extern void ShanHai_Import(string target);
        [DllImport("__Internal")] static extern void ShanHai_Overlay(int value);
#endif
        public static bool Compact
        {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return ShanHai_Compact()!=0;
#else
                return Screen.height<650 || Screen.width<1100 || Application.isMobilePlatform;
#endif
            }
        }
        static bool overlay;
        public static void SetOverlay(bool value)
        {
            if(overlay==value)return;overlay=value;
#if UNITY_WEBGL && !UNITY_EDITOR
            ShanHai_Overlay(value?1:0);
#endif
        }
        public static void Export(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShanHai_Export("山海神隐录_存档_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss")+".json",json);
#elif UNITY_EDITOR
            string path=UnityEditor.EditorUtility.SaveFilePanel("导出旅程备份","","山海神隐录_存档.json","json");
            if(path!="")System.IO.File.WriteAllText(path,json);
#endif
        }
        public static void Import(string target)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShanHai_Import(target);
#elif UNITY_EDITOR
            string path=UnityEditor.EditorUtility.OpenFilePanel("导入旅程备份","","json");
            if(path!="")GameObject.Find(target).SendMessage("ReceiveSaveArchive",System.IO.File.ReadAllText(path));
#endif
        }
    }
}
