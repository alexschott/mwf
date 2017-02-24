using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;
using MWF.Mobile.Core.Models.Attributes;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models
{

    // Model class which holds represents the results of a safety check performed by a driver on a vehicle/trailer
    [XmlRoot("safetycheckdata")]
    public class SafetyCheckData: IBlueSphereEntity
    {

        public SafetyCheckData()
        {
            this.ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]
        [XmlAttribute("id")]
        public Guid ID { get; set; }

        [XmlAttribute("driverid")]
        public Guid DriverID { get; set; }

        [XmlAttribute("vehicleid")]
        public Guid VehicleID { get; set; }

        [XmlAttribute("vehicleregistration")]
        public string VehicleRegistration { get; set; }

        [XmlAttribute("driver")]
        public string DriverTitle { get; set; }

        [XmlIgnore]
        public DateTime EffectiveDate { get; set; }

        [XmlAttribute("effectivedate")]
        public string EffectiveDateString
        {
            get { return this.EffectiveDate.ToLocalTime().ToString("u"); }
            set { this.EffectiveDate = DateTime.Parse(value); }
        }

        [XmlAttribute("mileage")]
        public int Mileage { get; set; }

        [XmlAttribute("smp")]
        public string SMP { get; set; }

        [ChildRelationship(typeof(Signature), RelationshipCardinality.OneToOne)]
        [XmlElement("signature")]
        public Signature Signature { get; set; }

        [ChildRelationship(typeof(SafetyCheckFault))]
        [XmlArray("faults")]
        public List<SafetyCheckFault> Faults { get; set; }

        [XmlAttribute("intlink")]
        public int ProfileIntLink { get; set; }

        [XmlIgnore]
        public bool IsTrailer { get; set; }

        [ForeignKey(typeof(LatestSafetyCheck))]
        [XmlIgnore]
        public Guid LatestSafetyCheckID { get; set; }

        public Enums.SafetyCheckStatus GetOverallStatus()
        {
            return GetOverallStatus(this.Faults.Select(f => f.Status));
        }

        public static Enums.SafetyCheckStatus GetOverallStatus(IEnumerable<Enums.SafetyCheckStatus> statuses)
        {
            if (!statuses.Any() || statuses.Any(s => s == Enums.SafetyCheckStatus.NotSet))
                return Enums.SafetyCheckStatus.NotSet;

            if (statuses.Any(s => s == Enums.SafetyCheckStatus.Failed))
                return Enums.SafetyCheckStatus.Failed;

            if (statuses.Any(s => s == Enums.SafetyCheckStatus.DiscretionaryPass))
                return Enums.SafetyCheckStatus.DiscretionaryPass;

            return Enums.SafetyCheckStatus.Passed;
        }

        /// <summary>
        /// Clone an existing safety check - doesn't include the Faults
        /// </summary>
        internal static SafetyCheckData ShallowCopy(SafetyCheckData scd)
        {
            return new SafetyCheckData
            {
                ID = scd.ID,
                DriverID = scd.DriverID,
                DriverTitle = scd.DriverTitle,
                VehicleID = scd.VehicleID,
                VehicleRegistration = scd.VehicleRegistration,
                EffectiveDate = scd.EffectiveDate,
                Mileage = scd.Mileage,
                SMP = scd.SMP,
                Signature = scd.Signature,
                ProfileIntLink = scd.ProfileIntLink,
                IsTrailer = scd.IsTrailer,
                LatestSafetyCheckID = scd.LatestSafetyCheckID,
            };
        }

    }

}
