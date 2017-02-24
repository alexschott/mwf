using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Tests.Helpers;
using Xunit;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.Portable;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class InstructionOnSiteViewModelTests
        : MvxIoCSupportingTest
    {

        #region Setup

        private IFixture _fixture;
        private MobileData _mobileData;
        private Mock<INavigationService> _navigationService; 
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<IInfoService> _mockInfoService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMvxMessenger> _mockMessenger;
        private Mock<IRepositories> _mockRepositories;

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

            var mockConfigRepo = _fixture.InjectNewMock<IConfigRepository>();
            mockConfigRepo.Setup(cr => cr.GetAsync()).ReturnsUsingFixture(_fixture);

            _fixture.Inject(Mock.Of<IRepositories>(r => r.ConfigRepository == mockConfigRepo.Object && r.MobileDataRepository == _mockMobileDataRepo.Object));

            _navigationService = _fixture.InjectNewMock<INavigationService>();

            _mockInfoService = _fixture.InjectNewMock<IInfoService>();

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            Ioc.RegisterSingleton<INavigationService>(_navigationService.Object);
        }

        #endregion Setup

        #region Test

        [Fact]
        public async Task InstructionOnSiteVM_FragmentTitle_Collect()
        {
            base.ClearAll();

            _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), null);

            var navData = new NavData<MobileData>() { Data = _mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            await instructionOnSiteVM.Init(navID);

            Assert.Equal("Collect On Site", instructionOnSiteVM.FragmentTitle);
        }

        [Fact]
        public async Task InstructionOnSiteVM_FragmentTitle_Deliver()
        {
            base.ClearAll();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), null);
            var navData = new NavData<MobileData>() { Data = mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            await instructionOnSiteVM.Init(navID);

            Assert.Equal("Deliver On Site", instructionOnSiteVM.FragmentTitle);
        }

        [Fact]
        public async Task InstructionOnSiteVM_NavButton_Collect_Complete()
        {
            base.ClearAll();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, true, false, false, false, false, false, true, null);
            var navData = new NavData<MobileData>() { Data = mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            await instructionOnSiteVM.Init(navID);

            Assert.Equal("Complete", instructionOnSiteVM.InstructionCommentButtonLabel);
        }

        [Fact]
        public async Task InstructionOnSiteVM_NavButton_Deliver_Complete()
        {
            base.ClearAll();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, true, false, false, false, false, false, true, null);
            var navData = new NavData<MobileData>() { Data = mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            await instructionOnSiteVM.Init(navID);

            Assert.Equal("Complete", instructionOnSiteVM.InstructionCommentButtonLabel);
        }

        [Fact]
        public async Task InstructionOnSiteVM_NavButton_Collect_Continue()
        {
            base.ClearAll();

            _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, false, true, false, false, false, true, null);

            var navData = new NavData<MobileData>() { Data = _mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            await instructionOnSiteVM.Init(navID);

            Assert.Equal("Continue", instructionOnSiteVM.InstructionCommentButtonLabel);
        }

        [Fact]
        public async Task InstructionOnSiteVM_NavButton_Deliver_Continue()
        {
            base.ClearAll();

            _fixture.SetUpInstruction(Core.Enums.InstructionType.Deliver, false, false, true, false, false, false, true, null);

            var navData = new NavData<MobileData>() { Data = _mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            await instructionOnSiteVM.Init(navID);

            Assert.Equal("Continue", instructionOnSiteVM.InstructionCommentButtonLabel);
        }

        [Fact]
        public async Task InstructionOnSiteVM_CheckInstructionNotification_Delete()
        {
            base.ClearAll();

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            instructionOnSiteVM.IsVisible = true;

            var navData = new NavData<MobileData>() { Data = _mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            await instructionOnSiteVM.Init(navID);
            await instructionOnSiteVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Delete));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _navigationService.Verify(ns => ns.GoToManifestAsync(), Times.Once);
        }

        [Fact]
        public async Task InstructionOnSiteVM_CheckInstructionNotification_Update_Confirm()
        {
            base.ClearAll();

            var instructionOnSiteVM = _fixture.Create<InstructionOnSiteViewModel>();
            instructionOnSiteVM.IsVisible = true;

            var navData = new NavData<MobileData>() { Data = _mobileData };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            await instructionOnSiteVM.Init(navID);
            await instructionOnSiteVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(gui => gui.ToString() == _mobileData.ID.ToString())), Times.Exactly(1));
        }

        #endregion Test

    }

}
