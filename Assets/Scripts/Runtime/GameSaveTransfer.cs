using System;
using System.Linq;
using UnityEngine;
using P=ShanHai.PaintUI;
namespace ShanHai
{
    public sealed partial class StoryGame
    {
        string transferMessage="";
        void DrawSaveTransfer()
        {
            P.ReadingLabel(new Rect(215,215,1150,180),"将七个存档位、图鉴、结局与设置一起保存为文件。换浏览器或设备后，可以在这里导入继续。清理网站数据前，请先导出备份。",28);
            if(P.Button(new Rect(225,400,550,108),"导出旅程备份",true,true,32))
            {
                try{WebPlatform.Export(LocalSaves.Export(playing?engine.State:null,profile));transferMessage="已交给浏览器下载，请保管好备份文件。";}
                catch(Exception e){transferMessage="导出失败："+e.Message;}
            }
            if(P.Button(new Rect(825,400,550,108),"选择备份文件",false,true,32))WebPlatform.Import(gameObject.name);
            if(P.Button(new Rect(225,540,1150,108),"恢复导入前的记录",false,LocalSaves.HasRestorePoint,32))ReceiveSaveArchive(LocalSaves.RestorePoint());
            P.ReadingLabel(new Rect(225,675,1150,110),transferMessage==""?"每次写入还会保留上一份有效记录；发现损坏时会尝试恢复并提示。":transferMessage,25,P.Gold);
        }
        public void SaveFileError(string message){transferMessage=message;}
        public void ReceiveSaveArchive(string json)
        {
            try{
                var imported=LocalSaves.Inspect(json);
                int count=imported.slots.Count(s=>!string.IsNullOrEmpty(s));
                Ask("导入 "+count+" 份存档、"+imported.profile.unlocked.Count+" 篇图鉴和 "+imported.profile.endings.Count+" 个结局？\n将替换当前记录，原记录可在本页恢复。",()=>{
                    if(!LocalSaves.Import(json)){transferMessage=LocalSaves.LastError;return;}
                    var data=engine.Data;profile=LocalSaves.LoadProfile();engine=new StoryEngine(data,profile);audioBus.profile=profile;
                    playing=false;autoPlay=skipping=false;panel="backup";CancelPointerGesture();
                    audioBus.PlayMusic("prologue");transferMessage="导入完成。返回主菜单，选择续卷或读取存档继续。";
                });
            }catch(Exception e){transferMessage="无法导入："+e.Message;}
        }
    }
}
