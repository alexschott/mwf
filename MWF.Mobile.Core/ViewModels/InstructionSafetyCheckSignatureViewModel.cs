using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Core.ViewModels
{

    public class InstructionSafetyCheckSignatureViewModel : SafetyCheckSignatureViewModel
    {

        public InstructionSafetyCheckSignatureViewModel(Services.IInfoService infoService, Services.IGatewayQueuedService gatewayQueuedService, ICustomUserInteraction userInteraction, Repositories.IRepositories repositories, INavigationService navigationService, ISafetyCheckService safetyCheckService)
            : base(infoService, gatewayQueuedService, userInteraction, repositories, navigationService, safetyCheckService)
        {
        }

        public override Task Init()
        {
            // Don't call down to base Init method: what it does is not relevant for instruction safety checks, there is corresponding code specifically for instructions in Init(navData) below.
            return Task.FromResult(0);
        }

        public async Task Init(Guid navID)
        {
            _navData = _navigationService.GetNavData<MobileData>(navID);

            // Retrieve the vehicle and trailer safety check data 
            // vehicle from start up service, trailer from navData
            var vehicleSafetyCheckData = _safetyCheckService.CurrentVehicleSafetyCheckData;
            var trailerSafetyCheckData = _navData.OtherData["UpdatedTrailerSafetyCheckData"] as SafetyCheckData;

            _safetyCheckData = _safetyCheckService.GetSafetyCheckData(vehicleSafetyCheckData, trailerSafetyCheckData);

            if (!_safetyCheckData.Any())
                throw new Exception("Invalid application state - signature screen should not be displayed in cases where there are no safety checks.");

            var vehicle = await _repositories.VehicleRepository.GetByIDAsync(_infoService.CurrentVehicleID.Value);
            var trailer = _navData.OtherData["UpdatedTrailer"] as Models.Trailer;

            await this.PopulateViewModelAsync(vehicle, trailer);
        }

    }

}
