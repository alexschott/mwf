using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models.GatewayServiceRequest
{

    public class Action
    {
        public Guid? ActionID { get; set; }
        public string Command { get; set; }
        public bool? IncludeIDs { get; set; }
        public string ContentXml { get; set; }
        public IEnumerable<Parameter> Parameters { get; set; }
    }

}
