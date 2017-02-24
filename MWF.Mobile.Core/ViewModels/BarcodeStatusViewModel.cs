using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;


namespace MWF.Mobile.Core.ViewModels
{
    public class BarcodeStatusViewModel : BaseModalViewModel<bool>, IBackButtonHandler
    {

        #region Private Members

        private MvxCommand _doneCommand;
        private BarcodeItemViewModel _originalBarcodeItemViewModel;
        private BarcodeItemViewModel _barcodeItemViewModel;
        private NavData<BarcodeItemViewModel> _navData;

        #endregion

        #region Construction

        public BarcodeStatusViewModel()
        {

        }

        public void Init(Guid navID)
        {
            SetMessageID(navID);

            _navData = Mvx.Resolve<Services.INavigationService>().GetNavData<BarcodeItemViewModel>(navID);

            // take a copy of the barcode item view model
            // we only want to effect the changes if the user presses "done"
            _originalBarcodeItemViewModel = _navData.Data;
            _barcodeItemViewModel = _originalBarcodeItemViewModel.Clone();
        }

        #endregion

        #region Public Properties


        public override string FragmentTitle
        {
            get { return "Set Pallet Status"; }
        }

        public string InstructionsText
        {
            get { return "Set delivery and damage status and enter any delivery notes."; }
        }


        public string CommentHintText
        {
            get { return "Enter comments."; }
        }

        public string DoneButtonLabel
        {
            get { return "Done"; }
        }

        public string PalletIDLabel
        {
            get
            {
                return (AreMultipleBarcodes) ? "Pallet IDs" : "Pallet ID";
            }
        }

        public string PalletIDText
        {
            get
            {
                if (!_navData.OtherData.IsDefined("SelectedBarcodes")) return Barcode.BarcodeText;
                
                List<BarcodeItemViewModel> selectedBarcodes = _navData.OtherData["SelectedBarcodes"] as List<BarcodeItemViewModel>;

                List<BarcodeItemViewModel> selectedBarcodesPlusThisBarcode = new List<BarcodeItemViewModel>(selectedBarcodes);
                if (!selectedBarcodesPlusThisBarcode.Contains(_originalBarcodeItemViewModel))
                    selectedBarcodesPlusThisBarcode.Insert(0, this.Barcode);


                var barcodes = selectedBarcodesPlusThisBarcode.Select(x => x.BarcodeText);
                return barcodes.Aggregate((i, j) => i + "\n" + j);
            }
        }

        public string DeliveryStatusLabel
        {
            get
            {
                return "Delivery Status";
            }
        }

        public string DamageStatusLabel
        {
            get
            {
                return "Damage Status";
            }
        }

        public string DeliveryCommentsLabel
        {
            get
            {
                return "Delivery Comments";
            }
        }



        public bool AreMultipleBarcodes
        {
            get
            {
                if (!_navData.OtherData.IsDefined("SelectedBarcodes")) return false;
                
                List<BarcodeItemViewModel> barcodes = _navData.OtherData["SelectedBarcodes"] as List<BarcodeItemViewModel>;

                if (barcodes.Count > 1) return true;

                if (barcodes.Count == 1 && barcodes[0].BarcodeText != Barcode.BarcodeText) return true;

                return false;
            }
        }


        public BarcodeItemViewModel Barcode
        {
            get
            {
                return _barcodeItemViewModel;
            }
        }


        public System.Windows.Input.ICommand DoneCommand
        {
            get { return (_doneCommand = _doneCommand ?? new MvxCommand(() => this.DoDoneCommand())); }
        }

        public bool UserChangesDetected
        {
            get
            {
                return (_originalBarcodeItemViewModel.IsDelivered != _barcodeItemViewModel.IsDelivered ||
                        _originalBarcodeItemViewModel.DamageStatus != _barcodeItemViewModel.DamageStatus ||
                        _originalBarcodeItemViewModel.DeliveryComments != _barcodeItemViewModel.DeliveryComments);

            }
        }

        #endregion

        #region Private Methods

        private void DoDoneCommand()
        {
            // effect the changes onto the original view model
             SetUpdatedBarcodeData(_originalBarcodeItemViewModel);

            if (_navData.OtherData.IsDefined("SelectedBarcodes"))
            {
                List<BarcodeItemViewModel> barcodes = _navData.OtherData["SelectedBarcodes"] as List<BarcodeItemViewModel>;
                foreach (var barcode in barcodes)
                {
                     SetUpdatedBarcodeData(barcode);
                }
            }

            ReturnResult(true);
        }

        private void SetUpdatedBarcodeData(BarcodeItemViewModel vm)
        {
            vm.IsDelivered = _barcodeItemViewModel.IsDelivered;
            vm.DamageStatus = _barcodeItemViewModel.DamageStatus;
            vm.DeliveryComments = _barcodeItemViewModel.DeliveryComments;

            // clear the selected flag
            vm.IsSelected = false;
        }

        #endregion

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            bool continueWithBackPress = true;

            if (UserChangesDetected)
            {
                continueWithBackPress = await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("The changes you have made will be lost, do you wish to continue?", "Changes will be lost!", "Continue");
            }

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
