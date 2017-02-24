using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models.GatewayServiceRequest
{

    public abstract class BaseContent
    {
        public Guid? MessageID { get; set; }
        public string DeviceIdentifier { get; set; }
        public string Password { get; set; }
        public string MobileApplication { get; set; }
        public string Version { get; set; }
        public Guid? DriverID { get; set; }
    }

}
