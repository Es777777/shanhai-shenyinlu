using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

class GameLauncher
{
    public class Entry { public string path,sha256; public long bytes; }
    public class Manifest { public string version; public Entry[] files; }
    static string root;
    static string Hash(string path) { using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant(); }
    static string Resolve(string path)
    {
        string full=Path.GetFullPath(Path.Combine(root,path.Replace('/',Path.DirectorySeparatorChar)));
        if(!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Invalid file path");
        return full;
    }
    static bool Same(string url,string path)
    {
        var request=(HttpWebRequest)WebRequest.Create(url);request.Timeout=8000;request.Proxy=null;
        using(var response=request.GetResponse())using(var remote=response.GetResponseStream())using(var local=File.OpenRead(path))
        {
            var a=new byte[65536];var b=new byte[65536];int n;
            while((n=local.Read(a,0,a.Length))>0)
            {
                int got=0,k;while(got<n&&(k=remote.Read(b,got,n-got))>0)got+=k;
                if(got!=n)return false;for(int i=0;i<n;i++)if(a[i]!=b[i])return false;
            }
            return remote.ReadByte()==-1;
        }
    }
    static int Main(string[] args)
    {
        Console.OutputEncoding=new UTF8Encoding(false);
        try
        {
            int port=8766;bool open=!args.Contains("--no-browser");
            root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            for(int i=0;i<args.Length;i++)
            {
                if(args[i]=="--port")port=int.Parse(args[++i]);
                else if(args[i]=="--root")root=Path.GetFullPath(args[++i]).TrimEnd(Path.DirectorySeparatorChar);
            }
            if(!File.Exists(Path.Combine(root,"index.html")))
            {
                string project=Path.GetFullPath(Path.Combine(root,"../.."));
                string builds=Path.Combine(project,"Builds");
                if(Directory.Exists(builds))
                {
                    string latest=Directory.GetDirectories(builds,"H5*").Where(d=>File.Exists(Path.Combine(d,"index.html"))&&File.Exists(Path.Combine(d,"build-manifest.json"))).OrderByDescending(d=>File.GetLastWriteTimeUtc(Path.Combine(d,"build-manifest.json"))).FirstOrDefault();
                    if(latest!=null)root=latest;
                }
            }
            string manifestFile=Resolve("build-manifest.json");
            if(!File.Exists(manifestFile))throw new IOException("游戏文件不完整，请完整解压游玩包。");
            var manifest=new JavaScriptSerializer().Deserialize<Manifest>(File.ReadAllText(manifestFile,Encoding.UTF8));
            if(manifest.files==null||manifest.files.Length<4)throw new IOException("游戏文件清单无效。");
            Console.WriteLine("正在核对山海卷页，请稍候…");
            foreach(var file in manifest.files)
            {
                string path=Resolve(file.path);
                if(!File.Exists(path)||new FileInfo(path).Length!=file.bytes||Hash(path)!=file.sha256)
                    throw new IOException("卷页缺失或已损坏："+file.path+"。请重新完整解压。");
            }
            if(args.Contains("--verify")){Console.WriteLine("PASS: complete package "+manifest.version);return 0;}
            string url="http://127.0.0.1:"+port+"/";
            var listener=new TcpListener(IPAddress.Loopback,port);
            try { listener.Start(); }
            catch(SocketException)
            {
                bool same=false;
                try { same=Same(url+"build-manifest.json",manifestFile)&&manifest.files.All(f=>Same(url+Uri.EscapeUriString(f.path),Resolve(f.path))); }
                catch(WebException){}catch(IOException){}
                if(!same)throw new IOException("同一地址正在运行其他版本或服务。请关闭旧游戏的服务窗口，再启动本包。沿用原地址可继续访问浏览器存档。");
                Console.WriteLine("Preview already running: "+url);if(open)Process.Start(url);return 0;
            }
            Console.WriteLine("山海神隐录 "+manifest.version+"\n"+url+"\n游玩时保留这个窗口。关闭窗口即可停止本地服务。");
            if(open)Process.Start(url);
            while(true){var client=listener.AcceptTcpClient();Task.Run(()=>Serve(client));}
        }
        catch(Exception error){Console.Error.WriteLine(error.Message);return 1;}
    }
    static void Serve(TcpClient client)
    {
        using(client)
        try
        {
            client.ReceiveTimeout=10000;client.SendTimeout=10000;
            using(var stream=client.GetStream())
            {
                var bytes=new MemoryStream();int value;string header="";
                while(bytes.Length<8192&&(value=stream.ReadByte())>=0)
                {bytes.WriteByte((byte)value);if(value==10){header=Encoding.ASCII.GetString(bytes.ToArray());if(header.EndsWith("\r\n\r\n"))break;}}
                string[] first=header.Split('\n')[0].Trim().Split(' ');
                if(first.Length<2||(first[0]!="GET"&&first[0]!="HEAD")){Reply(stream,405,"text/plain",new byte[0],false);return;}
                string relative=Uri.UnescapeDataString(first[1].Split('?')[0]).TrimStart('/');
                if(relative=="")relative="index.html";
                string path;try{path=Resolve(relative);}catch(IOException){Reply(stream,403,"text/plain",new byte[0],false);return;}
                if(!File.Exists(path)){Reply(stream,404,"text/plain",new byte[0],false);return;}
                string ext=Path.GetExtension(path).ToLowerInvariant();
                string mime=ext==".html"?"text/html; charset=utf-8":ext==".json"?"application/json":ext==".js"?"application/javascript":ext==".wasm"?"application/wasm":ext==".png"?"image/png":ext==".jpg"?"image/jpeg":"application/octet-stream";
                using(var file=File.OpenRead(path))
                {
                    byte[] response=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: "+mime+"\r\nContent-Length: "+file.Length+"\r\nCache-Control: no-cache\r\nX-Content-Type-Options: nosniff\r\nConnection: close\r\n\r\n");
                    stream.Write(response,0,response.Length);if(first[0]!="HEAD")file.CopyTo(stream);
                }
            }
        }
        catch(IOException){}catch(SocketException){}catch(ArgumentException){}
    }
    static void Reply(Stream stream,int status,string mime,byte[] body,bool head)
    {
        byte[] header=Encoding.ASCII.GetBytes("HTTP/1.1 "+status+" Error\r\nContent-Type: "+mime+"\r\nContent-Length: "+body.Length+"\r\nConnection: close\r\n\r\n");stream.Write(header,0,header.Length);if(!head)stream.Write(body,0,body.Length);
    }
}
