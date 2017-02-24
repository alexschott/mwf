using System;
using MWF.Mobile.Core.Enums;

namespace MWF.Mobile.Core.Helpers
{
    public class SMP
    {
        public int Mtc { get; set; }
        public int Uri { get; set; }
        public int Id { get; set; }
        public ReportReason Reason { get; set; }
        public DateTime ReportDateTime { get; set; }
        public DateTime LastFixDateTime { get; set; }
        public int Quality { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public short Heading { get; set; }
        public short Speed { get; set; }

        public override string ToString()
        {
            // <83042209E7F22E0CB787253325195001491164090038>
            //83 04 2209E7F2 2E0CB787 2 5332519500149116 409 003 8

            string SMPMessage = string.Empty;

            SMPMessage = "83"; // MTC 
            SMPMessage += ((int)this.Reason).ToString("X2"); // ReasonCode
            SMPMessage += DateTimeToSMP(this.ReportDateTime).ToString("X8"); // Last Resp
            SMPMessage += DateTimeToSMP(this.LastFixDateTime).ToString("X8"); // Last Pos
            SMPMessage += string.Format("{0:X1}", (byte)Math.Min(this.Quality, 9)); // Quality
            SMPMessage += ConvertLatLonToSMP(this.Latitude, this.Longitude); // Signs Lat Lon 
            SMPMessage += string.Format("{0:000}", this.Heading); // Heading
            SMPMessage += string.Format("{0:000}", this.Speed); // Speed


            return SMPMessage;
        }


        // this is the number of seconds since 06 Jan 1980 
        private Int32 DateTimeToSMP(DateTime rtc)
        {
            DateTime baseDate = new DateTime(1980, 01, 06, 00, 00, 00);

            Int32 seconds = (Int32)(rtc - baseDate).TotalSeconds;

            return seconds;
        }

        private string ConvertLatLonToSMP(decimal lat, decimal lon)
        {
            string retVal = string.Empty;

            Int32 sign = ((lat >= 0) ? 2 : 0) | ((lon >= 0) ? 1 : 0);

            retVal = sign.ToString();
            retVal += ConvertToRealNumber((double)lat, 6).ToString("00000000");
            retVal += ConvertToRealNumber((double)lon, 6).ToString("000000000");

            return retVal;

        }

        private Int32 ConvertToRealNumber(double value, int decimalPlaces)
        {
            double retVal = value * (System.Math.Pow(10, decimalPlaces));
            retVal = Math.Floor(retVal);

            if (retVal < 0)
                retVal = retVal * -1;

            return (Int32)retVal;
        }
    }
}