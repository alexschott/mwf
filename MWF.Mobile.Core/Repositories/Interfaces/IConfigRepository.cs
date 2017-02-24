using MWF.Mobile.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Repositories
{
    public interface IConfigRepository : IRepository<MWFMobileConfig>
    {
        Task<MWFMobileConfig> GetAsync();
    }
}
