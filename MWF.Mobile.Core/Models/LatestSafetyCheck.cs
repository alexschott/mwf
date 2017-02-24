using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models.Attributes;

namespace MWF.Mobile.Core.Models
{
    
    /// <summary>
    /// Stores the latest vehicle and trailer safety check for a driver
    /// </summary>
    public class LatestSafetyCheck : IBlueSphereEntity
    {

        public LatestSafetyCheck()
        {
            this.ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]
        public Guid ID { get; set; }

        [Unique]
        public Guid DriverID { get; set; }

        [ChildRelationship(typeof(SafetyCheckData), RelationshipCardinality.OneToZeroOrOne, "IsTrailer", false)]
        public SafetyCheckData VehicleSafetyCheck { get; set; }

        [ChildRelationship(typeof(SafetyCheckData), RelationshipCardinality.OneToZeroOrOne, "IsTrailer", true)]
        public SafetyCheckData TrailerSafetyCheck { get; set; }

    }

}
