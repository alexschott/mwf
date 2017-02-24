using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.GatewayServiceRequest;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ServiceTests
{
    public class LoggingServiceTests
        : MvxIoCSupportingTest
    {

        #region Private Members

        private IFixture _fixture;
        private Mock<IGatewayService> _mockGatewayService;
        private Mock<ILogMessageRepository> _mockLogMessageRepo;
        private Mock<IDeviceInfo> _mockDeviceInfo;

        #endregion Private Members

        #region Setup

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));

            _mockDeviceInfo = _fixture.InjectNewMock<IDeviceInfo>();
            _mockDeviceInfo.Setup(mdi => mdi.GetDeviceIdentifier()).Returns("TestID");

            _mockLogMessageRepo = new Mock<ILogMessageRepository>();
            _fixture.Inject<ILogMessageRepository>(_mockLogMessageRepo.Object);
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            Ioc.RegisterSingleton<IMvxMessenger>(_fixture.Create<IMvxMessenger>());

            _mockGatewayService = _fixture.InjectNewMock<IGatewayService>();
        }

        #endregion Setup


        [Fact]
        public async Task LoggingService_LogInfoEvent()
        {
            base.ClearAll();

            var loggingService = _fixture.Create<LoggingService>();

            await loggingService.LogEventAsync("Test Log", Core.Enums.LogType.Info);

            _mockLogMessageRepo.Verify(mlm => mlm.InsertAsync(It.Is<LogMessage>(lm => lm.LogType == Core.Enums.LogType.Info)), Times.Once);
        }

        [Fact]
        public async Task LoggingService_LogExceptions()
        {
            base.ClearAll();

            var loggingService = _fixture.Create<LoggingService>();

            await loggingService.LogEventAsync(new Exception("Test Exception"));

            _mockLogMessageRepo.Verify(mlm => mlm.InsertAsync(It.Is<LogMessage>(lm => lm.LogType == Core.Enums.LogType.Error)), Times.Once);
        }

        [Fact]
        public async Task LoggingService_UploadEventsSuccessfully()
        {
            base.ClearAll();

            var logMessages = _fixture.CreateMany<LogMessage>();
            _mockLogMessageRepo.Setup(mlm => mlm.GetAllAsync()).ReturnsAsync(logMessages);

            HttpResult result = new HttpResult();
            result.StatusCode = System.Net.HttpStatusCode.OK;

            _mockGatewayService.Setup(mgs => mgs.PostLogMessageAsync(It.IsAny<DeviceLogMessage>())).ReturnsAsync(result);

            var loggingService = _fixture.Create<LoggingService>();

            await loggingService.UploadLoggedEventsAsync();

            _mockLogMessageRepo.Verify(mlm => mlm.DeleteAsync(It.IsAny<LogMessage>()), Times.Exactly(logMessages.Count()));
            _mockLogMessageRepo.Verify(mlm => mlm.InsertAsync(It.Is<LogMessage>(lm => lm.LogType == Core.Enums.LogType.LogFailure)), Times.Never);
        }

        [Fact]
        public async Task LoggingService_UploadEventsError()
        {
            base.ClearAll();

            var logMessages = _fixture.CreateMany<LogMessage>();
            _mockLogMessageRepo.Setup(mlm => mlm.GetAllAsync()).ReturnsAsync(logMessages);

            HttpResult result = new HttpResult();

            _mockGatewayService.Setup(mgs => mgs.PostLogMessageAsync(It.IsAny<DeviceLogMessage>())).ReturnsAsync(result);

            var loggingService = _fixture.Create<LoggingService>();

            await loggingService.UploadLoggedEventsAsync();

            var messageCount = logMessages.Count();
            _mockLogMessageRepo.Verify(mlm => mlm.DeleteAsync(It.IsAny<LogMessage>()), Times.Exactly(messageCount));
            _mockLogMessageRepo.Verify(mlm => mlm.InsertAsync(It.Is<LogMessage>(lm => lm.LogType == Core.Enums.LogType.LogFailure)), Times.Exactly(messageCount));
        }

    }

}
