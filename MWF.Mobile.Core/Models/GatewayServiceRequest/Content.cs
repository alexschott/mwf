using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models.GatewayServiceRequest
{

    public class Content
        : BaseContent
    {
        public IEnumerable<Action> Actions { get; set; }
    }

}
