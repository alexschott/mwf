using Cirrious.MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class MenuViewModel : MvxViewModel
    {
        private MainViewModel.Option _option;
        public MainViewModel.Option Option
        {
            get { return this._option; }
            set
            {
                this._option = value;
            }
        }

        private string _text = string.Empty;
        public string Text
        {
            get { return _text; }
            set { _text = value; RaisePropertyChanged(() => Text); }
        }
        
    }
}
