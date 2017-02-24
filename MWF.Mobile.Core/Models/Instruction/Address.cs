using System;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Models.Attributes;
using Newtonsoft.Json;
using MWF.Mobile.Core.Converters;

namespace MWF.Mobile.Core.Models.Instruction
{
    public class Address : IBlueSphereEntity
    {

        private string[] _linesArray;

        public Address()
        {
            ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]
        public Guid ID { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("type")]
        public InstructionType Type { get; set; }
        [JsonProperty("line")]
        [Ignore]
        public string[] LinesArray { get { return _linesArray; } set { _linesArray = value; Lines = string.Join("|", _linesArray); } }
        public string Lines { get; set; }
        [JsonProperty("postcode")]
        public string Postcode { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("arrive")]
        [JsonConverter(typeof(JsonMultiFormatDateTimeConverter))]
        public DateTime Arrive { get; set; }
        [JsonProperty("depart")]
        [JsonConverter(typeof(JsonMultiFormatDateTimeConverter))]
        public DateTime Depart { get; set; }

        [ForeignKey(typeof(Order))]
        public Guid OrderId { get; set; }
    }
}