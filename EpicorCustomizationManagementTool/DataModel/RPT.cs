using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicorCustomizationManagementTool.DataModel
{
    public interface IRPT : IEC { }
    internal abstract class RPT : IRPT
    {
        public virtual string Name { get; }
        public virtual string Description { get; }
        public bool IsNew { get; }
    }

    internal class Crystal : RPT { }
    internal class BT : RPT { }
    internal class SSRS : RPT
    {
        public string ID;
        override public string Name { get { return RPTName; } }
        public string RPTName { get; }
        internal SSRS(string rptName) : base()
        {
            RPTName = rptName;
        }
    }
}
