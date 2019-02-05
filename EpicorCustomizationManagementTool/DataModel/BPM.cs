using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicorCustomizationManagementTool.DataModel
{
    public interface IBPM : IEC
    {
        string Folder { get; set; }
    }
    internal abstract class BPM : IBPM
    {
        public virtual string Name { get; }
        public virtual string Description { get; }
        public virtual string Folder { get; set; }
        static public string DefaultFolder { get { return string.Empty; } }
    }

    internal class BO : BPM
    {
        //Construct
        internal BO(string boName, string methodName) : base()
        {
            MethodName = methodName;
            BOName = boName;
        }
        //Properties
        override public string Name { get { return BOName + "." + MethodName; } }
        public override string Description => base.Description;
        public string BOName { get; }
        public string MethodName { get; }
        override public string Folder { get { return BO.DefaultFolder; } set { } }

        //static properties
        new public static string DefaultFolder { get { return @"BO\"; } }

    }
    internal class DT : BPM
    {
        internal DT(string tableName) : base()
        {
            TableName = tableName;
        }
        override public string Name { get { return TableName; } }
        public string TableName { get; set; }
        override public string Folder { get { return DT.DefaultFolder; } set { } }
        //static properties
        new public static string DefaultFolder { get { return @"DT\"; } }
    }
    internal class Ubaq : BPM
    {
        internal Ubaq(string ubaqID, string methodName,string companyID = "")
        {
            UbaqID = ubaqID;
            MethodName = methodName;
        }
        override public string Name { get { return UbaqID + "." + MethodName; } }
        override public string Folder { get { return Ubaq.DefaultFolder; }  set { } }
        public string UbaqID { get; set; }
        public string MethodName { get; set; }
        public bool IsE10Only
        {
            get
            {
                bool isE10Only = false;
                if (UbaqID.ToUpper().Contains("E10"))
                {
                    isE10Only = true;
                }
                else
                {
                    isE10Only = false;
                }
                return isE10Only;
            }
        }
        public string CompanyID
        {
            get
            {
                if (UbaqID.Contains("500"))
                {
                    _CompanyID = "500";
                    return _CompanyID;
                }
                else if (UbaqID.Contains("501"))
                {
                    _CompanyID = "501";
                    return _CompanyID;
                }
                else
                {
                    return _CompanyID;
                }

            }
            set
            {
                if (value == "500" || value == "501") { _CompanyID = value; }
                else
                {
                    _CompanyID = string.Empty;
                }

            }

        }

        //fields
        private string _CompanyID = string.Empty;

        //static properties
        new public static string DefaultFolder { get { return @"Ubaq\"; } }
    }
}
