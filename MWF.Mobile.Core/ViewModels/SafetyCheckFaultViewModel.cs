using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.Plugins.PictureChooser;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;


namespace MWF.Mobile.Core.ViewModels
{
    public class SafetyCheckFaultViewModel : BaseModalViewModel<bool>, IBackButtonHandler
    {

        #region Private Members

        private MvxCommand _doneCommand;
        private MvxCommand _takePictureCommand;
        private IInfoService _infoService;
        private readonly IMvxPictureChooserTask _pictureChooserTask;
        private SafetyCheckFault _safetyCheckFault;             // working copy of safety check fault for duration of this screen
        private SafetyCheckFault _originalSafetyCheckFault;     // original copy of safety check fault we'll write to when "done" is clicked
        private string _faultTypeText;
        ObservableCollection<SafetyCheckFaultImageViewModel> _images;

        #endregion

        #region Construction

        public SafetyCheckFaultViewModel(IInfoService infoService, IMvxPictureChooserTask pictureChooserTask)
        {
            _infoService = infoService;
            _pictureChooserTask = pictureChooserTask;
        }

        public void Init(Guid navID)
        {
            var navData = Mvx.Resolve<INavigationService>().GetNavData<SafetyCheckFault>(navID);

            SetMessageID(navID);

            _originalSafetyCheckFault = navData.Data;

            _faultTypeText = (string) navData.OtherData["FaultTypeText"];

            _safetyCheckFault = _originalSafetyCheckFault.Clone();

            PopulateImageList();
        }

        #endregion

        #region Public Properties

        public override string FragmentTitle
        {
            get { return "Log Fault"; }
        }

        public string DoneButtonLabel
        {
            get { return "Done"; }
        }

        public string TakePictureButtonLabel
        {
            get { return "Take Picture"; }
        }

        public string CheckTypeText
        {
            get { return _safetyCheckFault.Title;  }
        }

        public string CommentHintText
        {
            get { return "Type Comment"; }
        }

        public string CommentText
        {
            get { return _safetyCheckFault.Comment; }
            set 
            {
                _safetyCheckFault.Comment = value;
                RaisePropertyChanged(() => CommentText);
                RaisePropertyChanged(() => HasCommentText);
            }
        }

        public string InstructionsText
        {
            get { return "Add comment and images"; }
        }

        public bool HasCommentText
        {
            get { return !string.IsNullOrEmpty(_safetyCheckFault.Comment); }
        }

        public string DiscretionaryOrFailureText
        {
            get { return _faultTypeText; }
        }

        public System.Windows.Input.ICommand DoneCommand
        {
            get { return (_doneCommand = _doneCommand ?? new MvxCommand(() => DoDoneCommand())); }
        }

        public System.Windows.Input.ICommand TakePictureCommand
        {
            get { return (_takePictureCommand = _takePictureCommand ?? new MvxCommand(() => TakePicture() )); }
        }

        public ObservableCollection<SafetyCheckFaultImageViewModel> Images
        {
            get { return _images; }
            private set { _images = value; RaisePropertyChanged(() => Images); }
        }

        #endregion

        #region Private Methods

        private void DoDoneCommand()
        {
            _originalSafetyCheckFault.ValuesFrom(_safetyCheckFault);
            ReturnResult(true);
        }

        private void TakePicture()
        {
            // note use "TakePicture" for release
            // ChoosePictureFromLibrary should only be used when debugging with an emulator
            //_pictureChooserTask.ChoosePictureFromLibrary(400, 95, OnPictureTaken, () => { });
            _pictureChooserTask.TakePicture(400, 95, OnPictureTaken, () => { });

        }


        /// <summary>
        /// Packs up the image stre
        /// </summary>
        /// <param name="stream"></param>
        private void OnPictureTaken(Stream stream)
        {

            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            int sequenceNumber = (Images.Any()) ? _images.Max(i => i.FaultImage.Sequence) + 1 : 1;
            Image image = new Image() { ID = Guid.NewGuid(), SafetyCheckFaultID = _safetyCheckFault.ID, Sequence = sequenceNumber, Bytes = memoryStream.ToArray() };


            //Add to data model
            _safetyCheckFault.Images.Add(image);

            //Add to view model
            SafetyCheckFaultImageViewModel imageViewModel = new SafetyCheckFaultImageViewModel(image, this);
            Images.Add(imageViewModel);

        }


        /// <summary>
        /// Populates the image view models based on the images in the safety check fault data model
        /// </summary>
        private void PopulateImageList()
        {
            ObservableCollection<SafetyCheckFaultImageViewModel> images = new ObservableCollection<SafetyCheckFaultImageViewModel>();

            foreach (var image in _safetyCheckFault.Images)
            {
                images.Add(new SafetyCheckFaultImageViewModel(image, this));
            }

            Images = images;
        }

        /// <summary>
        /// Deletes an image from both the the view models and datamodels
        /// </summary>
        /// <param name="image"></param>
        internal void Delete(SafetyCheckFaultImageViewModel image)
        {
            // remove from backing data model (exists in startup service)
            _safetyCheckFault.Images.Remove(image.FaultImage);

            // remove view model
            Images.Remove(image);
        }

        #endregion

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            bool continueWithBackPress = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("The changes you have made will be lost, do you wish to continue?", "Changes will be lost!", "Continue");

            // since we are modal, we need to let the calling viewmodel know that we cancelled (it will handle the back press)
            if (continueWithBackPress)
            {
                this.Cancel();
            }

            return false;
        } 

        #endregion

    }

}
