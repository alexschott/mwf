using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models.GatewayServiceResponse
{

    [JsonConverter(typeof(JsonWrappedItemConverter<Vehicles>))]
    public class Vehicles
    {
        [JsonProperty("vehicle")]
        [JsonConverter(typeof(SingleObjectToListConverter<Vehicle>))]
        public List<Vehicle> List { get; set; }
    }

}
