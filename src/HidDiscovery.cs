using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EvoBridge {
    public static class HidDiscovery {
        [StructLayout(LayoutKind.Sequential)]struct Interface {public uint Size;public Guid ClassGuid;public uint Flags;public IntPtr Reserved;}
        [DllImport("hid.dll")]static extern void HidD_GetHidGuid(out Guid guid);
        [DllImport("setupapi.dll",CharSet=CharSet.Unicode)]static extern IntPtr SetupDiGetClassDevsW(ref Guid guid,IntPtr enumerator,IntPtr parent,uint flags);
        [DllImport("setupapi.dll")][return:MarshalAs(UnmanagedType.Bool)]static extern bool SetupDiEnumDeviceInterfaces(IntPtr set,IntPtr info,ref Guid guid,uint index,ref Interface data);
        [DllImport("setupapi.dll",CharSet=CharSet.Unicode)][return:MarshalAs(UnmanagedType.Bool)]static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr set,ref Interface data,IntPtr detail,uint size,out uint required,IntPtr info);
        [DllImport("setupapi.dll")][return:MarshalAs(UnmanagedType.Bool)]static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern SafeFileHandle CreateFileW(string path,uint access,uint share,IntPtr security,uint mode,uint flags,IntPtr template);
        [DllImport("hid.dll")][return:MarshalAs(UnmanagedType.Bool)]static extern bool HidD_GetPreparsedData(SafeFileHandle handle,out IntPtr data);
        [DllImport("hid.dll")][return:MarshalAs(UnmanagedType.Bool)]static extern bool HidD_FreePreparsedData(IntPtr data);
        [DllImport("hid.dll")]static extern int HidP_GetCaps(IntPtr data,[Out]byte[] caps);
        public static List<string> GamepadPaths(ushort vendor,ushort product){
            var result=new List<string>();Guid guid;HidD_GetHidGuid(out guid);IntPtr set=SetupDiGetClassDevsW(ref guid,IntPtr.Zero,IntPtr.Zero,0x12);
            if(set==new IntPtr(-1))return result;
            try{for(uint i=0;;i++){
                var data=new Interface{Size=(uint)Marshal.SizeOf(typeof(Interface))};if(!SetupDiEnumDeviceInterfaces(set,IntPtr.Zero,ref guid,i,ref data))break;
                uint required;SetupDiGetDeviceInterfaceDetailW(set,ref data,IntPtr.Zero,0,out required,IntPtr.Zero);if(required<8||required>65536)continue;
                IntPtr buffer=Marshal.AllocHGlobal((int)required);string path;
                try{Marshal.WriteInt32(buffer,IntPtr.Size==8?8:6);if(!SetupDiGetDeviceInterfaceDetailW(set,ref data,buffer,required,out required,IntPtr.Zero))continue;path=Marshal.PtrToStringUni(IntPtr.Add(buffer,4));}finally{Marshal.FreeHGlobal(buffer);}
                if(path.IndexOf("VID_"+vendor.ToString("X4")+"&PID_"+product.ToString("X4"),StringComparison.OrdinalIgnoreCase)<0)continue;
                using(var handle=CreateFileW(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)){
                    if(handle.IsInvalid)continue;IntPtr preparsed;if(!HidD_GetPreparsedData(handle,out preparsed))continue;
                    try{byte[] caps=new byte[64];if(HidP_GetCaps(preparsed,caps)==0x110000){ushort usage=BitConverter.ToUInt16(caps,0),page=BitConverter.ToUInt16(caps,2);if(page==1&&(usage==4||usage==5))result.Add(path);}}
                    finally{HidD_FreePreparsedData(preparsed);}
                }
            }}finally{SetupDiDestroyDeviceInfoList(set);}return result;
        }
    }
}
