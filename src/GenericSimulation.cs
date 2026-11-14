using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace EvoBridge {
    public static class GenericSimulation {
        [StructLayout(LayoutKind.Sequential)]struct Descriptor {
            public uint Version;public ushort Type,Padding,Vendor,Product,Axes,Buttons,Balls,Hats,Touchpads,Sensors,Padding2,Padding3;public uint ButtonMask,AxisMask;
            public IntPtr Name,TouchpadData,SensorData,Userdata,Update,PlayerIndex,Rumble,RumbleTriggers,LED,Effect,EnableSensors,Cleanup;
        }
        [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]static extern uint SDL_AttachVirtualJoystick(ref Descriptor desc);
        [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_DetachVirtualJoystick(uint id);
        [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_SetJoystickVirtualAxis(IntPtr joy,int axis,short value);
        [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_SetJoystickVirtualButton(IntPtr joy,int button,[MarshalAs(UnmanagedType.I1)]bool value);
        [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_SetJoystickVirtualHat(IntPtr joy,int hat,byte value);
        static void Check(bool ok,string name,List<string> checks){if(!ok)throw new Exception(name+": "+Sdl.Text(Sdl.SDL_GetError()));checks.Add(name);}
        public static void Run(List<string> checks){
            using(var input=new SdlInput()){
                IntPtr name=Marshal.StringToHGlobalAnsi("EvoBridge unmapped generic simulation"),joy=IntPtr.Zero;uint id=0;
                try{
                    var descriptor=new Descriptor{Version=(uint)Marshal.SizeOf(typeof(Descriptor)),Type=0,Vendor=0xCAFE,Product=0x0101,Axes=6,Buttons=16,Hats=1,Name=name};
                    Check(descriptor.Version==136,"SDL virtual descriptor matches the pinned x64 ABI",checks);
                    id=SDL_AttachVirtualJoystick(ref descriptor);if(id==0)throw new Exception(Sdl.Text(Sdl.SDL_GetError()));joy=Sdl.SDL_OpenJoystick(id);
                    SDL_SetJoystickVirtualAxis(joy,4,short.MinValue);SDL_SetJoystickVirtualAxis(joy,5,short.MinValue);input.Pump();
                    var device=input.Detect().First(d=>d.Id==id);
                    Check(!device.Mapped&&device.HidingProblem!=null,"An unknown generic joystick is detected and unsafe hiding is withheld",checks);
                    string mapping=device.Guid+",Generic simulation,a:b2,b:b3,x:b0,y:b1,back:b4,start:b5,leftstick:b6,rightstick:b7,leftshoulder:b8,rightshoulder:b9,dpup:h0.1,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,guide:b14,touchpad:b15,platform:Windows,";
                    Check(Sdl.SDL_AddGamepadMapping(mapping)>=0,"A custom mapping enables an initially unsupported generic joystick",checks);
                    input.Pump();device.Mapped=Sdl.SDL_IsGamepad(id);input.Select(device);
                    SDL_SetJoystickVirtualButton(joy,2,true);SDL_SetJoystickVirtualHat(joy,0,9);SDL_SetJoystickVirtualAxis(joy,0,30000);SDL_SetJoystickVirtualAxis(joy,1,20000);SDL_SetJoystickVirtualAxis(joy,4,short.MaxValue);input.Pump();
                    InputState state;bool read=input.Read(out state);
                    Check(read&&(state.Pad.Buttons&(Mapper.A|Mapper.Up|Mapper.Left))==(Mapper.A|Mapper.Up|Mapper.Left)&&state.Pad.LX==30000&&state.Pad.LY==-20000&&state.Pad.LeftTrigger==255&&state.Pad.RightTrigger==0,"Generic button, hat, axis orientation and independent trigger mapping reaches the mapper correctly",checks);
                    SDL_SetJoystickVirtualButton(joy,2,false);SDL_SetJoystickVirtualHat(joy,0,0);SDL_SetJoystickVirtualAxis(joy,0,0);SDL_SetJoystickVirtualAxis(joy,1,0);SDL_SetJoystickVirtualAxis(joy,4,short.MinValue);input.Pump();input.Read(out state);
                    Check(state.Pad.Buttons==0&&state.Pad.LX==0&&state.Pad.LY==0&&state.Pad.LeftTrigger==0,"Released generic controls return to neutral without sticky buttons or triggers",checks);
                    SDL_SetJoystickVirtualButton(joy,14,true);input.Pump();input.Read(out state);byte[] report=Mapper.Report(state.Pad,0,false,false);
                    Check(report[4]==1&&report[5]==0,"A native guide button produces PS without Share or Options",checks);
                    SDL_SetJoystickVirtualButton(joy,14,false);SDL_SetJoystickVirtualButton(joy,15,true);input.Pump();input.Read(out state);report=Mapper.Report(state.Pad,0,false,false);
                    Check(report[4]==2&&report[5]==0,"A native touchpad button produces the DS4 touchpad click",checks);
                }finally{input.Select(null);if(joy!=IntPtr.Zero)Sdl.SDL_CloseJoystick(joy);if(id!=0)SDL_DetachVirtualJoystick(id);Marshal.FreeHGlobal(name);}
            }
        }
    }
}
