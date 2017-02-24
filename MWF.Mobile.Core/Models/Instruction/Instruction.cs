using System;
using System.Collections.Generic;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models.Attributes;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;


namespace MWF.Mobile.Core.Models.Instruction
{
    public class Instruction : IBlueSphereEntity
    {

        private List<string> _linesList;

        public Instruction()
        {
            ID = Guid.NewGuid();
            _linesList = new List<string>();
        }

        [Unique]
        [PrimaryKey]
        public Guid ID { get; set; }

        [JsonProperty("line")]
        [JsonConverter(typeof(SingleObjectToListConverter<string>))]
        [Ignore]
        public List<string> LinesList { get { return _linesList; } set { _linesList = value; Lines = string.Join("\n", _linesList); } }

        public string Lines { get; set; }

        [ForeignKey(typeof(Order))]
        public Guid OrderId { get; set; }
    }
}