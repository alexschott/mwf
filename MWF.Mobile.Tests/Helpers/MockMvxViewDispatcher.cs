using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore.Core;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Views;

namespace MWF.Mobile.Tests.Helpers
{

    /// <summary>
    /// Pinched from https://github.com/MvvmCross/MvvmCross-Tutorials/blob/master/Sample%20-%20TwitterSearch/TwitterSearch.Test/Mocks/MockMvxViewDispatcher.cs
    /// </summary>
    public class MockMvxViewDispatcher : MvxMainThreadDispatcher, IMvxViewDispatcher
    {
        public List<IMvxViewModel> CloseRequests = new List<IMvxViewModel>();
        public List<MvxViewModelRequest> NavigateRequests = new List<MvxViewModelRequest>();

        public bool ShowViewModel(MvxViewModelRequest request)
        {
            NavigateRequests.Add(request);
            return true;
        }

        public bool ChangePresentation(MvxPresentationHint hint)
        {
            throw new NotImplementedException();
        }

        public bool RequestMainThreadAction(Action action)
        {
            action();
            return true;
        }
    }

}
