using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Portable;

namespace MWF.Mobile.Core.ViewModels
{

    public class CameraImageViewModel : MvxViewModel
    {

        #region Private Members

        private Image _image;
        private MvxCommand _deleteCommand;
        private MvxCommand _displayCommand;
        private BaseCameraViewModel _parentCameraViewModel;

        #endregion

        #region Construction

        public CameraImageViewModel(Image image, BaseCameraViewModel camera)
        {
            _image = image;
            _parentCameraViewModel = camera;
        }

        #endregion

        #region public properties

        public byte[] Bytes
        {
            get { return _image.Bytes; }
        }

        public Image Image
        {
            get { return _image; }
            set 
            { 
                _image = value;
                RaisePropertyChanged(() => Bytes);
            }
        }


        public System.Windows.Input.ICommand DisplayCommand
        {
            get { return (_displayCommand = _displayCommand ?? new MvxCommand(Display)); }
        }

        
        public System.Windows.Input.ICommand DeleteCommand
        {
            get { return (_deleteCommand = _deleteCommand ?? new MvxCommand(async () => await this.DeleteAsync())); }
        }


        #endregion

        #region Private Methods

        public async Task DeleteAsync()
        {
            if (await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("Are you sure you want to delete this picture?", "Delete Picture", "Delete", "Cancel"))
                _parentCameraViewModel.Delete(this);
        }

        private void Display()
        {
            Mvx.Resolve<ICustomUserInteraction>().PopUpImage(_image.Bytes, null, null, null, "Close");
        }

        #endregion

    }
}
