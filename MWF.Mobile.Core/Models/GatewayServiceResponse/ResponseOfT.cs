using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models.GatewayServiceResponse
{

    public class Response<TData>
        where TData: class
    {
        public IEnumerable<ResponseAction<TData>> Actions { get; set; }
    }


}
