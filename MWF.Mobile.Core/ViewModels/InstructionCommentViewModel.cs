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
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using MWF.Mobile.Core.ViewModels.Extensions;

namespace MWF.Mobile.Core.ViewModels
{
    public class InstructionCommentViewModel
        : BaseInstructionNotificationViewModel, IBackButtonHandler
    {
        #region Private Fields

        private readonly INavigationService _navigationService;
        private readonly IRepositories _repositories;
        private MobileData _mobileData;
        private NavData<MobileData> _navData;
        private IInfoService _infoService;

        #endregion

        #region Construction

        public InstructionCommentViewModel(INavigationService navigationService, IRepositories repositories, IInfoService infoService)
        {
            _navigationService = navigationService;
            _repositories = repositories;
            _infoService = infoService;
        }

        public void Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            _mobileData = _navData.Data;
        }

        #endregion

        #region Public Properties

        private MvxCommand _advanceInstructionCommentCommand;
        public ICommand AdvanceInstructionCommentCommand
        {
            get { return (_advanceInstructionCommentCommand = _advanceInstructionCommentCommand ?? new MvxCommand(async () => await this.AdvanceInstructionCommentAsync())); }
        }

        public string InstructionCommentButtonLabel
        {
            get
            {
                var deliveryOptions = _navData.GetWorseCaseDeliveryOptions();

                return ((_mobileData.Order.Type == Enums.InstructionType.Collect
                    && (_mobileData.Order.Additional.CustomerNameRequiredForCollection
                    || _mobileData.Order.Additional.CustomerSignatureRequiredForCollection))
                    || (_mobileData.Order.Type == Enums.InstructionType.Deliver
                    && (deliveryOptions.CustomerNameRequiredForDelivery
                    || deliveryOptions.CustomerSignatureRequiredForDelivery))) ? "Continue" : "Complete";
            }
        }

        public string InstructionCommentPageHeader { get { return "Comment Screen"; } }

        private string _commentText;
        public string CommentText
        {
            get { return _commentText; }
            set { _commentText = value; RaisePropertyChanged(() => CommentText); }
        }


        #endregion

        #region Private Methods

        public Task AdvanceInstructionCommentAsync()
        {
            var dataChunks = _navData.GetAllDataChunks();

            foreach (var dataChunk in dataChunks)
            {
                dataChunk.Comment = CommentText;
            }

            return _navigationService.MoveToNextAsync(_navData);
        }

        #endregion

        #region BaseFragmentViewModel Overrides
        public override string FragmentTitle
        {
            get { return "Comment"; }
        }

        #endregion

        #region BaseInstructionNotificationViewModel Overrides

        public override Task CheckInstructionNotificationAsync(Messages.GatewayInstructionNotificationMessage message)
        {
            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _mobileData = _navData.Data;
                RaiseAllPropertiesChanged();
            });
        }

        #endregion BaseInstructionNotificationViewModel Overrides

        #region IBackButtonHandler Implementation

        public async Task<bool> OnBackButtonPressedAsync()
        {
            if (_mobileData.Order.Type == Enums.InstructionType.Deliver)
            {
                // Delivery, continue back using normal backstack mechanism
                return true;
            }
            else
            {
                // Collection, use custom back mapping action to skip the select trailer workflow
                await _navigationService.GoBackAsync(_navData);
                return false;
            }
        }

        #endregion IBackButtonHandler Implementation

    }
}
