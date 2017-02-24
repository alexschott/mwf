using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore.Core;
using Cirrious.MvvmCross.Test.Core;
using Cirrious.MvvmCross.Views;
using Moq;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;
using MWF.Mobile.Tests.Helpers;

namespace MWF.Mobile.Tests.ViewModelTests
{
    
    public class PasscodeViewModelTests
        : MvxIoCSupportingTest
    {

        private IFixture _fixture;
        private Driver _driver;
        private IInfoService _infoService;

        private Mock<IApplicationProfileRepository> _mockApplicationProfile;

        protected override void AdditionalSetup()
        {
            var mockDispatcher = new MockDispatcher();
            Ioc.RegisterSingleton<IMvxViewDispatcher>(mockDispatcher);
            Ioc.RegisterSingleton<IMvxMainThreadDispatcher>(mockDispatcher);

            var mockUserInteraction = new Mock<ICustomUserInteraction>();
            Ioc.RegisterSingleton<ICustomUserInteraction>(mockUserInteraction.Object);

            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _driver = new Core.Models.Driver() { LastName = "TestName", ID = new Guid()};

            _infoService = _fixture.Create<InfoService>();
            _fixture.Inject<IInfoService>(_infoService);

            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService.Setup(m => m.AuthenticateAsync(It.IsAny<string>())).ReturnsAsync(new AuthenticationResult { Success = false });
            mockAuthenticationService.Setup(m => m.AuthenticateAsync(It.Is<string>(s => s == "9999"))).ReturnsAsync(new AuthenticationResult { Success = true, Driver = _driver });
            _fixture.Inject<IAuthenticationService>(mockAuthenticationService.Object);

            _mockApplicationProfile = _fixture.InjectNewMock<IApplicationProfileRepository>();
            _mockApplicationProfile.Setup(map => map.GetAsync()).ReturnsAsync(_fixture.Create<ApplicationProfile>());
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
        }

        /// <summary>
        /// Tests that on successful authentication the VehicleListViewModel is navigated to
        /// </summary>
        [Fact]
        public async Task PasscodeVM_SuccessfulAuthenticationRedirectsToVehicleListView()
        {
            base.ClearAll();

            var navigationServiceMock = new Mock<INavigationService>();
            navigationServiceMock.Setup(ns => ns.MoveToNextAsync()).Returns(Task.FromResult(0));
            _fixture.Inject<INavigationService>(navigationServiceMock.Object);

            var vm = _fixture.Create<PasscodeViewModel>();
            vm.Passcode = "9999";

            await vm.LoginAsync();

            // check that the navigation service was called
            navigationServiceMock.Verify(ns => ns.MoveToNextAsync(), Times.Once);
        }

        /// <summary>
        /// Tests that when the SendDiagnostics command is used the navigation service is called
        /// </summary>
        [Fact]
        public async Task PasscodeVM_SendDiagnosticsCommand()
        {
            base.ClearAll();

            var navigationServiceMock = new Mock<INavigationService>();
            navigationServiceMock.Setup(ns => ns.MoveToNextAsync(It.IsAny<NavData>())).Returns(Task.FromResult(Guid.NewGuid()));
            _fixture.Inject<INavigationService>(navigationServiceMock.Object);

            var vm = _fixture.Create<PasscodeViewModel>();

            await vm.SendDiagnosticsAsync();

            // check that the navigation service was called
            navigationServiceMock.Verify(ns => ns.MoveToNextAsync(It.Is<NavData>(nd=> nd.OtherData["Diagnostics"] != null)), Times.Once);
        }

        /// <summary>
        /// Tests that on successful authentication a driver 
        /// </summary>
        [Fact]
        public async Task PasscodeVM_SuccessfulAuthenticationStoresDriver()
        {
            base.ClearAll();

            var testDriver = new Core.Models.Driver
            {
                ID = Guid.NewGuid(),
                Passcode = "9999",
                LastName = "TestName",
            };

            var mockDriverRepo = Mock.Of<IDriverRepository>(m =>
                m.GetByIDAsync(testDriver.ID) == Task.FromResult(testDriver) &&
                m.GetAllAsync() == Task.FromResult(new[] { testDriver }.AsEnumerable()));

            _fixture.Inject(mockDriverRepo);

            var vm = _fixture.Create<PasscodeViewModel>();
            vm.Passcode = testDriver.Passcode;

            await vm.LoginAsync();

            Assert.NotNull(_infoService.CurrentDriverID);
            Assert.Equal(testDriver.DisplayName, _infoService.CurrentDriverDisplayName);
        }

        /// <summary>
        /// Tests that on successful authentication a current driver 
        /// </summary>
        [Fact]
        public async Task PasscodeVM_SuccessfulAuthenticationStoresCurrentDriver()
        {
            base.ClearAll();

            var currentDriverRepository = new Mock<ICurrentDriverRepository>();
            _fixture.Inject<ICurrentDriverRepository>(currentDriverRepository.Object);
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var vm = _fixture.Create<PasscodeViewModel>();
      
            vm.Passcode = "9999";

            await vm.LoginAsync();

            currentDriverRepository.Verify(cdr => cdr.InsertAsync(It.Is<CurrentDriver>( cd => cd.ID == _driver.ID )), Times.Once);

        }

        [Fact]
        public async Task PasscodeVM_BlankPasscodeDoesntRedirectToVehicleView()
        {
            base.ClearAll();

            var vm = _fixture.Create<PasscodeViewModel>();
            vm.Passcode = "";

            await vm.LoginAsync();

            var mockDispatcher = Ioc.Resolve<IMvxMainThreadDispatcher>() as MockDispatcher;
            Assert.Equal(0, mockDispatcher.Requests.Count);
        }

        [Fact]
        public async Task PasscodeVM_IncorrectPassword()
        {
            base.ClearAll();

            // Set incorrect password
            var vm = _fixture.Create<PasscodeViewModel>();
            vm.Passcode = "1212";

            await vm.LoginAsync();

            // Check we didn't redirect anywhere
            var mockDispatcher = Ioc.Resolve<IMvxMainThreadDispatcher>() as MockDispatcher;
            Assert.Equal(0, mockDispatcher.Requests.Count);

            //Check that the passcode got blanked out
            Assert.Equal(string.Empty, vm.Passcode);
        }

    }

}
