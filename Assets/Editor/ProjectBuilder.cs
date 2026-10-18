using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShanHai.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("山海神隐录/准备场景与项目设置")]
        public static void Setup()
        {
            // Preserve source aspect ratios for painted scenes and expression atlases.
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Art"}))
            {
                var importer=AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                bool readable=AssetDatabase.GUIDToAssetPath(guid).EndsWith("/workshop-paper.png");
                if(importer!=null&&(importer.npotScale!=TextureImporterNPOTScale.None||importer.isReadable!=readable))
                {importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=readable;importer.SaveAndReimport();}
            }
            if(!Directory.Exists("Assets/Scenes"))Directory.CreateDirectory("Assets/Scenes");
            if(!File.Exists("Assets/Scenes/Story.unity"))
            {
                Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var cameraObject=new GameObject("Main Camera");
                var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;cameraObject.tag="MainCamera";
                cameraObject.AddComponent<AudioListener>();
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/Story.unity");
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Story.unity",true)};
            var storyScene=EditorSceneManager.OpenScene("Assets/Scenes/Story.unity",OpenSceneMode.Single);
            if(UnityEngine.Object.FindFirstObjectByType<StoryGame>()==null)
            {
                new GameObject("StoryGame").AddComponent<StoryGame>();
                EditorSceneManager.SaveScene(storyScene);
            }
            PlayerSettings.companyName="ShanHai Studio";PlayerSettings.productName="山海神隐录";PlayerSettings.bundleVersion="0.5.0";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.runInBackground=true;
            // IMGUI's built-in skin contains native MonoScript references. Retain
            // engine types so incremental WebGL builds do not strip those resources.
            PlayerSettings.stripEngineCode=false;
            PlayerSettings.WebGL.template="PROJECT:ShanHai";
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            QualitySettings.vSyncCount=0;
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("山海工程已准备完成。打开 Story 场景，点击 Play 即可试玩。");
        }
        [MenuItem("山海神隐录/构建 H5 WebGL")]
        public static void BuildWeb()
        {
            Setup();
            Validate();
            ArtExporter.Export();
            string output=Environment.GetEnvironmentVariable("SHANHAI_BUILD_OUTPUT") ?? "Builds/H5";
            Directory.CreateDirectory(output);
            // Unity 2022 does not forward -job-worker-count to native compilation.
            // Opt-in helper adds Bee's supported -j flag, without changing editor files.
            string wrapper=Environment.GetEnvironmentVariable("SHANHAI_BEE_WRAPPER");
            FieldInfo backend=null;object original=null;
            try
            {
                if(!string.IsNullOrEmpty(wrapper))
                {
                    if(!File.Exists(wrapper))throw new FileNotFoundException("Missing Bee concurrency helper",wrapper);
                    var driver=typeof(EditorApplication).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.UnityBeeDriver",true);
                    backend=driver.GetField("BeeBackendExecutable",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
                    if(backend==null)throw new NotSupportedException("This editor version does not expose the expected Bee backend field.");
                    original=backend.GetValue(null);backend.SetValue(null,wrapper);
                    Debug.Log("BUILD_CONCURRENCY_LIMIT: "+Environment.GetEnvironmentVariable("SHANHAI_BEE_JOBS"));
                }
                var options=Environment.GetEnvironmentVariable("SHANHAI_CLEAN_BUILD")=="1"?BuildOptions.CleanBuildCache:BuildOptions.None;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Story.unity"},locationPathName=output,target=BuildTarget.WebGL,options=options});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
                WriteManifest(output);
                Debug.Log("H5_BUILD_SUCCESS "+report.summary.totalSize+" bytes");
            }
            finally { if(backend!=null&&original!=null)backend.SetValue(null,original); }
        }
        [Serializable] class BuildFile { public string path,sha256;public long bytes; }
        [Serializable] class BuildManifest { public string version;public BuildFile[] files; }
        static void WriteManifest(string root)
        {
            var files=new System.Collections.Generic.List<BuildFile>();
            foreach(string path in Directory.GetFiles(root,"*",SearchOption.AllDirectories))
            {
                string name=path.Replace('\\','/').Substring(root.Length+1);
                if(name=="build-manifest.json")continue;
                using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(path))
                    files.Add(new BuildFile{path=name,bytes=stream.Length,sha256=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()});
            }
            File.WriteAllText(root+"/build-manifest.json",JsonUtility.ToJson(new BuildManifest{version="0.5.0",files=files.ToArray()},true));
        }
        [MenuItem("山海神隐录/检查剧情与资源")]
        public static void Validate()
        {
            var data=JsonUtility.FromJson<StoryData>(File.ReadAllText("Assets/Resources/Story/story.json"));
            new StoryEngine(data,new PlayerProfile());
            foreach(var node in data.nodes)
            {
                if(ArtExporter.Get("Backgrounds/"+node.background)==null)throw new Exception("Missing background "+node.background);
                foreach(var character in node.characters)if(ArtExporter.Get("Characters/"+character)==null)throw new Exception("Missing character "+character);
            }
            if(Resources.Load<Font>("Fonts/StorySerif")==null)throw new Exception("Missing font");
            foreach(string layer in new[]{"danxue-room","danxue-layers","danxue-awake","fusang-room","fusang-awake","zhongshan-room","zhongshan-thawed","zhongshan-awake","youdu-lamps-room","youdu-lamps-awake","xiongli-room","xiongli-covered","xiongli-awake","wangchuan-room","youdu-leaf-room","youdu-leaf-awake","buzhou-room","mechanism-stone-dry","leaf-rings","ferry","bridge-layers","journey-items","worktable","workshop-paper","memory","codex-items","ending-everbright","ending-coexist","ending-balance"})
                if(ArtExporter.Get("Investigations/"+layer)==null)throw new Exception("Missing scene layer "+layer);
            Debug.Log("VALIDATION_SUCCESS: "+data.nodes.Length+" nodes");
        }
    }
    public sealed class AssetDefaults:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Art/"))return;
            var t=(TextureImporter)assetImporter;
            t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=false;t.isReadable=assetPath.EndsWith("/workshop-paper.png");
            t.alphaIsTransparency=true;t.wrapMode=TextureWrapMode.Clamp;
            t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;
            t.textureCompression=TextureImporterCompression.Compressed;
            var web=t.GetPlatformTextureSettings("WebGL");
            web.overridden=true;web.maxTextureSize=2048;web.format=TextureImporterFormat.RGBA32;
            t.SetPlatformTextureSettings(web);
        }
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Resources/Audio/"))return;
            var a=(AudioImporter)assetImporter;var s=a.defaultSampleSettings;
            s.loadType=AudioClipLoadType.CompressedInMemory;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.45f;
            s.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;s.sampleRateOverride=24000;
            s.preloadAudioData=false;a.defaultSampleSettings=s;a.loadInBackground=true;
        }
    }
}
