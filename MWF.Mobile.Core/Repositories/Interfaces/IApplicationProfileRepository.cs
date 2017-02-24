using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MWF.Mobile.Core.Models;

namespace MWF.Mobile.Core.Repositories
{
    public interface IApplicationProfileRepository : IRepository<ApplicationProfile> 
    {
        Task<ApplicationProfile> GetAsync();
    }
}
