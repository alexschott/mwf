using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Plugins.Visibility;
using MWF.Mobile.Core.ViewModels;
using Cirrious.CrossCore.UI;
using System.Globalization;

namespace MWF.Mobile.Core.Converters
{
    public class InstructionTypeEnumValueConverter : MvxBaseVisibilityValueConverter<Enums.InstructionType>
    {
        protected override MvxVisibility Convert(Enums.InstructionType value, object parameter, CultureInfo culture)
        {
            return (value.ToString() == (string)parameter) ? MvxVisibility.Visible : MvxVisibility.Collapsed;
        }
    }
}
