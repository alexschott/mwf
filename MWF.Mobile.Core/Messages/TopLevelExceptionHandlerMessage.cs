using Cirrious.MvvmCross.Plugins.Messenger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Messages
{
    public class TopLevelExceptionHandlerMessage : MvxMessage
    {
        public TopLevelExceptionHandlerMessage(object sender, Exception ex) : base(sender) 
        {
            TopLevelException = ex;
        }

        public Exception TopLevelException { get; set; }


    }
}
