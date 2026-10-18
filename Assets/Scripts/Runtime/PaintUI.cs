using System.Collections.Generic;
using UnityEngine;

namespace ShanHai
{
    public static class PaintUI
    {
        public static Font Font;
        public static readonly Color Ink = new Color(.035f,.075f,.085f,.94f);
        public static readonly Color Gold = new Color(.76f,.64f,.4f,1);
        public static readonly Color Paper = new Color(.94f,.9f,.79f,1);
        public static readonly Color Muted = new Color(.58f,.66f,.64f,1);
        public static readonly Color Jade = new Color(.37f,.69f,.65f,1);
        public static readonly Color Red = new Color(.76f,.31f,.24f,1);
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        public static GUIStyle Style(int size, TextAnchor align = TextAnchor.UpperLeft, Color? color = null)
        {
            Color c = color ?? Paper;
            string key = size + ":" + align + ":" + c;
            GUIStyle s;
            if (!styles.TryGetValue(key, out s))
            {
                s = new GUIStyle(GUI.skin.label) { font = Font, fontSize = size, alignment = align, wordWrap = true, richText = false, padding = new RectOffset(0,0,0,0) };
                s.normal.textColor = c; styles[key] = s;
            }
            return s;
        }
        public static void Fill(Rect r, Color c)
        {
            Color previous = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = previous;
        }
        public static void Line(Vector2 a, Vector2 b, Color c, float width = 1)
        {
            if (Event.current.type != EventType.Repaint || (a-b).sqrMagnitude < .001f) return;
            Matrix4x4 previous = GUI.matrix;
            // Compose in logical canvas space. RotateAroundPivot pre-multiplies
            // screen space and displaces each segment when the canvas is scaled.
            try
            {
                GUI.matrix = previous * Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);
                // Overlap adjacent segments so pixel snapping does not leave
                // seams in a handwritten path or a small circular mechanism.
                float cap=Mathf.Min(width*.5f,1.5f);
                Fill(new Rect(-cap,-width/2,Vector2.Distance(a,b)+cap*2,width), c);
            }
            finally { GUI.matrix = previous; }
        }
        public static void Circle(Vector2 center,float radius,Color color,float width=2)
        {
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;
                Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,color,width);
            }
        }
        public static void Border(Rect r, Color c, float width = 1)
        {
            Fill(new Rect(r.x,r.y,r.width,width),c); Fill(new Rect(r.x,r.yMax-width,r.width,width),c);
            Fill(new Rect(r.x,r.y,width,r.height),c); Fill(new Rect(r.xMax-width,r.y,width,r.height),c);
        }
        public static void Panel(Rect r, bool ornament = true)
        {
            Fill(r, Ink); Border(r, new Color(Gold.r,Gold.g,Gold.b,.45f));
            if (!ornament) return;
            float k = 16;
            foreach (var p in new[] {new Vector2(r.x+6,r.y+6),new Vector2(r.xMax-6,r.y+6),new Vector2(r.x+6,r.yMax-6),new Vector2(r.xMax-6,r.yMax-6)})
            {
                float dx = p.x < r.center.x ? 1 : -1, dy = p.y < r.center.y ? 1 : -1;
                Line(p, p+new Vector2(dx*k,0),Gold,2); Line(p,p+new Vector2(0,dy*k),Gold,2);
            }
        }
        public static void Label(Rect r, string t, int size = 24, Color? color = null, TextAnchor align = TextAnchor.UpperLeft) { GUI.Label(r,t,Style(size,align,color)); }
        public static bool Button(Rect r, string text, bool accent = false, bool enabled = true, int size = 23)
        {
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            Fill(r, enabled ? hover ? new Color(.19f,.31f,.3f,.98f) : accent ? new Color(.37f,.19f,.14f,.96f) : new Color(.045f,.1f,.11f,.9f) : new Color(.08f,.1f,.11f,.8f));
            Border(r, enabled ? hover || accent ? Gold : new Color(.4f,.49f,.45f,.65f) : new Color(.2f,.25f,.24f,1));
            Label(r,text,size,enabled ? Paper : Muted,TextAnchor.MiddleCenter);
            bool old = GUI.enabled; GUI.enabled = old && enabled;
            bool clicked = GUI.Button(r,GUIContent.none,GUIStyle.none); GUI.enabled = old; return clicked;
        }
        public static void Diamond(Vector2 p, float radius, Color c)
        {
            Line(p+Vector2.up*radius,p+Vector2.right*radius,c,2); Line(p+Vector2.right*radius,p+Vector2.down*radius,c,2);
            Line(p+Vector2.down*radius,p+Vector2.left*radius,c,2); Line(p+Vector2.left*radius,p+Vector2.up*radius,c,2);
        }
        public static void Image(Rect rect, Texture texture, Color? tint = null)
        {
            if (texture == null) return;
            Color old = GUI.color; GUI.color = tint ?? Color.white;
            GUI.DrawTexture(rect,texture,ScaleMode.ScaleAndCrop,true); GUI.color = old;
        }
    }
}
