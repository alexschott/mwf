using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;

namespace MWF.Mobile.Core.ViewModels
{
    public class SafetyCheckViewModel : BaseFragmentViewModel, IBackButtonHandler
    {

        #region private/protected members

        protected IInfoService _infoService;
        protected IRepositories _repositories;
        protected INavigationService _navigationService;
        protected NavData<MobileData> _navData;
        protected ISafetyCheckService _safetyCheckService;
        protected bool _manual = false;

        #endregion

        #region Construction

        public SafetyCheckViewModel(IInfoService infoService, INavigationService navigationService, IRepositories repositories, ISafetyCheckService safetyCheckService)
        {
            _infoService = infoService;
            _repositories = repositories;
            _navigationService = navigationService;
            _safetyCheckService = safetyCheckService;
        }

        public virtual async Task Init()
        {
            var vehicle = await _repositories.VehicleRepository.GetByIDAsync(_infoService.CurrentVehicleID.Value);
            var trailer = _infoService.CurrentTrailerID.HasValue ? await _repositories.TrailerRepository.GetByIDAsync(_infoService.CurrentTrailerID.Value) : null;

            var safetyProfiles = await _repositories.SafetyProfileRepository.GetAllAsync();
            this.SafetyProfileVehicle = safetyProfiles.SingleOrDefault(spv => spv.IntLink == vehicle.SafetyCheckProfileIntLink);
            this.SafetyProfileTrailer = trailer == null ? null : safetyProfiles.SingleOrDefault(spt => spt.IntLink == trailer.SafetyCheckProfileIntLink);

            this.SafetyCheckItemViewModels = new ObservableCollection<SafetyCheckItemViewModel>();

            _safetyCheckService.CurrentVehicleSafetyCheckData = null;
            _safetyCheckService.CurrentTrailerSafetyCheckData = null;

            if (SafetyProfileVehicle != null && SafetyProfileVehicle.DisplayAtLogon)
            {
                var safetyCheckData = await this.GenerateSafetyCheckDataAsync(this.SafetyProfileVehicle, _infoService.CurrentDriverID.Value, _infoService.CurrentVehicleID.Value, _infoService.CurrentVehicleRegistration, false);
                _safetyCheckService.CurrentVehicleSafetyCheckData = safetyCheckData;
            }

            if (SafetyProfileTrailer != null && SafetyProfileTrailer.DisplayAtLogon)
            {
                var safetyCheckData = await this.GenerateSafetyCheckDataAsync(this.SafetyProfileTrailer, _infoService.CurrentDriverID.Value, _infoService.CurrentTrailerID.Value, _infoService.CurrentTrailerRegistration, true);
                _safetyCheckService.CurrentTrailerSafetyCheckData = safetyCheckData;
            }

            if (_safetyCheckService.CurrentTrailerSafetyCheckData == null && _safetyCheckService.CurrentVehicleSafetyCheckData == null)
            {
                await Task.WhenAll(
                    Mvx.Resolve<ICustomUserInteraction>().AlertAsync("A safety check profile for your vehicle and/or trailer has not been found - Perform a manual safety check."),
                    _safetyCheckService.CommitSafetyCheckDataAsync());
                this._manual = true;

                await this.MoveToNextAsync();
            }
        }

        protected async Task<SafetyCheckData> GenerateSafetyCheckDataAsync(SafetyProfile safetyProfile, Guid driverID, Guid vehicleOrTrailerID, string vehicleOrTrailerRegistration, bool isTrailer)
        {
            var faults = safetyProfile.Children
                .OrderBy(scft => scft.Order)
                .Select(scft => new Models.SafetyCheckFault
                {
                    SafetyCheckDataID = safetyProfile.ID,
                    Title = scft.Title,
                    FaultTypeID = scft.ID,
                    IsDiscretionaryQuestion = scft.IsDiscretionaryQuestion,
                })
                .ToList();

            var driver = await _repositories.DriverRepository.GetByIDAsync(driverID);

            // Set up the safety check data to be set in the startup service.
            // This is the result of the safety check which will be sent to bluesphere
            // and persisted locally in case the safety check needs to be shown by the driver.
            var safetyCheckData = new SafetyCheckData
            {
                ProfileIntLink = safetyProfile.IntLink,
                DriverID = driverID,
                DriverTitle = driver.Title,
                VehicleID = vehicleOrTrailerID,
                VehicleRegistration = vehicleOrTrailerRegistration,
                Faults = faults,
                IsTrailer = isTrailer
            };

            // Add the safety check item view models
            foreach (var fault in faults)
            {
                this.SafetyCheckItemViewModels.Add(new SafetyCheckItemViewModel(this, _navigationService)
                {
                    ID = fault.ID,
                    SafetyCheckFault = fault,
                    IsVehicle = !isTrailer,
                    Title = (isTrailer ? "TRL: " : "VEH: ") + fault.Title,
                    IsDiscretionaryQuestion = fault.IsDiscretionaryQuestion,
                    CheckStatus = fault.Status,
                });
            }

            RaisePropertyChanged(() => this.SafetyCheckItemViewModels);

            return safetyCheckData;
        }

        #endregion

        #region Public Properties

        private SafetyProfile _safetyProfileVehicle;
        public SafetyProfile SafetyProfileVehicle
        {
            get { return _safetyProfileVehicle; }
            set { _safetyProfileVehicle = value; }
        }

        private SafetyProfile _safetyProfileTrailer;
        public SafetyProfile SafetyProfileTrailer
        {
            get { return _safetyProfileTrailer; }
            set { _safetyProfileTrailer = value; }
        }

        private ObservableCollection<SafetyCheckItemViewModel> _safetyCheckItemViewModels;
        public ObservableCollection<SafetyCheckItemViewModel> SafetyCheckItemViewModels
        {
            get { return _safetyCheckItemViewModels; }
            set { _safetyCheckItemViewModels = value; RaisePropertyChanged(() => SafetyCheckItemViewModels); }
        }

        public string ChecksDoneButtonLabel
        {
            get { return "Done"; }
        }

        private MvxCommand _checksDoneCommand;
        public System.Windows.Input.ICommand ChecksDoneCommand
        {
            get
            {
                _checksDoneCommand = _checksDoneCommand ?? new MvxCommand(async () => await this.MoveToNextAsync());
                return _checksDoneCommand;
            }
        }

        public bool CanSafetyChecksBeCompleted
        {
            get
            {
                if (this.IsProgressing)
                    return false;
                if (this._manual == true)
                    return true;

                bool allChecksCompleted = false;
                int count = 0;
                int index = 0;

                    if ((this.SafetyProfileVehicle != null && this.SafetyProfileVehicle.IsVOSACompliant)
                    || (this.SafetyProfileTrailer != null && this.SafetyProfileTrailer.IsVOSACompliant))
                {
                    if (SafetyCheckItemViewModels.Count == 0)
                        allChecksCompleted = false;
                    else
                    {
                        count = SafetyCheckItemViewModels.Count;
                        foreach (var safetyCheckItem in SafetyCheckItemViewModels)
                        {
                            allChecksCompleted = (safetyCheckItem.CheckStatus != Enums.SafetyCheckStatus.NotSet);
                            if (allChecksCompleted == true)
                                index++;
                        }
                        if (index == count)
                            return true;
                        else
                            return false;

                    }
                }

                return allChecksCompleted;
            }
        }

        public void CheckSafetyCheckItemsStatus()
        {
            RaisePropertyChanged(() => CanSafetyChecksBeCompleted);
        }

        private bool _isProgressing;
        public bool IsProgressing
        {
            get { return _isProgressing; }
            set { _isProgressing = value; RaisePropertyChanged(() => IsProgressing); }
        }

        public string ProgressingMessage
        {
            get { return "Please wait"; }
        }

        #endregion

        #region Private Methods

        protected async Task MoveToNextAsync()
        {
            // cannot progress if the safety checks need completing.
            if (!CanSafetyChecksBeCompleted)
                return;

                if (this.IsProgressing)
                return;

            this.IsProgressing = true;

            try
            {
                RaisePropertyChanged(() => CanSafetyChecksBeCompleted);
                await _navigationService.MoveToNextAsync(_navData);
            }
            finally
            {
                this.IsProgressing = false;
            }
        }

        public override string FragmentTitle
        {
            get { return "Safety checklist"; }
        }

        public async Task<bool> OnBackButtonPressedAsync()
        {
            bool continueWithBackPress = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("All information you have entered will be lost, do you wish to continue?", "Abandon safety check!", "Continue");

            if (continueWithBackPress)
            {
                if (_navigationService.IsBackActionDefined())
                {
                    await _navigationService.GoBackAsync();
                    return false;
                }

                return true;
            }

            return false;

        }

        #endregion

    }
}
