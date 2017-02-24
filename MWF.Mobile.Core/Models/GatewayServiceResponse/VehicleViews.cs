using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models.GatewayServiceResponse
{
    [JsonConverter(typeof(JsonWrappedItemConverter<VehicleViews>))]
    public class VehicleViews
    {
        [JsonProperty("vehicleview")]
        [JsonConverter(typeof(SingleObjectToListConverter<VehicleView>))]
        public List<VehicleView> List { get; set; }
    }

}
