using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Platform;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Views;
using Moq;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Presentation;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ServiceTests
{

    public class NavigationServiceTests
        : MvxIoCSupportingTest
    {

        #region Setup

        private IFixture _fixture;
        private MockDispatcher _mockViewDispatcher;
        private Mock<ICheckForSoftwareUpdates> _mockCheckForSoftwareUpdates;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMvxMessenger> _mockMessenger;

        private MobileData _mobileData;
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<IApplicationProfileRepository> _mockApplicationProfile;
        private Mock<IDataChunkService> _mockDataChunkService;
        private Mock<IInfoService> _mockInfoService;
        private Mock<ISafetyProfileRepository> _mockSafetyProfileRepository;
        private Mock<IConfigRepository> _mockConfigRepo;
        private Mock<IVehicleRepository> _mockVehicleRepo;
        private Mock<ITrailerRepository> _mockTrailerRepo;

        protected override void AdditionalSetup()
        {
            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Complete Instruction");
            _mockUserInteraction.Setup(mui => mui.ConfirmAsync(It.IsAny<string>(), It.Is<string>(s => s == "Change Trailer?"), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync<ICustomUserInteraction, bool>(true);
            _mockUserInteraction.Setup(mui => mui.ConfirmAsync(It.Is<string>(s => s == "Do you want to enter a comment for this instruction?"), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync<ICustomUserInteraction, bool>(true);

            _mockCheckForSoftwareUpdates = Ioc.RegisterNewMock<ICheckForSoftwareUpdates>();

            Ioc.RegisterSingleton<IMvxStringToTypeParser>(new MvxStringToTypeParser());

            _mockViewDispatcher = new MockDispatcher();
            Ioc.RegisterSingleton<IMvxViewDispatcher>(_mockViewDispatcher);

            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _mockMessenger = new Mock<IMvxMessenger>();
            Ioc.RegisterSingleton<IMvxMessenger>(_mockMessenger.Object);

            _mobileData = _fixture.Create<MobileData>();

            _mockApplicationProfile = _fixture.InjectNewMock<IApplicationProfileRepository>();
            _mockApplicationProfile.Setup(map => map.GetAllAsync()).ReturnsAsync(_fixture.CreateMany<ApplicationProfile>());
            var appProfileMock = _fixture.InjectNewMock<ApplicationProfile>();
            appProfileMock.Object.DeviceCheckInOutRequired = false;
            _mockApplicationProfile.Setup(map => map.GetAsync()).ReturnsAsync(appProfileMock.Object);

            _mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == _mobileData.ID))).ReturnsAsync(_mobileData);

            _mockSafetyProfileRepository = _fixture.InjectNewMock<ISafetyProfileRepository>();

            _mockConfigRepo = _fixture.InjectNewMock<IConfigRepository>();
            _mockConfigRepo.Setup(mcr => mcr.GetAsync()).ReturnsUsingFixture(_fixture);

            _mockVehicleRepo = _fixture.InjectNewMock<IVehicleRepository>();
            _mockTrailerRepo = _fixture.InjectNewMock<ITrailerRepository>();

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _mockDataChunkService = _fixture.InjectNewMock<IDataChunkService>();
            _mockDataChunkService
                .Setup(dcs => dcs.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.IsAny<MobileData>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(0));

            _mockInfoService = _fixture.InjectNewMock<IInfoService>();
            _mockInfoService.Setup(s => s.CurrentDriverID).ReturnsUsingFixture(_fixture);
            _mockInfoService.Setup(s => s.CurrentVehicleID).ReturnsUsingFixture(_fixture);

            var mockDeviceInfo = new Mock<Core.Services.IDeviceInfo>();
            mockDeviceInfo.SetupGet(m => m.IMEI).Returns("123456789012345");
            _fixture.Inject<Core.Services.IDeviceInfo>(mockDeviceInfo.Object);
        }

        private void InjectCustomPresenter<TActivityViewModel, TFragmentViewModel>()
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
        {
            this.InjectCustomPresenter(_fixture.Create<TActivityViewModel>(), _fixture.Create<TFragmentViewModel>());
        }

        private void InjectCustomPresenter<TActivityViewModel, TFragmentViewModel>(TFragmentViewModel fragmentViewModel)
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
        {
            this.InjectCustomPresenter(_fixture.Create<TActivityViewModel>(), fragmentViewModel);
        }

        private void InjectCustomPresenter<TActivityViewModel, TFragmentViewModel>(TActivityViewModel activityViewModel, TFragmentViewModel fragmentViewModel)
            where TActivityViewModel : BaseActivityViewModel
            where TFragmentViewModel : BaseFragmentViewModel
        {
            var mockCustomPresenter = Mock.Of<ICustomPresenter>(cp => cp.CurrentActivityViewModel == activityViewModel && cp.CurrentFragmentViewModel == fragmentViewModel);
            _fixture.Inject<ICustomPresenter>(mockCustomPresenter);
        }

        #endregion

        #region Core Service Tests

        [Fact]
        public void NavigationService_InsertNavAction()
        {

            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            service.InsertNavAction<ActivityViewModel, FragmentViewModel1, FragmentViewModel2>();

            Assert.True(service.NavActionExists<ActivityViewModel, FragmentViewModel1>());

        }

        [Fact]
        public async Task NavigationService_InsertCustomNavAction()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            Object myObj = null;
            Driver driver = _fixture.Create<Driver>();

            NavData<Driver> navData = new NavData<Driver> { Data = driver }; 

            // Specify that from StartUp/CustomerCode view model we should navigate to FragmentViewModel2
            service.InsertCustomNavAction<ActivityViewModel, FragmentViewModel1>((id, data) => { myObj = data; return Task.FromResult(0); });

            Assert.True(service.NavActionExists<ActivityViewModel, FragmentViewModel1>());

            var navAction = service.GetNavAction<ActivityViewModel, FragmentViewModel1>();

            await navAction.Invoke(Guid.NewGuid(), navData);

            Assert.Equal(navData, myObj);

        }

        [Fact]
        public void NavigationService_InsertBackNavAction()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            // Specify that from StartUp/PassCode view model we should navigate back to customer code model
            service.InsertBackNavAction<ActivityViewModel, FragmentViewModel2, FragmentViewModel1>();

            Assert.True(service.BackNavActionExists<ActivityViewModel, FragmentViewModel2>());

        }

        [Fact]
        public async Task NavigationService_InsertCustomBackNavAction()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            Object myObj = null;
            Driver driver = _fixture.Create<Driver>();

            NavData<Driver> navData = new NavData<Driver> { Data = driver }; 

            // Specify that from StartUp/CustomerCode view model we should navigate to FragmentViewModel2
            service.InsertCustomBackNavAction<ActivityViewModel, FragmentViewModel1>((id, data) => { myObj = data; return Task.FromResult(0); });

            Assert.True(service.BackNavActionExists<ActivityViewModel, FragmentViewModel1>());

            var navAction = service.GetBackNavAction(typeof(ActivityViewModel), typeof(FragmentViewModel1));

            await navAction.Invoke(Guid.NewGuid(), navData);

            Assert.Equal(navData, myObj);         

        }

        [Fact]
        public void NavigationService_NavActionExists_NoNavActionDefined()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            Assert.False(service.NavActionExists<ActivityViewModel, FragmentViewModel1>());

        }

        [Fact]
        public async Task NavigationService_GetNavAction()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            // Specify that from StartUp/CustomerCode view model we should navigate to FragmentViewModel2
            service.InsertNavAction<ActivityViewModel, FragmentViewModel1, FragmentViewModel2>();

            var navAction = service.GetNavAction<ActivityViewModel, FragmentViewModel1>();

            Assert.NotNull(navAction);

            // run the nav action
            await navAction.Invoke(Guid.Empty, null);

            //Check that the passcode view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(FragmentViewModel2), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_GetBackNavAction()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            // Specify that from StartUp/APsscode view model we should navigate to FragmentViewModel1
            service.InsertBackNavAction<ActivityViewModel, FragmentViewModel2, FragmentViewModel1>();

            var navAction = service.GetBackNavAction(typeof(ActivityViewModel), typeof(FragmentViewModel2));

            Assert.NotNull(navAction);

            // run the nav action
            await navAction.Invoke(Guid.Empty, null);

            //Check that the customer view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Hints.Count);
            var hint = _mockViewDispatcher.Hints.First();
            Assert.Equal(typeof(FragmentViewModel1), (hint as CloseUpToViewPresentationHint).ViewModelType);

        }


        [Fact]
        public async Task NavigationService_GetNavAction_DynamicOverload()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            // Specify that from StartUp/CustomerCode view model we should navigate to FragmentViewModel2
            service.InsertNavAction<ActivityViewModel, FragmentViewModel1, FragmentViewModel2>();

            var navAction = service.GetNavAction(typeof(ActivityViewModel), typeof(FragmentViewModel1));

            Assert.NotNull(navAction);

            // run the nav action
            await navAction.Invoke(Guid.Empty, null);

            //Check that the passcode view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(FragmentViewModel2), request.ViewModelType);

        }

        [Fact]
        public void NavigationService_GetNavAction_DynamicOverload_InvalidTypes()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            // first class is not a BaseActivityViewModel
            Assert.Throws<ArgumentException>(() => service.GetNavAction(typeof(HttpService), typeof(FragmentViewModel1)));

            //second class is not a BaseFragmentViewModel
            Assert.Throws<ArgumentException>(() => service.GetNavAction(typeof(ActivityViewModel), typeof(HttpService)));

        }

        [Fact]
        public void NavigationService_GetNavAction_NoNavActionDefined()
        {
            base.ClearAll();

            var service = _fixture.Create<NavigationService>();

            var navAction = service.GetNavAction<ActivityViewModel, FragmentViewModel1>();

            Assert.Null(navAction);

        }

        [Fact]
        public async Task NavigationService_MoveToNext()
        {
            base.ClearAll();

            this.InjectCustomPresenter<ActivityViewModel, FragmentViewModel1>();
            var service = _fixture.Create<NavigationService>();

            // Specify that from StartUp/CustomerCode view model we should navigate to FragmentViewModel2
            service.InsertNavAction<ActivityViewModel, FragmentViewModel1, FragmentViewModel2>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the passcode view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(FragmentViewModel2), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_MoveToNext_WithNavData()
        {
            base.ClearAll();

            this.InjectCustomPresenter<ActivityViewModel, FragmentViewModel1>();

            var service = _fixture.Create<NavigationService>();
            Ioc.RegisterSingleton<INavigationService>(service);

            // Specify that from StartUp/CustomerCode view model we should navigate to FragmentViewModel2
            service.InsertNavAction<ActivityViewModel, FragmentViewModel1, FragmentViewModel2>();

            //Create an object to pass through as a parameter 
            var driver = _fixture.Create<Driver>();
            var vehicle = _fixture.Create<Vehicle>();
            var navData = new NavData<Driver> { Data = driver };
            navData.OtherData["vehicle"] = vehicle;

            var navID = await service.MoveToNextAsync(navData);

            //Check that the startup the instruction view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(FragmentViewModel2), request.ViewModelType);

            // check that the nav item guid was passed through correctly
            var parametersObjectOut = request.ParameterValues.First();
            Assert.Equal("navID", parametersObjectOut.Key);
            Assert.Equal(navID.ToString(), parametersObjectOut.Value);

            // check that the nav item can be "re-inflated" by the nav service

            //clear down the nav data
            navData.OtherData = null;
            navData.Data = null;
            navData = service.GetNavData<Driver>(navID);

            Assert.Equal(driver, navData.Data);
            Assert.Equal(vehicle, navData.OtherData["vehicle"]);
        }

        [Fact]
        public async Task NavigationService_MoveToNext_UnknownMapping()
        {
            base.ClearAll();

            this.InjectCustomPresenter<ActivityViewModel, FragmentViewModel1>();

            var service = _fixture.Create<NavigationService>();

            // Don't specify any mappings

            // Attempt to move to the next view model
            await Assert.ThrowsAsync<UnknownNavigationMappingException>(service.MoveToNextAsync);
        }

        [Fact]
        public async Task NavigationService_GoBack()
        {
            base.ClearAll();

            this.InjectCustomPresenter<ActivityViewModel, FragmentViewModel2>();

            var service = _fixture.Create<NavigationService>();

            // Specify that from current view models we should navigate back to FragmentViewModel1
            service.InsertBackNavAction<ActivityViewModel, FragmentViewModel2, FragmentViewModel1>();

            // Move to the next view model
            Assert.True(service.IsBackActionDefined());

            await service.GoBackAsync();

            //Check that the customer view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Hints.Count);
            var hint = _mockViewDispatcher.Hints.First();
            Assert.Equal(typeof(FragmentViewModel1), (hint as CloseUpToViewPresentationHint).ViewModelType);

        }

        [Fact]
        public async Task NavigationService_GoBack_UnknownMapping()
        {
            base.ClearAll();

            this.InjectCustomPresenter<ActivityViewModel, FragmentViewModel1>();

            var service = _fixture.Create<NavigationService>();

            // Don't specify any mappings

            // Attempt to go back
            await Assert.ThrowsAsync<UnknownBackNavigationMappingException>(service.GoBackAsync);
        }

        #endregion

        #region Mapping Tests (test that the service has the mappings to correctly navigate from viewmodel to viewmodel corectly)


        [Fact]
        public async Task NavigationService_Mappings_CustomerCode()
        {
            base.ClearAll();

            // presenter will report the current activity view model as a StartUpViewModel, current fragment model a customer model
            this.InjectCustomPresenter<StartupViewModel, CustomerCodeViewModel>();
 
            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the passcode view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(PasscodeViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_Passcode()
        {
            base.ClearAll();

            // presenter will report the current activity view model as a StartUpViewModel,  current fragment model a passcode model
            this.InjectCustomPresenter<StartupViewModel, PasscodeViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the safetycheck view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(VehicleListViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_PasscodeToDiagnostics()
        {
            base.ClearAll();

            // presenter will report the current activity view model as a StartUpViewModel,  current fragment model a passcode model
            this.InjectCustomPresenter<StartupViewModel, PasscodeViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            NavData<object> navData = new NavData<object>();
            navData.OtherData["Diagnostics"] = true;
            await service.MoveToNextAsync(navData);

            //Check that the diagnostics view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(DiagnosticsViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_DiagnosticsToPasscode()
        {
            base.ClearAll();

            // presenter will report the current activity view model as a StartUpViewModel,  current fragment model a diagnostics model
            this.InjectCustomPresenter<StartupViewModel, DiagnosticsViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the passcode view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(PasscodeViewModel), request.ViewModelType);

        }


        [Fact]
        public async Task NavigationService_Mappings_VehicleList()
        {
            base.ClearAll();

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as a vehicle list view model
            this.InjectCustomPresenter<StartupViewModel, VehicleListViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(TrailerListViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_TrailerList()
        {
            base.ClearAll();

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as a vehicle list view model
            this.InjectCustomPresenter<StartupViewModel, TrailerListViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the safetycheck view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(SafetyCheckViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_SafetyCheck_OdometerRequired()
        {
            base.ClearAll();

            //omit properties causing circular dependencies
            var safetyCheckViewModel = _fixture.Build<SafetyCheckViewModel>().Without(s => s.SafetyCheckItemViewModels).Create();

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as a safety check view model
            this.InjectCustomPresenter<StartupViewModel, SafetyCheckViewModel>(safetyCheckViewModel);

            SetUpOdometerRequired(true);

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the odometer view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(OdometerViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_SafetyCheck_OdometerNotRequired()
        {
            base.ClearAll();

            //omit properties causing circular dependencies
            var safetyCheckViewModel = _fixture.Build<SafetyCheckViewModel>().Without(s => s.SafetyCheckItemViewModels).Create<SafetyCheckViewModel>();

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as a safety check view model
            this.InjectCustomPresenter<StartupViewModel, SafetyCheckViewModel>(safetyCheckViewModel);

            SetUpOdometerRequired(false);

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the signature screen view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(SafetyCheckSignatureViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_SafetyCheck_NoProfiles()
        {
            base.ClearAll();

            //omit properties causing circular dependencies
            var safetyCheckViewModel = _fixture.Build<SafetyCheckViewModel>().Without(s => s.SafetyCheckItemViewModels).Create<SafetyCheckViewModel>();

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as a safety check view model
            this.InjectCustomPresenter<StartupViewModel, SafetyCheckViewModel>(safetyCheckViewModel);

            // Set up so that there are no safety profiles
            SetUpNoSafetyProfiles();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the main activity view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(MainViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_Odometer()
        {
            base.ClearAll();

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as an odometer view model
            this.InjectCustomPresenter<StartupViewModel, OdometerViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(SafetyCheckSignatureViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_Signature_NoFaults()
        {
            base.ClearAll();

            // Set up so that the safety check data has no faults
            SetUpSafetyCheckData(false);

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as a safety check view model
            this.InjectCustomPresenter<StartupViewModel, SafetyCheckSignatureViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the main activity view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(MainViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_Signature_Faults()
        {
            base.ClearAll();

            // Set up so that the safety check data has faults
            SetUpSafetyCheckData(true);

            // presenter will report the current activity view model as StartUpViewModel, current fragment model as a safety check view model
            this.InjectCustomPresenter<StartupViewModel, SafetyCheckSignatureViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the main activity view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(StartupViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_BackMappings_Passcode()
        {
            base.ClearAll();

            var closeApplicationMock = _fixture.InjectNewMock<ICloseApplication>();

            // presenter will report the current activity view model as a StartUpViewModel,  current fragment model a passcode model
            this.InjectCustomPresenter<StartupViewModel, PasscodeViewModel>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.GoBackAsync();

            //Check that the application was closed
            closeApplicationMock.Verify(ca => ca.CloseApp(), Times.Once);
        }

        [Fact]
        public async Task NavigationService_BackMappings_Manifest()
        {
            base.ClearAll();

            _mobileData.Order.Type = InstructionType.Collect;

            var manifestViewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            _fixture.Inject(manifestViewModel);

            _mockVehicleRepo.Setup(vr => vr.GetByIDAsync(It.IsAny<Guid>())).ReturnsUsingFixture(_fixture);

            // presenter will report the current activity view model as a MainViewModel,  current fragment model a passcode model
            this.InjectCustomPresenter<MainViewModel, ManifestViewModel>(manifestViewModel);

            var service = _fixture.Create<NavigationService>();
            // Go back
            await service.GoBackAsync();

            //Check that the startup activity view model was navigated to (passcode
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(StartupViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Manifest_Instructions()
        {
            base.ClearAll();

            _mobileData.Order.Type = InstructionType.Collect;

            var manifestViewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create<ManifestViewModel>();

            // presenter will report the current activity view model as a MainViewModel,  current fragment model a passcode model
            this.InjectCustomPresenter<MainViewModel, ManifestViewModel>(manifestViewModel);

            var service = _fixture.Create<NavigationService>();

            //Create a nav item for a mobile data model
            var navData = new NavData<MobileData> { Data = _mobileData };
            var navID = await service.MoveToNextAsync(navData);

            //Check that the startup the instruction view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);

            // check that the supplied parameters were passed through correctly
            var parametersObjectOut = request.ParameterValues.First();

            Assert.Equal("navID", parametersObjectOut.Key);
            Assert.Equal(navID.ToString(), parametersObjectOut.Value);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_Trailer()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionViewModel>();

            var service = _fixture.Create<NavigationService>();

            var navItem = new NavData<MWF.Mobile.Core.Models.Instruction.MobileData>();
            navItem.OtherData["IsTrailerEditFromInstructionScreen"] = true;

            // Move to the next view model
            await service.MoveToNextAsync(navItem);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionTrailerViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_InstructionOnSite()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionViewModel>();

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData> { Data = _mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionOnSiteViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSiteToCommentScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSiteToCommentScreen_SkipClausedScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, false, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSiteToSignatureScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, true, true, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSiteToConfirmTimes()
        {
            base.ClearAll();

            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ConfirmTimesViewModel), request.ViewModelType);
        }

        #region Trailer Selection via "Change Trailer" Button on instruction screen

        // Tests the case when a user changes trailer via the "change trailer" button on the instruction screen and the trailer they select is the same
        // as the current trailer. Since no safety check logic is required they should be deposited directly back to the instruction screen,
        [Fact]
        public async Task NavigationService_Mappings_Instructions_ChangeTrailer_TrailerToInstruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, InstructionProgress.NotStarted);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);

            var loggedInDriver = _fixture.Create<Core.Models.Driver>();
            _mockInfoService.Setup(s => s.CurrentDriverID).Returns(loggedInDriver.ID);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            navData.OtherData["IsTrailerEditFromInstructionScreen"] = true;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);
        }

        // Tests the case when a user changes trailer via the "change trailer" button on the instruction screen and the trailer they select is the same
        // as the current trailer, but different from the one specified on the order. 
        // Since no safety check logic is required they should be deposited directly back to the instruction screen.
        // Addiotnally, the order should have been updated and the revised trailer data chunk fired
        [Fact]
        public async Task NavigationService_Mappings_Instructions_ChangeTrailer_TrailerToInstruction_UpdateTrailerOnOrder()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, InstructionProgress.NotStarted);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);

            // set the trailer the user has selected to be the same as current trailer (but not the current order)
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;

            navData.OtherData["IsTrailerEditFromInstructionScreen"] = true;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);


            // should have updated the trailer on the order
            Assert.Equal(trailer.Registration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));
        }

        // Tests the case when a user changes trailer via the "change trailer" button on the instruction screen and the trailer they select is the different
        // to the current trailer. Since a new trailer has been selected they should be direted to the safety check screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_ChangeTrailer_TrailerToSafetyCheck()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, InstructionProgress.NotStarted);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Object.SetCurrentTrailer(trailer);

            // set the trailer the user has selected to be the same different to the current trailer
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            navData.OtherData["IsTrailerEditFromInstructionScreen"] = true;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSafetyCheckViewModel), request.ViewModelType);
        }


        // Tests the case on the safety check screen where no safety check profile was detected for the updated trailer, so no signature is required so the
        // user can be directed back to the instruction screen
        // Since the selected trailer differs from the one on the order then the order is updated and the revised trailer chunk set
        [Fact]
        public async Task NavigationService_Mappings_Instructions_ChangeTrailer_SafetyCheck_ToInstruction_UpdateOrder()
        {
            base.ClearAll();

            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyCheckItemViewModels));

            // presenter will report the current activity view model as MainView, current fragment model as a the instruction safety check 
            this.InjectCustomPresenter<MainViewModel, InstructionSafetyCheckViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);

            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            navData.OtherData["IsTrailerEditFromInstructionScreen"] = true;

            var service = _fixture.Create<NavigationService>();

            //safety profile repository will be empty (so no safety profile can be retrieved for the trailer)
            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>());

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that, since no signature is required, the instruction screen is navigated back to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(trailer.Registration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));
        }


        // Tests that that when the instruction safety check signature screen is completed the user is directed back to the instruction screen
        // and the sfaety check data is comitted
        [Fact]
        public async Task NavigationService_Mappings_Instructions_ChangeTrailer_SafetyCheckSignature_ToInstruction()
        {
            base.ClearAll();

            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyCheckItemViewModels));

            // presenter will report the current activity view model as MainView, current fragment model as a the instruction safety check 
            this.InjectCustomPresenter<MainViewModel, InstructionSafetyCheckSignatureViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);

            var safetyCheckServiceMock = _fixture.InjectNewMock<ISafetyCheckService>();

            safetyCheckServiceMock.SetupProperty(x => x.CurrentTrailerSafetyCheckData);

            _fixture.Inject(trailer);

            _mockInfoService.Setup(s => s.CurrentDriverID).ReturnsUsingFixture(_fixture);

            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            var updatedSafetyCheckData = navData.OtherData["UpdatedTrailerSafetyCheckData"] = _fixture.Build<SafetyCheckData>().Without(s => s.EffectiveDateString).Create<SafetyCheckData>();
            navData.OtherData["IsTrailerEditFromInstructionScreen"] = true;

            var service = _fixture.Create<NavigationService>();

            //safety profile repository will be empty (so no safety profile can be retrieved for the trailer)
            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>());

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the instruction screen is navigated back to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(_mockInfoService.Object.CurrentTrailerRegistration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));

            // should have set the current trailer
            Assert.Equal(_mockInfoService.Object.CurrentTrailerID, trailer.ID);
            
            // should have set the safety check data for the trailer
            Assert.Equal(updatedSafetyCheckData, safetyCheckServiceMock.Object.CurrentTrailerSafetyCheckData);

            // check the safety check data was commited
            safetyCheckServiceMock.Verify(ss => ss.CommitSafetyCheckDataAsync(true));
        }

        #endregion

        #region Collection On Site Trailer Select Flow Logic

        // Tests the case when the trailer confirmation setting is enabled and the user elects to select trailer
        // Should send the user to the trailer select screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSiteToTrailerScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Change Trailer?");

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, true, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionTrailerViewModel), request.ViewModelType);
        }

        // Tests the case when the trailer confirmation setting is enabled and the user elects to use the current trailer
        // Should send the user to the trailer select screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSite_UseCurrentTrailer()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            mockUserInteraction.ConfirmAsyncReturnsFalseIfTitleStartsWith("Change Trailer?");

            // trailer prompt enabled, bypass comment, customer signature/name required
            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, true, true, true, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(ss => ss.CurrentTrailerID).Returns(trailer.ID);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            var loggedInDriver = _fixture.Create<Core.Models.Driver>();
            _mockInfoService.Setup(ss => ss.CurrentDriverID).Returns(loggedInDriver.ID);

            var navData = new NavData<MobileData>() { Data = mobileData };

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            // User elected to use the current trailer, so skip to the instruction signature
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();

            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(trailer.Registration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));
        }


        // Tests the case when the trailer confirmation setting is disabled (but the order and current trailer differ) and the user elects to select trailer
        // Should send the user to the trailer select screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSite_TrailerPromptDisabled_ToTrailerScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Change Trailer?");

            //trailer prompt disabled
            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, null);

            // current trailer will be different to that on the order
            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            var navData = new NavData<MobileData>() { Data = mobileData };

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionTrailerViewModel), request.ViewModelType);
        }

        // Tests the case when the trailer confirmation setting is disabled (but the order and current trailer differ) and the user elects to use the current trailer
        // Should send the user to the trailer select screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_InstructionOnSite_TrailerPromptDisabled_UseCurrentTrailer()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            mockUserInteraction.ConfirmAsyncReturnsFalseIfTitleStartsWith("Change Trailer?");

            // trailer prompt enabled, bypass comment, customer signature/name required
            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, true, true, true, false, false, true, null);

            // current trailer will be different to that on the order
            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            var navData = new NavData<MobileData>() { Data = mobileData };

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            // User elected to use the current trailer, so skip to the instruction signature
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();

            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(_mockInfoService.Object.CurrentTrailerRegistration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));
        }

        //Tests the case where a trailer was selected but it was the same as the current trailer
        //Since no safety check logic is required the user is moved directly onto the comment screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_TrailerToCommentScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that, since no safety check logic was required, the comment view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);
        }

        //Tests the case where a trailer was selected but it was the same as the current trailer
        //Since no safety check logic is required the user is moved directly onto the comment screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_TrailerToClausedScreen_SkipClauseScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, false, null);

            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that, since no safety check logic was required, the comment view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);
        }

        //Tests the case where a trailer was selected and since it differs from the current trailer
        //the user is directed to the safety check screen 
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_TrailerToSafetyCheckScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, true, false, false, false, false, true, null);

            var navData = new NavData<MobileData>() { Data = mobileData };

            // different trailer
            navData.OtherData["UpdatedTrailer"] = _fixture.Create<Core.Models.Trailer>();

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            // Should go to safety check screen
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSafetyCheckViewModel), request.ViewModelType);
        }


        // Tests the case where a trailer was selected that is the same as the current railer but differs from the one currently on the order
        // Since no safety check is required the user is directed to the comments scren but the order is updated and the revised trailer data chunk sent 
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_Trailer_ToCommentScreen_UpdateOrder()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that, since no safety check logic was required, the comment view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(_mockInfoService.Object.CurrentTrailerRegistration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));
        }


        //Tests the case where a trailer was selected but it was the same as the current trailer, comment bypass is enabled and
        //a signature is required
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_TrailerToSignatureScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, true, true, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);

            var loggedInDriver = _fixture.Create<Core.Models.Driver>();
            _mockInfoService.Setup(s => s.CurrentDriverID).Returns(loggedInDriver.ID);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);
        }

        [Fact]
        //Tests the case where a trailer was selected but it was the same as the current trailer (i.e no safety check logic)
        //and the bypass comment option is enabled
        public async Task NavigationService_Mappings_Instructions_Collection_TrailerToConfirmTimes()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);

            var loggedInDriver = _fixture.Create<Core.Models.Driver>();
            _mockInfoService.Setup(s => s.CurrentDriverID).Returns(loggedInDriver.ID);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ConfirmTimesViewModel), request.ViewModelType);
        }

        // Tests the case on the safety check screen where no safety check profile was detected for the updated trailer, so no signature is required so the comment screen can be navigated to
        // Since the selected trailer differs from the one on the order then the order is updated and the revised trailer chunk set
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_SafetyCheck_ToCommentScreen_UpdateOrder()
        {
            base.ClearAll();

            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyCheckItemViewModels));

            // presenter will report the current activity view model as MainView, current fragment model as a the instruction safety check 
            this.InjectCustomPresenter<MainViewModel, InstructionSafetyCheckViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;

            var service = _fixture.Create<NavigationService>();

            //safety profile repository will be empty (so no safety profile can be retrieved for the trailer)
            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>());

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that, since no safety check logic was required, the comment view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(_mockInfoService.Object.CurrentTrailerRegistration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));
        }


        // Tests the case on the safety check screen where a safety check has been completed so the user should be directed to the
        // safety check signature screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_SafetyCheck_ToSafetyCheckSignatureScreen()
        {
            base.ClearAll();

            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyCheckItemViewModels).With(x => x.IsProgressing, false));

            // presenter will report the current activity view model as MainView, current fragment model as a the instruction safety check 
            this.InjectCustomPresenter<MainViewModel, InstructionSafetyCheckViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockTrailerRepo.Setup(tr => tr.GetByIDAsync(trailer.ID)).ReturnsAsync(trailer);

            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;

            var service = _fixture.Create<NavigationService>();

            //safety profile repository will return a safety profile for the trailer
            var safetyCheckProfile = _fixture.Create<SafetyProfile>();
            safetyCheckProfile.IntLink = trailer.SafetyCheckProfileIntLink;
            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>() { safetyCheckProfile});

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that, since no safety check logic was required, the comment view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSafetyCheckSignatureViewModel), request.ViewModelType);
          
        }

        // Tests that that when the instruction safety check signature screen is completed the user is directed onto the comments scrteen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_ChangeTrailer_SafetyCheckSignature_ToCommentScreen()
        {
            base.ClearAll();

            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyCheckItemViewModels));

            // presenter will report the current activity view model as MainView, current fragment model as a the instruction safety check 
            this.InjectCustomPresenter<MainViewModel, InstructionSafetyCheckSignatureViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);

            var safetyCheckServiceMock = _fixture.InjectNewMock<ISafetyCheckService>();
            safetyCheckServiceMock.SetupProperty(x => x.CurrentTrailerSafetyCheckData);

            _mockInfoService.Setup(s => s.CurrentDriverID).ReturnsUsingFixture(_fixture);

            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            var updatedSafetyCheckData = navData.OtherData["UpdatedTrailerSafetyCheckData"] = _fixture.Build<SafetyCheckData>().Without(s => s.EffectiveDateString).Create<SafetyCheckData>();

            var service = _fixture.Create<NavigationService>();

            //safety profile repository will be empty (so no safety profile can be retrieved for the trailer)
            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>());

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the comment screen is navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(_mockInfoService.Object.CurrentTrailerRegistration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));

            // should have set the current trailer
            Assert.Equal(_mockInfoService.Object.CurrentTrailerID, trailer.ID);

            // should have set the safety check data for the trailer
            Assert.Equal(updatedSafetyCheckData, safetyCheckServiceMock.Object.CurrentTrailerSafetyCheckData);

            // check the safety check data was commited
            safetyCheckServiceMock.Verify(ss => ss.CommitSafetyCheckDataAsync(true));
        }

        // Tests that that when the instruction safety check signature screen is completed and there are faults the user is directed 
        // back to the instruction screen
        [Fact]
        public async Task NavigationService_Mappings_Instructions_ChangeTrailer_SafetyCheckSignature_FailedChecks_ToInstructionScreen()
        {
            base.ClearAll();

            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyCheckItemViewModels));

            // presenter will report the current activity view model as MainView, current fragment model as a the instruction safety check 
            this.InjectCustomPresenter<MainViewModel, InstructionSafetyCheckSignatureViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, false, false, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);
            _mockInfoService.Setup(s => s.CurrentTrailerRegistration).Returns(trailer.Registration);

            var safetyCheckServiceMock = _fixture.InjectNewMock<ISafetyCheckService>();
            safetyCheckServiceMock.SetupProperty(x => x.CurrentTrailerSafetyCheckData);

            _mockInfoService.Setup(s => s.CurrentDriverID).ReturnsUsingFixture(_fixture);

            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;

            SafetyCheckData updatedSafetyCheckData;
            navData.OtherData["UpdatedTrailerSafetyCheckData"] = updatedSafetyCheckData = _fixture.Build<SafetyCheckData>().Without(s => s.EffectiveDateString).Create<SafetyCheckData>();
            //add a failed check
            updatedSafetyCheckData.Faults.Add(new SafetyCheckFault() { Status = SafetyCheckStatus.Failed });

            var service = _fixture.Create<NavigationService>();

            //safety profile repository will be empty (so no safety profile can be retrieved for the trailer)
            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>());

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the instruction screen is navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);

            // should have updated the trailer on the order
            Assert.Equal(_mockInfoService.Object.CurrentTrailerRegistration, mobileData.Order.Additional.Trailer.TrailerId);

            // datachunk service should have been hit to send the revised trailer data chunk
            _mockDataChunkService.Verify(mds => mds.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.Is<MobileData>(md => md == mobileData), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(b => !b), It.Is<bool>(b => b)));

            // should have set the current trailer
            Assert.Equal(_mockInfoService.Object.CurrentTrailerID, trailer.ID);

            // should have set the safety check data for the trailer
            Assert.Equal(updatedSafetyCheckData, safetyCheckServiceMock.Object.CurrentTrailerSafetyCheckData);

            // check the safety check data was commited
            safetyCheckServiceMock.Verify(ss => ss.CommitSafetyCheckDataAsync(true));
        }

        #endregion

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_CommentToSignatureScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionCommentViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, true, true, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_CommentToConfirmTimes()
        {
            base.ClearAll();

            this.InjectCustomPresenter<MainViewModel, InstructionCommentViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the confirm times view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ConfirmTimesViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Collection_ConfirmTimesToComplete()
        {
            base.ClearAll();

            this.InjectCustomPresenter<MainViewModel, ConfirmTimesViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the manifest view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_InstructionOnSiteSkipTrailerScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, false, true, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_InstructionOnSiteToInstructionClaused()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, false, false, false, false, false, false, false, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionClausedViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_InstructionOnSiteToCommentScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, false, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_InstructionOnSiteToSignatureScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, true, true, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_InstructionOnSiteToConfirmTimes()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ConfirmTimesViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_ClausedToComment()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionClausedViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, false, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionCommentViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_ClausedToSignature()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionClausedViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, true, true, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_ClausedToConfirmTimes()
        {
            base.ClearAll();

            this.InjectCustomPresenter<MainViewModel, InstructionClausedViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ConfirmTimesViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_ConfirmTimesToComplete()
        {
            base.ClearAll();

            this.InjectCustomPresenter<MainViewModel, ConfirmTimesViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_CommentToSignatureScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionCommentViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, true, true, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_Delivery_CommentToConfirmTimes()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionCommentViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ConfirmTimesViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_OrderToReviseQuantity()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an order view model
            this.InjectCustomPresenter<MainViewModel, OrderViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, false, false, false, false, true, null);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the Revise Quantity screen was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ReviseQuantityViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_OrderToInstruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an order view view model
            this.InjectCustomPresenter<MainViewModel, OrderViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, true, false, false, false, false, true, MWF.Mobile.Core.Enums.InstructionProgress.Driving);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the Instruction view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_OrderToInstructionOnSite()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, OrderViewModel>();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, true, false, false, false, false, true, MWF.Mobile.Core.Enums.InstructionProgress.OnSite);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionOnSiteViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_InstructionOnSiteToInstruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();


            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = null };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_InstructionToManifest()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionViewModel>();


            var service = _fixture.Create<NavigationService>();


            // Move to the next view model
            await service.GoBackAsync();

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Instructions_SkipCommentToSignatureScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction trailer view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrailerViewModel>();

            _mockUserInteraction.Setup(mui => mui.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync<ICustomUserInteraction, bool>(false);

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, null);

            var trailer = _fixture.Create<MWF.Mobile.Core.Models.Trailer>();
            _mockInfoService.Setup(s => s.CurrentTrailerID).Returns(trailer.ID);

            var loggedInDriver = _fixture.Create<Core.Models.Driver>();
            _mockInfoService.Setup(s => s.CurrentDriverID).Returns(loggedInDriver.ID);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            navData.OtherData["UpdatedTrailer"] = trailer;
            mobileData.Order.Additional.Trailer.TrailerId = trailer.Registration;

            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionSignatureViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Camera_NoInstruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as a camera view model
            this.InjectCustomPresenter<MainViewModel, SidebarCameraViewModel>();


            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = null };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Camera_Instruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, SidebarCameraViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.NotStarted);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Camera_InstructionOnSite()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as a camera view model
            this.InjectCustomPresenter<MainViewModel, SidebarCameraViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.OnSite);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionOnSiteViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_Camera_NoInstruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, SidebarCameraViewModel>();


            var mobileDataRepositoryMock = _fixture.InjectNewMock<IMobileDataRepository>();
            mobileDataRepositoryMock.Setup(mdr => mdr.GetByIDAsync(It.IsAny<Guid>())).ReturnsAsync(null);

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = null };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_Camera_Instruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, SidebarCameraViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.NotStarted);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_Camera_InstructionOnSite()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, SidebarCameraViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.OnSite);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.GoBackAsync(navData);

            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionOnSiteViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_DisplaySafetyCheck_NoInstruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, DisplaySafetyCheckViewModel>();



            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = null };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_DisplaySafetyCheck_Instruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, DisplaySafetyCheckViewModel>();


            _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.NotStarted);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = _mobileData };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_DisplaySafetyCheck_InstructionOnSite()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, DisplaySafetyCheckViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.OnSite);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionOnSiteViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_DisplaySafetyCheck_NoInstruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, DisplaySafetyCheckViewModel>();




            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = null };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_DisplaySafetyCheck_Instruction()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, DisplaySafetyCheckViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.NotStarted);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.GoBackAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_BackAction_DisplaySafetyCheck_InstructionOnSite()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, DisplaySafetyCheckViewModel>();


            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, true, false, false, true, Core.Enums.InstructionProgress.OnSite);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionOnSiteViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_Manifest_TrunkTo()
        {
            base.ClearAll();

            _mobileData.Order.Type = InstructionType.TrunkTo;

            var manifestViewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create<ManifestViewModel>();

            // presenter will report the current activity view model as a MainViewModel,  current fragment model a passcode model
            this.InjectCustomPresenter<MainViewModel, ManifestViewModel>(manifestViewModel);

            var service = _fixture.Create<NavigationService>();

            //Create a nav item for a mobile data model
            NavData<MobileData> parametersObjectIn = new NavData<MobileData> { Data = _mobileData };
            await service.MoveToNextAsync(parametersObjectIn);

            //Check that the startup the instruction view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionTrunkProceedViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_Manifest_ProceedFrom()
        {
            base.ClearAll();

            _mobileData.Order.Type = InstructionType.ProceedFrom;

            var manifestViewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create<ManifestViewModel>();

            // presenter will report the current activity view model as a MainViewModel,  current fragment model a passcode model
            this.InjectCustomPresenter<MainViewModel, ManifestViewModel>(manifestViewModel);

            var service = _fixture.Create<NavigationService>();

            //Create a nav item for a mobile data model
            NavData<MobileData> parametersObjectIn = new NavData<MobileData> { Data = _mobileData };
            await service.MoveToNextAsync(parametersObjectIn);

            //Check that the startup the instruction view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionTrunkProceedViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_TrunkTo_TrailerSelect()
        {
            base.ClearAll();

            _mobileData.Order.Type = InstructionType.ProceedFrom;

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionTrunkProceedViewModel>();


            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Change Trailer?");

            var service = _fixture.Create<NavigationService>();

            //Create a nav item for a mobile data model
            NavData<MobileData> navData = new NavData<MobileData> { Data = _mobileData };
            _mobileData.Order.Additional.IsTrailerConfirmationEnabled = true;

            await service.MoveToNextAsync(navData);

            //Check that the trailer select screen was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(InstructionTrailerViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_TrunkTo_ConfirmTimes()
        {
            base.ClearAll();

            _mobileData.Order.Type = InstructionType.TrunkTo;

            this.InjectCustomPresenter<MainViewModel, InstructionTrunkProceedViewModel>();

            var service = _fixture.Create<NavigationService>();

            //Create a nav item for a mobile data model
            NavData<MobileData> navData = new NavData<MobileData> { Data = _mobileData };
            _mobileData.Order.Additional.IsTrailerConfirmationEnabled = false;

            await service.MoveToNextAsync(navData);

            //Check that the confirm times view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ConfirmTimesViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_TrunkTo_Manifest()
        {
            base.ClearAll();

            _mobileData.Order.Type = InstructionType.TrunkTo;

            this.InjectCustomPresenter<MainViewModel, ConfirmTimesViewModel>();

            var service = _fixture.Create<NavigationService>();

            //Create a nav item for a mobile data model
            NavData<MobileData> navData = new NavData<MobileData> { Data = _mobileData };
            _mobileData.Order.Additional.IsTrailerConfirmationEnabled = false;

            await service.MoveToNextAsync(navData);

            //Check that the manifest view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_OnSite_BarcodeScreen()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionOnSiteViewModel>();

            //Skip trailer selection and go straight to barcode screen
            _mockUserInteraction.ConfirmAsyncReturnsFalseIfTitleStartsWith("Change Trailer?");

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, false, false, false, false, true, true, true, Core.Enums.InstructionProgress.OnSite);

            var service = _fixture.Create<NavigationService>();

            var navData = new NavData<MobileData>() { Data = mobileData };

            // Move to the next view model
            await service.MoveToNextAsync(navData);

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(BarcodeScanningViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_LogoutSafetyCheck()
        {
            base.ClearAll();

            //presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionViewModel>();

            SetUpLogOffSafetyCheckRequired(true);
            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.LogoutAsync();

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(SafetyCheckViewModel), request.ViewModelType);

        }

        [Fact]
        public async Task NavigationService_Mappings_NoLogoutSafetyCheck()
        {
            base.ClearAll();

            // presenter will report the current activity view model as MainView, current fragment model as an instruction view model
            this.InjectCustomPresenter<MainViewModel, InstructionViewModel>();

            SetUpLogOffSafetyCheckRequired(false);
            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.LogoutAsync();

            //Check that the trailer list view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(StartupViewModel), request.ViewModelType);
        }

        [Fact]
        public async Task NavigationService_Mappings_DiagnosticsToManifest()
        {
            base.ClearAll();

            // presenter will report the current activity view model as a mainViewModel,  current fragment model a diagnostics model
            this.InjectCustomPresenter<MainViewModel, DiagnosticsViewModel>();


            var service = _fixture.Create<NavigationService>();

            // Move to the next view model
            await service.MoveToNextAsync();

            //Check that the manifest view model was navigated to
            Assert.Equal(1, _mockViewDispatcher.Requests.Count);
            var request = _mockViewDispatcher.Requests.First();
            Assert.Equal(typeof(ManifestViewModel), request.ViewModelType);

        }


        #endregion

        #region Helper Functions

        private void SetUpOdometerRequired(bool required)
        {
            //create vehicle and a safety profile that link together in repositories
            var vehicle = _fixture.Create<Vehicle>();
            var safetyProfile = _fixture.Create<SafetyProfile>();
            safetyProfile.IntLink = vehicle.SafetyCheckProfileIntLink;
            safetyProfile.OdometerRequired = required;

            var safetyProfileRepositoryMock = _fixture.InjectNewMock<ISafetyProfileRepository>();
            safetyProfileRepositoryMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>() {safetyProfile}) ;

            _mockVehicleRepo.Setup(vr => vr.GetByIDAsync(vehicle.ID)).ReturnsAsync(vehicle);
            _mockInfoService.Setup(s => s.CurrentVehicleID).Returns(vehicle.ID);

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

        }

        private void SetUpLogOffSafetyCheckRequired(bool required)
        {
            //create vehicle and a safety profile that link together in repositories
            var vehicle = _fixture.Create<Vehicle>();
            var safetyProfile = _fixture.Create<SafetyProfile>();
            safetyProfile.IntLink = vehicle.SafetyCheckProfileIntLink;
            safetyProfile.DisplayAtLogoff = required;

            var safetyProfileRepositoryMock = _fixture.InjectNewMock<ISafetyProfileRepository>();
            safetyProfileRepositoryMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>() { safetyProfile });

            _mockVehicleRepo.Setup(vr => vr.GetByIDAsync(vehicle.ID)).ReturnsAsync(vehicle);
            _mockInfoService.Setup(s => s.CurrentVehicleID).Returns(vehicle.ID);

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

        }

        private void SetUpNoSafetyProfiles()
        {
            //create vehicle and a safety profile that link together in repositories
            var vehicle = _fixture.Create<Vehicle>();
            var safetyProfile = _fixture.Create<SafetyProfile>();
            safetyProfile.IntLink = vehicle.SafetyCheckProfileIntLink;
            safetyProfile.OdometerRequired = true;

            //No safety profiles
            var safetyProfileRepositoryMock = _fixture.InjectNewMock<ISafetyProfileRepository>();
            safetyProfileRepositoryMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>());

            _mockVehicleRepo.Setup(vr => vr.GetByIDAsync(vehicle.ID)).ReturnsAsync(vehicle);
            _mockInfoService.Setup(s => s.CurrentVehicleID).Returns(vehicle.ID);

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

        }

        private void SetUpSafetyCheckData(bool faults)
        {
            // Create safety check
            var latestSafetyCheck = _fixture.Build<SafetyCheckData>().Without(s => s.EffectiveDateString).Create<LatestSafetyCheck>();

            //omit properties causing circular dependencies
            var safetyCheckViewModel = _fixture.Build<SafetyCheckViewModel>().Without(s => s.SafetyCheckItemViewModels).Create<SafetyCheckViewModel>();

            IEnumerable<SafetyCheckData> safetyCheckData = new List<SafetyCheckData>() { latestSafetyCheck.VehicleSafetyCheck };

            if (!faults)
            {
                foreach (var fault in latestSafetyCheck.VehicleSafetyCheck.Faults)
                {
                    fault.Status = Core.Enums.SafetyCheckStatus.Passed;
                }
                
            }
            else
            {
               latestSafetyCheck.VehicleSafetyCheck.Faults[0].Status = Core.Enums.SafetyCheckStatus.Failed;               
            }

            var safetyCheckService = Mock.Of<ISafetyCheckService>(ss => ss.GetCurrentSafetyCheckData() == safetyCheckData);
            _fixture.Inject<ISafetyCheckService>(safetyCheckService);

        }

        #endregion

    }

    #region Test Classes

    public class ActivityViewModel : BaseActivityViewModel
    {
        public ActivityViewModel(IMvxViewModelLoader viewModelLoader) : base(viewModelLoader)
        { }
    }

    public class FragmentViewModel1 : BaseFragmentViewModel
    {
        public override string FragmentTitle { get { return "FragmentViewModel1"; } }
    }

    public class FragmentViewModel2 : BaseFragmentViewModel
    {
        public override string FragmentTitle { get { return "FragmentViewModel2"; } }
    }

    #endregion
}
