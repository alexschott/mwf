using System;
using SQLite.Net.Attributes;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models
{
    public class Customer : IBlueSphereEntity
    {
        [Unique]
        [PrimaryKey]
        [JsonProperty("id")]
        public Guid ID { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        public string CustomerCode { get; set; }
    }
}
