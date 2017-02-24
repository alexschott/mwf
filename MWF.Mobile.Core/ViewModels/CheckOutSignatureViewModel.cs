using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class CheckOutSignatureViewModel : BaseFragmentViewModel, IBackButtonHandler
    {
        private readonly ICloseApplication _closeApplication;
        private readonly INavigationService _navigationService;
        private readonly IRepositories _repositories;

        private string _driverName;
        private string _driverSignature;        

        public CheckOutSignatureViewModel(ICloseApplication closeApplication,
            INavigationService navigationService, IRepositories repositories)
        {
            _closeApplication = closeApplication;
            _navigationService = navigationService;
            _repositories = repositories;
        }

        public string DriverName
        {
            get { return _driverName; }
            set
            {
                _driverName = value;
                if (string.IsNullOrEmpty(_driverName) || string.IsNullOrWhiteSpace(_driverName))
                    Mvx.Resolve<ICustomUserInteraction>().AlertAsync("To complete, please enter your name");
            }
        }

        public string DriverSignature
        {
            get { return _driverSignature; }
            set
            {
                _driverSignature = value;
                if (string.IsNullOrEmpty(_driverSignature) || string.IsNullOrWhiteSpace(_driverSignature))
                    Mvx.Resolve<ICustomUserInteraction>().AlertAsync("To continue, please sign on the pad");                
            }
        }

        public override string FragmentTitle
        {
            get { return "Sign Device Out"; }
        }

        public string Message
        {
            get { return "Enter your name and signature and click Complete"; }
        }

        public string NameText
        {
            get { return "Name"; }
        }

        public string CompleteButtonLabel
        {
            get { return "Complete"; }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
        }

        public string ProgressTitle
        {
            get { return "Checking Out..."; }
        }

        public string ProgressMessage
        {
            get { return "Checking out the device. Please wait."; }
        }

        public async Task<bool> OnBackButtonPressedAsync()
        {
            await Task.Run(() => ShowViewModel<CheckOutTermsViewModel>());
            return false;
        }

        private MvxCommand _completeCommand;
        public System.Windows.Input.ICommand CompleteCommand
        {
            get { return (_completeCommand = _completeCommand ?? new MvxCommand(async () => await this.MoveToNextAsync())); }
        }

        private MvxCommand _sendDiagnosticsCommand;
        public System.Windows.Input.ICommand SendDiagnosticsCommand
        {
            get { return (_sendDiagnosticsCommand = _sendDiagnosticsCommand ?? new MvxCommand(async () => await this.SendDiagnosticsAsync())); }
        }

        public async Task MoveToNextAsync()
        {
            if (string.IsNullOrEmpty(DriverName) || string.IsNullOrWhiteSpace(DriverName))
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("To complete, please enter your name");

            if (string.IsNullOrEmpty(DriverSignature) || string.IsNullOrWhiteSpace(DriverSignature))
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("To continue, please sign on the pad");

            if (!string.IsNullOrEmpty(DriverName) && !string.IsNullOrWhiteSpace(DriverName) &&
                !string.IsNullOrEmpty(DriverSignature) && !string.IsNullOrWhiteSpace(DriverSignature))
            {
                IsBusy = true;
                NavData<Models.CheckInOutData> navData = _navigationService.CurrentNavData as NavData<Models.CheckInOutData>;
                navData.Data.actualActionPerformed = CheckInOutActions.CheckOut;
                navData.Data.actualIMEI = Mvx.Resolve<IDeviceInfo>().IMEI;
                navData.Data.signature = DriverSignature;
                navData.Data.driverName = DriverName;

                CheckInOutService service = new CheckInOutService(_repositories);
                HttpResult result = await service.CheckOutDevice(navData.Data);
                IsBusy = false;
                if (result.Succeeded)
                {
                    await _navigationService.MoveToNextAsync();
                }
                else if (result.StatusCode == System.Net.HttpStatusCode.NotAcceptable)
                {
                    await Mvx.Resolve<ICustomUserInteraction>().AlertAsync(
                        "The QR code data and device details did not match. Please ensure that you are checking out the correct device.");
                }
                else if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    await Mvx.Resolve<ICustomUserInteraction>().AlertAsync(
                        "Could not complete Check out process because of server error. Please try again later.");
                }
                else
                {
                    await Mvx.Resolve<ICustomUserInteraction>().AlertAsync(
                        "Unable to communicate with Device management. Please ensure mobile data is ON.");
                }
            }
        }

        public Task SendDiagnosticsAsync()
        {
            NavData<object> navData = new NavData<object>();
            navData.OtherData["Diagnostics"] = true;
            return _navigationService.MoveToNextAsync(navData);
        }
    }
}
