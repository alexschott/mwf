using System;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models
{

    public enum DeviceType
    {

        BlueScout = 1,
 
        BluePort = 2,

        Any = 10
    }

    [JsonConverter(typeof(JsonWrappedItemConverter<Device>))]
    public class Device : IBlueSphereEntity
    {
    
        [Unique]
        [PrimaryKey]
        [JsonProperty("@id")]
        public Guid ID { get; set; }

        [JsonProperty("@title")]
        public string Title { get; set; }

        [JsonProperty("@type")]
        public DeviceType Type { get; set; }

        [JsonProperty("@identifier")]
        public string DeviceIdentifier { get; set; }

        [JsonProperty("@customer_id")]
        public Guid CustomerID{ get; set; }

        [JsonProperty("@customer_title")]
        public string CustomerTitle { get; set; }

    }
    
}
