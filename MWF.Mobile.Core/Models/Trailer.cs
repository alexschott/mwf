using System;
using SQLite.Net.Attributes;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models
{

    public class Trailer : BaseVehicle
    {

        public Trailer()
            : base()
        {  }

        public Trailer (BaseVehicle baseVehicle)
            : base()
        {
            this.ID = baseVehicle.ID;
            this.Title = baseVehicle.Title;
            this.Registration = baseVehicle.Registration;
            this.SafetyCheckProfileIntLink = baseVehicle.SafetyCheckProfileIntLink;
            this.IsTrailer = IsTrailer;
        }

        public static bool SameAs(Trailer trailer, Instruction.Trailer instructionTrailer)
        {
            if (trailer == null || instructionTrailer == null)
            {
                if (trailer == null && instructionTrailer != null)
                    return true;
                return (trailer == null || instructionTrailer == null);
            }

            return trailer.Registration == instructionTrailer.TrailerId;
        }

    }

}
