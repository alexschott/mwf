using MWF.Mobile.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Models.Instruction;

namespace MWF.Mobile.Core.Services
{
    public interface IDiagnosticsService
    {
        Task<bool> UploadDiagnosticsAsync(string databasePath);
    }
}
