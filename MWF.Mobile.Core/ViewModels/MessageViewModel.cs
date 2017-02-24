using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MWF.Mobile.Core.ViewModels
{
    public class MessageViewModel
        : BaseModalViewModel<bool>,
        IBackButtonHandler
    {

        #region Private Members

        private MessageModalNavItem _navItem;
        private MobileData _mobileData;
        private IRepositories _repositories;
        private IDataChunkService _dataChunkService;
        private IInfoService _infoService;
        private MvxCommand _readMessageCommand;
        private bool _isMessageRead;

        #endregion Private Members

        #region Construction

        public MessageViewModel(IRepositories repositories, IInfoService infoService, IDataChunkService dataChunkService)
        {
            _dataChunkService = dataChunkService;
            _infoService = infoService;
            _repositories = repositories;
        }

        public async Task Init(Guid navID)
        {
            SetMessageID(navID);

            var navData = Mvx.Resolve<INavigationService>().GetNavData<MessageModalNavItem>(navID);
            _navItem = navData.Data;

            _isMessageRead = _navItem.IsRead;
            _mobileData = await _repositories.MobileDataRepository.GetByIDAsync(_navItem.MobileDataID);

            this.MessageContentText = _mobileData.Order.Items.First().Description;
            this.IsWithPoint = _mobileData.Order.Addresses.Any();
            this.PointDescription = _mobileData.Order.Description;
            this.ReadButtonText = _isMessageRead ? "Return" : "Mark as read";
            this.Address = this.IsWithPoint ? (_mobileData.Order.Addresses[0].Lines.Replace("|", "\n") + "\n" + _mobileData.Order.Addresses[0].Postcode) : string.Empty;
            
            RaiseAllPropertiesChanged();
        }

        #endregion Construction

        #region Public Properties

        private string _messageContentText;
        public string MessageContentText
        {
            get { return _messageContentText; }
            set { _messageContentText = value; RaisePropertyChanged(() => MessageContentText); }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set { _address = value; RaisePropertyChanged(() => Address); }
        }

        private string _pointDescription;
        public string PointDescription
        {
            get { return _pointDescription; }
            set { _pointDescription = value; RaisePropertyChanged(() => PointDescription); }
        }

        public string AddressLabelText
        {
            get { return "Address"; }
        }

        private string _readButtonText;
        public string ReadButtonText
        {
            get { return _readButtonText; }
            set { _readButtonText = value; RaisePropertyChanged(() => ReadButtonText); }
        }

        private bool _isWithPoint;
        public bool IsWithPoint
        {
            get { return _isWithPoint; }
            set { _isWithPoint = value; RaisePropertyChanged(() => IsWithPoint); }
        }

        public ICommand ReadMessageCommand
        {
            get { return (_readMessageCommand = _readMessageCommand ?? new MvxCommand(async () => await this.ReadMessageAsync())); }
        }

        #endregion Public Properties

        #region Private Methods

        public async Task ReadMessageAsync()
        {
            if (!_isMessageRead)
            {
                _mobileData.ProgressState = Enums.InstructionProgress.Complete;

                await _dataChunkService.SendDataChunkAsync(new MobileApplicationDataChunkContentActivity(), _mobileData, _infoService.CurrentDriverID.Value, _infoService.CurrentVehicleRegistration);
            }

            ReturnResult(!_isMessageRead);
        }

        #endregion Private Methods

        #region BaseFragmentViewModel Overrides

        public override string FragmentTitle
        {
            get { return "Message"; }
        }

        #endregion  BaseFragmentViewModel Overrides

        #region IBackButtonHandler Implementation

        public Task<bool> OnBackButtonPressedAsync()
        {
            ReturnResult(false);
            return Task.FromResult(false);
        }

        #endregion IBackButtonHandler Implementation

    }
}
