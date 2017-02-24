using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Extensions;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;

namespace MWF.Mobile.Core.ViewModels
{
    public class DisplaySafetyCheckViewModel
        : BaseInstructionNotificationViewModel, 
        IBackButtonHandler
    {

        #region Private Members

        private IInfoService _infoService;
        private INavigationService _navigationService;
        private Repositories.IRepositories _repositories;
        private LatestSafetyCheck _latestSafetyCheckData;

        #endregion Private Members

        #region Construction

        public DisplaySafetyCheckViewModel(IInfoService infoService, INavigationService navigationService, Repositories.IRepositories repositories)
        {
            _infoService = infoService;
            _navigationService = navigationService;
            _repositories = repositories;
        }

        public async Task Init()
        {
            _latestSafetyCheckData = await _repositories.LatestSafetyCheckRepository.GetForDriverAsync(_infoService.CurrentDriverID.Value);

            var hasVehicleSafetyCheck = _latestSafetyCheckData.VehicleSafetyCheck != null;
            var hasTrailerSafetyCheck = _latestSafetyCheckData.TrailerSafetyCheck != null;

            //If there is no safety check data to view, then sends them back to where they came.
            if (!hasVehicleSafetyCheck && !hasTrailerSafetyCheck)
            {
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("A safety check profile for your vehicle and/or trailer has not been completed - Refer to your manual safety check.");
                await _navigationService.MoveToNextAsync(_navigationService.CurrentNavData);
                return;
            }

            this.VehicleRegistration = "Vehicle: " + (hasVehicleSafetyCheck ? _latestSafetyCheckData.VehicleSafetyCheck.VehicleRegistration : string.Empty);
            this.TrailerRegistration = "Trailer: " + (hasTrailerSafetyCheck ? _latestSafetyCheckData.TrailerSafetyCheck.VehicleRegistration : string.Empty);
//            this.VehicleSafetyCheckStatus = "Checked: " + (hasVehicleSafetyCheck ? _latestSafetyCheckData.VehicleSafetyCheck.EffectiveDate.ToString("g") : string.Empty);
//            this.TrailerSafetyCheckStatus = "Checked: " + (hasTrailerSafetyCheck ? _latestSafetyCheckData.TrailerSafetyCheck.EffectiveDate.ToString("g") : string.Empty);
            this.VehicleSafetyCheckStatus = "Checked: " + (hasVehicleSafetyCheck ? _latestSafetyCheckData.VehicleSafetyCheck.EffectiveDate.ToUniversalTime().ToString("dd/MM/yyyy HH:mm:ss") : string.Empty);
            this.TrailerSafetyCheckStatus = "Checked: " + (hasTrailerSafetyCheck ? _latestSafetyCheckData.TrailerSafetyCheck.EffectiveDate.ToUniversalTime().ToString("dd/MM/yyyy HH:mm:ss") : string.Empty);

            if (hasVehicleSafetyCheck)
                GenerateSafetyCheckFaultItems(_latestSafetyCheckData.VehicleSafetyCheck.Faults, false);

            if (hasTrailerSafetyCheck)
                GenerateSafetyCheckFaultItems(_latestSafetyCheckData.TrailerSafetyCheck.Faults, true);
        }

        #endregion Construction

        #region Public Properties

        private string _vehicleRegistration;
        public string VehicleRegistration
        {
            get { return _vehicleRegistration; }
            set { _vehicleRegistration = value; RaisePropertyChanged(() => VehicleRegistration); }
        }

        private string _trailerRegistration;
        public string TrailerRegistration
        {
            get { return _trailerRegistration; }
            set { _trailerRegistration = value; RaisePropertyChanged(() => TrailerRegistration); }
        }

        private string _vehicleSafetyCheckStatus;
        public string VehicleSafetyCheckStatus
        {
            get { return _vehicleSafetyCheckStatus; }
            set { _vehicleSafetyCheckStatus = value; RaisePropertyChanged(() => VehicleSafetyCheckStatus); }
        }

        private string _trailerSafetyCheckStatus;
        public string TrailerSafetyCheckStatus
        {
            get { return _trailerSafetyCheckStatus; }
            set { _trailerSafetyCheckStatus = value; RaisePropertyChanged(() => TrailerSafetyCheckStatus); }
        }

        public string SafetyCheckStatusKey
        {
            get { return "* FP = Full Pass; DP = Discretionary Pass; F = Fail"; }
        }

        public string SafetyCheckTypeColumnHeader
        {
            get { return "Type"; }
        }

        public string SafetyCheckCheckTitleColumnHeader
        {
            get { return "Check - Comment"; }
        }

        public string SafetyCheckStatusColumnHeader
        {
            get { return "Status"; }
        }

        public string DriverName
        {
            get { return _infoService.CurrentDriverDisplayName; }
        }

        private ObservableCollection<DisplaySafetyCheckFaultItemViewModel> _safetyCheckFaultItemViewModels = new ObservableCollection<DisplaySafetyCheckFaultItemViewModel>();
        public ObservableCollection<DisplaySafetyCheckFaultItemViewModel> SafetyCheckFaultItemViewModels
        {
            get { return _safetyCheckFaultItemViewModels; }
            set { _safetyCheckFaultItemViewModels = value; RaisePropertyChanged(() => SafetyCheckFaultItemViewModels); }
        }

        private MvxCommand<DisplaySafetyCheckFaultItemViewModel> _showSafetyCheckFaultCommand;
        public ICommand ShowSafetyCheckFaultCommand
        {
            get { return (_showSafetyCheckFaultCommand = _showSafetyCheckFaultCommand ?? new MvxCommand<DisplaySafetyCheckFaultItemViewModel>((f) => SafetyCheckFaultDetail(f))); }
        }

        #endregion Public Properties

        #region Private Methods

        private void SafetyCheckFaultDetail(DisplaySafetyCheckFaultItemViewModel fault)
        {
            if (!string.IsNullOrWhiteSpace(fault.FaultCheckComment))
                Mvx.Resolve<ICustomUserInteraction>().Alert(fault.FaultCheckComment.Trim(new Char[] {' ', '-'}), null, string.Format("{0} - {1}", "Fault", fault.FaultCheckTitle));
        }

        private void GenerateSafetyCheckFaultItems(List<SafetyCheckFault> faults, bool isTrailer)
        {
            // Add the safety check item view models
            foreach (var fault in faults)
            {
                this.SafetyCheckFaultItemViewModels.Add(new DisplaySafetyCheckFaultItemViewModel
                {
                    FaultCheckTitle = fault.Title,
                    FaultCheckComment = (string.IsNullOrWhiteSpace(fault.Comment) ? "" : " - " + fault.Comment),
                    FaultStatus = GetFaultStatusKey(fault.Status),
                    FaultType = (isTrailer ? "TRL" : "VEH"),
                });
            }

            RaisePropertyChanged(() => SafetyCheckFaultItemViewModels);
        }

        private string GetFaultStatusKey(Enums.SafetyCheckStatus status)
        {
            switch (status)
            {
                case MWF.Mobile.Core.Enums.SafetyCheckStatus.NotSet:
                    return "";
                case MWF.Mobile.Core.Enums.SafetyCheckStatus.Passed:
                    return "FP";
                case MWF.Mobile.Core.Enums.SafetyCheckStatus.DiscretionaryPass:
                    return "DP";
                case MWF.Mobile.Core.Enums.SafetyCheckStatus.Failed:
                    return "F";
                default:
                    return "";
            }
        }

        #endregion Private Methods

        #region BaseInstructionNotificationViewModel

        public override Task CheckInstructionNotificationAsync(GatewayInstructionNotificationMessage message)
        {
            var currentMobileNavData = _navigationService.CurrentNavData as NavData<MobileData>;

            if (currentMobileNavData != null)
                return this.RespondToInstructionNotificationAsync(message, currentMobileNavData, null);

            return Task.FromResult(0);
        }

        #endregion BaseInstructionNotificationViewModel

        #region BaseFragmentViewModel Overrides

        public override string FragmentTitle
        {
            get { return "Safety Check"; }
        }

        #endregion BaseFragmentViewModel Overrides

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            await _navigationService.GoBackAsync(_navigationService.CurrentNavData);
            return false;
        }

        #endregion IBackButtonHandler Implementation

    }
}
