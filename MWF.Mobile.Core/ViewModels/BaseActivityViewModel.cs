using System;
using System.Collections.Generic;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Messages;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.ViewModels
{

    public abstract class BaseActivityViewModel 
		: MvxViewModel
    {

        private readonly IMvxViewModelLoader _viewModelLoader = null;
        private IMvxViewModel _initialViewModel = null;

        public BaseActivityViewModel(IMvxViewModelLoader viewModelLoader)
        {
            _viewModelLoader = viewModelLoader;
        }

        public IMvxViewModel InitialViewModel
        {
            get { return _initialViewModel; }
            private set { _initialViewModel = value; RaisePropertyChanged(() => InitialViewModel); }
        }

        protected void SetInitialViewModel<TViewModel>(IMvxBundle parameters = null)
            where TViewModel : IMvxViewModel
        {
            var request = MvxViewModelRequest.GetDefaultRequest(typeof(TViewModel));
            var viewModel = _viewModelLoader.LoadViewModel(request, parameters);
            this.InitialViewModel = viewModel;
        }

        protected void CloseToInitialView()
        {
            this.ChangePresentation(new Presentation.CloseToInitialViewPresentationHint());
        }

        protected void CloseUpToView<TViewModel>()
            where TViewModel : IMvxViewModel
        {
            this.ChangePresentation(new Presentation.CloseUpToViewPresentationHint(typeof(TViewModel)));
        }

        public bool Close()
        {
            return base.Close(this);
        }

    }

}
