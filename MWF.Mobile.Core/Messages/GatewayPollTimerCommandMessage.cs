using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Messenger;

namespace MWF.Mobile.Core.Messages
{
    public class GatewayPollTimerCommandMessage : MvxMessage
    {
        public enum TimerCommand { Start, Stop, Reset, Trigger };

        public GatewayPollTimerCommandMessage(object sender, TimerCommand command) : base(sender)
        {
            Command = command;
        }

        public TimerCommand Command { get; private set; }
    }
}