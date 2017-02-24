using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Extensions;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;

namespace MWF.Mobile.Core.ViewModels
{

    public class BarcodeScanningViewModel :
        BaseInstructionNotificationViewModel, IBackButtonHandler
    {

        #region Construction

        private MobileData _mobileData = null;
        private NavData<MobileData> _navData;

        protected BarcodeSectionViewModel _unprocessedBarcodes;
        protected BarcodeSectionViewModel _processedBarcodes;
        private IRepositories _repositories;
        private List<DamageStatus> _damageStatuses;
        List<MobileData> _additionalInstructions;

        public List<DamageStatus> DamageStatuses
        {
            get { return _damageStatuses; }
        }

        private INavigationService _navigationService;

        public BarcodeScanningViewModel(INavigationService navigationService, IRepositories repositories)
        {
            _navigationService = navigationService;
            _repositories = repositories;
        }

        public async Task Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            _mobileData = _navData.Data;
            _additionalInstructions = _navData.GetAdditionalInstructions();

            await this.BuildDamageStatusesAsync();
            this.CreateSections();
        }

        private void CreateSections()
        {
            BarcodeSections = new ObservableCollection<BarcodeSectionViewModel>();

            _unprocessedBarcodes = new BarcodeSectionViewModel(this)
            {
                SectionHeader = "To Do",
            };

            BarcodeSections.Add(_unprocessedBarcodes);

            _processedBarcodes = new BarcodeSectionViewModel(this)
            {
                SectionHeader = "Done",
            };

            BarcodeSections.Add(_processedBarcodes);

            //Initalize the unprocessed collection
            List<BarcodeItemViewModel> barcodeItemsViewModels = new List<BarcodeItemViewModel>();
            var items = _mobileData.Order.Items;
            foreach (var item in items)
            {
                barcodeItemsViewModels.AddRange(item.BarcodesList.Select(b => new BarcodeItemViewModel(_navigationService, _damageStatuses, this) { BarcodeText = b, OrderID = item.ItemIdFormatted, MobileDataID = _mobileData.ID}));
            }

            // additional instructions
            foreach (var additionalInstruction in _additionalInstructions)
            {
                foreach (var item in additionalInstruction.Order.Items)
                {
                    barcodeItemsViewModels.AddRange(item.BarcodesList.Select(b => new BarcodeItemViewModel(_navigationService, _damageStatuses, this) { BarcodeText = b, OrderID = item.ItemIdFormatted, MobileDataID = additionalInstruction.ID }));
                }
            }

            foreach (var vm in barcodeItemsViewModels)
            {
                _unprocessedBarcodes.Barcodes.Add(vm);
            }

            RaisePropertyChanged(() => BarcodeSections);
        }

        #endregion Construction

        #region Public Properties

        private ObservableCollection<BarcodeSectionViewModel> _barcodeSections;
        public ObservableCollection<BarcodeSectionViewModel> BarcodeSections
        {
            get { return _barcodeSections; }
            set { _barcodeSections = value; RaisePropertyChanged(() => BarcodeSections); }
        }

        public IEnumerable<BarcodeItemViewModel> SelectedBarcodes
        {
            get
            {
                System.Diagnostics.Debug.Assert(_processedBarcodes != null, "SelectBarcodes: _processedBarcodes is null");
                return _processedBarcodes.Where(x => x.IsSelected);
            }
        }


        private string _barcodeInput = string.Empty;
        public string BarcodeInput
        {
            get { return _barcodeInput; }
            set 
            {
                //Scanner adds newline when setting NewLine ways is set to Model second.
                _barcodeInput = value.TrimEnd('\n'); 
                CheckScanInput(); 
            }
        }

        private MvxCommand _completeScanningCommand;
        public ICommand CompleteScanningCommand
        {
            get { return (_completeScanningCommand = _completeScanningCommand ?? new MvxCommand(async () => await this.CompleteScanningAsync())); }
        }

        public string InstructionsText
        {
            get { return "Scan barcodes or select them from the To Do list."; }
        }

        public string CompleteButtonText
        {
            get { return "Continue"; }
        }

        private bool _canScanningBeCompleted;
        public bool CanScanningBeCompleted
        {
            get
            {
                return _canScanningBeCompleted;
            }
            set
            {
                _canScanningBeCompleted = value;
                RaisePropertyChanged(() => CanScanningBeCompleted);
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(() => IsBusy); }
        }

        public string ScanTitle
        {
            get { return "Processing..."; }
        }

        public string ScanMessage
        {
            get { return "Processing..."; }
        }

        #endregion Public Properties

        #region Public Methods

        public virtual void MarkBarcodeAsProcessed(BarcodeItemViewModel barcodeItem, bool wasScanned = true)
        {
            barcodeItem.IsScanned = wasScanned;
            barcodeItem.IsDelivered = true;

            _unprocessedBarcodes.Barcodes.Remove(barcodeItem);
            _processedBarcodes.Barcodes.Add(barcodeItem);

            if (_unprocessedBarcodes.Barcodes.Count == 0)
            {
                _unprocessedBarcodes.Barcodes.Add(new DummyBarcodeItemViewModel());
                this.CanScanningBeCompleted = true;
            }

            RaisePropertyChanged(() => BarcodeSections);
        }

        #endregion

        #region Private Methods

        private async Task BuildDamageStatusesAsync()
        {
            _damageStatuses = new List<DamageStatus>();

            _damageStatuses.Add(new DamageStatus() { Text = "Clean", Code = "POD" });

            var data = await _repositories.VerbProfileRepository.GetAllAsync();
            var palletForceVerbProfile = data.Where(vp => vp.Code == "PFORCE").FirstOrDefault();

            if (palletForceVerbProfile != null)
            {
                var customDamageStatuses = palletForceVerbProfile.Children.Select(vp => new DamageStatus() { Text = vp.Title, Code = vp.Category });
                _damageStatuses.AddRange(customDamageStatuses);
            }
        }

        private void CheckScanInput()
        {
            var barcode = _unprocessedBarcodes.FirstOrDefault(bc => bc.BarcodeText == this.BarcodeInput);

            if (barcode !=null)
            {
                MarkBarcodeAsProcessed(barcode);
                ClearBarcode();
                RegainFocusViaHack();
            }
            else
            {
                ShowErrorAlert();
            }
        }

        private void ShowErrorAlert()
        {
            string errorMessage = "Invalid Barcode";

            if (_processedBarcodes.Barcodes.Any(x => x.BarcodeText == BarcodeInput))
                errorMessage = "Barcode already scanned";

            Mvx.Resolve<ICustomUserInteraction>().Alert(errorMessage, () => { ClearBarcode(); RequestBarcodeFocus(); });
        }

        private async void RegainFocusViaHack()
        {
            this.IsBusy = true;
            await Task.Delay(200);
            this.IsBusy = false;
            RequestBarcodeFocus();
        }

        public Task CompleteScanningAsync()
        {

            if (!CanScanningBeCompleted){
                return Task.FromResult(0);
            }
            // Update datachunk for this order
            var newScannedDelivery = GetScannedDelivery(_mobileData.ID);
            _navData.GetDataChunk().ScannedDelivery = newScannedDelivery;

            // Do the same for all additional orders
            foreach (var additionalInstruction in _additionalInstructions)
            {
                newScannedDelivery = GetScannedDelivery(additionalInstruction.ID);
                _navData.GetAdditionalDataChunk(additionalInstruction).ScannedDelivery = newScannedDelivery;
            }

            return _navigationService.MoveToNextAsync(_navData);
        }

        private ScannedDelivery GetScannedDelivery(Guid Id)
        {
            var newScannedDelivery = new ScannedDelivery()
            {
                Barcodes = _processedBarcodes.Barcodes.Where(bc => bc.MobileDataID == Id).Select(bvm =>
                              new Barcode
                              {
                                  BarcodeText = bvm.BarcodeText,
                                  IsScanned = bvm.IsScanned,
                                  OrderID = bvm.OrderID,
                                  IsDelivered = bvm.IsDelivered.Value,
                                  DamageStatusCode = bvm.DamageStatus.Code,
                                  DeliveryStatusCode = bvm.PalletforceDeliveryStatus,
                                  DeliveryStatusNote = bvm.DeliveryComments

                              }).ToList()
            };
            return newScannedDelivery;
        }

        private void ClearBarcode()
        {
            _barcodeInput = string.Empty;
            RaisePropertyChanged(() => BarcodeInput);
        }

        private void RequestBarcodeFocus()
        {
            RaisePropertyChanged("RequestBarcodeFocus");
        }

        #endregion Private Methods

        #region BaseInstructionNotificationViewModel Overrides

        public override Task CheckInstructionNotificationAsync(Messages.GatewayInstructionNotificationMessage message)
        {
            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _mobileData = _navData.Data;
                _additionalInstructions = _navData.GetAdditionalInstructions();

                InvokeOnMainThread(() =>
                {
                    CreateSections();
                    RaiseAllPropertiesChanged();
                });
            });
        }

        #endregion BaseInstructionNotificationViewModel Overrides

        #region BaseFragmentViewModel Overrides

        public override string FragmentTitle
        {
            get { return _mobileData.Order.Type.ToString() + " Scan"; }
        }

        #endregion BaseFragmentViewModel Overrides

        #region IBackButtonHandler Implementation

        public Task<bool> OnBackButtonPressedAsync()
        {
            return Mvx.Resolve<ICustomUserInteraction>().ConfirmAsync("The changes you have made will be lost, do you wish to continue?", "Changes will be lost!", "Continue");
        }

        #endregion
    }

}
