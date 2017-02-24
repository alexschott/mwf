using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;

namespace MWF.Mobile.Core.Models.Attributes
{

    public enum RelationshipCardinality
    {
        OneToOne,
        OneToMany,
        OneToZeroOrOne,
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ChildRelationshipAttribute : IgnoreAttribute
    {
        public ChildRelationshipAttribute(Type childType, RelationshipCardinality cardinality = RelationshipCardinality.OneToMany, string childIdentifyingPropertyName = "", object childIdentifyingPropertyValue = null)
        {
            // childType must implement IBlueSphereEntity
            //Contract.Requires<ArgumentException>(typeof(IBlueSphereEntity).GetTypeInfo().IsAssignableFrom(childType.GetTypeInfo()), 
            //                                    "ChildRelationshipAttribute should only apply to types that implement IBluesphereEntity");

            ChildType = childType;
            ChildIdentifyingPropertyName = childIdentifyingPropertyName;
            ChildIdentifyingPropertyValue = childIdentifyingPropertyValue;
            Cardinality = cardinality;
        }

        public Type ChildType { get; private set; }
        public string ChildIdentifyingPropertyName { get; private set; }
        public object ChildIdentifyingPropertyValue { get; private set; }
        public RelationshipCardinality Cardinality { get; private set; }
    }

}
