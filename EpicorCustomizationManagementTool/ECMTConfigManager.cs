using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EpicorCustomizationManagementTool
{
    public class ECMTConfigManager
    {
        //Singleton Construct
        private static readonly object mutex = new object();
        private static ECMTConfigManager singleton;
        public static ECMTConfigManager Singleton
        {
            get
            {
                lock (mutex)
                {
                    if (singleton == null)
                    {
                        singleton = new ECMTConfigManager();
                    }
                    return singleton;
                }
            }
        }
        //Constructor
        private ECMTConfigManager()
        {
            this.CurrentLoadedConfigFile = @"..\..\ECMT.Config";// "ECMT_" + Environment + ".Config";
            if ((!string.IsNullOrEmpty(Startup.EpicorEnvironment)) && (!string.IsNullOrEmpty(Startup.UserEnvironment)))
            {
                CurrentLoadedConfigFile = @"..\..\ECMT_" + Startup.EpicorEnvironment + "_" + Startup.UserEnvironment + ".Config";
            }
            this.CurrentLoadedConfigFile = CurrentLoadedConfigFile;
            //Check ECMTConfig Available; if Available, load ECMT.Config; if not Available, create new ECMT.Config
            if (true == System.IO.File.Exists(CurrentLoadedConfigFile))
            {
                //create xml serializer
                System.Xml.Serialization.XmlSerializer deserializer = new System.Xml.Serialization.XmlSerializer(typeof(ECMTConfig));
                //find xml file
                System.IO.StreamReader ECMTConfigFileHandler = new System.IO.StreamReader(CurrentLoadedConfigFile);
                //serialize obj to xml file
                this.ECMTConfig = (ECMTConfig)deserializer.Deserialize(ECMTConfigFileHandler);
                //close file 
                ECMTConfigFileHandler.Close();
            }
            else
            {
                //create ECMTConfig object
                this.ECMTConfig = new ECMTConfig()
                {
                    UserGuideInfo = @"../../Resources/UserGuideInfo.txt",
                    EpicorAppServerRootDir = @"E:\wwwepicor\EpicorERP" + (Startup.EpicorEnvironment.ToLower() == "live" ? "" : Startup.EpicorEnvironment) + @"\Server\",
                    EpicorAppServer = @"EpicorERP" + (Startup.EpicorEnvironment.ToLower() == "live" ? "" : Startup.EpicorEnvironment),
                    EpicorDatabase = @"EpicorERP" + (Startup.EpicorEnvironment.ToLower() == "live" ? "" : Startup.EpicorEnvironment),
                    EpicorClientRootDir = @"E:\Epicor\ERP10\LocalClients\EpicorERP" + (Startup.EpicorEnvironment.ToLower() == "live" ? "" : Startup.EpicorEnvironment) + @"\",
                    ECTargetRootDir = @"C:\Users\" + Startup.UserEnvironment + @"\Documents\Epicor102100\",
                    IISRecyclePath = @"IISRecycle\IISRecycle\bin\Release\IISRecycle.exe",

                    AutoOpen = true,
                    AutoCommit = false,
                    AutoDeploy = true,
                    AutoIISRecycle = true,
                    DebugMode = true,


                    DeployCMD = "xcopy /f /y \"$(TargetPath)\"",
                    DeployCMD_DEBUG = "xcopy / f / y \"$(TargetDir)$(TargetName).pdb\"",

                    BPMConfig = new BPMConfig()
                    {
                        BPMSourceCodeDir = @"BPM\Sources\",
                        BPMTargetProjectDir = @"BPM\",
                        BPMProjectReferenceDirs = new List<string>() { @"Assemblies\", @"Bin\" },
                        BPMTemplateDir = @"../../Resources/BPMTemplate.xml",
                        BPMTemplateUserDir = @"../../Resources/BPMTemplate.User.xml",
                        BPMProjectDebugRootDir = @"Customization\",

                        BPMAutoCommit = false,
                        BPMAutoDeploy = false,
                        BPMAutoIISRecycle = true,
                        BPMAutoOpen = true
                    },
                    UIConfig = new UIConfig()
                    {

                    },


                    ELConfig = new ELConfig()
                    {
                        ELAutoCommit = false,
                        ELAutoDeploy = true,
                        ELAutoIISRecycle = true,
                        ELAutoOpen = true,
                        ELAutoDeployDir = @"Customization\Externals\",
                        ELEpicorAppServerAssembliesDir = @"Assemblies\",
                        ELEpicorAppServerBinDir = @"Bin\",
                        ELCSGGenericServerProcessTemplate = @"GenericTemplate"
                    },
                    RPTConfig = new RPTConfig()
                    {
                        SSRSReportingServerHost = @"srvikepic10",
                        SSRSWebPortal = @"http://srvikepic10:80/reports",
                        RPTTargetProjectDir = @"SSRS\",
                        SSRSProjectTemplateDir = @"../../Resources/SSRSReportProjectTemplate.xml",
                        SSRSSharedDataSourceTemplateDir = @"../../Resources/SSRSReportSharedDataSourceTemplate.xml",
                        SSRSEpicorReportDatabase = @"EpicorERP" + (Startup.EpicorEnvironment.ToLower() == "live" ? "" : Startup.EpicorEnvironment) + "Reports",
                        RPTAutoDeploy = true,
                        RPTAutoOpen = true

                    },
                };
                //create xml serializer
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ECMTConfig));
                //create xml file
                System.IO.FileStream ECMTConfigFileHandler = System.IO.File.Open(CurrentLoadedConfigFile, System.IO.FileMode.Create);
                //serialize obj to xml file
                serializer.Serialize(ECMTConfigFileHandler, this.ECMTConfig);
                //close file 
                ECMTConfigFileHandler.Close();
            }
        }
        //Property
        public ECMTConfig ECMTConfig { get; } //auto property
        public string CurrentLoadedConfigFile { get; }
    }

    [Serializable]
    public class ECMTConfig
    {
        public string UserGuideInfo { get; set; }
        public string EpicorAppServerRootDir { get; set; }
        public string EpicorAppServer { get; set; }
        public string EpicorDatabase { get; set; }
        public string EpicorClientRootDir { get; set; }
        public string ECTargetRootDir { get; set; }

        public bool AutoOpen { get; set; }
        public bool AutoCommit { get; set; }
        public bool AutoDeploy { get; set; }
        public bool AutoIISRecycle { get; set; }
        public string IISRecyclePath { get; set; }
        public bool DebugMode { get; set; }
        public string DeployCMD { get; set; }
        public string DeployCMD_DEBUG { get; set; }

        public BPMConfig BPMConfig { get; set; }
        public ELConfig ELConfig { get; set; }
        public UIConfig UIConfig { get; set; }
        public RPTConfig RPTConfig { get; set; }

    }
    [Serializable]
    public class BPMConfig
    {
        public string BPMSourceCodeDir { get; set; }
        public string BPMTargetProjectDir { get; set; }
        public string BPMTemplateDir { get; set; }
        public string BPMTemplateUserDir { get; set; }
        public List<string> BPMProjectReferenceDirs { get; set; }
        public string BPMProjectDebugRootDir { get; set; }

        public bool BPMAutoOpen { get; set; }
        public bool BPMAutoCommit { get; set; }
        public bool BPMAutoDeploy { get; set; }
        public bool BPMAutoIISRecycle { get; set; }
    }
    [Serializable]
    public class ELConfig
    {
        public bool ELAutoOpen { get; set; }
        public bool ELAutoCommit { get; set; }
        public bool ELAutoDeploy { get; set; }
        public bool ELAutoIISRecycle { get; set; }

        public string ELAutoDeployDir { get; set; }
        public string ELEpicorAppServerAssembliesDir { get; set; }
        public string ELEpicorAppServerBinDir { get; set; }

        public string ELCSGGenericServerProcessTemplate { get; set; }
    }
    [Serializable]
    public class UIConfig
    {

    }
    [Serializable]
    public class RPTConfig
    {
        public string RPTTargetProjectDir { get; set; }

        public string SSRSReportingServerHost { get; set; }
        public string SSRSWebPortal { get; set; }
        public string SSRSProjectTemplateDir { get; set; }
        public string SSRSSharedDataSourceTemplateDir { get; set; }
        public string SSRSEpicorReportDatabase { get; set; }

        public bool RPTAutoOpen { get; set; }
        public bool RPTAutoDeploy { get; set; }
        
    }

}
