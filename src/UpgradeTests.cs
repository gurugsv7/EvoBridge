using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;

namespace EvoBridge {
    public static class UpgradeTests {
        static void Save(string path,object value){File.WriteAllText(path,new JavaScriptSerializer().Serialize(value));}
        static void Check(bool value,string name,List<string> checks){if(!value)throw new Exception(name);checks.Add(name);}
        public static int Software(string path){var checks=new List<string>();try{
            string iface=@"\\?\HID#VID_1234&PID_5678&IG_00#7&SYNTHETIC&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
            Check(DeviceTopology.InstanceFromPath(iface)==@"HID\VID_1234&PID_5678&IG_00\7&SYNTHETIC&0&0000","HID interface resolves to its exact controller instance",checks);
            Check(DeviceTopology.InstanceFromPath("XInput#0")==null&&DeviceTopology.InstanceFromPath(@"\\?\ROOT#SYSTEM#0#{x}")==null,"Unresolvable paths and system roots cannot become hiding targets",checks);
            Check(!DeviceTopology.SafeInstance("USB\\*")&&!DeviceTopology.SafeInstance("HID\\x\" --cloak-off")&&!DeviceTopology.SafeInstance("ROOT\\USB\\0000"),"Wildcards, injected arguments and host-controller roots are rejected",checks);
            var one=new ControllerDevice{Guid="model",InstanceId="HID\\one",TransportIdentity="USB\\serial-one"};
            var two=new ControllerDevice{Guid="model",InstanceId="HID\\two",TransportIdentity="USB\\serial-two"};
            Check(Profiles.Key(one)!=Profiles.Key(two),"Identical controller models keep separate per-device profiles",checks);
            string before=Profiles.Key(one);one.InstanceId="HID\\other-interface";
            Check(Profiles.Key(one)==before,"Changing a HID interface preserves a profile with stable transport identity",checks);
            one.TransportIdentity=one.TransportIdentity.ToLowerInvariant();
            Check(Profiles.Key(one)==before,"Windows path casing does not create a duplicate profile",checks);
            bool blocked=false;try{new InputHiding().Enable(new ControllerDevice{SharedTransport=true});}catch(IOException){blocked=true;}
            Check(blocked,"A shared transport is rejected before any hiding configuration is changed",checks);
            one.Serial="stable-serial";before=Profiles.Key(one);one.TransportIdentity="USB\\different-port";
            Check(Profiles.Key(one)==before,"A reported device serial preserves settings across transport-path changes",checks);
            Check(SdlInput.Flip(short.MinValue)==short.MaxValue&&SdlInput.Flip(0)==0&&SdlInput.Trigger(-100)==0&&SdlInput.Trigger(short.MaxValue)==255,"SDL axis conversion handles endpoints and released/full triggers",checks);
            Profiles.TestPath=path+".profiles.json";
            var profile=new ControllerProfile{Key="verification-profile",Name="Generic test",Guid="test",Deadzone=13,Rumble=false,HidePhysical=false,Mapping="test,Test,a:b2,platform:Windows,"};
            Profiles.Save(profile);var loaded=Profiles.Load().Controllers.First(p=>p.Key==profile.Key);
            Check(loaded.Deadzone==13&&!loaded.Rumble&&!loaded.HidePhysical&&loaded.Mapping==profile.Mapping,"Saved settings and custom mappings survive a disk reload",checks);
            profile.Deadzone=19;Profiles.Save(profile);
            Check(Profiles.Load().Controllers.Count(p=>p.Key==profile.Key)==1&&File.Exists(Profiles.PathName+".backup"),"Updating a profile keeps one entry and preserves the prior file as a backup",checks);
            GenericSimulation.Run(checks);
            Save(path,new{success=true,checks=checks});return 0;
        }catch(Exception ex){Save(path,new{success=false,checks=checks,error=ex.Message});return 1;}finally{Profiles.TestPath=null;}}
        public static int Run(string path,int seconds=30){
            using(var input=new SdlInput())using(var bridge=new Bridge()){
                try{
                    var devices=input.Detect();for(int attempt=0;devices.Count==0&&attempt<50;attempt++){Thread.Sleep(40);devices=input.Detect();}
                    if(devices.Count!=1)throw new IOException("Connect exactly one physical controller for the bounded integration test; detected "+devices.Count+".");
                    var selected=devices[0];if(!selected.Mapped)throw new IOException("This controller needs mapping first.");input.Select(selected);
                    var profile=Profiles.For(selected);Profiles.Save(profile);
                    bridge.HidePhysical=true;bridge.RumbleEnabled=true;bridge.Start(input,selected,profile.Deadzone);
                    DateTime deadline=DateTime.UtcNow.AddSeconds(seconds);int samples=0,changes=0,previous=-1;bool virtualFound=false,rumbleSent=false,rumbleAccepted=false;IntPtr output=IntPtr.Zero;
                    DateTime nextReport=DateTime.MinValue;
                    try{while(DateTime.UtcNow<deadline){
                        input.Pump();if(bridge.Error!=null)throw new IOException(bridge.Error);
                        InputState state;if(!input.Read(out state))throw new IOException("Physical source disconnected.");samples++;if(previous>=0&&previous!=state.Pad.Buttons)changes++;previous=state.Pad.Buttons;
                        if(DateTime.UtcNow>=nextReport){
                            nextReport=DateTime.UtcNow.AddSeconds(1);
                            if(output==IntPtr.Zero&&bridge.Running){int count;IntPtr list=Sdl.SDL_GetJoysticks(out count);try{for(int i=0;i<count;i++){
                                uint id=(uint)System.Runtime.InteropServices.Marshal.ReadInt32(list,i*4);var d=new ControllerDevice{Id=id,Path=Sdl.Text(Sdl.SDL_GetJoystickPathForID(id))};DeviceTopology.Resolve(d);
                                if(d.Virtual&&Sdl.SDL_IsGamepad(id)){output=Sdl.SDL_OpenGamepad(id);virtualFound=output!=IntPtr.Zero;break;}
                            }}finally{if(list!=IntPtr.Zero)Sdl.SDL_free(list);}}
                            Save(path,new{phase="streaming",selected=selected,physicalInputRead=true,virtualDS4Found=virtualFound,samples=samples,inputButtonChanges=changes,rumbleCommandAccepted=rumbleAccepted,physicalRumbleCommandAccepted=input.RumbleAccepted,physicalRumbleError=input.RumbleError});
                        }
                        if(seconds<=30&&output!=IntPtr.Zero&&!rumbleSent&&samples>400){rumbleAccepted=Sdl.SDL_RumbleGamepad(output,30000,30000,650);rumbleSent=true;}
                        Thread.Sleep(8);
                    }}finally{if(output!=IntPtr.Zero){Sdl.SDL_RumbleGamepad(output,0,0,0);Sdl.SDL_CloseGamepad(output);}}
                    bridge.Stop();if(bridge.Error!=null)throw new IOException(bridge.Error);
                    Save(path,new{success=virtualFound,phase="complete",selected=selected,physicalInputRead=true,virtualDS4Found=virtualFound,samples=samples,inputButtonChanges=changes,rumbleCommandAccepted=rumbleAccepted,physicalRumbleCommandAccepted=input.RumbleAccepted,physicalRumbleError=input.RumbleError,physicalRumbleConfirmed=false,remotePlayTested=false});return virtualFound?0:1;
                }catch(Exception ex){bridge.Stop();Save(path,new{success=false,phase="failed",error=ex.Message});return 1;}
            }
        }
    }
}
