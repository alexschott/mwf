using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Extensions;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;

namespace MWF.Mobile.Core.ViewModels
{

    public class CustomerCodeViewModel : BaseFragmentViewModel, IBackButtonHandler
    {
        private readonly string _progressTitleForCustomerCode = "Downloading Data...";
        private readonly string _progressMessageForCustomerCode = "Your customer code is being checked to set-up your device. This can take up to 5 minutes.";

        private readonly string _progressTitleForCheckIn = "Status check...";
        private readonly string _progressMessageForCheckIn = "Checking the check out status of the device.";

        private readonly Services.IGatewayService _gatewayService;
        private readonly Services.IDataService _dataService;
        private readonly IReachability _reachability;
        private readonly ICustomUserInteraction _userInteraction;
        private readonly INavigationService _navigationService;

        private readonly IApplicationProfileRepository _applicationProfileRepository;
        private readonly ICustomerRepository _customerRepository; 
        private readonly IDeviceRepository _deviceRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly ISafetyProfileRepository _safetyProfileRepository;
        private readonly ITrailerRepository _trailerRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVerbProfileRepository _verbProfileRepository;
        private readonly IConfigRepository _configRepository;
        private readonly ICloseApplication _closeApplication;

        private string _progressTitle;
        private string _progressMessage;

        public CustomerCodeViewModel(IGatewayService gatewayService, IReachability reachability, IDataService dataService, IRepositories repositories, ICustomUserInteraction userInteraction, INavigationService navigationService, ICloseApplication closeApplication)
        {
            _gatewayService = gatewayService;
            _dataService = dataService;
            _reachability = reachability;
            _userInteraction = userInteraction;
            _navigationService = navigationService;

            _applicationProfileRepository = repositories.ApplicationRepository;
            _customerRepository = repositories.CustomerRepository;
            _deviceRepository = repositories.DeviceRepository;
            _driverRepository = repositories.DriverRepository;
            _safetyProfileRepository = repositories.SafetyProfileRepository;
            _trailerRepository = repositories.TrailerRepository;
            _vehicleRepository = repositories.VehicleRepository;
            _verbProfileRepository = repositories.VerbProfileRepository;
            _configRepository = repositories.ConfigRepository;
            _closeApplication = closeApplication;
        }

        public async Task Init()
        {
#if DEBUG
            await this.ClearAllDataAsync();

            var debugCustomerCodes = new Dictionary<string, string>
            {
                { "Firmin", "A2A67DE7-DC95-49D9-BF53-34829CF865C9" },
                { "MWF Dev", "C697166B-2E1B-45B0-8F77-270C4EADC031" },
                { "Proteo Demo", "DB78C027-CA85-4DF3-A25A-760562C4EEB5" },
                { "Fagan & Whalley", "B448BEF1-9320-4AAB-A529-06605A288A96" },
                { "Bomfords", "0DA9F71D-3A0F-4351-BCE1-A49D2D9AECC1" },
            };

            var debugSuggestedCustomer = "Firmin";

            if (await _userInteraction.ConfirmAsync(string.Format("DEBUGGING: use the {0} customer code?", debugSuggestedCustomer)))
                this.CustomerCode = debugCustomerCodes[debugSuggestedCustomer];
#endif
        }

        public override string FragmentTitle { get { return "Customer Code"; } }

        private string _customerCode = string.Empty;
        public string CustomerCode
        {
            get { return _customerCode; }
            set { _customerCode = value; RaisePropertyChanged(() => CustomerCode); }
        }

        public string EnterButtonLabel
        {
            get { return "Submit"; }
        }

        public string CustomerCodeLabel
        {
            get { return "Customer Code"; }
        }

        public string VersionText
        {
            get { return string.Format("Version: {0}           DeviceID: {1}",Mvx.Resolve<IDeviceInfo>().SoftwareVersion, Mvx.Resolve<IDeviceInfo>().IMEI); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
        }

        public string ProgressTitle
        {
            get { return _progressTitle; }
            set
            {
                _progressTitle = value;
                RaisePropertyChanged(() => ProgressTitle);
            }
        }

        public string ProgressMessage
        {
            get { return _progressMessage; }
            set
            {
                _progressMessage = value;
                RaisePropertyChanged(() => ProgressMessage);
            }
        }

        private MvxCommand _enterCodeCommand;
        public System.Windows.Input.ICommand EnterCodeCommand
        {
            get
            {
                _enterCodeCommand = _enterCodeCommand ?? new MvxCommand(async () => await this.EnterCodeAsync());
                return _enterCodeCommand;
            }
        }

        private string _errorMessage;
        private string _unexpectedErrorMessage = "Unfortunately, there was a problem setting up your device, try restarting the device and try again.";

        public async Task EnterCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(this.CustomerCode))
            {
                //TODO: probably should additionally implement presentation layer required field validation so we don't even get this far.
                await _userInteraction.AlertAsync("To register this device, submit a customer code");
                return;
            }

            if (!_reachability.IsConnected())
            {
                await _userInteraction.AlertAsync("To set-up this device, a connection to the internet is required");
            }
            else
            {
                bool success = false;
                ProgressTitle = _progressTitleForCustomerCode;
                ProgressMessage = _progressMessageForCustomerCode;

                this.IsBusy = true;

                try
                {
                    success = await this.SetupDeviceAsync();
                }
                catch (Exception ex)
                {
                    MvxTrace.Warning("Exception while setting up device: {0} at {1}", ex.Message, ex.StackTrace);
                    success = false;
                    _errorMessage = _unexpectedErrorMessage;
                }
                finally
                {
                    this.IsBusy = false;
                }

                if (success)
                {
                    ProgressTitle = _progressTitleForCheckIn;
                    ProgressMessage = _progressMessageForCheckIn;
                    IsBusy = true;
                    await _navigationService.MoveToNextAsync();
                    IsBusy = false;
                }
                else
                    await _userInteraction.AlertAsync(_errorMessage);
            }
        }

        // returns false if the customer code is not known
        // throws exceptions if the web services or db inserts fail
        private async Task<bool> SetupDeviceAsync()
        {
            if (!await _gatewayService.CreateDeviceAsync())
            {
                _errorMessage = _unexpectedErrorMessage;
                return false;
            }

            var device = await _gatewayService.GetDeviceAsync(CustomerCode);
            if (device == null)
            {
                _errorMessage = "The customer passcode you submitted doesn't exist, check the passcode and try again.";
                return false;
            }
            var config = await _gatewayService.GetConfigAsync();
            var applicationProfile = await _gatewayService.GetApplicationProfileAsync();
            var drivers = await _gatewayService.GetDriversAsync();
            var vehicleViews = await _gatewayService.GetVehicleViewsAsync();
            var safetyProfiles = await _gatewayService.GetSafetyProfilesAsync();

            var vehicleViewVehicles = new Dictionary<string, IEnumerable<Models.BaseVehicle>>(vehicleViews.Count());

            foreach (var vehicleView in vehicleViews)
            {
                vehicleViewVehicles.Add(vehicleView.Title, await _gatewayService.GetVehiclesAsync(vehicleView.Title));
            }

            var vehiclesAndTrailers = vehicleViewVehicles.SelectMany(vvv => vvv.Value).DistinctBy(v => v.ID);
            var vehicles = vehiclesAndTrailers.Where(bv => !bv.IsTrailer).Select(bv => new Models.Vehicle(bv));
            var trailers = vehiclesAndTrailers.Where(bv => bv.IsTrailer).Select(bv => new Models.Trailer(bv));

            //TODO: I'm not sure whether verb profiles will be used anywhere within this app, or whether this is a hangover from the Tough Touch implementation? If not used on completion of the project then remove from here and local db.
            // TODO: Get verb profile titles from config or somewhere?
            var verbProfileTitles = new[] { "Palletforce", "Cancel", "Complete", "Suspend" };
            var verbProfiles = new List<Models.VerbProfile>(verbProfileTitles.Count());
            
            foreach (var verbProfileTitle in verbProfileTitles)
            {
                var verbProfile = await _gatewayService.GetVerbProfileAsync(verbProfileTitle);
                if (verbProfile != null) 
                    verbProfiles.Add(verbProfile);
            }

            await _dataService.RunInTransactionAsync(c => 
            {
                //TODO: Store the customer title? Need to get the customer title from somewhere.
                _customerRepository.Insert(new Customer() { ID = Guid.NewGuid(), CustomerCode = CustomerCode }, c);
                _deviceRepository.Insert(device, c);
                _verbProfileRepository.Insert(verbProfiles, c);
                _applicationProfileRepository.Insert(applicationProfile, c);
                _driverRepository.Insert(drivers, c);
                //TODO: relate Vehicles to VehicleViews?  Are VehicleViews actually used for anything within the app?
                _vehicleRepository.Insert(vehicles, c);
                _trailerRepository.Insert(trailers, c);
                _safetyProfileRepository.Insert(safetyProfiles, c);
                _configRepository.Insert(config, c);
            });

            //TODO: call fwRegisterDevice - what does this actually do?

            return true;
        }

        private async Task ClearAllDataAsync()
        {
            await _customerRepository.DeleteAllAsync();
            await _deviceRepository.DeleteAllAsync();
            await _verbProfileRepository.DeleteAllAsync();
            await _applicationProfileRepository.DeleteAllAsync();
            await _driverRepository.DeleteAllAsync();
            await _vehicleRepository.DeleteAllAsync();
            await _trailerRepository.DeleteAllAsync();
            await _safetyProfileRepository.DeleteAllAsync();
            await _configRepository.DeleteAllAsync();
        }

        #region IBackButtonHandler Implementation

        public Task<bool> OnBackButtonPressedAsync()
        {
            // Always close the app on Back button press on this screen
            // (in Debug mode the user may have been routed to here from the passcode screen, but Back button should not navigate back to there)
            _closeApplication.CloseApp();
            return Task.FromResult(false);
        }

        #endregion IBackButtonHandler Implementation

    }

}
