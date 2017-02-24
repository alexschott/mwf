using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;

namespace MWF.Mobile.Core.Messages
{

    public class ModalNavigationResultMessage<TResult> : MvxMessage
    {
        public TResult Result { get; private set; }
        public Guid MessageId { get; set; }

        public ModalNavigationResultMessage(object sender, Guid messageId, TResult result)
            : base(sender)
        {
            Result = result;
            MessageId = messageId;
        }
    }
}
