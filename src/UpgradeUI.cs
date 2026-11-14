using System;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace EvoBridge {
    static class Program {
        [STAThread]static int Main(string[] args){
            if(args.Length==2&&args[0]=="--self-test")return SelfTests.Run(args[1]);
            if(args.Length==2&&args[0]=="--upgrade-self-test")return UpgradeTests.Software(args[1]);
            if(args.Length==2&&args[0]=="--inventory"){try{using(var s=new SdlInput()){var found=s.Detect();for(int i=0;found.Count==0&&i<50;i++){Thread.Sleep(40);found=s.Detect();}File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(found));File.WriteAllText(args[1]+".diagnostic.txt",s.Diagnostic);}return 0;}catch(Exception ex){File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{error=ex.Message}));return 1;}}
            using(var instance=new Mutex(false,"Local\\EvoBridge.Mapper")){
                bool acquired;try{acquired=instance.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
                if(!acquired){MessageBox.Show("Close the other EvoBridge version before starting this one. Your running session has been left untouched.","EvoBridge");return 2;}
                try{
                    InputHiding.Restore();
                    if(args.Length==1&&args[0]=="--restore-input")return 0;
                    if(args.Length==2&&args[0]=="--upgrade-test")return UpgradeTests.Run(args[1]);
                    if(args.Length==2&&args[0]=="--controller-test")return UpgradeTests.Run(args[1],100);
                    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm());return 0;
                }catch(Exception ex){MessageBox.Show(ex.Message,"EvoBridge Next");return 1;}
                finally{instance.ReleaseMutex();}
            }
        }
    }
}
