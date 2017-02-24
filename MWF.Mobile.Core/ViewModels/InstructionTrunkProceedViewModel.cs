using Cirrious.CrossCore;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.ViewModels.Extensions;

namespace MWF.Mobile.Core.ViewModels
{
    public class InstructionTrunkProceedViewModel
        : BaseInstructionNotificationViewModel
    {
        #region Private Members

        private MobileData _mobileData;
        private NavData<MobileData> _navData;
        private IInfoService _infoService;
        private IRepositories _repositories;
        private INavigationService _navigationService;

        private MvxCommand _completeInstructionCommand;

        #endregion Private Members

        #region Construction

        public InstructionTrunkProceedViewModel(INavigationService navigationService, IRepositories repositories, IInfoService infoService)
        {
            _navigationService = navigationService;
            _infoService = infoService;
            _repositories = repositories;
        }

        public void Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);
            _mobileData = _navData.Data;
        }

        #endregion Construction

        #region Public Properties

        public string RunID { get { return _mobileData.Order.RouteTitle; } }

        public string ArriveDepartDateTime { get { return _mobileData.Order.Arrive.ToStringIgnoreDefaultDate(); } }

        public string Address { get { return _mobileData.Order.Addresses[0].Lines.Replace("|", "\n") + "\n" + _mobileData.Order.Addresses[0].Postcode; } }

        public string ArriveDepartLabelText { get { return (IsTrunkTo) ? "Arrive" : "Depart"; } }

        public string AddressLabelText { get { return "Address"; } }

        public string ProgressButtonText { get { return "Complete"; } }

        public bool IsTrunkTo { get { return _mobileData.Order.Type == Enums.InstructionType.TrunkTo; } }

        public ICommand CompleteInstructionCommand
        {
            get { return (_completeInstructionCommand = _completeInstructionCommand ?? new MvxCommand(async () => await this.CompleteInstructionAsync())); }
        }

        #endregion Public Properties

        #region Private Methods

        public Task CompleteInstructionAsync()
        {
            return _navigationService.MoveToNextAsync(_navData);
        }

        #endregion Private Methods

        #region BaseInstructionNotificationViewModel

        public override Task CheckInstructionNotificationAsync(GatewayInstructionNotificationMessage message)
        {
            return this.RespondToInstructionNotificationAsync(message, _navData, () =>
            {
                _mobileData = _navData.Data;
                RaiseAllPropertiesChanged();
            });
        }

        #endregion BaseInstructionNotificationViewModel

        #region BaseFragmentViewModel Overrides

        public override string FragmentTitle { get { return (IsTrunkTo) ? "Trunk To" : "Proceed From"; } }

        #endregion  BaseFragmentViewModel Overrides


    }
}
