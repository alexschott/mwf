using MWF.Mobile.Core.Models.Instruction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Repositories.Interfaces
{
    public interface IMobileDataRepository : IRepository<MobileData>
    {
        Task<bool> InstructionExistsAsync(Guid id);
        Task<IEnumerable<MobileData>> GetNonCompletedInstructionsAsync(Guid driverID);
        Task<IEnumerable<MobileData>> GetInProgressInstructionsAsync(Guid driverID);
        Task<IEnumerable<MobileData>> GetNotStartedInstructionsAsync(Guid driverID);
        Task<IEnumerable<MobileData>> GetNonCompletedMessagesAsync(Guid driverID);
        Task<IEnumerable<MobileData>> GetAllMessagesAsync(Guid driverID);
        Task<IEnumerable<MobileData>> GetObsoleteInstructionsAsync(int dataRetentionDays);
    }
}
