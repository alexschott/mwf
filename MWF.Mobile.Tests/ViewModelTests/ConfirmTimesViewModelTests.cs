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
    public class ConfirmTimesViewModelTests
        : MvxIoCSupportingTest
    {

        #region Setup

        private IFixture _fixture;
        private MobileData _mobileData;
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<INavigationService> _navigationService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMvxMessenger> _mockMessenger;
        private NavData<MobileData> _navData;
        private Guid _navID;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.OmitProperty("EffectiveDateString");

            _mobileData = _fixture.Create<MobileData>();

            _mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == _mobileData.ID))).ReturnsAsync(_mobileData);

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _navigationService = _fixture.InjectNewMock<INavigationService>();

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            Ioc.RegisterSingleton<INavigationService>(_navigationService.Object);

            _navData = new NavData<MobileData>() { Data = _mobileData };
            _navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(_navID)).Returns(_navData);
        }

        #endregion Setup

        #region Tests

        [Fact]
        public async Task ConfirmTimesVM_OverrideOnSiteDateTimeAddToMobileApplicationDataChunkService()
        {
            base.ClearAll();

            var confirmTimesVM = _fixture.Create<ConfirmTimesViewModel>();
            confirmTimesVM.Init(_navID);

            var onSiteDateTime = DateTime.Now.AddDays(-1);
            confirmTimesVM.OnSiteDateTime = onSiteDateTime;

            await confirmTimesVM.AdvanceConfirmTimesAsync();

            Assert.Equal(onSiteDateTime, _navData.Data.OnSiteDateTime);   
        }

        [Fact]
        public async Task ConfirmTimesVM_OverrideCompleteDateTimeAddToMobileApplicationDataChunkService()
        {
            base.ClearAll();

            var confirmTimesVM = _fixture.Create<ConfirmTimesViewModel>();
            confirmTimesVM.Init(_navID);

            var completeDateTime = DateTime.Now.AddDays(-1);
            confirmTimesVM.CompleteDateTime = completeDateTime;

            await confirmTimesVM.AdvanceConfirmTimesAsync();

            Assert.Equal(completeDateTime, _navData.Data.CompleteDateTime);
        }

        #endregion Tests

    }

}
