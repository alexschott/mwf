using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Services
{
    public interface IDataChunkService
    {
        Task SendReadChunkAsync(IEnumerable<MobileData> instructions, Guid currentDriverID, string currentVehicleRegistration);
        Task SendDataChunkAsync(MobileApplicationDataChunkContentActivity dataChunkActivity, MobileData currentMobileData, Guid currentDriverID, string currentVehicleRegistration, bool updateQuantity = false, bool updateTrailer = false);
    }
}
