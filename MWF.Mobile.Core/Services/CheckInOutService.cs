using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Repositories;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{
    public class CheckInOutService
    {
        //If you change pwd1stHalf below remember to change in Proteo Analytics too 
        private readonly string pwd1stHalf = "{6A50F099-DEA4-4B34-9D2C-73C438D8A005}";

        private HttpService _httpService;
        private IRepositories _repositories;

        public CheckInOutService(IRepositories repositories)
        {
            _repositories = repositories;
            _httpService = new HttpService();            
        }

        public async Task<CheckInOutActions> GetDeviceStatus(string imei)
        {
            try
            {
                var appProfile = await _repositories.ApplicationRepository.GetAsync();
                string deviceStatusUrl = appProfile.DeviceManagementAPIURL + "status?";

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("imei", imei);
                int value = await _httpService.GetWithAuthAsync<int>(parameters, deviceStatusUrl,
                    "ProteoMobile", pwd1stHalf + Guid.NewGuid().ToString());
                return (CheckInOutActions)value;
            }
            catch(Exception ex)
            {
                return CheckInOutActions.ErrorOrDeviceNotFound;
            }
        }

        public async Task<HttpResult> CheckOutDevice(CheckInOutData checkOutData)
        {
            return await performDeviceAction(JsonConvert.SerializeObject(checkOutData));
        }

        public async Task<HttpResult> CheckInDevice(CheckInOutData checkInData)
        {
            return await performDeviceAction(JsonConvert.SerializeObject(checkInData));
        }

        private async Task<HttpResult> performDeviceAction(string jsonContent)
        {
            try
            {
                var appProfile = await _repositories.ApplicationRepository.GetAsync();
                string deviceEventUrl = appProfile.DeviceManagementAPIURL + "recordevent";

                return await _httpService.PostJsonWithAuthAsync(jsonContent, deviceEventUrl,
                    "ProteoMobile", pwd1stHalf + Guid.NewGuid().ToString());
            }
            catch
            {
                return new HttpResult { StatusCode = System.Net.HttpStatusCode.InternalServerError };
            }
        }
    }
}
