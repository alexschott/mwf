using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MWF.Mobile.Core.Models;

namespace MWF.Mobile.Core.Services
{

    // Simple class for in-memory storage of useful information e.g. current driver, vehicle, trailer etc
    public class InfoService
        : IInfoService
    {

        public Guid? CurrentDriverID { get; set; }
        public string CurrentDriverDisplayName { get; set; }
        public Guid? CurrentVehicleID { get; set; }
        public string CurrentVehicleRegistration { get; set; }
        public Guid? CurrentTrailerID { get; set; }
        public string CurrentTrailerRegistration { get; set; }
        public int Mileage { get; set; }

        public void SetCurrentDriver(Driver driver)
        {
            this.CurrentDriverID = driver == null ? (Guid?)null : driver.ID;
            this.CurrentDriverDisplayName = driver == null ? null : driver.DisplayName;
        }

        public void SetCurrentVehicle(Vehicle vehicle)
        {
            this.CurrentVehicleID = vehicle == null ? (Guid?)null : vehicle.ID;
            this.CurrentVehicleRegistration = vehicle == null ? null : vehicle.Registration;
        }

        public void SetCurrentTrailer(Trailer trailer)
        {
            this.CurrentTrailerID = trailer == null ? (Guid?)null : trailer.ID;
            this.CurrentTrailerRegistration = trailer == null ? null : trailer.Registration;
        }

        public void Clear()
        {
            this.SetCurrentDriver(null);
            this.SetCurrentVehicle(null);
            this.SetCurrentTrailer(null);
        }

    }

}
