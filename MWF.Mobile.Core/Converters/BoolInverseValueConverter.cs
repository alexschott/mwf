using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore.Converters;

namespace MWF.Mobile.Core.Converters
{

    /// <summary>
    /// Value converter that translates true to false and vice-versa.
    /// </summary>
    public class BoolInverseValueConverter : MvxValueConverter<bool, bool>
    {

        protected override bool Convert(bool value, Type targetType, object parameter, CultureInfo culture)
        {
            return !value;
        }

    }

}
