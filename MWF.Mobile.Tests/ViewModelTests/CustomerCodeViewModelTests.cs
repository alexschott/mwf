using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore.Core;
using SQLite.Net.Attributes;
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


namespace MWF.Mobile.Tests.ViewModelTests
{
    public class CustomerCodeViewModelTests : MvxIoCSupportingTest
    {
        private IFixture _fixture;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IDataService> _dataService;

        protected override void AdditionalSetup()
        {
            var mockDispatcher = new MockDispatcher();
            Ioc.RegisterSingleton<IMvxViewDispatcher>(mockDispatcher);
            Ioc.RegisterSingleton<IMvxMainThreadDispatcher>(mockDispatcher);

            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            _mockUserInteraction = new Mock<ICustomUserInteraction>();
            _fixture.Register<ICustomUserInteraction>(() => _mockUserInteraction.Object);

            _dataService = new Mock<IDataService>();
            var asyncConnection = _fixture.Create<Core.Database.IAsyncConnection>();
            var connectionMock = new Mock<Core.Database.IConnection>();

            _dataService.Setup(c => c.RunInTransactionAsync(It.IsAny<Action<Core.Database.IConnection>>()))
                .Callback<Action<Core.Database.IConnection>>(a => a.Invoke(connectionMock.Object))
                .Returns(Task.FromResult(0));

            _fixture.Register<IDataService>(() => _dataService.Object);
        }

        [Fact]
        public async Task CustCodeVM_NoInternetDoesNotShowSpinner()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == false));

            var ccvm =_fixture.Create<CustomerCodeViewModel>();
            ccvm.CustomerCode = "123";
            ccvm.IsBusy = false;

            await ccvm.EnterCodeAsync();

            Assert.Equal(false, ccvm.IsBusy);
        }

        [Fact]
        //Tests that when the customer code is invalid, an alert with an error message is shown
        public async Task CustCodeVM_InvalidCustomerCode()
        {
            base.ClearAll();

            // Get device returns null (i.e. customer code is invalid)
            _fixture.Register<IGatewayService>(() => Mock.Of<IGatewayService>(gs => gs.GetDeviceAsync(It.IsAny<string>()) == Task.FromResult<Device>((Device) null) &&
                                                                                    gs.CreateDeviceAsync() == Task.FromResult<bool>(true)));

            var ccvm = _fixture.Create<CustomerCodeViewModel>();

            // Enter the code
            await ccvm.EnterCodeAsync();

            // check error message has returned
            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s == "The customer passcode you submitted doesn't exist, check the passcode and try again."), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Fact]
        //Tests that when an exception is thrown by the gateway service (or sql service) an error message is displayed
        public async Task CustCodeVM_SetUpException()
        {
            base.ClearAll();

            // Get device throws an exception
            var gatewayService = new Mock<IGatewayService>();
            gatewayService.Setup(gs => gs.CreateDeviceAsync()).ReturnsAsync(true);
            gatewayService.Setup(gs => gs.GetDeviceAsync(It.IsAny<string>())).Callback(() => { throw new Exception(); });
            _fixture.Register<IGatewayService>(() => gatewayService.Object);

            var ccvm = _fixture.Create<CustomerCodeViewModel>();

            // Enter the code
            await ccvm.EnterCodeAsync();

            // check error message has returned
            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s == "Unfortunately, there was a problem setting up your device, try restarting the device and try again."), It.IsAny<string>(), It.IsAny<string>()), Times.Once());

        }

        [Fact]
        // Tests that what when a valid customer code is entered
        // data is pulled from the web services, customer code is persisted in the db
        public async Task CustCodeVM_ValidCustomerCode()
        {
            base.ClearAll();

            Device dev = new Device();

            var gateWayService = Mock.Of<IGatewayService>(gs =>
                gs.GetDeviceAsync("123") == Task.FromResult<Device>(dev) &&
                gs.CreateDeviceAsync() == Task.FromResult<bool>(true));

            _fixture.Register<IGatewayService>(() => gateWayService);

            var customerRepository = new Mock<ICustomerRepository>();
            _fixture.Inject<ICustomerRepository>(customerRepository.Object);
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var navigationServiceMock = new Mock<INavigationService>();
            navigationServiceMock.Setup(ns => ns.MoveToNextAsync()).Returns(Task.FromResult(0));
            _fixture.Inject<INavigationService>(navigationServiceMock.Object);

            var ccvm = _fixture.Create<CustomerCodeViewModel>();
            ccvm.CustomerCode = "123";

            // Enter the code
            await ccvm.EnterCodeAsync();

            //check that the customer repository was written to
            customerRepository.Verify(cr => cr.Insert(It.IsAny<Customer>(), It.IsAny<Core.Database.IConnection>()), Times.Once);

            // check that the navigation service was called
            navigationServiceMock.Verify(ns => ns.MoveToNextAsync(), Times.Once);
        }

    }

}
