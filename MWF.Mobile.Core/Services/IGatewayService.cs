using MWF.Mobile.Core.Models.GatewayServiceRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{

    public interface IGatewayService
    {
        Task<Models.ApplicationProfile> GetApplicationProfileAsync();
        Task<Models.Device> GetDeviceAsync(string customerID);
        Task<IEnumerable<Models.Driver>> GetDriversAsync();
        Task<IEnumerable<Models.SafetyProfile>> GetSafetyProfilesAsync();
        Task<IEnumerable<Models.Vehicle>> GetVehiclesAsync(string vehicleViewTitle);
        Task<IEnumerable<Models.VehicleView>> GetVehicleViewsAsync();
        Task<Models.VerbProfile> GetVerbProfileAsync(string verbProfileTitle);
        Task<IEnumerable<Models.Instruction.MobileData>> GetDriverInstructionsAsync(string vehicleRegistration,
                                                                               Guid driverTitle, 
                                                                               DateTime startDate,
                                                                               DateTime endDate);
        Task<Models.MWFMobileConfig> GetConfigAsync();
        Task<bool> CreateDeviceAsync();
        Task<HttpResult> PostLogMessageAsync(DeviceLogMessage log);
        Task<bool> LicenceCheckAsync(Guid driverID);
    }

}
