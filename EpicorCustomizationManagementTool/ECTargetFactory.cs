using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicorCustomizationManagementTool.DataModel;
using EpicorCustomizationManagementTool.BPMProject;
using EpicorCustomizationManagementTool.ELProject;
using EpicorCustomizationManagementTool.UIProject;
using EpicorCustomizationManagementTool.RPTProject;

namespace EpicorCustomizationManagementTool
{
    public interface IECTargetFactory
    {
        void Build(IEC EC);
    }

    internal class ECTargetFactory: IECTargetFactory
    {
        public void Build(IEC EC)
        {
            System.Type type = EC.GetType();
            if (EC is IBPM)
            {
                new BPMProjectFactory().BuildProject((EC as IBPM), ECMTConfigManager.Singleton.ECMTConfig);
            }
            else if (EC is IUI)
            {
                new UIProjectFactory().BuildProject((EC as IUI), ECMTConfigManager.Singleton.ECMTConfig);
            }
            else if (EC is IEL)
            {
                new ELProjectFactory().Open((EC as IEL), ECMTConfigManager.Singleton.ECMTConfig);
            }
            else if (EC is IRPT)
            {
                new RPTProjectFactory().Open((EC as IRPT), ECMTConfigManager.Singleton.ECMTConfig);
            }
            else { throw new Exception("Epicor Customization Not Recognized"); }
        }
    }

    public enum VSProjectFileType
    {
        [System.ComponentModel.DefaultValue(".sln")]
        Solution,
        [System.ComponentModel.DefaultValue(".csproj")]
        CSharpProject,
        [System.ComponentModel.DefaultValue(".csproj.user")]
        CSharpProjectUser,
        [System.ComponentModel.DefaultValue(".cs")]
        CSharpSource,
        [System.ComponentModel.DefaultValue(".txt")]
        Text,
        [System.ComponentModel.DefaultValue(".xml")]
        Config,
        [System.ComponentModel.DefaultValue(".xml")]
        BPMTemplate,
        [System.ComponentModel.DefaultValue(".xml")]
        RPTProjTemplate,
        [System.ComponentModel.DefaultValue(".rptproj")]
        SSRSProj,
        [System.ComponentModel.DefaultValue(".rds")]
        SSRSReportDataSource,
        [System.ComponentModel.DefaultValue(".rdl")]
        ReportDefinitionLanguage
    }
}
