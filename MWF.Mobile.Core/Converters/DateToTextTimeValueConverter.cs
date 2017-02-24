using Cirrious.CrossCore.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Converters
{
    public class DateToTextTimeValueConverter : MvxValueConverter<DateTime,string>
    {

        protected override string Convert(DateTime value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == default(DateTime) ? string.Empty : value.ToString("HH:mm");
        }

    }
}
