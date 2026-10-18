using System;
using System.Collections.Generic;
using System.Linq;

namespace ShanHai
{
    public sealed class InvestigationDefinition
    {
        public string id,title,goal,tool,toolName,clueTitle,clue,instruction,success;
        public int sprite;
        public string[] hints;
        public InvestigationDefinition(string id,string title,string goal,string tool,string toolName,int sprite,string clueTitle,string clue,string instruction,string success,params string[] hints)
        {this.id=id;this.title=title;this.goal=goal;this.tool=tool;this.toolName=toolName;this.sprite=sprite;this.clueTitle=clueTitle;this.clue=clue;this.instruction=instruction;this.success=success;this.hints=hints;}
    }
    // Pure gameplay rules shared by saves, runtime and deterministic tests.
    public static class InvestigationRules
    {
        public static readonly InvestigationDefinition[] All={
            new InvestigationDefinition("N101","扶桑 · 借光辨形","让晨光照到高枝，看清金乌的形。","mirror","青铜镜",0,"树根古刻","光行于直，逢镜而折。三面镜接住晨光，高枝上的形影才会显现。","点击镜面改变斜向，让光从左侧穿过三面镜，到达右下的枝影。","晨光抵达高枝。三足之影映在树干上——这是金乌。","树根下落着一面青铜镜，空镜座正对着晨光。","拾起石台上的镜子，再点击空镜座；转动镜面观察光路。","三面镜由光线经过的顺序，依次应为 ／、／、＼。"),
            new InvestigationDefinition("N201","钟山 · 融冰辨时","揭开冰下铭文，开启通往洞府的石门。","ember","余烬提灯",2,"冰下铭文","闭目为夜，睁目为昼；昼夜既过，星辰引路。门上三眼，从左至右依时而开。","用提灯融开铭文，再转动三只石眼。石眼的图案有日、云、月、星。","石眼依次映出月、日、星。霜从门缝退开，洞府的烛光透了出来。","雪中的提灯仍有余烬，能融开薄冰。","提灯先用于冰碑，读到铭文后再检查石门。","三只石眼从左至右调成：月、日、星。"),
            new InvestigationDefinition("N301","幽都 · 名灯循序","以不灭烛唤醒名灯，循回声进入城门。","everflame","不灭烛",2,"城门残刻","水先醒，木承水，火随木，土藏火。灯不求长明，只求名字依次应答。","用不灭烛引灯，再按残刻的顺序点亮四盏名灯。点错会熄灭，重来即可。","四盏名灯依次应答。九重回声穿过城门，呼唤着相柳。","你带来的不灭烛，可以唤醒熄灭的名灯。","看城门旁的残刻，再点击熄灭的名灯，以持有的不灭烛引灯。","依次点击：水、木、火、土。"),
            new InvestigationDefinition("N401","丹穴 · 两翼生风","拂开余烬，调通凤巢下的风道。","fan","蒲叶扇",7,"凤巢壁画","两翼同振，腹息无风。左右风闸各牵动一翼与腹闸，中闸牵动三处。","点击风闸会连带改变相邻气道。让两翼通风、中央静止，火中的新羽才会展开。","两翼的气流托起凤凰。旧羽化为灰，新生的轮廓留在石壁上。","落在石阶边的扇子，能拂开封住风道的灰。","先用扇子清开机关。左闸动左与中，右闸动中与右。","从全部关闭开始，依次打开左闸、右闸；中央应保持关闭。"),
            new InvestigationDefinition("N403","凶犁 · 循河寻路","拂去浮沙，沿旧河道找到离开旱地的路。","brush","软毫拂尘",3,"河碑残纹","名字已被风磨去，河床的转折仍在。循其形，不必为它补一个旧名。","转动河纹石块，让流沙从左上入口连到右下出口。沙流会显示当前通路。","流沙沿故河散开，露出一条可行的石径。河碑仍旧无名。","碑脚的拂尘可以扫开遮住河纹的沙。","拾起拂尘，在河碑上按住来回轻扫；再转动石纹接通流沙。","从左上转向下，沿最左列向下，再沿最下排向右。"),
            new InvestigationDefinition("N501","忘川 · 渡舟配重","让渡舟恢复平衡，顺水抵达对岸。","ballast","镇舟石",5,"渡口刻度","罔象不辨善恶，舟只辨轻重。离支点越远，同一块石头便压得越重。","点击两块石头交换位置。石上的点代表重量，石头离船心的远近决定作用。","舟身平稳，水纹让出一条路。罔象沉入雾中，渡船向对岸移去。","岸边的镇舟石要放在船内四个座位上。","将镇舟石用于渡舟。两边不仅重量要合适，还要考虑座位距离。","从左至右，一组平衡摆法是：三、一、四、二。"),
            new InvestigationDefinition("N601","幽都 · 桑脉照门","让桑叶的脉络接通雾门上的三重圆纹。","leaf","帝女桑叶",4,"桑叶脉络","由根入叶，由叶见枝。三重叶脉要一一接上：外环向上，中环向下，内环向右。","将桑叶用于门心，点击外、中、内环转动。让青色叶脉与金色缺口相接。","叶脉贯通三重门纹，青光穿过幽雾。门后的声音渐渐清晰。","帝女留给你的桑叶，可以嵌入门心的叶形凹槽。","将桑叶用于雾门，再分别转动三个圆环。","外环朝上，中环朝下，内环朝右。"),
            new InvestigationDefinition("N801","不周 · 双卷架桥","借双卷之力补上残阶，共同抵达断脊。","rope","攀山绳",6,"残阶纹路","有名而裂者，以朱卷镇住；无形而空者，以青卷映出。二人轮换，方能同行。","先系安全绳。选择朱卷或青卷，再点相邻残阶：裂纹需朱卷，虚影需青卷。","朱与青接续成一条山路。两人一同抵达天缺断脊。","岩台上的绳索能系在断桥边的石桩上。","先用绳索保护，再从左下残阶开始。只可走相邻的一格。","从左下依次：朱、青（右）、朱（上）、青（右）、朱（上）、青（右）。")
        };
        public static InvestigationDefinition Find(string id){return All.FirstOrDefault(x=>x.id==id);}
        public static void Prepare(SaveState s,string node)
        {
            if(s.items==null)s.items=new List<string>();if(s.evidence==null)s.evidence=new List<string>();
            if(s.investigation!=null&&s.investigation.node==node&&s.investigation.dials!=null&&s.investigation.dials.Count==9)return;
            s.investigation=new InvestigationState{node=node,dials=Enumerable.Repeat(0,9).ToList()};
            int[] initial=node=="N101"?new[]{1,1,0}:node=="N501"?new[]{4,1,3,2}:node=="N601"?new[]{3,1,2}:node=="N403"?new[]{1,1,0,1,1,0,1,1,1}:new int[0];
            for(int i=0;i<initial.Length;i++)s.investigation.dials[i]=initial[i];
            if(node=="N301"&&!s.items.Contains("everflame"))s.items.Add("everflame");
            if(node=="N601"&&!s.items.Contains("leaf"))s.items.Add("leaf");
        }
        public static bool UseTool(SaveState s,string item)
        {
            var d=Find(s.investigation.node);
            if(d==null||d.tool!=item||!s.items.Contains(item)||s.investigation.prepared)return false;
            if(d.id=="N401"&&s.investigation.progress!=7)return false;
            s.investigation.prepared=true;
            if(item!="everflame"&&item!="leaf")s.items.Remove(item);
            return true;
        }
        public static int Reflect(int direction,int mirror){return mirror==0?new[]{1,0,3,2}[direction]:new[]{3,2,1,0}[direction];}
        // 0 = up, 1 = right, 2 = down, 3 = left; cells are visited in beam order.
        public static List<Point2> LightPath(IList<int> mirrors,bool prepared)
        {
            var result=new List<Point2>{new Point2(-1,2)};int x=-1,y=2,d=1;
            int[] dx={0,1,0,-1},dy={-1,0,1,0};
            for(int n=0;n<35;n++)
            {
                x+=dx[d];y+=dy[d];result.Add(new Point2(x,y));
                if(x<0||x>6||y<0||y>3)break;
                int m=x==1&&y==2?0:x==1&&y==0?1:x==5&&y==0?2:-1;
                if(m==0&&!prepared)break;if(m>=0)d=Reflect(d,mirrors[m]);
                if(x==5&&y==3)break;
            }
            return result;
        }
        public static bool LightSolved(InvestigationState s){var p=LightPath(s.dials,s.prepared);return s.prepared&&p.Last().x==5&&p.Last().y==3;}
        public static bool PressLamp(InvestigationState s,int lamp)
        {
            if(!s.prepared||s.solved)return false;
            if(s.progress>=4)return true;
            int[] order={2,0,3,1};
            if(lamp==order[s.progress])s.progress++;else s.progress=0;
            return s.progress==4;
        }
        public static void ToggleVent(InvestigationState s,int index)
        {
            if(!s.prepared||s.solved||index<0||index>2)return;
            for(int i=0;i<3;i++)if(i==index||i==1||index==1)s.dials[i]=1-s.dials[i];
        }
        // N401 uses progress as a three-bit mask. Old prepared saves need no migration.
        public static bool VentCleared(InvestigationState s,int index)
        {return index>=0&&index<3&&(s.prepared||(s.progress&(1<<index))!=0);}
        public static bool ClearVent(SaveState s,int index)
        {
            var inv=s.investigation;
            if(inv.node!="N401"||inv.prepared||index<0||index>2||!s.items.Contains("fan")||VentCleared(inv,index))return false;
            inv.progress|=1<<index;
            if(inv.progress==7)UseTool(s,"fan");
            return true;
        }
        static readonly int[] RiverBase={12,10,6,5,9,3,3,10,10};
        public static int RiverMask(int cell,int turn){int m=RiverBase[cell];for(int i=0;i<turn%4;i++)m=((m<<1)&15)|(m>>3);return m;}
        public static List<int> RiverPath(IList<int> turns)
        {
            var path=new List<int>();int cell=0,incoming=3;
            for(int count=0;count<10;count++)
            {
                int mask=RiverMask(cell,turns[cell]);if((mask&(1<<incoming))==0||path.Contains(cell))break;
                path.Add(cell);int outgoing=Enumerable.Range(0,4).First(d=>d!=incoming&&(mask&(1<<d))!=0);
                int x=cell%3+new[]{0,1,0,-1}[outgoing],y=cell/3+new[]{-1,0,1,0}[outgoing];
                if(x==3&&y==2){path.Add(9);break;}if(x<0||x>2||y<0||y>2)break;
                cell=y*3+x;incoming=(outgoing+2)%4;
            }
            return path;
        }
        public static int Torque(IList<int> stones){return -3*stones[0]-stones[1]+stones[2]+3*stones[3];}
        public static readonly int[] BridgeCells={8,9,5,6,2,3};
        public static bool BridgeStep(InvestigationState s,int cell,int scroll)
        {
            if(!s.prepared||s.progress>=6||cell!=BridgeCells[s.progress]||scroll!=s.progress%2)return false;
            s.progress++;return true;
        }
        public static bool IsSolved(InvestigationState s)
        {
            if(!s.prepared)return false;
            switch(s.node)
            {
                case "N101":return LightSolved(s);
                case "N201":return s.dials[0]==2&&s.dials[1]==0&&s.dials[2]==3;
                case "N301":return s.progress==4;
                case "N401":return s.dials[0]==1&&s.dials[1]==0&&s.dials[2]==1;
                case "N403":return RiverPath(s.dials).Contains(9);
                case "N501":return Torque(s.dials)==0;
                case "N601":return s.dials[0]==0&&s.dials[1]==2&&s.dials[2]==1;
                case "N801":return s.progress==6;
                default:return false;
            }
        }
    }
}
