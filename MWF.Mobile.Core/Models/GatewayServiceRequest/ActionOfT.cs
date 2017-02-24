using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models.GatewayServiceRequest
{

    public class Action<TData>
        : Action
        where TData : class
    {
        public TData Data { get; set; }
    }

}
