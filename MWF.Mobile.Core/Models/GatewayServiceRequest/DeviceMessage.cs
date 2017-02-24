using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models.GatewayServiceRequest
{
    public class DeviceLogMessage
    {
        public string Message { get; set; }
        public string DeviceIdentifier { get; set; }
        public DateTime LogDateTime { get; set; }
    }
}
