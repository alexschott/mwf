using System;
using SQLite.Net.Attributes;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models
{

    public class Driver : IBlueSphereEntity
    {

        [Unique]
        [PrimaryKey]
        [JsonProperty("@id")]
        public Guid ID { get; set; }

        [JsonProperty("@title")]
        public string Title { get; set; }

        [JsonProperty("@intlink")]
        public int IntLink { get; set; }

        [JsonProperty("@firstname")]
        public string FirstName { get; set; }

        [JsonProperty("@lastname")]
        public string LastName { get; set; }

        [JsonProperty("@passcode")]
        public string Passcode { get; set; }

        [Ignore]
        public string DisplayName { get { return string.Format("{0} {1}", FirstName, LastName); } }

        public bool IsLicensed { get; set; }

    }

}
