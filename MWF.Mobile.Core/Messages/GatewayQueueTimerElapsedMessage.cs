using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;

namespace MWF.Mobile.Core.Messages
{
    
    public class GatewayQueueTimerElapsedMessage
        : MvxMessage
    {

        public GatewayQueueTimerElapsedMessage(object sender)
            : base(sender)
        {
        }

    }

}
