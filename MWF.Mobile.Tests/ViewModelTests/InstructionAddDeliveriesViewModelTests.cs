using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.Models;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Tests.Helpers;
using Xunit;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.Portable;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using MWF.Mobile.Core.Messages;

namespace MWF.Mobile.Tests.ViewModelTests
{
    public class InstructionAddDeliveriesViewModelTests
        : MvxIoCSupportingTest
    {

        #region Setup

        private IFixture _fixture;
        private Mock<INavigationService> _navigationService; 
        private Mock<IMobileDataRepository> _mockMobileDataRepo;
        private IInfoService _infoService;
        private Mock<ICustomUserInteraction> _mockCustomUserInteraction;
        private Mock<IMvxMessenger> _mockMessenger;
        private Mock<IApplicationProfileRepository> _applicationProfileRepoMock;
        private ApplicationProfile _applicationProfile;

        protected override void AdditionalSetup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _fixture.Customize<MobileData>(md => md.With(x => x.EffectiveDate, DateTime.Now));

            _applicationProfile = _fixture.Create<ApplicationProfile>();
            _applicationProfile.DisplaySpan = 3;
            _applicationProfile.DisplayRetention = 3;

            _applicationProfileRepoMock = _fixture.InjectNewMock<IApplicationProfileRepository>();
            _applicationProfileRepoMock.Setup(aprm => aprm.GetAllAsync()).ReturnsAsync(new List<ApplicationProfile>() { _applicationProfile });

            _mockMobileDataRepo = _fixture.InjectNewMock<IMobileDataRepository>();

            _fixture.Inject<IRepositories>(_fixture.Create<Repositories>());

            _navigationService = _fixture.InjectNewMock<INavigationService>();

            _infoService = _fixture.Create<InfoService>();
            _fixture.Inject<IInfoService>(_infoService);

            _mockCustomUserInteraction = Ioc.RegisterNewMock<ICustomUserInteraction>();

            _mockMessenger = Ioc.RegisterNewMock<IMvxMessenger>();
            _mockMessenger.Setup(m => m.Unsubscribe<GatewayInstructionNotificationMessage>(It.IsAny<MvxSubscriptionToken>()));
            _mockMessenger.Setup(m => m.Subscribe(It.IsAny<Action<GatewayInstructionNotificationMessage>>(), It.IsAny<MvxReference>(), It.IsAny<string>())).Returns(_fixture.Create<MvxSubscriptionToken>());

            Ioc.RegisterSingleton<INavigationService>(_navigationService.Object);

        }

        #endregion Setup

        #region Test

        [Fact]
        public void InstructionAddDeliveriesVM_FragmentTitle()
        {
            base.ClearAll();

            var addDeliveriesVM = _fixture.Create<InstructionAddDeliveriesViewModel>();

            Assert.Equal("Add/Remove Deliveries", addDeliveriesVM.FragmentTitle);
        }

        [Fact]
        // should be no instructions
        public async Task InstructionAddDeliveriesVM_BuildDeliveriesList_CollectionsOnly()
        {
            base.ClearAll();
            
            _fixture.Customize<Order>(md => md.With(x => x.Type, Core.Enums.InstructionType.Collect));

            var nonCompletedCollectionInstructions = _fixture.CreateMany<MobileData>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetNonCompletedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(nonCompletedCollectionInstructions);

            var addDeliveriesVM = _fixture.Create<InstructionAddDeliveriesViewModel>();
            NavData<MobileData> navData = new NavData<MobileData>() { Data = nonCompletedCollectionInstructions.First() };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            await addDeliveriesVM.Init(navID);

            Assert.Equal(0, addDeliveriesVM.DeliveryInstructions.Count);
        }

        [Fact]
        public async Task InstructionAddDeliveriesVM_BuildDeliveriesList_Deliveries()
        {
            base.ClearAll();

            _fixture.Customize<Order>(md => md.With(x => x.Type, Core.Enums.InstructionType.Deliver));
            _fixture.Customize<ItemAdditional>(md => md.With(x => x.BarcodeScanRequiredForDelivery, true));

            var nonCompletedDeliveryInstructions = _fixture.CreateMany<MobileData>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetNonCompletedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(nonCompletedDeliveryInstructions);

            var addDeliveriesVM = _fixture.Create<InstructionAddDeliveriesViewModel>();
            NavData<MobileData> navData = new NavData<MobileData>() { Data = nonCompletedDeliveryInstructions.First() };
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            await addDeliveriesVM.Init(navID);

            // should be one less than the total delivery instructions (the vm's own mobile data should not be counted)
            // i.e. you can't a delivery to itself
            Assert.Equal(nonCompletedDeliveryInstructions.Count() - 1, addDeliveriesVM.DeliveryInstructions.Count);

            foreach (var instruction in addDeliveriesVM.DeliveryInstructions)
            {
                Assert.NotEqual(navData.Data, instruction.MobileData);
                Assert.Contains(instruction.MobileData, nonCompletedDeliveryInstructions);
                Assert.False(instruction.IsSelected);
            }

        }

        [Fact]
        public async Task InstructionAddDeliveriesVM_BuildDeliveriesList_Deliveries_DifferentBarcodeScanningOptions()
        {
            base.ClearAll();

            _fixture.Customize<Order>(md => md.With(x => x.Type, Core.Enums.InstructionType.Deliver));
            _fixture.Customize<ItemAdditional>(md => md.With(x => x.BarcodeScanRequiredForDelivery, true));

            var nonCompletedDeliveryInstructions = _fixture.CreateMany<MobileData>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetNonCompletedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(nonCompletedDeliveryInstructions);

            var addDeliveriesVM = _fixture.Create<InstructionAddDeliveriesViewModel>();
            NavData<MobileData> navData = new NavData<MobileData>() { Data = nonCompletedDeliveryInstructions.First() };

            //set vm's own mobile data to not use barcode scanning
            navData.Data.Order.Items.FirstOrDefault().Additional.BarcodeScanRequiredForDelivery = false;
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            await addDeliveriesVM.Init(navID);

            // should see no matching delivery instructions
            Assert.Equal(0, addDeliveriesVM.DeliveryInstructions.Count);
        }


        [Fact]
        public async Task InstructionAddDeliveriesVM_BuildDeliveriesList_IsSelectedPopulated()
        {
            base.ClearAll();

            _fixture.Customize<Order>(md => md.With(x => x.Type, Core.Enums.InstructionType.Deliver));
            _fixture.Customize<ItemAdditional>(md => md.With(x => x.BarcodeScanRequiredForDelivery, true));

            var nonCompletedDeliveryInstructions = _fixture.CreateMany<MobileData>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetNonCompletedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(nonCompletedDeliveryInstructions);

            var addDeliveriesVM = _fixture.Create<InstructionAddDeliveriesViewModel>();
            NavData<MobileData> navData = new NavData<MobileData>() { Data = nonCompletedDeliveryInstructions.First() };

            // set the instruction to already have one "additional instruction"
            var additionalInstruction = nonCompletedDeliveryInstructions.Last();
            navData.GetAdditionalInstructions().Add(additionalInstruction);
            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            await addDeliveriesVM.Init(navID);

            // the delivery that lives is already added as an additional delivery should be selected
            Assert.True(addDeliveriesVM.DeliveryInstructions.Single(x => x.MobileData == additionalInstruction).IsSelected);

        }

        [Fact]
        public async Task InstructionAddDeliveriesVM_BuildDeliveriesList_Done_AddDelivery()
        {
            base.ClearAll();

            _fixture.Customize<Order>(md => md.With(x => x.Type, Core.Enums.InstructionType.Deliver));
            _fixture.Customize<ItemAdditional>(md => md.With(x => x.BarcodeScanRequiredForDelivery, true));

            var nonCompletedDeliveryInstructions = _fixture.CreateMany<MobileData>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetNonCompletedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(nonCompletedDeliveryInstructions);

            var addDeliveriesVM = _fixture.Create<InstructionAddDeliveriesViewModel>();
            NavData<MobileData> navData = new NavData<MobileData>() { Data = nonCompletedDeliveryInstructions.First() };

            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            await addDeliveriesVM.Init(navID);

            // select one of the deliveries
            var selectedDelivery = addDeliveriesVM.DeliveryInstructions.Last();
            selectedDelivery.IsSelected = true;

            // now excute the done command
            addDeliveriesVM.DoneCommand.Execute(null);

            // check that the selected item has been added to the additional deliveries
            Assert.Equal(1, navData.GetAdditionalInstructions().Count);
            Assert.Equal(selectedDelivery.MobileData, navData.GetAdditionalInstructions().First());

            // since user pressed "done" the modal should have returned with true
            _mockMessenger.Verify(mm => mm.Publish(It.Is<ModalNavigationResultMessage<bool>>(msg => msg.Result == true)), Times.Once);
        }

        [Fact]
        public void InstructionAddDeliveriesVM_Done_RemoveDelivery()
        {
            base.ClearAll();

            _fixture.Customize<Order>(md => md.With(x => x.Type, Core.Enums.InstructionType.Deliver));
            _fixture.Customize<ItemAdditional>(md => md.With(x => x.BarcodeScanRequiredForDelivery, true));

            var nonCompletedDeliveryInstructions = _fixture.CreateMany<MobileData>();
            _mockMobileDataRepo.Setup(mdr => mdr.GetNonCompletedInstructionsAsync(It.IsAny<Guid>())).ReturnsAsync(nonCompletedDeliveryInstructions);

            var addDeliveriesVM = _fixture.Create<InstructionAddDeliveriesViewModel>();
            NavData<MobileData> navData = new NavData<MobileData>() { Data = nonCompletedDeliveryInstructions.First() };

            // set the instruction to already have one "additional instruction"
            var additionalInstruction = nonCompletedDeliveryInstructions.Last();
            navData.GetAdditionalInstructions().Add(additionalInstruction);

            var navID = Guid.NewGuid();
            _navigationService.Setup(ns => ns.GetNavData<MobileData>(navID)).Returns(navData);

            addDeliveriesVM.Init(navID);

            // make sure all the items are unselected
            foreach (var delivery in addDeliveriesVM.DeliveryInstructions)
            {
                delivery.IsSelected = false;
            }

            // now excute the done command
            addDeliveriesVM.DoneCommand.Execute(null);

            // check that the selected item has been removed from the additional deliveries
            Assert.Equal(0, navData.GetAdditionalInstructions().Count);

            // since user pressed "done" the modal should have returned with true
            _mockMessenger.Verify(mm => mm.Publish(It.Is<ModalNavigationResultMessage<bool>>(msg => msg.Result == true)), Times.Once);
        }

        #endregion Test

    }
}
