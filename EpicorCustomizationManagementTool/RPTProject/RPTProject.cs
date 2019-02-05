using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicorCustomizationManagementTool.DataModel;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Extensions;
using System.Xml;


namespace EpicorCustomizationManagementTool.RPTProject
{
    public interface IRPTProjectFactory
    {
        void Open(IRPT RPT, ECMTConfig Config);
    }
    internal class RPTProjectFactory : IRPTProjectFactory
    {
        public void Open(IRPT RPT, ECMTConfig Config)
        {
            if (RPT is Crystal) { throw new NotImplementedException(); }
            else if (RPT is BT) { throw new NotImplementedException(); }
            else if (RPT is SSRS) { new SSRSProject(RPT,Config).Build(); }
            else { throw new Exception("BPMType Not Recognized"); }
        }
    }


    internal abstract class RPTProject
    {
        protected IRPT rpt { get; }
        public RPTProject(IRPT RPT, ECMTConfig Config)
        {
            this.rpt = RPT;
        }
        public virtual void Build() { }
    }
    internal class SSRSProject : RPTProject
    {
        protected string BaseURL;
        protected string Resource;
        protected string TargetProject_SLN_Dir;
        protected string TargetProject_RDLandPROJandRDS_Dir;
        protected string ProjTemplatePath;
        protected string ProjTemplateSharedDataSourcePath;
        protected string RPTRDLDir;
        protected string RPTProjPath;
        protected string SDSPath;
        protected string EnvStr;
        protected string ProjectBuildTargetServerURL;
        protected string ProjectBuildTargetReportFolder;
        protected string ReportingServerHost;
        protected string ReportDatabase;
        protected bool AutoOpenProj;

        public SSRSProject(IRPT RPT, ECMTConfig Config): base(RPT, Config)
        {
            this.BaseURL = Config.RPTConfig.SSRSWebPortal;
            this.Resource = @"api/v1.0/catalogitems";
            this.TargetProject_SLN_Dir = Config.ECTargetRootDir + Config.RPTConfig.RPTTargetProjectDir + this.rpt.Name + @"\";
            this.TargetProject_RDLandPROJandRDS_Dir = this.TargetProject_SLN_Dir + this.rpt.Name + @"\";
            this.RPTRDLDir = this.TargetProject_RDLandPROJandRDS_Dir;
            this.EnvStr = Config.EpicorAppServer;
            this.ProjTemplatePath = Config.RPTConfig.SSRSProjectTemplateDir;
            this.ProjTemplateSharedDataSourcePath = Config.RPTConfig.SSRSSharedDataSourceTemplateDir;
            //.rptproj and .rds
            this.RPTProjPath = this.TargetProject_RDLandPROJandRDS_Dir + rpt.Name + this.ProjectFileTypeDictionary[VSProjectFileType.SSRSProj]; //.csproj
            this.SDSPath = this.TargetProject_RDLandPROJandRDS_Dir + rpt.Name + this.ProjectFileTypeDictionary[VSProjectFileType.SSRSReportDataSource];//.rds
            this.ReportDatabase = Config.RPTConfig.SSRSEpicorReportDatabase;
            this.ReportingServerHost = Config.RPTConfig.SSRSReportingServerHost;
            this.ProjectBuildTargetServerURL = @"http://" + this.ReportingServerHost + @"/reportserver";
            this.AutoOpenProj = Config.AutoOpen && Config.RPTConfig.RPTAutoOpen;
        }
        protected Dictionary<VSProjectFileType, string> ProjectFileTypeDictionary = new Dictionary<VSProjectFileType, string>()
        {
            { VSProjectFileType.Solution,".sln"},
            { VSProjectFileType.SSRSProj,".rptproj"},
            { VSProjectFileType.ReportDefinitionLanguage,".rdl"},
            { VSProjectFileType.SSRSReportDataSource,".rds"},
            { VSProjectFileType.Text,".txt"},
            { VSProjectFileType.Config,".xml"},
            { VSProjectFileType.RPTProjTemplate,".xml"}
        };
        public override void Build()
        {
            //base.Build will find or create target directory
            base.Build();

            //any additonal config 
            //Create Directory
            CreateProjectDirectory();
            //Create RptProject
            CreateRPTProj();
            //Download .RDL - RestSharp Client
            DownloadRDLs();
            //PrepareRptProject&PostBuildEvent - Deployment
            PrepareRPTProj();
            //PrepareSSRSSharedDataSource
            PrepareSSRSSharedDataSource();
            //PrepareSSRSRDL
            PrepareSSRSRDL();
            //Open Project
            if (AutoOpenProj == true) { this.OpenProj(); }
        }
        protected void CreateProjectDirectory()
        {
            //find or create target path
            if (false == System.IO.Directory.Exists(this.RPTRDLDir))
            {
                //create directory
                System.IO.Directory.CreateDirectory(this.RPTRDLDir);
            }
        }
        protected void DownloadRDLs()
        {
            var client = new RestClient();
            client.BaseUrl = new Uri(this.BaseURL);
            client.Authenticator = new NtlmAuthenticator();

            //DownloadAllRDLsFromReportFolderInCustomFolder
            if (DownloadAllRDLsFromReportFolderInCustomFolder(client, out string SourceFolderDirInCustomReports))
            {
                //Console.WriteLine("All RDLs Downloaded from Report Folder in CustomReports Directory - should trigger an event or alert");
                this.ProjectBuildTargetReportFolder = SourceFolderDirInCustomReports;
            }
            //DownloadAllRDLsFromReportFolderInSystemFolder()
            else if (DownloadAllRDLsFromReportFolderInSystemFolder(client, out string SourceFolderDirInSystemReports))
            {
                Console.WriteLine("Warning: System Report Downloaded.");
                this.ProjectBuildTargetReportFolder = SourceFolderDirInSystemReports;
            }
            //DownloadRDLInCustomFolder()
            else if (DownloadRDLInCustomFolder(client, out string SourceFileFolderOneRDLInCustomReports))
            {
                //Console.WriteLine("One RDL Downloaded in CustomReports Directory - should trigger an event or alert");
                this.ProjectBuildTargetReportFolder = SourceFileFolderOneRDLInCustomReports;
            }
            else
            {
                throw new ApplicationException("cannot find or download RDL/RDLs");
            }
        }
        protected void CreateRPTProj()
        {
            //create cs project file (.csproj) from template xml
            var rptproj = System.IO.File.ReadAllText(this.ProjTemplatePath)
                                         .Replace("EpicorERPTest", EnvStr)
                                         .Replace("SSRSReportTemplate", rpt.Name) ;
            var rptshareddatasource = System.IO.File.ReadAllText(this.ProjTemplateSharedDataSourcePath)
                                             .Replace("SSRSReportTemplate", rpt.Name)
                                             .Replace("SRVIKEPIC10", this.ReportingServerHost)
                                             .Replace("EpicorERPTestReports",this.ReportDatabase);
            //create .csproj and .csproj.user file
            System.IO.File.WriteAllText(this.RPTProjPath, rptproj);
            System.IO.File.WriteAllText(this.SDSPath, rptshareddatasource);
        }
        protected void PrepareRPTProj()
        {
            //load proj 
            var proj = new XmlDocument();
            proj.Load(this.RPTProjPath);

            //create XML NameSpace
            var strNamespace = proj.DocumentElement.NamespaceURI;
            var nsManager = new XmlNamespaceManager(proj.NameTable);
            string prefix = rpt.GetType().ToString() + "Project";
            nsManager.AddNamespace(prefix, strNamespace);

            
            //remove child nodes of Compile and Reference node
            var compileParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":DataSource", nsManager).ParentNode;
            compileParent.RemoveAll();
            var ReferenceParent = proj.DocumentElement.SelectSingleNode("//" + prefix + ":Report", nsManager).ParentNode;
            ReferenceParent.RemoveAll();
            //ItemGroup: DataSource and ItemGroup: RDL
            //find target rds and rdl
            var targetSourceFiles = System.IO.Directory.GetFiles(this.TargetProject_RDLandPROJandRDS_Dir);
            targetSourceFiles.ToList().ForEach((targetSourceFile) => 
            {
                //if is .rds, create data source node
                if (targetSourceFile.Split('\\').LastOrDefault() != null && targetSourceFile.Split('.').LastOrDefault().ToLower() == "rds" )
                {
                    var compileNode = proj.CreateNode(XmlNodeType.Element, "DataSource", strNamespace);
                    var attribute = proj.CreateAttribute("Include");
                    attribute.Value = targetSourceFile.Split('\\').LastOrDefault(); compileNode.Attributes.Append(attribute);
                    compileParent.AppendChild(compileNode);
                    
                }
                else if (targetSourceFile.Split('\\').LastOrDefault() != null && targetSourceFile.Split('.').LastOrDefault().ToLower() == "rdl")
                {
                    var compileNode = proj.CreateNode(XmlNodeType.Element, "Report", strNamespace);
                    var attribute = proj.CreateAttribute("Include");
                    if (targetSourceFile.Split('\\').LastOrDefault() != null)
                    {
                        attribute.Value = targetSourceFile.Split('\\').LastOrDefault();
                    }
                    compileNode.Attributes.Append(attribute);
                    compileParent.AppendChild(compileNode);
                }
                //if is .rdl, create report note
            });

            //output path
            //get all output path
            foreach (var OutputPathNode in proj.DocumentElement.SelectNodes("//" + prefix + ":OutputPath", nsManager))
            {
                var OutputPathParentNode = (OutputPathNode as XmlElement).ParentNode;
                foreach (var node in OutputPathParentNode.ChildNodes)
                {
                    if ((node as XmlNode).Name == "TargetServerURL")
                    {
                        (node as XmlNode).InnerText = this.ProjectBuildTargetServerURL;
                    }
                    if ((node as XmlNode).Name == "TargetReportFolder")
                    {
                        (node as XmlNode).InnerText = this.ProjectBuildTargetReportFolder;
                    }
                }
            }

            //save proj
            proj.Save(this.RPTProjPath);
        }
        protected void PrepareSSRSSharedDataSource()
        {
        }
        protected void PrepareSSRSRDL()
        {
            var RDLFiles = System.IO.Directory.GetFiles(this.TargetProject_RDLandPROJandRDS_Dir, "*.rdl",System.IO.SearchOption.TopDirectoryOnly);
            RDLFiles.ToList().ForEach((RDLFile) => 
            {
                //load proj 
                var proj = new XmlDocument();
                proj.Load(RDLFile);

                //create XML NameSpace
                var strNamespace = proj.DocumentElement.NamespaceURI;
                var nsManager = new XmlNamespaceManager(proj.NameTable);
                string prefix = rpt.GetType().ToString() + "Project";
                nsManager.AddNamespace(prefix, strNamespace);
                foreach (var DataSourcesNode in proj.DocumentElement.SelectNodes("//" + prefix + ":DataSources", nsManager))
                {
                    var DataSourcesParentNode = (DataSourcesNode as XmlElement).ParentNode;
                    foreach (var dataSourcesNode in DataSourcesParentNode.ChildNodes)
                    {
                        if ((dataSourcesNode as XmlNode).Name == "DataSources")
                        {
                            foreach(var childNodeOfDataSourcesNode in (dataSourcesNode as XmlNode).ChildNodes)
                            {
                                if ((childNodeOfDataSourcesNode as XmlNode).Name != "DataSource") { continue; }
                                foreach (var childNode in (childNodeOfDataSourcesNode as XmlNode).ChildNodes)
                                {
                                    if ((childNode as XmlNode).Name == "DataSourceReference" && this.SDSPath.Split('\\').LastOrDefault() != null)
                                    {
                                        (childNode as XmlNode).InnerText = this.SDSPath.Split('\\').LastOrDefault().Replace(".rds", "");
                                    }
                                    if ((childNode as XmlNode).Name == "rd:DataSourceID")
                                    {
                                        (childNode as XmlNode).InnerText = "18105fee-0a7d-440d-8998-ed1a7aaef9cb";
                                    }
                                }
                            }
                            
                        }
                        
                    }
                }
                //save proj
                proj.Save(RDLFile);
            });
            

            
        }
        protected void OpenProj()
        {
            System.Diagnostics.Process.Start(this.RPTProjPath);
        }

        private bool DownloadAllRDLsFromReportFolderInCustomFolder(RestClient client, out string sourceFolderDir)
        {
            bool downloadSucessful = false;
            var request = new RestRequest();
            //request.Resource = this.Resource + "?$filter=" + "name" + " " + "eq" + " " + "'" + (this.rpt as SSRS).RPTName + "'";
            try
            {
                sourceFolderDir = "";
                //Find Folder From Custom Folder
                request.Resource = this.Resource
                                    + $"?$filter="
                                    + $"contains(tolower(path),'{EnvStr.ToLower()}')"
                                    + " and "
                                    + $"contains(path,'CustomReports')"
                                    + " and "
                                    + $"tolower(name) eq '{(this.rpt as SSRS).RPTName.ToLower()}'";
                IRestResponse response = client.Execute(request);
                Dictionary<string, object> data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                //Console.WriteLine(data);
                var valueStr = "{ \"Ary\":" + data["value"].ToString() + "}";
                Dictionary<string, Dictionary<string, object>[]> values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>[]>>(valueStr);
                foreach(var catalogItem in values["Ary"])
                {
                    if ((catalogItem["Type"] as string) != "Folder") { continue; }
                    var folderPath = catalogItem["Path"] as string;
                    if (true == DownloadRDLsFrom(folderPath, client)) { downloadSucessful = true; };
                    sourceFolderDir = folderPath;
                }
                return downloadSucessful;
                
            }
            catch (Exception ex)
            {
                throw ex;
                return false;
            }
        }
        private bool DownloadAllRDLsFromReportFolderInSystemFolder(RestClient client, out string sourceFolderDir)
        {
            bool downloadSucessful = false;
            var request = new RestRequest();
            //request.Resource = this.Resource + "?$filter=" + "name" + " " + "eq" + " " + "'" + (this.rpt as SSRS).RPTName + "'";
            try
            {
                sourceFolderDir = "";
                //Find Folder From Custom Folder
                request.Resource = this.Resource
                                    + $"?$filter="
                                    + $"contains(tolower(path),'{EnvStr.ToLower()}')"
                                    + " and "
                                    + $"not contains(path,'CustomReports')"
                                    + " and "
                                    + $"tolower(name) eq '{(this.rpt as SSRS).RPTName.ToLower()}'";
                IRestResponse response = client.Execute(request);
                Dictionary<string, object> data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                //Console.WriteLine(data);
                var valueStr = "{ \"Ary\":" + data["value"].ToString() + "}";
                Dictionary<string, Dictionary<string, object>[]> values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>[]>>(valueStr);
                foreach (var catalogItem in values["Ary"])
                {
                    if ((catalogItem["Type"] as string) != "Folder") { continue; }
                    var folderPath = catalogItem["Path"] as string;
                    if (true == DownloadRDLsFrom(folderPath, client)) { downloadSucessful = true; };
                    sourceFolderDir = folderPath;
                }
                
                return downloadSucessful;

            }
            catch (Exception ex)
            {
                throw ex;
                return false;
            }
        }
        private bool DownloadRDLInCustomFolder(RestClient client, out string sourceFolderDir)
        {
            bool downloadSucessful = false;
            var request = new RestRequest();
            //request.Resource = this.Resource + "?$filter=" + "name" + " " + "eq" + " " + "'" + (this.rpt as SSRS).RPTName + "'";
            try
            {
                sourceFolderDir = "";
                //Find Folder From Custom Folder
                request.Resource = this.Resource
                                    + $"?$filter="
                                    + $"contains(tolower(path),'{EnvStr.ToLower()}')"
                                    + " and "
                                    + $"contains(path,'CustomReports')"
                                    + " and "
                                    + $"tolower(name) eq '{(this.rpt as SSRS).RPTName.ToLower()}'";
                IRestResponse response = client.Execute(request);
                Dictionary<string, object> data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                //Console.WriteLine(data);
                var valueStr = "{ \"Ary\":" + data["value"].ToString() + "}";
                Dictionary<string, Dictionary<string, object>[]> values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>[]>>(valueStr);
                foreach (var catalogItem in values["Ary"])
                {
                    if ((catalogItem["Type"] as string) != "Report") { Console.WriteLine("Embeded Folder Found!"); continue; }
                    var catalogItemId = catalogItem["Id"] as string;
                    var catalogItemName = catalogItem["Name"] as string;
                    var catalogItemPath = catalogItem["Path"] as string;
                    var request2 = new RestRequest();
                    request2.Resource = this.Resource + "(" + catalogItemId + ")/Content/$Value";
                    client.DownloadData(request2).SaveAs(this.RPTRDLDir + catalogItemName + ".rdl");
                    downloadSucessful = true;
                    string[] temp = catalogItemPath.Split('/');
                    var tempCount = temp.Count();
                    if(tempCount >= 1)
                    {
                        for (int i = 0; i < temp.Count() - 1; i++)
                        {
                            sourceFolderDir += temp[i] + @"/";
                        }
                    }
                    if (sourceFolderDir.EndsWith(@"/")) { sourceFolderDir =  sourceFolderDir.Remove(sourceFolderDir.Length - 1,1); }
                }
                return downloadSucessful;

            }
            catch (Exception ex)
            {
                throw ex;
                return false;
            }
        }
        private bool DownloadRDLsFrom(string FolderDir, RestClient client)
        {
            try
            {
                
                var request = new RestRequest();
                request.Resource = this.Resource
                                        + $"?$filter="
                                        + $"contains(tolower(path),'{FolderDir.ToLower() + "/"}')";
                IRestResponse response = client.Execute(request);
                Dictionary<string, object> data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                var valueStr = "{ \"Ary\":" + data["value"].ToString() + "}";
                Dictionary<string, Dictionary<string, object>[]> values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>[]>>(valueStr);
                foreach (var catalogItem in values["Ary"])
                {
                    if ((catalogItem["Type"] as string) != "Report") { Console.WriteLine("Embeded Folder Found!");continue; }
                    var catalogItemId = catalogItem["Id"] as string;
                    var catalogItemName = catalogItem["Name"] as string;
                    var request2 = new RestRequest();
                    request2.Resource = this.Resource + "(" + catalogItemId + ")/Content/$Value";
                    client.DownloadData(request2).SaveAs(this.RPTRDLDir + catalogItemName + ".rdl");
                }
                return true;
            }
            catch(Exception ex)
            {
                throw ex;
                return false;
            }

        }
    }



}
