using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class CheckOutViewModel : BaseFragmentViewModel, IBackButtonHandler
    {
        private readonly ICloseApplication _closeApplication;
        private readonly INavigationService _navigationService;

        public CheckOutViewModel(ICloseApplication closeApplication,
            INavigationService navigationService)
        {
            _closeApplication = closeApplication;
            _navigationService = navigationService;
        }

        public string CheckOutLabel
        {
            get { return "Check Out"; }
        }

        public string CheckOutMessage
        {
            get { return "Before you can use this application this device needs to be checked out"; }
        }

        public string CheckOutButtonLabel
        {
            get { return "Check Out"; }
        }

        public override string FragmentTitle
        {
            get { return "Check Out"; }
        }

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

        private MvxCommand _checkOutCommand;
        public System.Windows.Input.ICommand CheckOutCommand
        {
            get { return (_checkOutCommand = _checkOutCommand ?? new MvxCommand(async () => await this.MoveToNextAsync())); }
        }

        private MvxCommand _sendDiagnosticsCommand;
        public System.Windows.Input.ICommand SendDiagnosticsCommand
        {
            get { return (_sendDiagnosticsCommand = _sendDiagnosticsCommand ?? new MvxCommand(async () => await this.SendDiagnosticsAsync())); }
        }

        public Task MoveToNextAsync()
        {
            NavData<object> navData = new NavData<object>();
            return _navigationService.MoveToNextAsync(navData);
        }

        public Task SendDiagnosticsAsync()
        {
            NavData<object> navData = new NavData<object>();
            navData.OtherData["Diagnostics"] = true;
            return _navigationService.MoveToNextAsync(navData);
        }
    }
}
