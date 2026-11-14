using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace EvoBridge {
    public static class WindowsJoystick {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        struct Caps {
            public ushort Vendor,Product;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Name;
            public uint XMin,XMax,YMin,YMax,ZMin,ZMax,Buttons,PeriodMin,PeriodMax,
                RMin,RMax,UMin,UMax,VMin,VMax,Flags,MaxAxes,Axes,MaxButtons;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Registry;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Oem;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct Position {
            public uint Size,Flags,X,Y,Z,R,U,V,Buttons,ButtonNumber,Pov,Reserved1,Reserved2;
        }
        [DllImport("winmm.dll")]static extern uint joyGetNumDevs();
        [DllImport("winmm.dll",CharSet=CharSet.Unicode,EntryPoint="joyGetDevCapsW")]
        static extern uint GetCaps(UIntPtr index,out Caps caps,uint size);
        [DllImport("winmm.dll",EntryPoint="joyGetPosEx")]
        static extern uint GetPosition(uint index,ref Position position);
        static int FindDS4() {
            for(uint i=0;i<joyGetNumDevs();i++) {
                Caps c;
                if(GetCaps((UIntPtr)i,out c,(uint)Marshal.SizeOf(typeof(Caps)))==0 && c.Vendor==0x054c && (c.Product==0x09cc||c.Product==0x05c4)) {
                    var p=new Position{Size=52,Flags=0xff};if(GetPosition(i,ref p)==0)return (int)i;
                }
            }
            return -1;
        }
        public static int[] Test(ViiperDevice output) {
            int id=-1;
            for(int i=0;i<20&&id<0;i++){id=FindDS4();if(id<0)Thread.Sleep(100);}
            if(id<0)return new int[]{0,0,0};
            ushort[] buttons={Mapper.A,Mapper.B,Mapper.X,Mapper.Y};uint[] expected={2,4,1,8};
            int passed=0;
            for(int i=0;i<buttons.Length;i++) {
                output.Send(Mapper.Report(new Gamepad{Buttons=buttons[i]},0,false,false));Thread.Sleep(100);
                var p=new Position{Size=52,Flags=0xff};if(GetPosition((uint)id,ref p)==0&&p.Buttons==expected[i])passed++;
            }
            output.Send(Mapper.Report(new Gamepad{LX=-32768},0,false,false));Thread.Sleep(100);
            var axis=new Position{Size=52,Flags=0xff};bool analog=GetPosition((uint)id,ref axis)==0&&axis.X<2000;
            output.Send(Mapper.Report(new Gamepad(),0,false,false));Thread.Sleep(100);
            return new int[]{1,passed,analog?1:0};
        }
    }
}
