using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models.GatewayServiceResponse
{

    [JsonConverter(typeof(JsonWrappedItemConverter<SafetyProfiles>))]
    public class SafetyProfiles
    {
        [JsonProperty("safetyprofile")]
        [JsonConverter(typeof(SingleObjectToListConverter<SafetyProfile>))]
        public List<SafetyProfile> List { get; set; }
    }

}
