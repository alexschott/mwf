using System;
using System.Collections.Generic;

namespace MWF.Mobile.Core.Extensions
{
    public static class DateTimeExtensions
    {
        
        public static string ToStringIgnoreDefaultDate(this DateTime dateTime)
        {
            //if datetime is the default date (i.e. it was never set) then return empty string
            return (dateTime.CompareTo(new DateTime()) == 0) ? string.Empty : dateTime.ToString("dd/MM/yyyy HH:mm");
        }

    }
}
