using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore.Platform;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.Repositories
{

    public class LatestSafetyCheckRepository : Repository<LatestSafetyCheck>, ILatestSafetyCheckRepository
    {

        #region Construction

        public LatestSafetyCheckRepository(IDataService dataService)
            : base(dataService)
        { }

        #endregion

        public async Task<LatestSafetyCheck> GetForDriverAsync(Guid driverID)
        {
            var a = await this.GetAllAsync();
            return a.FirstOrDefault(lsc => lsc.DriverID == driverID);
        }

        public async Task SetForDriverAsync(LatestSafetyCheck latestSafetyCheck)
        {
            var latestSafetyCheckForDriver = await this.GetForDriverAsync(latestSafetyCheck.DriverID);

            // Delete any existing latest safety check for the driver before adding a new one.
            // Currently this deletes both vehicle and trailer safety checks and it is possible that one of these will be null in the latestSafetyCheck object - if this is the case should we actually be retaining the older safety check?
            if (latestSafetyCheckForDriver != null)
                await this.DeleteAsync(latestSafetyCheckForDriver);

            try
            {
                await this.InsertAsync(latestSafetyCheck);
            }
            catch (Exception ex)
            {
                MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "LatestSafetyCheckRepository", "InsertAsync", ex.StackTrace);
                throw;
            }
        }

    }

}
