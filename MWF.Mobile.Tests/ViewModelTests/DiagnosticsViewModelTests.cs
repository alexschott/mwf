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
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;
using MWF.Mobile.Tests.Helpers;
using SQLite.Net.Attributes;

namespace MWF.Mobile.Tests.ViewModelTests
{
    
    public class DiagnosticsViewModelTests
        : MvxIoCSupportingTest
    {

        private IFixture _fixture;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private string _softwareVersion = "1.1";
        private string _imei = "SDFSDSADDASDD";
        private string _dbPath = "somedbpath";
        private bool _isConnected = true;
        private Mock<IDiagnosticsService> _mockDiagnosticsService;
        private Mock<IDataService> _mockDataService;

        protected override void AdditionalSetup()
        {

            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Register<IDeviceInfo>(() => Mock.Of<IDeviceInfo>(di => di.AndroidId == _imei &&
                                                                            di.SoftwareVersion == _softwareVersion));
            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == _isConnected));
            _mockUserInteraction = _fixture.InjectNewMock<ICustomUserInteraction>();

            _mockDiagnosticsService = _fixture.InjectNewMock<IDiagnosticsService>();

            _mockDiagnosticsService.Setup(ds => ds.UploadDiagnosticsAsync(It.IsAny<string>())).ReturnsAsync(true);

            _mockDataService = _fixture.InjectNewMock<IDataService>();
            _mockDataService.Setup(ds => ds.DatabasePath).Returns(_dbPath);
        }

        /// <summary>
        /// Tests that the version text is correct
        /// </summary>
        [Fact]
        public void DiagnosticsViewModel_VersionText()
        {
            base.ClearAll();

            var dvm = _fixture.Create<DiagnosticsViewModel>();
      
            // check that the version text is correct
            string versionText = string.Format("Version: {0}           DeviceID: {1}", _softwareVersion, _imei);
            Assert.Equal(versionText, dvm.VersionText);
        }

        [Fact]
        public async Task DiagnosticsViewModel_SendDiagnosticsCommand_NotConnected()
        {
            base.ClearAll();

            _isConnected = false;

            var dvm = _fixture.Create<DiagnosticsViewModel>();

            await dvm.UploadDiagnosticsAsync();

            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s.StartsWith("You need a connection to the internet to submit diagnostics.")), It.IsAny<string>(), It.IsAny<string>()));
        }

        [Fact]
        public async Task DiagnosticsViewModel_SendDiagnosticsCommand_Successful()
        {
            base.ClearAll();

            var dvm = _fixture.Create<DiagnosticsViewModel>();

            await dvm.UploadDiagnosticsAsync();

            _mockDiagnosticsService.Verify(ds => ds.UploadDiagnosticsAsync(It.Is<string>(s => s == _dbPath )));

            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s.StartsWith("Support diagnostic information uploaded successfully")), It.IsAny<string>(), It.IsAny<string>()));
        }

        [Fact]
        public async Task DiagnosticsViewModel_SendDiagnosticsCommand_Unsuccessful()
        {
            base.ClearAll();

            _mockDiagnosticsService.Setup(ds => ds.UploadDiagnosticsAsync(It.IsAny<string>())).ReturnsAsync(false);

            var dvm = _fixture.Create<DiagnosticsViewModel>();

            await dvm.UploadDiagnosticsAsync();

            _mockDiagnosticsService.Verify(ds => ds.UploadDiagnosticsAsync(It.Is<string>(s => s == _dbPath)));

            _mockUserInteraction.Verify(ui => ui.AlertAsync(It.Is<string>(s => s.StartsWith("Unfortunately, there was an error uploading diagnostic data")), It.IsAny<string>(), It.IsAny<string>()));
        }

    }

}
