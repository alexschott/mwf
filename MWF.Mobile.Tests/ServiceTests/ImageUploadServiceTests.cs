using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ServiceTests
{
    public class ImageUploadServiceTests : MvxIoCSupportingTest
    {

        #region Private Members

        private IFixture _fixture;
        private Mock<IGpsService> _mockGpsService;
        private Mock<IConfigRepository> _mockConfigRepo;
        private MWFMobileConfig _mockMobileConfig;
        private Mock<ILoggingService> _mockLoggingService;
        private Mock<IHttpService> _mockHttpService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IToast> _mockToast; 


        #endregion Private Members


        #region Setup

        protected override void AdditionalSetup()
        {

            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Register<IReachability>(() => Mock.Of<IReachability>(r => r.IsConnected() == true));
            _mockMobileConfig = _fixture.Build<MWFMobileConfig>().With(m => m.HEUrl, "http://demo.proteoenterprise.co.uk").Create<MWFMobileConfig>();

            _mockGpsService = _fixture.InjectNewMock<IGpsService>();
            _mockGpsService.Setup(mgs => mgs.GetLatitude()).Returns(1);
            _mockGpsService.Setup(mgs => mgs.GetLongitude()).Returns(2);

            _mockConfigRepo = _fixture.InjectNewMock<IConfigRepository>();
            _mockConfigRepo.Setup(mcr => mcr.GetAsync()).ReturnsAsync(_mockMobileConfig);

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            var response = new Core.HttpResult<HttpResponseMessage>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
            };

            _mockHttpService = _fixture.InjectNewMock<Core.Services.IHttpService>();
            _mockHttpService.Setup(mhs => mhs.SendAsyncPlainResponse<HttpResponseMessage>(It.IsAny<HttpRequestMessage>())).ReturnsAsync(response);

            _mockLoggingService = _fixture.InjectNewMock<ILoggingService>();

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockToast = Ioc.RegisterNewMock<IToast>();


        }

        #endregion Setup


        /// <summary>
        /// This test is to verify that the right content is added to the gatewayqueuedservice for a driver uploading
        /// a photo and comment for an instruction.
        /// </summary>
        [Fact]
        public async Task ImageUploadService_SendCommentAndImageAttachedToInstruction()
        {
            base.ClearAll();

            string comment = _fixture.Create<string>();
            List<Image> photos = _fixture.CreateMany<Image>().ToList();

            MobileData mobileData = _fixture.Create<MobileData>();
            Driver driver = _fixture.Create<Driver>();
            var imageUploadService = _fixture.Create<ImageUploadService>();

            await imageUploadService.SendPhotoAndCommentAsync(comment, photos, driver.ID, driver.DisplayName, null);

            _mockConfigRepo.Verify(mcr => mcr.GetAsync(), Times.Once);

            _mockGpsService.Verify(mgs => mgs.GetLongitude(), Times.Exactly(3));
            _mockGpsService.Verify(mgs => mgs.GetLatitude(), Times.Exactly(3));

            //This only gets logged when it has been successfully uploaded
            _mockLoggingService.Verify(mls => mls.LogEventAsync(It.IsAny<string>(), It.Is<MWF.Mobile.Core.Enums.LogType>(i => i == Core.Enums.LogType.Info)), Times.Exactly(3));

            _mockToast.Verify(mt => mt.Show(It.IsAny<string>()), Times.Exactly(2));
        }

        /// <summary>
        /// This test is to verify that the right content is added to the gatewayqueuedservice for a driver uploading
        /// a photo and comment (not attached to an instruction).
        /// </summary>
        [Fact]
        public async Task ImageUploadService_SendCommentAndImageAttachedToNothing()
        {
            base.ClearAll();

            string comment = _fixture.Create<string>();
            List<Image> photos = _fixture.CreateMany<Image>().ToList();

            var imageUploadService = _fixture.Create<ImageUploadService>();

            MobileData mobileData = _fixture.Create<MobileData>();
            Driver driver = _fixture.Create<Driver>();

            await imageUploadService.SendPhotoAndCommentAsync(comment, photos, driver.ID, driver.DisplayName, null);

            _mockConfigRepo.Verify(mcr => mcr.GetAsync(), Times.Once);

            _mockGpsService.Verify(mgs => mgs.GetLongitude(), Times.Exactly(3));
            _mockGpsService.Verify(mgs => mgs.GetLatitude(), Times.Exactly(3));

            //This only gets logged when it has been successfully uploaded
            _mockLoggingService.Verify(mls => mls.LogEventAsync(It.IsAny<string>(), It.Is<MWF.Mobile.Core.Enums.LogType>(i => i == Core.Enums.LogType.Info)), Times.Exactly(3));

            _mockToast.Verify(mt => mt.Show(It.IsAny<string>()), Times.Exactly(2));


        }

        /// <summary>
        /// This test is to verify that the right content is added to the gatewayqueuedservice for a driver uploading
        /// a photo and comment (not attached to an instruction).
        /// </summary>
        [Fact]
        public async Task ImageUploadService_SendFails()
        {
            base.ClearAll();

            string comment = _fixture.Create<string>();
            List<Image> photos = _fixture.CreateMany<Image>().ToList();

            var imageUploadService = _fixture.Create<ImageUploadService>();

            MobileData mobileData = _fixture.Create<MobileData>();
            Driver driver = _fixture.Create<Driver>();

            var response = new Core.HttpResult<HttpResponseMessage>
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
            };

            _mockHttpService.Setup(mhs => mhs.SendAsyncPlainResponse<HttpResponseMessage>(It.IsAny<HttpRequestMessage>())).ReturnsAsync(response);

            await imageUploadService.SendPhotoAndCommentAsync(comment, photos, driver.ID, driver.DisplayName, null);

            _mockConfigRepo.Verify(mcr => mcr.GetAsync(), Times.Once);

            _mockGpsService.Verify(mgs => mgs.GetLongitude(), Times.Exactly(3));
            _mockGpsService.Verify(mgs => mgs.GetLatitude(), Times.Exactly(3));

            //This only gets logged when it has been successfully uploaded
            _mockUserInteraction.Verify(mui => mui.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockToast.Verify(mt => mt.Show(It.IsAny<string>()), Times.Once);


        }

    }
}
