using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;

namespace MWF.Mobile.Core.Models.Attributes
{

    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : IndexedAttribute
    {
        public ForeignKeyAttribute(Type foreignType)
        {
            ForeignType = foreignType;
        }

        public Type ForeignType { get; private set; }
    }
   
}
