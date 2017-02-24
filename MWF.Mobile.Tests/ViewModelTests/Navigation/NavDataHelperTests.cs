using Cirrious.MvvmCross.Test.Core;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using MWF.Mobile.Tests.Helpers;

namespace MWF.Mobile.Tests.ViewModels.Navigation
{
    public class NavDataHelperTests
        : MvxIoCSupportingTest
    {

        #region Test SetUp

        private IFixture _fixture;
        private Mock<IMobileDataRepository> _mobileDataRepositoryMock;
        private IRepositories _repositories;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.OmitProperty<MobileApplicationDataChunkContentActivity>("EffectiveDateString");

            _mobileDataRepositoryMock = new Mock<IMobileDataRepository>();
            _fixture.Inject<IMobileDataRepository>(_mobileDataRepositoryMock.Object);
            _repositories = _fixture.Create<Repositories>();
        }

        #endregion

        #region Tests

        [Fact]
        public void NavDataHelper_GetAdditionalInstructions_NothingSet()
        {
            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            Assert.Equal(0, navData.GetAdditionalInstructions().Count);

        }

        [Fact]
        public void NavDataHelper_GetAdditionalInstructions()
        {
            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            var additionalInstructions = _fixture.CreateMany<MobileData>().ToList();
            navData.OtherData["AdditionalInstructions"] = additionalInstructions;
            Assert.Equal(additionalInstructions, navData.GetAdditionalInstructions());
        }

         [Fact]
        public void NavDataHelper_GetAllInstructions_NoAdditionalInstructions()
        {
            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            Assert.Equal(1 , navData.GetAllInstructions().Count());
            Assert.Equal(navData.Data, navData.GetAllInstructions().First());
        }

        [Fact]
        public void NavDataHelper_GetAllInstructions()
        {
            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            var additionalInstructions = _fixture.CreateMany<MobileData>().ToList();
            navData.OtherData["AdditionalInstructions"] = additionalInstructions;

            Assert.Equal(additionalInstructions.Count + 1 , navData.GetAllInstructions().Count());
            Assert.Equal(navData.Data, navData.GetAllInstructions().First());

            for (int i = 0; i < additionalInstructions.Count; i++)
            {
                Assert.Equal(additionalInstructions[i], navData.GetAllInstructions().ToList()[i + 1]);
            }

        }

        [Fact]
        public void NavDataHelper_GetAdditionalDataChunk()
        {
            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            var additionalInstructions = _fixture.CreateMany<MobileData>().ToList();
            navData.OtherData["AdditionalInstructions"] = additionalInstructions;

            var additionalDataChunks = new Dictionary<Guid, MobileApplicationDataChunkContentActivity>();
            foreach (var additionalInstruction in additionalInstructions)
            {
                additionalDataChunks[additionalInstruction.ID] = _fixture.Create<MobileApplicationDataChunkContentActivity>();
            }

            navData.OtherData["AdditionalDataChunks"] = additionalDataChunks;


            foreach (var additionalInstruction in additionalInstructions)
	        {
                Assert.Equal(additionalDataChunks[additionalInstruction.ID], navData.GetAdditionalDataChunk(additionalInstruction));
	        }

        }

        [Fact]
        public void NavDataHelper_GetAdditionalDataChunk_UnknownMobileData()
        {
            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            var additionalInstructions = _fixture.CreateMany<MobileData>().ToList();
            navData.OtherData["AdditionalInstructions"] = additionalInstructions;

            var additionalDataChunks = new Dictionary<Guid, MobileApplicationDataChunkContentActivity>();
            foreach (var additionalInstruction in additionalInstructions)
            {
                additionalDataChunks[additionalInstruction.ID] = _fixture.Create<MobileApplicationDataChunkContentActivity>();
            }

            navData.OtherData["AdditionalDataChunks"] = additionalDataChunks;


            // should create a new data chunk
            var unknownMobileData = _fixture.Create<MobileData>();
            var dataChunk = navData.GetAdditionalDataChunk(unknownMobileData);

            Assert.Equal(additionalDataChunks[unknownMobileData.ID], navData.GetAdditionalDataChunk(unknownMobileData));


        }


        [Fact]
        public void NavDataHelper_GetAllDataChunks()
        {
            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            var additionalInstructions = _fixture.CreateMany<MobileData>().ToList();
            navData.OtherData["AdditionalInstructions"] = additionalInstructions;

            var additionalDataChunks = new Dictionary<Guid, MobileApplicationDataChunkContentActivity>();
            foreach (var additionalInstruction in additionalInstructions)
            {
                additionalDataChunks[additionalInstruction.ID] = _fixture.Create<MobileApplicationDataChunkContentActivity>();
            }

            navData.OtherData["AdditionalDataChunks"] = additionalDataChunks;

            var mainDataChunk = _fixture.Create<MobileApplicationDataChunkContentActivity>();
            navData.OtherData["DataChunk"] = mainDataChunk;


            var allDataChunks = navData.GetAllDataChunks().ToList();

            Assert.Equal(additionalDataChunks.Count + 1, allDataChunks.Count);
            Assert.Equal(mainDataChunk, allDataChunks.First());


            for (int i = 0; i < additionalDataChunks.Count; i++)
            {
                Assert.Equal(additionalDataChunks.Values.ToList()[i], allDataChunks[i + 1]);
            }


        }


        [Theory]
        [InlineData(0, false, true, true, false, false)]
        [InlineData(1, false, true, true, false, false)]
        [InlineData(0, true, true, true, false, false)]
        [InlineData(1, true, true, true, false, false)]
        [InlineData(0, false, false, true, false, false)]
        [InlineData(1, false, false, true, false, false)]
        [InlineData(0, false, true, false, false, false)]
        [InlineData(1, false, true, false, false, false)]
        [InlineData(0, false, true, true, true, false)]
        [InlineData(1, false, true, true, true, false)]
        [InlineData(0, false, true, true, false, true)]
        [InlineData(1, false, true, true, false, true)]
        public void NavDataHelper_GetWorseCaseDeliveryOptions(int instructionIndex, bool barcodeScanningOnDelivery,  bool bypassClausedScreen,  bool bypassCommentScreen, bool customerSignatureRequired, bool customerNameRequired)
        {

            base.ClearAll();

            var navData = _fixture.Create<NavData<MobileData>>();
            var additionalInstructions = _fixture.CreateMany<MobileData>().ToList();
            navData.OtherData["AdditionalInstructions"] = additionalInstructions;

            SetDeliveryOptionsOnAllInstructions(navData,
                barcodeScanningOnDelivery: false,
                bypassClausedScreen: true,
                bypassCommentScreen: true,
                customerSignatureRequired: false,
                customerNameRequired: false);

            var allInstructions = navData.GetAllInstructions().ToList();


            allInstructions[instructionIndex].Order.Items.FirstOrDefault().Additional.BarcodeScanRequiredForDelivery = barcodeScanningOnDelivery;
            allInstructions[instructionIndex].Order.Items.FirstOrDefault().Additional.BypassCleanClausedScreen = bypassClausedScreen;
            allInstructions[instructionIndex].Order.Items.FirstOrDefault().Additional.BypassCommentsScreen = bypassCommentScreen;
            allInstructions[instructionIndex].Order.Additional.CustomerNameRequiredForDelivery = customerNameRequired;
            allInstructions[instructionIndex].Order.Additional.CustomerSignatureRequiredForDelivery = customerSignatureRequired;


            var deliveryOptions = navData.GetWorseCaseDeliveryOptions();

            Assert.Equal(barcodeScanningOnDelivery, deliveryOptions.BarcodeScanRequiredForDelivery);
            Assert.Equal(bypassClausedScreen, deliveryOptions.BypassCleanClausedScreen);
            Assert.Equal(bypassCommentScreen, deliveryOptions.BypassCommentsScreen);
            Assert.Equal(customerSignatureRequired, deliveryOptions.CustomerSignatureRequiredForDelivery);
            Assert.Equal(customerNameRequired, deliveryOptions.CustomerNameRequiredForDelivery);
        }

        #endregion

        #region helper functions

        private void SetDeliveryOptionsOnAllInstructions(NavData<MobileData> navData, 
                                                         bool barcodeScanningOnDelivery = false,
                                                         bool bypassCommentScreen = true,
                                                         bool bypassClausedScreen = true,
                                                         bool customerSignatureRequired = false,
                                                         bool customerNameRequired = false)
        {
            var mobileDatas = navData.GetAllInstructions();
            foreach (var mobileData in mobileDatas)
            {
                foreach (var item in mobileData.Order.Items)
                {
                    item.Additional.BarcodeScanRequiredForDelivery = barcodeScanningOnDelivery;
                    item.Additional.BypassCleanClausedScreen = bypassClausedScreen;
                    item.Additional.BypassCommentsScreen = bypassCommentScreen;
                }

                mobileData.Order.Additional.CustomerSignatureRequiredForDelivery = customerSignatureRequired;
                mobileData.Order.Additional.CustomerNameRequiredForDelivery = customerNameRequired;
            }

        }



        #endregion

    }

}
