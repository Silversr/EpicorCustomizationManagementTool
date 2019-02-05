using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using EpicorCustomizationManagementTool.DataModel;

namespace EpicorCustomizationManagementTool
{
    internal class CMDEntry
    {
        //ECs is to hold all EC (data model)
        private List<IEC> ECs = new List<IEC>();
        //ECFactory is to generate required EC
        private Lazy<IECFactory> ECFactory = new Lazy<IECFactory>(() => new ECFactory());
        //ECTargetFactory is to build required EC project/customization
        private Lazy<IECTargetFactory> ECTargetFactory = new Lazy<IECTargetFactory>(() => new ECTargetFactory());

        //private flags for CMD UI
        private bool ParseOK = false;
        private bool QuitCmdDetected = false;
        private bool HelpInfoRequired = false;
        private bool ShowOptionsCmdDetected = false;

        //Methods
        //Run method call ECTargetFactory.Build() method to build required EC project/customization
        public void Run()
        {
            CMDEntry.GenerateInfo("Current Config : " + ECMTConfigManager.Singleton.CurrentLoadedConfigFile);
            this.QuitCmdDetected = false;
            this.ShowOptionsCmdDetected = false;
            this.HelpInfoRequired = true;
            string cmd = String.Empty;
            do
            {

                //Help Info
                if (true == this.HelpInfoRequired) { GenerateHelpInfo(); this.HelpInfoRequired = false; }

                //Show ECMT Options Status
                if (true == this.Parse(cmd) && this.ShowOptionsCmdDetected == true) { GenerateECMTOptions(); this.ShowOptionsCmdDetected = false; }

                //Read Cmd and GenerateInfo
                cmd = Console.ReadLine();

                if (false == this.Parse(cmd) && !string.IsNullOrEmpty(cmd)) { GenerateParseFailedInfo(); }
                else
                {
                    //Build Project/Customization
                    foreach (var ec in this.ECs)
                    {
                        try
                        {
                            if (this.ECs.Count() > 1) { GenerateInfo(ec.Name + " - ECType: " + ec.GetType().ToString()); }
                            ECTargetFactory.Value.Build(ec);
                        }
                        catch (Exception ex) { GenerateInfo("Exception: " + ex.Message); }
                    }
                    if (this.ECs.Count() > 1) { GenerateInfo("Done!"); }
                }
                //commit to bitbucket - powerShell ps1

            }
            while (!QuitCmdDetected);
        }

        //Parse method call ECFactory to generate ECs (stored in ECs properties)
        private bool Parse(string cmd)
        {
            this.ECs.Clear();
            switch (cmd.ToLower().Split(' ')[0])
            {
                case "rebuildallbpms":
                    this.ParseOK = checkFormatRebuildAllBpms(cmd);
                    if (this.ParseOK)
                    {
                        this.ECs = this.ECFactory.Value.GetAllAvailableECs();
                        foreach (var ec in ECs) { this.ECFactory.Value.GenerateEC(ec.Name, ec.GetType()); }
                    }
                    break;
                case "quit":
                    this.ParseOK = checkQuit(cmd);
                    this.QuitCmdDetected = true;
                    break;
                case "help":
                    this.ParseOK = true;
                    this.HelpInfoRequired = true;
                    break;
                case "options":
                    this.ParseOK = true;
                    this.ShowOptionsCmdDetected = true;
                    break;
                case "bo":
                    this.ParseOK = checkBO(cmd);
                    if (this.ParseOK) { this.ECs.Add(this.ECFactory.Value.GenerateEC(cmd.Split(' ')[1], typeof(BO))); }
                    break;
                case "dt":
                    this.ParseOK = checkDT(cmd);
                    if (this.ParseOK) { this.ECs.Add(this.ECFactory.Value.GenerateEC(cmd.Split(' ')[1], typeof(DT))); }
                    break;
                case "ubaq":
                    this.ParseOK = checkUbaq(cmd);
                    if (this.ParseOK) { this.ECs.Add(this.ECFactory.Value.GenerateEC(cmd.Split(' ')[1], typeof(Ubaq))); }
                    break;
                case "gh":
                    this.ParseOK = checkGH(cmd);
                    if (this.ParseOK) { this.ECs.Add(this.ECFactory.Value.GenerateEC("GelitaHelpers", typeof(GH))); }
                    break;
                case "csg":
                    this.ParseOK = checkCSG(cmd);
                    if (this.ParseOK) { this.ECs.Add(this.ECFactory.Value.GenerateEC(cmd.Split(' ')[1], typeof(CSG))); }
                    break;
                case "form":
                    this.ParseOK = checkForm(cmd);
                    if (this.ParseOK) { this.ECs.Add(this.ECFactory.Value.GenerateEC(cmd.Split(' ')[1], typeof(FC))); }
                    break;
                case "ssrs":
                    this.ParseOK = checkSSRS(cmd);
                    if (this.ParseOK) { this.ECs.Add(this.ECFactory.Value.GenerateEC(cmd.Split(' ')[1], typeof(SSRS))); }
                    break;
                default:
                    this.ParseOK = false;
                    break;
            }

            return ParseOK;
        }

        //static fields and funcs
        #region CMDCheckMethods
        private static bool checkFormatRebuildAllBpms(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = true;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAry[0] == "rebuildallbpms")
            {
                for (int i = 1; i < cmdAryCnt; i++)
                {
                    if (!string.IsNullOrEmpty(cmdAry[i])) { formatCorrect = false; }
                }
            }
            else
            {
                formatCorrect = false;
            }
            return formatCorrect;
        }
        private static bool checkQuit(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = true;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAry[0] == "SSRS")
            {
                for (int i = 1; i < cmdAryCnt; i++)
                {
                    if (!string.IsNullOrEmpty(cmdAry[i])) { formatCorrect = false; }
                }
            }
            else
            {
                formatCorrect = false;
            }
            return formatCorrect;
        }
        private static bool checkBO(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = false;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAryCnt >= 2 && cmdAry[0] == "bo" && cmdAry[1] != "")
            {
                var BONameMethodNameAry = cmdAry[1].Split('.');
                int BONameMethodNameAryCnt = BONameMethodNameAry.Count();
                if (BONameMethodNameAryCnt == 2)
                {
                    formatCorrect = true;
                }

            }
            return formatCorrect;
        }
        private static bool checkDT(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = false;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAryCnt >= 2 && cmdAry[0] == "dt" && cmdAry[1] != "")
            {
                formatCorrect = true;
            }
            return formatCorrect;
        }
        private static bool checkUbaq(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = false;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAryCnt >= 2 && cmdAry[0] == "ubaq" && cmdAry[1] != "")
            {
                var UbaqIDMethodNameAry = cmdAry[1].Split('.');
                int UbaqIDMethodNameAryCnt = UbaqIDMethodNameAry.Count();
                if (UbaqIDMethodNameAryCnt == 2
                    &&
                   (UbaqIDMethodNameAry[1] == "update" || UbaqIDMethodNameAry[1] == "getnew")
                   )
                {
                    formatCorrect = true;
                }
            }
            return formatCorrect;
        }
        private static bool checkGH(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = true;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAry[0] == "gh")
            {
                for (int i = 1; i < cmdAryCnt; i++)
                {
                    if (!string.IsNullOrEmpty(cmdAry[i])) { formatCorrect = false; }
                }
            }
            else
            {
                formatCorrect = false;
            }
            return formatCorrect;
        }
        private static bool checkCSG(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = false;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAryCnt >= 2 && cmdAry[0] == "csg" && cmdAry[1] != "")
            {
                formatCorrect = true;
            }
            return formatCorrect;
        }
        private static bool checkForm(string cmd)
        {
            return true;
        }
        private static bool checkSSRS(string cmd)
        {
            cmd = cmd.ToLower();
            bool formatCorrect = false;
            string[] cmdAry = cmd.Split(' ');
            int cmdAryCnt = cmdAry.Count();
            if (cmdAryCnt >= 2 && cmdAry[0] == "ssrs" && cmdAry[1] != "")
            {
                formatCorrect = true;
            }
            return formatCorrect;
        }
        #endregion

        #region UserPromtInfo
        private static readonly string parseFailedInfo = "Command Not Recognized";

        /// <summary>
        /// BPMProjectBuilder cmd user guide:
        /// 1. build a Method Directive
        ///    Format: BO [BusinessObject].[MethodName]
        ///    e.g. BO JobEntry.Update
        ///    
        /// 2. build a Data Directive
        ///    Format: DT [TableName]
        /// e.g. DT JobHead
        /// 
        /// 3. build an Updatable BAQ       
        ///     Format: Ubaq [Updatable BAQ ID]
        /// e.g. Ubaq 500_JobInspectionsCreate
        /// 
        /// 4. rebuild all projects
        ///     Format: build all
        ///     
        /// 5. exit the program
        ///     Format: quit
        ///     
        /// 6. help information
        ///     Format: help
        /// </summary>
        /// 
        private static void GenerateHelpInfo()
        {
            string helpInfo = System.IO.File.ReadAllText(ECMTConfigManager.Singleton.ECMTConfig.UserGuideInfo);
            Console.WriteLine(helpInfo);
        }
        private static void GenerateParseFailedInfo()
        {
            Console.WriteLine(parseFailedInfo);
        }
        private static void GenerateECMTOptions()
        {
            throw new NotImplementedException();
        }
        private static void GenerateInfo(string info)
        {
            Console.WriteLine(info);
        }
        #endregion
    }
}
