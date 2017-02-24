using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Tests.ServiceTests
{
    public class GatewayPollingServiceTests : MvxIoCSupportingTest
    {
        private IFixture _fixture;
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<IGatewayQueuedService> _mockGatewayQueuedService;
        private ApplicationProfile _applicationProfile;
        private Mock<IGatewayService> _gatewayMock;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMvxMessenger> _mockMvxMessenger;
        private IInfoService _mockInfoService;
        private Mock<IDataChunkService> _mockDataChunkService;
        private Mock<INavigationService> _mockNavigationService;
        private object ssr;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            IDeviceRepository deviceRepo = Mock.Of<IDeviceRepository>(dr => dr.GetAllAsync() == Task.FromResult(_fixture.CreateMany<Device>()));
            _mockMobileDataRepo = new Mock<IMobileDataRepository>();
            _fixture.Inject<IMobileDataRepository>(_mockMobileDataRepo.Object);

            _mockGatewayQueuedService = new Mock<IGatewayQueuedService>();
            _fixture.Inject<IGatewayQueuedService>(_mockGatewayQueuedService.Object);

            _applicationProfile = new ApplicationProfile();
            var applicationRepo = _fixture.InjectNewMock<IApplicationProfileRepository>();
            applicationRepo.Setup(ar => ar.GetAllAsync()).ReturnsAsync(new List<ApplicationProfile>() { _applicationProfile });

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            //Setup to skip the modal and call the action instead.
            _mockUserInteraction.Setup(cui => cui.PopUpInstructionNotificationAsync(It.IsAny<List<ManifestInstructionViewModel>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<List<ManifestInstructionViewModel>, string, string>((vms, title, ok) => Task.FromResult(vms));

            _mockInfoService = Mock.Of<IInfoService>(s => s.CurrentVehicleID == Guid.NewGuid() && s.CurrentDriverID == Guid.NewGuid());

            _fixture.Inject<IInfoService>(_mockInfoService);

            IRepositories repos = Mock.Of<IRepositories>(r => r.DeviceRepository == deviceRepo);
            _fixture.Register<IRepositories>(() => repos);

            _mockMvxMessenger = new Mock<IMvxMessenger>();
            _fixture.Inject<IMvxMessenger>(_mockMvxMessenger.Object);

            _mockDataChunkService = _fixture.InjectNewMock<IDataChunkService>();

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();
            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);
        }

        [Fact]
        public async Task GatewayPollingService_AddsSingleInstruction()
        {
            base.ClearAll();
            var id = Guid.NewGuid();
            CreateSingleMobileData(SyncState.Add, id);

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(2000);

            // Check that we insert the instruction
            _mockMobileDataRepo.Verify(mdr => mdr.InsertAsync(It.Is<MobileData>(md => md.ID == id)), Times.Once);
        }

        [Fact]
        public async Task GatewayPollingService_RespectsApplicationProfile_DataSpan()
        {
            base.ClearAll();

            _applicationProfile.DataSpan = 5;

            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now;

            IEnumerable<MobileData> mobileDatas = new List<MobileData>();

            _gatewayMock = _fixture.InjectNewMock<IGatewayService>();
            _gatewayMock.Setup(g => g.GetDriverInstructionsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                        .ReturnsAsync(mobileDatas)
                        .Callback<string, Guid, DateTime, DateTime>((s1, g, dt1, dt2) => { startDate = dt1; endDate = dt2; });

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(2000);

            //Check that the start and end date of driver instructions requested from gateway service matches up with the the span 
            //specified int he application profile
            Assert.Equal(_applicationProfile.DataSpan, (endDate - startDate).Days);
        }

        [Fact]
        public async Task GatewayPollingService_UpdatesSingleInstruction()
        {
            base.ClearAll();
            var id = Guid.NewGuid();
            CreateSingleMobileData(SyncState.Update, id);

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(100);

            // Check we look for the instruction
            _mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(g => g.ToString() == id.ToString())), Times.Once);
            // Check we delete the old instruction
            _mockMobileDataRepo.Verify(mdr => mdr.DeleteAsync(It.Is<MobileData>(md => md.ID == id)), Times.Once);
            // Check that we insert the new instruction
            _mockMobileDataRepo.Verify(mdr => mdr.InsertAsync(It.Is<MobileData>(md => md.ID == id)), Times.Once);
            // Check that it publishes the updated instruction to the current view model
            _mockMvxMessenger.Verify(mm => mm.Publish(It.Is<GatewayInstructionNotificationMessage>(inm => inm.UpdatedInstructionIDs.Contains(id))), Times.Once);
        }

        [Fact]
        public async Task GatewayPollingService_DeletesSingleInstruction()
        {
            base.ClearAll();
            var id = Guid.NewGuid();
            CreateSingleMobileData(SyncState.Delete, id);

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(100);

            // Check we look for the instruction
            _mockMobileDataRepo.Verify(mdr => mdr.GetByIDAsync(It.Is<Guid>(g => g.ToString() == id.ToString())), Times.Once);
            // Check we delete the instruction
            _mockMobileDataRepo.Verify(mdr => mdr.DeleteAsync(It.Is<MobileData>(md => md.ID == id)), Times.Once);
            // Check that it publishes the deleted instruction to the current view model
            _mockMvxMessenger.Verify(mm => mm.Publish(It.Is<GatewayInstructionNotificationMessage>(inm => inm.DeletedInstructionIDs.Contains(id))));

        }

        [Fact]
        public async Task GatewayPollingService_AddsMultipleInstructions()
        {
            base.ClearAll();

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            Guid[] ids = { id1, id2, id3 };
            CreateMultipleMobileDatas(SyncState.Add, ids);

            List<MobileData> insertList = new List<MobileData>();

            _mockMobileDataRepo.Setup(mdr => mdr.InsertAsync(It.IsAny<MobileData>()))
                .Callback<MobileData>(md => { insertList.Add(md); })
                .Returns(Task.FromResult(0));

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(100);

            // Check that we insert the instructions
            var counter = 0;
            foreach (var item in insertList)
            {
                Assert.Equal(item.ID, ids[counter]);
                counter++;
            }
        }

        [Fact]
        public async Task GatewayPollingService_UpdatesMultipleInstructions()
        {
            base.ClearAll();

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            Guid[] ids = { id1, id2, id3 };
            CreateMultipleMobileDatas(SyncState.Update, ids);

            var getList = new List<Guid>();
            var deleteList = new List<MobileData>();
            var insertList = new List<MobileData>();

            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.IsAny<Guid>()))
                               .Callback<Guid>(id => { getList.Add(id); })
                               .Returns((Guid id) => Task.FromResult(new MobileData { ID = id }));

            _mockMobileDataRepo.Setup(mdr => mdr.DeleteAsync(It.IsAny<MobileData>()))
                               .Callback<MobileData>(md => { deleteList.Add(md); })
                               .Returns(Task.FromResult(0));

            _mockMobileDataRepo.Setup(mdr => mdr.InsertAsync(It.IsAny<MobileData>()))
                               .Callback<MobileData>(md => { insertList.Add(md); })
                               .Returns(Task.FromResult(0));

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(100);

            // Check that we get the instructions
            var getCounter = 0;
            foreach (var item in getList)
            {
                Assert.Equal(item, ids[getCounter]);
                getCounter++;
            }

            // Check that we delete the instructions
            var deleteCounter = 0;
            foreach (var item in deleteList)
            {
                Assert.Equal(item.ID, ids[deleteCounter]);
                deleteCounter++;
            }

            // Check that we insert the instructions
            var insertCounter = 0;
            foreach (var item in insertList)
            {
                Assert.Equal(item.ID, ids[insertCounter]);
                insertCounter++;
            }

            // Check that the instruction updates have been published
            _mockMvxMessenger.Verify(mm => mm.Publish(It.Is<GatewayInstructionNotificationMessage>(inm => inm.UpdatedInstructionIDs.Union(ids).Count() == ids.Count())), Times.Once);
        }

        [Fact]
        public async Task GatewayPollingService_DeletesMultipleInstructions()
        {
            base.ClearAll();

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            Guid[] ids = { id1, id2, id3 };
            CreateMultipleMobileDatas(SyncState.Delete, ids);

            var getList = new List<Guid>();
            var deleteList = new List<MobileData>();

            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.IsAny<Guid>()))
                               .Callback<Guid>(id => { getList.Add(id); })
                               .Returns((Guid id) => Task.FromResult(new MobileData { ID = id }));

            _mockMobileDataRepo.Setup(mdr => mdr.DeleteAsync(It.IsAny<MobileData>()))
                               .Callback<MobileData>(md => { deleteList.Add(md); })
                               .Returns(Task.FromResult(0));

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(100);

            // Check that we get the instructions
            var getCounter = 0;
            foreach (var item in getList)
            {
                Assert.Equal(item, ids[getCounter]);
                getCounter++;
            }

            // Check that we delete the instructions
            var deleteCounter = 0;
            foreach (var item in deleteList)
            {
                Assert.Equal(item.ID, ids[deleteCounter]);
                deleteCounter++;
            }

            // Check that the instruction deletions have been published
            _mockMvxMessenger.Verify(mm => mm.Publish(It.Is<GatewayInstructionNotificationMessage>(inm => inm.DeletedInstructionIDs.Union(ids).Count() == ids.Count())), Times.Once);
        }

        /// <summary>
        /// This tests is to make sure when a instruction is polled then an acknowledgement is sent off.
        /// </summary>
        [Fact]
        public async Task GatewayPollingService_AcknowledgeInstruction()
        {
            base.ClearAll();
            var id = Guid.NewGuid();
            CreateSingleMobileData(SyncState.Add, id);

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(2000);

            _mockGatewayQueuedService.Verify(mgqs =>
                mgqs.AddToQueueAsync(It.IsAny<IEnumerable<MWF.Mobile.Core.Models.GatewayServiceRequest.Action<MWF.Mobile.Core.Models.SyncAck>>>()), Times.Once);

            _mockDataChunkService.Verify(mms => mms.SendReadChunkAsync(It.IsAny<IEnumerable<MobileData>>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GatewayPollingService_SingleInstructionNotificationPopUp()
        {
            base.ClearAll();
            var id = Guid.NewGuid();
            CreateSingleMobileData(SyncState.Add, id);

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(2000);

            _mockUserInteraction.Verify(cui => cui.PopUpInstructionNotificationAsync(It.Is<List<ManifestInstructionViewModel>>(lmd => lmd.Count == 1), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockGatewayQueuedService.Verify(mgqs =>
             mgqs.AddToQueueAsync(It.IsAny<IEnumerable<MWF.Mobile.Core.Models.GatewayServiceRequest.Action<MWF.Mobile.Core.Models.SyncAck>>>()), Times.Once);

            _mockDataChunkService.Verify(mms => mms.SendReadChunkAsync(It.IsAny<IEnumerable<MobileData>>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GatewayPollingService_MultipleInstructionNotificationPopUp()
        {
            base.ClearAll();

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            Guid[] ids = { id1, id2, id3 };
            CreateMultipleMobileDatas(SyncState.Delete, ids);

            List<Guid> getList = new List<Guid>();
            List<MobileData> deleteList = new List<MobileData>();

            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.IsAny<Guid>()))
                               .ReturnsAsync(new MobileData { SyncState = SyncState.Delete })
                               .Callback<Guid>((md) => { getList.Add(md); });

            _mockMobileDataRepo.Setup(mdr => mdr.DeleteAsync(It.IsAny<MobileData>()))
                               .Callback<MobileData>((md) => { deleteList.Add(md); })
                               .Returns(Task.FromResult(0));

            _fixture.Register<Core.Portable.IReachability>(() => Mock.Of<Core.Portable.IReachability>(r => r.IsConnected()));
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());
            var service = _fixture.Create<GatewayPollingService>();

            service.StartPollingTimer();
            await service.PollForInstructionsAsync();

            // Allow the timer to process the queue
            await Task.Delay(100);

            _mockUserInteraction.Verify(cui => cui.PopUpInstructionNotificationAsync(It.Is<List<ManifestInstructionViewModel>>(lmd => lmd.Count == ids.Count()), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockGatewayQueuedService.Verify(mgqs =>
             mgqs.AddToQueueAsync(It.IsAny<IEnumerable<MWF.Mobile.Core.Models.GatewayServiceRequest.Action<MWF.Mobile.Core.Models.SyncAck>>>()), Times.Once);

            _mockDataChunkService.Verify(mms => mms.SendReadChunkAsync(It.IsAny<IEnumerable<MobileData>>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);

        }

        #region Helpers

        private void CreateSingleMobileData(SyncState syncState, Guid id)
        {
            var mobileData = _fixture.Create<MobileData>();
            mobileData.ID = id;
            mobileData.SyncState = syncState;

            if (syncState == SyncState.Update || syncState == SyncState.Delete)
            {
                _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(g => g == id))).ReturnsAsync(mobileData);
            }

            IEnumerable<MobileData> mobileDatas = new List<MobileData>
                {
                    mobileData
                };

            _gatewayMock = new Mock<IGatewayService>();
            _gatewayMock.Setup(
                gm =>
                gm.GetDriverInstructionsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTime>(),
                    It.IsAny<DateTime>())).ReturnsAsync(mobileDatas);

            _fixture.Inject(_gatewayMock.Object);
        }

        private void CreateMultipleMobileDatas(SyncState syncState, Guid[] ids)
        {
            var mobileDatas = _fixture.CreateMany<MobileData>();
            var counter = 0;
            foreach (var mobileData in mobileDatas)
            {
                mobileData.ID = ids[counter];
                mobileData.SyncState = syncState;
                counter++;
            }

            _gatewayMock = new Mock<IGatewayService>();
            _gatewayMock.Setup(
                gm =>
                gm.GetDriverInstructionsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTime>(),
                    It.IsAny<DateTime>())).ReturnsAsync(mobileDatas);

            _fixture.Inject(_gatewayMock.Object);
        }

        #endregion
    }
}
