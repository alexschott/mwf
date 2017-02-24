using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;
using MWF.Mobile.Core.Models.Attributes;

namespace MWF.Mobile.Core.Models
{
    public class SafetyProfile : IBlueSphereEntity
    {
        [Unique]
        [PrimaryKey]
        [JsonProperty("@id")]
        public Guid ID { get; set; }

        [JsonProperty("@title")]
        public string Title { get; set; }

        [JsonProperty("@intlink")]
        public int IntLink { get; set; }

        [JsonProperty("@odo")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool OdometerRequired { get; set; }

        [JsonProperty("@sig")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool SignatureRequired { get; set; }

        [JsonProperty("@checklist")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool DisplayAsChecklist { get; set; }

        [JsonProperty("@logon")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool DisplayAtLogon { get; set; }

        [JsonProperty("@logoff")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool DisplayAtLogoff { get; set; }

        [JsonProperty("@strailerprofile")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool IsTrailerProfile { get; set; }

        [JsonProperty("@isvosacompliant")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool IsVOSACompliant { get; set; }

        [ChildRelationship(typeof(SafetyCheckFaultType))]
        [JsonProperty("faults")]
        [JsonConverter(typeof(JsonWrappedListConverter<SafetyCheckFaultType>))]
        public List<SafetyCheckFaultType> Children { get; set; }


    }
}
