using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class BarcodeStatusViewModelTests
        : MvxIoCSupportingTest
    {
        #region Setup

        private IFixture _fixture;
        private Mock<INavigationService> _mockNavigationService;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private BarcodeScanningViewModel _barcodeScanningViewModel;
        private List<DamageStatus> _damageStatuses;
        private Mock<IMvxMessenger> _mockMessenger;
        private MobileData _mobileData;
        private BarcodeItemViewModel _barcodeItemViewModel1;
        private BarcodeItemViewModel _barcodeItemViewModel2;
        private BarcodeItemViewModel _barcodeItemViewModel3;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _mobileData = _fixture.Create<MobileData>();
            _mobileData.Order.Type = InstructionType.Deliver;

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            _mockUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _fixture.Customize<BarcodeScanningViewModel>(vm => vm.Without(x => x.BarcodeSections));
            _fixture.Customize<BarcodeScanningViewModel>(vm => vm.Without(x => x.BarcodeInput));

            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);

            _damageStatuses = _fixture.CreateMany<DamageStatus>().ToList();
            _damageStatuses[0].Code = "POD";
            _damageStatuses[1].Code = "PODD";
        }

        private async Task SetupBarcodeScanningViewModel()
        {
            var navData = new NavData<MobileData>() { Data = _mobileData };
            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            _barcodeScanningViewModel = _fixture.Create<BarcodeScanningViewModel>();
            await _barcodeScanningViewModel.Init(navID);

            // mark all the barcode items as processed
            _barcodeScanningViewModel.MarkBarcodeAsProcessed(_barcodeScanningViewModel.BarcodeSections[0].Barcodes[0]);
            _barcodeScanningViewModel.MarkBarcodeAsProcessed(_barcodeScanningViewModel.BarcodeSections[0].Barcodes[0]);
            _barcodeScanningViewModel.MarkBarcodeAsProcessed(_barcodeScanningViewModel.BarcodeSections[0].Barcodes[0]);

            _barcodeItemViewModel1 = _barcodeScanningViewModel.BarcodeSections[1].Barcodes[0];
            _barcodeItemViewModel2 = _barcodeScanningViewModel.BarcodeSections[1].Barcodes[1];
            _barcodeItemViewModel3 = _barcodeScanningViewModel.BarcodeSections[1].Barcodes[2];
        }

        #endregion Setup

        #region Test

        [Fact]
        public void BarcodeStatusVM_FragmentTitle()
        {
            base.ClearAll();

            var barcodeStatusVM = _fixture.Create<BarcodeStatusViewModel>();

            Assert.Equal("Set Pallet Status", barcodeStatusVM.FragmentTitle);
        }

        [Fact]
        // checks that when initialised with a single barcode the pallet id of the view model is correct
        public async Task BarcodeStatusVM_SingleBarcode_PalletID()
        {
            base.ClearAll();

            await this.SetupBarcodeScanningViewModel();

            var navData = new NavData<BarcodeItemViewModel>() { Data = _barcodeItemViewModel1 };
            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<BarcodeItemViewModel>(navID)).Returns(navData);

            var barcodeStatusVM = _fixture.Create<BarcodeStatusViewModel>();
            barcodeStatusVM.Init(navID);

            Assert.Equal("Pallet ID", barcodeStatusVM.PalletIDLabel);
            Assert.Equal(_barcodeItemViewModel1.BarcodeText, barcodeStatusVM.PalletIDText);
        }

        [Fact]
        // checks that when initialised with multiple the pallet id of the view model is correct
        public async Task BarcodeStatusVM_MultipleBarcode_PalletID()
        {
            base.ClearAll();

            await this.SetupBarcodeScanningViewModel();

            var barcodeStatusVM = _fixture.Create<BarcodeStatusViewModel>();

            // add another two "selected" barcodes
            var navData = new NavData<BarcodeItemViewModel>() { Data = _barcodeItemViewModel1 };
            navData.OtherData["SelectedBarcodes"] = new List<BarcodeItemViewModel>() { _barcodeItemViewModel2, _barcodeItemViewModel3 };
            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<BarcodeItemViewModel>(navID)).Returns(navData);

            barcodeStatusVM.Init(navID);        

            Assert.Equal("Pallet IDs", barcodeStatusVM.PalletIDLabel);
            string expectedString = _barcodeItemViewModel1.BarcodeText + "\n" + _barcodeItemViewModel2.BarcodeText + "\n" + _barcodeItemViewModel3.BarcodeText;
            Assert.Equal(expectedString, barcodeStatusVM.PalletIDText);
        }

        [Fact]
        // checks that modifying barcode properties doesn't modify properties on the barcode passed in
        // (those changes should only take effect when the user presses "done")
        public async Task BarcodeStatusVM_ModifyBarcode()
        {
            base.ClearAll();

            await this.SetupBarcodeScanningViewModel();

            var barcodeStatusVM = _fixture.Create<BarcodeStatusViewModel>();

            _barcodeItemViewModel1.IsDelivered = true;
            var navData = new NavData<BarcodeItemViewModel>() { Data = _barcodeItemViewModel1 };
            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<BarcodeItemViewModel>(navID)).Returns(navData);

            barcodeStatusVM.Init(navID);

            var originalBarcode = _barcodeItemViewModel1.Clone();

            // change some properties
            barcodeStatusVM.Barcode.DeliveryComments = "Changed comments";
            barcodeStatusVM.Barcode.IsDelivered = !barcodeStatusVM.Barcode.IsDelivered;

            // original barcode shouldn't have changed
            Assert.Equal(originalBarcode.IsDelivered, _barcodeItemViewModel1.IsDelivered);
            Assert.Equal(originalBarcode.DeliveryComments, _barcodeItemViewModel1.DeliveryComments);
        }

        [Fact]
        // checks that when "done" is pressed changes made are set back on the original barcode
        public async Task BarcodeStatusVM_Done()
        {
            base.ClearAll();

            await this.SetupBarcodeScanningViewModel();

            var barcodeStatusVM = _fixture.Create<BarcodeStatusViewModel>();

            _barcodeItemViewModel1.IsDelivered = true;
            var navData = new NavData<BarcodeItemViewModel>() { Data = _barcodeItemViewModel1 };
            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<BarcodeItemViewModel>(navID)).Returns(navData);

            barcodeStatusVM.Init(navID);

            // change some properties
            barcodeStatusVM.Barcode.DeliveryComments = "Changed comments";
            barcodeStatusVM.Barcode.IsDelivered = !barcodeStatusVM.Barcode.IsDelivered;

            barcodeStatusVM.DoneCommand.Execute(null);

            // original barcode should have changed
            Assert.Equal(_barcodeItemViewModel1.IsDelivered, barcodeStatusVM.Barcode.IsDelivered);
            Assert.Equal(_barcodeItemViewModel1.DeliveryComments, barcodeStatusVM.Barcode.DeliveryComments);
        }

        [Fact]
        // checks that when "done" is pressed when multiple barcodes have been selected, changes made are set back on all those barcodes      
        public async Task BarcodeStatusVM_MultipleDone()
        {
            base.ClearAll();

            await this.SetupBarcodeScanningViewModel();

            var barcodeStatusVM = _fixture.Create<BarcodeStatusViewModel>();

            // add another two "selected" barcodes
            _barcodeItemViewModel2.IsSelected = true;
            _barcodeItemViewModel3.IsSelected = true;
            var navData = new NavData<BarcodeItemViewModel>() { Data = _barcodeItemViewModel1 };
            navData.OtherData["SelectedBarcodes"] = new List<BarcodeItemViewModel>() { _barcodeItemViewModel2, _barcodeItemViewModel3 };

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<BarcodeItemViewModel>(navID)).Returns(navData);

            barcodeStatusVM.Init(navID);        

            // change some properties
            barcodeStatusVM.Barcode.DeliveryComments = "Changed comments";
            barcodeStatusVM.Barcode.IsDelivered = !barcodeStatusVM.Barcode.IsDelivered;

            barcodeStatusVM.DoneCommand.Execute(null);

            // all barcodes should have changed
            Assert.Equal(_barcodeItemViewModel1.IsDelivered, barcodeStatusVM.Barcode.IsDelivered);
            Assert.Equal(_barcodeItemViewModel1.DeliveryComments, barcodeStatusVM.Barcode.DeliveryComments);

            Assert.Equal(_barcodeItemViewModel2.IsDelivered, barcodeStatusVM.Barcode.IsDelivered);
            Assert.Equal(_barcodeItemViewModel2.DeliveryComments, barcodeStatusVM.Barcode.DeliveryComments);

            Assert.Equal(_barcodeItemViewModel3.IsDelivered, barcodeStatusVM.Barcode.IsDelivered);
            Assert.Equal(_barcodeItemViewModel3.DeliveryComments, barcodeStatusVM.Barcode.DeliveryComments);

            //check that the "isSelected" flags have been cleared
            Assert.False(_barcodeItemViewModel2.IsSelected);
            Assert.False(_barcodeItemViewModel3.IsSelected);
        }

        #endregion Test

    }

}
