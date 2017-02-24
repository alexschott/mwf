using SQLite.Net.Attributes;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MWF.Mobile.Core.Repositories.Interfaces;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Extensions;
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class InstructionViewModelTests
        : MvxIoCSupportingTest
    {
        #region Setup

        private IFixture _fixture;
        private MobileData _mobileData;
        private Mock<IRepositories> _mockRepositories;
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<INavigationService> _mockNavigationService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IInfoService> _mockInfoService;
        private Mock<IMvxMessenger> _mockMessenger;
        private NavData<MobileData> _navData;
        private Guid _navID;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _mobileData = _fixture.Create<MobileData>();
            _mobileData.GroupTitle = "Run1010";

            _mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == _mobileData.ID))).ReturnsAsync(_mobileData);

            _mockRepositories = _fixture.InjectNewMock<IRepositories>();
            _mockRepositories.Setup(r => r.MobileDataRepository).Returns(_mockMobileDataRepo.Object);
            Ioc.RegisterSingleton<IRepositories>(_mockRepositories.Object);

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            _mockInfoService = _fixture.InjectNewMock<IInfoService>();
            _mockInfoService.Setup(s => s.CurrentDriverID).ReturnsUsingFixture(_fixture);
            _mockInfoService.Setup(s => s.CurrentVehicleID).ReturnsUsingFixture(_fixture);

            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);

            _navData = new NavData<MobileData>() { Data = _mobileData };
            _navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<MobileData>(_navID)).Returns(_navData);
        }

        #endregion Setup

        #region Test

        [Fact]
        public void InstructionVM_FragmentTitle()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Type.ToString(), instructionVM.FragmentTitle);

        }

        [Fact]
        public void InstructionVM_RunID()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.Equal(_mobileData.Order.RouteTitle, instructionVM.RunID);

        }

        [Fact]
        public void InstructionVM_ArriveDateTime()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Arrive.ToStringIgnoreDefaultDate(), instructionVM.ArriveDateTime);

        }

        [Fact]
        public void InstructionVM_ArriveDateTime_BlankIfDefaultDate()
        {
            base.ClearAll();

            _mobileData.Order.Arrive = new DateTime();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.Equal(string.Empty, instructionVM.ArriveDateTime);

        }

        [Fact]
        public void InstructionVM_DepartDateTime()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Depart.ToStringIgnoreDefaultDate(), instructionVM.DepartDateTime);

        }

        [Fact]
        public void InstructionVM_DepartDateTime_BlankIfDefaultDate()
        {
            base.ClearAll();

            _mobileData.Order.Depart = new DateTime();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.Equal(string.Empty, instructionVM.DepartDateTime);

        }

        [Fact]
        public void InstructionVM_Address()
        {
            base.ClearAll();

            _mobileData.Order.Addresses[0].Lines = "Testline1|TestLine2|TestLine3";

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Addresses[0].Lines.Replace("|", "\n") + "\n" + _mobileData.Order.Addresses[0].Postcode, instructionVM.Address);

        }

        [Fact]
        public async Task InstructionVM_ProgressButtonText_NotStarted()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            _mobileData.ProgressState = Core.Enums.InstructionProgress.NotStarted;

            instructionVM.Init(_navID);

            Assert.Equal("Drive", instructionVM.ProgressButtonText);

            await instructionVM.ProgressInstructionAsync();

            Assert.True(_mobileData.ProgressState == Core.Enums.InstructionProgress.Driving);

            _mockMobileDataRepo.Verify(mdr => mdr.UpdateAsync(It.Is<MobileData>(md => md == _mobileData)), Times.Once);

        }

        [Fact]
        public async Task InstructionVM_ProgressButton_Driving()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            _mobileData.ProgressState = Core.Enums.InstructionProgress.Driving;

            instructionVM.Init(_navID);

            Assert.Equal("On Site", instructionVM.ProgressButtonText);

            await instructionVM.ProgressInstructionAsync();

            Assert.True(_mobileData.ProgressState == Core.Enums.InstructionProgress.OnSite);

            _mockMobileDataRepo.Verify(mdr => mdr.UpdateAsync(It.Is<MobileData>(md => md == _mobileData)), Times.Once);

        }

        [Fact]
        public async Task InstructionVM_ProgressButton_OnSite()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();

            _mobileData.ProgressState = Core.Enums.InstructionProgress.OnSite;

            instructionVM.Init(_navID);

            Assert.Equal("On Site", instructionVM.ProgressButtonText);

            await instructionVM.ProgressInstructionAsync();

            // Shouldn't have set to complete yet
            Assert.True(_mobileData.ProgressState == Core.Enums.InstructionProgress.OnSite);

            // Should have told navigation service to move on
            _mockNavigationService.Verify(ns => ns.MoveToNextAsync(It.Is<NavData<MobileData>>(ni => ni.Data == _mobileData)), Times.Once);

        }


        [Fact]
        public void InstructionVM_ChangeTrailerAllowed_DeliveryInstruction()
        {
            base.ClearAll();

            _mobileData.Order.Type = Core.Enums.InstructionType.Deliver;

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.False(instructionVM.ChangeTrailerAllowed);

        }

        [Fact]
        public void InstructionVM_ChangeTrailerAllowed_CollectInstruction_TrailerConfirmationEnabled()
        {
            base.ClearAll();

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;
            _mobileData.ProgressState = Core.Enums.InstructionProgress.Driving;
            _mobileData.Order.Additional.IsTrailerConfirmationEnabled = true;

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.True(instructionVM.ChangeTrailerAllowed);

        }

        [Fact]
        public void InstructionVM_ChangeTrailerAllowed_CollectInstruction_TrailerConfirmationDisabled()
        {
            base.ClearAll();

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;
            _mobileData.ProgressState = Core.Enums.InstructionProgress.OnSite;
            _mobileData.Order.Additional.IsTrailerConfirmationEnabled = false;

            var instructionVM = _fixture.Create<InstructionViewModel>();

            instructionVM.Init(_navID);

            Assert.False(instructionVM.ChangeTrailerAllowed);

        }

        [Fact]
        public async Task InstructionVM_CheckInstructionNotification_Delete()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();
            instructionVM.IsVisible = true;

            instructionVM.Init(_navID);

            await instructionVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Delete));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockNavigationService.Verify(ns => ns.GoToManifestAsync(), Times.Once);

        }


        [Fact]
        public async Task InstructionVM_CheckInstructionNotification_Update_Confirm()
        {
            base.ClearAll();

            var instructionVM = _fixture.Create<InstructionViewModel>();
            instructionVM.IsVisible = true;

            instructionVM.Init(_navID);

            _mobileData.GroupTitle = "UpdateTitle";

            await instructionVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(gui => gui.ToString() == _mobileData.ID.ToString())), Times.Exactly(1));
        }

        #endregion Test

    }

}
