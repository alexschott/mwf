using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.ViewModels
{
    public class ManifestInstructionViewModel : MvxViewModel
    {

        private readonly MobileData _mobileData;
        private readonly BaseFragmentViewModel _baseViewModel;
        private readonly INavigationService _navigationService;

        public ManifestInstructionViewModel(BaseFragmentViewModel viewModel, MobileData mobileData)
        {
            _baseViewModel = viewModel;
            _mobileData = mobileData;

            _navigationService = Mvx.Resolve<INavigationService>();
        }

        public MobileData MobileData
        {
            get { return _mobileData; }
        }

        public Guid InstructionID
        {
            get { return _mobileData.ID; }
        }

        public string RunID
        {
            get { return (InstructionType == Enums.InstructionType.OrderMessage) ? GenerateMessageTypeText() : _mobileData.Order.RouteTitle; }
        }

        public string PointDescripion
        {
			get { return (InstructionType == Enums.InstructionType.OrderMessage) ? _mobileData.MessageText : _mobileData.Order.Description; }
        }

        public DateTime ArrivalDate
        {
            get { return _mobileData.Order.Arrive; }
        }

        public Enums.InstructionType InstructionType
        {
            get { return _mobileData.Order.Type; }
        }

        public Enums.InstructionProgress ProgressState
        {
            get { return _mobileData.ProgressState; }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set 
            { 
                _isSelected = value;
                RaisePropertyChanged(() => IsSelected);
            }
        }

        private MvxCommand _selectInstructionCommand;
        public ICommand SelectInstructionCommand
        {
            get
            {
                if (_selectInstructionCommand == null)
                {
                    switch (this.InstructionType)
                    {
                        case default(Enums.InstructionType):
                            _selectInstructionCommand = null; // non-clickable blank "instruction" used in instruction sections that contain no items
                            break;

                        case Enums.InstructionType.OrderMessage:
                            _selectInstructionCommand = new MvxCommand(SelectOrderMessage);
                            break;

                        default:
                            _selectInstructionCommand = new MvxCommand(async () => await this.SelectInstructionAsync());
                            break;
                    }
                }

                return _selectInstructionCommand;
            }
        }

        private MvxCommand _toggleIsSelectedInstructionCommand;
        public ICommand ToggleIsSelectedInstructionCommand
        {
            get { return (_toggleIsSelectedInstructionCommand = _toggleIsSelectedInstructionCommand ?? new MvxCommand(ToggleIsSelectedInstruction)); }
        }

        private Task SelectInstructionAsync()
        {
            var navItem = new NavData<MobileData>() { Data = _mobileData };
            return _navigationService.MoveToNextAsync(navItem);
        }

        private void ToggleIsSelectedInstruction()
        {
            this.IsSelected = !this.IsSelected;
        }

        public void SelectOrderMessage()
        {
            this.OpenMessageModal(modalResult =>
            {
                // Update any read messages in the inbox.
                var inboxVM = _baseViewModel as InboxViewModel;

                if (inboxVM != null)
                    inboxVM.RefreshMessagesCommand.Execute(null);
            });
        }

        public void OpenMessageModal(Action<bool> callback)
        {
            var navItem = new MessageModalNavItem { MobileDataID = _mobileData.ID, IsRead = (_mobileData.ProgressState == Enums.InstructionProgress.Complete) };
            var navData = new NavData<MessageModalNavItem> { Data = navItem };
            _navigationService.ShowModalViewModel<MessageViewModel, bool>(navData, callback);
        }

        private string GenerateMessageTypeText()
        {
            if (_mobileData.Order.Addresses.Count > 0)
                return "Message with a Point";
            else
                return "Message";
        }
    }
}
