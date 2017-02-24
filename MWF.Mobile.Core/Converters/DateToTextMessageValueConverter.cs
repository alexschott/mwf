using Cirrious.CrossCore.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Converters
{
    public class DateToTextMessageValueConverter : MvxValueConverter<DateTime,string>
    {
        protected override string Convert(DateTime value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == default(DateTime))
                return string.Empty;

            if (DateTime.Today == value.Date)
                return "Today";

            if (DateTime.Today.AddDays(1) == value.Date)
                return "Tomorrow";

            return value.Date.ToLocalTime().ToString("dd/MM/yyyy");
        }

    }
}
