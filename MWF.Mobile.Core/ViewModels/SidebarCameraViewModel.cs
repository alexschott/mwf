using Cirrious.MvvmCross.Plugins.PictureChooser;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class SidebarCameraViewModel
        : BaseCameraViewModel,
        IBackButtonHandler
    {

        public SidebarCameraViewModel(
            IMvxPictureChooserTask pictureChooserTask, 
            IInfoService infoService, 
            INavigationService navigationService,
            IImageUploadService imageUploadService)
            : base(pictureChooserTask, infoService, navigationService, imageUploadService)
        {

        }

        public override async Task DoDoneCommandAsync()
        {
            List<Image> images = new List<Image>();

            foreach (var viewModel in ImagesVM)
            {
                images.Add(viewModel.Image);
            }

            await _navigationService.MoveToNextAsync(_navigationService.CurrentNavData);

            IEnumerable<MobileData> mobileDatas = null;
            if (_navigationService.CurrentNavData != null && _navigationService.CurrentNavData is NavData<MobileData>)
            {
                mobileDatas = (_navigationService.CurrentNavData as NavData<MobileData>).GetAllInstructions();
            }

            await _imageUploadService.SendPhotoAndCommentAsync(CommentText, images, _infoService.CurrentDriverID.Value, _infoService.CurrentDriverDisplayName, mobileDatas);
        }

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            await _navigationService.GoBackAsync(_navigationService.CurrentNavData);
            return false;
        }

        #endregion IBackButtonHandler Implementation
    }
}
