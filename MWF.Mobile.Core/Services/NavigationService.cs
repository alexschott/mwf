using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Presentation;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Utilities;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Core.Services
{

    /// <summary>
    /// Class to centralise navigation logic. Stores a navigation graph as two dictionaries (forward and backward)
    /// that map a Activity/Fragment view model graph node to the action that will navigate to the next node.
    /// The actual mappings that define the MWF.Mobile navigation graph are in the SetMappings function.
    /// </summary>
    public class NavigationService : MvxNavigatingObject, INavigationService
    {

        #region Private Members

        private Dictionary<Tuple<Type, Type>, Func<Guid, NavData, Task>> _forwardNavActionDictionary;
        private Dictionary<Tuple<Type, Type>, Func<Guid, NavData, Task>> _backwardNavActionDictionary;
        private Dictionary<Guid, Tuple<Object, Dictionary<string, Object>>> _navDataDictionary;
        private NavData _currentNavData;


        private readonly IGatewayPollingService _gatewayPollingService;
        private readonly IInfoService _infoService;
        private readonly IDataChunkService _dataChunkService;
        private readonly ISafetyCheckService _safetyCheckService;
        private readonly IGatewayQueuedService _gatewayQueuedService = null;
        private readonly IGpsService _gpsService = null;
        private ICustomPresenter _presenter;
        private bool _inLogoutSafetyCheck = false;

        IRepositories _repositories;
        ICloseApplication _closeApplication;
        private Timer _loginSessionTimer;
        private readonly IMvxMessenger _messenger = null;
        private MvxSubscriptionToken _notificationToken;
        private MvxSubscriptionToken _modalSubscriptionToken = null;

        #endregion

        #region Construction

        public NavigationService(
            ICustomPresenter presenter,
            IInfoService infoService,
            ICloseApplication closeApplication,
            IRepositories repositories,
            IGatewayPollingService gatewayPollingService,
            IDataChunkService dataChunkService,
            IMvxMessenger messenger,
            ISafetyCheckService safetyCheckService,
            IGatewayQueuedService gatewayQueuedService,
            IGpsService gpsService)
        {
            _forwardNavActionDictionary = new Dictionary<Tuple<Type, Type>, Func<Guid, NavData, Task>>();
            _backwardNavActionDictionary = new Dictionary<Tuple<Type, Type>, Func<Guid, NavData, Task>>();
            _navDataDictionary = new Dictionary<Guid, Tuple<object, Dictionary<string, object>>>();
            _presenter = presenter;

            _repositories = repositories;
            _gatewayPollingService = gatewayPollingService;
            _infoService = infoService;
            _closeApplication = closeApplication;
            _dataChunkService = dataChunkService;
            _safetyCheckService = safetyCheckService;
            _gatewayQueuedService = gatewayQueuedService;
            _gpsService = gpsService;
            _messenger = messenger;

            _notificationToken = Mvx.Resolve<IMvxMessenger>().Subscribe<Messages.InvalidLicenseNotificationMessage>(async m =>
                await this.InvalidLicenseLogoutAsync()
             );

            SetMappings();
        }

        #endregion

        #region Public Methods

        public void InsertNavAction<TActivityViewModel, TFragmentViewModel, TDestinationViewModel>()
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
            where TDestinationViewModel : IMvxViewModel
        {
            var key = CreateKey<TActivityViewModel, TFragmentViewModel>();
            _forwardNavActionDictionary.Add(key, (navID, navData) => this.MoveToAsync<TDestinationViewModel>(navID));
        }

        public void InsertCustomNavAction<TActivityViewModel, TFragmentViewModel>(Func<Guid, NavData, Task> action)
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
        {
            var key = CreateKey<TActivityViewModel, TFragmentViewModel>();
            _forwardNavActionDictionary.Add(key, action);
        }

        public void InsertBackNavAction<TActivityViewModel, TFragmentViewModel, TDestinationViewModel>()
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
            where TDestinationViewModel : IMvxViewModel
        {
            var key = CreateKey<TActivityViewModel, TFragmentViewModel>();
            _backwardNavActionDictionary.Add(key, (navID, navData) => this.MoveBackToAsync<TDestinationViewModel>());
        }

        public void InsertCustomBackNavAction<TActivityViewModel, TFragmentViewModel>(Func<Guid, NavData, Task> action)
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
        {
            var key = CreateKey<TActivityViewModel, TFragmentViewModel>();
            _backwardNavActionDictionary.Add(key, action);
        }

        public bool NavActionExists<TActivityViewModel, TFragmentViewModel>()
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
        {
            var key = CreateKey<TActivityViewModel, TFragmentViewModel>();
            return _forwardNavActionDictionary.ContainsKey(key);
        }

        public bool BackNavActionExists<TActivityViewModel, TFragmentViewModel>()
            where TActivityViewModel : MvxViewModel
            where TFragmentViewModel : MvxViewModel
        {
            var key = CreateKey<TActivityViewModel, TFragmentViewModel>();
            return _backwardNavActionDictionary.ContainsKey(key);
        }

        public bool BackNavActionExists(Type activityType, Type fragmentType)
        {
            if (!AreSourceTypesValid(activityType, fragmentType))
                throw new ArgumentException("View model types must derive from BaseActivityViewModel, BaseFragmentViewModel");

            var key = CreateKey(activityType, fragmentType);
            return _backwardNavActionDictionary.ContainsKey(key);
        }

        public Func<Guid, NavData, Task> GetNavAction<TActivityViewModel, TFragmentViewModel>()
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
        {
            var key = CreateKey<TActivityViewModel, TFragmentViewModel>();
            return GetNavActionWithKey(_forwardNavActionDictionary, key);
        }

        public Func<Guid, NavData, Task> GetNavAction(Type activityType, Type fragmentType)
        {
            if (!AreSourceTypesValid(activityType, fragmentType))
                throw new ArgumentException("View model types must derive from BaseActivityViewModel, BaseFragmentViewModel");

            var key = CreateKey(activityType, fragmentType);
            return GetNavActionWithKey(_forwardNavActionDictionary, key);
        }

        public Func<Guid, NavData, Task> GetBackNavAction(Type activityType, Type fragmentType)
        {
            if (!AreSourceTypesValid(activityType, fragmentType))
                throw new ArgumentException("View model types must derive from BaseActivityViewModel, BaseFragmentViewModel");

            var key = CreateKey(activityType, fragmentType);
            return GetNavActionWithKey(_backwardNavActionDictionary, key);
        }

        #endregion Public Methods

        #region INavigationService

        public async Task MoveToNextAsync()
        {
            Type currentActivityType = _presenter.CurrentActivityViewModel.GetType();
            Type currentFragmentType = _presenter.CurrentFragmentViewModel.GetType();

            Func<Guid, NavData, Task> navAction = this.GetNavAction(currentActivityType, currentFragmentType);

            if (navAction == null)
                throw new UnknownNavigationMappingException(currentActivityType, currentFragmentType);

            _currentNavData = null;

            await navAction.Invoke(Guid.Empty, null);
        }

        /// <summary>
        /// Shows a view modal that will return a result value when it is closed
        /// </summary>
        /// <see cref="http://www.gregshackles.com/2012/11/returning-results-from-view-models-in-mvvmcross/" />
        /// <typeparam name="TModalViewModel">View Model to show (must implement  IModalViewModel) </typeparam>
        /// <typeparam name="TResult">The result type the modal view model will return</typeparam>
        /// <param name="navData">Data needed by the modal.  A navID guid will be passed to the modal's Init method and it can retrieve the data using navigationService.GetNavData(navID).</param>
        /// <param name="onResult">Action to run when the modal view has closed, returning with a result</param>
        public bool ShowModalViewModel<TModalViewModel, TResult>(NavData navData, Action<TResult> onResult)
            where TModalViewModel : IModalViewModel<TResult>
        {
            var navID = AddNavDataToDictionary(navData);
            _currentNavData = navData;

            _modalSubscriptionToken = _messenger.SubscribeOnMainThread<ModalNavigationResultMessage<TResult>>(msg =>
            {
                // make sure message ids match up
                if (msg.MessageId == navID)
                {
                    if (_modalSubscriptionToken != null)
                        _messenger.Unsubscribe<ModalNavigationResultMessage<TResult>>(_modalSubscriptionToken);

                    if (onResult != null)
                        onResult(msg.Result);
                }
            });

            return ShowViewModel<TModalViewModel>(new { navID = navID });
        }

        public async Task<Guid> MoveToNextAsync(NavData navData)
        {
            if (navData == null)
            {
                await MoveToNextAsync();
                return Guid.Empty;
            }

            Type currentActivityType = _presenter.CurrentActivityViewModel.GetType();
            Type currentFragmentType = _presenter.CurrentFragmentViewModel.GetType();

            Func<Guid, NavData, Task> navAction = this.GetNavAction(currentActivityType, currentFragmentType);

            if (navAction == null)
                throw new UnknownNavigationMappingException(currentActivityType, currentFragmentType);

            var navID = AddNavDataToDictionary(navData);
            _currentNavData = navData;

            await navAction.Invoke(navID, navData);

            return navID;
        }


        public Task GoBackAsync()
        {
            Type currentActivityType = _presenter.CurrentActivityViewModel.GetType();
            Type currentFragmentType = _presenter.CurrentFragmentViewModel.GetType();

            Func<Guid, NavData, Task> navAction = this.GetBackNavAction(currentActivityType, currentFragmentType);

            if (navAction == null)
                throw new UnknownBackNavigationMappingException(currentActivityType, currentFragmentType);

            _currentNavData = null;

            return navAction.Invoke(Guid.Empty, null);
        }

        public async Task<Guid> GoBackAsync(NavData navData)
        {
            if (navData == null)
            {
                await GoBackAsync();
                return Guid.Empty;
            }

            Type currentActivityType = _presenter.CurrentActivityViewModel.GetType();
            Type currentFragmentType = _presenter.CurrentFragmentViewModel.GetType();

            Func<Guid, NavData, Task> navAction = this.GetBackNavAction(currentActivityType, currentFragmentType);

            if (navAction == null)
                throw new UnknownBackNavigationMappingException(currentActivityType, currentFragmentType);

            var navID = AddNavDataToDictionary(navData);
            _currentNavData = navData;

            await navAction.Invoke(navID, navData);

            return navID;
        }

        public bool IsBackActionDefined()
        {
            Type currentActivityType = _presenter.CurrentActivityViewModel.GetType();
            Type currentFragmentType = _presenter.CurrentFragmentViewModel.GetType();

            return BackNavActionExists(currentActivityType, currentFragmentType);
        }

        public Task GoToManifestAsync()
        {
            this.ShowViewModel<ManifestViewModel>();
            return Task.FromResult(0);
        }

        public Task LogoutAsync()
        {
            return this.Logout_ActionAsync(Guid.Empty, null);
        }

        /// <summary>
        /// Logout, skipping any safety checks.  This simply stops any timers and returns the user to the passcode screen.
        /// This is intended for use in extreme cases such as when the device OS lifecycle has cleared out from memory key data needed for the app to function.
        /// </summary>
        public Task DirectLogoutAsync()
        {
            return this.DoLogoutAsync();
        }

        private async Task Logout_ActionAsync(Guid navID, NavData navData)
        {
            var _vehicleSafetyProfile = await VehicleSafetyProfileAsync();
            var _trailerSafetyProfile = await TrailerSafetyProfileAsync();

            if ((_vehicleSafetyProfile != null && _vehicleSafetyProfile.DisplayAtLogoff)
                || (_trailerSafetyProfile != null && _trailerSafetyProfile.DisplayAtLogoff))
            {
                _inLogoutSafetyCheck = true;
                this.ShowViewModel<SafetyCheckViewModel>();
            }
            else
            {
                await this.DoLogoutAsync();
            }
        }

        private async Task InvalidLicenseLogoutAsync()
        {
            await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("Your license is no longer valid. Logging out.");

            var driverRepository = _repositories.DriverRepository;
            var driver = await driverRepository.GetByIDAsync(_infoService.CurrentDriverID.Value);

            if (driver != null)
            {
                driver.IsLicensed = false;

                try
                {
                    await _repositories.DriverRepository.UpdateAsync(driver);
                }
                catch (Exception ex)
                {
                    MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "DriverRepository", "UpdateAsync", ex.StackTrace);
                    throw;
                }
            }

            await this.DoLogoutAsync();
        }

        private Task DoLogoutAsync()
        {
            // Stop the gateway polling service before we "logout" the user.
            _gatewayPollingService.StopPollingTimer();
            this.StopLoginSessionTimer();

            _infoService.Clear();

            return this.MoveToAsync<StartupViewModel>(Guid.Empty);
        }

        public NavData<T> GetNavData<T>(Guid navID)
            where T : class
        {
            Tuple<object, Dictionary<string, object>> tuple;
            if (!_navDataDictionary.TryGetValue(navID, out tuple))
                throw new Exception("navID does not exist in navDataDictionary");

            _navDataDictionary.Remove(navID);

            var navData = new NavData<T>();
            navData.SetData(tuple.Item1);
            navData.OtherData = tuple.Item2;

            return navData;
        }

        public NavData CurrentNavData
        {
            get { return _currentNavData; }
        }

        #endregion INavigationService

        #region Private Properties

        private async Task<SafetyProfile> VehicleSafetyProfileAsync()
        {
            var vehicle = await _repositories.VehicleRepository.GetByIDAsync(_infoService.CurrentVehicleID.Value);
            var safetyProfiles = await _repositories.SafetyProfileRepository.GetAllAsync();
            var retVal = safetyProfiles.SingleOrDefault(spv => spv.IntLink == vehicle.SafetyCheckProfileIntLink);
            return retVal;
        }

        private async Task<SafetyProfile> TrailerSafetyProfileAsync()
        {
            if (!_infoService.CurrentTrailerID.HasValue)
                return null;

            var trailer = await _repositories.TrailerRepository.GetByIDAsync(_infoService.CurrentTrailerID.Value);
            return await this.GetTrailerSafetyProfileAsync(trailer);
        }

        private async Task<SafetyProfile> GetTrailerSafetyProfileAsync(Models.Trailer trailer)
        {
            if (trailer == null)
                return null;

            var data = await _repositories.SafetyProfileRepository.GetAllAsync();
            var retVal = data.SingleOrDefault(spv => spv.IntLink == trailer.SafetyCheckProfileIntLink);

            return retVal;
        }

        private Enums.SafetyCheckStatus SafetyCheckStatus
        {
            get { return SafetyCheckData.GetOverallStatus(_safetyCheckService.GetCurrentSafetyCheckData().Select(scd => scd.GetOverallStatus())); }
        }

        #endregion Private Properties

        #region Private Methods

        private Task MoveToAsync<TViewModel>(Guid navID)
            where TViewModel : IMvxViewModel
        {
            this.ShowViewModel<TViewModel>(navID);
            return Task.FromResult(0);
        }

        private Task MoveBackToAsync<TViewModel>()
        {
            ChangePresentation(new Presentation.CloseUpToViewPresentationHint(typeof(TViewModel)));
            return Task.FromResult(0);
        }

        private Tuple<Type, Type> CreateKey<TActivityViewModel, TFragmentViewModel>()
            where TActivityViewModel : MvxViewModel
            where TFragmentViewModel : MvxViewModel
        {
            return Tuple.Create<Type, Type>(typeof(TActivityViewModel), typeof(TFragmentViewModel));
        }

        private Tuple<Type, Type> CreateKey(Type activityType, Type fragmentType)
        {
            return Tuple.Create<Type, Type>(activityType, fragmentType);
        }

        private Func<Guid, NavData, Task> GetNavActionWithKey(Dictionary<Tuple<Type, Type>, Func<Guid, NavData, Task>> dictionary, Tuple<Type, Type> key)
        {
            Func<Guid, NavData, Task> action = null;
            dictionary.TryGetValue(key, out action);
            return action;
        }

        private bool AreSourceTypesValid(Type activityType, Type fragmentType)
        {
            return typeof(BaseActivityViewModel).IsAssignableFrom(activityType) && typeof(BaseFragmentViewModel).IsAssignableFrom(fragmentType);
        }

        private async Task<bool> ConfirmCommentAccessAsync(Guid navID, NavData navData)
        {
            var advanceToCommentScreen = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("Do you want to enter a comment for this instruction?", "", "Yes", "No");

            if (advanceToCommentScreen)
            {
                navData.OtherData["VisitedCommentScreen"] = true;
                this.ShowViewModel<InstructionCommentViewModel>(navID);
            }
            else
            {
                navData.OtherData["VisitedCommentScreen"] = null;
            }

            return advanceToCommentScreen;
        }

        private async Task<bool> IsCleanInstructionAsync(Guid navID)
        {
            var isClean = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("Is the delivery clean?", "", "Yes", "No");

            if (!isClean)
                this.ShowViewModel<InstructionClausedViewModel>(navID);

            return isClean;
        }

        /// <summary>
        /// Adds navigation data object to our dictionary so it can be retreived by a view model after a navigation action has completed
        /// MVVM cross only allows passing of simple objects via serialization. Navigation improves this by having MVVM Cross pass a GUID
        /// linked to the full objects that can be retrieved after the navigated to ViewModel is shown
        /// </summary>
        /// <param name="navData"></param>
        private Guid AddNavDataToDictionary(NavData navData)
        {
            var navID = Guid.NewGuid();
            var tuple = Tuple.Create<object, Dictionary<string, object>>(navData.GetData(), navData.OtherData);
            _navDataDictionary[navID] = tuple;
            return navID;
        }

        /// <summary>
        /// Once the user confirms the prompt it updates the instruction to complete and then sends off the Complete chunk to Bluesphere.
        /// </summary>
        private async Task CompleteInstructionAsync(NavData<MobileData> navData)
        {
            var isConfirmed = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("Do you wish to complete?", "Complete Instruction", "Confirm", "Cancel");

            if (isConfirmed)
            {
                await this.SendMobileDataAsync(navData);
            }
        }

        /// <summary>
        /// Updates the instruction/Message to complete and then sends off the Complete chunk to Bluesphere.
        /// </summary>
        private async Task SendMobileDataAsync(NavData<MobileData> navData)
        {
            navData.Data.ProgressState = Enums.InstructionProgress.Complete;
            await _dataChunkService.SendDataChunkAsync(navData.GetDataChunk(), navData.Data, _infoService.CurrentDriverID.Value, _infoService.CurrentVehicleRegistration);

            // send any datachunks for addiotional instructions
            var additionalInstructions = navData.GetAdditionalInstructions();
            foreach (var additionalInstruction in additionalInstructions)
            {

                additionalInstruction.ProgressState = Enums.InstructionProgress.Complete;
                await _dataChunkService.SendDataChunkAsync(navData.GetAdditionalDataChunk(additionalInstruction), additionalInstruction, _infoService.CurrentDriverID.Value, _infoService.CurrentVehicleRegistration);
            }

            this.ShowViewModel<ManifestViewModel>();
        }

        private async Task DriverLogInAsync()
        {
            var currentDriver = new DriverActivity(_infoService.CurrentDriverID.Value, _infoService.CurrentVehicleID.Value, Enums.DriverActivity.LogOn);
            currentDriver.Smp = _gpsService.GetSmpData(Enums.ReportReason.DriverLogOn);

            await _gatewayQueuedService.AddToQueueAsync("fwSetDriverActivity", currentDriver);

            await this.StartLoginSessionTimerAsync();
            _gatewayPollingService.StartPollingTimer();
        }

        private async Task StartLoginSessionTimerAsync()
        {
            if (_loginSessionTimer == null)
            {
                var config = await _repositories.ConfigRepository.GetAsync();
                var sessionTimeoutInSeconds = config.SessionTimeoutInSeconds;

                if (sessionTimeoutInSeconds > 0)
                    _loginSessionTimer = new Timer(
                        async state =>
                        {
                            await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("Your login session has expired.");
                            await this.DoLogoutAsync();
                        },
                        null,
                        sessionTimeoutInSeconds * 1000);
            }
            else
                _loginSessionTimer.Reset();
        }

        private void StopLoginSessionTimer()
        {
            if (_loginSessionTimer == null)
                return;

            _loginSessionTimer.Dispose();
            _loginSessionTimer = null;
        }

        private bool ShowViewModel<TViewModel>(Guid navID)
             where TViewModel : IMvxViewModel
        {
            return this.ShowViewModel<TViewModel>(new { navID = navID });
        }

        #endregion Private Methods

        #region Mappings Definitions

        private void SetMappings()
        {
            // StartUp Activity
            InsertCustomNavAction<StartupViewModel, CustomerCodeViewModel>(CustomerCode_CustomActionAsync);
            InsertNavAction<StartupViewModel, CheckOutViewModel, CheckOutQRCodeViewModel>();
            InsertNavAction<StartupViewModel, CheckOutQRCodeViewModel, CheckOutTermsViewModel>();
            InsertNavAction<StartupViewModel, CheckOutTermsViewModel, CheckOutSignatureViewModel>();
            InsertNavAction<StartupViewModel, CheckOutSignatureViewModel, PasscodeViewModel>();
            InsertNavAction<StartupViewModel, CheckInViewModel, CheckInCompleteViewModel>();

            InsertCustomNavAction<StartupViewModel, PasscodeViewModel>(Passcode_CustomActionAsync);
            InsertNavAction<StartupViewModel, DiagnosticsViewModel, PasscodeViewModel>();
            InsertNavAction<StartupViewModel, VehicleListViewModel, TrailerListViewModel>();
            InsertNavAction<StartupViewModel, TrailerListViewModel, SafetyCheckViewModel>();
            InsertCustomNavAction<StartupViewModel, SafetyCheckViewModel>(SafetyCheck_CustomActionAsync);        //to odometer, signature screen or main activity (manifest)      

            InsertNavAction<StartupViewModel, OdometerViewModel, SafetyCheckSignatureViewModel>();
            InsertCustomNavAction<StartupViewModel, SafetyCheckSignatureViewModel>(Signature_CustomAction_LoginAsync); //to either main activity (manifest) or back to driver passcode

            InsertCustomBackNavAction<StartupViewModel, PasscodeViewModel>(CloseApplicationAsync);               //Back from passcode closes app


            // Main Activity
            InsertCustomBackNavAction<MainViewModel, ManifestViewModel>(Logout_ActionAsync); // Back from manifest sends back to startup activity
            InsertCustomNavAction<MainViewModel, ManifestViewModel>(Manifest_CustomActionAsync);

            InsertCustomBackNavAction<MainViewModel, InstructionViewModel>(Instruction_CustomBackActionAsync);
            InsertCustomNavAction<MainViewModel, InstructionViewModel>(Instruction_CustomActionAsync);

            InsertCustomBackNavAction<MainViewModel, InstructionOnSiteViewModel>(InstructionOnSite_CustomBackActionAsync);
            InsertCustomNavAction<MainViewModel, InstructionOnSiteViewModel>(InstructionOnSite_CustomActionAsync);

            // trailer select/safety check/signature for instruction
            InsertCustomNavAction<MainViewModel, InstructionTrailerViewModel>(InstructionTrailer_CustomActionAsync);
            InsertCustomNavAction<MainViewModel, InstructionSafetyCheckViewModel>(TrailerSafetyCheck_CustomActionAsync);
            InsertCustomNavAction<MainViewModel, InstructionSafetyCheckSignatureViewModel>(InstructionSafetyCheckSignature_CustomActionAsync);

            InsertCustomNavAction<MainViewModel, BarcodeScanningViewModel>(Barcode_CustomActionAsync);

            InsertCustomNavAction<MainViewModel, InstructionCommentViewModel>(InstructionComment_CustomActionAsync);
            InsertCustomBackNavAction<MainViewModel, InstructionCommentViewModel>(InstructionComment_CustomBackActionAsync);

            InsertCustomNavAction<MainViewModel, InstructionClausedViewModel>(InstructionClaused_CustomActionAsync);

            InsertCustomNavAction<MainViewModel, InstructionSignatureViewModel>(InstructionSignature_CustomActionAsync);
            InsertCustomBackNavAction<MainViewModel, InstructionSignatureViewModel>(InstructionSignature_CustomBackActionAsync);

            InsertCustomNavAction<MainViewModel, ConfirmTimesViewModel>(ConfirmTimes_CustomActionAsync);
            InsertCustomBackNavAction<MainViewModel, ConfirmTimesViewModel>(ConfirmTimes_CustomBackActionAsync);

            InsertCustomBackNavAction<MainViewModel, OrderViewModel>(Order_CustomBackActionAsync);
            InsertNavAction<MainViewModel, OrderViewModel, ReviseQuantityViewModel>();

            InsertNavAction<MainViewModel, ReviseQuantityViewModel, OrderViewModel>();

            InsertCustomNavAction<MainViewModel, InstructionTrunkProceedViewModel>(InstructionTrunkProceed_CustomActionAsync);

            InsertCustomNavAction<MainViewModel, InboxViewModel>(Inbox_CustomActionAsync);
            InsertCustomBackNavAction<MainViewModel, SafetyCheckViewModel>(SafetyCheck_CustomBackActionAsync);


            // safety check on logout sequence
            InsertCustomNavAction<MainViewModel, SafetyCheckViewModel>(SafetyCheck_CustomActionAsync);
            InsertNavAction<MainViewModel, OdometerViewModel, SafetyCheckSignatureViewModel>();
            InsertCustomNavAction<MainViewModel, SafetyCheckSignatureViewModel>(Signature_CustomAction_SidebarAsync);

            // Side bar Activity
            InsertCustomNavAction<MainViewModel, SidebarCameraViewModel>(SidebarNavigation_CustomActionAsync);
            InsertCustomBackNavAction<MainViewModel, SidebarCameraViewModel>(SidebarNavigation_CustomActionAsync);

            InsertCustomNavAction<MainViewModel, DisplaySafetyCheckViewModel>(SidebarNavigation_CustomActionAsync);
            InsertCustomBackNavAction<MainViewModel, DisplaySafetyCheckViewModel>(SidebarNavigation_CustomActionAsync);

            InsertCustomNavAction<MainViewModel, DiagnosticsViewModel>(SidebarNavigation_CustomActionAsync);

        }

        #endregion Mappings Definitions

        #region Custom Mapping Actions

        private Task CloseApplicationAsync(Guid navID, NavData navData)
        {
            _closeApplication.CloseApp();
            return Task.FromResult(0);
        }

        public async Task CustomerCode_CustomActionAsync(Guid navID, NavData navData)
        {
            var appProfile = await _repositories.ApplicationRepository.GetAsync();
            if (appProfile.DeviceCheckInOutRequired)
            {
                CheckInOutService service = new CheckInOutService(_repositories);
                Enums.CheckInOutActions status = await service.GetDeviceStatus(Mvx.Resolve<IDeviceInfo>().IMEI);
                if(status == Enums.CheckInOutActions.CheckIn)
                    await this.MoveToAsync<CheckOutViewModel>(navID);
                else
                    await this.MoveToAsync<PasscodeViewModel>(navID);
            }
            else
                await this.MoveToAsync<PasscodeViewModel>(navID);
        }

        public Task Passcode_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData != null)
                return this.MoveToAsync<DiagnosticsViewModel>(navID);

            return this.MoveToAsync<VehicleListViewModel>(navID);
        }

        /// <summary>
        /// Safety Check screen goes to main activity (manifest) if there are no profiles
        /// or odometer screen if odometer reading is required, safety check signature screen otherwise
        /// </summary>
        public async Task SafetyCheck_CustomActionAsync(Guid navID, NavData navData)
        {
            var _vehicleSafetyProfile = await VehicleSafetyProfileAsync();
            var _trailerSafetyProfile = await TrailerSafetyProfileAsync();

            if (_vehicleSafetyProfile == null && _trailerSafetyProfile == null)
            {
                if (_presenter.CurrentActivityViewModel.GetType().Equals(typeof(ViewModels.StartupViewModel)))
                    await this.DriverLogInAsync();

                await MoveToAsync<MainViewModel>(navID);
            }
            else
            {
                if (_vehicleSafetyProfile != null && _vehicleSafetyProfile.OdometerRequired)
                    this.ShowViewModel<OdometerViewModel>();
                else
                    this.ShowViewModel<SafetyCheckSignatureViewModel>();
            }
        }

        /// <summary>
        /// Signature screen goes back to driver pass code screen if we have any safety check failures
        /// and to main acticity (manifest) otherwise
        /// </summary>
        public async Task Signature_CustomAction_LoginAsync(Guid navID, NavData navData)
        {
            // commit safety check data to repositories and bluesphere
            await _safetyCheckService.CommitSafetyCheckDataAsync();

            if (SafetyCheckStatus == Enums.SafetyCheckStatus.Failed)
            {
                await this.MoveToAsync<StartupViewModel>(navID);
            }
            else
            {
                if (_presenter.CurrentActivityViewModel.GetType().Equals(typeof(ViewModels.StartupViewModel)))
                    await this.DriverLogInAsync();

                await this.MoveToAsync<MainViewModel>(navID);
            }
        }

        /// <summary>
        /// Commits the safety check and does the logout if we were doing a logout safety check
        ///  Otherwise this must have have been a safety check from the sidebar so go back to manifest screen
        /// </summary>
        public async Task Signature_CustomAction_SidebarAsync(Guid navID, NavData navData)
        {
            try
            {
                // commit safety check data to repositories and bluesphere
                await _safetyCheckService.CommitSafetyCheckDataAsync();

                if (_inLogoutSafetyCheck)
                {
                    await this.DoLogoutAsync();
                    _inLogoutSafetyCheck = false;
                }
                else
                {
                    this.ShowViewModel<ManifestViewModel>();
                }
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Manifest screen goes back to to instruction screen if we get a mobile data nav item
        /// and to main acticity (manifest) otherwise
        /// </summary>
        public Task Manifest_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
            {
                var mobileNavData = navData as NavData<MobileData>;

                if (mobileNavData.Data.Order.Type == Enums.InstructionType.Collect || mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver)
                    this.ShowViewModel<InstructionViewModel>(navID);
                else if (mobileNavData.Data.Order.Type == Enums.InstructionType.TrunkTo || mobileNavData.Data.Order.Type == Enums.InstructionType.ProceedFrom)
                    this.ShowViewModel<InstructionTrunkProceedViewModel>(navID);
                else if (mobileNavData.Data.Order.Type == Enums.InstructionType.OrderMessage)
                    this.ShowViewModel<MessageViewModel>(navID);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Instruction screen. If we're getting an "item" (order) then show the order in question
        /// If we're getting a trailer then show the select trailer screen
        /// If we're getting a mobile data then move on to InstructionOnSiteViewModel
        /// </summary>
        public async Task Instruction_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<Models.Instruction.Trailer>)
            {

            }
            else if (navData is NavData<MobileData>)
            {
                if (navData.OtherData.IsDefined("IsTrailerEditFromInstructionScreen"))
                {
                    this.ShowViewModel<InstructionTrailerViewModel>(navID);
                }
                else
                {
                    var mobileDataNav = (NavData<MobileData>)navData;
                    
                    // send "onsite" data chunk 
                    await _dataChunkService.SendDataChunkAsync(mobileDataNav.GetDataChunk(), mobileDataNav.Data, _infoService.CurrentDriverID.Value, _infoService.CurrentVehicleRegistration);

                    if (mobileDataNav.Data.ProgressState == Enums.InstructionProgress.OnSite)
                        mobileDataNav.Data.OnSiteDateTime = DateTime.Now;

                    this.ShowViewModel<InstructionOnSiteViewModel>(navID);
                }
            }
        }

        /// <summary>
        /// Instruction on site screen, if the trailer selection is enabled then it will redirect to the trailer selection screen
        /// else if trailer selection is not enabled and the bypass comment screen is not then enabled then will it redirect to comment screen.
        /// else if trailer selection is not enabled and the bypass comment screen is enabled 
        /// and if either either name required or signature required are enabled then redirect to signature screen.
        /// </summary>
        public async Task InstructionOnSite_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
            {
                var mobileNavData = navData as NavData<MobileData>;

                var additionalContent = mobileNavData.Data.Order.Additional;
                var itemAdditionalContent = mobileNavData.Data.Order.Items.FirstOrDefault().Additional;
                var deliveryOptions = mobileNavData.GetWorseCaseDeliveryOptions();

                // Collection
                if (mobileNavData.Data.Order.Type == Enums.InstructionType.Collect)
                {
                    var currentTrailerID = _infoService.CurrentTrailerID;
                    var currentTrailer = currentTrailerID.HasValue ? await _repositories.TrailerRepository.GetByIDAsync(currentTrailerID.Value) : null;

                    if (additionalContent.IsTrailerConfirmationEnabled || !Models.Trailer.SameAs(currentTrailer, mobileNavData.Data.Order.Additional.Trailer))
                    {
                        // Note if trailer confirmation is not explicitly enabled, still need to cater for ambiguous case
                        // where the current trailer doesn't match the one specified on the order. Which one does the driver
                        // actually have attached and intend to use for the order?
                        var instructionTrailer = mobileNavData.Data.Order.Additional.Trailer;
                        string orderTrailerMessage = (instructionTrailer == null || instructionTrailer.TrailerId == null) ? "No trailer specified on instruction." : string.Format("Trailer specified on instruction is {0}.", instructionTrailer.TrailerId);
                        string currentTrailerMessage = (currentTrailer == null) ? " You currently have no trailer." : string.Format(" Current trailer is {0}.", currentTrailer.Registration);

                        string message = orderTrailerMessage + currentTrailerMessage;
                        var isConfirmed = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync(message, "Change Trailer?", "Select Trailer", "Use Current");

                        if (isConfirmed)
                        {
                            this.ShowViewModel<InstructionTrailerViewModel>(navID);
                            return;
                        }
                        else
                        {
                            await this.UpdateTrailerForInstructionAsync(mobileNavData, currentTrailer);
                        }
                    }
                }

                if (mobileNavData.Data.Order.IsBarcodeScanRequired())
                {
                    if (mobileNavData.Data.Order.HasBarcodes())
                    {
                        this.ShowViewModel<BarcodeScanningViewModel>(navID);
                        return;
                    }

                    await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("There are no barcodes to be scanned on this instruction.");
                }

                // Delivery Clean/Clause Prompt
                if (mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver &&
                    !deliveryOptions.BypassCleanClausedScreen)
                {
                    bool isClean = await IsCleanInstructionAsync(navID);

                    if (!isClean)
                        return;
                }

                if ((mobileNavData.Data.Order.Type == Enums.InstructionType.Collect && !itemAdditionalContent.BypassCommentsScreen) ||
                     (mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver && !deliveryOptions.BypassCommentsScreen))
                {
                    bool hasAdvanced = await ConfirmCommentAccessAsync(navID, mobileNavData);

                    if (hasAdvanced)
                        return;
                }

                if (((deliveryOptions.CustomerNameRequiredForDelivery || deliveryOptions.CustomerSignatureRequiredForDelivery) && mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver) ||
                    ((additionalContent.CustomerNameRequiredForCollection || additionalContent.CustomerSignatureRequiredForCollection) && mobileNavData.Data.Order.Type == Enums.InstructionType.Collect))
                {
                    this.ShowViewModel<InstructionSignatureViewModel>(navID);
                    return;
                }

                this.ShowViewModel<ConfirmTimesViewModel>(navID);
            }
        }

        /// <summary>
        /// Instruction trailer screen, if the bypass comment screen is not then enabled then will it redirect to comment screen.
        /// else if the bypass comment screen is enabled and if either either name required or signature required are enabled then redirect to signature screen.
        /// </summary>
        public async Task InstructionTrailer_CustomActionAsync(Guid navID, NavData navData)
        {
            var mobileNavData = navData as NavData<MobileData>;
            var trailer = mobileNavData.OtherData["UpdatedTrailer"] as Models.Trailer;

            var trailerDiffersFromCurrentTrailer =
                trailer != null &&
                (!_infoService.CurrentTrailerID.HasValue || trailer.ID != _infoService.CurrentTrailerID.Value);

            if (trailerDiffersFromCurrentTrailer)
            {
                this.ShowViewModel<InstructionSafetyCheckViewModel>(navID);
            }
            else
            {
                await this.UpdateTrailerForInstructionAsync(mobileNavData, trailer);
                mobileNavData.OtherData["UpdatedTrailer"] = null;

                if (navData.OtherData.IsDefined("IsTrailerEditFromInstructionScreen"))
                {
                    mobileNavData.OtherData["IsTrailerEditFromInstructionScreen"] = null;
                    // Go back to the instruction screen
                    this.ShowViewModel<InstructionViewModel>(navID);
                }
                else
                {
                    // else trailer select was via collection on-site flow
                    await this.CompleteInstructionTrailerSelectionAsync(navID, mobileNavData);
                }
            }
        }

        private async Task CompleteInstructionTrailerSelectionAsync(Guid navID, NavData<MobileData> mobileNavData)
        {
            if (mobileNavData.OtherData.IsDefined("IsProceedFrom"))
            {
                mobileNavData.OtherData["IsProceedFrom"] = null;
                this.ShowViewModel<ConfirmTimesViewModel>(navID);
                return;
            }

            var additionalContent = mobileNavData.Data.Order.Additional;
            var itemAdditionalContent = mobileNavData.Data.Order.Items.First().Additional;

            if (!itemAdditionalContent.BypassCommentsScreen)
            {
                bool hasAdvanced = await ConfirmCommentAccessAsync(navID, mobileNavData);

                if (hasAdvanced)
                    return;
            }

            if (additionalContent.CustomerNameRequiredForCollection || additionalContent.CustomerSignatureRequiredForCollection)
            {
                this.ShowViewModel<InstructionSignatureViewModel>(navID);
                return;
            }

            this.ShowViewModel<ConfirmTimesViewModel>(navID);
        }

        private async Task UpdateTrailerForInstructionAsync(NavData<MobileData> mobileNavData, Models.Trailer trailer)
        {
            // Trailer differs from one on the order
            if (!Models.Trailer.SameAs(trailer, mobileNavData.Data.Order.Additional.Trailer))
            {
                // set the trailer back on the order
                if (trailer == null)
                    mobileNavData.Data.Order.Additional.Trailer = null;
                else
                {
                    if (mobileNavData.Data.Order.Additional.Trailer == null)
                        mobileNavData.Data.Order.Additional.Trailer = new Models.Instruction.Trailer();

                    mobileNavData.Data.Order.Additional.Trailer.TrailerId = trailer.Registration;
                }

                // send the revised trailer data chunk
                await _dataChunkService.SendDataChunkAsync(mobileNavData.GetDataChunk(), mobileNavData.Data, _infoService.CurrentDriverID.Value, _infoService.CurrentVehicleRegistration, updateTrailer: true);
            }

            _infoService.SetCurrentTrailer(trailer);
        }

        public async Task TrailerSafetyCheck_CustomActionAsync(Guid navID, NavData navData)
        {
            var mobileNavData = navData as NavData<MobileData>;
            Models.Trailer trailer = mobileNavData.OtherData["UpdatedTrailer"] as Models.Trailer;
            var trailerSafetyProfile = await this.GetTrailerSafetyProfileAsync(trailer);

            if (trailerSafetyProfile == null)
            {
                //No trailer safety profile
                await this.UpdateTrailerForInstructionAsync(mobileNavData, trailer);

                // Trailer select was via the "Change Trailer" button
                if (navData.OtherData.IsDefined("IsTrailerEditFromInstructionScreen"))
                {
                    navData.OtherData["IsTrailerEditFromInstructionScreen"] = null;
                    this.ShowViewModel<InstructionViewModel>(navID);
                }
                else
                {
                    await this.CompleteInstructionTrailerSelectionAsync(navID, mobileNavData);
                }
            }
            else
            {
                this.ShowViewModel<InstructionSafetyCheckSignatureViewModel>(navID);
            }
        }

        private async Task Barcode_CustomActionAsync(Guid navID, NavData navData)
        {
            var mobileNavData = navData as NavData<MobileData>;

            var additionalContent = mobileNavData.Data.Order.Additional;
            var itemAdditionalContent = mobileNavData.Data.Order.Items.First().Additional;
            var deliveryOptions = mobileNavData.GetWorseCaseDeliveryOptions();

            // Delivery Clean/Clause Prompt
            if (mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver &&
                !deliveryOptions.BypassCleanClausedScreen)
            {
                bool isClean = await IsCleanInstructionAsync(navID);

                if (!isClean)
                    return;
            }

            if ((mobileNavData.Data.Order.Type == Enums.InstructionType.Collect && !itemAdditionalContent.BypassCommentsScreen) ||
                    (mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver && !deliveryOptions.BypassCommentsScreen))
            {
                bool hasAdvanced = await ConfirmCommentAccessAsync(navID, mobileNavData);

                if (hasAdvanced)
                    return;
            }

            if (((deliveryOptions.CustomerNameRequiredForDelivery || deliveryOptions.CustomerSignatureRequiredForDelivery) && mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver) ||
                               ((additionalContent.CustomerNameRequiredForCollection || additionalContent.CustomerSignatureRequiredForCollection) && mobileNavData.Data.Order.Type == Enums.InstructionType.Collect))
            {
                this.ShowViewModel<InstructionSignatureViewModel>(navID);
                return;
            }

            this.ShowViewModel<ConfirmTimesViewModel>(navID);
        }

        /// <summary>
        /// Safety check screen when selecting a new trailer for an instruction. Either moves the use back to
        /// the instruction screen or moves then onto the rest of the collection on site flow
        /// </summary>
        public async Task InstructionSafetyCheckSignature_CustomActionAsync(Guid navID, NavData navData)
        {
            var mobileNavData = navData as NavData<MobileData>;
            Models.Trailer trailer = mobileNavData.OtherData["UpdatedTrailer"] as Models.Trailer;

            await this.UpdateTrailerForInstructionAsync(mobileNavData, trailer);

            _safetyCheckService.CurrentTrailerSafetyCheckData = navData.OtherData["UpdatedTrailerSafetyCheckData"] as SafetyCheckData;

            // commit safety check data to repositories and bluesphere
            await _safetyCheckService.CommitSafetyCheckDataAsync(trailerOnly: true);

            // clear all nav item data related to trailer selection flow
            mobileNavData.OtherData["UpdatedTrailer"] = null;
            mobileNavData.OtherData["UpdatedTrailerSafetyCheckData"] = null;

            // Failed safety checks Trailer select was via the "Change Trailer" button
            if (SafetyCheckStatus == Enums.SafetyCheckStatus.Failed || navData.OtherData.IsDefined("IsTrailerEditFromInstructionScreen"))
            {
                if (SafetyCheckStatus == Enums.SafetyCheckStatus.Failed)
                    await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("As a result of the safety check failure you will be returned to the instruction screen.", "Failed Safety Check");

                mobileNavData.OtherData["IsTrailerEditFromInstructionScreen"] = null;
                this.ShowViewModel<InstructionViewModel>(navID);
                return;
            }
            else
            {
                // otherwise continue with collection on site flow
                await this.CompleteInstructionTrailerSelectionAsync(navID, mobileNavData);
                return;
            }
        }

        public async Task InstructionClaused_CustomActionAsync(Guid navID, NavData navData)
        {
            var mobileNavData = navData as NavData<MobileData>;

            var additionalContent = mobileNavData.Data.Order.Additional;
            var itemAdditionalContent = mobileNavData.Data.Order.Items.First().Additional;
            var deliveryOptions = mobileNavData.GetWorseCaseDeliveryOptions();

            if ((mobileNavData.Data.Order.Type == Enums.InstructionType.Collect && !itemAdditionalContent.BypassCommentsScreen) ||
                 (mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver && !deliveryOptions.BypassCommentsScreen))
            {
                bool hasAdvanced = await ConfirmCommentAccessAsync(navID, mobileNavData);
                if (hasAdvanced)
                    return;
            }

            if (((deliveryOptions.CustomerNameRequiredForDelivery || deliveryOptions.CustomerSignatureRequiredForDelivery) && mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver) ||
                    ((additionalContent.CustomerNameRequiredForCollection || additionalContent.CustomerSignatureRequiredForCollection) && mobileNavData.Data.Order.Type == Enums.InstructionType.Collect))
            {
                this.ShowViewModel<InstructionSignatureViewModel>(navID);
                return;
            }

            this.ShowViewModel<ConfirmTimesViewModel>(navID);
        }

        /// <summary>
        /// Instruction comment screen, if either either name required or signature required are enabled then redirect to signature screen.
        /// </summary>
        public Task InstructionComment_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
            {
                var mobileNavData = navData as NavData<MobileData>;

                var additionalContent = mobileNavData.Data.Order.Additional;
                var itemAdditionalContent = mobileNavData.Data.Order.Items.First().Additional;
                var deliveryOptions = mobileNavData.GetWorseCaseDeliveryOptions();

                var requireSignature =
                    ((deliveryOptions.CustomerNameRequiredForDelivery || deliveryOptions.CustomerSignatureRequiredForDelivery) && mobileNavData.Data.Order.Type == Enums.InstructionType.Deliver) ||
                    ((additionalContent.CustomerNameRequiredForCollection || additionalContent.CustomerSignatureRequiredForCollection) && mobileNavData.Data.Order.Type == Enums.InstructionType.Collect);

                if (requireSignature)
                {
                    this.ShowViewModel<InstructionSignatureViewModel>(navID);
                }
                else
                {
                    this.ShowViewModel<ConfirmTimesViewModel>(navID);
                }
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// We always show this screen 
        /// </summary>
        /// <param name="navData"></param>
        public Task ConfirmTimes_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
            {
                var mobileNavData = navData as NavData<MobileData>;
                return CompleteInstructionAsync(mobileNavData);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Going back from the instruction comment screen we should skip back to instruction on site
        /// so as to avoid going back through the select trailer/safety check workflow (note: collection only)
        /// </summary>
        public Task InstructionComment_CustomBackActionAsync(Guid navID, NavData navData)
        {
            this.ShowViewModel<InstructionOnSiteViewModel>(navID);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Instruction signature screen, complete instruction and go back to manifest screen
        /// </summary>
        public Task InstructionSignature_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
                this.ShowViewModel<ConfirmTimesViewModel>(navID);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Going back from the instruction signature screen we should skip back to instruction on site 
        /// so as to avoid going back through the select trailer/safety check workflow (note: collection only)
        /// </summary>
        public Task InstructionSignature_CustomBackActionAsync(Guid navID, NavData navData)
        {

            if (navData.OtherData.IsDefined("VisitedCommentScreen"))
                this.ShowViewModel<InstructionCommentViewModel>(navID);
            else
                this.ShowViewModel<InstructionOnSiteViewModel>(navID);

            return Task.FromResult(0);
        }

        public Task ConfirmTimes_CustomBackActionAsync(Guid navID, NavData navData)
        {
            var mobileNavData = navData as NavData<MobileData>;

            if (mobileNavData.Data.Order.Type == Enums.InstructionType.ProceedFrom || mobileNavData.Data.Order.Type == Enums.InstructionType.TrunkTo)
                return GoToManifestAsync();

            if (navData.OtherData.IsDefined("VisitedCommentScreen"))
                this.ShowViewModel<InstructionCommentViewModel>(navID);
            else
                this.ShowViewModel<InstructionOnSiteViewModel>(navID);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Instruction TrunkTo/Proceed screens, completes the instruction and goes back to manifest screen
        /// </summary>
        public async Task InstructionTrunkProceed_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
            {
                var mobileNavData = navData as NavData<MobileData>;
                var additionalContent = mobileNavData.Data.Order.Additional;

                if (additionalContent.IsTrailerConfirmationEnabled && mobileNavData.Data.Order.Type == Enums.InstructionType.ProceedFrom)
                {
                    var currentTrailerID = _infoService.CurrentTrailerID;
                    var currentTrailer = currentTrailerID.HasValue ? await _repositories.TrailerRepository.GetByIDAsync(currentTrailerID.Value) : null;
                    var instructionTrailer = mobileNavData.Data.Order.Additional.Trailer;

                    string orderTrailerMessage = (instructionTrailer == null || instructionTrailer.TrailerId == null) ? "No trailer specified on instruction." : string.Format("Trailer specified on instruction is {0}.", instructionTrailer.TrailerId);
                    string currentTrailerMessage = (currentTrailer == null) ? " You currently have no trailer." : string.Format(" Current trailer is {0}.", currentTrailer.Registration);

                    string message = orderTrailerMessage + currentTrailerMessage;
                    var isConfirmed = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync(message, "Change Trailer?", "Select Trailer", "Use Current");

                    if (isConfirmed)
                    {
                        mobileNavData.OtherData["IsProceedFrom"] = true;
                        this.ShowViewModel<InstructionTrailerViewModel>(navID);
                        return;
                    }
                    else
                    {
                        await this.UpdateTrailerForInstructionAsync(mobileNavData, currentTrailer);
                    }
                }

                this.ShowViewModel<ConfirmTimesViewModel>(navID);
            }
        }

        private Task Inbox_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
                this.ShowViewModel<MessageViewModel>(navID);

            return Task.FromResult(0);
        }

        #endregion Custom Mapping Actions

        #region CustomBackActions

        public Task SafetyCheck_CustomBackActionAsync(Guid navID, NavData navData)
        {
            _inLogoutSafetyCheck = false;
            this.ShowViewModel<ManifestViewModel>();
            return Task.FromResult(0);
        }

        public Task Instruction_CustomBackActionAsync(Guid navID, NavData navData)
        {
            this.ShowViewModel<ManifestViewModel>();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Order screen depending on the state of the instruction then it will go to the instruction on site screen if its
        /// Progress: Onsite, else it will go to the instruction screen the other times.
        /// </summary>
        public Task Order_CustomBackActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
            {
                var navMobileData = navData as NavData<MobileData>;

                switch (navMobileData.Data.ProgressState)
                {
                    case MWF.Mobile.Core.Enums.InstructionProgress.NotStarted:
                        this.ShowViewModel<InstructionViewModel>(navID);
                        break;
                    case MWF.Mobile.Core.Enums.InstructionProgress.Driving:
                        this.ShowViewModel<InstructionViewModel>(navID);
                        break;
                    case MWF.Mobile.Core.Enums.InstructionProgress.OnSite:
                        this.ShowViewModel<InstructionOnSiteViewModel>(navID);
                        break;
                }
            }

            return Task.FromResult(0);
        }

        public Task InstructionOnSite_CustomBackActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
                this.ShowViewModel<InstructionViewModel>(navID);

            return Task.FromResult(0);
        }

        public Task SidebarNavigation_CustomActionAsync(Guid navID, NavData navData)
        {
            if (navData is NavData<MobileData>)
            {
                var navMobileData = navData as NavData<MobileData>;

                if (navMobileData.Data == null)
                {
                    this.ShowViewModel<ManifestViewModel>();
                }
                else
                {
                    switch (navMobileData.Data.ProgressState)
                    {
                        case MWF.Mobile.Core.Enums.InstructionProgress.NotStarted:
                            this.ShowViewModel<InstructionViewModel>(navID);
                            break;
                        case MWF.Mobile.Core.Enums.InstructionProgress.Driving:
                            this.ShowViewModel<InstructionViewModel>(navID);
                            break;
                        case MWF.Mobile.Core.Enums.InstructionProgress.OnSite:
                            this.ShowViewModel<InstructionOnSiteViewModel>(navID);
                            break;
                    }
                }
            }
            else
            {
                this.ShowViewModel<ManifestViewModel>();
            }

            return Task.FromResult(0);
        }

        #endregion CustomBackActions

    }

    #region Exception Classes

    public class UnknownNavigationMappingException : Exception
    {
        public UnknownNavigationMappingException(Type activityType, Type fragmentType)
            : base(string.Format("No mapping defined for {0} activity / {1} fragment", activityType, fragmentType))
        {
        }
    }

    public class UnknownBackNavigationMappingException : Exception
    {
        public UnknownBackNavigationMappingException(Type activityType, Type fragmentType)
            : base(string.Format("No backward mapping defined for {0} activity / {1} fragment", activityType, fragmentType))
        {
        }
    }

    #endregion

}
