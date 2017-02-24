using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Core.Presentation
{
    public interface ICustomPresenter
    {
        BaseActivityViewModel CurrentActivityViewModel { get; }
        MvxViewModel CurrentFragmentViewModel { get; }
    }
}
