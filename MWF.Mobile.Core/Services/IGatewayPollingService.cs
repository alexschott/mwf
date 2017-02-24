using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{

    public class PollingAction<TData>
        where TData : class
    {
        public string Command { get; set; }
        public TData Data { get; set; }
        public Models.GatewayServiceRequest.Parameter[] Parameters { get; set; }
    }

    public interface IGatewayPollingService
    {
        void StartPollingTimer();
        void StopPollingTimer();
        Task PollForInstructionsAsync();
    }

}
