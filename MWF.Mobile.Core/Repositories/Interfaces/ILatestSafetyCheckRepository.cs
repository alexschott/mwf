using System;
using System.Collections.Generic;
using MWF.Mobile.Core.Models;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Repositories
{

    public interface ILatestSafetyCheckRepository : IRepository<LatestSafetyCheck> 
    {
        Task<LatestSafetyCheck> GetForDriverAsync(Guid driverID);
        Task SetForDriverAsync(LatestSafetyCheck latestSafetyCheck);
    }

}
