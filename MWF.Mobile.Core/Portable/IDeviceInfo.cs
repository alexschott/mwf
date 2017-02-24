using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{
    
    public interface IDeviceInfo
    {
        string GatewayPassword { get; }

        string MobileApplication { get; }

        string GetDeviceIdentifier();

        string IMEI { get; }

        string AndroidId { get; }

        string SerialNumber { get; }

        string OsVersion { get; }

        string Platform { get; }

        string Model { get; }

        string Manufacturer { get; }

        string SoftwareVersion { get; }

        string DatabasePath { get; }
    }

}
