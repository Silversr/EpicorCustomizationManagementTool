using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicorCustomizationManagementTool.DataModel;
using System.Xml;

namespace EpicorCustomizationManagementTool.ELProject
{
    public interface IELProjectFactory
    {
        void Open(IEL EL, ECMTConfig Config);
    }

    internal class ELProjectFactory: IELProjectFactory
    {
        public void Open(IEL EL, ECMTConfig Config)
        {
            if (EL is GH) { new GHProjectManager(EL,Config).OpenOrCreateProject(); }
            else if (EL is CSG) { new CSGProjectManager(EL, Config).OpenOrCreateProject(); }
            else { throw new Exception("EL Type not Recognized"); }
        }
    }

    internal abstract class ELProjectManager
    {
        protected IEL el { get; }
        abstract protected string ProjectDllsBuiltToDir { get; set; }
        protected string ProjectReferenceDirAssemblies { get; }
        protected string ProjectReferenceDirBin { get; }
        abstract protected string CSProjPath { get; set; }
        abstract protected string CSProjUserPath { get; set; }
        abstract protected string SLNProjPath { get; set; }

        protected abstract string DeployCMD { get; }
        protected abstract string IISRecycleCMD { get; }

        protected bool AutoDeploy;
        abstract protected bool AutoIISRecycle { get; }
        protected bool AutoOpenProj;
        protected bool AutoCommit;

        abstract protected bool IsSLNProj { get; }

        protected Dictionary<VSProjectFileType, string> ProjectFileTypeDictionary = new Dictionary<VSProjectFileType, string>()
        {
            { VSProjectFileType.Solution,".sln"},
            { VSProjectFileType.CSharpProject,".csproj"},
            { VSProjectFileType.CSharpProjectUser,".csproj.user"},
            { VSProjectFileType.CSharpSource,".cs"},
            { VSProjectFileType.Text,".txt"},
            { VSProjectFileType.Config,".xml"},
            { VSProjectFileType.BPMTemplate,".xml"}
        };

        internal ELProjectManager(IEL EL, ECMTConfig Config)
        {
            el = EL;

            //Reference dir
            this.ProjectReferenceDirAssemblies = Config.EpicorAppServerRootDir + Config.ELConfig.ELEpicorAppServerAssembliesDir;
            this.ProjectReferenceDirBin = Config.EpicorAppServerRootDir + Config.ELConfig.ELEpicorAppServerBinDir;

            //Auto Flag
            this.AutoOpenProj = Config.AutoOpen && Config.ELConfig.ELAutoOpen;
            this.AutoDeploy = Config.AutoDeploy && Config.ELConfig.ELAutoDeploy; 
            //this.AutoIISRecycle = Config.AutoIISRecycle && Config.ELConfig.ELAutoIISRecycle;
            this.AutoCommit = Config.AutoCommit && Config.ELConfig.ELAutoCommit; //For future upgrade, not in use for now


        }
        internal void OpenOrCreateProject()
        {
            if (ProjectExists())
            {
                this.Open();
            }
            else
            {
                this.Create();
            }
        }
        internal virtual void Open()
        {
            //Prepare .csproj
            this.PrepareCSProj();

            //Prepare .csproj.user
            this.PrepareCSProjUser();

            //Open .sln OR .csproj in VS
            if (this.AutoOpenProj && this.IsSLNProj == true) { this.OpenSLN(); }
            else if (this.AutoOpenProj && this.IsSLNProj == false) { this.OpenCSProj(); }
        }
        internal abstract void Create();
        protected void PrepareCSProj()
        {
            //load csproj 
            var proj = new System.Xml.XmlDocument();
            proj.Load(this.CSProjPath);

            //create XML NameSpace
            var strNamespace = proj.DocumentElement.NamespaceURI;
            var nsManager = new XmlNamespaceManager(proj.NameTable);
            string prefix = el.GetType().ToString() + "Project";
            nsManager.AddNamespace(prefix, strNamespace);

            //get all references
            var ReferenceParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":Reference", nsManager).ParentNode;
            foreach(var reference in ReferenceParent.ChildNodes)
            {
                //update references path
                foreach (var referenceChildElement in (reference as XmlElement).ChildNodes)
                {
                    if ((referenceChildElement as XmlElement).Name.ToLower() == "hintpath")
                    {
                        var hintPath = (referenceChildElement as XmlElement).InnerText;
                        var dllName = hintPath.Split('\\').LastOrDefault();
                        //Find reference in assembilies folder
                        if (true == System.IO.File.Exists(this.ProjectReferenceDirAssemblies + dllName))
                        {
                            (referenceChildElement as XmlElement).InnerText = this.ProjectReferenceDirAssemblies + dllName;
                        }
                        else if (true == System.IO.File.Exists(this.ProjectReferenceDirBin + dllName))
                        {
                            (referenceChildElement as XmlElement).InnerText = this.ProjectReferenceDirBin + dllName;
                        }
                    }
                }
            }

            //set up build to 
            if (proj.DocumentElement.SelectSingleNode("//" + prefix + ":PostBuildEvent", nsManager) != null)
            {
                var PostBuildEventParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":PostBuildEvent", nsManager).ParentNode;
                PostBuildEventParent.RemoveAll();
                var referenceNode = proj.CreateNode(XmlNodeType.Element, "PostBuildEvent", strNamespace);
                referenceNode.InnerText = string.Empty;
                if (this.AutoDeploy == true)
                {
                    referenceNode.InnerText += this.DeployCMD + Environment.NewLine;
                }
                if (this.AutoIISRecycle == true)
                {
                    referenceNode.InnerText += this.IISRecycleCMD + Environment.NewLine;
                }
                PostBuildEventParent.AppendChild(referenceNode);
            }

            //save csproj
            proj.Save(this.CSProjPath);

        }
        protected void PrepareCSProjUser()
        {
            //load csproj user
            var proj = new System.Xml.XmlDocument();
            proj.Load(this.CSProjUserPath);

            //create XML NameSpace
            var strNamespace = proj.DocumentElement.NamespaceURI;
            var nsManager = new XmlNamespaceManager(proj.NameTable);
            string prefix = el.GetType().ToString() + "Project";
            nsManager.AddNamespace(prefix, strNamespace);

            //remove all reference paths
            XmlNode ReferencePathParent;
            if (proj.DocumentElement.SelectSingleNode("//" + prefix + ":ReferencePath", nsManager) == null)
            {
                ReferencePathParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":ReferencePath", nsManager).ParentNode;
                ReferencePathParent.RemoveAll();
                var referenceNode = proj.CreateNode(XmlNodeType.Element, "ReferencePath", strNamespace);
                referenceNode.InnerText = this.ProjectReferenceDirAssemblies + ";" + this.ProjectReferenceDirBin;
                ReferencePathParent.AppendChild(referenceNode);
            }
            //save .csproj.user
            proj.Save(this.CSProjUserPath);

        }
        protected void OpenSLN()
        {
            //Open .sln
            System.Diagnostics.Process.Start(this.SLNProjPath);
        }
        protected void OpenCSProj()
        {
            //Open .sln
            System.Diagnostics.Process.Start(this.CSProjPath);
        }
        protected bool ProjectExists()
        {
            return System.IO.File.Exists(this.SLNProjPath) && System.IO.File.Exists(this.CSProjPath);
        }
    }

    sealed internal class GHProjectManager : ELProjectManager
    {
        protected override bool AutoIISRecycle { get; }
        protected override string CSProjPath { get; set; }
        protected override string CSProjUserPath { get; set; }
        protected override string SLNProjPath { get; set; }
        protected override bool IsSLNProj { get; }
        protected override string DeployCMD { get; }
        protected override string IISRecycleCMD { get; }
        protected override string ProjectDllsBuiltToDir { get; set; }
        internal GHProjectManager(IEL EL, ECMTConfig Config) : base(EL, Config)
        {
            //Project Build To Dir
            this.ProjectDllsBuiltToDir = Config.EpicorAppServerRootDir + Config.ELConfig.ELAutoDeployDir;
            if (false == System.IO.Directory.Exists(this.ProjectDllsBuiltToDir))
            {
                throw new Exception("Project DLLs Build Path Not Available.");
            }

            //AutoIISCycle
            this.AutoIISRecycle = Config.AutoIISRecycle && Config.ELConfig.ELAutoIISRecycle;

            //csproj and sln file path
            this.CSProjPath = Config.ECTargetRootDir + el.Folder + el.Folder + el.Name + this.ProjectFileTypeDictionary[VSProjectFileType.CSharpProject];
            this.CSProjUserPath = Config.ECTargetRootDir + el.Folder + el.Folder + el.Name + this.ProjectFileTypeDictionary[VSProjectFileType.CSharpProjectUser];
            this.SLNProjPath = Config.ECTargetRootDir + el.Folder + el.Name + this.ProjectFileTypeDictionary[VSProjectFileType.Solution];
            if (true == System.IO.File.Exists(this.SLNProjPath))
            {
                this.IsSLNProj = true;
            }else if (true == System.IO.File.Exists(this.CSProjPath))
            {
                this.IsSLNProj = false;
            }

            //Deploy CMD, Even in debug mode, .pdb files not copied to deployment folder
            if (Config.DebugMode == true)
            {
                this.DeployCMD = Config.DeployCMD + " " + this.ProjectDllsBuiltToDir;
            }
            else
            {
                this.DeployCMD = Config.DeployCMD + " " + this.ProjectDllsBuiltToDir;
            }
            if (this.AutoIISRecycle)
            {
                this.IISRecycleCMD = Config.ECTargetRootDir + Config.IISRecyclePath + " " + Config.EpicorAppServer;
            }
        }
        internal override void Create()
        {
            throw new NotImplementedException("Cannot Create new GelitaHelpers, GelitaHeplers must exist!");
        }


    }
    
    sealed internal class CSGProjectManager : ELProjectManager
    {
        protected override bool AutoIISRecycle { get { return false; } }//No IISCycle Allowed in CSG Project
        protected override string CSProjPath { get; set; }
        protected override string CSProjUserPath { get; set; }
        protected override string SLNProjPath { get; set; }
        protected override bool IsSLNProj { get { return true; } }
        protected override string DeployCMD { get; }
        protected override string IISRecycleCMD { get; }
        protected override string ProjectDllsBuiltToDir { get; set; }

        private string CSGProjDir { get; }
        private string CSGProjRootDir { get; }
        private string GenericServerProcessTemplate { get; }

        internal CSGProjectManager(IEL EL, ECMTConfig Config) : base(EL, Config)
        {
            //ProjcectDllsBuiltTo
            this.ProjectDllsBuiltToDir = Config.EpicorAppServerRootDir + Config.ELConfig.ELEpicorAppServerAssembliesDir;
            if (false == System.IO.Directory.Exists(this.ProjectDllsBuiltToDir))
            {
                throw new Exception("Project DLLs Build Path Not Available.");
            }
            //csproj and sln file path
            this.CSProjPath = Config.ECTargetRootDir + el.Folder + "Erp.Internal.CSG." + el.Name + this.ProjectFileTypeDictionary[VSProjectFileType.CSharpProject];
            this.CSProjUserPath = Config.ECTargetRootDir + el.Folder + "Erp.Internal.CSG." + el.Name + this.ProjectFileTypeDictionary[VSProjectFileType.CSharpProjectUser];
            this.SLNProjPath = Config.ECTargetRootDir + el.Folder + "Erp.Internal.CSG." + el.Name + this.ProjectFileTypeDictionary[VSProjectFileType.Solution];

            //Deploy CMD, Even in debug mode, .pdb files not copied to deployment folder
            if (Config.DebugMode == true)
            {
                this.DeployCMD = Config.DeployCMD + " " + this.ProjectDllsBuiltToDir;
            }
            else
            {
                this.DeployCMD = Config.DeployCMD + " " + this.ProjectDllsBuiltToDir;
            }
            this.IISRecycleCMD = string.Empty;

            //CSG Template Dir
            this.GenericServerProcessTemplate = Config.ELConfig.ELCSGGenericServerProcessTemplate;

            //CSG Project Dir (for both New or Current)
            this.CSGProjDir = Config.ECTargetRootDir + el.Folder;
            this.CSGProjRootDir = Config.ECTargetRootDir + CSG.DefaultFolder + el.Name + @"\";
        }
        internal override void Create()
        {
            this.CreateFromGenericServerProcessTemplate();
        }

        private void CreateFromGenericServerProcessTemplate()
        {
            //Check if GenericServerProcess Tempalte Exists
            if (false == System.IO.Directory.Exists(this.CSGProjRootDir.Replace(el.Name,this.GenericServerProcessTemplate)))
            {
                throw new Exception("Generic Server Process Template Not Exists, cannot create new CSG project from template.");
            }

            //Copy Template to New Project
            this.CopyFromTempate();

            //Prepare New CSG Project
            try
            {
                //Update Project (.csproj, .csproj.user, .sln) Name
                //Update Build Assembly Name 
                //Update Class Name
                this.UpdateDirsNames();
                this.UpdateFilesNames();
                this.UpdateFilesContents();

                //open proj
                this.Open();
            }
            catch(Exception ex)
            {
                //if failed to update names, delete incomplete project 
                this.DeleteIncompleteProject();
                throw ex;
            }
        }
        private void CopyFromTempate()
        {
            //Now Create all of the directories
            foreach (string dirPath in System.IO.Directory.GetDirectories(this.CSGProjRootDir.Replace(el.Name, this.GenericServerProcessTemplate), "*",
                System.IO.SearchOption.AllDirectories))
            {
                System.IO.Directory.CreateDirectory(dirPath.Replace(this.CSGProjRootDir.Replace(el.Name, this.GenericServerProcessTemplate), this.CSGProjRootDir));
            }
                
            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in System.IO.Directory.GetFiles(this.CSGProjRootDir.Replace(el.Name, this.GenericServerProcessTemplate), "*.*",
                System.IO.SearchOption.AllDirectories))
            {
                System.IO.File.Copy(newPath, newPath.Replace(this.CSGProjRootDir.Replace(el.Name, this.GenericServerProcessTemplate), this.CSGProjRootDir), true);
            }   
        }
        private void UpdateDirsNames()
        {
            List<string> dirsTemp = new List<string>();
            string dir = System.IO.Directory.GetDirectories(this.CSGProjRootDir, "*" + this.GenericServerProcessTemplate + "*", System.IO.SearchOption.AllDirectories).OrderBy(r => r.Length).FirstOrDefault();
            while(!string.IsNullOrEmpty(dir))
            {
                System.IO.Directory.Move(dir, dir.Replace(this.GenericServerProcessTemplate, el.Name));
                var dirs = System.IO.Directory.GetDirectories(this.CSGProjRootDir, "*" + this.GenericServerProcessTemplate + "*", System.IO.SearchOption.AllDirectories).OrderBy(r => r.Length);
                dir = dirs.FirstOrDefault();
            }
        }
        private void UpdateFilesNames()
        {
            foreach (var filePath in System.IO.Directory.GetFiles(this.CSGProjRootDir, "*" + this.GenericServerProcessTemplate + "*", System.IO.SearchOption.AllDirectories))
            {
                System.IO.File.Move(filePath,filePath.Replace(this.GenericServerProcessTemplate,el.Name));
            }
        }
        private void UpdateFilesContents()
        {
            foreach(var filePath in System.IO.Directory.GetFiles(this.CSGProjRootDir,"*"+ el.Name + "*", System.IO.SearchOption.AllDirectories))
            {
                var fileStr = System.IO.File.ReadAllText(filePath)
                                         .Replace(this.GenericServerProcessTemplate, el.Name);
                System.IO.File.WriteAllText(filePath, fileStr);
            }
        }
        private void DeleteIncompleteProject()
        {
            System.IO.Directory.Delete(this.CSGProjRootDir,true);
        }

    }
    

}
