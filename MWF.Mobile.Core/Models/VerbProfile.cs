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

    [JsonConverter(typeof(JsonWrappedItemConverter<VerbProfile>))]
    public class VerbProfile : IBlueSphereEntity
    {

        [Unique]
        [PrimaryKey]
        [JsonProperty("@id")]
        public Guid ID { get; set; }

        [JsonProperty("@t")]
        public string Title { get; set; }

        [JsonProperty("@l")]
        public int IntLink { get; set; }

        [JsonProperty("@c")]
        public string Code { get; set; }

        [ChildRelationship(typeof(VerbProfileItem))]
        [JsonProperty("vpis")]
        [JsonConverter(typeof(JsonWrappedListConverter<VerbProfileItem>))]
        public List<VerbProfileItem> Children { get; set; }

    }

}
