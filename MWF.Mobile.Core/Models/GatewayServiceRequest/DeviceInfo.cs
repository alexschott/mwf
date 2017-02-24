using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models.GatewayServiceRequest
{
    public class DeviceInfo
    {
        public string DeviceIdentifier { get; set; }
        public string IMEI { get; set; }
        public string OsVersion { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Platform { get; set; }
        public string SoftwareVersion { get; set; }
        public string Password { get; set; }
    }
}
