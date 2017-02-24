using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Test.Core;
using Cirrious.MvvmCross.ViewModels;
using Moq;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class StartupViewModelTests : MvxIoCSupportingTest
    {
        private IFixture _fixture;
        private Mock<IMvxViewModelLoader> _viewModelLoader;

        private Mock<IApplicationProfileRepository> _mockApplicationProfile;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _viewModelLoader = _fixture.InjectNewMock<IMvxViewModelLoader>();

            _mockApplicationProfile = _fixture.InjectNewMock<IApplicationProfileRepository>();
            _mockApplicationProfile.Setup(map => map.GetAsync()).ReturnsAsync(_fixture.Create<ApplicationProfile>());
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
        }

        [Fact]
        public async Task StartupVM_EmptyCustomerRepository()
        {
            base.ClearAll();

            // Customer repository will return an empty list
            _fixture.Inject<ICustomerRepository>(Mock.Of<ICustomerRepository>(cr => cr.GetAllAsync() == Task.FromResult(Enumerable.Empty<Customer>())));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var startUpViewModel = _fixture.Create<StartupViewModel>();
            await startUpViewModel.Init();

            // startup view model should return a customer code view model
            _viewModelLoader.Verify(vml => vml.LoadViewModel(It.Is<MvxViewModelRequest>(vmr => vmr.ViewModelType == typeof(CustomerCodeViewModel)), It.IsAny<IMvxBundle>()));
        }

        [Fact]
        public async Task StartupVM_NonEmptyCustomerRepository()
        {
            base.ClearAll();

            _fixture.Inject<ICustomerRepository>(Mock.Of<ICustomerRepository>(cr => cr.GetAllAsync() == Task.FromResult(_fixture.CreateMany<Customer>())));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var startUpViewModel = _fixture.Create<StartupViewModel>();
            await startUpViewModel.Init();

            // startup view model should return a passcode view model
            _viewModelLoader.Verify(vml => vml.LoadViewModel(It.Is<MvxViewModelRequest>(vmr => vmr.ViewModelType == typeof(PasscodeViewModel)), It.IsAny<IMvxBundle>()));
        }

    }

}
