using System;
using System.Collections.Generic;
using MWF.Mobile.Core.Models;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Repositories
{

    public interface IGatewayQueueItemRepository : IRepository<GatewayQueueItem> 
    {

        Task<IEnumerable<GatewayQueueItem>> GetAllInQueueOrderAsync();
    }

}
