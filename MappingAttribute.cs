using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JustConnekt.Models.Utility
{
    [AttributeUsage(AttributeTargets.All)]
    public class MappingAttribute : Attribute
    {
        public string sql_ClassTable { get; set; }
        public string sql_RelationTable { get; set; }
        public bool ignore { get; set; }
        public string sql_name { get; set; }
        public bool sql_RelationshipKey { get; set; }
        public bool sql_Rel { get; set; }
        public MappingAttribute() {
            ignore = false;
        }

        public MappingAttribute(string sqlName) { sql_name = sqlName; ignore = false; }

        public MappingAttribute(string sqlName, bool ignoreThisMapping)
        {
            ignore = false;
            sql_name = sqlName;
        }
    }
}