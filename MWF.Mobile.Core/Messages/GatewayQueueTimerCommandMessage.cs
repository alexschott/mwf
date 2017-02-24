using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;

namespace MWF.Mobile.Core.Messages
{
    
    public class GatewayQueueTimerCommandMessage
        : MvxMessage
    {

        public enum TimerCommand { Start, Stop, Reset, Trigger };

        public GatewayQueueTimerCommandMessage(object sender, TimerCommand command)
            : base(sender)
        {
            this.Command = command;
        }

        public TimerCommand Command { get; private set; }

    }

}
