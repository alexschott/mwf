using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Messages;
using MWF.Mobile.Core.ViewModels.Interfaces;

namespace MWF.Mobile.Core.ViewModels
{

    // View Model class that can be shown "modally", returning a result to a calling view model
    // when user has done interacting with it.
    // See http://www.gregshackles.com/2012/11/returning-results-from-view-models-in-mvvmcross/

    public abstract class BaseModalViewModel<TResult> : BaseFragmentViewModel, IModalViewModel<TResult>
    {

        private IMvxMessenger _messenger;

        private IMvxMessenger Messenger
        {
            get { return (_messenger = _messenger ?? Mvx.Resolve<IMvxMessenger>()); }
        }

        //Subclasses should call this method during their Init to ensure
        //the message id used to sync message passing is correct.
        public void SetMessageID(Guid messageId)
        {
            if (messageId != Guid.Empty)
                this.MessageId = messageId;
        }

        public Guid MessageId { get; set; }

        public void Cancel()
        {
            ReturnResult(default(TResult));
        }

        public void ReturnResult(TResult result)
        {
            var message = new ModalNavigationResultMessage<TResult>(this, MessageId, result);

            this.Messenger.Publish(message);
            this.Close(this);
        }
    }
}
