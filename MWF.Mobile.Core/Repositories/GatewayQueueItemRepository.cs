using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.Repositories
{

    public class GatewayQueueItemRepository : Repository<GatewayQueueItem>, IGatewayQueueItemRepository
    {

        #region Construction

        public GatewayQueueItemRepository(IDataService dataService)
            : base(dataService)
        { }

        #endregion

        public async Task<IEnumerable<GatewayQueueItem>> GetAllInQueueOrderAsync()
        {
            var r = await this.GetAllAsync();
            IOrderedEnumerable<GatewayQueueItem> retVal = null;
            if (r != null && r.Count() > 0)
            {retVal = r.OrderBy(gqi => gqi.QueuedDateTime); }

            return retVal;
        }

    }

}
