using System;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models.Attributes;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
    [Table("InstructionTrailer")]
    public class Trailer : IBlueSphereEntity
    {
        public Trailer()
        {
            ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]
        [XmlIgnore]
        public Guid ID { get; set; }

        [JsonProperty("#text")]
        [XmlText]
        public string TrailerId { get; set; }

        [JsonProperty("@DisplayName")]
        [XmlIgnore]
        public string DisplayName { get; set; }

        [JsonProperty("@type")]
        [XmlIgnore]
        public string Type { get; set; }

        [ForeignKey(typeof(Additional))]
        [XmlIgnore]
        public Guid AdditionalId { get; set; }
    }
}
