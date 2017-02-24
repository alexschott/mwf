using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Moq;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class OrderViewModelTests
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
        private Mock<IConfigRepository> _mockConfigRepo;
        private NavData<MobileData> _navData;
        private MWFMobileConfig _mwfMobileConfig;
        private Guid _navID;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _mobileData = _fixture.Create<MobileData>();

            _mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == _mobileData.ID))).ReturnsAsync(_mobileData);

            _mockRepositories = _fixture.InjectNewMock<IRepositories>();
            _mockRepositories.Setup(r => r.MobileDataRepository).Returns(_mockMobileDataRepo.Object);
            Ioc.RegisterSingleton<IRepositories>(_mockRepositories.Object);

            _mwfMobileConfig = _fixture.Create<MWFMobileConfig>();
            _mockConfigRepo = _fixture.InjectNewMock<IConfigRepository>();
            _mockConfigRepo.Setup(mcr => mcr.GetByIDAsync(It.IsAny<Guid>())).Returns(() => Task.FromResult(_mwfMobileConfig));

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _navigationService = _fixture.InjectNewMock<INavigationService>();

            _mockInfoService = _fixture.InjectNewMock<IInfoService>();

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            Ioc.RegisterSingleton<INavigationService>(_navigationService.Object);

            _navData = new NavData<MobileData>() { Data = _mobileData };
            _navData.OtherData["Order"] = _mobileData.Order.Items.FirstOrDefault();
            _navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(_navID)).Returns(_navData);
        }

        #endregion Setup

        #region Tests

        [Fact]
        public async Task OrderVM_OrderID()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            await orderVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().ItemIdFormatted, orderVM.OrderID);
        }

        [Fact]
        public async Task OrderVM_Title()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            await orderVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().Title, orderVM.OrderLoadNo);
        }

        [Fact]
        public async Task OrderVM_DeliveryOrderNumber()
        {
            base.ClearAll();
            var orderVM = _fixture.Create<OrderViewModel>();

            await orderVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().DeliveryOrderNumber, orderVM.OrderDeliveryNo);
        }

        [Fact]
        public async Task OrderVM_Quantity()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            await orderVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().Quantity, orderVM.OrderQuantity);
        }

        [Fact]
        public async Task OrderVM_Weight()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            await orderVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().Weight, orderVM.OrderWeight);
        }

        [Fact]
        public async Task OrderVM_BusinessType()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            await orderVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().BusinessType, orderVM.OrderBusinessType);
        }

        [Fact]
        public async Task OrderVM_GoodsType()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            await orderVM.Init(_navID);

            Assert.Equal(_mobileData.Order.Items.FirstOrDefault().GoodsType, orderVM.OrderGoodsType);
        }

        [Fact]
        public async Task OrderVM_Collection_QuantityEditable()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            _mwfMobileConfig.QuantityIsEditable = true;

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;

            await orderVM.Init(_navID);

            Assert.Equal(true, orderVM.ChangeOrderQuantity);
        }

        [Fact]
        public async Task OrderVM_Collection_QuantityNotEditable()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            _mwfMobileConfig.QuantityIsEditable = false;

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;

            await orderVM.Init(_navID);

            Assert.Equal(false, orderVM.ChangeOrderQuantity);
        }


        [Fact]
        public async Task OrderVM_Delivery_QuantityNotEditable()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();

            _mobileData.Order.Type = Core.Enums.InstructionType.Deliver;

            await orderVM.Init(_navID);

            Assert.Equal(false, orderVM.ChangeOrderQuantity);
        }

        [Fact]
        public async Task OrderVM_CheckInstructionNotification_Delete()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();
            orderVM.IsVisible = true;

            await orderVM.Init(_navID);
            await orderVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Delete));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _navigationService.Verify(ns => ns.GoToManifestAsync(), Times.Once);

        }


        [Fact]
        public async Task OrderVM_CheckInstructionNotification_Update_Confirm()
        {
            base.ClearAll();

            var orderVM = _fixture.Create<OrderViewModel>();
            orderVM.IsVisible = true;

            await orderVM.Init(_navID);

            await orderVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(gui => gui.ToString() == _mobileData.ID.ToString())), Times.Exactly(1));
        }

        #endregion Tests
    }
}
