using System;
using System.Runtime.InteropServices;

namespace EvoBridge {
    [StructLayout(LayoutKind.Sequential)]
    public struct Gamepad {
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short LX, LY, RX, RY;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct InputState { public uint Packet; public Gamepad Pad; }
    public static class Mapper {
        public const ushort Up=1, Down=2, Left=4, Right=8, Start=16, Back=32,
            L3=64, R3=128, L1=256, R1=512, A=4096, B=8192, X=16384, Y=32768;
        public static byte[] Report(Gamepad p, double deadzone, bool ps, bool touch) {
            if (deadzone < 0 || deadzone >= 1) throw new ArgumentOutOfRangeException("deadzone");
            byte[] r=new byte[31];
            Stick(p.LX,p.LY,deadzone,r,0); Stick(p.RX,p.RY,deadzone,r,2);
            ushort b=0;
            if ((p.Buttons&A)!=0) b|=0x20;
            if ((p.Buttons&B)!=0) b|=0x40;
            if ((p.Buttons&X)!=0) b|=0x10;
            if ((p.Buttons&Y)!=0) b|=0x80;
            if ((p.Buttons&L1)!=0) b|=0x100;
            if ((p.Buttons&R1)!=0) b|=0x200;
            if (p.LeftTrigger>30) b|=0x400;
            if (p.RightTrigger>30) b|=0x800;
            bool psChord=(p.Buttons&(Back|Start))==(Back|Start);
            bool touchChord=(p.Buttons&(Back|R3))==(Back|R3) && !psChord;
            if ((p.Buttons&Back)!=0 && !psChord && !touchChord) b|=0x1000;
            if ((p.Buttons&Start)!=0 && !psChord) b|=0x2000;
            if ((p.Buttons&L3)!=0) b|=0x4000;
            if ((p.Buttons&R3)!=0 && !touchChord) b|=0x8000;
            if (ps || psChord) b|=1;
            if (touch || touchChord) b|=2;
            r[4]=(byte)b; r[5]=(byte)(b>>8);
            // VIIPER uses direction bits, not the USB HID hat ordinal.
            r[6]=(byte)(p.Buttons&15);
            r[7]=p.LeftTrigger; r[8]=p.RightTrigger;
            // Remaining touch/IMU fields are zero; this controller has no motion sensors.
            return r;
        }
        static void Stick(short rawX, short rawY, double dz, byte[] r, int offset) {
            double x=rawX/(rawX<0?32768.0:32767.0);
            double y=rawY/(rawY<0?32768.0:32767.0);
            double magnitude=Math.Sqrt(x*x+y*y);
            if (magnitude<=dz) return;
            double scaled=(Math.Min(magnitude,1)-dz)/(1-dz);
            x=x/magnitude*scaled; y=-y/magnitude*scaled;
            r[offset]=Axis(x); r[offset+1]=Axis(y);
        }
        static byte Axis(double value) {
            int v=(int)Math.Round(value*(value<0?128:127),MidpointRounding.AwayFromZero);
            return unchecked((byte)(sbyte)Math.Max(-128,Math.Min(127,v)));
        }
    }
}
