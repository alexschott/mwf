using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MWF.Mobile.Core.ViewModels
{
    public class InboxViewModel
        : BaseInstructionNotificationViewModel
    {

        private IMobileDataRepository _mobileDataRepository;
        private IInfoService _infoService;
        private INavigationService _navigationService;
        private IGatewayPollingService _gatewayPollingService;
        private MvxCommand _refreshMessagesCommand;

        public InboxViewModel(
            IRepositories repositories, 
            IInfoService infoService, 
            INavigationService navigationService,
            IGatewayPollingService gatewayPollingService)
        {
            _mobileDataRepository = repositories.MobileDataRepository;
            _infoService = infoService;
            _navigationService = navigationService;
            _gatewayPollingService = gatewayPollingService;
        }

        public async Task Init()
        {
            await this.RefreshMessagesAsync();
        }

        private ObservableCollection<ManifestInstructionViewModel> _messages = new ObservableCollection<ManifestInstructionViewModel>();
        public ObservableCollection<ManifestInstructionViewModel> Messages
        {
            get { return _messages; }
        }

        public string InboxHeaderText
        {
            get { return "Showing " + MessagesCount + " messages"; }
        }

        public int MessagesCount
        {
            get { return Messages.ToList().Count; }
        }

        public ICommand RefreshMessagesCommand
        {
            get { return (_refreshMessagesCommand = _refreshMessagesCommand ?? new MvxCommand(async () => await this.RefreshMessagesAsync())); }
        }

        public async Task RefreshMessagesAsync()
        {
            await _gatewayPollingService.PollForInstructionsAsync();
            await this.ReloadPageAsync();
        }

        private async Task ReloadPageAsync()
        {
            //Show messages that are no older than a week
            var allMessages = await _mobileDataRepository.GetAllMessagesAsync(_infoService.CurrentDriverID.Value);

            var messages = allMessages
                .Where(i => i.EffectiveDate > DateTime.Today.AddDays(-7))
                .Select(m => new ManifestInstructionViewModel(this, m))
                .OrderBy(m => m.ProgressState)
                .ThenBy(m => m.ArrivalDate);

            InvokeOnMainThread(() =>
            {
                _messages.Clear();

                foreach (var message in messages)
                {
                    _messages.Add(message);
                }

                RaisePropertyChanged(() => Messages);
                RaisePropertyChanged(() => MessagesCount);
                RaisePropertyChanged(() => InboxHeaderText);
            });
        }

        #region BaseFragmentViewModel Overrides

        public override string FragmentTitle
        {
            get { return "Inbox"; }
        }

        #endregion

        #region BaseInstructionNotificationViewModel Overrides

        public override Task CheckInstructionNotificationAsync(Messages.GatewayInstructionNotificationMessage message)
        {
            return this.ReloadPageAsync();
        }

        #endregion BaseInstructionNotificationViewModel Overrides

    }
}
