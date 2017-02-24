using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Converters;
using MWF.Mobile.Core.Models.Instruction;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models.GatewayServiceResponse
{
    [JsonConverter(typeof(JsonWrappedItemConverter<MobileDatum>))]
    public class MobileDatum
    {
        [JsonProperty("mobiledata")]
        [JsonConverter(typeof(SingleObjectToListConverter<MobileData>))]
        public List<MobileData> List { get; set; }
    }

}
