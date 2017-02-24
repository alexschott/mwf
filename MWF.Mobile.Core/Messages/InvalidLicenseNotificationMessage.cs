using Cirrious.MvvmCross.Plugins.Messenger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Messages
{
    public class InvalidLicenseNotificationMessage
        :MvxMessage
    {

        public InvalidLicenseNotificationMessage(object sender)
            :base(sender)
        {
        }

    }
}
