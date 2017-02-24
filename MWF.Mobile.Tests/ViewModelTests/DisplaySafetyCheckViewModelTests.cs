using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class DisplaySafetyCheckViewModelTests
        : MvxIoCSupportingTest
    {

        #region Setup

        private IFixture _fixture;
        private LatestSafetyCheck _latestSafetyCheck;
        private MobileData _mobileData;
        private Mock<INavigationService> _mockNavigationService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IInfoService> _mockInfoService;
        private Mock<ILatestSafetyCheckRepository> _mockLatestSafetyCheckRepository;
        private Mock<IMvxMessenger> _mockMessenger;
        private Mock<IRepositories> _mockRepositories;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.OmitProperty("EffectiveDateString");

            _latestSafetyCheck = _fixture.Create<LatestSafetyCheck>();
            _mobileData = _fixture.Create<MobileData>();

            _mockInfoService = _fixture.InjectNewMock<IInfoService>();
            _mockInfoService.Setup(m => m.CurrentDriverID).Returns(_fixture.Create<Driver>().ID);

            _mockRepositories = _fixture.InjectNewMock<IRepositories>();
            Ioc.RegisterSingleton<IRepositories>(_mockRepositories.Object);

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();
            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);

            _mockLatestSafetyCheckRepository = _fixture.InjectNewMock<ILatestSafetyCheckRepository>();
            _mockLatestSafetyCheckRepository.Setup(mls => mls.GetForDriverAsync(It.IsAny<Guid>())).ReturnsAsync(_latestSafetyCheck);
            _mockRepositories.Setup(r => r.LatestSafetyCheckRepository).Returns(_mockLatestSafetyCheckRepository.Object);

            var mobileDataRepository = Mock.Of<IMobileDataRepository>(mdr => mdr.GetByIDAsync(_mobileData.ID) == Task.FromResult(_mobileData));
            _mockRepositories.Setup(r => r.MobileDataRepository).Returns(mobileDataRepository);

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();
            _mockUserInteraction.ConfirmReturnsTrue();
            _mockUserInteraction.AlertInvokeAction();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());
        }

        #endregion Setup

        #region Tests

        [Fact]
        public async Task DisplaySafetyCheckVM_VehicleRegistration_NotNull()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Vehicle: " + _latestSafetyCheck.VehicleSafetyCheck.VehicleRegistration, displaySafetyCheckVM.VehicleRegistration);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_VehicleRegistration_Null()
        {
            base.ClearAll();

            _latestSafetyCheck.VehicleSafetyCheck = null;
            _mockLatestSafetyCheckRepository.Setup(mls => mls.GetForDriverAsync(It.IsAny<Guid>())).ReturnsAsync(_latestSafetyCheck);

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Vehicle: " , displaySafetyCheckVM.VehicleRegistration);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_TrailerRegistration_NotNull()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Trailer: " + _latestSafetyCheck.TrailerSafetyCheck.VehicleRegistration, displaySafetyCheckVM.TrailerRegistration);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_TrailerRegistration_Null()
        {
            base.ClearAll();

            _latestSafetyCheck.TrailerSafetyCheck = null;
            _mockLatestSafetyCheckRepository.Setup(mls => mls.GetForDriverAsync(It.IsAny<Guid>())).ReturnsAsync(_latestSafetyCheck);

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Trailer: ", displaySafetyCheckVM.TrailerRegistration);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_VehicleSafetyCheckStatus_NotNull()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Checked: " + _latestSafetyCheck.VehicleSafetyCheck.EffectiveDate.ToString("g"), displaySafetyCheckVM.VehicleSafetyCheckStatus);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_VehicleSafetyCheckStatus_Null()
        {
            base.ClearAll();

            _latestSafetyCheck.VehicleSafetyCheck = null;
            _mockLatestSafetyCheckRepository.Setup(mls => mls.GetForDriverAsync(It.IsAny<Guid>())).ReturnsAsync(_latestSafetyCheck);

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Checked: ", displaySafetyCheckVM.VehicleSafetyCheckStatus);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_TrailerSafetyCheckStatus_NotNull()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Checked: " + _latestSafetyCheck.TrailerSafetyCheck.EffectiveDate.ToString("g"), displaySafetyCheckVM.TrailerSafetyCheckStatus);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_TrailerSafetyCheckStatus_Null()
        {
            base.ClearAll();

            _latestSafetyCheck.TrailerSafetyCheck = null;
            _mockLatestSafetyCheckRepository.Setup(mls => mls.GetForDriverAsync(It.IsAny<Guid>())).ReturnsAsync(_latestSafetyCheck);

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            Assert.Equal("Checked: ", displaySafetyCheckVM.TrailerSafetyCheckStatus);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_CreationOfSafetyCheckFaultItemViewModels()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Build<DisplaySafetyCheckViewModel>().Without(dscvm => dscvm.SafetyCheckFaultItemViewModels).Create();

            int faultsCount = _latestSafetyCheck.TrailerSafetyCheck.Faults.Count() + _latestSafetyCheck.VehicleSafetyCheck.Faults.Count();
            await displaySafetyCheckVM.Init();

            Assert.Equal(faultsCount, displaySafetyCheckVM.SafetyCheckFaultItemViewModels.Count());

            int vehicleCount = 0;
            int trailerCount = 0;

            foreach (var safetyCheckFaultVM in displaySafetyCheckVM.SafetyCheckFaultItemViewModels)
            {
                if (safetyCheckFaultVM.FaultType == "VEH")
                    vehicleCount++;
                else if (safetyCheckFaultVM.FaultType == "TRL")
                    trailerCount++;
            }

            Assert.Equal(3, vehicleCount);
            Assert.Equal(3, trailerCount);
            
        }

        /// <summary>
        /// This test is to make sure the pop up only appears when a comment has been entered.
        /// </summary>
        [Fact]
        public void DisplaySafetyCheckVM_SafetyCheckCommentPopUpWithComment()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();

            var fault = _fixture.Create<DisplaySafetyCheckFaultItemViewModel>();

            displaySafetyCheckVM.ShowSafetyCheckFaultCommand.Execute(fault);

            _mockUserInteraction.Verify(mui => mui.Alert(It.IsAny<string>(), It.IsAny<Action>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// This test is to make sure the pop up doesn't appears when a comment has not  been entered.
        /// </summary>
        [Fact]
        public void DisplaySafetyCheckVM_SafetyCheckCommentPopUpWithoutComment()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();

            var fault = _fixture.Create<DisplaySafetyCheckFaultItemViewModel>();
            fault.FaultCheckComment = string.Empty;

            displaySafetyCheckVM.ShowSafetyCheckFaultCommand.Execute(fault);

            _mockUserInteraction.Verify(mui => mui.Alert(It.IsAny<string>(), It.IsAny<Action>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_NullSafetyChecks()
        {
            base.ClearAll();

            _latestSafetyCheck.VehicleSafetyCheck = null;
            _latestSafetyCheck.TrailerSafetyCheck = null;
            _mockLatestSafetyCheckRepository.Setup(mls => mls.GetForDriverAsync(It.IsAny<Guid>())).ReturnsAsync(_latestSafetyCheck);

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            await displaySafetyCheckVM.Init();

            _mockUserInteraction.Verify(mui => mui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockNavigationService.Verify(ns => ns.MoveToNextAsync(It.Is<NavData<MobileData>>(ni => ni == null)), Times.Once);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_CheckInstructionNotification_Delete()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            displaySafetyCheckVM.IsVisible = true;

            _mockNavigationService.SetupGet(x => x.CurrentNavData).Returns(new NavData<MobileData>() { Data = _mobileData });

            await displaySafetyCheckVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Delete));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockNavigationService.Verify(ns => ns.GoToManifestAsync(), Times.Once);
        }

        [Fact]
        public async Task DisplaySafetyCheckVM_CheckInstructionNotification_Update_Confirm()
        {
            base.ClearAll();

            var displaySafetyCheckVM = _fixture.Create<DisplaySafetyCheckViewModel>();
            displaySafetyCheckVM.IsVisible = true;

            _mockNavigationService.SetupGet(x => x.CurrentNavData).Returns(new NavData<MobileData>() { Data = _mobileData });

            await displaySafetyCheckVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion Tests
    }
}
