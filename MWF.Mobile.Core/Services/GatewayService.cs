using MWF.Mobile.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.GatewayServiceRequest;
using System.Net;

namespace MWF.Mobile.Core.Services
{
    
    public class GatewayService
        : IGatewayService
    {

        private readonly IDeviceInfo _deviceInfo = null;
        private readonly IHttpService _httpService = null;
        private readonly string _gatewayDeviceRequestUrl = null;
        private readonly string _gatewayDeviceCreateUrl = null;
        private readonly string _gatewayConfigRequestUrl = null;
        private readonly string _gatewayLogMessageUrl = null;
        private readonly string _gatewayLicenceCheckUrl = null;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IInfoService _infoService;
        private readonly IMvxMessenger _messenger = null;

        public GatewayService(IDeviceInfo deviceInfo, IHttpService httpService, IRepositories repositories, IInfoService infoService, IMvxMessenger messenger)
        {
            _deviceInfo = deviceInfo;
            _httpService = httpService;
            _infoService = infoService;
            _messenger = messenger;

            string urlBase = "http://blueport.gateway.fleetwoodmobile.net:7090";

            //TODO: read this from config or somewhere?
            _gatewayDeviceRequestUrl = urlBase + "/api/gateway/devicerequest";
            _gatewayDeviceCreateUrl = urlBase + "/api/gateway/createdevice";
            _gatewayConfigRequestUrl = urlBase + "/api/gateway/configrequest";
            _gatewayLogMessageUrl = urlBase + "/api/gateway/logmessage";
            _gatewayLicenceCheckUrl = urlBase + "/api/gateway/systemcheck";

            //Local url, will need to change your own IP
            //_gatewayDeviceCreateUrl = "http://192.168.3.119:17337/api/gateway/createdevice";
            //_gatewayDeviceRequestUrl = "http://192.168.3.119:17337/api/gateway/devicerequest";
            //_gatewayLogMessageUrl = "http://192.168.3.119:17337/api/gateway/logmessage";
            //_gatewayConfigRequestUrl = "http://192.168.3.119:17337/api/gateway/configrequest";
            //_gatewayLicenceCheckUrl = "http://192.168.3.119:17337/api/gateway/systemcheck";


            _deviceRepository = repositories.DeviceRepository;
        }

        #region Public Methods

        public async Task<Models.ApplicationProfile> GetApplicationProfileAsync()
        {
            //TODO: work out what BlueSphere's doing here with the MobileApplicationProfileIntLink parameter
            var parameters = new[] { new Models.GatewayServiceRequest.Parameter { Name = "MobileApplicationProfileIntLink", Value = "0" }, };
            var data = await ServiceCallAsync<Core.Models.ApplicationProfile>("fwGetApplicationProfile", parameters);
            return data.Result;
        }

        public async Task<Models.MWFMobileConfig> GetConfigAsync()
        {
            var deviceInfo = new DeviceInfo()
            {
                DeviceIdentifier = _deviceInfo.GetDeviceIdentifier(),
                Password = _deviceInfo.GatewayPassword
            };
            var response = await _httpService.PostAsJsonAsync<DeviceInfo, MWFMobileConfig>(deviceInfo, _gatewayConfigRequestUrl);
            response.Content.FtpUrl = "ftp://updates.proteotoughtouch.com";
            response.Content.FtpUsername = "TTUpdate";
            response.Content.FtpPassword = "proteo.tough.touch";
            response.Content.FtpPort = 21;

            return response.Content;
        }

        public async Task<bool> CreateDeviceAsync()
        {
            var deviceInfo = new DeviceInfo
            {
                IMEI = _deviceInfo.IMEI,
                DeviceIdentifier = _deviceInfo.GetDeviceIdentifier(),
                OsVersion = _deviceInfo.OsVersion,
                Manufacturer = _deviceInfo.Manufacturer,
                Model = _deviceInfo.Model,
                Platform = _deviceInfo.Platform,
                SoftwareVersion = _deviceInfo.SoftwareVersion,
                Password = _deviceInfo.GatewayPassword,
            };

            var response = await _httpService.PostAsJsonAsync<DeviceInfo, HttpStatusCode>(deviceInfo, _gatewayDeviceCreateUrl);
            return (response.StatusCode != HttpStatusCode.InternalServerError);
        }


        public async Task<Models.Device> GetDeviceAsync(string customerID)
        {
            var parameters = new[] { new Models.GatewayServiceRequest.Parameter { Name = "CustomerID", Value = customerID } };
            var data = await ServiceCallAsync<Core.Models.Device>("fwGetDevice", parameters);
            return data.Result;
        }

        public async Task<IEnumerable<Models.Driver>> GetDriversAsync()
        {
            var data = await ServiceCallAsync<Models.GatewayServiceResponse.Drivers>("fwGetDrivers");
            return data.Result == null ? Enumerable.Empty<Models.Driver>() : data.Result.List;
        }

        public async Task<IEnumerable<Models.SafetyProfile>> GetSafetyProfilesAsync()
        {
            var data = await ServiceCallAsync<Models.GatewayServiceResponse.SafetyProfiles>("fwGetSafetyProfiles");
            return data.Result == null ? Enumerable.Empty<Models.SafetyProfile>() : data.Result.List;
        }

        public async Task<IEnumerable<Models.Vehicle>> GetVehiclesAsync(string vehicleViewTitle)
        {
            try
            {
                var parameters = new[] { new Models.GatewayServiceRequest.Parameter { Name = "VehicleView", Value = vehicleViewTitle } };
                var data = await ServiceCallAsync<Models.GatewayServiceResponse.Vehicles>("fwGetVehicles", parameters);
                return data.Result == null ? Enumerable.Empty<Models.Vehicle>() : data.Result.List;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Models.VehicleView>> GetVehicleViewsAsync()
        {
            var data = await ServiceCallAsync<Models.GatewayServiceResponse.VehicleViews>("fwGetVehicleViews");
            return data.Result == null ? Enumerable.Empty<Models.VehicleView>() : data.Result.List;
        }

        public async Task<Models.VerbProfile> GetVerbProfileAsync(string verbProfileTitle)
        {
            var parameters = new[] { new Models.GatewayServiceRequest.Parameter { Name = "VerbProfileTitle", Value = verbProfileTitle } };
            var data = await ServiceCallAsync<Core.Models.VerbProfile>("fwGetVerbProfile", parameters);
            return data.Result;
        }

        public async Task<IEnumerable<Models.Instruction.MobileData>> GetDriverInstructionsAsync(string vehicleRegistration, 
                                                                                 Guid driverTitle,
                                                                                 DateTime startDate,
                                                                                 DateTime endDate)
        {
            var parameters = new[]
            {
                new Parameter { Name = "VehicleRegistration", Value = vehicleRegistration },
                new Parameter { Name = "DriverTitle", Value = driverTitle.ToString() }, 
                new Parameter { Name = "StartDate", Value = startDate.Date.ToString("yyyy-MM-dd HH:mm:ss") }, 
                new Parameter { Name = "EndDate", Value = endDate.Date.ToString("yyyy-MM-dd HH:mm:ss") }, 
            };
            var data = await ServiceCallAsync<Models.GatewayServiceResponse.MobileDatum>("fwSyncFromServer", parameters);
            return data.Result == null ? Enumerable.Empty<Models.Instruction.MobileData>() : data.Result.List;
        }

        public async Task<HttpResult> PostLogMessageAsync(DeviceLogMessage deviceMessage)
        {
            var response = await _httpService.PostAsJsonAsync<DeviceLogMessage, HttpStatusCode>(deviceMessage, _gatewayLogMessageUrl);
            return response;
        }

        public async Task<bool> LicenceCheckAsync(Guid driverID)
        {
            LicenceCheckMessage licenceCheckMessage = new LicenceCheckMessage() { DriverID = driverID };

            var response = await _httpService.PostAsJsonAsync<LicenceCheckMessage, HttpStatusCode>(licenceCheckMessage, _gatewayLicenceCheckUrl);
            return response.Succeeded;
        }

        #endregion Public Methods

        #region Private Methods

        private class ServiceCallResult<T>
        {
            public T Result { get; set; }
            public IEnumerable<string> Errors { get; set; }
        }

        private async Task<ServiceCallResult<T>> ServiceCallAsync<T>(string command, Parameter[] parameters = null)
            where T: class
        {
            try
            {
                var requestContent = await CreateRequestContentAsync(command, parameters);
                var response = await this.PostAsync<T>(requestContent);

                if (!response.Succeeded && response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _messenger.Publish(new Messages.InvalidLicenseNotificationMessage(this));
                    return new ServiceCallResult<T> { Result = default(T) };
                }

                var responseActions = response.Content.Actions;

                if (!response.Succeeded || responseActions.Count() != 1)
                {
                    throw new Exception("No actions returned from Gateway service call.");
                }

                var responseAction = responseActions.First();

                if (!responseAction.Ack)
                    return new ServiceCallResult<T> { Result = default(T), Errors = responseAction.Errors };

                return new ServiceCallResult<T> { Result = responseAction.Data };
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        private Task<HttpResult<Models.GatewayServiceResponse.Response<TData>>> PostAsync<TData>(Models.GatewayServiceRequest.Content content)
            where TData: class
        {
            return _httpService.PostAsJsonAsync<Models.GatewayServiceRequest.Content, Models.GatewayServiceResponse.Response<TData>>(content, _gatewayDeviceRequestUrl);
        }

        /// <summary>
        /// Create a single-action request's content
        /// </summary>
        private Task<Models.GatewayServiceRequest.Content> CreateRequestContentAsync(string command, IEnumerable<Models.GatewayServiceRequest.Parameter> parameters = null)
        {
            return this.CreateRequestContentAsync(new[]
            {
                new Core.Models.GatewayServiceRequest.Action
                {
                    Command = command,
                    Parameters = parameters,
                }
            });
        }

        /// <summary>
        /// Create the request content, allowing multiple actions per request
        /// </summary>
        private async Task<Models.GatewayServiceRequest.Content> CreateRequestContentAsync(Models.GatewayServiceRequest.Action[] actions)
        {
            Models.Device device = await _deviceRepository.GetAllAsync().ContinueWith(x=> x.Result.FirstOrDefault());
            var deviceIdentifier = device == null ? _deviceInfo.GetDeviceIdentifier() : device.DeviceIdentifier;

            return new Core.Models.GatewayServiceRequest.Content
            {
                DeviceIdentifier = deviceIdentifier,
                Password = _deviceInfo.GatewayPassword,
                MobileApplication = _deviceInfo.MobileApplication,
                Actions = actions,
                DriverID = _infoService.CurrentDriverID,
            };
        }

        #endregion Private Methods


    }

}
