using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace EvoBridge {
    // Hides only exact instances resolved from the selected controller path.
    public sealed class HidingState {
        public bool Active;
        public bool CloakWasOn;
        public string[] AddedDevices; public string[] SelectedInstances;
    }
    public sealed class InputHiding : IDisposable {
        static string Journal {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"input-hiding-state.json");}}
        public static string CliPath {
            get {
                string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"Nefarius Software Solutions\HidHide");
                foreach(string relative in new[]{"HidHideCLI.exe",@"x64\HidHideCLI.exe"}) {
                    string path=Path.Combine(dir,relative);if(File.Exists(path))return path;
                }
                return null;
            }
        }
        static string Quote(string value) {
            if(value.IndexOf('"')>=0||value.EndsWith("\\"))throw new ArgumentException("Unsupported command argument.");
            return "\""+value+"\"";
        }
        static string Call(string arguments) {
            string cli=CliPath;if(cli==null)throw new IOException("HidHide is not installed. Install it and restart Windows before enabling double-input prevention.");
            using(var p=new Process()) {
                p.StartInfo=new ProcessStartInfo(cli,arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                p.Start();
                var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();
                if(!p.WaitForExit(6000)){p.Kill();p.WaitForExit();throw new IOException("HidHide did not respond. Close its Configuration Client and try again.");}
                string output=stdout.Result,error=stderr.Result;
                if(p.ExitCode!=0)throw new IOException("HidHide is unavailable. Restart Windows after installing it, and close its Configuration Client. "+error.Trim());
                return output;
            }
        }
        static string[] Devices(string text) {
            var found=new List<string>();
            foreach(string line in text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)) {
                string trimmed=line.Trim();if(!trimmed.StartsWith("--dev-hide ",StringComparison.Ordinal))continue;
                found.Add(trimmed.Substring(11).Trim().Trim('"'));
            }
            return found.ToArray();
        }
        static void Save(HidingState state){File.WriteAllText(Journal,new JavaScriptSerializer().Serialize(state));}
        public static void Restore() {
            if(!File.Exists(Journal))return;
            var state=new JavaScriptSerializer().Deserialize<HidingState>(File.ReadAllText(Journal));
            if(state==null||!state.Active)return;
            string args="";
            foreach(string device in state.AddedDevices??new string[0]) {
                // Do not trust a modified journal to target another device.
                if(!DeviceTopology.SafeInstance(device)||(state.SelectedInstances==null)||!state.SelectedInstances.Contains(device,StringComparer.OrdinalIgnoreCase))throw new IOException("Unexpected device in the recovery journal; no changes made.");
                args+=" --dev-unhide "+Quote(device);
            }
            if(args.Length>0)Call(args.Trim());
            // Preserve other tools' hides and global state.
            if(!state.CloakWasOn&&Devices(Call("--dev-list")).Length==0)Call("--cloak-off");
            state.Active=false;Save(state);
        }
        public void Enable(ControllerDevice device=null) {
            if(device==null)throw new IOException("Select an identified physical controller first.");
            if(device.SharedTransport)throw new IOException("This transport is shared by multiple controllers; automatic hiding is disabled.");
            DeviceTopology.Resolve(device);
            if(device.Virtual||device.HidingProblem!=null||device.HideInstances.Length==0)throw new IOException(device.HidingProblem??"Virtual output cannot be used as a source.");
            Restore();
            string snapshot=Call("--inv-state --cloak-state --dev-list --app-list");
            if(snapshot.Contains("--inv-on"))throw new IOException("HidHide uses an existing inverted application list. EvoBridge will not change that shared configuration.");
            string[] existing=Devices(snapshot);
            string[] added=device.HideInstances.Where(x=>!existing.Contains(x,StringComparer.OrdinalIgnoreCase)).ToArray();
            var state=new HidingState{Active=true,CloakWasOn=snapshot.Contains("--cloak-on"),AddedDevices=added,SelectedInstances=device.HideInstances};
            // Journal first: reopening EvoBridge or Restore Xbox.cmd can recover a crash.
            Save(state);
            try {
                Call("--app-reg "+Quote(System.Windows.Forms.Application.ExecutablePath));
                string args="";foreach(string instance in added)args+=" --dev-hide "+Quote(instance);
                Call((args+" --cloak-on").Trim());
            }catch{Restore();throw;}
        }
        public void Dispose(){Restore();}
    }
}
