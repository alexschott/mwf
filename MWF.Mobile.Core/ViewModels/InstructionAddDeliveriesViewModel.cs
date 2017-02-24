using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Extensions;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;


namespace MWF.Mobile.Core.ViewModels
{
    public class InstructionAddDeliveriesViewModel : BaseInstructionNotificationViewModel,
        IModalViewModel<bool>, 
        IBackButtonHandler
    {

        #region Private Members

        private IRepositories _repositories;
        private IInfoService _infoService;
        private MvxCommand _doneCommand;
        private NavData<MobileData> _navData;
        private ApplicationProfile _appProfile;
        private INavigationService _navigationService;
        private List<MobileData> _additionalInstructions;
        private string _originalSelection;
        private bool _doneButtonEnabled;
        #endregion

        #region Construction

        public InstructionAddDeliveriesViewModel(IRepositories repositories, IInfoService infoService, INavigationService navigationService)
        {
            _repositories = repositories;
            _infoService = infoService;
            _navigationService = navigationService;
        }

        public async Task Init(Guid navID)
        {
            DoneButtonEnabled = false;
            _navData = _navigationService.GetNavData<MobileData>(navID);
            this.MessageId = navID;
            _additionalInstructions = _navData.GetAdditionalInstructions();
            _appProfile = (await _repositories.ApplicationRepository.GetAllAsync()).First();
            await this.GetDeliveryInstructionsAsync();
            DoneButtonEnabled = true;
        }

        private async Task GetDeliveryInstructionsAsync()
        {
            var today = DateTime.Today;

            var data = await _repositories.MobileDataRepository.GetNonCompletedInstructionsAsync(_infoService.CurrentDriverID.Value);
            // get all non-complete deliveries (excluding the current one) that conform to the same "barcode scanning on delivery) type as the current one
            var nonCompletedDeliveries = data.Where(i => i.Order.Type == Enums.InstructionType.Deliver && 
                                                    i.ID != _navData.Data.ID &&                                                                                                                                              
                                                    i.Order.Items.First().Additional.BarcodeScanRequiredForDelivery == _navData.Data.Order.Items.First().Additional.BarcodeScanRequiredForDelivery);      
            // only get the ones that show up in the same time range as displayed in the manifest screen
            var nonCompletedDeliveriesInRange = nonCompletedDeliveries.Where(i => i.EffectiveDate < today.AddDays(_appProfile.DisplaySpan) && i.EffectiveDate > today.AddDays(-_appProfile.DisplayRetention)).OrderBy(x => x.EffectiveDate);
           
            // build view models

            var viewModels = nonCompletedDeliveries.Select(i => new ManifestInstructionViewModel(this, i) { IsSelected = _additionalInstructions.Any( ai => ai.ID == i.ID) });
            this.DeliveryInstructions = new ObservableCollection<ManifestInstructionViewModel>(viewModels);

            _originalSelection = GetSelectionSummary();
        }

        #endregion

        #region Public Properties


        public override string FragmentTitle
        {
            get { return "Add/Remove Deliveries"; }
        }

        public string InstructionsText
        {
            get { return "Add or remove deliveries to be completed with this delivery."; }
        }

        public string DoneButtonLabel
        {
            get { return "Done"; }
        }

        public bool DoneButtonEnabled
        {
            get { return _doneButtonEnabled; }
            set
            {
                _doneButtonEnabled = value;
                RaisePropertyChanged(() => DoneButtonEnabled);
            }
        }

        public System.Windows.Input.ICommand DoneCommand
        {
            get { return (_doneCommand = _doneCommand ?? new MvxCommand(() => DoDoneCommand())); }
        }

        public bool UserChangesDetected
        {
            get { return (this.DeliveryInstructions != null && _originalSelection != GetSelectionSummary()); }
        }

        private ObservableCollection<ManifestInstructionViewModel> _deliveryInstructions;
        public ObservableCollection<ManifestInstructionViewModel> DeliveryInstructions
        {
            get
            {
                return _deliveryInstructions;
            }
            set
            {
                _deliveryInstructions = value;
                RaisePropertyChanged(() => DeliveryInstructions);
            }
        }

        #endregion

        #region Private Methods

        private void DoDoneCommand()
        {
            var selectedInstructionIDs = this.DeliveryInstructions.Where(di => di.IsSelected).Select(di => di.InstructionID).ToList();
            var additionalInstructions = _navData.GetAdditionalInstructions();

            // work out which instructions no longer are selected and remove them
            var instructionsToRemove = additionalInstructions.Where(ai => !selectedInstructionIDs.Contains(ai.ID)).ToList(); ;

            // work out which newly selected instructions need to be added         
            var instructionsToAdd = this.DeliveryInstructions.Where(di => di.IsSelected && !additionalInstructions.Any(ai => ai.ID == di.InstructionID)).ToList();

            foreach (var item in instructionsToRemove)
            {
                additionalInstructions.Remove(item);
            }         

            foreach (var item in instructionsToAdd)
            {
                additionalInstructions.Add(item.MobileData);
            }

            ReturnResult(true);
        }

        private string GetSelectionSummary()
        {
            return string.Join(" ", this.DeliveryInstructions.Select(i => i.IsSelected.ToString()).ToArray());
        }

        #endregion

        #region IModalViewModel

        public Guid MessageId { get; set; }

        public void Cancel()
        {
            ReturnResult(default(bool));
        }

        public void ReturnResult(bool result)
        {
            var message = new ModalNavigationResultMessage<bool>(this, MessageId, result);

            this.Messenger.Publish(message);
            this.Close(this);
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

        #region BaseInstructionNotificationViewModel

        public override Task CheckInstructionNotificationAsync(Messages.GatewayInstructionNotificationMessage message)
        {
            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _navData.GetAdditionalInstructions().Clear();

                InvokeOnMainThread(async () =>
                {
                    await this.GetDeliveryInstructionsAsync();
                    RaiseAllPropertiesChanged();
                });
            });
        }

        #endregion


    }

}
