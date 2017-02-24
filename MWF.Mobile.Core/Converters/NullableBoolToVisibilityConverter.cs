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

    public class NullableBoolToVisibilityConverter : MvxBaseVisibilityValueConverter<bool?>
    {
        protected override MvxVisibility Convert(bool? value, object parameter, CultureInfo culture)
        {
            if (parameter is string)
            {
                string paramString = (string) parameter;

                if (paramString == "NotNull")
                    return (value != null) ? MvxVisibility.Visible : MvxVisibility.Collapsed;

                throw new ArgumentException("Unknown converter parameter value: " + paramString);
            }

            return (value == (bool?)parameter) ? MvxVisibility.Visible : MvxVisibility.Collapsed;
        }
    }
}
