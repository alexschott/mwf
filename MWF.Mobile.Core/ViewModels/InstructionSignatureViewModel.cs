using System;
using System.Collections.Generic;
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
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Extensions;

namespace MWF.Mobile.Core.ViewModels
{
    public class InstructionSignatureViewModel :
        BaseInstructionNotificationViewModel, IBackButtonHandler
    {

        #region Private Fields

        private readonly INavigationService _navigationService;
        private readonly IRepositories _repositories;
        private readonly ICustomUserInteraction _userInteraction;
        private readonly IInfoService _infoService;
        private MobileData _mobileData;
        private NavData<MobileData> _navData;

        #endregion

        #region Construction

        public InstructionSignatureViewModel(INavigationService navigationService, IRepositories repositories, ICustomUserInteraction userInteraction, IInfoService mobileApplicationDataChunkService)
        {
            _navigationService = navigationService;
            _repositories = repositories;
            _userInteraction = userInteraction;
            _infoService = mobileApplicationDataChunkService;

        }

        public void Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            _mobileData = _navData.Data;
        }

        #endregion

        #region Public Properties

        public string InstructionSignatureButtonLabel { get { return "Complete"; } }

        public bool IsSignaturePadEnabled { get; set; }

        /// <summary>
        ///  If signature is required, the signature box stays active and you cannot press the 'Signature toggle' button.
        ///
        ///  If signature is NOT required, then the signature box is disabled by default, but can be turned on with the 'Signature toggle' button
        /// </summary>
        public bool IsSignatureToggleButtonEnabled
        {
            get
            {

                var deliveryOptions = _navData.GetWorseCaseDeliveryOptions();

                if ((_mobileData.Order.Type == Enums.InstructionType.Collect && _mobileData.Order.Additional.CustomerSignatureRequiredForCollection)
                    || (_mobileData.Order.Type == Enums.InstructionType.Deliver && deliveryOptions.CustomerSignatureRequiredForDelivery))
                {
                    IsSignaturePadEnabled = true;
                    RaisePropertyChanged(() => SignatureToggleButtonLabel);
                    return false;
                }
                else
                {
                    IsSignaturePadEnabled = false;
                    RaisePropertyChanged(() => SignatureToggleButtonLabel);
                    return true;
                }
            }
        }

        public string SignatureToggleButtonLabel
        {
            get { return (IsSignaturePadEnabled) ? "Signature unavailable" : "Signature available"; }
        }

        public string InstructionSignaturePageHeader { get { return "I confirm this transaction"; } }

        private string _customerName;
        public string CustomerName
        {
            get { return _customerName; }
            set { _customerName = value; RaisePropertyChanged(() => CustomerName); }
        }

        private string _customerSignatureEncodedImage;
        public string CustomerSignatureEncodedImage
        {
            get { return _customerSignatureEncodedImage; }
            set { _customerSignatureEncodedImage = value; RaisePropertyChanged(() => CustomerSignatureEncodedImage); }
        }

        private MvxCommand _instructionDoneCommand;
        public ICommand InstructionDoneCommand
        {
            get { return (_instructionDoneCommand = _instructionDoneCommand ?? new MvxCommand(async () => await this.InstructionDoneAsync())); }
        }

        #endregion

        #region Private Methods

        public async Task InstructionDoneAsync()
        {
            var deliveryOptions = _navData.GetWorseCaseDeliveryOptions();

            if (((_mobileData.Order.Type == Enums.InstructionType.Collect && _mobileData.Order.Additional.CustomerSignatureRequiredForCollection)
                    || (_mobileData.Order.Type == Enums.InstructionType.Deliver && deliveryOptions.CustomerSignatureRequiredForDelivery))
                    && string.IsNullOrWhiteSpace(CustomerSignatureEncodedImage))
            {
                await _userInteraction.AlertAsync("Signature is required");
                return;
            }

            if (((_mobileData.Order.Type == Enums.InstructionType.Collect && _mobileData.Order.Additional.CustomerNameRequiredForCollection)
                    || (_mobileData.Order.Type == Enums.InstructionType.Deliver && deliveryOptions.CustomerNameRequiredForDelivery))
                    && string.IsNullOrWhiteSpace(CustomerName))
            {
                await _userInteraction.AlertAsync("The signer's name is required");
                return;
            }

            var dataChunks = _navData.GetAllDataChunks();
            foreach (var dataChunk in dataChunks)
            {
                dataChunk.Signature = new Models.Signature { Title = CustomerName, EncodedImage = CustomerSignatureEncodedImage };

                if (dataChunk.ScannedDelivery != null)
                {
                    dataChunk.ScannedDelivery.CustomerName = CustomerName;
                    dataChunk.ScannedDelivery.HasCustomerSigned = !string.IsNullOrWhiteSpace(CustomerSignatureEncodedImage);
                }
            }

            await _navigationService.MoveToNextAsync(_navData);
        }

        #endregion

        #region BaseFragmentViewModel Overrides
        public override string FragmentTitle
        {
            get { return "Sign for " + ((_mobileData.Order.Type == Enums.InstructionType.Collect) ? "Collection" : "Delivery"); }
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
            //TODO: navigation service logic has leaked here. Need to navigation 
            if (_mobileData.Order.Type == Enums.InstructionType.Deliver)
            {
                // Delivery, or was previouslyon comments screen,  continue back using normal backstack mechanism
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
