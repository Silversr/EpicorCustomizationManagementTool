using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicorCustomizationManagementTool.DataModel
{
    public interface IUI : IEC { }
    internal abstract class UI : IUI
    {
        virtual public string Name { get; }
        virtual public string Description { get; }
    }

    internal class FC : UI
    {
        internal FC(string formName)
        {
            this.FormName = formName;
        }
        //Properties
        override public string Name { get { return FormName; } }
        public override string Description => base.Description;
        public string FormName { get; }
        public string CustomizationType { get; } //Verticalization, Customization

    }
    internal class DBD : UI
    {

    }

}
