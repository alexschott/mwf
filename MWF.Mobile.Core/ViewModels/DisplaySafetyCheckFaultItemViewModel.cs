using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{
    public class DisplaySafetyCheckFaultItemViewModel
        : MvxViewModel
    {

        private string _faultType;
        public string FaultType
        {
            get { return _faultType; }
            set { _faultType = value; RaisePropertyChanged(() => FaultType); }
        }

        private string _faultCheckTitle;
        public string FaultCheckTitle
        {
            get { return _faultCheckTitle; }
            set { _faultCheckTitle = value; RaisePropertyChanged(() => FaultCheckTitle); }
        }

        private string _faultCheckComment;
        public string FaultCheckComment
        {
            get { return _faultCheckComment; }
            set { _faultCheckComment = value; RaisePropertyChanged(() => FaultCheckComment); }
        }

        private string _faultStatus;
        public string FaultStatus
        {
            get { return _faultStatus; }
            set { _faultStatus = value; RaisePropertyChanged(() => FaultStatus); }
        }

    }
}
