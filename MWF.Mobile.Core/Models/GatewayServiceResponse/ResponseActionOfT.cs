using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models.GatewayServiceResponse
{

    public class ResponseAction<TData>
        : ResponseAction
        where TData : class
    {
        public TData Data { get; set; }
        public List<string> Errors { get; set; }
    }

}
