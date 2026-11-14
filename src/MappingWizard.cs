using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace EvoBridge {
    public sealed class MappingWizard:StudioForm {
        readonly ControllerDevice device;readonly IntPtr joystick;readonly SdlInput input;
        readonly string[] targets={"a","b","x","y","back","start","leftstick","rightstick","leftshoulder","rightshoulder","dpup","dpdown","dpleft","dpright","leftx","lefty","rightx","righty","lefttrigger","righttrigger"};
        readonly string[] prompts={"Press the bottom face button","Press the right face button","Press the left face button","Press the top face button","Press Back / Select","Press Start / Options","Click the left stick","Click the right stick","Press LB / L1","Press RB / R1","Press D-pad Up","Press D-pad Down","Press D-pad Left","Press D-pad Right","Move LEFT stick fully RIGHT","Move LEFT stick fully DOWN","Move RIGHT stick fully RIGHT","Move RIGHT stick fully DOWN","Fully squeeze LEFT trigger","Fully squeeze RIGHT trigger"};
        readonly Label title=new Label(),message=new Label();readonly StudioButton next,skip,cancel;
        readonly GamepadArtwork art=new GamepadArtwork();readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly List<string> bindings=new List<string>(),extras=new List<string>();
        bool[] initialButtons;short[] initialAxes;byte[] initialHats;bool armed;int step;
        public string Mapping{get;private set;}
        [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr SDL_GetGamepadMapping(IntPtr gamepad);
        public MappingWizard(ControllerDevice d,SdlInput runtime,bool reviewExisting=false){
            input=runtime;device=d;joystick=Sdl.SDL_OpenJoystick(d.Id);if(joystick==IntPtr.Zero){art.Dispose();throw new InvalidOperationException(Sdl.Text(Sdl.SDL_GetError()));}
            Text="EvoBridge · Map controls";StartPosition=FormStartPosition.CenterParent;
            Button("CONTROLLER",new RectangleF(0,112,171,46),CancelMapping,false,true);
            Button("PROFILES",new RectangleF(0,158,171,46),CancelMapping,false,true);
            var nav=Button("MAP CONTROLS",new RectangleF(0,204,171,46),()=>{},false,true);nav.Selected=true;
            next=Button("BEGIN STEP   →",new RectangleF(194,642,216,45),Arm,true);
            skip=Button("SKIP CONTROL",new RectangleF(425,642,168,45),()=>{armed=false;Advance();});
            cancel=Button("CANCEL",new RectangleF(609,642,154,45),CancelMapping);
            Canvas.Render=DrawWizard;timer.Interval=33;timer.Tick+=(s,e)=>{Poll();Canvas.Invalidate();};timer.Start();
            FormClosed+=(s,e)=>{timer.Stop();Sdl.SDL_CloseJoystick(joystick);art.Dispose();};ShowStep();
            try{if(d.Mapped){IntPtr gamepad=Sdl.SDL_OpenGamepad(d.Id);if(gamepad==IntPtr.Zero)throw new InvalidOperationException("The controller has no current mapping.");IntPtr text=IntPtr.Zero;try{text=SDL_GetGamepadMapping(gamepad);string map=Sdl.Text(text);foreach(string part in map.Split(',')){int split=part.IndexOf(':');if(split<=0)continue;string target=part.Substring(0,split);if(target=="guide"||target=="touchpad")extras.Add(part);if(reviewExisting&&targets.Contains(target))bindings.Add(part);}if(reviewExisting){if(bindings.Count==0)throw new InvalidOperationException("No reviewable mapping bindings were found.");step=targets.Length;Finish();}}finally{if(text!=IntPtr.Zero)Sdl.SDL_free(text);Sdl.SDL_CloseGamepad(gamepad);}}}catch{timer.Stop();Sdl.SDL_CloseJoystick(joystick);art.Dispose();throw;}
        }
        void CancelMapping(){DialogResult=DialogResult.Cancel;Close();}
        protected override bool ProcessCmdKey(ref Message msg,Keys keys){if(keys==Keys.Escape){CancelMapping();return true;}return base.ProcessCmdKey(ref msg,keys);}
        void DrawWizard(Graphics g){
            bool review=step>=targets.Length;
            StudioCanvas.Shell(g,review?"MAPPING / REVIEW":"MAPPING / "+(step+1).ToString("00")+" OF 20",review?"Mapping ready.":"Make it yours.",null,false);
            if(review){DrawReview(g);return;}
            StudioTheme.ControllerIcon(g,new RectangleF(1020,22,29,21),StudioTheme.Text);StudioTheme.TextAt(g,device.Name,1060,23,12,StudioTheme.Text,false,true,124,32);
            using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,196,103,138,22);
            StudioTheme.TextAt(g,"MAPPING WIZARD",204,107,12,Color.FromArgb(10,15,16),true,true);
            StudioTheme.TextAt(g,prompts[step],199,140,30,StudioTheme.Text,true,false,709,75);
            string help=armed?"Listening. Move only the requested control.":bindings.Count==0?"Release other controls, then begin.":"Release and centre controls, then begin the next step.";
            StudioTheme.TextAt(g,help,201,185,15,StudioTheme.Text,false,true,700,38);
            string focus=targets[step];if(focus=="leftx"||focus=="lefty")focus="leftstick";if(focus=="rightx"||focus=="righty")focus="rightstick";
            art.Draw(g,new RectangleF(234,224,590,354),new Gamepad(),focus,false,false);
            if(step<4){float[] px={.779f,.854f,.720f,.779f},py={.451f,.344f,.344f,.242f};float x=234+590*px[step],y=224+354*py[step],labelY=y+10;using(var pen=new Pen(StudioTheme.Text,.9f)){g.DrawLines(pen,new[]{new PointF(x+17,y),new PointF(766,labelY+12),new PointF(807,labelY+12)});g.DrawRectangle(pen,807,labelY,105,24);}StudioTheme.TextAt(g,new[]{"A / CROSS","B / CIRCLE","X / SQUARE","Y / TRIANGLE"}[step],814,labelY+5,11,StudioTheme.Text,true,true);}
            StudioTheme.Rule(g,923,85,923,617);StudioTheme.TextAt(g,"MAPPING SEQUENCE",944,108,15,StudioTheme.Text,true,true);
            string[] groups={"01–04  FACE BUTTONS","05–08  SYSTEM + STICKS","09–14  SHOULDERS + D-PAD","15–20  ANALOG INPUTS"};int active=step<4?0:step<8?1:step<14?2:3;
            for(int i=0;i<4;i++){float y=156+i*48;if(i==active){using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,940,y,244,38);}else StudioTheme.Rule(g,944,y+43,1184,y+43);StudioTheme.TextAt(g,groups[i],952,y+10,12.5f,i==active?Color.FromArgb(10,15,16):StudioTheme.Text,i==active,true);}
            StudioTheme.Rule(g,944,365,1184,365);StudioTheme.TextAt(g,"CURRENT BINDING",944,387,15,StudioTheme.Text,true,true);using(var b=new SolidBrush(StudioTheme.Panel))g.FillRectangle(b,944,422,240,38);
            StudioTheme.TextAt(g,armed?"Listening…":bindings.Count==0?"Waiting to begin":bindings[bindings.Count-1],955,434,13,StudioTheme.Text,false,true,216,23);
            StudioTheme.Rule(g,194,617,1184,617);StudioTheme.Rule(g,784,642,784,687);StudioTheme.TextAt(g,"Release and centre controls before each step.",805,657,11,StudioTheme.Text,false,true,375,30);
        }
        string Binding(string target){string key=target+":";string value=bindings.LastOrDefault(b=>b.StartsWith(key,StringComparison.Ordinal));return value==null?"—":value.Substring(key.Length);}
        void DrawReview(Graphics g){
            using(var b=new SolidBrush(bindings.Count==20?StudioTheme.Lime:StudioTheme.Orange))g.FillEllipse(b,1017,29,23,23);
            StudioTheme.TextAt(g,"✓",1022,32,14,Color.FromArgb(10,15,16),true);StudioTheme.TextAt(g,bindings.Count+" CONTROLS",1055,31,14,StudioTheme.Text,false,true);
            StudioTheme.Rule(g,670,101,670,532);
            ReviewGroup(g,"FACE BUTTONS",200,118,440,new[]{"A","B","X","Y"},new[]{Binding("a"),Binding("b"),Binding("x"),Binding("y")});
            ReviewGroup(g,"SYSTEM + STICKS",700,118,472,new[]{"BACK","START","L3","R3"},new[]{Binding("back"),Binding("start"),Binding("leftstick"),Binding("rightstick")});
            int dpad=new[]{"dpup","dpdown","dpleft","dpright"}.Count(t=>Binding(t)!="—");
            ReviewGroup(g,"SHOULDERS + D-PAD",200,339,440,new[]{"L1","R1","D-PAD"},new[]{Binding("leftshoulder"),Binding("rightshoulder"),dpad==4?"mapped":dpad+" / 4"});
            ReviewGroup(g,"ANALOG INPUTS",700,339,472,new[]{"LEFT STICK","RIGHT STICK","L2","R2"},new[]{Binding("leftx")+"/"+Binding("lefty"),Binding("rightx")+"/"+Binding("righty"),Binding("lefttrigger"),Binding("righttrigger")});
            StudioTheme.Rule(g,194,545,1184,545);StudioTheme.TextAt(g,"Test your new mapping in live preview before starting a session.",198,567,15,StudioTheme.Text,false,true);StudioTheme.Rule(g,194,608,1184,608);
        }
        static void ReviewGroup(Graphics g,string heading,float x,float y,float width,string[] labels,string[] values){using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,x,y,heading.Length*9+22,26);StudioTheme.TextAt(g,heading,x+9,y+5,14,Color.FromArgb(10,15,16),true,true);for(int i=0;i<labels.Length;i++){float yy=y+42+i*38;StudioTheme.TextAt(g,labels[i],x+8,yy,17,StudioTheme.Text,true,true);float chipWidth=Math.Max(52,Math.Min(150,g.MeasureString(values[i],StudioTheme.Font(15,false,true)).Width+20));var rect=new RectangleF(x+width-(x<600?8:20)-chipWidth,yy-2,chipWidth,25);using(var shape=StudioTheme.Round(rect,4))using(var pen=new Pen(Color.FromArgb(180,217,224,220),.9f))g.DrawPath(pen,shape);using(var brush=new SolidBrush(StudioTheme.Text))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(values[i],StudioTheme.Font(15,false,true),brush,rect,format);StudioTheme.Rule(g,x+5,yy+31,x+width,yy+31);}}
        void ShowStep(){title.Text="Step "+(step+1)+" of "+targets.Length+" · "+prompts[step];message.Text="Release all buttons and centre both sticks, then click Begin step.\n\nOnly move the control named above. You can skip a control your controller does not have.";next.Text="BEGIN STEP   →";next.Enabled=true;}
        void Arm(){
            if(step>=targets.Length){DialogResult=DialogResult.OK;Close();return;}
            input.Pump();
            initialButtons=new bool[Sdl.SDL_GetNumJoystickButtons(joystick)];for(int b=0;b<initialButtons.Length;b++)initialButtons[b]=Sdl.SDL_GetJoystickButton(joystick,b);
            initialAxes=new short[Sdl.SDL_GetNumJoystickAxes(joystick)];for(int a=0;a<initialAxes.Length;a++)initialAxes[a]=Sdl.SDL_GetJoystickAxis(joystick,a);
            initialHats=new byte[Sdl.SDL_GetNumJoystickHats(joystick)];for(int h=0;h<initialHats.Length;h++)initialHats[h]=Sdl.SDL_GetJoystickHat(joystick,h);
            armed=true;next.Enabled=false;message.Text=prompts[step]+" now.\n\nWaiting for controller input…";
        }
        void Poll(){
            input.Pump();if(!armed)return;string binding=null;
            if(step<14||step>=18){for(int b=0;b<initialButtons.Length;b++)if(!initialButtons[b]&&Sdl.SDL_GetJoystickButton(joystick,b)){binding="b"+b;break;}}
            if(binding==null&&step>=10&&step<14){for(int h=0;h<initialHats.Length;h++){byte v=Sdl.SDL_GetJoystickHat(joystick,h);if(v!=initialHats[h]&&(v==1||v==2||v==4||v==8)){binding="h"+h+"."+v;break;}}}
            if(binding==null&&step>=14){for(int a=0;a<initialAxes.Length;a++){
                int value=Sdl.SDL_GetJoystickAxis(joystick,a),delta=value-initialAxes[a];if(Math.Abs(delta)<22000)continue;
                if(step<18)binding="a"+a+(delta<0?"~":"");
                else if(initialAxes[a]<-16000)binding="a"+a;
                else if(initialAxes[a]>16000)binding="a"+a+"~";
                else binding=(delta<0?"-":"+")+"a"+a;
                break;
            }}
            if(binding==null)return;
            bindings.Add(targets[step]+":"+binding);armed=false;step++;
            if(step>=targets.Length){Finish();return;}
            title.Text="Captured "+binding+" · next: "+prompts[step];message.Text="Release the last control and centre the sticks. Click Begin step when ready.";next.Enabled=true;
        }
        void Advance(){step++;if(step>=targets.Length)Finish();else ShowStep();}
        void Finish(){
            if(bindings.Count==0){MessageBox.Show(this,"No controls mapped. Nothing has been saved.");DialogResult=DialogResult.Cancel;Close();return;}
            Mapping=device.Guid+","+device.Name.Replace(',',' ').Replace(':',' ').Replace('\r',' ').Replace('\n',' ')+","+String.Join(",",bindings.Concat(extras))+",platform:Windows,";
            title.Text="Mapping ready";message.Text="Click Save mapping to keep it. Test buttons, sticks and triggers in preview before starting output.";next.Text="SAVE MAPPING   →";next.Enabled=true;skip.Visible=false;Canvas.Place(next,new RectangleF(194,633,323,52));Canvas.Place(cancel,new RectangleF(541,633,184,52));Canvas.Invalidate();
        }
    }
}
