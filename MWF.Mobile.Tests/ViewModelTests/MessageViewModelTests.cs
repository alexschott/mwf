using SQLite.Net.Attributes;
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
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MWF.Mobile.Core.Repositories.Interfaces;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Extensions;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class MessageViewModelTests
        : MvxIoCSupportingTest
    {
        #region Setup

        private IFixture _fixture;
        private IInfoService _infoService;
        private MobileData _mobileData;
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private Mock<IMvxMessenger> _mockMvxMessenger;
        private Mock<IDataChunkService> _mockDataChunkService;
        private Mock<INavigationService> _mockNavigationService;
        private NavData<MessageModalNavItem> _navData;
        private Guid _navID;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _infoService = _fixture.Create<InfoService>();
            _fixture.Inject<IInfoService>(_infoService);

            _mobileData = _fixture.Create<MobileData>();
            _mobileData.GroupTitle = "Run1010";
            _mobileData.Order.Type = InstructionType.OrderMessage;

            _mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetByIDAsync(It.Is<Guid>(i => i == _mobileData.ID))).ReturnsAsync(_mobileData);

            _mockDataChunkService = _fixture.InjectNewMock<IDataChunkService>();
            _mockDataChunkService
                .Setup(dc => dc.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.IsAny<MobileData>(), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(i => i == false), It.Is<bool>(i => i == false)))
                .Returns(Task.FromResult(0));

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _mockMvxMessenger = _fixture.InjectNewMock<IMvxMessenger>();
            Ioc.RegisterSingleton<IMvxMessenger>(_mockMvxMessenger.Object);

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();
            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);

            _navData = new NavData<MessageModalNavItem>() { Data = new MessageModalNavItem { MobileDataID = _mobileData.ID, IsRead = false } };
            _navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<MessageModalNavItem>(_navID)).Returns(_navData);
        }

        #endregion Setup

        #region Test

        /// <summary>
        /// This is to test setup for the view model for if its a message with a point.
        /// This is determined if the mobile data has a point (MobileData.Order.Addresses)
        /// </summary>
        [Fact]
        public async Task MessageVM_MessageWithPoint_Setup()
        {
            base.ClearAll();

            var messageVM = _fixture.Create<MessageViewModel>();

            await messageVM.Init(_navID);

            Assert.Equal(true, messageVM.IsWithPoint);
            Assert.Equal(_mobileData.Order.Addresses[0].Lines.Replace("|", "\n") + "\n" + _mobileData.Order.Addresses[0].Postcode, messageVM.Address);

        }

        /// <summary>
        /// This is to test setup for the view model for if its a message.
        /// This is determined if the mobile data is null (MobileData.Order.Addresses)
        /// </summary>
        [Fact]
        public async Task MessageVM_Message_Setup()
        {
            base.ClearAll();

            _mobileData.Order.Addresses = new List<Address>();

            var messageVM = _fixture.Create<MessageViewModel>();

            await messageVM.Init(_navID);

            Assert.Equal(false, messageVM.IsWithPoint);
            Assert.Equal(string.Empty, messageVM.Address);
        }

        [Fact]
        public async Task MessageVM_Message_ReadButton_Unread()
        {
            base.ClearAll();

            _mobileData.Order.Addresses = new List<Address>();

            var messageVM = _fixture.Create<MessageViewModel>();

            await messageVM.Init(_navID);

            Assert.Equal("Mark as read", messageVM.ReadButtonText);

            await messageVM.ReadMessageAsync();

            _mockDataChunkService.Verify(dc => dc.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.IsAny<MobileData>(), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(i => i == false), It.Is<bool>(i => i == false)), Times.Once);
        }

        [Fact]
        public async Task MessageVM_Message_ReadButton_Read()
        {
            base.ClearAll();

            _mobileData.Order.Addresses = new List<Address>();

            var messageVM = _fixture.Create<MessageViewModel>();
            _navData.Data.IsRead = true;

            await messageVM.Init(_navID);

            Assert.Equal("Return", messageVM.ReadButtonText);

            await messageVM.ReadMessageAsync();

            _mockDataChunkService.Verify(dc => dc.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.IsAny<MobileData>(), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(i => i == false), It.Is<bool>(i => i == false)), Times.Never);
        }

        [Fact]
        public async Task MessageVM_MessageWithPoint_ReadButton_Unread()
        {
            base.ClearAll();

            var messageVM = _fixture.Create<MessageViewModel>();

            await messageVM.Init(_navID);

            Assert.Equal("Mark as read", messageVM.ReadButtonText);

            await messageVM.ReadMessageAsync();

            _mockDataChunkService.Verify(dc => dc.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.IsAny<MobileData>(), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(i => i == false), It.Is<bool>(i => i == false)), Times.Once);
        }

        [Fact]
        public async Task MessageVM_MessageWithPoint_ReadButton_Read()
        {
            base.ClearAll();

            var messageVM = _fixture.Create<MessageViewModel>();
            _navData.Data.IsRead = true;

            await messageVM.Init(_navID);

            Assert.Equal("Return", messageVM.ReadButtonText);

            await messageVM.ReadMessageAsync();

            _mockDataChunkService.Verify(dc => dc.SendDataChunkAsync(It.IsAny<MobileApplicationDataChunkContentActivity>(), It.IsAny<MobileData>(), It.IsAny<Guid>(), It.IsAny<string>(), It.Is<bool>(i => i == false), It.Is<bool>(i => i == false)), Times.Never);

        }

        #endregion Test

    }

}
