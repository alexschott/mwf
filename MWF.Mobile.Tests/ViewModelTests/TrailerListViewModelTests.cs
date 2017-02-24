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
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class TrailerListViewModelTests
        :  MvxIoCSupportingTest
    {
        private IFixture _fixture;
        private Driver _driver;
        private Trailer _trailer;
        private TrailerItemViewModel _trailerItemViewModel;
        private IInfoService _infoService;
        private Mock<ICurrentDriverRepository> _currentDriverRepository;
        private Mock<IApplicationProfileRepository> _applicationRepository;
        private Mock<ITrailerRepository> _trailerRepository;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private ApplicationProfile _applicationProfile;

        protected override void AdditionalSetup()
        {
            var mockDispatcher = new MockDispatcher();
            Ioc.RegisterSingleton<IMvxViewDispatcher>(mockDispatcher);
            Ioc.RegisterSingleton<IMvxMainThreadDispatcher>(mockDispatcher);

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            _driver = new Core.Models.Driver() { LastName = "TestName", ID = Guid.NewGuid() };

            _trailer = new Core.Models.Trailer() { Registration = "TestRegistration", ID = Guid.NewGuid() };
            _trailerItemViewModel = new TrailerItemViewModel() { Trailer = _trailer };

            _infoService = _fixture.Create<InfoService>();
            _infoService.CurrentDriverID = _driver.ID;
            _fixture.Inject<IInfoService>(_infoService);

            _currentDriverRepository = new Mock<ICurrentDriverRepository>();
            _currentDriverRepository.Setup(cdr => cdr.GetByIDAsync(It.IsAny<Guid>())).ReturnsAsync(new CurrentDriver());
            _fixture.Inject<ICurrentDriverRepository>(_currentDriverRepository.Object);

            _applicationProfile = new ApplicationProfile { LastVehicleAndDriverSync = DateTime.Now };
            _applicationRepository = _fixture.InjectNewMock<IApplicationProfileRepository>();
            _applicationRepository.Setup(ar => ar.GetAllAsync()).ReturnsAsync(new List<ApplicationProfile>() { _applicationProfile });

            _trailerRepository = new Mock<ITrailerRepository>();
            _fixture.Inject<ITrailerRepository>(_trailerRepository.Object);

            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService.Setup(m => m.AuthenticateAsync(It.IsAny<string>())).ReturnsAsync(new AuthenticationResult { Success = false });
            mockAuthenticationService.Setup(m => m.AuthenticateAsync(It.Is<string>(s => s == "9999"))).ReturnsAsync(new AuthenticationResult { Success = true, Driver = _driver });
            _fixture.Inject<IAuthenticationService>(mockAuthenticationService.Object);

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
        }

        /// <summary>
        /// Tests that on successful authentication the SafetyCheckViewModel is navigated to
        /// </summary>
        [Fact]
        public async Task TrailerListVM_SuccessfulAuthenticationRedirectsToSafetyCheckListView()
        {
            base.ClearAll();

            var navigationServiceMock = new Mock<INavigationService>();
            navigationServiceMock.Setup(ns => ns.MoveToNextAsync()).Returns(Task.FromResult(0));
            _fixture.Inject<INavigationService>(navigationServiceMock.Object);

            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Confirm your trailer");

            var vm = _fixture.Create<TrailerListViewModel>();
            await vm.Init();

            await vm.ConfirmTrailerAsync(_trailerItemViewModel);

            // check that the navigation service was called
            navigationServiceMock.Verify(ns => ns.MoveToNextAsync(), Times.Once);
        }

        /// <summary>
        /// Tests that on successful authentication a drivers trailer ID 
        /// </summary>
        [Fact]
        public async Task TrailerListVM_SuccessfulAuthenticationStoresDriverTrailerID()
        {
            base.ClearAll();

            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Confirm your trailer");

            var vm = _fixture.Create<TrailerListViewModel>();

            await vm.ConfirmTrailerAsync(_trailerItemViewModel);

            Assert.NotNull(_infoService.CurrentDriverID);
            Assert.Equal(_trailer.ID, _infoService.CurrentTrailerID);
        }

        /// <summary>
        /// Tests that on successful selection of no trailer 
        /// </summary>
        [Fact]
        public async Task TrailerListVM_SuccessfulSelectionOfNoTrailer()
        {
            base.ClearAll();

            var vm = _fixture.Create<TrailerListViewModel>();

            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("No trailer");

            await vm.ConfirmNoTrailerAsync();

            Assert.NotNull(_infoService.CurrentDriverID);
            Assert.Null(_infoService.CurrentTrailerID);
        }

        /// <summary>
        /// Tests that the toast message appears when there is no internet connection
        /// on the refresh of the trailer list.
        /// </summary>
        [Fact]
        public async Task TrailerListVM_NoInternetShowToast()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == false));

            var toast = new Mock<IToast>();
            _fixture.Inject<IToast>(toast.Object);

            var vm = _fixture.Create<TrailerListViewModel>();

            await vm.UpdateTrailerListAsync();

            toast.Verify(t => t.Show("No internet connection!"));

        }

        /// <summary>
        /// Tests that the list is filtered correctly when you search for a trailer.
        /// </summary>
        [Fact]
        public async Task TrailerListVM_SuccessfulTrailerListFilter()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            var trailers = _fixture.CreateMany<Trailer>();
            _trailerRepository.Setup(vr => vr.GetAllAsync()).ReturnsAsync(trailers);

            var vm = _fixture.Create<TrailerListViewModel>();
            await vm.Init();

            vm.TrailerSearchText = trailers.First().Registration;

            Assert.Equal(1, vm.Trailers.ToList().Count);

            Assert.Equal(trailers.First(), vm.Trailers.First().Trailer);

        }

        /// <summary>
        /// Tests that the refresh function on the trailer list works.
        /// </summary>
        [Fact]
        public async Task TrailerListVM_SuccessfulTrailerListRefreshNoFilter()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            var trailers = _fixture.CreateMany<Trailer>();
            _trailerRepository.Setup(vr => vr.GetAllAsync()).ReturnsAsync(trailers);

            var vm = _fixture.Create<TrailerListViewModel>();
            await vm.Init();

            vm.TrailerSearchText = "";

            await vm.UpdateTrailerListAsync();

            var trailerModels = vm.Trailers.Select(x => x.Trailer);

            Assert.Equal(trailers, trailerModels);
            //Its get all twice because it calls it once on setup and another on refresh
            _trailerRepository.Verify(vr => vr.GetAllAsync(), Times.Exactly(2));

        }

        /// <summary>
        /// Tests that the refresh function on the trailer list works but refilters the list.
        /// </summary>
        [Fact]
        public async Task TrailerListVM_SuccessfulTrailerListRefreshFilter()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            var trailers = _fixture.CreateMany<Trailer>(20);
            _trailerRepository.Setup(vr => vr.GetAllAsync()).ReturnsAsync(trailers);

            var vm = _fixture.Create<TrailerListViewModel>();
            await vm.Init();

            vm.TrailerSearchText = "Registration";

            await vm.UpdateTrailerListAsync();

            var trailerModels = vm.Trailers.Select(x => x.Trailer);

            Assert.Equal(trailers, trailerModels);
            //Its get all twice because it calls it once on setup and another on refresh
            _trailerRepository.Verify(vr => vr.GetAllAsync(), Times.Exactly(2));

        }

    }

}
