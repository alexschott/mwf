using SQLite.Net.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models
{

    public class CurrentDriver : IBlueSphereEntity
    {

        [Unique]
        [PrimaryKey]
        [JsonProperty("@id")]
        public Guid ID { get; set; }

        public Guid LastVehicleID { get; set; }

    }
}
