using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.Enums;

namespace MWF.Mobile.Core.ViewModels
{

    public class PasscodeViewModel
        : BaseFragmentViewModel, IBackButtonHandler
    {
        private readonly string _progressTitleForPasscode = "Checking Passcode...";
        private readonly string _progressMessageForPasscode = "Your driver passcode is being checked. This can take up to 5 minutes.";

        private readonly string _progressTitleForCheckIn = "Status check...";
        private readonly string _progressMessageForCheckIn = "Checking the check out status of the device.";

        private readonly ICurrentDriverRepository _currentDriverRepository = null;
        private readonly IAuthenticationService _authenticationService = null;
        private readonly IInfoService _infoService = null;
        private readonly ICloseApplication _closeApplication;
        private readonly INavigationService _navigationService;
        private readonly ILoggingService _loggingService;
        private readonly IGatewayQueuedService _gatewayQueuedService;
        private readonly IRepositories _repositories;
        private bool _isBusy = false;
        private bool _checkInButtonVisible = false;
        private string _progressTitle;
        private string _progressMessage;

        public PasscodeViewModel(
            IAuthenticationService authenticationService, 
            IInfoService infoService,
            ICloseApplication closeApplication, 
            IRepositories repositories, 
            INavigationService navigationService,
            ILoggingService loggingService,
            IGatewayQueuedService gatewayQueuedService)
        {
            _authenticationService = authenticationService;
            _infoService = infoService;
            _navigationService = navigationService;
            _loggingService = loggingService;

            _repositories = repositories;
            _currentDriverRepository = repositories.CurrentDriverRepository;
            _closeApplication = closeApplication;
            _gatewayQueuedService = gatewayQueuedService;

            Task.Run(() => setCheckInButtonVisibility()).Wait();
        }

        #region Public Members

        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
        }

        public string PasscodeLabel
        {
            get { return "Driver Passcode"; }
        }

        public string PasscodeButtonLabel
        {
            get { return "Submit"; }
        }

        public string CheckInButtonLabel
        {
            get { return "Check In"; }
        }

        public bool CheckInButtonVisible
        {
            get { return _checkInButtonVisible; }
            private set
            {
                _checkInButtonVisible = value;
                RaisePropertyChanged(() => CheckInButtonVisible);
            }
        }

        public string VersionText
        {
            get { return string.Format("Version: {0}           DeviceID: {1}", Mvx.Resolve<IDeviceInfo>().SoftwareVersion, Mvx.Resolve<IDeviceInfo>().IMEI); }
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

        private string _passcode = null;
        public string Passcode
        {
            get { return _passcode; }
            set { _passcode = value; RaisePropertyChanged(() => Passcode); }
        }

        private MvxCommand _loginCommand;
        public System.Windows.Input.ICommand LoginCommand
        {
            get { return (_loginCommand = _loginCommand ?? new MvxCommand(async () => await this.LoginAsync())); }
        }

        private MvxCommand _sendDiagnosticsCommand;
        public System.Windows.Input.ICommand SendDiagnosticsCommand
        {
            get { return (_sendDiagnosticsCommand = _sendDiagnosticsCommand ?? new MvxCommand(async () => await this.SendDiagnosticsAsync()));}
        }

        private MvxCommand _checkInCommand;
        public System.Windows.Input.ICommand CheckInCommand
        {
            get { return (_checkInCommand = _checkInCommand ?? new MvxCommand(async () => await this.CheckInDeviceAsync())); }
        }

        public override string FragmentTitle
        {
            get { return "Passcode"; }
        }

        #endregion Public Members

        #region Private Methods

        private async Task setCheckInButtonVisibility()
        {
            var appProfile = await _repositories.ApplicationRepository.GetAsync();
            CheckInButtonVisible = appProfile.DeviceCheckInOutRequired;
        }

        public Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Passcode))
            {
                //TODO: probably should additionally implement presentation layer required field validation so we don't even get this far.
                return Mvx.Resolve<ICustomUserInteraction>().AlertAsync("To login, submit a passcode");
            }

            return this.AuthenticateAsync();
        }

        public async Task CheckInDeviceAsync()
        {
            ProgressTitle = _progressTitleForCheckIn;
            ProgressMessage = _progressMessageForCheckIn;
            IsBusy = true;

            CheckInOutService service = new CheckInOutService(_repositories);
            CheckInOutActions status = await service.GetDeviceStatus(Mvx.Resolve<IDeviceInfo>().IMEI);

            IsBusy = false;
            if(status == CheckInOutActions.CheckOut)
            {
                ShowViewModel<CheckInViewModel>();
            }
            else
            {
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("This device is not checked out, so can not be checked in.");
            }
        }

        public Task SendDiagnosticsAsync()
        {
            NavData<object> navData = new NavData<object>();
            navData.OtherData["Diagnostics"] = true;         
            return _navigationService.MoveToNextAsync(navData);
        }

        private async Task AuthenticateAsync()
        {
            AuthenticationResult result;
			LogMessage exceptionMsg = null;

            ProgressTitle = _progressTitleForPasscode;
            ProgressMessage = _progressMessageForPasscode;

            this.IsBusy = true;

            try
            {
                result = await _authenticationService.AuthenticateAsync(this.Passcode);

                if (result.Success)
                {
                    _infoService.SetCurrentDriver(result.Driver);

                    if (await _currentDriverRepository.GetByIDAsync(_infoService.CurrentDriverID.Value) == null)
                    {
                        var newDriver = new CurrentDriver { ID = _infoService.CurrentDriverID.Value };

                        try
                        {
                            await _currentDriverRepository.InsertAsync(newDriver);
                        }
                        catch (Exception ex)
                        {
                            MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "CurrentDriverRepository", "InsertAsync", ex.StackTrace);
                            throw;
                        }
                    }

                    // Start the gateway queue timer which will cause submission of any queued data to the MWF Mobile gateway service on a repeat basis
                    _gatewayQueuedService.StartQueueTimer();
                    
                    await _navigationService.MoveToNextAsync();
                }
            }
            catch (Exception ex)
            {
				exceptionMsg = _loggingService.GetExceptionLogMessage(ex);
                result = new AuthenticationResult() { AuthenticationFailedMessage = "Unable to check your passcode.", Success = false };
            }
            finally
            {
                // clear the passcode
                this.Passcode = string.Empty;
                this.IsBusy = false;
            }

			if (exceptionMsg != null)
				await _loggingService.LogEventAsync(exceptionMsg);

			// Let the user know
            if (!result.Success)
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync(result.AuthenticationFailedMessage);
        }

        #endregion

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            var closeApp = true;

#if DEBUG
            closeApp = !await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("DEBUGGING: Return to Customer Code screen?", cancelButton: "No, close the app");
#endif

            if (closeApp)
                _closeApplication.CloseApp();
            else
                ShowViewModel<CustomerCodeViewModel>();

            return false;
        }

        #endregion IBackButtonHandler Implementation
    }

}
