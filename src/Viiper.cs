using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace EvoBridge {
    public sealed class ViiperDevice : IDisposable {
        readonly int port;
        TcpClient streamClient;
        NetworkStream stream;
        int bus=-1;
        string device;
        Thread feedback;
        volatile bool disposed;
        public event Action<byte,byte> RumbleReceived;
        public ViiperDevice(int apiPort) {port=apiPort;}
        TcpClient Open() {
            TcpClient client=new TcpClient();
            IAsyncResult attempt=client.BeginConnect("127.0.0.1",port,null,null);
            try {
                if (!attempt.AsyncWaitHandle.WaitOne(1500)) throw new IOException("VIIPER connection timed out.");
                client.EndConnect(attempt);
                client.NoDelay=true; client.SendTimeout=1500; client.ReceiveTimeout=2500;
                return client;
            } catch {client.Close(); throw;}
            finally {attempt.AsyncWaitHandle.Close();}
        }
        public Dictionary<string,object> Command(string command) {
            using(TcpClient client=Open()) {
                NetworkStream s=client.GetStream();
                byte[] request=Encoding.UTF8.GetBytes(command+"\0"); s.Write(request,0,request.Length);
                using(MemoryStream result=new MemoryStream()) {
                    byte[] b=new byte[2048]; int count;
                    while((count=s.Read(b,0,b.Length))>0) {
                        if(result.Length+count>65536) throw new IOException("VIIPER response exceeded the limit.");
                        result.Write(b,0,count);
                    }
                    string json=Encoding.UTF8.GetString(result.ToArray()).Trim();
                    if(json.Length==0) return new Dictionary<string,object>();
                    Dictionary<string,object> obj=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
                    if(obj.ContainsKey("status") && Convert.ToInt32(obj["status"])>=400)
                        throw new IOException(obj.ContainsKey("detail")?Convert.ToString(obj["detail"]):json);
                    return obj;
                }
            }
        }
        public bool Ping() {
            try {var p=Command("ping");return p.ContainsKey("server") && Convert.ToString(p["server"])=="VIIPER";}
            catch {return false;}
        }
        public void Connect() {
            try {
                bus=Convert.ToInt32(Command("bus/create")["busId"]);
                var added=Command("bus/"+bus+"/add {\"type\":\"dualshock4\"}");
                device=Convert.ToString(added["devId"]);
                int n;
                if(!Int32.TryParse(device,out n) || n<0) throw new IOException("Unexpected VIIPER device ID.");
                streamClient=Open();stream=streamClient.GetStream();
                byte[] handshake=Encoding.ASCII.GetBytes("bus/"+bus+"/"+device+"\0");
                stream.Write(handshake,0,handshake.Length);
                feedback=new Thread(ReadFeedback){IsBackground=true,Name="EvoBridge feedback"};feedback.Start();
            } catch {Dispose();throw;}
        }
        public void Send(byte[] packet) {
            if(disposed || stream==null) throw new IOException("DS4 stream is closed.");
            if(packet.Length!=31) throw new ArgumentException("Expected a 31-byte DS4 input packet.");
            stream.Write(packet,0,packet.Length);
        }
        void ReadFeedback() {
            try {
                byte[] b=new byte[8];int offset=0;
                while(!disposed) {
                    int read;
                    try {read=stream.Read(b,offset,8-offset);}
                    catch(IOException ex) {
                        SocketException socket=ex.InnerException as SocketException;
                        if(socket!=null && socket.SocketErrorCode==SocketError.TimedOut) continue;
                        break;
                    }
                    if(read==0) break;
                    offset+=read;
                    if(offset!=8) continue;
                    offset=0;
                    if((b[0]&1)!=0) {var handler=RumbleReceived;if(handler!=null)handler(b[2],b[1]);}
                }
            } catch(ObjectDisposedException) {} catch(IOException) {} catch(SocketException) {}
        }
        public void Dispose() {
            if(disposed)return;disposed=true;
            if(streamClient!=null)streamClient.Close();
            if(feedback!=null && feedback!=Thread.CurrentThread)feedback.Join(500);
            // Only remove this client's new device/bus, never a pre-existing shared bus.
            if(bus>=0) {
                try {if(device!=null)Command("bus/"+bus+"/remove "+device);}catch{}
                try {Command("bus/remove "+bus);}catch{}
            }
        }
    }
    public sealed class Backend : IDisposable {
        public const int ApiPort=39422;
        public const string ExpectedHash="142c144f6abee8b8a5f6a1abbc7018683444de71d2e45102cde6aaab9b2223e6";
        Process owned;
        string exe;
        readonly object logGate=new object();
        string diagnostics="";
        public string Diagnostics {get{lock(logGate)return diagnostics;}}
        void RecordOutput(object sender,DataReceivedEventArgs e) {
            if(String.IsNullOrWhiteSpace(e.Data))return;
            string clean=Regex.Replace(e.Data,"\u001b\\[[0-9;]*m","");
            // Upstream INFO startup output can include an authentication secret on
            // a separate unlabeled line. Retain diagnostics only at warning/error level.
            if(!Regex.IsMatch(clean,@"\b(WARN|ERROR|FATAL)\b"))return;
            string lower=clean.ToLowerInvariant();
            if(lower.Contains("password")||lower.Contains("token")||lower.Contains("secret")||lower.Contains("key"))return;
            lock(logGate){diagnostics+=(clean+"\n");if(diagnostics.Length>2200)diagnostics=diagnostics.Substring(diagnostics.Length-2200);}
        }
        public static string UsbipPath() {
            string[] folders={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"USBip"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"usbip-win2")};
            foreach(string dir in folders) {
                string file=Path.Combine(dir,"usbip.exe");if(File.Exists(file))return file;
            }
            foreach(string path in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')) {
                try {string file=Path.Combine(path.Trim('"'),"usbip.exe");if(File.Exists(file))return file;}catch{}
            }
            return null;
        }
        public void Start(WaitHandle cancellation) {
            if(cancellation.WaitOne(0))throw new OperationCanceledException();
            if(UsbipPath()==null)throw new IOException("USB/IP driver tools are not installed. Preview works; virtual output requires usbip-win2 setup.");
            using(var probe=new ViiperDevice(ApiPort)) {
                if(probe.Ping()) throw new IOException("Port 39422 already has a VIIPER instance. Stop the other EvoBridge instance first.");
            }
            exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"backend","viiper.exe");
            if(!File.Exists(exe))throw new FileNotFoundException("The bundled VIIPER backend is missing.",exe);
            using(var sha=SHA256.Create())using(var f=File.OpenRead(exe)) {
                string hash=BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();
                if(hash!=ExpectedHash)throw new IOException("Bundled VIIPER checksum does not match the pinned release.");
            }
            ProcessStartInfo info=new ProcessStartInfo(exe,
                "server --api.addr=127.0.0.1:39422 --usb.addr=127.0.0.1:39421 --api.auto-attach-local-client=true --update-notify=none");
            info.WorkingDirectory=Path.GetDirectoryName(exe);info.UseShellExecute=false;info.CreateNoWindow=true;
            info.RedirectStandardOutput=true;info.RedirectStandardError=true;
            info.EnvironmentVariables["PATH"]=Path.GetDirectoryName(UsbipPath())+";"+info.EnvironmentVariables["PATH"];
            owned=new Process{StartInfo=info};owned.OutputDataReceived+=RecordOutput;owned.ErrorDataReceived+=RecordOutput;
            owned.Start();owned.BeginOutputReadLine();owned.BeginErrorReadLine();
            for(int i=0;i<40;i++) {
                if(cancellation.WaitOne(0))throw new OperationCanceledException();
                if(owned.HasExited)throw new IOException("VIIPER exited during startup. "+Diagnostics);
                using(var probe=new ViiperDevice(ApiPort))if(probe.Ping())return;
                if(cancellation.WaitOne(100))throw new OperationCanceledException();
            }
            throw new IOException("VIIPER did not become ready.");
        }
        public void Dispose() {
            if(owned==null)return;
            try {
                if(!owned.HasExited) {
                    // Device streams/bus are already detached before ending this private backend.
                    if(!owned.CloseMainWindow() || !owned.WaitForExit(500))owned.Kill();
                    owned.WaitForExit(2500);
                }
            }catch(InvalidOperationException){}finally{owned.Dispose();owned=null;}
        }
    }
}
