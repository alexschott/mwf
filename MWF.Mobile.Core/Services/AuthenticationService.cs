using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;

namespace MWF.Mobile.Core.Services
{

    public class AuthenticationService
        : IAuthenticationService
    {

        #region Private Members

        private IDriverRepository _driverRepository;
        private IReachability _reachability;
        private IGatewayService _gatewayService;

        #endregion

        #region Construction

        public AuthenticationService(IDriverRepository driverRepository, IReachability reachability, IGatewayService gatewayService)
        {
            _driverRepository = driverRepository;
            _reachability = reachability;
            _gatewayService = gatewayService;
        }

        #endregion

        public async Task<AuthenticationResult> AuthenticateAsync(string passcode)
        {
            Mvx.Trace("Looking up passcode in local repository");
            var driver = await GetMatchingDriverAsync(passcode);

            // driver not in local DB, update from BlueSphere (if we can)
            if (driver == null && _reachability.IsConnected())
            {
                Mvx.Trace("Driver not found - refreshing driver list from Gateway");
                await UpdateDriversAsync();
                Mvx.Trace("Driver list updated - looking up passcode in local repository again");
                driver = await GetMatchingDriverAsync(passcode);
            }

            // the passcode doesn't match any driver we know about
            if (driver == null)
                 return new AuthenticationResult { Success = false, AuthenticationFailedMessage = "The driver passcode you submitted doesn't exist, check the passcode and try again." };

            // check if driver is licensed
            if (await IsLicensedAsync(driver))
                return new AuthenticationResult { Success = true, AuthenticationFailedMessage = null,  Driver = driver };
            else
                return new AuthenticationResult { Success = false, AuthenticationFailedMessage = "Request for user license failed. Please contact Proteo for licensing queries." };
        }

        #region Private Methods

        private async Task<bool> IsLicensedAsync(Driver driver)
        {
            if (_reachability.IsConnected())
            {
                driver.IsLicensed = await _gatewayService.LicenceCheckAsync(driver.ID);

                try
                {
                    await _driverRepository.UpdateAsync(driver);
                }
                catch (Exception ex)
                {
                    MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "DriverRepository", "UpdateAsync", ex.StackTrace);
                    throw;
                }
            }

            return driver.IsLicensed;
        }

        private async Task<Driver> GetMatchingDriverAsync(string passcode)
        {
            var data = await _driverRepository.GetAllAsync();
            var driver = data.SingleOrDefault(x => x.Passcode == passcode);
            return driver;
        }

        private async Task UpdateDriversAsync()
        {
            IEnumerable<Driver> drivers = await _gatewayService.GetDriversAsync();
            await _driverRepository.DeleteAllAsync();

            try
            {
                await _driverRepository.InsertAsync(drivers);
            }
            catch (Exception ex)
            {
                MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "DriverRepository", "InsertAsync", ex.StackTrace);
                throw;
            }
        }

        #endregion

    }

}
