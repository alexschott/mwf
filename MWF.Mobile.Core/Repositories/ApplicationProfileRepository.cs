using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Services;
using System.Diagnostics;

namespace MWF.Mobile.Core.Repositories
{

    public class ApplicationProfileRepository : Repository<ApplicationProfile>, IApplicationProfileRepository
    {

        #region Construction

        public ApplicationProfileRepository(IDataService dataService)
            : base(dataService)
        { }

        public async Task<ApplicationProfile> GetAsync()
        {
            var data = await this.GetAllAsync();
			Debug.Assert(data.Count() == 1, string.Format("Expected one ApplicationProfile record but found {0}", data.Count()));
            return data.First();
        }

        #endregion

    }

}
