using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace EvoBridge {
    public static class SelfTests {
        static readonly List<string> passed=new List<string>();
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);passed.Add(message);}
        static ushort Buttons(byte[] p){return (ushort)(p[4]|p[5]<<8);}
        public static int Run(string reportPath) {
            try {
                byte[] neutral=Mapper.Report(new Gamepad(),0.08,false,false);
                Check(neutral.Length==31,"DS4 packet matches the documented 31-byte contract");
                bool allZero=true;foreach(byte b in neutral)allZero&=b==0;
                Check(allZero,"Disconnect/neutral input releases every button, axis and trigger");
                ushort[] source={Mapper.A,Mapper.B,Mapper.X,Mapper.Y,Mapper.L1,Mapper.R1,Mapper.Back,Mapper.Start,Mapper.L3,Mapper.R3};
                ushort[] expected={0x20,0x40,0x10,0x80,0x100,0x200,0x1000,0x2000,0x4000,0x8000};
                bool mappings=true;
                for(int i=0;i<source.Length;i++)mappings&=Buttons(Mapper.Report(new Gamepad{Buttons=source[i]},0,false,false))==expected[i];
                Check(mappings,"All ten physical buttons match the documented DS4 bit values");
                var chord=Mapper.Report(new Gamepad{Buttons=(ushort)(Mapper.Back|Mapper.Start)},0,false,false);
                Check(Buttons(chord)==1,"PS chord consumes Share/Options and emits only PS");
                chord=Mapper.Report(new Gamepad{Buttons=(ushort)(Mapper.Back|Mapper.R3)},0,false,false);
                Check(Buttons(chord)==2,"Touchpad chord consumes Share/R3 and emits only touch click");
                Check(Buttons(Mapper.Report(new Gamepad(),0,true,true))==3,"On-screen PS/touch controls set the two correct special bits");
                byte[] p=Mapper.Report(new Gamepad{Buttons=(ushort)(Mapper.Up|Mapper.Right),LeftTrigger=255,RightTrigger=31},0,false,false);
                Check(p[6]==9&&p[7]==255&&p[8]==31&&Buttons(p)==0xC00,"Diagonal D-pad and analogue/digital triggers use the correct wire fields");
                p=Mapper.Report(new Gamepad{LX=32767,RY=32767},0,false,false);
                Check((sbyte)p[0]==127&&(sbyte)p[3]==-128,"Full positive axes map correctly and vertical direction is inverted");
                p=Mapper.Report(new Gamepad{LX=-32768,RY=-32768},0,false,false);
                Check((sbyte)p[0]==-128&&(sbyte)p[3]==127,"Negative axis endpoints do not overflow");
                p=Mapper.Report(new Gamepad{LX=1000,LY=-1000},0.08,false,false);
                Check(p[0]==0&&p[1]==0,"Radial deadzone removes small centre noise");
                p=Mapper.Report(new Gamepad{LX=32767,LY=32767},0,false,false);
                Check((sbyte)p[0]>=89&&(sbyte)p[0]<=91&&(sbyte)p[1]>=-92&&(sbyte)p[1]<=-89,"Diagonal stick output is normalized rather than over-amplified");
                using(var fake=new FakeServer()) {
                    using(var device=new ViiperDevice(fake.Port)) {
                        Check(device.Ping(),"Loopback backend identity is verified");
                        ManualResetEvent feedback=new ManualResetEvent(false);
                        byte large=0,small=0;
                        device.RumbleReceived+=(l,s)=>{large=l;small=s;feedback.Set();};
                        device.Connect();device.Send(Mapper.Report(new Gamepad{Buttons=Mapper.A},0,false,false));
                        Check(fake.PacketArrived.WaitOne(3000),"A binary DS4 report follows the null-terminated stream handshake");
                        Check(fake.Packet!=null&&fake.Packet.Length==31&&fake.Packet[4]==0x20,"Mock backend receives a Cross press with the exact documented byte layout");
                        Check(feedback.WaitOne(3000)&&large==34&&small==17,"Eight-byte feedback routes large and small rumble motors correctly");
                        feedback.Dispose();
                    }
                    Check(fake.HasCommand("bus/7/remove 12")&&fake.HasCommand("bus/remove 7"),"Stopping removes only this client's device and newly created bus");
                }
                File.WriteAllText(reportPath,new JavaScriptSerializer().Serialize(new {success=true,checks=passed,hardwareOutputTested=false}));
                return 0;
            }catch(Exception ex){File.WriteAllText(reportPath,new JavaScriptSerializer().Serialize(new {success=false,checks=passed,error=ex.ToString()}));return 1;}
        }
        sealed class FakeServer : IDisposable {
            readonly TcpListener listener;
            readonly Thread acceptor;
            readonly List<string> commands=new List<string>();
            readonly List<TcpClient> clients=new List<TcpClient>();
            volatile bool disposed;
            public readonly ManualResetEvent PacketArrived=new ManualResetEvent(false);
            public byte[] Packet;
            public int Port{get;private set;}
            public FakeServer(){listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Port=((IPEndPoint)listener.LocalEndpoint).Port;acceptor=new Thread(Accept){IsBackground=true};acceptor.Start();}
            public bool HasCommand(string command){lock(commands)return commands.Contains(command);}
            void Accept(){while(!disposed){try{TcpClient c=listener.AcceptTcpClient();lock(clients)clients.Add(c);new Thread(()=>Handle(c)){IsBackground=true}.Start();}catch(SocketException){break;}catch(ObjectDisposedException){break;}}}
            void Handle(TcpClient client){
                using(client)try {
                    NetworkStream stream=client.GetStream();List<byte> b=new List<byte>();int read;
                    while((read=stream.ReadByte())>0){b.Add((byte)read);if(b.Count>4096)throw new IOException("Invalid test handshake");}
                    string command=Encoding.UTF8.GetString(b.ToArray());lock(commands)commands.Add(command);
                    if(command=="bus/7/12") {
                        byte[] packet=new byte[31];int offset=0;
                        while(offset<31){int n=stream.Read(packet,offset,31-offset);if(n==0)return;offset+=n;}
                        Packet=packet;PacketArrived.Set();
                        stream.Write(new byte[]{1,17,34,0,0,0,0,0},0,8);
                        while(!disposed && stream.ReadByte()!=-1){}
                        return;
                    }
                    string response="{}";
                    if(command=="ping")response="{\"server\":\"VIIPER\",\"version\":\"0.9.0\"}";
                    if(command=="bus/create")response="{\"busId\":7}";
                    if(command=="bus/7/add {\"type\":\"dualshock4\"}")response="{\"busId\":7,\"devId\":\"12\"}";
                    byte[] reply=Encoding.UTF8.GetBytes(response+"\n");stream.Write(reply,0,reply.Length);
                }catch(IOException){}catch(ObjectDisposedException){}
            }
            public void Dispose(){disposed=true;listener.Stop();lock(clients)foreach(var c in clients)c.Close();acceptor.Join(1000);PacketArrived.Dispose();}
        }
    }
}
