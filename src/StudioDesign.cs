using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace EvoBridge {
    public static class StudioTheme {
        public static readonly Color Background=Color.FromArgb(13,18,20),Panel=Color.FromArgb(25,30,32),Text=Color.FromArgb(242,238,230),Muted=Color.FromArgb(145,153,153),Line=Color.FromArgb(59,67,69),Orange=Color.FromArgb(255,116,74),Lime=Color.FromArgb(187,233,94);
        static readonly Dictionary<string,Font> fonts=new Dictionary<string,Font>();
        public static Font Font(float size,bool bold=false,bool mono=false){string key=size+"/"+bold+"/"+mono;if(!fonts.ContainsKey(key))fonts[key]=new Font(mono?"Consolas":"Bahnschrift",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);return fonts[key];}
        public static void Setup(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;g.PixelOffsetMode=PixelOffsetMode.HighQuality;}
        public static void TextAt(Graphics g,string text,float x,float y,float size,Color color,bool bold=false,bool mono=false,float width=0,float height=0){using(var brush=new SolidBrush(color)){if(width<=0)g.DrawString(text,Font(size,bold,mono),brush,x,y);else using(var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=height>0&&height<=size*1.8f?StringFormatFlags.NoWrap:0})g.DrawString(text,Font(size,bold,mono),brush,new RectangleF(x,y,width,height>0?height:size*2.8f),format);}}
        public static void Rule(Graphics g,float x,float y,float x2,float y2){using(var pen=new Pen(Line,0.8f))g.DrawLine(pen,x,y,x2,y2);}
        public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        public static void Logo(Graphics g){
            using(var p=new Pen(Orange,3.3f)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLines(p,new[]{new PointF(25,39),new PointF(31,29),new PointF(42,29),new PointF(45,24),new PointF(40,24)});g.DrawLines(p,new[]{new PointF(22,45),new PointF(19,45),new PointF(16,50),new PointF(28,50),new PointF(34,40),new PointF(40,40)});}
            TextAt(g,"EVOBRIDGE",20,51,22,Text,true);}
        public static void ControllerIcon(Graphics g,RectangleF r,Color color){var s=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/40,r.Height/27);using(var p=new Pen(color,1.5f)){var path=new GraphicsPath();path.AddBezier(7,3,1,7,0,21,2,24);path.AddBezier(2,24,6,28,12,16,15,17);path.AddLine(15,17,25,17);path.AddBezier(25,17,33,28,37,24,38,22);path.AddBezier(38,22,38,8,33,1,30,3);path.AddBezier(30,3,20,5,16,5,7,3);g.DrawPath(p,path);path.Dispose();g.DrawLine(p,10,8,10,15);g.DrawLine(p,6.5f,11.5f,13.5f,11.5f);g.DrawEllipse(p,28,8,2,2);g.DrawEllipse(p,31,11,2,2);}g.Restore(s);}
    }
    public class StudioCanvas:Control {
        readonly Dictionary<Control,RectangleF> placement=new Dictionary<Control,RectangleF>();
        public Action<Graphics> Render;
        public float UiScale {get;private set;}
        public PointF UiOffset {get;private set;}
        public StudioCanvas(){DoubleBuffered=true;BackColor=StudioTheme.Background;Dock=DockStyle.Fill;SetStyle(ControlStyles.ResizeRedraw,true);}
        public void Place(Control c,RectangleF bounds){if(!placement.ContainsKey(c))Controls.Add(c);placement[c]=bounds;LayoutItems();}
        protected override void OnResize(EventArgs e){base.OnResize(e);LayoutItems();}
        void LayoutItems(){UiScale=Math.Max(.2f,Math.Min(ClientSize.Width/1200f,ClientSize.Height/720f));UiOffset=new PointF((ClientSize.Width-1200*UiScale)/2,(ClientSize.Height-720*UiScale)/2);foreach(var pair in placement){var r=pair.Value;pair.Key.SetBounds((int)(UiOffset.X+r.X*UiScale),(int)(UiOffset.Y+r.Y*UiScale),Math.Max(1,(int)(r.Width*UiScale)),Math.Max(1,(int)(r.Height*UiScale)));var item=pair.Key as IStudioControl;if(item!=null)item.ScaleFactor=UiScale;else pair.Key.Font=new Font("Consolas",Math.Max(10,14*UiScale),FontStyle.Regular,GraphicsUnit.Pixel);pair.Key.Invalidate();}Invalidate();}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);StudioTheme.Setup(e.Graphics);e.Graphics.TranslateTransform(UiOffset.X,UiOffset.Y);e.Graphics.ScaleTransform(UiScale,UiScale);if(Render!=null)Render(e.Graphics);}
        public PointF Logical(Point point){return new PointF((point.X-UiOffset.X)/UiScale,(point.Y-UiOffset.Y)/UiScale);}
        public static void Shell(Graphics g,string section,string title,string status,bool connected){
            using(var b=new LinearGradientBrush(new Rectangle(0,0,1200,720),Color.FromArgb(17,23,25),Color.FromArgb(10,14,16),90))g.FillRectangle(b,0,0,1200,720);
            using(var p=new SolidBrush(Color.FromArgb(12,16,18)))g.FillRectangle(p,0,0,171,720);
            StudioTheme.Rule(g,171,0,171,720);StudioTheme.Logo(g);
            StudioTheme.TextAt(g,section,196,17,13,StudioTheme.Muted,false,true);
            StudioTheme.TextAt(g,title,194,36,39,StudioTheme.Text,true);
            StudioTheme.Rule(g,194,85,1184,85);
            if(!String.IsNullOrEmpty(status)){using(var b=new SolidBrush(connected?StudioTheme.Lime:StudioTheme.Muted))g.FillEllipse(b,1030,25,10,10);StudioTheme.TextAt(g,status,1052,22,13,StudioTheme.Text,false,true,132,38);}
            StudioTheme.TextAt(g,"LOCAL. OPEN. YOURS.",20,651,10,StudioTheme.Text,false,true);
            StudioTheme.TextAt(g,"v0.4",20,674,10,StudioTheme.Muted,false,true);
        }
    }
    public interface IStudioControl {float ScaleFactor{get;set;}}
    public class StudioButton:Control,IStudioControl {
        public float ScaleFactor{get;set;}public bool Primary,Flat;bool selected,hovered;
        public bool Selected{get{return selected;}set{if(selected==value)return;selected=value;Invalidate();}}
        public StudioButton(){DoubleBuffered=true;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.PushButton;SetStyle(ControlStyles.Selectable,true);}
        protected override void OnTextChanged(EventArgs e){AccessibleName=Text;Invalidate();base.OnTextChanged(e);}
        protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnClick(EventArgs e){if(Enabled){Focus();base.OnClick(e);}}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;StudioTheme.Setup(g);float scale=ScaleFactor>0?ScaleFactor:1;g.ScaleTransform(scale,scale);float w=Width/scale,h=Height/scale;
            Color baseColor=Primary?StudioTheme.Orange:StudioTheme.Panel;
            if(!Flat||Selected||hovered){using(var path=StudioTheme.Round(new RectangleF(.5f,.5f,w-1,h-1),Flat?1:3))using(var b=new LinearGradientBrush(new RectangleF(0,0,w,h),baseColor,Primary?Color.FromArgb(255,102,59):Color.FromArgb(30,36,38),90)){g.FillPath(b,path);if(!Primary&&!Flat)using(var pen=new Pen(Enabled?Color.FromArgb(119,127,126):StudioTheme.Line,.8f))g.DrawPath(pen,path);}}
            if(Selected)using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,9,6,4,h-12);
            using(var b=new SolidBrush(!Enabled?Color.FromArgb(86,95,95):Primary?Color.FromArgb(18,22,22):StudioTheme.Text))using(var f=new StringFormat{Alignment=Flat?StringAlignment.Near:StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter})g.DrawString(Text,StudioTheme.Font(Flat?14:14,!Flat||Selected,true),b,new RectangleF(Flat?28:8,0,w-(Flat?40:16),h),f);
            if(Focused&&ShowFocusCues)using(var pen=new Pen(Primary?Color.Black:StudioTheme.Orange,1)){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,4,4,w-9,h-9);}
        }
    }
    public sealed class StudioToggle:Control,IStudioControl {
        bool value;public event EventHandler CheckedChanged;public float ScaleFactor{get;set;}
        public bool Checked{get{return value;}set{if(this.value==value)return;this.value=value;Invalidate();if(CheckedChanged!=null)CheckedChanged(this,EventArgs.Empty);}}
        public StudioToggle(){DoubleBuffered=true;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.CheckButton;}
        protected override void OnClick(EventArgs e){if(Enabled){Focus();Checked=!Checked;}base.OnClick(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
        protected override void OnPaint(PaintEventArgs e){StudioTheme.Setup(e.Graphics);float s=ScaleFactor>0?ScaleFactor:1;e.Graphics.ScaleTransform(s,s);using(var path=StudioTheme.Round(new RectangleF(1,1,38,21),10))using(var b=new SolidBrush(Checked&&Enabled?StudioTheme.Orange:Color.FromArgb(64,73,74)))e.Graphics.FillPath(b,path);using(var b=new SolidBrush(Enabled?StudioTheme.Text:StudioTheme.Muted))e.Graphics.FillEllipse(b,Checked?22:3,3,17,17);if(Focused)using(var p=new Pen(StudioTheme.Text))e.Graphics.DrawRectangle(p,0,0,40,23);}
        protected override AccessibleObject CreateAccessibilityInstance(){return new ToggleAccessibility(this);}
        sealed class ToggleAccessibility:ControlAccessibleObject{readonly StudioToggle owner;public ToggleAccessibility(StudioToggle owner):base(owner){this.owner=owner;}public override AccessibleStates State{get{return base.State|(owner.Checked?AccessibleStates.Checked:0);}}public override void DoDefaultAction(){owner.OnClick(EventArgs.Empty);}}
    }
    public sealed class StudioSlider:Control,IStudioControl {
        int value=8;public float ScaleFactor{get;set;}public event EventHandler ValueChanged;
        public int Value{get{return value;}set{int next=Math.Max(0,Math.Min(30,value));if(next==this.value)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
        public StudioSlider(){DoubleBuffered=true;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;}
        void Position(int x){Focus();Value=(int)Math.Round(Math.Max(0,Math.Min(1,(x-7f)/(Width-14)))*30);}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Capture=true;Position(e.X);}protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture&&e.Button==MouseButtons.Left)Position(e.X);}protected override void OnMouseUp(MouseEventArgs e){Capture=false;base.OnMouseUp(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down){Value--;e.Handled=true;}if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up){Value++;e.Handled=true;}base.OnKeyDown(e);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;StudioTheme.Setup(g);float s=ScaleFactor>0?ScaleFactor:1;float center=Height/2f,left=6*s,right=Width-6*s,x=left+(right-left)*Value/30;using(var p=new Pen(Color.FromArgb(81,90,92),4*s))g.DrawLine(p,left,center,right,center);using(var p=new Pen(StudioTheme.Orange,4*s))g.DrawLine(p,left,center,x,center);using(var b=new SolidBrush(StudioTheme.Text))g.FillEllipse(b,x-6.5f*s,center-6.5f*s,13*s,13*s);if(Focused)using(var p=new Pen(StudioTheme.Orange))g.DrawRectangle(p,1,1,Width-3,Height-3);}
        protected override AccessibleObject CreateAccessibilityInstance(){return new SliderAccessibility(this);}
        sealed class SliderAccessibility:ControlAccessibleObject{readonly StudioSlider owner;public SliderAccessibility(StudioSlider owner):base(owner){this.owner=owner;}public override string Value{get{return owner.Value.ToString();}set{int n;if(Int32.TryParse(value,out n))owner.Value=n;}}}
    }
    public sealed class GamepadArtwork:IDisposable {
        readonly Image image;
        public GamepadArtwork(){image=Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","controller.png"));}
        public void Draw(Graphics g,RectangleF area,Gamepad p,string focus=null,bool muted=false,bool liveSticks=true){
            g.DrawImage(image,area);
            if(muted)return;
            if(liveSticks){DrawStick(g,area,.364f,.555f,p.LX,p.LY,(p.Buttons&Mapper.L3)!=0||focus=="leftstick");DrawStick(g,area,.635f,.555f,p.RX,p.RY,(p.Buttons&Mapper.R3)!=0||focus=="rightstick");}
            else{Mark(g,area,.364f,.555f,.065f,focus=="leftstick");Mark(g,area,.635f,.555f,.065f,focus=="rightstick");}
            bool ps=(p.Buttons&(Mapper.Back|Mapper.Start))==(Mapper.Back|Mapper.Start),touch=(p.Buttons&(Mapper.Back|Mapper.R3))==(Mapper.Back|Mapper.R3)&&!ps;
            Mark(g,area,.779f,.451f,.027f,(p.Buttons&Mapper.A)!=0||focus=="a");Mark(g,area,.854f,.344f,.027f,(p.Buttons&Mapper.B)!=0||focus=="b");Mark(g,area,.720f,.344f,.027f,(p.Buttons&Mapper.X)!=0||focus=="x");Mark(g,area,.779f,.242f,.027f,(p.Buttons&Mapper.Y)!=0||focus=="y");
            Mark(g,area,.224f,.282f,.025f,(p.Buttons&Mapper.Up)!=0||focus=="dpup");Mark(g,area,.224f,.430f,.025f,(p.Buttons&Mapper.Down)!=0||focus=="dpdown");Mark(g,area,.177f,.350f,.025f,(p.Buttons&Mapper.Left)!=0||focus=="dpleft");Mark(g,area,.270f,.350f,.025f,(p.Buttons&Mapper.Right)!=0||focus=="dpright");
            Mark(g,area,.338f,.218f,.014f,((p.Buttons&Mapper.Back)!=0&&!ps&&!touch)||focus=="back");Mark(g,area,.660f,.218f,.014f,((p.Buttons&Mapper.Start)!=0&&!ps)||focus=="start");Mark(g,area,.500f,.557f,.019f,ps||focus=="guide");
            if(touch||focus=="touchpad")using(var pen=new Pen(StudioTheme.Orange,2))g.DrawRectangle(pen,area.X+area.Width*.373f,area.Y+area.Height*.110f,area.Width*.253f,area.Height*.265f);
            Bar(g,area,.16f,.101f,.16f,(p.Buttons&Mapper.L1)!=0||focus=="leftshoulder");Bar(g,area,.68f,.101f,.16f,(p.Buttons&Mapper.R1)!=0||focus=="rightshoulder");
            Trigger(g,area,.16f,.043f,p.LeftTrigger,focus=="lefttrigger");Trigger(g,area,.68f,.043f,p.RightTrigger,focus=="righttrigger");
        }
        public static void Outline(Graphics g,RectangleF area){
            var state=g.Save();g.TranslateTransform(area.X,area.Y);g.ScaleTransform(area.Width/400,area.Height/250);
            using(var pen=new Pen(Color.FromArgb(185,230,232,222),1.35f)){
                using(var body=new GraphicsPath()){body.AddBezier(91,43,67,40,52,61,43,96);body.AddBezier(43,96,34,130,15,193,24,230);body.AddBezier(24,230,30,254,54,256,74,232);body.AddLine(74,232,121,193);body.AddBezier(121,193,172,197,227,197,279,193);body.AddLine(279,193,326,232);body.AddBezier(326,232,346,256,370,254,376,230);body.AddBezier(376,230,385,193,366,130,357,96);body.AddBezier(357,96,348,61,333,40,309,43);body.AddLine(309,43,91,43);g.DrawPath(pen,body);}
                g.DrawBezier(pen,63,48,64,25,115,23,126,43);g.DrawBezier(pen,274,43,285,23,336,25,337,48);
                using(var touch=StudioTheme.Round(new RectangleF(151,52,98,70),5))g.DrawPath(pen,touch);
                g.DrawEllipse(pen,51,60,86,86);g.DrawEllipse(pen,263,60,86,86);
                foreach(var r in new[]{new RectangleF(85,70,18,26),new RectangleF(85,110,18,26),new RectangleF(62,96,27,18),new RectangleF(100,96,27,18)})using(var shape=StudioTheme.Round(r,4))g.DrawPath(pen,shape);
                g.DrawEllipse(pen,281-10,100-10,20,20);g.DrawEllipse(pen,331-10,100-10,20,20);g.DrawEllipse(pen,306-10,76-10,20,20);g.DrawEllipse(pen,306-10,125-10,20,20);
                g.DrawRectangle(pen,276,95,10,10);g.DrawEllipse(pen,326,95,10,10);g.DrawLines(pen,new[]{new PointF(306,70),new PointF(300,81),new PointF(312,81),new PointF(306,70)});g.DrawLine(pen,301,120,311,130);g.DrawLine(pen,301,130,311,120);
                g.DrawEllipse(pen,127,128,56,56);g.DrawEllipse(pen,134,135,42,42);g.DrawEllipse(pen,217,128,56,56);g.DrawEllipse(pen,224,135,42,42);g.DrawEllipse(pen,190,145,20,20);
                using(var dot=new SolidBrush(Color.FromArgb(155,220,224,215))){g.FillEllipse(dot,198,153,4,4);for(int y=0;y<3;y++)for(int x=0;x<5;x++)g.FillEllipse(dot,187+x*6,130+y*4,1.2f,1.2f);for(int y=0;y<5;y++)for(int x=0;x<8;x++)g.FillEllipse(dot,162+x*10,62+y*11,.7f,.7f);}
                using(var b=new SolidBrush(Color.FromArgb(150,200,205,195))){g.FillRectangle(b,136,66,6,15);g.FillRectangle(b,257,66,6,15);}
            }g.Restore(state);
        }
        void DrawStick(Graphics g,RectangleF r,float nx,float ny,short vx,short vy,bool pressed){
            float cx=r.X+r.Width*nx,cy=r.Y+r.Height*ny,radius=r.Width*.055f,dx=vx/32768f*r.Width*.017f,dy=-vy/32768f*r.Width*.017f;
            using(var b=new SolidBrush(Color.FromArgb(17,20,21)))g.FillEllipse(b,cx-radius-2,cy-radius-2,radius*2+4,radius*2+4);
            var source=new RectangleF(image.Width*(nx-.056f),image.Height*(ny-.056f*image.Width/image.Height),image.Width*.112f,image.Width*.112f);
            var destination=new RectangleF(cx-radius+dx,cy-radius+dy,radius*2,radius*2);var state=g.Save();using(var clip=new GraphicsPath()){clip.AddEllipse(destination);g.SetClip(clip,CombineMode.Intersect);g.DrawImage(image,destination,source,GraphicsUnit.Pixel);}g.Restore(state);
            using(var pen=new Pen(Color.FromArgb(110,154,163,161),.8f)){g.DrawLine(pen,cx-radius*.65f,cy,cx+radius*.65f,cy);g.DrawLine(pen,cx,cy-radius*.65f,cx,cy+radius*.65f);}
            using(var b=new SolidBrush(StudioTheme.Orange))g.FillEllipse(b,cx+dx-2.5f,cy+dy-2.5f,5,5);if(pressed)using(var pen=new Pen(StudioTheme.Orange,2))g.DrawEllipse(pen,destination);
        }
        static void Mark(Graphics g,RectangleF r,float x,float y,float radius,bool on){if(!on)return;float size=r.Width*radius;var rect=new RectangleF(r.X+r.Width*x-size,r.Y+r.Height*y-size,size*2,size*2);using(var b=new SolidBrush(Color.FromArgb(110,255,116,74)))g.FillEllipse(b,rect);using(var p=new Pen(StudioTheme.Orange,1.7f))g.DrawEllipse(p,rect);}
        static void Bar(Graphics g,RectangleF r,float x,float y,float w,bool on){if(!on)return;using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,r.X+r.Width*x,r.Y+r.Height*y,r.Width*w,3);}
        static void Trigger(Graphics g,RectangleF r,float x,float y,byte value,bool focus){if(value==0&&!focus)return;using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,r.X+r.Width*x,r.Y+r.Height*y,r.Width*.16f*(focus?1:value/255f),4);}
        public void Dispose(){image.Dispose();}
    }
    public class StudioForm:Form {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        protected readonly StudioCanvas Canvas=new StudioCanvas();
        protected StudioForm(){BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;AutoScaleMode=AutoScaleMode.None;MinimumSize=new Size(860,540);StartPosition=FormStartPosition.CenterScreen;Controls.Add(Canvas);var work=Screen.PrimaryScreen.WorkingArea;float s=Math.Min(1f,Math.Min((work.Width-40)/1200f,(work.Height-64)/720f));ClientSize=new Size((int)(1200*s),(int)(720*s));HandleCreated+=(sdr,e)=>{int value=1;try{DwmSetWindowAttribute(Handle,20,ref value,4);}catch{}};}
        protected StudioButton Button(string text,RectangleF rect,Action action,bool primary=false,bool flat=false){var b=new StudioButton{Text=text,AccessibleName=text,Primary=primary,Flat=flat};b.Click+=(s,e)=>action();Canvas.Place(b,rect);return b;}
        protected StudioToggle Toggle(string name,RectangleF rect,Action action){var c=new StudioToggle{AccessibleName=name};c.CheckedChanged+=(s,e)=>action();Canvas.Place(c,rect);return c;}
        protected StudioSlider Slider(string name,RectangleF rect,Action action){var c=new StudioSlider{AccessibleName=name};c.ValueChanged+=(s,e)=>action();Canvas.Place(c,rect);return c;}
    }
}
