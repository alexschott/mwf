using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.ViewModels;

namespace MWF.Mobile.Core.ViewModels
{

    public abstract class BaseFragmentViewModel
        : MvxViewModel, Portable.IVisible
    {

        abstract public string FragmentTitle { get; }
        private bool _isVisible;

        #region IVisible

        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; }
        }

        #endregion IVisible

    }

}