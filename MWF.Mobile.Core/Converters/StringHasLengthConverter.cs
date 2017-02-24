using System;
using System.Globalization;
using Cirrious.CrossCore.Converters;

namespace MWF.Mobile.Core.Converters
{
    public class StringHasLengthConverter : MvxValueConverter<string, bool>
    {
        protected override bool Convert(string value, Type targetType, object parameter, CultureInfo cultureInfo)
        {
            return (value.Length > 0);
        }
    }
}