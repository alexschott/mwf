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
    public class InstructionSignatureViewModelTests
        : MvxIoCSupportingTest
    {

        #region Private Members

        private IFixture _fixture;
        private MobileData _mobileData;
        private Mock<INavigationService> _navigationService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<IInfoService> _mockInfoService;
        private Mock<IDataChunkService> _mockDataChunkService;
        private Mock<IMvxMessenger> _mockMessenger;
        private NavData<MobileData> _navData;
        private Guid _navID;
        private Mock<IRepositories> _mockRepositories;

        #endregion Private Members

        #region Setup

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

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();
            _mockUserInteraction.ConfirmReturnsTrueIfTitleStartsWith("Complete Instruction");

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _mockInfoService = _fixture.InjectNewMock<IInfoService>();

            _mockDataChunkService = _fixture.InjectNewMock<IDataChunkService>();

            _mockDataChunkService.Setup(m => m.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(),  It.IsAny<MobileData>(), It.IsAny<Guid>(), It.IsAny<string>(),false, false));

            _navigationService = _fixture.InjectNewMock<INavigationService>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            Ioc.RegisterSingleton<INavigationService>(_navigationService.Object);

            _navData = new NavData<MobileData>() { Data = _mobileData };
            _navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(_navID)).Returns(_navData);
        }

        #endregion Setup

        #region Test

        [Fact]
        public async Task InstructionSignatureVM_Complete_Signature()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            var navData = new NavData<MobileData>() { Data = _mobileData };
            var dataChunk = _fixture.Create<MobileApplicationDataChunkContentActivity>();
            navData.OtherData["DataChunk"] = dataChunk;

            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            instructionSignatureVM.Init(navID);

            await instructionSignatureVM.InstructionDoneAsync();

            _navigationService.Verify(ns => ns.MoveToNextAsync(It.IsAny <NavData<MobileData>>()), Times.Once);

            Assert.Same(instructionSignatureVM.CustomerSignatureEncodedImage, dataChunk.Signature.EncodedImage);
            Assert.Same(instructionSignatureVM.CustomerName, dataChunk.Signature.Title);
        }

        [Fact]
        public async Task InstructionSignatureVM_NullSignatureCheck_Collect()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;
            _mobileData.Order.Additional.CustomerSignatureRequiredForCollection = true;

            instructionSignatureVM.CustomerSignatureEncodedImage = "";

            instructionSignatureVM.Init(_navID);

            await instructionSignatureVM.InstructionDoneAsync();

            _navigationService.Verify(ns => ns.MoveToNextAsync(It.IsAny<NavData<MobileData>>()), Times.Never);
        }

        [Fact]
        public async Task InstructionSignatureVM_NullSignatureCheck_Deliver()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            _mobileData.Order.Type = Core.Enums.InstructionType.Deliver;
            _mobileData.Order.Additional.CustomerSignatureRequiredForDelivery = true;

            instructionSignatureVM.CustomerSignatureEncodedImage = "";

            instructionSignatureVM.Init(_navID);

            await instructionSignatureVM.InstructionDoneAsync();

            _navigationService.Verify(ns => ns.MoveToNextAsync(It.IsAny<NavData<MobileData>>()), Times.Never);
        }

        [Fact]
        public async Task InstructionSignatureVM_NullCustomerNameCheck_Collect()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;
            _mobileData.Order.Additional.CustomerNameRequiredForCollection = true;

            instructionSignatureVM.CustomerName = "";

            instructionSignatureVM.Init(_navID);

            await instructionSignatureVM.InstructionDoneAsync();

            _navigationService.Verify(ns => ns.MoveToNextAsync(It.IsAny<NavData<MobileData>>()), Times.Never);
        }

        [Fact]
        public async Task InstructionSignatureVM_NullCustomerNameCheck_Deliver()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            _mobileData.Order.Type = Core.Enums.InstructionType.Deliver;
            _mobileData.Order.Additional.CustomerNameRequiredForDelivery = true;

            instructionSignatureVM.CustomerName = "";

            instructionSignatureVM.Init(_navID);

            await instructionSignatureVM.InstructionDoneAsync();

            _navigationService.Verify(ns => ns.MoveToNextAsync(It.IsAny<NavData<MobileData>>()), Times.Never);
        }

        [Fact]
        public void InstructionSignatureVM__Collect_FragmentTitle()
        {
            base.ClearAll();

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);

            Assert.Equal("Sign for Collection", instructionSignatureVM.FragmentTitle);

        }

        [Fact]
        public void InstructionSignatureVM_Deliver_FragmentTitle()
        {
            base.ClearAll();

            _mobileData.Order.Type = Core.Enums.InstructionType.Deliver;

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);

            Assert.Equal("Sign for Delivery", instructionSignatureVM.FragmentTitle);

        }

        [Fact]
        public void InstructionSignatureVM_SignatureUnavailableToggleButtonText()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.IsSignaturePadEnabled = false;

            instructionSignatureVM.Init(_navID);

            Assert.Equal("Signature available", instructionSignatureVM.SignatureToggleButtonLabel);

        }

        [Fact]
        public void InstructionSignatureVM_SignatureAvailableToggleButtonText()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.IsSignaturePadEnabled = true;

            instructionSignatureVM.Init(_navID);

            Assert.Equal("Signature unavailable", instructionSignatureVM.SignatureToggleButtonLabel);

        }

        [Fact]
        public void InstructionSignatureVM_Collect_SignatureToggleButtonDisabled()
        {
            base.ClearAll();

            _mobileData.Order.Additional.CustomerSignatureRequiredForCollection = true;
            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);
         
            Assert.Equal(false, instructionSignatureVM.IsSignatureToggleButtonEnabled);
            Assert.Equal(true, instructionSignatureVM.IsSignaturePadEnabled);

            Assert.Equal("Signature unavailable", instructionSignatureVM.SignatureToggleButtonLabel);

        }

        [Fact]
        public void InstructionSignatureVM_Delivery_SignatureToggleButtonDisabled()
        {
            base.ClearAll();

            _mobileData.Order.Additional.CustomerSignatureRequiredForDelivery = true;
            _mobileData.Order.Type = Core.Enums.InstructionType.Deliver;

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);

            Assert.Equal(false, instructionSignatureVM.IsSignatureToggleButtonEnabled);
            Assert.Equal(true, instructionSignatureVM.IsSignaturePadEnabled);

            Assert.Equal("Signature unavailable", instructionSignatureVM.SignatureToggleButtonLabel);
            

        }

        [Fact]
        public void InstructionSignatureVM_Collect_SignatureToggleButtonEnabled()
        {
            base.ClearAll();

            _mobileData.Order.Type = Core.Enums.InstructionType.Collect;
            _mobileData.Order.Additional.CustomerSignatureRequiredForCollection = false;

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);


            Assert.Equal(true, instructionSignatureVM.IsSignatureToggleButtonEnabled);
            Assert.Equal(false, instructionSignatureVM.IsSignaturePadEnabled);

            Assert.Equal("Signature available", instructionSignatureVM.SignatureToggleButtonLabel);
        }

        [Fact]
        public void InstructionSignatureVM_Delivery_SignatureToggleButtonEnabled()
        {
            base.ClearAll();

            _mobileData.Order.Type = Core.Enums.InstructionType.Deliver;
            _mobileData.Order.Additional.CustomerSignatureRequiredForDelivery = false;

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);

            Assert.Equal(false, instructionSignatureVM.IsSignaturePadEnabled);
            Assert.Equal(true, instructionSignatureVM.IsSignatureToggleButtonEnabled);

            Assert.Equal("Signature available", instructionSignatureVM.SignatureToggleButtonLabel);

        }

        [Fact]
        public async Task InstructionSignatureVM_CheckInstructionNotification_Delete()
        {

            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);

            await instructionSignatureVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Delete));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _navigationService.Verify(ns => ns.GoToManifestAsync(), Times.Once);
        }

        [Fact]
        public async Task InstructionSignatureVM_CheckInstructionNotification_Update_Confirm()
        {
            base.ClearAll();

            var instructionSignatureVM = _fixture.Create<InstructionSignatureViewModel>();

            instructionSignatureVM.Init(_navID);

            await instructionSignatureVM.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(gui => gui.ToString() == _mobileData.ID.ToString())), Times.Exactly(1));

        }


        #endregion Test
    }
}
