using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.ViewModels
{

    public class SafetyCheckSignatureViewModel : BaseFragmentViewModel
    {

        #region private/protected members

        protected Services.IInfoService _infoService = null;
        protected Services.IGatewayQueuedService _gatewayQueuedService = null;
        protected Services.ISafetyCheckService _safetyCheckService = null;
        protected ICustomUserInteraction _userInteraction = null;
        protected INavigationService _navigationService;
        protected IEnumerable<Models.SafetyCheckData> _safetyCheckData;
        protected NavData<MobileData> _navData;
        protected IRepositories _repositories;

        #endregion private/protected members

        #region construction

        public SafetyCheckSignatureViewModel(Services.IInfoService infoService, Services.IGatewayQueuedService gatewayQueuedService, ICustomUserInteraction userInteraction, Repositories.IRepositories repositories, INavigationService navigationService, ISafetyCheckService safetyCheckService)
        {
            _infoService = infoService;
            _gatewayQueuedService = gatewayQueuedService;
            _userInteraction = userInteraction;
            _navigationService = navigationService;
            _repositories = repositories;
            _safetyCheckService = safetyCheckService;
        }

        public virtual async Task Init()
        {
            // Retrieve the vehicle and trailer safety check data from the startup info service
            _safetyCheckData = _safetyCheckService.GetCurrentSafetyCheckData();

            if (!_safetyCheckData.Any())
                throw new Exception("Invalid application state - signature screen should not be displayed in cases where there are no safety checks.");

            var vehicle = await _repositories.VehicleRepository.GetByIDAsync(_infoService.CurrentVehicleID.Value);
            var trailer = _infoService.CurrentTrailerID.HasValue ? await _repositories.TrailerRepository.GetByIDAsync(_infoService.CurrentTrailerID.Value) : null;

            await this.PopulateViewModelAsync(vehicle, trailer);
        }

        protected virtual async Task PopulateViewModelAsync(Models.Vehicle vehicle, Models.Trailer trailer)
        {
            var safetyProfiles = await _repositories.SafetyProfileRepository.GetAllAsync();
            var safetyProfileVehicle = safetyProfiles.SingleOrDefault(spv => spv.IntLink == vehicle.SafetyCheckProfileIntLink);
            var safetyProfileTrailer = trailer == null ? null : safetyProfiles.SingleOrDefault(spt => spt.IntLink == trailer.SafetyCheckProfileIntLink);

            var combinedOverallStatus = Models.SafetyCheckData.GetOverallStatus(_safetyCheckData.Select(scd => scd.GetOverallStatus()));

            if ((combinedOverallStatus == Enums.SafetyCheckStatus.NotSet) &&
               ((safetyProfileVehicle != null && safetyProfileVehicle.IsVOSACompliant)
               || (safetyProfileTrailer != null && safetyProfileTrailer.IsVOSACompliant)))
                throw new Exception("Cannot proceed to safety check signature screen because the safety check hasn't been completed");

            this.DriverName = _infoService.CurrentDriverDisplayName;
            this.VehicleRegistration = _infoService.CurrentVehicleRegistration;
            this.TrailerRef = trailer == null ? "- no trailer -" : trailer.Registration;

            var config = await _repositories.ConfigRepository.GetAsync();
            var hasVOSACompliantSafetyCheck =
                (safetyProfileVehicle != null && safetyProfileVehicle.IsVOSACompliant) ||
                (safetyProfileTrailer != null && safetyProfileTrailer.IsVOSACompliant);

            if (hasVOSACompliantSafetyCheck)
            {
                switch (combinedOverallStatus)
                {
                    case Enums.SafetyCheckStatus.Failed:
                        this.ConfirmationText = config.SafetyCheckFailText;
                        break;
                    case Enums.SafetyCheckStatus.DiscretionaryPass:
                        this.ConfirmationText = config.SafetyCheckDiscretionaryText;
                        break;
                    case Enums.SafetyCheckStatus.Passed:
                        this.ConfirmationText = config.SafetyCheckPassText;
                        break;
                    default:
                        throw new Exception("Unexpected safety check status");
                }
            }
            else
            {
                this.ConfirmationText = config.SafetyCheckPassText;
            }
        }

        #endregion construction

        #region properties

        private string _driverName;
        public string DriverName
        {
            get { return _driverName; }
            set { _driverName = value; RaisePropertyChanged(() => DriverName); }
        }

        private string _vehicleRegistration;
        public string VehicleRegistration
        {
            get { return _vehicleRegistration; }
            set { _vehicleRegistration = value; RaisePropertyChanged(() => VehicleRegistration); }
        }

        private string _trailerRef;
        public string TrailerRef
        {
            get { return _trailerRef; }
            set { _trailerRef = value; RaisePropertyChanged(() => TrailerRef); }
        }

        private string _confirmationText;
        public string ConfirmationText
        {
            get { return _confirmationText; }
            set { _confirmationText = value; RaisePropertyChanged(() => ConfirmationText); }
        }

        public string DoneLabel
        {
            get { return "Accept"; }
        }

        private string _signatureEncodedImage;
        public string SignatureEncodedImage
        {
            get { return _signatureEncodedImage; }
            set { _signatureEncodedImage = value; RaisePropertyChanged(() => SignatureEncodedImage); }
        }

        private MvxCommand _doneCommand;
        public System.Windows.Input.ICommand DoneCommand
        {
            get { return (_doneCommand = _doneCommand ?? new MvxCommand(async () => await this.DoneAsync())); }
        }

        public override string FragmentTitle
        {
            get { return "Signature"; }
        }

        private bool _isProgressing;
        public bool IsProgressing
        {
            get { return _isProgressing; }
            set { _isProgressing = value; RaisePropertyChanged(() => IsProgressing); }
        }

        public string ProgressingMessage
        {
            get { return "Storing safety check"; }
        }

        #endregion properties

        public async Task DoneAsync()
        {
            if (this.IsProgressing)
                return;

            if (string.IsNullOrWhiteSpace(SignatureEncodedImage))
            {
                await _userInteraction.AlertAsync("A signature is required to complete a safety check");
                return;
            }

            this.IsProgressing = true;

            try
            {
                // Set the signature on the vehicle and trailer safety checks
                foreach (var safetyCheckData in _safetyCheckData)
                {
                    safetyCheckData.Signature = new Models.Signature { EncodedImage = this.SignatureEncodedImage };
                }

                await _navigationService.MoveToNextAsync(_navData);
            }
            finally
            {
                this.IsProgressing = false;
            }
        }

    }

}
