using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Repositories.Interfaces;
using MWF.Mobile.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Repositories
{
    public class CurrentDriverRepository : Repository<CurrentDriver>, ICurrentDriverRepository
    {

        #region Construction

        public CurrentDriverRepository(IDataService dataService)
            : base(dataService)
        { }


        #endregion

    }
}
    
