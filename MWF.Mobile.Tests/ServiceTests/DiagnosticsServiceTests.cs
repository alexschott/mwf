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
using System.IO;

namespace MWF.Mobile.Tests.ServiceTests
{
    public class DiagnosticsServiceTests
        : MvxIoCSupportingTest
    {

        #region Private Members

        private IFixture _fixture;
        private Mock<IConfigRepository> _mockConfigRepo;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private bool _isConnected = true;
        private MWFMobileConfig _config;
        private Mock<IUpload> _mockUpload;

        #endregion Private Members


        #region Setup

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == _isConnected));

            _fixture.Register<IDeviceInfo>(() => Mock.Of<IDeviceInfo>(di => di.AndroidId == "TESTID"));

            _config = _fixture.Create<MWFMobileConfig>();
            _mockConfigRepo = new Mock<IConfigRepository>();
            _mockConfigRepo.Setup(cr => cr.GetAsync()).ReturnsAsync(_config);
            _fixture.Inject<IConfigRepository>(_mockConfigRepo.Object);
            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _mockUpload = _fixture.InjectNewMock<IUpload>();

            _mockUpload.Setup(u => u.UploadFileAsync(It.IsAny<Uri>(),
                                            It.IsAny<string>(),
                                            It.IsAny<string>(),
                                            It.IsAny<string>())).ReturnsAsync(true);

            Ioc.RegisterSingleton<IMvxMessenger>(_fixture.Create<IMvxMessenger>());


            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

        }

        #endregion Setup


        [Fact]
        // Should return false when the device is not connected
        public async Task DiagnosticsService_UploadDiagnostics_NotConnected()
        {
            base.ClearAll();

            _isConnected = false;

            var diagnosticsService = _fixture.Create<DiagnosticsService>();

            bool result = await diagnosticsService.UploadDiagnosticsAsync("someDbPath");

            Assert.False(result);

        }

        [Fact]
        // Should display alert when the ftp url has not been set
        public async Task DiagnosticsService_UploadDiagnostics_EmptyFtpUrl()
        {
            base.ClearAll();

            _config.FtpUrl = "";

            var diagnosticsService = _fixture.Create<DiagnosticsService>();

            bool result = await diagnosticsService.UploadDiagnosticsAsync("someDbPath");

            Assert.False(result);

            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s.StartsWith("Your FTP credentials have not been set up")), It.IsAny<string>(), It.IsAny<string>()));

        }

        [Fact]
        // Should display alert when the ftp username has not been set
        public async Task DiagnosticsService_UploadDiagnostics_EmptyFtpUsername()
        {
            base.ClearAll();

            _config.FtpUsername = "";

            var diagnosticsService = _fixture.Create<DiagnosticsService>();

            bool result = await diagnosticsService.UploadDiagnosticsAsync("someDbPath");

            Assert.False(result);

            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s.StartsWith("Your FTP credentials have not been set up")), It.IsAny<string>(), It.IsAny<string>()));

        }

        [Fact]
        // Should display alert when the ftp password has not been set
        public async Task DiagnosticsService_UploadDiagnostics_EmptyFtpPassword()
        {
            base.ClearAll();

            _config.FtpPassword = "";

            var diagnosticsService = _fixture.Create<DiagnosticsService>();

            bool result = await diagnosticsService.UploadDiagnosticsAsync("someDbPath");

            Assert.False(result);

            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s.StartsWith("Your FTP credentials have not been set up")), It.IsAny<string>(), It.IsAny<string>()));

        }

        [Fact]
        // Should hit the upload service and return true 
        public async Task DiagnosticsService_UploadDiagnostics_SuccessfulUpload()
        {
            base.ClearAll();

            _config.FtpUrl = "http://sensibleurl.com";

            var diagnosticsService = _fixture.Create<DiagnosticsService>();

            bool result = await diagnosticsService.UploadDiagnosticsAsync("someDbPath");

            Assert.True(result);

            var ftpUri = string.Format("{0}/{1}/{2}", _config.FtpUrl , "TESTID", "someDbPath");

            _mockUpload.Verify(u => u.UploadFileAsync(It.Is<Uri>(uri => uri.AbsoluteUri == ftpUri),
                                                 It.Is<string>(s => s == _config.FtpUsername),
                                                 It.Is<string>(s => s == _config.FtpPassword),
                                                 It.Is<string>(s => s == "someDbPath")));

        }

        [Fact]
        // Should hit the upload service and return true 
        public async Task DiagnosticsService_UploadDiagnostics_UnsuccessfulUpload()
        {
            base.ClearAll();

            _config.FtpUrl = "http://sensibleurl.com";


            _mockUpload.Setup(u => u.UploadFileAsync(It.IsAny<Uri>(),
                                         It.IsAny<string>(),
                                         It.IsAny<string>(),
                                         It.IsAny<string>())).ReturnsAsync(false);

            var diagnosticsService = _fixture.Create<DiagnosticsService>();

            bool result = await diagnosticsService.UploadDiagnosticsAsync("someDbPath");

            Assert.False(result);

            var ftpUri = string.Format("{0}/{1}/{2}", _config.FtpUrl, "TESTID", "someDbPath");

            _mockUpload.Verify(u => u.UploadFileAsync(It.Is<Uri>(uri => uri.AbsoluteUri == ftpUri),
                                                 It.Is<string>(s => s == _config.FtpUsername),
                                                 It.Is<string>(s => s == _config.FtpPassword),
                                                 It.Is<string>(s => s == "someDbPath")));

        }

    }
}
