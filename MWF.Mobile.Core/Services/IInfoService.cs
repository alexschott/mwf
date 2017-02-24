using System;
using System.Collections.Generic;
using MWF.Mobile.Core.Models;

namespace MWF.Mobile.Core.Services
{

    public interface IInfoService
    {

        Guid? CurrentDriverID { get; set; }
        string CurrentDriverDisplayName { get; set; }
        Guid? CurrentVehicleID { get; set; }
        string CurrentVehicleRegistration { get; set; }
        Guid? CurrentTrailerID { get; set; }
        string CurrentTrailerRegistration { get; set; }
        int Mileage { get; set; }

        void SetCurrentDriver(Driver driver);
        void SetCurrentVehicle(Vehicle vehicle);
        void SetCurrentTrailer(Trailer trailer);

        void Clear();

    }

}
