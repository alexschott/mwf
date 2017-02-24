using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
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
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class ReviseQuantityViewModelTests
        : MvxIoCSupportingTest
    {

        #region Setup

        private IFixture _fixture;
        private MobileData _mobileData;
        private Mock<IRepositories> _mockRepositories;
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<INavigationService> _navigationService;
        private Mock<IInfoService> _mockInfoService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMvxMessenger> _mockMessenger;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.OmitProperty("EffectiveDateString");

            _mobileData = _fixture.Create<MobileData>();

            _mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == _mobileData.ID))).ReturnsAsync(_mobileData);

            _mockRepositories = _fixture.InjectNewMock<IRepositories>();
            _mockRepositories.Setup(r => r.MobileDataRepository).Returns(_mockMobileDataRepo.Object);
            Ioc.RegisterSingleton<IRepositories>(_mockRepositories.Object);

            _navigationService = _fixture.InjectNewMock<INavigationService>();

            _mockInfoService = _fixture.InjectNewMock<IInfoService>();
            _mockInfoService.Setup(s => s.CurrentDriverID).ReturnsUsingFixture(_fixture);
            _mockInfoService.Setup(s => s.CurrentDriverID).ReturnsUsingFixture(_fixture);

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            Ioc.RegisterSingleton<INavigationService>(_navigationService.Object);

        }

        #endregion Setup

        #region Tests

        [Fact]
        public async Task ReviseQuantityVM_SuccessfulUpdate()
        {
            base.ClearAll();

            var reviseQuantityVM = _fixture.Create<ReviseQuantityViewModel>();

            int newQuantity = 123;

            var navData = new NavData<MobileData>() { Data = _mobileData };
            navData.OtherData["Order"] = _mobileData.Order.Items.FirstOrDefault();
            navData.OtherData["DataChunk"] = _fixture.Create<MobileApplicationDataChunkContentActivity>();

            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            reviseQuantityVM.Init(navID);

            reviseQuantityVM.OrderQuantity = newQuantity.ToString();

            await reviseQuantityVM.ReviseQuantityAsync();

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().Quantity, reviseQuantityVM.OrderQuantity);

        }

        [Fact]
        public async Task ReviseQuantityVM_CheckInstructionNotification_Delete()
        {
            base.ClearAll();

            var reviseQuantityVM = _fixture.Create<ReviseQuantityViewModel>();
            reviseQuantityVM.IsVisible = true;

            var navData = new NavData<MobileData>() { Data = _mobileData };
            navData.OtherData["Order"] = _mobileData.Order.Items.FirstOrDefault();
            navData.OtherData["DataChunk"] = _fixture.Create<MobileApplicationDataChunkContentActivity>();

            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            reviseQuantityVM.Init(navID);

            await reviseQuantityVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Delete));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _navigationService.Verify(ns => ns.GoToManifestAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviseQuantityVM_CheckInstructionNotification_Update_Confirm()
        {
            base.ClearAll();

            var reviseQuantityVM = _fixture.Create<ReviseQuantityViewModel>();
            reviseQuantityVM.IsVisible = true;

            var navData = new NavData<MobileData>() { Data = _mobileData };
            navData.OtherData["Order"] = _mobileData.Order.Items.FirstOrDefault();
            navData.OtherData["DataChunk"] = _fixture.Create<MobileApplicationDataChunkContentActivity>();

            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            reviseQuantityVM.Init(navID);

            await reviseQuantityVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(gui => gui.ToString() == _mobileData.ID.ToString())), Times.Exactly(1));
        }

        #endregion Tests

    }

}
