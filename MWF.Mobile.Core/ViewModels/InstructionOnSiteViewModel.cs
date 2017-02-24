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
    public class InstructionOnSiteViewModel : 
        BaseInstructionNotificationViewModel, 
        IBackButtonHandler
    {
        
        #region Private Fields

        private readonly INavigationService _navigationService;
        private readonly IRepositories _repositories;
        private MobileData _mobileData;
        private NavData<MobileData> _navData;
        private IInfoService _infoService;
        private IDataChunkService _dataChunkService;


        #endregion

        #region Construction

        public InstructionOnSiteViewModel(INavigationService navigationService, IRepositories repositories, IInfoService infoService, IDataChunkService dataChunkService)
        {
            _navigationService = navigationService;
            _repositories = repositories;
            _infoService = infoService;
            _dataChunkService = dataChunkService;
        }

        public async Task Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            _mobileData = _navData.Data;
            _orderList = new ObservableCollection<Item>(_mobileData.Order.Items);
            var config = await _repositories.ConfigRepository.GetAsync();
            this.IsDeliveryAddEnabled = _mobileData.Order.Type == Enums.InstructionType.Deliver && config.DeliveryAdd;
            this.SetInstructionCommentButtonLabel();
        }

        #endregion

        #region Public Properites

        private MvxCommand _advanceInstructionOnSiteCommand;
        public ICommand AdvanceInstructionOnSiteCommand
        {
            get { return (_advanceInstructionOnSiteCommand = _advanceInstructionOnSiteCommand ?? new MvxCommand(async () => await this.AdvanceInstructionOnSiteAsync())); }
        }

        private MvxCommand<Item> _showInstructionOrderCommand;
        public ICommand ShowInstructionOrderCommand
        {
            get { return(_showInstructionOrderCommand = _showInstructionOrderCommand ?? new MvxCommand<Item>(o => ShowOrder(o))); }
        }

        private MvxCommand _addDeliveriesCommand;
        public ICommand AddDeliveriesCommand
        {
            get { return (_addDeliveriesCommand = _addDeliveriesCommand ?? new MvxCommand(() => AddDeliveries())); }
        }

        private ObservableCollection<Item> _orderList;
        public ObservableCollection<Item> OrderList
        {
            get { return _orderList; }
            set { _orderList = value; RaisePropertyChanged(() => OrderList); }
        }

        private string _instructionCommentButtonLabel;
        public string InstructionCommentButtonLabel
        {
            get { return _instructionCommentButtonLabel; }
            set { _instructionCommentButtonLabel = value; RaisePropertyChanged(() => InstructionCommentButtonLabel); }
        }

        public string HeaderText { get { return "Select an order for further details"; } }

        public string AddDeliveriesButtonLabel
        {
            get { return "Add/Remove Deliveries"; }
        }

        private bool _isDeliveryAddEnabled;
        public bool IsDeliveryAddEnabled
        {
            get { return _isDeliveryAddEnabled; }
            private set { _isDeliveryAddEnabled = value; RaisePropertyChanged(() => IsDeliveryAddEnabled); }
        }

        #endregion

        #region Private Methods

        private async Task AdvanceInstructionOnSiteAsync()
        {
            await this.SendAdditionalInstructionOnSiteDataChunksAsync();
            await _navigationService.MoveToNextAsync(_navData);
        }

        private void ShowOrder(Item order)
        {
            NavData<MobileData> navData = new NavData<MobileData>();

            if (order.OrderId == _navData.Data.Order.ID)
            {
                navData.Data = _mobileData;
                navData.OtherData["DataChunk"] = navData.GetDataChunk();
            }
            else
            {
                navData.Data = _navData.GetAdditionalInstructions().FirstOrDefault(md => md.Order.ID == order.OrderId);
                navData.OtherData["DataChunk"] = navData.GetAdditionalDataChunk(navData.Data);
            }

            navData.OtherData["Order"] = order;

            _navigationService.ShowModalViewModel<OrderViewModel, bool>(navData, (modified) =>
                {
                    if (modified)
                    {
                       
                    }
                }               
            );
        }

        /// <summary>
        /// Ensures that all the additional instructions added send "onSite" data chunks
        /// </summary>
        private async Task SendAdditionalInstructionOnSiteDataChunksAsync()
        {
            var additionalInstructions = _navData.GetAdditionalInstructions();

            foreach (var additionalInstruction in additionalInstructions)
            {
                if (additionalInstruction.ProgressState != Enums.InstructionProgress.OnSite)
                {
                    additionalInstruction.ProgressState = Enums.InstructionProgress.OnSite;
                    _navData.Data.OnSiteDateTime = DateTime.Now;
                    await _dataChunkService.SendDataChunkAsync(_navData.GetAdditionalDataChunk(additionalInstruction), additionalInstruction, _infoService.CurrentDriverID.Value, _infoService.CurrentVehicleRegistration);
                }
            }

        }

        private void AddDeliveries()
        {
            _navigationService.ShowModalViewModel<InstructionAddDeliveriesViewModel, bool>(_navData, (modified) =>
            {
                if (modified)
                {
                    RefreshOrders();
                }
            });
        }

        private void RefreshOrders()
        {
            List<Item> newOrderList = new List<Item>(_mobileData.Order.Items);
            var additionalInstructions = _navData.GetAdditionalInstructions();

            foreach (var additionalInstruction in additionalInstructions)
            {
                newOrderList.AddRange(additionalInstruction.Order.Items);
            }

            this.OrderList = new ObservableCollection<Item>(newOrderList);
        }

        private void SetInstructionCommentButtonLabel()
        {
            var deliveryOptions = _navData.GetWorseCaseDeliveryOptions();

            this.InstructionCommentButtonLabel =
                ((_mobileData.Order.Type == Enums.InstructionType.Collect
                && (_mobileData.Order.Additional.CustomerNameRequiredForCollection
                || _mobileData.Order.Additional.CustomerSignatureRequiredForCollection
                || _mobileData.Order.Additional.IsTrailerConfirmationEnabled))
                || (_mobileData.Order.Type == Enums.InstructionType.Deliver
                && (deliveryOptions.CustomerNameRequiredForDelivery
                || deliveryOptions.CustomerSignatureRequiredForDelivery
                || deliveryOptions.BarcodeScanRequiredForDelivery
                || !deliveryOptions.BypassCommentsScreen
                || !deliveryOptions.BypassCleanClausedScreen))) ? "Continue" : "Complete";
        }

        #endregion

        #region BaseFragmentViewModel Overrides

        public override string FragmentTitle
        {
            get { return _mobileData.Order.Type.ToString() + " On Site"; }
        }

        #endregion BaseFragmentViewModel Overrides

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            await _navigationService.GoBackAsync(_navData);
            return false;
        }

        #endregion IBackButtonHandler Implementation

        #region BaseInstructionNotificationViewModel Overrides

        public override Task CheckInstructionNotificationAsync(Messages.GatewayInstructionNotificationMessage message)
        {
            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _mobileData = _navData.Data;

                InvokeOnMainThread(() =>
                {
                    RefreshOrders();
                    RaiseAllPropertiesChanged();
                });
            });
        }

        #endregion BaseInstructionNotificationViewModel Overrides

    }
}
