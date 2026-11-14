using System;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;

namespace EvoBridge {
    public static class HardwareTest {
        static void Report(string path,object data){File.WriteAllText(path,new JavaScriptSerializer().Serialize(data));}
        public static int Run(string path) {
            int slot=XInput.FirstConnected();InputState input;
            if(slot<0||!XInput.Read(slot,out input)){
                Report(path,new {success=false,phase="stopped",error="No physical XInput controller is available. Connect and power on the controller."});return 1;
            }
            using(var cancellation=new ManualResetEvent(false))using(var backend=new Backend())using(var output=new ViiperDevice(Backend.ApiPort)) {
                try {
                    backend.Start(cancellation);output.Connect();
                    int[] windowsTest=WindowsJoystick.Test(output);
                    Report(path,new {phase="streaming",sourceIndex=slot,testSeconds=20});
                    DateTime deadline=DateTime.UtcNow.AddSeconds(20);int reports=0,changes=0;
                    Gamepad previous=input.Pad;
                    while(DateTime.UtcNow<deadline) {
                        if(!XInput.Read(slot,out input))throw new IOException("Physical controller disconnected during the hardware test.");
                        if(input.Pad.Buttons!=previous.Buttons||input.Pad.LX!=previous.LX||input.Pad.LY!=previous.LY||input.Pad.RX!=previous.RX||input.Pad.RY!=previous.RY||input.Pad.LeftTrigger!=previous.LeftTrigger||input.Pad.RightTrigger!=previous.RightTrigger)changes++;
                        previous=input.Pad;output.Send(Mapper.Report(input.Pad,0.08,false,false));reports++;Thread.Sleep(8);
                    }
                    output.Send(Mapper.Report(new Gamepad(),0,false,false));
                    Report(path,new {success=true,phase="complete",physicalInputRead=true,sourceIndex=slot,forwardedReports=reports,inputChangesObserved=changes,windowsJoystickDetected=windowsTest[0]==1,windowsFaceButtonChecksPassed=windowsTest[1],windowsAnalogCheckPassed=windowsTest[2]==1,remotePlayTested=false,rumbleHardwareTested=false,backendDiagnostics=backend.Diagnostics});
                    return 0;
                }catch(Exception ex){Report(path,new {success=false,phase="stopped",error=ex.Message,backendDiagnostics=backend.Diagnostics});return 1;}
                finally{try{output.Send(Mapper.Report(new Gamepad(),0,false,false));}catch{};XInput.Rumble(slot,0,0);}
            }
        }
    }
}
