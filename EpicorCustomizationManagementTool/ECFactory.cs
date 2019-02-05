using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicorCustomizationManagementTool.DataModel;

namespace EpicorCustomizationManagementTool
{
    public interface IECFactory
    {
        IEC GenerateEC(string name, System.Type type);
        List<IEC> GetAllAvailableECs();
    }

    internal class ECFactory : IECFactory
    {
        //Properties
        private IEC parsedEC;
        //Methods
        public IEC GenerateEC(string name, System.Type type)
        {
            if (type == typeof(BO))
            {
                this.parsedEC = new BO(name.Split('.')[0], name.Split('.')[1]);
            }
            else if (type == typeof(DT))
            {
                this.parsedEC = new DT(name);
            }
            else if (type == typeof(Ubaq))
            {
                this.parsedEC = new Ubaq(name.Split('.')[0], name.Split('.')[1]);
            }
            else if (type == typeof(FC))
            {
                this.parsedEC = new FC(name);
            }
            else if (type == typeof(DBD))
            {
                throw new NotImplementedException();
            }
            else if (type == typeof(CSG))
            {
                this.parsedEC = new CSG(name);
            }
            else if (type == typeof(GH))
            {
                this.parsedEC = new GH();
            }
            else if (type == typeof(DT))
            {
                throw new NotImplementedException();

            }
            else if (type == typeof(Crystal))
            {
                throw new NotImplementedException();
            }
            else if (type == typeof(SSRS))
            {
                this.parsedEC = new SSRS(name);
            }
            else if (type == typeof(BT))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new Exception("EpicorCustomization Not Recognized");
            }

            return parsedEC;
        }

        public List<IEC> GetAllAvailableECs()
        {
            ECMTConfigManager.Singleton.ECMTConfig.AutoOpen = false;
            ECMTConfigManager.Singleton.ECMTConfig.AutoCommit = false;
            List<IEC> ecLst = new List<IEC>();
            //BPMs
            var BPMs = this.GetAllAvailableBPMs();
            foreach(var bpm in BPMs)
            {
                ecLst.Add((bpm as IEC));
            }
            //EL
            //UI
            //RPT
            return ecLst;
        
        }

        private List<IBPM> GetAllAvailableBPMs()
        {
            List<IBPM> bpmLst = new List<IBPM>();

            //GetAllBO
            List<string> BOsDirLst = System.IO.Directory.GetDirectories(ECMTConfigManager.Singleton.ECMTConfig.EpicorAppServerRootDir + ECMTConfigManager.Singleton.ECMTConfig.BPMConfig.BPMSourceCodeDir + BO.DefaultFolder)
                                        .ToList();
            foreach (var BODir in BOsDirLst) { bpmLst.Add(this.GenerateEC(BODir.Split('\\').LastOrDefault(), typeof(BO)) as IBPM); }

            //GetAllDT
            List<string> DTsDirLst = System.IO.Directory.GetDirectories(ECMTConfigManager.Singleton.ECMTConfig.EpicorAppServerRootDir + ECMTConfigManager.Singleton.ECMTConfig.BPMConfig.BPMSourceCodeDir + DT.DefaultFolder)
                            .ToList();
            foreach (var DTDir in DTsDirLst) { bpmLst.Add(this.GenerateEC(DTDir.Split('.')[1], typeof(DT)) as IBPM); }

            //GetAllUbaq
            List<string> UbaqsDirLst = System.IO.Directory.GetDirectories(ECMTConfigManager.Singleton.ECMTConfig.EpicorAppServerRootDir + ECMTConfigManager.Singleton.ECMTConfig.BPMConfig.BPMSourceCodeDir + Ubaq.DefaultFolder)
                .ToList();
            foreach (var UbaqDir in UbaqsDirLst)
            {
                if (UbaqDir.Split('\\').LastOrDefault().StartsWith("_") && !UbaqDir.Split('\\').LastOrDefault().StartsWith("__z"))
                {
                    bpmLst.Add(this.GenerateEC(UbaqDir.Split('\\').LastOrDefault().Replace("__", "").Replace("_500_", "").Replace("_501_", ""), typeof(Ubaq)) as IBPM);
                }

            }

            return bpmLst;
        }
    }
}
