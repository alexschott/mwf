using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ViewModelTests
{
    
    public class InstructionSafetyCheckViewModelTests
        : MvxIoCSupportingTest
    {

        private IFixture _fixture;
        private IInfoService _infoService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMvxMessenger> _mockMessenger;
        private MobileData _mobileData;
        private NavData<MobileData> _navData;
        private Core.Models.Driver _driver;
        private Core.Models.Vehicle _vehicle;
        private Core.Models.Trailer _trailer;
        private Mock<ISafetyProfileRepository> _mockSafetyProfileRepository;
        private Mock<IDriverRepository> _mockDriverRepository;
        private Mock<INavigationService> _mockNavigationService;
        IEnumerable<SafetyProfile> _safetyProfiles;
        private SafetyProfile _safetyProfile;
        private Guid _navID;

        protected override void AdditionalSetup()
        {

            _mockUserInteraction = new Mock<ICustomUserInteraction>();
            Ioc.RegisterSingleton<ICustomUserInteraction>(_mockUserInteraction.Object);

            _mockMessenger = new Mock<IMvxMessenger>();
            Ioc.RegisterSingleton<IMvxMessenger>(_mockMessenger.Object);

            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyCheckItemViewModels));
            _fixture.Customize<InstructionSafetyCheckViewModel>(vm => vm.Without(x => x.SafetyProfileVehicle));

            _driver = _fixture.Create<Core.Models.Driver>();
            _vehicle = _fixture.Create<Core.Models.Vehicle>();
            _trailer = _fixture.Create<Core.Models.Trailer>();

            _infoService = _fixture.Create<InfoService>();
            _fixture.Inject<IInfoService>(_infoService);
            _infoService.SetCurrentDriver(_driver);
            _infoService.SetCurrentVehicle(_vehicle);

            _mockDriverRepository = _fixture.InjectNewMock<IDriverRepository>();
            _mockDriverRepository.Setup(dr => dr.GetByIDAsync(_driver.ID)).ReturnsAsync(_driver);

            _mobileData = _fixture.SetUpInstruction(Core.Enums.InstructionType.Collect, false, true, false, false, false, false, true, null);
            _navData = new NavData<MobileData>() { Data = _mobileData };

            _navData.OtherData["UpdatedTrailer"] = _trailer;

            var mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == _mobileData.ID))).ReturnsAsync(_mobileData);

            _mockSafetyProfileRepository = _fixture.InjectNewMock<ISafetyProfileRepository>();

            var repositories = _fixture.Create<Repositories>();
            _fixture.Inject<IRepositories>(repositories);

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();
            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);

            _safetyProfiles = CreateSafetyProfiles();

            _navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<MobileData>(_navID)).Returns(_navData);
        }

        private IEnumerable<SafetyProfile> CreateSafetyProfiles()
        {
            _safetyProfile = _fixture.Create<SafetyProfile>();

            _safetyProfile.IsTrailerProfile = true;
            _safetyProfile.IntLink = _trailer.SafetyCheckProfileIntLink;

            List<SafetyProfile> profiles = new List<SafetyProfile>() { _safetyProfile };

            return profiles;
        }

        // Checks that after initialization the view model displays the safety checks for the trailer being checked
        [Fact]
        public async Task InstructionSafetyCheckVM_Init()
        {
            base.ClearAll();

            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(_safetyProfiles);

            var vm = _fixture.Create<InstructionSafetyCheckViewModel>();

            await vm.Init(_navID);

            // check that we got the right safety profile for the trailer
            Assert.Equal(vm.SafetyProfileTrailer, _safetyProfile);

            // check that we have the right number of items being displayed in the checklist
            Assert.Equal(vm.SafetyCheckItemViewModels.Count, _safetyProfile.Children.ToList().Count);

            //check that the safety check data is correct
            var safetyCheckData = _navData.OtherData["UpdatedTrailerSafetyCheckData"] as SafetyCheckData;
            Assert.Equal(safetyCheckData.DriverID, _driver.ID);
            Assert.Equal(safetyCheckData.VehicleID, _trailer.ID);
            Assert.Equal(safetyCheckData.Faults.ToList().Count, _safetyProfile.Children.ToList().Count);

        }


        [Fact]
        public async Task InstructionSafetyCheckVM_Init_NoSafetyProfile()
        {
            base.ClearAll();

            _mockSafetyProfileRepository.Setup(spr => spr.GetAllAsync()).ReturnsAsync(new List<SafetyProfile>());

            _mockUserInteraction
                .Setup(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            var vm = _fixture.Build<InstructionSafetyCheckViewModel>().With(iscvm => iscvm.IsProgressing, false).Create();

            await vm.Init(_navID);

            // no safety profile required for the trailer
            Assert.Null(vm.SafetyProfileTrailer);
            Assert.False(_navData.OtherData.IsDefined("UpdatedTrailerSafetyCheckData"));

            //should have shown the custom user interaction and moved to the the next view model
            _mockUserInteraction.Verify(cui => cui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockNavigationService.Verify(ns => ns.MoveToNextAsync(It.Is<NavData<MobileData>>(x => x == _navData)), Times.Once);
        }

    }

}
