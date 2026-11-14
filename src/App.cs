using System;
using System.Drawing;
using System.Windows.Forms;
namespace EvoBridge {
    public sealed class StickView : Control {
        public Gamepad Pad;
        public StickView(){DoubleBuffered=true;}
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using(var ring=new Pen(Color.FromArgb(78,95,121),2))using(var dot=new SolidBrush(Color.FromArgb(94,225,185)))
            using(var text=new SolidBrush(Color.FromArgb(166,178,197))) {
                for(int i=0;i<2;i++) {
                    int cx=i==0?120:330,cy=77;
                    e.Graphics.DrawEllipse(ring,cx-49,cy-49,98,98);e.Graphics.DrawLine(ring,cx-55,cy,cx+55,cy);e.Graphics.DrawLine(ring,cx,cy-55,cx,cy+55);
                    short x=i==0?Pad.LX:Pad.RX,y=i==0?Pad.LY:Pad.RY;
                    e.Graphics.FillEllipse(dot,cx+(float)x/32768*43-6,cy-(float)y/32768*43-6,12,12);
                    e.Graphics.DrawString(i==0?"LEFT STICK":"RIGHT STICK",Font,text,cx-48,137);
                }
                e.Graphics.DrawString("L2   "+Pad.LeftTrigger+"\n\nR2   "+Pad.RightTrigger,Font,text,470,43);
            }
        }
    }
}