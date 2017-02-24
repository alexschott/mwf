using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore.Core;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Cirrious.MvvmCross.Views;
using Moq;
using MWF.Mobile.Core.Messages;
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
    public class InstructionTrailerViewModelTests
        :  MvxIoCSupportingTest
    {
        private IFixture _fixture;
        private Core.Models.Trailer _trailer;
        private TrailerItemViewModel _trailerItemViewModel;
        private IInfoService _infoService;
        private Mock<IMvxMessenger> _mockMessenger;
        private Mock<INavigationService> _navigationServiceMock;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IRepositories> _mockRepositories;

        protected override void AdditionalSetup()
        {
            var mockDispatcher = new MockDispatcher();
            Ioc.RegisterSingleton<IMvxViewDispatcher>(mockDispatcher);
            Ioc.RegisterSingleton<IMvxMainThreadDispatcher>(mockDispatcher);

            var mockUserInteraction = new Mock<ICustomUserInteraction>();
            Ioc.RegisterSingleton<ICustomUserInteraction>(mockUserInteraction.Object);

            _mockUserInteraction = new Mock<ICustomUserInteraction>();
            _mockUserInteraction.Setup(ui => ui.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            Ioc.RegisterSingleton<ICustomUserInteraction>(_mockUserInteraction.Object);

            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            _trailer = new Core.Models.Trailer() { Registration = "TestRegistration", ID = Guid.NewGuid() };
            _trailerItemViewModel = new TrailerItemViewModel() { Trailer = _trailer };

            _infoService = _fixture.Create<InfoService>();
            _fixture.Inject<IInfoService>(_infoService);

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            _navigationServiceMock = _fixture.InjectNewMock<INavigationService>();
            Ioc.RegisterSingleton<INavigationService>(_navigationServiceMock.Object);

            var repositories = _fixture.Create<IRepositories>();
            _fixture.Inject(repositories);
            _mockRepositories = Mock.Get(repositories);
            Ioc.RegisterSingleton<IRepositories>(_mockRepositories.Object);

            var applicationRepository = Mock.Of<IApplicationProfileRepository>(apr => apr.GetAllAsync() == Task.FromResult(_fixture.CreateMany<Core.Models.ApplicationProfile>()));
            _mockRepositories.Setup(r => r.ApplicationRepository).Returns(applicationRepository);
        }

        /// <summary>
        /// Tests the view model can be initialized correctly
        /// </summary>
        [Fact]
        public async Task InstructionTrailerListVM_Initialization()
        {
            base.ClearAll();

            var trailerRepository = new Mock<ITrailerRepository>();
            var trailers = _fixture.CreateMany<Core.Models.Trailer>();
            trailerRepository.Setup(vr => vr.GetAllAsync()).ReturnsAsync(trailers);

            _mockRepositories.Setup(r => r.TrailerRepository).Returns(trailerRepository.Object);

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, true, false, false, false, false, true, null);
            var navData = new NavData<MobileData>() { Data = mobileData };
            //set the trailer in the current order to have the same registration as first trailer
            mobileData.Order.Additional.Trailer.TrailerId = trailers.First().Registration;

            var navID = Guid.NewGuid();
            _navigationServiceMock.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var vm = _fixture.Build<InstructionTrailerViewModel>().With(itvm => itvm.TrailerSearchText, string.Empty).Create();
            await vm.Init(navID);

            Assert.Equal(vm.DefaultTrailerReg, mobileData.Order.Additional.Trailer.TrailerId);

            // first item in list should be the "default" i.e. it is the one specified in the order
            Assert.True(vm.Trailers.First().IsDefault);
            Assert.True(vm.Trailers.First().TrailerText.EndsWith("(as order)"));
        }

        /// <summary>
        /// Tests that when a trailer is selected, the updated trailer is stored in the nav data
        /// and the next view model is navigated to
        /// </summary>
        [Fact]
        public async Task InstructionTrailerListVM_TrailerSelection()
        {
            base.ClearAll();

            var trailerRepository = new Mock<ITrailerRepository>();
            var trailers = _fixture.CreateMany<Core.Models.Trailer>();
            trailerRepository.Setup(vr => vr.GetAllAsync()).ReturnsAsync(trailers);

            _mockRepositories.Setup(r => r.TrailerRepository).Returns(trailerRepository.Object);

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, true, false, false, false, false, true, null);
            var navData = new NavData<MobileData>() { Data = mobileData };

            var navID = Guid.NewGuid();
            _navigationServiceMock.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var vm = _fixture.Build<InstructionTrailerViewModel>().With(itvm => itvm.TrailerSearchText, string.Empty).Create();
            await vm.Init(navID);

            var trailerItem = vm.Trailers.First();

            var mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();
            mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Confirm your trailer");

            //select the first trailer
            await vm.ConfirmTrailerAsync(trailerItem);

            //Should have set the updated trailer on the nav data
            Assert.Equal(navData.OtherData["UpdatedTrailer"], trailerItem.Trailer);

            _navigationServiceMock.Verify( ns => ns.MoveToNextAsync(It.Is<NavData<MobileData>>(nd => nd == navData)));
        }

        [Fact]
        public async Task InstructionTrailerListVM_CheckInstructionNotification_Delete()
        {
            base.ClearAll();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, true, false, false, false, false, true, null);
            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };

            var navID = Guid.NewGuid();
            _navigationServiceMock.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var vm = _fixture.Create<InstructionTrailerViewModel>();
            vm.IsVisible = true;

            await vm.Init(navID);

            await vm.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Delete));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _navigationServiceMock.Verify(ns => ns.GoToManifestAsync(), Times.Once);
        }

        [Fact]
        public async Task InstructionTrailerListVM_CheckInstructionNotification_Update_Confirm()
        {
            base.ClearAll();

            var mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, true, false, false, false, false, true, null);

            var mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == mobileData.ID))).ReturnsAsync(mobileData);

            _mockRepositories.Setup(r => r.MobileDataRepository).Returns(mockMobileDataRepo.Object);

            // set the trailer the user has selected to be the same as current trailer and the one specified on the order
            var navData = new NavData<MobileData>() { Data = mobileData };
            var navID = Guid.NewGuid();
            _navigationServiceMock.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            var vm = _fixture.Create<InstructionTrailerViewModel>();
            vm.IsVisible = true;

            await vm.Init(navID);

            await vm.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(gui => gui.ToString() == mobileData.ID.ToString())), Times.Exactly(1));

        }

    }

}
