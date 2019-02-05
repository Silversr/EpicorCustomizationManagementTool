using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicorCustomizationManagementTool.DataModel
{
    public interface IEL : IEC
    {
        string Folder { get; set; }
    }
    internal abstract class EL : IEL
    {
        public virtual string Name { get; }
        public string Description { get; }
        public virtual string Folder { get; set; }
        static public string DefaultFolder { get { return string.Empty; } }
    }

    internal class CSG : EL
    {
        public CSG(string genericServerProcessName)
        {
            this.GenericServerProcessName = genericServerProcessName;
        }
        public override string Name { get { return GenericServerProcessName; } }
        public string GenericServerProcessName { get; set; }
        override public string Folder { get { return CSG.DefaultFolder + GenericServerProcessName + @"\Server\Internal\CSG\" + GenericServerProcessName + @"\"; } set { } }

        //static properties
        new public static string DefaultFolder { get { return @"CSG\" ; } }
    }
    internal class GH : EL
    {
        public override string Name { get { return GH.DefaultName; } }
        private static string DefaultName = "GelitaHelpers";
        override public string Folder { get { return GH.DefaultFolder; } set { } }

        //static properties
        new public static string DefaultFolder { get { return GH.DefaultName + @"\"; } }
    }
}
