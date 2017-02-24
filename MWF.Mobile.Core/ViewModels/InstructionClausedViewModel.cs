using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using MWF.Mobile.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MWF.Mobile.Core.ViewModels.Extensions;

namespace MWF.Mobile.Core.ViewModels
{
    public class InstructionClausedViewModel
        : BaseInstructionNotificationViewModel
    {

        #region Private Properties

        private MobileData _mobileData;
        private NavData<MobileData> _navData;
        private readonly INavigationService _navigationService;

        #endregion Private Properties

        #region Construction

        public InstructionClausedViewModel(INavigationService navigationService, IRepositories repositories)
        {
            _navigationService = navigationService;
        }

        public void Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            _mobileData = _navData.Data;
        }

        #endregion Construction

        #region Public Properties

        public string PhoneSentenceText
        {
            get
            {
                return "Please call the office to confirm details";
            }
        }

        public string PhotoSentenceText
        {
            get
            {
                return "Use the photo button to take one or more photos of the affected delivery";
            }
        }

        public string AdvanceButtonText
        {
            get
            {
                return "Continue";
            }
        }

        private MvxCommand _openCameraScreenCommand;
        public ICommand OpenCameraScreenCommand
        {
            get
            {
                return (_openCameraScreenCommand = _openCameraScreenCommand ?? new MvxCommand(() => OpenCameraScreen()));
            }
        }

        private MvxCommand _launchPhoneAppCommand;
        public ICommand LaunchPhoneAppCommand
        {
            get
            {
                return (_launchPhoneAppCommand = _launchPhoneAppCommand ?? new MvxCommand(() => LaunchPhoneApp()));
            }
        }

        private MvxCommand _advanceInstructionCommand;
        public ICommand AdvanceInstructionCommand
        {
            get
            {
                return (_advanceInstructionCommand = _advanceInstructionCommand ?? new MvxCommand(async () => await this.AdvanceInstructionAsync()));
            }
        }

        #endregion Public Properties

        #region Private Methods

        private Task AdvanceInstructionAsync()
        {
            var dataChunks = _navData.GetAllDataChunks();
            foreach (var datachunk in dataChunks)
            {
                datachunk.IsClaused = true;
            }

            return _navigationService.MoveToNextAsync(_navData);
        }

        private void LaunchPhoneApp()
        {
            Mvx.Resolve<ILaunchPhone>().Launch();
        }

        private void OpenCameraScreen()
        {
            var modal = _navigationService.ShowModalViewModel<ModalCameraViewModel, bool>(_navData, (sendChunk) => {});
        }

        #endregion Private Methods

        #region BaseInstructionNotificationViewModel

        public override Task CheckInstructionNotificationAsync(Messages.GatewayInstructionNotificationMessage message)
        {
            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _mobileData = _navData.Data;
                RaiseAllPropertiesChanged();
            });
        }

        #endregion BaseInstructionNotificationViewModel

        #region BaseFragmentViewModel Overrides

        public override string FragmentTitle
        {
            get { return "Claused Delivery"; }
        }

        #endregion BaseFragmentViewModel Overrides

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            await _navigationService.GoBackAsync(_navData);
            return false;
        }

        #endregion IBackButtonHandler Implementation

    }
}
