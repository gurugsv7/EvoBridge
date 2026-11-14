using System;
using System.Threading;

namespace EvoBridge {
    public sealed class Bridge : IDisposable {
        readonly ManualResetEvent stop=new ManualResetEvent(false);
        Thread worker;
        readonly object gate=new object();
        InputState latest;
        public volatile string Status="Preview mode";
        public volatile string Error;
        public volatile bool Running;
        public volatile bool RumbleEnabled=true;
        public bool HidePhysical=true;
        public int Slot=-1;
        public int DeadzonePercent=8;
        SdlInput source;
        ControllerDevice device;
        long psUntil,touchUntil;
        volatile byte desiredLow,desiredHigh;
        public InputState Latest { get {lock(gate)return latest;} }
        public void PressPS(){Interlocked.Exchange(ref psUntil,DateTime.UtcNow.AddMilliseconds(180).Ticks);}
        public void PressTouch(){Interlocked.Exchange(ref touchUntil,DateTime.UtcNow.AddMilliseconds(180).Ticks);}
        public void Start(SdlInput input,ControllerDevice selected,int deadzone) {
            if(worker!=null && worker.IsAlive)throw new InvalidOperationException("Bridge is already active.");
            source=input;device=selected;
            InputState first;
            if(device==null||!device.Mapped||!source.Read(out first))throw new InvalidOperationException("Select a connected controller. Use Map buttons if it needs a mapping.");
            DeadzonePercent=deadzone;Error=null;stop.Reset();Status="Starting virtual DS4...";
            worker=new Thread(Run){IsBackground=true,Name="EvoBridge input"};worker.Start();
        }
        void Run() {
            InputHiding hiding=null;
            using(var backend=new Backend())using(var output=new ViiperDevice(Backend.ApiPort)) {
                try {
                    if(HidePhysical){hiding=new InputHiding();hiding.Enable(device);}
                    backend.Start(stop);output.Connect();
                    desiredLow=desiredHigh=0;
                    output.RumbleReceived+=(large,small)=>{desiredLow=large;desiredHigh=small;if(RumbleEnabled)source.Rumble(large,small);};
                    Running=true;Status="DS4 stream active";
                    DateTime nextRumbleRefresh=DateTime.UtcNow;
                    DateTime disconnected=DateTime.MinValue;
                    while(!stop.WaitOne(8)) {
                        if(DateTime.UtcNow>=nextRumbleRefresh){source.Rumble(RumbleEnabled?desiredLow:(byte)0,RumbleEnabled?desiredHigh:(byte)0);nextRumbleRefresh=DateTime.UtcNow.AddMilliseconds(500);}
                        InputState state;
                        if(source.Read(out state))disconnected=DateTime.MinValue;
                        else {
                            state=new InputState();
                            if(disconnected==DateTime.MinValue)disconnected=DateTime.UtcNow;
                            Status="Controller disconnected; sending neutral input";
                            if((DateTime.UtcNow-disconnected).TotalSeconds>=3)break;
                        }
                        lock(gate)latest=state;
                        bool ps=DateTime.UtcNow.Ticks<Interlocked.Read(ref psUntil);
                        bool touch=DateTime.UtcNow.Ticks<Interlocked.Read(ref touchUntil);
                        output.Send(Mapper.Report(state.Pad,DeadzonePercent/100.0,ps,touch));
                        if(disconnected==DateTime.MinValue)Status="DS4 stream active";
                    }
                }catch(OperationCanceledException){Status="Stopped";}
                catch(Exception ex){Error=ex.Message;Status="Output stopped";}
                finally {
                    try {output.Send(Mapper.Report(new Gamepad(),0,false,false));}catch{}
                    try {source.Rumble(0,0);}catch{}
                    // using disposes output before its private backend.
                    output.Dispose();Running=false;
                    if(hiding!=null)try{hiding.Dispose();}catch(Exception ex){Error="Could not restore physical input: "+ex.Message+" Use Restore Xbox.cmd.";}
                    if(Error==null)Status="Stopped; virtual controller released";
                }
            }
        }
        public void Stop(){stop.Set();if(worker!=null && worker!=Thread.CurrentThread)worker.Join(12000);}
        public bool Busy {get{return worker!=null && worker.IsAlive;}}
        public void Dispose(){Stop();if(!Busy)stop.Dispose();}
    }
}
