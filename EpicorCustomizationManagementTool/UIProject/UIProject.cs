using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpicorCustomizationManagementTool.DataModel;

namespace EpicorCustomizationManagementTool.UIProject
{
    public interface IUIProjectFactory
    {
        void BuildProject(IUI UI, ECMTConfig Config);
    }

    internal class UIProjectFactory : IUIProjectFactory
    {
        public void BuildProject(IUI UI, ECMTConfig Config)
        {
            if (UI is FC) { new FCProject(UI, Config).Build(); }
            else if (UI is DBD) { new DBDProject(UI, Config).Build(); }
            else { throw new Exception("UIType Not Recognized"); }
        }
    }


    internal abstract class UIProject
    {
        protected IUI ui { get; }
        public UIProject(IUI UI, ECMTConfig Config) { }
        public void Build()
        {
            //if (this.CheckSourceAvailable() == false) { throw new Exception("Source Not Exists"); }
            //check template available
            //create project directory
            //create .csproj and .csproj.user from template           
            //create empty file .cs to hold form script 
            //update and save csproj for new UI Project
            //PostBuildEvent: AutoIISRecycle
            //Auto Open Proj
            Console.WriteLine("UI Test!");
            throw new NotImplementedException(); 
        } 
    }
    internal class FCProject : UIProject
    {
        public FCProject(IUI UI, ECMTConfig Config) : base(UI, Config) { }
    }
    internal class DBDProject : UIProject
    {
        public DBDProject(IUI UI, ECMTConfig Config) : base(UI, Config) { }
    }
}
