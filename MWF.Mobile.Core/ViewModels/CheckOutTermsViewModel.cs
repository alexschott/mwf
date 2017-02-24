using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class CheckOutTermsViewModel : BaseFragmentViewModel, IBackButtonHandler
    {
        private readonly ICloseApplication _closeApplication;
        private readonly INavigationService _navigationService;
        private readonly IRepositories _repositories;

        private string _termsAndConditions = string.Empty;

        public CheckOutTermsViewModel(ICloseApplication closeApplication,
            INavigationService navigationService, IRepositories repositories)
        {
            _closeApplication = closeApplication;
            _navigationService = navigationService;
            _repositories = repositories;

            setTermsAndConditions();
        }

        public override string FragmentTitle
        {
            get { return "Terms and Conditions"; }
        }

        public string TermsAndConditionsLabel
        {
            get { return "Terms And Conditions"; }
        }

        public string TermsAndConditions
        {
            get { return _termsAndConditions; }
            private set
            {
                _termsAndConditions = value;
                RaisePropertyChanged(() => TermsAndConditions);
            }
        }

        public string AcceptTnC
        {
            get { return "I accept the terms and conditions"; }
        }

        public string ContinueButtonLabel
        {
            get { return "Continue"; }
        }

        public bool IsAccepted { get; set; }

        public async Task<bool> OnBackButtonPressedAsync()
        {
            await Task.Run(() => ShowViewModel<CheckOutQRCodeViewModel>());
            return false;
        }

        private MvxCommand _continueCommand;
        public System.Windows.Input.ICommand ContinueCommand
        {
            get { return (_continueCommand = _continueCommand ?? new MvxCommand(async () => await this.MoveToNextAsync())); }
        }

        private MvxCommand _sendDiagnosticsCommand;
        public System.Windows.Input.ICommand SendDiagnosticsCommand
        {
            get { return (_sendDiagnosticsCommand = _sendDiagnosticsCommand ?? new MvxCommand(async () => await this.SendDiagnosticsAsync())); }
        }

        public Task MoveToNextAsync()
        {
            if (!IsAccepted)
                return Mvx.Resolve<ICustomUserInteraction>().AlertAsync("To continue, please accept the terms and conditions");

            NavData<Models.CheckInOutData> navData = _navigationService.CurrentNavData as NavData<Models.CheckInOutData>;
            if (navData == null) navData = new NavData<Models.CheckInOutData>();
            navData.Data.termsAndConditions = this.TermsAndConditions;
            return _navigationService.MoveToNextAsync(navData);
        }

        public Task SendDiagnosticsAsync()
        {
            NavData<object> navData = new NavData<object>();
            navData.OtherData["Diagnostics"] = true;
            return _navigationService.MoveToNextAsync(navData);
        }

        private async Task setTermsAndConditions()
        {
            var appProfile = await _repositories.ApplicationRepository.GetAsync();
            TermsAndConditions = appProfile.DeviceCheckOutTermsAndConditions;
        }
    }
}
