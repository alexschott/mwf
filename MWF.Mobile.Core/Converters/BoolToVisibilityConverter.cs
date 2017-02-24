using Cirrious.CrossCore.UI;
using Cirrious.MvvmCross.Plugins.Visibility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Converters
{
    public class BoolToVisibilityConverter : MvxBaseVisibilityValueConverter<bool>
    {
        protected override MvxVisibility Convert(bool value, object parameter, CultureInfo culture)
        {
            return (value == (bool)parameter) ? MvxVisibility.Visible : MvxVisibility.Collapsed;
        }
    }
}
