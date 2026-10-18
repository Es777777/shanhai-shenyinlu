using System;
using System.Collections.Generic;
using System.Linq;

namespace ShanHai
{
    public struct Point2
    {
        public float x,y;
        public Point2(float x,float y) {this.x=x;this.y=y;}
        public static float Distance(Point2 a,Point2 b) {return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));}
    }
    public static class InteractionRules
    {
        public static bool IsSolved(IList<int> puzzle) {return puzzle.Count==6 && Enumerable.Range(0,6).All(i=>puzzle[i]==i);}
        public static void Swap(IList<int> puzzle,int a,int b)
        {
            if(a<0||a>=puzzle.Count||b<0||b>=puzzle.Count)throw new ArgumentOutOfRangeException();
            int t=puzzle[a];puzzle[a]=puzzle[b];puzzle[b]=t;
        }
        static float SegmentDistance(Point2 p,Point2 a,Point2 b)
        {
            float dx=b.x-a.x,dy=b.y-a.y,denom=dx*dx+dy*dy;
            float t=denom==0?0:Math.Max(0,Math.Min(1,((p.x-a.x)*dx+(p.y-a.y)*dy)/denom));
            return Point2.Distance(p,new Point2(a.x+t*dx,a.y+t*dy));
        }
        static float PathDistance(Point2 p,IList<Point2> path)
        {
            float min=float.MaxValue;
            for(int i=1;i<path.Count;i++)min=Math.Min(min,SegmentDistance(p,path[i-1],path[i]));
            return min;
        }
        static float Length(IList<Point2> path)
        {
            float length=0;for(int i=1;i<path.Count;i++)length+=Point2.Distance(path[i-1],path[i]);return length;
        }
        public static bool JudgeStroke(IList<Point2> drawn,IList<Point2> guide,float tolerance)
        {
            if(drawn.Count<2||guide.Count<2)return false;
            if(Point2.Distance(drawn[0],guide[0])>tolerance||Point2.Distance(drawn[drawn.Count-1],guide[guide.Count-1])>tolerance)return false;
            float length=Length(guide),actual=Length(drawn);
            if(actual<length*.7f||actual>length*1.65f)return false;
            // Evaluate segments in both directions so a fast diagonal cannot shortcut bends.
            foreach(var pair in new[]{new[]{drawn,guide},new[]{guide,drawn}})
            {
                var from=pair[0];var to=pair[1];
                for(int i=1;i<from.Count;i++)
                {
                    int count=Math.Max(1,(int)(Point2.Distance(from[i-1],from[i])/(tolerance*.4f)));
                    for(int j=0;j<=count;j++)
                    {
                        float t=j/(float)count;
                        var point=new Point2(from[i-1].x+(from[i].x-from[i-1].x)*t,from[i-1].y+(from[i].y-from[i-1].y)*t);
                        if(PathDistance(point,to)>tolerance)return false;
                    }
                }
            }
            return true;
        }
    }
}
