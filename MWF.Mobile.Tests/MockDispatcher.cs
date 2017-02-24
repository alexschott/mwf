using System;
using System.Collections.Generic;
using Cirrious.CrossCore.Core;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Views;

namespace MWF.Mobile.Tests
{

    public class MockDispatcher
        : MvxMainThreadDispatcher, IMvxViewDispatcher
    {

        public readonly ICollection<MvxViewModelRequest> Requests = new List<MvxViewModelRequest>();
        public readonly ICollection<MvxPresentationHint> Hints = new List<MvxPresentationHint>();

        public bool RequestMainThreadAction(Action action)
        {
            action();
            return true;
        }

        public bool ShowViewModel(MvxViewModelRequest request)
        {
            this.Requests.Add(request);
            return true;
        }

        public bool ChangePresentation(MvxPresentationHint hint)
        {
            this.Hints.Add(hint);
            return true;
        }

    }

}