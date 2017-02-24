using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MWF.Mobile.Core.Repositories.Interfaces;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class ManifestViewModelTests
        : MvxIoCSupportingTest
    {
        private IFixture _fixture;
        private MobileData _mobileData;
        private InfoService _infoService;
        private Mock<IMobileDataRepository> _mobileDataRepoMock;
        private Mock<IApplicationProfileRepository> _mockApplicationProfile;
        private Mock<IMvxMessenger> _mockMessenger;
        private Mock<ICheckForSoftwareUpdates> _mockCheckForSoftwareUpdates;
        private Mock<INavigationService> _mockNavigationService;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            _mobileData = _fixture.Create<MobileData>();
            _mobileData.GroupTitle = "Run1010";

            _infoService = _fixture.Create<InfoService>();
            _fixture.Inject<IInfoService>(_infoService);

            _mockApplicationProfile = _fixture.InjectNewMock<IApplicationProfileRepository>();
            List<ApplicationProfile> appProfiles = new List<ApplicationProfile>();
            ApplicationProfile appProfile = new ApplicationProfile();
            appProfile.DisplayRetention = 2;
            appProfile.DisplaySpan = 2;
            appProfiles.Add(appProfile);

            _mockApplicationProfile.Setup(map => map.GetAllAsync()).ReturnsAsync(appProfiles);

            _mobileDataRepoMock = _fixture.InjectNewMock<IMobileDataRepository>();

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            _mockCheckForSoftwareUpdates = Ioc.RegisterNewMock<ICheckForSoftwareUpdates>();

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();
            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);
        }

        /// <summary>
        ///  Tests that the Manifest creates successfully
        /// </summary>
        [Fact]
        public async Task ManifestVM_SuccessfulCreationOfInstruction()
        {
            base.ClearAll();

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            var mobileData = _fixture.Create<MobileData>(new MobileData() { ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileData.EffectiveDate = DateTime.Now;
            mobileDataStartedList.Add(mobileData);

            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            Assert.Equal((mobileDataStartedList.Count + mobileDataNotStartedList.Count), viewModel.InstructionsCount);
            
            //check that the logged indriver id was used for the calls to the mobile data repository
            _mobileDataRepoMock.Verify(mdr => mdr.GetInProgressInstructionsAsync(It.Is<Guid>(i => i == _infoService.CurrentDriverID)), Times.Once);
            _mobileDataRepoMock.Verify(mdr => mdr.GetNotStartedInstructionsAsync(It.Is<Guid>(i => i == _infoService.CurrentDriverID)), Times.Once);

        }

        /// <summary>
        /// Tests that the refresh function on the manifest instruction list works.
        /// </summary>
        [Fact]
        public async Task ManifestVM_SuccessfulInstructionRefresh()
        {
            base.ClearAll();

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            var mobileData = _fixture.Create<MobileData>(new MobileData() { ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileData.EffectiveDate = DateTime.Now.AddDays(-4);
            mobileDataStartedList.Add(mobileData);

            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            await viewModel.UpdateInstructionsListAsync();

            _mobileDataRepoMock.Verify(mdr => mdr.GetInProgressInstructionsAsync(It.Is<Guid>(i => i == _infoService.CurrentDriverID)), Times.Once);
            _mobileDataRepoMock.Verify(mdr => mdr.GetNotStartedInstructionsAsync(It.Is<Guid>(i => i == _infoService.CurrentDriverID)), Times.Once);
        }

        /// <summary>
        /// Tests that the instruction count is correct and doesn't count the dummy mobile data.
        /// </summary>
        [Fact]
        public async Task ManifestVM_CorrectInstructionCount()
        {
            base.ClearAll();

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            var mobileData = _fixture.Create<MobileData>(new MobileData() { ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileData.EffectiveDate = DateTime.Now;
            mobileDataStartedList.Add(mobileData);

            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            Assert.Equal((mobileDataStartedList.Count + mobileDataNotStartedList.Count), viewModel.InstructionsCount);
        }

        /// <summary>
        /// This test make sure instructions are excluded when outside of the display retention
        /// </summary>
        [Fact]
        public void ManifestVM_InstructionDisplayRetention_Exclude()
        {
            base.ClearAll();

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            var mobileData = _fixture.Create<MobileData>(new MobileData() { ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileData.EffectiveDate = DateTime.Now.AddDays(-4);
            mobileDataStartedList.Add(mobileData);

            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();

            Assert.Equal(0, viewModel.InstructionsCount);
        }

        /// <summary>
        /// This test make sure instructions are included within the display retention
        /// </summary>
        [Fact]
        public async Task ManifestVM_InstructionDisplayRetention_Include()
        {
            base.ClearAll();

            List<ApplicationProfile> appProfiles = new List<ApplicationProfile>();
            ApplicationProfile appProfile = new ApplicationProfile();
            appProfile.DisplayRetention = 2;
            appProfile.DisplaySpan = 2;
            appProfiles.Add(appProfile);

            _mockApplicationProfile.Setup(map => map.GetAllAsync()).ReturnsAsync(appProfiles);

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            var mobileData = _fixture.Create<MobileData>(new MobileData() { ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileData.EffectiveDate = DateTime.Now.AddDays(-1);
            mobileDataStartedList.Add(mobileData);

            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            Assert.Equal((mobileDataStartedList.Count + mobileDataNotStartedList.Count), viewModel.InstructionsCount);
        }

        /// <summary>
        /// This test make sure instructions are excluded when outside of the display span
        /// </summary>
        [Fact]
        public void ManifestVM_InstructionDisplaySpan_Exclude()
        {
            base.ClearAll();

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            var mobileData = _fixture.Create<MobileData>(new MobileData() { ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileData.EffectiveDate = DateTime.Now.AddDays(4);
            mobileDataStartedList.Add(mobileData);

            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();

            Assert.Equal(0, viewModel.InstructionsCount);
        }

        /// <summary>
        /// This test make sure instructions are included within the display span
        /// </summary>
        [Fact]
        public async Task ManifestVM_InstructionDisplaySpan_Include()
        {
            base.ClearAll();

            List<ApplicationProfile> appProfiles = new List<ApplicationProfile>();
            ApplicationProfile appProfile = new ApplicationProfile();
            appProfile.DisplayRetention = 2;
            appProfile.DisplaySpan = 2;
            appProfiles.Add(appProfile);

            _mockApplicationProfile.Setup(map => map.GetAllAsync()).ReturnsAsync(appProfiles);

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            var mobileData = _fixture.Create<MobileData>(new MobileData() { ProgressState = Core.Enums.InstructionProgress.OnSite });
            mobileData.EffectiveDate = DateTime.Now.AddDays(1);
            mobileDataStartedList.Add(mobileData);

            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            Assert.Equal((mobileDataStartedList.Count + mobileDataNotStartedList.Count), viewModel.InstructionsCount);
        }


        /// <summary>
        /// Tests that the toast message appears when there is no internet connection
        /// on the refresh of the manifest instruction list.
        /// </summary>
        [Fact]
        public async Task ManifestVM_NoInternetShowToast()
        {
            base.ClearAll();

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == false));

            var toast = new Mock<IToast>();
            _fixture.Inject<IToast>(toast.Object);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();

            await viewModel.UpdateInstructionsListAsync();

            toast.Verify(t => t.Show("No internet connection!"));

        }

        /// <summary>
        /// Tests that the "No Active Instructions" Item displays when no items are in the active instruction section.
        /// </summary>
        [Fact]
        public async Task ManifestVM_ShowNoInstructionItem_ActiveInstruction()
        {
            base.ClearAll();

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            Assert.Equal("No Active Instructions", viewModel.Sections.First().First().PointDescripion);
        }

        /// <summary>
        /// Tests that the "No Instructions" Item displays when no items are in the instruction section.
        /// </summary>
        [Fact]
        public async Task ManifestVM_ShowNoInstructionItem_NonActiveInstruction()
        {
            base.ClearAll();

            List<MobileData> mobileDataStartedList = new List<MobileData>();
            List<MobileData> mobileDataNotStartedList = new List<MobileData>();

            _mobileDataRepoMock.Setup(mdr => mdr.GetInProgressInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataStartedList);
            _mobileDataRepoMock.Setup(mdr => mdr.GetNotStartedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(mobileDataNotStartedList);

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            Assert.Equal("No Instructions", viewModel.Sections.ElementAt(1).First().PointDescripion);
        }

        [Fact]
        public async Task ManifestVM_CheckInstructionNotification()
        {
            base.ClearAll();

            var viewModel = _fixture.Build<ManifestViewModel>().Without(mvm => mvm.Sections).Create();
            await viewModel.Init();

            _mobileData.GroupTitle = "UpdateTitle";

            await viewModel.CheckInstructionNotificationAsync(new GatewayInstructionNotificationMessage(this, _mobileData.ID, GatewayInstructionNotificationMessage.NotificationCommand.Update));

            //It is checked twice because it checks on view model Init() and when you refresh.
            _mobileDataRepoMock.Verify(mdr => mdr.GetInProgressInstructionsAsync(It.Is<Guid>(i => i == _infoService.CurrentDriverID)), Times.Exactly(2));
            _mobileDataRepoMock.Verify(mdr => mdr.GetNotStartedInstructionsAsync(It.Is<Guid>(i => i == _infoService.CurrentDriverID)), Times.Exactly(2));

        }

    }

}
