using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EvoBridge {
    public static class DeviceTopology {
        [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)]static extern uint CM_Locate_DevNodeW(out uint node,string id,uint flags);
        [DllImport("cfgmgr32.dll")]static extern uint CM_Get_Parent(out uint parent,uint node,uint flags);
        [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)]static extern uint CM_Get_Device_IDW(uint node,StringBuilder id,uint length,uint flags);
        [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)]static extern uint CM_Get_DevNode_Registry_PropertyW(uint node,uint property,out uint kind,byte[] data,ref uint bytes,uint flags);
        static string Property(uint node,uint property){byte[] data=new byte[2048];uint size=(uint)data.Length,kind;if(CM_Get_DevNode_Registry_PropertyW(node,property,out kind,data,ref size,0)!=0)return "";return Encoding.Unicode.GetString(data,0,(int)size).TrimEnd('\0');}
        static string Id(uint node){var text=new StringBuilder(512);return CM_Get_Device_IDW(node,text,512,0)==0?text.ToString():"";}
        public static string InstanceFromPath(string path){
            if(String.IsNullOrEmpty(path)||!path.StartsWith(@"\\?\",StringComparison.Ordinal))return null;
            string[] parts=path.Substring(4).Split('#');if(parts.Length<4)return null;
            if(!parts[0].Equals("HID",StringComparison.OrdinalIgnoreCase)&&!parts[0].Equals("USB",StringComparison.OrdinalIgnoreCase))return null;
            return parts[0]+"\\"+parts[1]+"\\"+parts[2];
        }
        public static bool SafeInstance(string id){
            return !String.IsNullOrEmpty(id)&&id.Length<512&&id.IndexOf('"')<0&&id.IndexOf('\r')<0&&id.IndexOf('\n')<0&&id.IndexOf('*')<0&&id.IndexOf('?')<0&&(id.StartsWith("HID\\",StringComparison.OrdinalIgnoreCase)||id.StartsWith("USB\\",StringComparison.OrdinalIgnoreCase));
        }
        public static void Resolve(ControllerDevice device){
            device.HideInstances=new string[0];device.HidingProblem=null;
            string instance=InstanceFromPath(device.Path);device.InstanceId=instance;
            if(instance==null){device.HidingProblem="Windows device path unavailable; automatic hiding cannot be verified for this input backend.";return;}
            uint node;if(CM_Locate_DevNodeW(out node,instance,0)!=0){device.HidingProblem="Controller device path is no longer present. Refresh detection.";return;}
            string leafClass=Property(node,8); // CM_DRP_CLASS, not a wildcard search.
            if(!leafClass.Equals("HIDClass",StringComparison.OrdinalIgnoreCase)&&!leafClass.Equals("XnaComposite",StringComparison.OrdinalIgnoreCase)&&!leafClass.Equals("XboxComposite",StringComparison.OrdinalIgnoreCase)){
                device.HidingProblem="Input path is not an identified controller node; automatic hiding is disabled.";return;
            }
            var hides=new List<string>();if(SafeInstance(instance))hides.Add(instance);
            for(int depth=0;depth<18;depth++){
                string service=Property(node,5),kind=Property(node,8),id=Id(node);
                if(service.StartsWith("usbip",StringComparison.OrdinalIgnoreCase)){device.Virtual=true;device.HideInstances=new string[0];return;}
                // Only XInput controller nodes need their USB transport node hidden.
                // Never hide a USB hub, Bluetooth adapter, or a generic composite parent.
                if((kind.Equals("XnaComposite",StringComparison.OrdinalIgnoreCase)||kind.Equals("XboxComposite",StringComparison.OrdinalIgnoreCase))&&SafeInstance(id)){
                    string description=Property(node,1)+" "+Property(node,13);
                    if(description.IndexOf("receiver",StringComparison.OrdinalIgnoreCase)>=0||description.IndexOf("adapter",StringComparison.OrdinalIgnoreCase)>=0){device.HidingProblem="A shared receiver/adapter cannot be hidden automatically; no receiver-wide hiding will be applied.";}
                    else{if(device.TransportIdentity==null)device.TransportIdentity=id;if(!hides.Contains(id))hides.Add(id);}
                }
                uint parent;if(CM_Get_Parent(out parent,node,0)!=0)break;node=parent;
            }
            device.HideInstances=hides.ToArray();
            if(hides.Count==0)device.HidingProblem="No exact controller instance could be verified.";
        }
    }
}
