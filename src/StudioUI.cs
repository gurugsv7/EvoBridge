using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EvoBridge {
    public sealed class MainForm:StudioForm {
        readonly SdlInput input=new SdlInput();readonly Bridge bridge=new Bridge();readonly GamepadArtwork art=new GamepadArtwork();
        readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
        readonly StudioButton controllerNav,profilesNav,mappingNav,change,start,stop,ps,touch,save,rumbleTest,back,editSave,previousProfiles,nextProfiles;
        readonly StudioToggle rumble,hide,editRumble,editHide;readonly StudioSlider deadzone,editDeadzone;
        readonly TextBox profileName=new TextBox();readonly List<ProfileRow> rows=new List<ProfileRow>();
        List<ControllerDevice> devices=new List<ControllerDevice>();List<ControllerProfile> saved=new List<ControllerProfile>();
        ControllerProfile current,editing;Gamepad pad;bool connected,closing,stopping,mapping;int page,profileOffset;
        DateTime nextScan=DateTime.MinValue,noteUntil;string note;
        public MainForm(){
            Text="EvoBridge · Controller desk";
            controllerNav=Button("CONTROLLER",new RectangleF(0,112,171,46),()=>SetPage(0),false,true);
            profilesNav=Button("PROFILES",new RectangleF(0,158,171,46),()=>SetPage(1),false,true);
            mappingNav=Button("MAP CONTROLS",new RectangleF(0,204,171,46),MapDevice,false,true);
            var mapMenu=new ContextMenuStrip{BackColor=StudioTheme.Panel,ForeColor=StudioTheme.Text};mapMenu.Items.Add("Create a new mapping",null,(s,e)=>MapDevice());mapMenu.Items.Add("Review current mapping",null,(s,e)=>MapDevice(true));mappingNav.ContextMenuStrip=mapMenu;
            change=Button("Change device  ⌄",new RectangleF(415,91,240,39),ChooseDevice,false,true);
            start=Button("START SESSION   →",new RectangleF(194,642,205,45),StartOutput,true);
            stop=Button("STOP",new RectangleF(415,642,113,45),StopOutput);
            ps=Button("PS",new RectangleF(565,642,99,45),()=>bridge.PressPS());
            touch=Button("TOUCHPAD",new RectangleF(681,642,140,45),()=>bridge.PressTouch());
            rumble=Toggle("Forward rumble",new RectangleF(1141,291,42,24),()=>{bridge.RumbleEnabled=rumble.Checked;if(!rumble.Checked)input.Rumble(0,0);});
            hide=Toggle("Prevent double input",new RectangleF(1141,353,42,24),()=>Canvas.Invalidate());
            deadzone=Slider("Stick deadzone",new RectangleF(943,228,201,28),()=>Canvas.Invalidate());
            rumbleTest=Button("TEST RUMBLE",new RectangleF(944,453,240,39),()=>{if(!input.TestRumble())Notice("This controller/driver did not accept rumble.","Rumble unavailable");else SetNote("Short rumble test sent.");});
            save=Button("SAVE PROFILE",new RectangleF(944,506,240,39),()=>SaveCurrent(),true);
            back=Button("←  BACK TO CONTROLLER",new RectangleF(196,650,240,39),()=>SetPage(0));
            profileName.BackColor=StudioTheme.Panel;profileName.ForeColor=StudioTheme.Text;profileName.BorderStyle=BorderStyle.FixedSingle;profileName.MaxLength=64;profileName.AccessibleName="Profile name";Canvas.Place(profileName,new RectangleF(883,369,300,30));
            editDeadzone=Slider("Saved profile stick deadzone",new RectangleF(881,416,230,28),()=>Canvas.Invalidate());
            editRumble=Toggle("Saved profile rumble",new RectangleF(1141,464,42,24),()=>Canvas.Invalidate());
            editHide=Toggle("Saved profile prevent double input",new RectangleF(1141,507,42,24),()=>Canvas.Invalidate());
            editSave=Button("SAVE CHANGES",new RectangleF(702,559,482,43),SaveEditing,true);
            previousProfiles=Button("↑",new RectangleF(1119,91,27,25),()=>ScrollProfiles(-1));nextProfiles=Button("↓",new RectangleF(1156,91,27,25),()=>ScrollProfiles(1));
            for(int i=0;i<3;i++){var row=new ProfileRow();row.Click+=(s,e)=>EditProfile(((ProfileRow)s).Profile);Canvas.Place(row,new RectangleF(194,130+i*54,990,54));rows.Add(row);}
            Canvas.MouseWheel+=(s,e)=>{if(page==1)ScrollProfiles(e.Delta>0?-1:1);};
            Canvas.Render=PaintScreen;timer.Interval=33;timer.Tick+=(s,e)=>TickState();timer.Start();
            FormClosing+=(s,e)=>{if(closing)return;closing=true;timer.Stop();bridge.Dispose();input.Dispose();art.Dispose();};
            ReloadSaved();Scan();SetPage(0);TickState();
        }
        void Notice(string message,string title){MessageBox.Show(this,message,title,MessageBoxButtons.OK,MessageBoxIcon.Information);}
        void SetNote(string text){note=text;noteUntil=DateTime.UtcNow.AddSeconds(3);Canvas.Invalidate();}
        void SetPage(int value){page=value;if(page==1)ReloadSaved();controllerNav.Selected=page==0;profilesNav.Selected=page==1;UpdateVisibility();Canvas.Invalidate();}
        void UpdateVisibility(){
            foreach(Control c in new Control[]{change,start,stop,ps,touch,rumble,hide,deadzone,rumbleTest,save})c.Visible=page==0;
            foreach(Control c in new Control[]{back,profileName,editDeadzone,editRumble,editHide,editSave})c.Visible=page==1;
            previousProfiles.Visible=nextProfiles.Visible=page==1&&saved.Count>3;foreach(var row in rows)row.Visible=page==1&&row.Profile!=null;
        }
        void Scan(){
            if(bridge.Busy||mapping)return;
            try{
                var found=input.Detect();uint old=input.Selected==null?0:input.Selected.Id;
                if(devices.Count==found.Count&&devices.Select(d=>d.Id).SequenceEqual(found.Select(d=>d.Id)))return;
                devices=found;var selection=found.FirstOrDefault(d=>d.Id==old);if(selection==null){string last=Profiles.Load().LastSelected;selection=found.FirstOrDefault(d=>d.ProfileKey==last)??found.FirstOrDefault();}
                SelectDevice(selection);
            }catch(Exception ex){SetNote(ex.Message);}finally{nextScan=DateTime.UtcNow.AddSeconds(2);}
        }
        void SelectDevice(ControllerDevice d){try{input.Select(d);current=d==null?null:Profiles.For(d);if(current!=null){deadzone.Value=current.Deadzone;rumble.Checked=current.Rumble;hide.Checked=current.HidePhysical;editing=current;}Canvas.Invalidate();}catch(Exception ex){Notice(ex.Message,"Controller selection");}}
        void ChooseDevice(){if(bridge.Busy)return;Scan();var menu=new ContextMenuStrip{BackColor=StudioTheme.Panel,ForeColor=StudioTheme.Text,ShowImageMargin=false};foreach(var d in devices){var selected=d;var item=new ToolStripMenuItem(d.ToString()){Checked=input.Selected!=null&&input.Selected.Id==d.Id};item.Click+=(s,e)=>SelectDevice(selected);menu.Items.Add(item);}menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Refresh controllers",null,(s,e)=>{nextScan=DateTime.MinValue;Scan();});menu.Closed+=(s,e)=>menu.Dispose();menu.Show(change,new Point(0,change.Height));}
        bool SaveCurrent(){if(current==null)return false;try{current.Deadzone=deadzone.Value;current.Rumble=rumble.Checked;current.HidePhysical=hide.Checked;Profiles.Save(current);SetNote("Profile saved on this PC.");return true;}catch(Exception ex){Notice(ex.Message,"Cannot save profile");return false;}}
        void StartOutput(){try{if(!SaveCurrent())return;bridge.HidePhysical=hide.Checked;bridge.RumbleEnabled=rumble.Checked;bridge.Start(input,input.Selected,deadzone.Value);}catch(Exception ex){Notice(ex.Message,"Cannot start session");}}
        async void StopOutput(){if(stopping)return;stopping=true;await Task.Run((Action)bridge.Stop);stopping=false;Canvas.Invalidate();}
        void MapDevice(){MapDevice(false);}
        void MapDevice(bool review){var d=input.Selected;if(d==null||bridge.Busy)return;if(review&&!d.Mapped){Notice("Create a mapping for this controller first.","Mapping needed");return;}mapping=true;timer.Stop();mappingNav.Selected=true;try{input.Select(null);using(var wizard=new MappingWizard(d,input,review))if(wizard.ShowDialog(this)==DialogResult.OK){current.Mapping=wizard.Mapping;Profiles.Save(current);if(Sdl.SDL_AddGamepadMapping(wizard.Mapping)<0)throw new InvalidOperationException(Sdl.Text(Sdl.SDL_GetError()));d.Mapped=Sdl.SDL_IsGamepad(d.Id);SetNote("Mapping saved. Test controls in live preview.");}input.Select(d);}catch(Exception ex){Notice(ex.Message,"Mapping setup");try{input.Select(d);}catch{}}finally{mapping=false;mappingNav.Selected=false;timer.Start();}}
        void ReloadSaved(){saved=Profiles.Load().Controllers;profileOffset=Math.Max(0,Math.Min(profileOffset,Math.Max(0,saved.Count-3)));if(editing==null||!saved.Any(p=>p.Key==editing.Key))editing=saved.FirstOrDefault(p=>current!=null&&p.Key==current.Key)??saved.FirstOrDefault();else editing=saved.First(p=>p.Key==editing.Key);BindRows();EditProfile(editing);}
        void BindRows(){for(int i=0;i<rows.Count;i++){int index=profileOffset+i;rows[i].Profile=index<saved.Count?saved[index]:null;rows[i].Selected=rows[i].Profile!=null&&editing!=null&&rows[i].Profile.Key==editing.Key;rows[i].Visible=page==1&&rows[i].Profile!=null;rows[i].Invalidate();}}
        void EditProfile(ControllerProfile p){editing=p;profileName.Text=p==null?"":p.Name;editDeadzone.Value=p==null?8:p.Deadzone;editRumble.Checked=p!=null&&p.Rumble;editHide.Checked=p!=null&&p.HidePhysical;editSave.Enabled=p!=null;foreach(var row in rows){row.Selected=row.Profile!=null&&p!=null&&row.Profile.Key==p.Key;row.Invalidate();}Canvas.Invalidate();}
        void ScrollProfiles(int delta){profileOffset=Math.Max(0,Math.Min(Math.Max(0,saved.Count-3),profileOffset+delta));BindRows();}
        void SaveEditing(){if(editing==null)return;try{string name=profileName.Text.Trim();if(name.Length==0){Notice("Enter a profile name.","Profile name");return;}editing.Name=name;editing.Deadzone=editDeadzone.Value;editing.Rumble=editRumble.Checked;editing.HidePhysical=editHide.Checked;Profiles.Save(editing);if(current!=null&&editing.Key==current.Key){current=Profiles.For(input.Selected);deadzone.Value=current.Deadzone;rumble.Checked=current.Rumble;hide.Checked=current.HidePhysical;}ReloadSaved();SetNote("Profile changes saved.");}catch(Exception ex){Notice(ex.Message,"Cannot save profile");}}
        void TickState(){if(closing||mapping)return;input.Pump();if(!bridge.Busy&&DateTime.UtcNow>=nextScan)Scan();InputState state;connected=input.Read(out state);pad=connected?state.Pad:new Gamepad();bool busy=bridge.Busy;
            start.Text=busy?"SESSION RUNNING":"START SESSION   →";start.Enabled=!busy&&!stopping&&input.Selected!=null&&input.Selected.Mapped;stop.Enabled=busy&&!stopping;ps.Enabled=touch.Enabled=bridge.Running;
            change.Enabled=deadzone.Enabled=hide.Enabled=!busy;save.Enabled=current!=null;rumbleTest.Enabled=connected&&!busy;profilesNav.Enabled=!busy;mappingNav.Enabled=input.Selected!=null&&!busy;
            bridge.RumbleEnabled=rumble.Checked;Canvas.Invalidate();
        }
        void PaintScreen(Graphics g){if(page==1)PaintProfiles(g);else PaintController(g);}
        void PaintController(Graphics g){
            StudioCanvas.Shell(g,"CONTROLLER / LIVE","Controller desk",connected?(bridge.Running?"SESSION LIVE":"CONNECTED"):"NO INPUT",connected);
            StudioTheme.ControllerIcon(g,new RectangleF(201,97,29,21),StudioTheme.Text);
            string name=current==null?"Choose a controller":current.Name;float nameSize=20;while(nameSize>14&&g.MeasureString(name,StudioTheme.Font(nameSize,true)).Width>165)nameSize-=.5f;
            StudioTheme.TextAt(g,name,246,98,nameSize,StudioTheme.Text,true,false,165,28);
            StudioTheme.Rule(g,414,96,414,119);StudioTheme.Rule(g,194,135,1184,135);StudioTheme.Rule(g,923,135,923,617);
            string tag=bridge.Error!=null?"CHECK SETUP":input.Selected!=null&&!input.Selected.Mapped?"MAPPING NEEDED":connected?"INPUT LIVE":"WAITING";
            using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,198,154,tag.Length>11?152:105,22);
            StudioTheme.TextAt(g,tag,206,157,12,Color.FromArgb(10,15,16),true,true);
            string info=bridge.Error??(DateTime.UtcNow<noteUntil?note:input.Selected!=null?input.Selected.HidingProblem:null);
            if(info!=null)StudioTheme.TextAt(g,info,365,153,11,StudioTheme.Muted,false,true,537,37);
            art.Draw(g,new RectangleF(224,185,660,396),pad);
            Callout(g,"L2",246,186,337,202);Callout(g,"L1",246,219,339,221);Callout(g,"R2",842,186,760,202);Callout(g,"R1",842,219,760,221);
            StudioTheme.TextAt(g,String.Format("LEFT STICK  {0:0.00} / {1:0.00}",pad.LX/32768f,pad.LY/32768f),196,579,11,StudioTheme.Text,false,true);
            StudioTheme.Rule(g,371,572,371,596);
            StudioTheme.TextAt(g,String.Format("RIGHT STICK  {0:0.00} / {1:0.00}",pad.RX/32768f,pad.RY/32768f),390,579,11,StudioTheme.Text,false,true);
            Meter(g,"L2",pad.LeftTrigger,620,584);Meter(g,"R2",pad.RightTrigger,779,584);
            StudioTheme.TextAt(g,"SESSION SETTINGS",944,157,15,StudioTheme.Text,true,true);StudioTheme.Rule(g,944,187,1184,187);
            StudioTheme.TextAt(g,"STICK DEADZONE",944,209,12,StudioTheme.Text,true,true);StudioTheme.TextAt(g,deadzone.Value+"%",1156,236,12,StudioTheme.Text,false,true);
            StudioTheme.Rule(g,944,276,1184,276);StudioTheme.TextAt(g,"FORWARD RUMBLE",944,298,13,StudioTheme.Text,true,true);
            StudioTheme.Rule(g,944,328,1184,328);StudioTheme.TextAt(g,"PREVENT DOUBLE INPUT",944,359,12,StudioTheme.Text,true,true);StudioTheme.Rule(g,944,383,1184,383);
            StudioTheme.TextAt(g,"Only the selected physical\ncontroller is hidden.",944,398,12,StudioTheme.Muted,false,true);
            StudioTheme.Rule(g,194,617,1184,617);StudioTheme.Rule(g,544,644,544,686);StudioTheme.Rule(g,842,644,842,686);
            StudioTheme.TextAt(g,"BACK + START: PS  /  BACK + R3: TOUCHPAD CLICK",865,650,11,StudioTheme.Muted,false,true,320,35);
        }
        static void Callout(Graphics g,string label,float x,float y,float endX,float endY){var rect=new RectangleF(x,y,32,21);using(var p=new Pen(Color.FromArgb(157,165,162),.8f)){g.DrawRectangle(p,rect.X,rect.Y,rect.Width,rect.Height);float origin=endX>x?rect.Right:rect.Left;g.DrawLine(p,origin,y+10,endX,endY);}StudioTheme.TextAt(g,label,x+6,y+3,12,StudioTheme.Text,false,true);}
        static void Meter(Graphics g,string label,byte value,float x,float y){StudioTheme.TextAt(g,label,x,y-6,11,StudioTheme.Text,true,true);using(var p=new Pen(StudioTheme.Muted,.9f))g.DrawRectangle(p,x+26,y-5,89,10);if(value>0)using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,x+28,y-3,85*value/255f,6);StudioTheme.TextAt(g,(value*100/255)+"%",x+123,y-6,10,StudioTheme.Text,false,true);}
        void PaintProfiles(Graphics g){
            StudioCanvas.Shell(g,"LIBRARY / LOCAL","Your controllers.","SAVED LOCALLY",true);
            StudioTheme.TextAt(g,"PROFILE",211,101,14,StudioTheme.Text,true,true);StudioTheme.TextAt(g,"INPUT MODE",638,101,14,StudioTheme.Text,true,true);StudioTheme.TextAt(g,"DEADZONE",858,101,14,StudioTheme.Text,true,true);StudioTheme.TextAt(g,"RUMBLE",1057,101,14,StudioTheme.Text,true,true);
            StudioTheme.Rule(g,194,130,1184,130);StudioTheme.Rule(g,194,312,1184,312);StudioTheme.Rule(g,677,312,677,629);
            GamepadArtwork.Outline(g,new RectangleF(237,333,390,235));
            StudioTheme.TextAt(g,"Each connection mode keeps its own settings.",196,597,11,StudioTheme.Text,false,true);
            StudioTheme.TextAt(g,"PROFILE DETAILS",701,328,15,StudioTheme.Text,true,true);StudioTheme.Rule(g,701,355,1184,355);
            StudioTheme.TextAt(g,"PROFILE NAME",701,378,12,StudioTheme.Text,true,true);StudioTheme.Rule(g,701,408,1184,408);
            StudioTheme.TextAt(g,"STICK DEADZONE",701,426,12,StudioTheme.Text,true,true);StudioTheme.TextAt(g,editDeadzone.Value+"%",1154,426,12,StudioTheme.Text,false,true);StudioTheme.Rule(g,701,453,1184,453);
            StudioTheme.TextAt(g,"RUMBLE",701,471,12,StudioTheme.Text,true,true);StudioTheme.Rule(g,701,496,1184,496);
            StudioTheme.TextAt(g,"PREVENT DOUBLE INPUT",701,514,12,StudioTheme.Text,true,true);StudioTheme.Rule(g,701,541,1184,541);StudioTheme.Rule(g,194,629,1184,629);
            if(saved.Count==0)StudioTheme.TextAt(g,"Connect a controller and save a profile to begin.",211,155,18,StudioTheme.Muted);
            if(DateTime.UtcNow<noteUntil)StudioTheme.TextAt(g,note,485,662,12,StudioTheme.Muted,false,true);
        }
    }
    public sealed class ProfileRow:Control,IStudioControl {
        public ControllerProfile Profile;public bool Selected;public float ScaleFactor{get;set;}
        public ProfileRow(){DoubleBuffered=true;Cursor=Cursors.Hand;TabStop=true;AccessibleRole=AccessibleRole.ListItem;}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
        protected override void OnPaint(PaintEventArgs e){if(Profile==null)return;AccessibleName=Profile.Name;var g=e.Graphics;StudioTheme.Setup(g);float scale=ScaleFactor>0?ScaleFactor:1;g.ScaleTransform(scale,scale);if(Selected){using(var b=new SolidBrush(Color.FromArgb(31,38,40)))g.FillRectangle(b,0,0,990,54);using(var b=new SolidBrush(StudioTheme.Orange))g.FillRectangle(b,0,0,5,54);}StudioTheme.ControllerIcon(g,new RectangleF(30,15,42,28),StudioTheme.Text);StudioTheme.TextAt(g,Profile.Name,107,18,18,StudioTheme.Text,true,false,305,30);string mode=Profile.Guid!=null&&(Profile.Guid.EndsWith("7801")||Profile.Guid.EndsWith("7200"))?"XInput":Profile.Guid!=null&&Profile.Guid.Contains("4c050000cc09")?"PS4 / HID":"Generic HID";StudioTheme.TextAt(g,mode,444,19,16,StudioTheme.Text,false,true);StudioTheme.TextAt(g,Profile.Deadzone+"%",664,19,16,StudioTheme.Text,false,true);StudioTheme.TextAt(g,Profile.Rumble?"ON":"OFF",863,19,16,StudioTheme.Text,false,true);StudioTheme.Rule(g,0,53,990,53);StudioTheme.Rule(g,423,7,423,47);StudioTheme.Rule(g,639,7,639,47);StudioTheme.Rule(g,838,7,838,47);}
    }
}
