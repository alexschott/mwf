using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;
using MWF.Mobile.Core.ViewModels.Interfaces;
using MWF.Mobile.Core.ViewModels.Navigation.Extensions;

namespace MWF.Mobile.Core.ViewModels
{

    /// <summary>
    /// This base class is used to recieve notifications for when instructions that are being viewed are updated or deleted.
    /// </summary>
    public abstract class BaseInstructionNotificationViewModel
        : BaseFragmentViewModel, MWF.Mobile.Core.Portable.IDisposable, IInstructionNotificationViewModel
    {

        private MvxSubscriptionToken _notificationToken;
        private IMvxMessenger _messenger;

        public BaseInstructionNotificationViewModel()
        {
            _notificationToken = Messenger.Subscribe<Messages.GatewayInstructionNotificationMessage>(async m => await CheckInstructionNotificationAsync(m));
        }

        protected IMvxMessenger Messenger
        {
            get { return (_messenger = _messenger ?? Mvx.Resolve<IMvxMessenger>()); }
        }

        public void UnsubscribeNotificationToken()
        {
            if (_notificationToken != null)
                Messenger.Unsubscribe<Messages.GatewayInstructionNotificationMessage>(_notificationToken);
        }

        public abstract Task CheckInstructionNotificationAsync(Messages.GatewayInstructionNotificationMessage message);

        public void Dispose()
        {
            UnsubscribeNotificationToken();
        }

    }

}
