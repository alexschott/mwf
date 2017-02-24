using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;

namespace MWF.Mobile.Core.Messages
{
    public class GatewayPollTimerElapsedMessage : MvxMessage
    {
        public GatewayPollTimerElapsedMessage(object sender) : base(sender) { }
    }
}