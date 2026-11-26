using UnityEngine;

namespace ConstructionSiteEssentials.NightShift
{
    // IMGUI uses Unity's native pointer events without a project-specific input package.
    public sealed class NightShiftPanel : MonoBehaviour
    {
        public NightShiftJob job;
        static readonly Color Ink = new Color(.045f,.078f,.105f,.98f);
        static readonly Color Paper = new Color(.95f,.95f,.90f);
        static readonly Color Amber = new Color(1,.72f,.22f);
        static readonly Color Muted = new Color(.73f,.80f,.81f);
        GUIStyle type;
        static readonly string[] Instructions = {
            "Pick up the cable reel.", "Place the reel in the marked bay.",
            "Pick up the work light.", "Place the light beside the mixer.",
            "Switch on the work light.", "The mixing station is ready."
        };
        static readonly string[] Tags = {"Cable reel", "Reel bay", "Work light", "Light bay", "Power on", ""};

        void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        void Label(Rect rect, string text, int size, Color color, bool bold = false, bool center = false)
        {
            type.fontSize = size;
            type.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            type.alignment = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            type.normal.textColor = color;
            GUI.Label(rect, text, type);
        }

        void OnGUI()
        {
            if (type == null) type = new GUIStyle(GUI.skin.label) {padding = new RectOffset()};
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 920f, Screen.height / 520f), .50f, 2.5f);
            float w = Screen.width / scale, h = Screen.height / scale;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            Rect footer = new Rect(0, h-76, w, 76);
            int step = (int)job.Current;
            bool done = job.Current == NightShiftJob.Task.Finished;
            Fill(footer, Ink);
            Fill(new Rect(0,footer.y,w,2), new Color(.27f,.32f,.33f));
            for (int i=0;i<3;i++)
                Fill(new Rect(22+i*29,footer.y+15,24,4), (step >= i*2) ? Amber : new Color(.28f,.33f,.35f));
            Label(new Rect(22,footer.y+22,w-190,37), job.Notice ?? Instructions[step], 23, Paper, true);
            Rect header = new Rect(18,16,213,45);
            Fill(header, Ink);
            Fill(new Rect(header.x,header.y,4,header.height), Amber);
            Label(new Rect(33,16,192,45), "Night Shift Setup", 21, Paper, true);
            string action = done ? "Run again" : job.Carrying ? "Put back" : "Restart";
            Rect button = new Rect(w-128,footer.y+16,107,44);
            Fill(button, done ? Amber : new Color(.15f,.21f,.24f));
            Label(button, action, 16, done ? Ink : Paper, true, true);
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            Vector2 screen = new Vector2(mouse.x*scale, Screen.height-mouse.y*scale);
            Rect tag = Rect.zero;
            if (!done)
            {
                Vector3 anchor = job.Carrying ? job.Socket.position : job.Item.transform.position;
                if (!job.Carrying) anchor.y += job.Item == job.reel ? .65f : 1.72f;
                Vector3 p = job.view.WorldToScreenPoint(anchor);
                float x = Mathf.Clamp(p.x/scale,66,w-66);
                float y = Mathf.Clamp((Screen.height-p.y)/scale-38,64,footer.y-41);
                tag = new Rect(x-57,y,114,34);
                Fill(tag, Ink);
                Fill(new Rect(x-1,y+34,2,7), Amber);
                Label(tag,Tags[step],15,Amber,true,true);
            }
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (button.Contains(mouse)) { if (job.Carrying) job.CancelCarry(); else job.Restart(); }
                else if (!footer.Contains(mouse) && !header.Contains(mouse) && !done)
                {
                    if (tag.Contains(mouse) && job.Carrying)
                        screen = job.view.WorldToScreenPoint(job.Socket.position);
                    job.Activate(screen, tag.Contains(mouse));
                }
                e.Use();
            }
            else if ((e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.Repaint) &&
                     job.Carrying && !footer.Contains(mouse))
                job.MovePointer(screen);
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && job.Carrying)
            { job.CancelCarry(); e.Use(); }
            GUI.matrix = previous;
        }
    }
}
