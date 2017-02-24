using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class VehicleListViewModelTests 
        : MvxIoCSupportingTest
    {

        private IFixture _fixture;
        private Driver _driver;
        private Vehicle _vehicle;
        private IInfoService _infoService;
        private Mock<ICurrentDriverRepository> _currentDriverRepository;
        private Mock<ICustomUserInteraction> _mockUserInteraction;

        protected override void AdditionalSetup()
        {
            _mockUserInteraction = new Mock<ICustomUserInteraction>();
            _mockUserInteraction.ConfirmReturnsFalseIfTitleStartsWith("Last Used Vehicle");
            Ioc.RegisterSingleton<ICustomUserInteraction>(_mockUserInteraction.Object);

            _mockUserInteraction = new Mock<ICustomUserInteraction>();
            
            Ioc.RegisterSingleton<ICustomUserInteraction>(_mockUserInteraction.Object);


            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            _driver = new Core.Models.Driver() { LastName = "TestName", ID = new Guid()  };

            _vehicle = new Core.Models.Vehicle() { Registration = "TestRegistration", ID = new Guid() };

            _infoService = _fixture.Create<InfoService>();
            _infoService.CurrentDriverID = _driver.ID;
            _fixture.Inject<IInfoService>(_infoService);

            _currentDriverRepository = new Mock<ICurrentDriverRepository>();
            _currentDriverRepository.Setup(cdr => cdr.GetByIDAsync(It.IsAny<Guid>())).ReturnsAsync(new CurrentDriver());
            _fixture.Inject<ICurrentDriverRepository>(_currentDriverRepository.Object);

        }

        /// <summary>
        /// Tests that when a vehicle is selected and the confirm message is shown then the navigation service is called 
        /// </summary>
        [Fact]
        public async Task VehicleListVM_SelectVehicle_Navigation()
        {
            base.ClearAll();

            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Confirm your vehicle");

            var navigationServiceMock = new Mock<INavigationService>();
            navigationServiceMock.Setup(ns => ns.MoveToNextAsync()).Returns(Task.FromResult(0));
            _fixture.Inject<INavigationService>(navigationServiceMock.Object);

            var vm = _fixture.Create<VehicleListViewModel>();
            await vm.Init();

            await vm.VehicleDetailAsync(_vehicle);

            // check that the navigation service was called
            navigationServiceMock.Verify(ns => ns.MoveToNextAsync(), Times.Once);

        }

        /// <summary>
        /// Tests that vehicle is selected and the confirm message the vehicle is stored
        /// </summary>
        [Fact]
        public async Task VehicleListVM_SelectVehicle_VehicleStored()
        {
            base.ClearAll();

            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Confirm your vehicle");

            var vm = _fixture.Create<VehicleListViewModel>();

            await vm.VehicleDetailAsync(_vehicle);

            Assert.NotNull(_infoService.CurrentDriverID);
            Assert.Equal(_vehicle.ID, _infoService.CurrentVehicleID);

        }

        /// <summary>
        /// Tests that on successful authentication a current drivers vehicle ID 
        /// </summary>
        [Fact]
        public async Task VehicleListVM_SuccessfulAuthenticationStoresCurrentDriverVehicleID()
        {
            base.ClearAll();

            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Confirm your vehicle");

            var vm = _fixture.Create<VehicleListViewModel>();

            await vm.VehicleDetailAsync(_vehicle);

            _currentDriverRepository.Verify(cdr => cdr.InsertAsync(It.Is<CurrentDriver>(cd => cd.ID == _vehicle.ID)), Times.Once);   
        }

        /// <summary>
        /// Tests that the toast message appears when there is no internet connection
        /// on the refresh of the vehicle list.
        /// </summary>
        [Fact]
        public async Task VehicleListVM_NoInternetShowToast()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == false));

            var toast = new Mock<IToast>();
            _fixture.Inject<IToast>(toast.Object);

            var vm = _fixture.Create<VehicleListViewModel>();

            await vm.UpdateVehicleListAsync();

            toast.Verify(t => t.Show("No internet connection!"));
             
        }

        /// <summary>
        /// Tests that the list is filtered correctly when you search for a vehicle.
        /// </summary>
        [Fact]
        public async Task VehicleListVM_SuccessfulVehicleListFilter()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            var vehicleRepositry = new Mock<IVehicleRepository>();
            var vehicles = _fixture.CreateMany<Vehicle>();
            vehicleRepositry.Setup(vr => vr.GetAllAsync()).ReturnsAsync(vehicles);

            _fixture.Inject<IVehicleRepository>(vehicleRepositry.Object);
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var vm = _fixture.Create<VehicleListViewModel>();
            await vm.Init();

            vm.VehicleSearchText = "registration";

            Assert.Equal(vehicles, vm.Vehicles);

        }

        /// <summary>
        /// Tests that the refresh function on the vehicle list works.
        /// </summary>
        [Fact]
        public async Task VehicleListVM_SuccessfulVehicleListRefreshNoFilter()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            var vehicleRepositry = new Mock<IVehicleRepository>();
            var vehicles = _fixture.CreateMany<Vehicle>();
            vehicleRepositry.Setup(vr => vr.GetAllAsync()).ReturnsAsync(vehicles);

            _fixture.Inject<IVehicleRepository>(vehicleRepositry.Object);
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var vm = _fixture.Create<VehicleListViewModel>();
            await vm.Init();

            vm.VehicleSearchText = "";

            await vm.UpdateVehicleListAsync();

            Assert.Equal(vehicles, vm.Vehicles);
            //Its get all twice because it calls it once on setup and another on refresh
            vehicleRepositry.Verify(vr => vr.GetAllAsync(), Times.Exactly(2));
  
        }
        
        /// <summary>
        /// Tests that the refresh function on the vehicle list works but refilters the list.
        /// </summary>
        [Fact]
        public async Task VehicleListVM_SuccessfulVehicleListRefreshFilter()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            var vehicleRepositry = new Mock<IVehicleRepository>();
            var vehicles = _fixture.CreateMany<Vehicle>(20);
            vehicleRepositry.Setup(vr => vr.GetAllAsync()).ReturnsAsync(vehicles);

            _fixture.Inject<IVehicleRepository>(vehicleRepositry.Object);
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var vm = _fixture.Create<VehicleListViewModel>();
            await vm.Init();

            vm.VehicleSearchText = "Registration";

            await vm.UpdateVehicleListAsync();

            Assert.Equal(vehicles, vm.Vehicles);
            //Its get all twice because it calls it once on setup and another on refresh
            vehicleRepositry.Verify(vr => vr.GetAllAsync(), Times.Exactly(2));

        }
         
    }

}
