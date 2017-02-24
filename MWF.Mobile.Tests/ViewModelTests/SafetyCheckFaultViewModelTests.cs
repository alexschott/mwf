using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.Plugins.PictureChooser;
using Cirrious.MvvmCross.Test.Core;
using Moq;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Tests.Helpers;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace MWF.Mobile.Tests.ViewModelTests
{
    
    public class SafetyCheckFaultViewModelTests
        : MvxIoCSupportingTest
    {

        private IFixture _fixture;
        private IInfoService _infoService;
        private Mock<INavigationService> _mockNavigationService;
        private SafetyCheckFault _vehicleSafetyCheckFault;
        private SafetyCheckFault _trailerSafetyCheckFault;
        private Mock<IMvxPictureChooserTask> _pictureChooserMock;
        private Mock<ICustomUserInteraction> _mockUserInteraction;
        private Mock<IMvxMessenger> _mockMessenger;
        private byte[] _pictureBytes;

        protected override void AdditionalSetup()
        {

            _mockUserInteraction = new Mock<ICustomUserInteraction>();
            Ioc.RegisterSingleton<ICustomUserInteraction>(_mockUserInteraction.Object);

            _mockMessenger = new Mock<IMvxMessenger>();
            Ioc.RegisterSingleton<IMvxMessenger>(_mockMessenger.Object);

            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _infoService = _fixture.Create<InfoService>();
            _fixture.Inject<IInfoService>(_infoService);


            _pictureBytes = new byte[] {1, 2, 3, 4};
            _pictureChooserMock = new Mock<IMvxPictureChooserTask>();
            _pictureChooserMock.Setup(pc => pc.TakePicture(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Action<Stream>>(), It.IsAny<Action>())).
                                Callback<int, int, Action<Stream>, Action>((s1, s2, a1, a2) => { a1.Invoke(new MemoryStream(_pictureBytes)); });
            _fixture.Inject<IMvxPictureChooserTask>(_pictureChooserMock.Object);


            _vehicleSafetyCheckFault = _fixture.Create<SafetyCheckFault>();

            _trailerSafetyCheckFault = _fixture.Create<SafetyCheckFault>();

            _mockNavigationService = _fixture.InjectNewMock<INavigationService>();
            Ioc.RegisterSingleton<INavigationService>(_mockNavigationService.Object);

        }

        // Checks that after initialization the "DiscretionaryOrFailureText" property is set correctly
        // i.e. to the FaultTypeText it was initialized with
        [Fact]
        public void SafetyCheckFaultVM_Init_DiscretionaryOrFailureText()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            Assert.Equal(navItem.OtherData["FaultTypeText"], safetyCheckFaultVM.DiscretionaryOrFailureText);

        }

        // Checks that after initialization the "CheckTypeText" property is set correctly
        // i.e. to the title of the SafetyCheckFault data model
        [Fact]
        public void SafetyCheckFaultVM_Init_CheckTypeText()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            Assert.Equal(_vehicleSafetyCheckFault.Title, safetyCheckFaultVM.CheckTypeText);
        }

        // Checks that after initialization the "CommentText" property is set correctly
        // i.e. to the comment property of the SafetyCheckFault data model
        [Fact]
        public void SafetyCheckFaultVM_Init_CommentText()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            Assert.Equal(_vehicleSafetyCheckFault.Comment, safetyCheckFaultVM.CommentText);
        }

        // Checks that after initialization the "HasCommentText" property is false
        // if the Comment field is empty
        [Fact]
        public void SafetyCheckFaultVM_Init_HasCommentText_CommentEmpty()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text"; 

            _vehicleSafetyCheckFault.Comment = string.Empty;

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            Assert.False(safetyCheckFaultVM.HasCommentText);
        }

        // Checks that after initialization the "HasCommentText" property is true
        // if the Comment field has a value
        [Fact]
        public void SafetyCheckFaultVM_Init_HasCommentText()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            Assert.True(safetyCheckFaultVM.HasCommentText);
        }

        // Checks that after initialization the image list is populated with SafetyCheckFaultImageViewModels built up from the images
        // in the fault datamodel
        [Fact]
        public void SafetyCheckFaultVM_Init_ImagesPopulated()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            Assert.Equal(_vehicleSafetyCheckFault.Images.ToList().Count, safetyCheckFaultVM.Images.Count);

            for (int i = 0; i < safetyCheckFaultVM.Images.Count; i++)
            {
                Assert.Same(_vehicleSafetyCheckFault.Images.ToList()[i], safetyCheckFaultVM.Images[i].FaultImage);
            }

        }


        // Checks that after initialization All the properties in the above tests are correct, but for
        // the case when a fault is for a trailer
        [Fact]
        public void SafetyCheckFaultVM_Init_Trailer()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _trailerSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            Assert.Equal(navItem.OtherData["FaultTypeText"], safetyCheckFaultVM.DiscretionaryOrFailureText);
            Assert.Equal(_trailerSafetyCheckFault.Title, safetyCheckFaultVM.CheckTypeText);
            Assert.Equal(_trailerSafetyCheckFault.Comment, safetyCheckFaultVM.CommentText);
            Assert.Equal(_trailerSafetyCheckFault.Images.ToList().Count, safetyCheckFaultVM.Images.Count);

        }

        // Checks that when the safety check fault comment is modified and the done command is fired
        // the comment change has been made on the underlying safety check fault data model
        [Fact]
        public void SafetyCheckFaultVM_CommentTextChanged()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            // Change the comment text
            safetyCheckFaultVM.CommentText = "Some new text";

            // Invoke the done command
            safetyCheckFaultVM.DoneCommand.Execute(null);

            Assert.Equal(safetyCheckFaultVM.CommentText, _vehicleSafetyCheckFault.Comment);
        }

        // Checks that when the TakePicture command is executed a new image view model appers in the images list
        [Fact]
        public void SafetyCheckFaultVM_TakePicture()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            int previousImageCount = safetyCheckFaultVM.Images.Count;
            int previousSequence = safetyCheckFaultVM.Images.Max(i => i.FaultImage.Sequence);

            // Take the picture
            safetyCheckFaultVM.TakePictureCommand.Execute(null);

            // should have an extra image in view model model
            Assert.Equal(previousImageCount + 1, safetyCheckFaultVM.Images.Count);

            // data model should still have previous number of images at this point (we haven't pressed "done" yet)
            Assert.Equal(previousImageCount, _vehicleSafetyCheckFault.Images.Count);

            // new image bytes should be the same as the one supplied by the mock
            Assert.Equal(_pictureBytes.Length, safetyCheckFaultVM.Images[previousImageCount].Bytes.Length);

            // Invoke the done command
            safetyCheckFaultVM.DoneCommand.Execute(null);

            // should have an extra image in data model
            Assert.Equal(previousImageCount + 1, _vehicleSafetyCheckFault.Images.Count);

            // sequence number should be an in increment of previous highest sequence
            Assert.Equal(previousSequence + 1, _vehicleSafetyCheckFault.Images[previousImageCount].Sequence);

            // id should point back to parent safety fault
            Assert.Equal(_vehicleSafetyCheckFault.ID , _vehicleSafetyCheckFault.Images[previousImageCount].SafetyCheckFaultID);

            // new image bytes should be the same as the one supplied by the mock
            Assert.Equal(_pictureBytes.Length, _vehicleSafetyCheckFault.Images[previousImageCount].Bytes.Length);
        }

        // Checks that when the back button is pressed a dialog is shown, and if the users oks it then the view model fires a result message
        // back to the messenger service
        [Fact]
        public async Task SafetyCheckFaultVM_BackButton_OK()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _trailerSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            _mockUserInteraction.Setup(ui => ui.ConfirmAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>())).Returns( Task.FromResult<bool>(true));
           
            await safetyCheckFaultVM.OnBackButtonPressedAsync();

            // since user pressed "ok", the back button stack proceeds and (as a modal view model) the view model should have
            // published a "false" result
            _mockMessenger.Verify(mm => mm.Publish(It.Is<ModalNavigationResultMessage<bool>>(msg => msg.Result == false )), Times.Once);
            
        }

        // Checks that when the back button is pressed a dialog is shown, and if the users cancels it then view model doesn't fire a result message
        // back to the messenger service
        [Fact]
        public async Task SafetyCheckFaultVM_BackButton_Cancel()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _trailerSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            _mockUserInteraction.Setup(ui => ui.ConfirmAsync(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>())).ReturnsAsync(false);

            await safetyCheckFaultVM.OnBackButtonPressedAsync();

            // since user pressed "ok", the back button stack proceeds and (as a modal view model) the view model should have
            // published a "false" result
            _mockMessenger.Verify(mm => mm.Publish(It.IsAny<ModalNavigationResultMessage<bool>>()), Times.Never);

        }

        // Checks that when the Done Command is exectuted the view model fires a result message
        // back to the messenger service
        [Fact]
        public void SafetyCheckFaultVM_Done()
        {

            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _trailerSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            safetyCheckFaultVM.DoneCommand.Execute(null);

            _mockMessenger.Verify(mm => mm.Publish(It.Is<ModalNavigationResultMessage<bool>>(msg => msg.Result == true)), Times.Once);

        }

        // Checks that when the delete command is executed on a fault image the image is deleted from the view model (initially) and the data
        // model (when done has been pressed)
        [Fact]
        public async Task SafetyCheckFaultImageVM_Delete_OK()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            int previousImageCount = safetyCheckFaultVM.Images.Count;

            // dialog returns true
            _mockUserInteraction.ConfirmAsyncReturnsTrueIfTitleStartsWith("Delete Picture");

            //delete the last image
            await safetyCheckFaultVM.Images[previousImageCount - 1].DeleteAsync();

            // should have one less image in view model 
            Assert.Equal(previousImageCount - 1, safetyCheckFaultVM.Images.Count);

            // data model should still have previous number of images at this point (we haven't pressed "done" yet)
            Assert.Equal(previousImageCount, _vehicleSafetyCheckFault.Images.Count);

            // Invoke the done command
            safetyCheckFaultVM.DoneCommand.Execute(null);

            // Data model should now have one image less
            Assert.Equal(previousImageCount - 1, _vehicleSafetyCheckFault.Images.Count);

        }

        [Fact]
        // Checks that when the user cancels out of an image deletion nothing is actually deleted
        public async Task SafetyCheckFaultImageVM_Delete_Cancel()
        {
            base.ClearAll();

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            int previousImageCount = safetyCheckFaultVM.Images.Count;

            // dialog returns false
            _mockUserInteraction.ConfirmReturnsFalse();

            // attempt to delete the last image
            await safetyCheckFaultVM.Images[previousImageCount - 1].DeleteAsync();

            // nothing should have been deleted since we cancelled out of the deletion
            Assert.Equal(previousImageCount, safetyCheckFaultVM.Images.Count);
            Assert.Equal(previousImageCount, _vehicleSafetyCheckFault.Images.Count);

        }

        [Fact]
        // Checks that when the Display command is executed on an image the image is displayed in a popup image
        public void SafetyCheckFaultImageVM_DisplayImage()
        {
            base.ClearAll();

            var customUI = new Mock<ICustomUserInteraction>();
            Ioc.RegisterSingleton<ICustomUserInteraction>(customUI.Object);

            var safetyCheckFaultVM = _fixture.Build<SafetyCheckFaultViewModel>().Without(p => p.CommentText).Create<SafetyCheckFaultViewModel>();

            NavData<SafetyCheckFault> navItem = new NavData<SafetyCheckFault>() { Data = _vehicleSafetyCheckFault };
            navItem.OtherData["FaultTypeText"] = "Test Text";

            var navID = Guid.NewGuid();
            _mockNavigationService.Setup(ns => ns.GetNavData<SafetyCheckFault>(navID)).Returns(navItem);

            safetyCheckFaultVM.Init(navID);

            safetyCheckFaultVM.Images[0].DisplayCommand.Execute(null);

            customUI.Verify(cui => cui.PopUpImage(It.Is<byte[]>(ba => ba[0] ==  safetyCheckFaultVM.Images[0].Bytes[0]),
                                                  It.Is<string>(s=> String.IsNullOrEmpty(s)),
                                                  It.IsAny<Action>(),
                                                  It.IsAny<string>(),
                                                  It.Is<string>(s => s=="Close")));



        }



    }

}
