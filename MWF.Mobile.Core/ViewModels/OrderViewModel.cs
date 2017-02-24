using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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

namespace MWF.Mobile.Core.ViewModels
{
    public class OrderViewModel
        : BaseInstructionNotificationViewModel,
        IModalViewModel<bool>,
        IBackButtonHandler
    {
        #region Private Fields

        private readonly INavigationService _navigationService;
        private readonly IRepositories _repositories;
        private readonly IConfigRepository _configRepository;

        private IInfoService _infoService;
        private MobileData _mobileData;
        private MWFMobileConfig _mobileConfig;
        private Item _order;
        private NavData<MobileData> _navData;

        #endregion Private Fields

        #region Construction

        public OrderViewModel(INavigationService navigationService, IRepositories repositories, IInfoService infoService)
        {
            _navigationService = navigationService;
            _repositories = repositories;
            _infoService = infoService;
            _configRepository = repositories.ConfigRepository;
        }

        public async Task Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            this.MessageId = navID;
            _order = _navData.OtherData["Order"] as Item;
            _mobileData = _navData.Data;
            _mobileConfig = await _configRepository.GetByIDAsync(_mobileData.CustomerId);
            this.ChangeOrderQuantity = true && _mobileData.Order.Type != Enums.InstructionType.Deliver;//_mobileConfig.QuantityIsEditable && _mobileData.Order.Type != Enums.InstructionType.Deliver;
        }

        #endregion Construction

        #region Public Properties

        public string OrderName { get { return "Order " + _order.ItemIdFormatted; } }

        public string OrderID { get { return _order.ItemIdFormatted; } }

        public string OrderLoadNo { get { return _order.Title; } }

        public string OrderDeliveryNo { get { return _order.DeliveryOrderNumber; } }

        public string OrderQuantity { get { return _order.Quantity; } }

        public string OrderWeight { get { return _order.Weight; } }

        public string OrderBusinessType { get { return _order.BusinessType; } }

        public string OrderGoodsType { get { return _order.GoodsType; } }

        public string OrderCases { get { return _order.Cases; } }

        public string OrderPallets { get { return _order.Pallets; } }

        public string ChangeOrderQuantityButtonLabel { get { return "Change Quantity"; } }

        private bool _changeOrderQuantity;
        public bool ChangeOrderQuantity
        {
            get { return _changeOrderQuantity; }
            private set { _changeOrderQuantity = value; RaisePropertyChanged(() => ChangeOrderQuantity); }
        }

        #endregion Public Properties

        #region Public Methods

        private MvxCommand<Item> _reviseQuantityOrderCommand;
        public ICommand ReviseQuantityOrderCommand
        {
            get
            {
                return (_reviseQuantityOrderCommand = _reviseQuantityOrderCommand ?? new MvxCommand<Item>(o => ReviseQuantity(o)));
            }
        }

        #endregion

        #region Private Methods

        private void ReviseQuantity(Item order)
        {
            _navigationService.ShowModalViewModel<ReviseQuantityViewModel, bool>(_navData, (confirmed) => {});

            //_navigationService.MoveToNext(_navData);
        }

        #endregion Private Methods

        #region BaseFragmentViewModel Overrides
        public override string FragmentTitle
        {
            get { return "Order"; }
        }

        #endregion BaseFragmentViewModel Overrides

        #region IBackButtonHandler Implementation

        public Task<bool> OnBackButtonPressedAsync()
        {
            this.Cancel();
            return Task.FromResult(false);
        }

        #endregion IBackButtonHandler Implementation

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

        #region BaseInstructionNotificationViewModel

        public override Task CheckInstructionNotificationAsync(GatewayInstructionNotificationMessage message)
        {
            var orderID = _order.ID;

            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _mobileData = _navData.Data;
                _order = _mobileData.Order.Items.FirstOrDefault(i => i.ID == orderID);
                _navData.OtherData["Order"] = _order;
                RaiseAllPropertiesChanged();
            });
        }

        #endregion BaseInstructionNotificationViewModel

    }

}
