using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using MWF.Mobile.Core.Models;

namespace MWF.Mobile.Core.Services
{
    public class SafetyCheckService : MWF.Mobile.Core.Services.ISafetyCheckService
    {
        #region private members

        private readonly Repositories.IRepositories _repositories = null;
        private readonly IGatewayQueuedService _gatewayQueuedService = null;
        private readonly IGpsService _gpsService = null;
        private readonly IInfoService _infoService = null;

        #endregion

        #region construction

        public SafetyCheckService(Repositories.IRepositories repositories, IGatewayQueuedService gatewayQueuedService, IGpsService gpsService, IInfoService infoService)
        {
            _repositories = repositories;
            _gatewayQueuedService = gatewayQueuedService;
            _gpsService = gpsService;
            _infoService = infoService;
        }

        #endregion

        #region public properties

        public SafetyCheckData CurrentVehicleSafetyCheckData { get; set; }
        public SafetyCheckData CurrentTrailerSafetyCheckData { get; set; }

        #endregion

        #region public methods

        public IEnumerable<Models.SafetyCheckData> GetCurrentSafetyCheckData()
        {
            return GetSafetyCheckData(this.CurrentVehicleSafetyCheckData, this.CurrentTrailerSafetyCheckData);
        }

        public IEnumerable<Models.SafetyCheckData> GetSafetyCheckData(Models.SafetyCheckData vehicleSafetyCheckData, Models.SafetyCheckData trailerSafetyCheckData)
        {
            var retVal = new List<Models.SafetyCheckData>(2);

            if (vehicleSafetyCheckData != null && vehicleSafetyCheckData.Faults.Any())
                retVal.Add(vehicleSafetyCheckData);

            if (trailerSafetyCheckData != null && trailerSafetyCheckData.Faults.Any())
                retVal.Add(trailerSafetyCheckData);

            return retVal;
        }

        public async Task CommitSafetyCheckDataAsync(bool trailerOnly = false)
        {
            // Add the safety checks to the gateway queue
            var safetyCheckData = this.GetCurrentSafetyCheckData();

            var safetyCheckFailed = false;

            // Store the latest safety check so the driver can display it at a later point if required
            var latestSafetyCheck = new LatestSafetyCheck { DriverID = _infoService.CurrentDriverID.Value };

            if (safetyCheckData.Any())
            {
                var overallStatus = SafetyCheckData.GetOverallStatus(safetyCheckData.Select(scd => scd.GetOverallStatus()));
                safetyCheckFailed = overallStatus == Enums.SafetyCheckStatus.Failed;

                // Submit the safety checks to the gateway service
                var smp = _gpsService.GetSmpData(Enums.ReportReason.SafetyReport);

                // Don't include milliseconds in the EffectiveDate submitted to BlueSphere
                var effectiveDateTime = DateTime.Now;
                effectiveDateTime = effectiveDateTime.AddMilliseconds(-effectiveDateTime.Millisecond);

                if (this.CurrentVehicleSafetyCheckData != null && this.CurrentVehicleSafetyCheckData.Faults.Any())
                {
                    latestSafetyCheck.VehicleSafetyCheck = this.CurrentVehicleSafetyCheckData;

                    if (!trailerOnly)
                        latestSafetyCheck.VehicleSafetyCheck.EffectiveDate = effectiveDateTime;
                }

                if (this.CurrentTrailerSafetyCheckData != null && this.CurrentTrailerSafetyCheckData.Faults.Any())
                {
                    latestSafetyCheck.TrailerSafetyCheck = this.CurrentTrailerSafetyCheckData;
                    latestSafetyCheck.TrailerSafetyCheck.EffectiveDate = effectiveDateTime;
                }

                await _repositories.LatestSafetyCheckRepository.SetForDriverAsync(latestSafetyCheck);

                // Submit the safety check data to BlueSphere
                var safetyCheckDataToSubmit = new List<SafetyCheckData>(safetyCheckData.Count());

                foreach (var scd in safetyCheckData)
                {
                    if (trailerOnly && !scd.IsTrailer)
                        continue;

                    var safetyCheck = Models.SafetyCheckData.ShallowCopy(scd);

                    // Passed safety check items shouldn't be submitted to the gateway service, only Fails and Discretionary Passes.
                    safetyCheck.Faults = scd.Faults.Where(scf => scf.Status != Enums.SafetyCheckStatus.Passed).ToList();

                    // Add the SMP, mileage and effective-date to the safety check
                    safetyCheck.SMP = smp;
                    safetyCheck.Mileage = _infoService.Mileage;
                    safetyCheck.EffectiveDate = effectiveDateTime;

                    safetyCheckDataToSubmit.Add(safetyCheck);
                }

                if (safetyCheckDataToSubmit.Any())
                {
                    var actions = safetyCheckDataToSubmit.Select(scd => new Models.GatewayServiceRequest.Action<Models.SafetyCheckData> { Command = "fwSetSafetyCheckData", Data = scd });
                    await _gatewayQueuedService.AddToQueueAsync(actions);
                }
            }
            else
            {
                await _repositories.LatestSafetyCheckRepository.SetForDriverAsync(latestSafetyCheck);
            }
        }

        #endregion


    }
}
