using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cirrious.MvvmCross.ViewModels;

namespace MWF.Mobile.Core.Presentation
{

    public class CloseUpToViewPresentationHint
        : MvxPresentationHint
    {
        public CloseUpToViewPresentationHint(Type viewModelType)
        {
            //todo check type is a view model type here
            ViewModelType = viewModelType;
        }

        public Type ViewModelType 
        {
            get; private set;
        }

    }

}
