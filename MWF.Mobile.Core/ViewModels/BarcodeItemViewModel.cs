using System;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.Portable;
using System.Windows.Input;

namespace MWF.Mobile.Core.ViewModels
{
    public class BarcodeItemViewModel : BaseFragmentViewModel
    {

        private const string NOT_DELIVERED_CODE = "XPODX";

        #region private members

        private BarcodeScanningViewModel _barcodeScanningViewModel;
        private INavigationService _navigationService;
        private List<DamageStatus> _damageStatuses;
        private DamageStatus _damageStatus;
        private string _deliveryComments;

        #endregion

        #region construction

        public BarcodeItemViewModel(INavigationService navigationService, List<DamageStatus> damageStatuses, BarcodeScanningViewModel barcodeScanningViewModel)
        {
            _navigationService = navigationService;
            _barcodeScanningViewModel = barcodeScanningViewModel;

            this.DamageStatuses = damageStatuses;

            if (damageStatuses != null)
                this.DamageStatus = damageStatuses[0];
        }

        #endregion

        #region public properties

        private string _barcodeText;
        public string BarcodeText
        {
            get { return _barcodeText; }
            set { _barcodeText = value; RaisePropertyChanged(() => BarcodeText); }
        }

        private bool _isScanned;
        public bool IsScanned
        {
            get { return _isScanned; }
            set 
            {
                _isScanned = value; 
                RaisePropertyChanged(() => IsScanned);
            }
        }

        private bool? _isDelivered = null;
        public bool? IsDelivered
        {
            get { return _isDelivered; }
            set 
            { 
                _isDelivered = value; 
                RaisePropertyChanged(() => IsDelivered);
                RaisePropertyChanged(() => ValidComments);
            }
        }

        public string OrderID { get; set; }

        public Guid MobileDataID { get; set; }

        public string DamageIndicatorText
        {
            get { return (DamageStatus.Code == "POD") ? string.Empty : DamageStatus.Text.Substring(0, 1); }
        }

        public List<DamageStatus> DamageStatuses
        {
            get
            {
                return _damageStatuses;
            }
            set
            {
                _damageStatuses = value;
                RaisePropertyChanged(() => DamageStatuses);
            }

        }
        
        public DamageStatus DamageStatus
        {
            get
            {
                return _damageStatus;
            }
            set
            {
                _damageStatus = value;
                RaisePropertyChanged(() => DamageStatus);
                RaisePropertyChanged(() => ValidComments);
                RaisePropertyChanged(() =>  DamageIndicatorText);
            }
        }

        public string DeliveryComments
        {
            get
            {
                return _deliveryComments;
            }
            set
            {
                _deliveryComments = value;
                RaisePropertyChanged(() => DeliveryComments);
                RaisePropertyChanged(() => ValidComments);
            }
        }

        // translates delivery and damage status into single field for pallet force
        public string PalletforceDeliveryStatus
        {
            get
            {
                if (!IsDelivered.HasValue)
                    return DamageStatus.Code;

                return (IsDelivered.Value) ? DamageStatus.Code : NOT_DELIVERED_CODE;
            }
        }

        public bool ValidComments
        {
            get { return ((this.IsDelivered.Value && this.DamageStatus.Code == "POD") || !string.IsNullOrEmpty(this.DeliveryComments)); }
        }

        public virtual bool IsDummy
        {
            get { return false; }
        }

        private bool _isSelected ;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChanged(() => IsSelected);            
            }
        }

        private MvxCommand _selectBarcodeCommand;
        public ICommand SelectBarcodeCommand
        {
            get { return (_selectBarcodeCommand = _selectBarcodeCommand ?? new MvxCommand(async () => await this.SelectBarcodeAsync())); }
        }

        public override string FragmentTitle
        {
            get { return ""; }
        }

        #endregion

        #region public methods

        public BarcodeItemViewModel Clone()
        {
            BarcodeItemViewModel clone = new BarcodeItemViewModel(_navigationService, this.DamageStatuses, _barcodeScanningViewModel)
            {
                DamageStatus = this.DamageStatus,
                BarcodeText = this.BarcodeText,
                IsDelivered = this.IsDelivered,
                DeliveryComments = this.DeliveryComments
            };

            return clone;
        }

        #endregion

        #region private methods

        public async Task SelectBarcodeAsync()
        {
            if (this.IsDummy)
                return; 
            
            if (!this.IsDelivered.HasValue) 
            {
                string message = string.Format("Barcodes should be scanned if possible. Confirm the pallet with barcode {0} has been manually processed.", _barcodeText);
                string title = "Mark Barcode as Manually Processed?";

                if (await Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync(message, title))
                    _barcodeScanningViewModel.MarkBarcodeAsProcessed(this, false);

                return;
            }

            var navItem = new NavData<BarcodeItemViewModel>() { Data = this };

            // if there are multiple barcodes selected then add them to the nav item
            if (_barcodeScanningViewModel.SelectedBarcodes.Any())
            {
                navItem.OtherData["SelectedBarcodes"] = _barcodeScanningViewModel.SelectedBarcodes.ToList();
            }

            _navigationService.ShowModalViewModel<BarcodeStatusViewModel, bool>(navItem, (modified) =>
            {
                if (modified)
                {
                    // need to do anything here?
                }
            });
        }
    }


        public class DummyBarcodeItemViewModel : BarcodeItemViewModel
        {
            public DummyBarcodeItemViewModel()
                : base(null, null, null)
            { }

            public override bool IsDummy
            {
                get
                {
                    return true;
                }
            }
        }

        #endregion


}
