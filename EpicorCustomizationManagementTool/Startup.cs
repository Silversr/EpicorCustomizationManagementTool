using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicorCustomizationManagementTool
{
    class Startup
    {
        public static string EpicorEnvironment;
        public static string UserEnvironment;
        static void Main(string[] args)
        {
            //Environment
            if (args.Count() >= 2)
            {
                EpicorEnvironment = args[0];
                UserEnvironment = args[1];
            }
            //only for test
            else
            {
                EpicorEnvironment = "Test";
                UserEnvironment = "ZhangH";
            }
            //Exec cmd entry
            new CMDEntry().Run();
        }
    }
}
