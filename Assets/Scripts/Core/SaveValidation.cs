using System;
using System.Linq;
namespace ShanHai
{
    public static class SaveValidation
    {
        public static void Profile(StoryData data,PlayerProfile p,Action<string> validateCheckpoint)
        {
            if(p==null||p.version!=1||p.unlocked==null||p.readLines==null||p.endings==null||p.checkpoints==null)
                throw new ArgumentException("收藏与设置数据不完整");
            if(p.unlocked.Any(x=>!data.codex.Any(c=>c.id==x))||p.endings.Any(x=>x<1||x>3)||p.checkpoints.Count>80||p.readLines.Count>2000)
                throw new ArgumentException("收藏内容无效");
            foreach(float f in new[]{p.musicVolume,p.soundVolume,p.textSpeed,p.autoDelay,p.textScale})
                if(float.IsNaN(f)||float.IsInfinity(f))throw new ArgumentException("设置数值无效");
            foreach(var cp in p.checkpoints)
            {
                if(cp==null||string.IsNullOrEmpty(cp.nodeId)||string.IsNullOrEmpty(cp.json)||cp.title==null)
                    throw new ArgumentException("章节快照不完整");
                validateCheckpoint(cp.json);
            }
        }
    }
}
