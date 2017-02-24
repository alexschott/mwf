using System;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;
using MWF.Mobile.Core.Models.Attributes;

namespace MWF.Mobile.Core.Models
{
    public class VerbProfileItem : IBlueSphereEntity
    {
        [Unique]
        [PrimaryKey]
        [JsonProperty("@id")]
        public Guid ID { get; set; }

        [JsonProperty("@tle")]
        public string Title { get; set; }

        [JsonProperty("@seq")]
        public string Order { get; set; }

        [JsonProperty("@hlt")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool IsHighlighted { get; set; }

        [JsonProperty("@cde")]
        public string Code { get; set; }

        [JsonProperty("@sig")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool ShowSignature { get; set; }

        [JsonProperty("@com")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool ShowComment { get; set; }

        [JsonProperty("@img")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool ShowImage { get; set; }

        [JsonProperty("@cat")]
        public string Category { get; set; }

        [ForeignKey(typeof(VerbProfile))]
        public Guid VerbProfileID { get; set; }

    }
}
