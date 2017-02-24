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
    public class ScanStateEnumValueConverter : MvxBaseVisibilityValueConverter<Enums.ScanState>
    {
        protected override MvxVisibility Convert(Enums.ScanState value, object parameter, CultureInfo culture)
        {
            return (value.ToString() == (string)parameter) ? MvxVisibility.Visible : MvxVisibility.Collapsed;
        }
    }
}
