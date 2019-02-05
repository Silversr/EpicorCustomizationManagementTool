using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using EpicorCustomizationManagementTool.DataModel;

namespace EpicorCustomizationManagementTool.BPMProject
{
    public interface IBPMProjectFactory
    {
        void BuildProject(IBPM BPM, ECMTConfig Config);
    }

    internal class BPMProjectFactory : IBPMProjectFactory
    {
        public void BuildProject(IBPM BPM, ECMTConfig Config)
        {
            if (BPM is BO) { new BOProject(BPM,Config).Build(); }
            else if (BPM is DT) { new DTProject(BPM, Config).Build(); }
            else if (BPM is Ubaq) { new UbaqProject(BPM, Config).Build(); }
            else { throw new Exception("BPMType Not Recognized");}
        }
    }

    internal abstract class BPMProject
    {
        protected IBPM bpm { get; }
        protected string TargetProject_CSPROJ_Dir;
        protected string TargetProject_SourceCode_Dir;
        protected string SourceCode_SourcesFolder_Dir;
        protected string ProjTemplatePath;
        protected string ProjUserTemplatePath;
        protected string ProjReferenceDirs = string.Empty;
        protected string GelitaHelpersReferenceDir = string.Empty;
        protected string CSProjPath;
        protected string CSProjUserPath;
        protected string PostBuildEvent_IISRecycleForCurrentEpicorServer;
        protected string ProjectDebugBuiltToDir;

        protected bool AutoDeploy;
        protected bool AutoIISRecycle;
        protected bool AutoOpenProj;
        protected bool AutoCommit;
        

        public BPMProject(IBPM BPM, ECMTConfig Config)
        {
            bpm = BPM;
            //Source
            this.SourceCode_SourcesFolder_Dir = Config.EpicorAppServerRootDir + Config.BPMConfig.BPMSourceCodeDir + bpm.Folder;//..\BPM\Sources\BO\

            //Target
            this.TargetProject_CSPROJ_Dir = Config.ECTargetRootDir + Config.BPMConfig.BPMTargetProjectDir + bpm.Folder + bpm.Name + @"\"; //..\BPM\BO\ABCCode.Update\
            this.TargetProject_SourceCode_Dir = Config.ECTargetRootDir + Config.BPMConfig.BPMTargetProjectDir + bpm.Folder + bpm.Name + @"\" + bpm.Name + @"\"; //..\BPM\BO\ABCCode.Update\ABCCode.Update\

            //Proj Template and Proj User Template
            this.ProjTemplatePath = Config.BPMConfig.BPMTemplateDir;
            this.ProjUserTemplatePath = Config.BPMConfig.BPMTemplateUserDir;

            //.csproj and .csproj.user
            this.CSProjPath = this.TargetProject_CSPROJ_Dir + bpm.Name + this.ProjectFileTypeDictionary[VSProjectFileType.CSharpProject]; //..\Bpm\BO\ABCCode.Update\ABCCode.Update.csproj
            this.CSProjUserPath = this.TargetProject_CSPROJ_Dir + bpm.Name + this.ProjectFileTypeDictionary[VSProjectFileType.CSharpProjectUser];//..\Bpm\BO\ABCCode.Update\ABCCode.Update.csproj.user

            //Reference 
            foreach (var referenceDir in Config.BPMConfig.BPMProjectReferenceDirs)
            {
                ProjReferenceDirs += ProjReferenceDirs == string.Empty ? Config.EpicorAppServerRootDir + referenceDir : ";" + Config.EpicorAppServerRootDir + referenceDir;
            }

            //GelitaHelpers Reference Path
            this.GelitaHelpersReferenceDir = Config.EpicorAppServerRootDir + Config.ELConfig.ELAutoDeployDir;

            //Auto Flag
            this.AutoOpenProj = Config.AutoOpen && Config.BPMConfig.BPMAutoOpen;
            this.AutoDeploy = Config.AutoDeploy && Config.BPMConfig.BPMAutoDeploy; 
            this.AutoIISRecycle = Config.AutoIISRecycle && Config.BPMConfig.BPMAutoIISRecycle;
            this.AutoCommit = Config.AutoCommit && Config.BPMConfig.BPMAutoCommit; //For future upgrade, not in use for now

            //cmd IISRecycle for current in new project postbuild event
            this.PostBuildEvent_IISRecycleForCurrentEpicorServer = Config.ECTargetRootDir + Config.IISRecyclePath + " " + Config.EpicorAppServer;

            //Project Build to
            this.ProjectDebugBuiltToDir = Config.EpicorAppServerRootDir + Config.BPMConfig.BPMProjectDebugRootDir + bpm.Folder;
        }

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
        
        public virtual void Build()
        {
            if (this.CheckSourceAvailable() == false) { throw new Exception("Source Not Exists"); }

            //create project directory
            this.CreateProjectDirectory();

            //create .csproj and .csproj.user 
            this.CreateCSProj();

            //copy source code to target directory
            this.CopySourceToTarget();

            //update and save csproj for new BO Project
            this.PrepareCSProj();

            //PostBuildEvent: AutoIISRecycle
            this.PrepareCSProjPostBuildEvent();

            //Auto Open Proj
            if (AutoOpenProj == true) { this.OpenProj(); }
        }

        protected void CreateProjectDirectory()
        {
            //find or create target path
            if (false == System.IO.Directory.Exists(this.TargetProject_SourceCode_Dir))
            {
                //create directory
                System.IO.Directory.CreateDirectory(this.TargetProject_SourceCode_Dir);
            }
        }
        protected void CreateCSProj()
        {
            //create cs project file (.csproj) from template xml
            var csproj = System.IO.File.ReadAllText(this.ProjTemplatePath)
                                         .Replace("ABCCode.Update", bpm.Name);
            var csprojuser = System.IO.File.ReadAllText(this.ProjUserTemplatePath)
                                             .Replace("TemplateReferencePath", this.ProjReferenceDirs);
            //create .csproj and .csproj.user file
            System.IO.File.WriteAllText(this.CSProjPath, csproj);
            System.IO.File.WriteAllText(this.CSProjUserPath, csprojuser);
        }
        protected abstract void CopySourceToTarget();
        protected void PrepareCSProj()
        {
            //load csproj 
            var proj = new XmlDocument();
            proj.Load(this.CSProjPath);

            //create XML NameSpace
            var strNamespace = proj.DocumentElement.NamespaceURI;
            var nsManager = new XmlNamespaceManager(proj.NameTable);
            string prefix = bpm.GetType().ToString() + "Project";
            nsManager.AddNamespace(prefix, strNamespace);

            //remove child nodes of Compile and Reference node
            var compileParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":Compile", nsManager).ParentNode;
            compileParent.RemoveAll();
            var ReferenceParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":Reference", nsManager).ParentNode;
            ReferenceParent.RemoveAll();

            //find target source files
            var targetSourceFiles = System.IO.Directory.GetFiles(this.TargetProject_SourceCode_Dir);

            //update csproj
            targetSourceFiles.ToList().ForEach((targetSourceFile) =>
            {
                //if is reference.lst, create reference node
                if (targetSourceFile.Split('.').LastOrDefault().ToLower() == "lst" && targetSourceFile.Split('\\').LastOrDefault().ToLower() == "references.lst")
                {
                    //load all references
                    string[] referenceLst = System.IO.File.ReadAllLines(targetSourceFile);

                    //write all references to csproj
                    foreach (var reference in referenceLst)
                    {
                        var referenceNode = proj.CreateNode(XmlNodeType.Element, "Reference", strNamespace);
                        var attribute = proj.CreateAttribute("Include");
                        attribute.Value = reference;
                        referenceNode.Attributes.Append(attribute);
                        ReferenceParent.AppendChild(referenceNode);

                        //HintPath Node
                        var HintPathNode = proj.CreateNode(XmlNodeType.Element, "HintPath", strNamespace);
                        string[] referenceDirs = this.ProjReferenceDirs.Split(';');
                        string dllName = reference + ".dll";
                        foreach(var referenceDir in referenceDirs)
                        {
                            if (true == System.IO.File.Exists(referenceDir + dllName))
                            {
                                HintPathNode.InnerText = referenceDir + dllName;
                            }
                        }
                        referenceNode.AppendChild(HintPathNode);

                        //Private Node
                        var PrivateNode = proj.CreateNode(XmlNodeType.Element, "Private", strNamespace);
                        PrivateNode.InnerText = "False";
                        referenceNode.AppendChild(PrivateNode);

                    }
                }
                //if is .cs, create compile node
                else if (targetSourceFile.Split('.').LastOrDefault().ToLower() == "cs")
                {
                    var compileNode = proj.CreateNode(XmlNodeType.Element, "Compile", strNamespace);
                    var attribute = proj.CreateAttribute("Include");
                    attribute.Value = targetSourceFile;
                    compileNode.Attributes.Append(attribute);
                    compileParent.AppendChild(compileNode);
                }
            });

            //Add GelitaHelpers to csproj
            AddGelitaHelpersReferenceToCSProj(proj,nsManager,ReferenceParent,prefix,strNamespace);

            //output path update for DEBUG
            //get all output path
            //var OutputPathParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":Reference", nsManager).ParentNode;
            foreach (var OutputPathNode in proj.DocumentElement.SelectNodes("//" + prefix + ":OutputPath",nsManager))
            {
                var OutputPathParentNode = (OutputPathNode as XmlElement).ParentNode;
                foreach(var ParentNodeAttribute in OutputPathParentNode.Attributes)
                {
                    if ( (ParentNodeAttribute as XmlAttribute).Value.Contains("Debug"))
                    {
                        (OutputPathNode as XmlElement).InnerText = this.ProjectDebugBuiltToDir;
                    }
                }
            }

            //save csproj
            proj.Save(this.CSProjPath);
        }
        protected abstract bool CheckSourceAvailable();
        protected void PrepareCSProjPostBuildEvent()
        {
            //load csproj 
            var proj = new XmlDocument();
            proj.Load(this.CSProjPath);

            //create XML NameSpace
            var strNamespace = proj.DocumentElement.NamespaceURI;
            var nsManager = new XmlNamespaceManager(proj.NameTable);
            string prefix = bpm.GetType().ToString() + "Project";
            nsManager.AddNamespace(prefix, strNamespace);

            //remove child nodes of PostBuildEvent
            var postBuildEventParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":PostBuildEvent", nsManager).ParentNode;
            postBuildEventParent.RemoveAll();

            //Add PostBuildEvent
            var PostBuildEventNode = proj.CreateNode(XmlNodeType.Element, "PostBuildEvent", strNamespace);
            if (this.AutoIISRecycle == true)
            {
                PostBuildEventNode.InnerText = this.PostBuildEvent_IISRecycleForCurrentEpicorServer;
            }
            postBuildEventParent.AppendChild(PostBuildEventNode);

            //save csproj
            proj.Save(this.CSProjPath);
        }
        protected void OpenProj()
        {
            System.Diagnostics.Process.Start(this.CSProjPath);
        }

        private void AddGelitaHelpersReferenceToCSProj(XmlDocument proj, XmlNamespaceManager nsManager,XmlNode ReferenceParent,string prefix,string strNamespace)
        {
            var referenceNode = proj.CreateNode(XmlNodeType.Element, "Reference", strNamespace);
            var attribute = proj.CreateAttribute("Include");
            attribute.Value = "GelitaHelpers";
            referenceNode.Attributes.Append(attribute);
            ReferenceParent.AppendChild(referenceNode);

            //HintPath Node
            var HintPathNode = proj.CreateNode(XmlNodeType.Element, "HintPath", strNamespace);
            string dllName = "GelitaHelpers" + ".dll";
            if (true == System.IO.File.Exists(this.GelitaHelpersReferenceDir + dllName))
            {
                HintPathNode.InnerText = this.GelitaHelpersReferenceDir + dllName;
            }
            referenceNode.AppendChild(HintPathNode);

            //Private Node
            var PrivateNode = proj.CreateNode(XmlNodeType.Element, "Private", strNamespace);
            PrivateNode.InnerText = "False";
            referenceNode.AppendChild(PrivateNode);
        }
    }
    
    internal class BOProject : BPMProject
    {
        public BOProject(IBPM BPM, ECMTConfig Config) : base(BPM,Config)
        {
            this.SourceCode_SourcesFolder_Dir = base.SourceCode_SourcesFolder_Dir + (bpm as BO).Name;
        }
        public override void Build()
        {
            //base.Build will find or create target directory
            base.Build();

            //any additonal config 

        }
        protected override void CopySourceToTarget()
        {
            //find latest version of the source code
            var sourceBO = this.SourceCode_SourcesFolder_Dir;
            var sourceBO_LatestVersion = System.IO.Directory.GetDirectories(sourceBO)
                        .OrderByDescending(x => int.Parse(x.Split('\\').Last().Split('.').First()))
                        .ThenByDescending(x => x).First();
            var sourceFiles = System.IO.Directory.GetFiles(sourceBO_LatestVersion);
            sourceFiles.ToList().ForEach((sourceFile) =>
            {
                var targetSourceFile = sourceFile.Split('\\').ToList().Last();
                System.IO.File.Copy(sourceFile, this.TargetProject_SourceCode_Dir + targetSourceFile, true);
            });
        }
        protected override bool CheckSourceAvailable()
        {
            bool isSourceAvailable = false;
            var dirs = System.IO.Directory.GetDirectories(this.SourceCode_SourcesFolder_Dir);
            if (dirs.Count() == 0) { isSourceAvailable = false; }
            else if (dirs.Count() > 0) { isSourceAvailable = true; }
            return isSourceAvailable;
        }
    }
    
    internal class DTProject : BPMProject
    {
        public DTProject(IBPM BPM, ECMTConfig Config) : base(BPM, Config)
        {
            this.SourceCode_SourcesFolder_Dir = base.SourceCode_SourcesFolder_Dir + "ERP." + (bpm as DT).Name + ".Triggers";
        }
        public override void Build()
        {
            //base.Build will find or create target directory
            base.Build();

            //any additonal config 

        }
        protected override void CopySourceToTarget()
        {
            //find latest version of the source code
            var sourceDT = this.SourceCode_SourcesFolder_Dir;
            var sourceDT_LatestVersion = System.IO.Directory.GetDirectories(sourceDT)
                        .OrderByDescending(x => int.Parse(x.Split('\\').Last().Split('.').First()))
                        .ThenByDescending(x => x).First();
            var sourceFiles = System.IO.Directory.GetFiles(sourceDT_LatestVersion);
            sourceFiles.ToList().ForEach((sourceFile) =>
            {
                var targetSourceFile = sourceFile.Split('\\').ToList().Last();
                System.IO.File.Copy(sourceFile, this.TargetProject_SourceCode_Dir + targetSourceFile, true);
            });
        }
        protected override bool CheckSourceAvailable()
        {
            bool isSourceAvailable = false;
            var dirs = System.IO.Directory.GetDirectories(this.SourceCode_SourcesFolder_Dir);
            if (dirs.Count() == 0) { isSourceAvailable = false; }
            else if (dirs.Count() > 0) { isSourceAvailable = true; }
            return isSourceAvailable;
        }
    }
    
    internal class UbaqProject : BPMProject
    {
        public UbaqProject(IBPM BPM,ECMTConfig Config) : base(BPM, Config)
        {
            this.SetUbaqCompanyID(bpm as Ubaq);
            this.SourceCode_SourcesFolder_Dir = base.SourceCode_SourcesFolder_Dir + "_" + (bpm as Ubaq).CompanyID + "_" + (bpm as Ubaq).Name;
        }
        public override void Build()
        {
            //base.Build will find or create target directory
            base.Build();

            //any additonal config 

        }
        protected override void CopySourceToTarget()
        {
            //Ubaq source folder format: _CompanyID_UbaqID.MethodName e.g. _500_E10LotStatusMaint.Update
            //find latest version of the source code 
            string sourceUbaq = this.SourceCode_SourcesFolder_Dir;
            var sourceUbaq_LatestVersion = System.IO.Directory.GetDirectories(sourceUbaq)
                        .OrderByDescending(x => int.Parse(x.Split('\\').Last().Split('.').First()))
                        .ThenByDescending(x => x).First();
            var sourceFiles = System.IO.Directory.GetFiles(sourceUbaq_LatestVersion);
            sourceFiles.ToList().ForEach((sourceFile) =>
            {
                var targetSourceFile = sourceFile.Split('\\').ToList().Last();
                System.IO.File.Copy(sourceFile, this.TargetProject_SourceCode_Dir + targetSourceFile, true);
            });
        }
        protected override bool CheckSourceAvailable()
        {
            //this.SetUbaqCompanyID(bpm as Ubaq);
            bool isSourceAvailable = false;

            //System or company-unspecified Ubaq not created
            if ((bpm as Ubaq).CompanyID != "500" && (bpm as Ubaq).CompanyID != "501") { throw new Exception("System or Non-Gelita Ubaq will not be Created"); }
            var dirs = System.IO.Directory.GetDirectories(this.SourceCode_SourcesFolder_Dir);
            if (dirs.Count() == 0) { isSourceAvailable = false; }
            else if (dirs.Count() > 0) { isSourceAvailable = true; }
            return isSourceAvailable;
        }
        private void SetUbaqCompanyID(Ubaq ubaq)
        {
            string searchPath = string.Empty;
            if (ubaq.CompanyID != "500" && ubaq.CompanyID != "501")
            {
                searchPath = this.SourceCode_SourcesFolder_Dir + "_" + "*" + "_" + ubaq.Name;
                string[] dirs = System.IO.Directory.GetDirectories(this.SourceCode_SourcesFolder_Dir, "_" + "*" + "_" + ubaq.Name, System.IO.SearchOption.TopDirectoryOnly);
                if (dirs.Count() == 1)
                {
                    if (dirs[0].Contains("500")) { ubaq.CompanyID = "500"; }
                    else if (dirs[0].Contains("501")) { ubaq.CompanyID = "501"; }
                }
            }

        }
        
    }

}
