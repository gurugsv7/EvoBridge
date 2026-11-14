using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace EvoBridge {
    public static class Sdl {
        const string D="SDL3.dll";
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]public static extern bool SDL_Init(uint flags);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]public static extern bool SDL_SetHint(string name,string value);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern void SDL_Quit();
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern void SDL_PumpEvents();
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern void SDL_UpdateGamepads();
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr SDL_GetJoysticks(out int count);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]public static extern bool SDL_IsGamepad(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr SDL_OpenGamepad(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern void SDL_CloseGamepad(IntPtr pad);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr SDL_OpenJoystick(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern void SDL_CloseJoystick(IntPtr pad);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern void SDL_free(IntPtr ptr);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr SDL_GetJoystickNameForID(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr SDL_GetJoystickPathForID(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern ushort SDL_GetJoystickVendorForID(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern ushort SDL_GetJoystickProductForID(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr SDL_GetJoystickSerial(IntPtr joy);
        [StructLayout(LayoutKind.Sequential)]public struct GuidBytes {public ulong Low,High;}
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern GuidBytes SDL_GetJoystickGUIDForID(uint id);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern void SDL_GUIDToString(GuidBytes guid,[Out]byte[] text,int size);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern int SDL_GetGamepadPlayerIndex(IntPtr pad);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern short SDL_GetGamepadAxis(IntPtr pad,int axis);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]public static extern bool SDL_GetGamepadButton(IntPtr pad,int button);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]public static extern bool SDL_GamepadConnected(IntPtr pad);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]public static extern bool SDL_RumbleGamepad(IntPtr pad,ushort low,ushort high,uint ms);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr SDL_GetError();
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern int SDL_AddGamepadMapping(string mapping);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern int SDL_GetNumJoystickButtons(IntPtr joy);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern int SDL_GetNumJoystickAxes(IntPtr joy);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern int SDL_GetNumJoystickHats(IntPtr joy);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]public static extern bool SDL_GetJoystickButton(IntPtr joy,int button);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern short SDL_GetJoystickAxis(IntPtr joy,int axis);
        [DllImport(D,CallingConvention=CallingConvention.Cdecl)]public static extern byte SDL_GetJoystickHat(IntPtr joy,int hat);
        public static string Text(IntPtr p){return p==IntPtr.Zero?"":Marshal.PtrToStringAnsi(p);}
        public static string GuidFor(uint id){byte[] b=new byte[33];SDL_GUIDToString(SDL_GetJoystickGUIDForID(id),b,b.Length);return System.Text.Encoding.ASCII.GetString(b,0,32);}
    }
    public sealed class ControllerDevice {
        public uint Id;public string Name,Path,Guid,Serial,InstanceId,TransportIdentity,ProfileKey,HidingProblem;
        public ushort Vendor,Product;public bool Mapped,Virtual,SharedTransport;
        public string[] HideInstances=new string[0];
        public override string ToString(){return Name+" · "+Vendor.ToString("X4")+":"+Product.ToString("X4")+(Mapped?"":" · needs mapping");}
    }
    public sealed class SdlInput : IDisposable {
        IntPtr gamepad;readonly object gate=new object();
        bool triedFallback;
        public string Diagnostic="";
        public volatile string RumbleError;
        public volatile bool RumbleAccepted;
        readonly Queue<Action> commands=new Queue<Action>();
        readonly AutoResetEvent wake=new AutoResetEvent(false);
        readonly Thread runtime;
        volatile bool shuttingDown;
        public ControllerDevice Selected;
        public SdlInput(){
            Exception failure=null;
            using(var ready=new ManualResetEvent(false)){
                runtime=new Thread(()=>{
                    try{Configure();Initialize();}catch(Exception ex){failure=ex;}finally{ready.Set();}
                    if(failure!=null)return;
                    try{while(!shuttingDown){
                        Action action=null;lock(commands){if(commands.Count>0)action=commands.Dequeue();}
                        if(action!=null)action();else{PumpCore();wake.WaitOne(8);}
                    }}finally{Sdl.SDL_Quit();}
                }){IsBackground=true,Name="EvoBridge SDL input"};
                runtime.SetApartmentState(ApartmentState.MTA);runtime.Start();ready.WaitOne();
            }
            if(failure!=null){wake.Dispose();throw new IOException("SDL input initialization failed.",failure);}
        }
        void Configure(){
            Sdl.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS","1");
            Sdl.SDL_SetHint("SDL_JOYSTICK_HIDAPI","1");
            Sdl.SDL_SetHint("SDL_JOYSTICK_HIDAPI_PS4","1");
            Sdl.SDL_SetHint("SDL_JOYSTICK_RAWINPUT","0");
            Sdl.SDL_SetHint("SDL_JOYSTICK_RAWINPUT_CORRELATE_XINPUT","1");
        }
        void Initialize(){if(!Sdl.SDL_Init(0x2000))throw new IOException(Sdl.Text(Sdl.SDL_GetError()));foreach(var p in Profiles.Load().Controllers)if(!String.IsNullOrEmpty(p.Mapping))Sdl.SDL_AddGamepadMapping(p.Mapping);}
        T Invoke<T>(Func<T> callback){
            if(Thread.CurrentThread==runtime)return callback();
            if(shuttingDown)throw new ObjectDisposedException("SDL input");
            var completion=new TaskCompletionSource<T>();lock(commands){commands.Enqueue(()=>{try{completion.SetResult(callback());}catch(Exception ex){completion.SetException(ex);}});}wake.Set();return completion.Task.GetAwaiter().GetResult();
        }
        void PumpCore(){Sdl.SDL_PumpEvents();Sdl.SDL_UpdateGamepads();}
        public void Pump(){Invoke(()=>{PumpCore();return true;});}
        public List<ControllerDevice> Detect(){return Invoke(DetectCore);}
        List<ControllerDevice> DetectCore(){
            PumpCore();int count;IntPtr list=Sdl.SDL_GetJoysticks(out count);var result=new List<ControllerDevice>();Diagnostic="SDL joystick count="+count;
            try{for(int i=0;i<count;i++){
                uint id=(uint)Marshal.ReadInt32(list,i*4);
                var d=new ControllerDevice{Id=id,Name=Sdl.Text(Sdl.SDL_GetJoystickNameForID(id)),Path=Sdl.Text(Sdl.SDL_GetJoystickPathForID(id)),Guid=Sdl.GuidFor(id),Vendor=Sdl.SDL_GetJoystickVendorForID(id),Product=Sdl.SDL_GetJoystickProductForID(id),Mapped=Sdl.SDL_IsGamepad(id)};
                IntPtr joy=Sdl.SDL_OpenJoystick(id);if(joy!=IntPtr.Zero){d.Serial=Sdl.Text(Sdl.SDL_GetJoystickSerial(joy));Sdl.SDL_CloseJoystick(joy);}
                DeviceTopology.Resolve(d);
                Diagnostic+="; "+d.Name+" virtual="+d.Virtual+" path="+d.Path;
                if(!d.Virtual)result.Add(d);
            }}finally{if(list!=IntPtr.Zero)Sdl.SDL_free(list);}
            if(result.Count==0&&!triedFallback&&Selected==null){triedFallback=true;Sdl.SDL_Quit();Sdl.SDL_SetHint("SDL_JOYSTICK_RAWINPUT","0");Initialize();return DetectCore();}
            foreach(var d in result){
                if(d.InstanceId==null&&d.Path.StartsWith("XInput#",StringComparison.Ordinal)){
                    var paths=HidDiscovery.GamepadPaths(d.Vendor,d.Product);
                    if(paths.Count==1&&result.Count(other=>other.Vendor==d.Vendor&&other.Product==d.Product)==1){d.Path=paths[0];DeviceTopology.Resolve(d);}
                    else d.HidingProblem="Cannot uniquely match this XInput device to a physical gamepad. Disconnect identical pads before setup or use preview without hiding.";
                }
                d.ProfileKey=Profiles.Key(d);
            }
            foreach(var d in result){
                d.SharedTransport=result.Any(other=>other.Id!=d.Id&&d.HideInstances.Any(x=>x.StartsWith("USB\\",StringComparison.OrdinalIgnoreCase)&&other.HideInstances.Contains(x,StringComparer.OrdinalIgnoreCase)));
                if(d.SharedTransport)d.HidingProblem="Multiple controllers share this transport node. Automatic hiding is disabled to protect the other controller.";
            }
            return result;
        }
        public void Select(ControllerDevice device){Invoke(()=>{SelectCore(device);return true;});}
        void SelectCore(ControllerDevice device){lock(gate){
            if(gamepad!=IntPtr.Zero){Sdl.SDL_RumbleGamepad(gamepad,0,0,0);Sdl.SDL_CloseGamepad(gamepad);gamepad=IntPtr.Zero;}
            Selected=device;
            if(device!=null&&device.Mapped){gamepad=Sdl.SDL_OpenGamepad(device.Id);if(gamepad==IntPtr.Zero)throw new IOException(Sdl.Text(Sdl.SDL_GetError()));}
        }}
        public bool Read(out InputState state){lock(gate){
            state=new InputState();if(gamepad==IntPtr.Zero||!Sdl.SDL_GamepadConnected(gamepad))return false;
            ushort[] mapping={Mapper.A,Mapper.B,Mapper.X,Mapper.Y,Mapper.Back,0,Mapper.Start,Mapper.L3,Mapper.R3,Mapper.L1,Mapper.R1,Mapper.Up,Mapper.Down,Mapper.Left,Mapper.Right};
            Gamepad p=new Gamepad();for(int b=0;b<mapping.Length;b++)if(Sdl.SDL_GetGamepadButton(gamepad,b))p.Buttons|=mapping[b];
            if(Sdl.SDL_GetGamepadButton(gamepad,5))p.Buttons|=(ushort)(Mapper.Back|Mapper.Start);
            if(Sdl.SDL_GetGamepadButton(gamepad,20))p.Buttons|=(ushort)(Mapper.Back|Mapper.R3);
            p.LX=Sdl.SDL_GetGamepadAxis(gamepad,0);p.LY=Flip(Sdl.SDL_GetGamepadAxis(gamepad,1));
            p.RX=Sdl.SDL_GetGamepadAxis(gamepad,2);p.RY=Flip(Sdl.SDL_GetGamepadAxis(gamepad,3));
            p.LeftTrigger=Trigger(Sdl.SDL_GetGamepadAxis(gamepad,4));p.RightTrigger=Trigger(Sdl.SDL_GetGamepadAxis(gamepad,5));
            state.Pad=p;return true;
        }}
        public static short Flip(short value){return value==short.MinValue?short.MaxValue:(short)-value;}
        public static byte Trigger(short value){return (byte)(Math.Max(0,(int)value)*255/32767);}
        public void Rumble(byte low,byte high){Invoke(()=>{lock(gate){if(gamepad!=IntPtr.Zero){RumbleAccepted=Sdl.SDL_RumbleGamepad(gamepad,(ushort)(low*257),(ushort)(high*257),(low==0&&high==0)?0u:1000u);RumbleError=RumbleAccepted?null:Sdl.Text(Sdl.SDL_GetError());}}return true;});}
        public bool TestRumble(){return Invoke(()=>{lock(gate){return gamepad!=IntPtr.Zero&&Sdl.SDL_RumbleGamepad(gamepad,30000,30000,600);}});}
        public void Dispose(){if(shuttingDown)return;Select(null);shuttingDown=true;wake.Set();if(!runtime.Join(5000))throw new IOException("SDL input thread did not exit.");wake.Dispose();}
    }
}
