using System;
using MWF.Mobile.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{
    public interface ISafetyCheckService
    {
        Task CommitSafetyCheckDataAsync(bool trailerOnly = false);
        SafetyCheckData CurrentTrailerSafetyCheckData { get; set; }
        SafetyCheckData CurrentVehicleSafetyCheckData { get; set; }
        IEnumerable<MWF.Mobile.Core.Models.SafetyCheckData> GetCurrentSafetyCheckData();
        IEnumerable<MWF.Mobile.Core.Models.SafetyCheckData> GetSafetyCheckData(SafetyCheckData vehicleSafetyCheckData, SafetyCheckData trailerSafetyCheckData);
    }
}
