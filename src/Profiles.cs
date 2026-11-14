using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace EvoBridge {
    public sealed class ControllerProfile {
        public string Key,Name,Guid,TransportIdentity,Mapping;
        public int Deadzone=8;
        public bool Rumble=true,HidePhysical=true;
    }
    public sealed class ProfileData {public int Schema=1;public string LastSelected;public List<ControllerProfile> Controllers=new List<ControllerProfile>();}
    public static class Profiles {
        internal static string TestPath;
        public static string PathName {get{return TestPath??Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"profiles.json");}}
        public static string Key(ControllerDevice d){
            // GUID alone identifies a model, not a particular pad/connection.
            string identity=d.Vendor.ToString("X4")+":"+d.Product.ToString("X4")+"|"+(!String.IsNullOrEmpty(d.Serial)?"serial:"+d.Serial:"path:"+(d.TransportIdentity??d.InstanceId??d.Path??"").ToUpperInvariant());
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-","").ToLowerInvariant();
        }
        public static ProfileData Load(){
            if(!File.Exists(PathName))return new ProfileData();
            var data=new JavaScriptSerializer().Deserialize<ProfileData>(File.ReadAllText(PathName));
            if(data==null||data.Schema!=1||data.Controllers==null)throw new IOException("Unsupported or invalid profiles.json. Your saved file has been preserved.");
            foreach(var p in data.Controllers)if(p==null||p.Key==null||p.Deadzone<0||p.Deadzone>30)throw new IOException("Invalid controller profile. Your saved file has been preserved.");
            return data;
        }
        public static ControllerProfile For(ControllerDevice d){return Load().Controllers.FirstOrDefault(p=>p.Key==d.ProfileKey)??new ControllerProfile{Key=d.ProfileKey,Name=d.Name,Guid=d.Guid,TransportIdentity=d.TransportIdentity??d.InstanceId??d.Path};}
        public static void Save(ControllerProfile profile){
            var data=Load();data.Controllers.RemoveAll(p=>p.Key==profile.Key);data.Controllers.Add(profile);data.LastSelected=profile.Key;
            string temp=PathName+".pending";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(data));
            if(File.Exists(PathName))File.Replace(temp,PathName,PathName+".backup");else File.Move(temp,PathName);
        }
    }
}
