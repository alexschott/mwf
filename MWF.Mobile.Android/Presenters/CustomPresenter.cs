using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.MvvmCross.Droid.Views;
using Cirrious.MvvmCross.Droid.FullFragging.Fragments;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.Presentation;
using Android.Content;

namespace MWF.Mobile.Android.Presenters
{

    public interface IFragmentHost
    {
        event EventHandler OnFragmentChanged;

        bool Show(MvxViewModelRequest request);
        bool Close(IMvxViewModel viewModel);
        MvxFragment CurrentFragment { get; }
        void CloseUpToView(Type viewModelType);
        void CloseToInitialView();
        int FragmentHostID { get; }
        IDictionary<Type, Type> SupportedFragmentViewModels { get; }
    }


    /// <summary>
    /// Custom presenter allowing fragments to be rendered within activities using MVVMCross's ShowViewModel.
    /// Activities that can contain fragments should implement IFragmentHost.
    /// </summary>
    public class CustomPresenter
        : MvxAndroidViewPresenter, ICustomPresenter
    {

        public override void Show(MvxViewModelRequest request)
        {
            var currentFragmentHost = this.Activity as IFragmentHost;

            if (currentFragmentHost != null)
                if (currentFragmentHost.Show(request))
                    return;

            base.Show(request);
        }

        public override void Close(IMvxViewModel viewModel)
        {
            var currentFragmentHost = this.Activity as IFragmentHost;

            if (currentFragmentHost != null)
                if (currentFragmentHost.Close(viewModel))
                    return;

            base.Close(viewModel);
        }

        public override void ChangePresentation(MvxPresentationHint hint)
        {
            if (hint is Core.Presentation.CloseToInitialViewPresentationHint)
            {
                var currentFragmentHost = this.Activity as IFragmentHost;

                if (currentFragmentHost != null)
                {
                    currentFragmentHost.CloseToInitialView();
                    return;
                }
            }
            else
            {

                if (hint is Core.Presentation.CloseUpToViewPresentationHint)
                {
                    var currentFragmentHost = this.Activity as IFragmentHost;

                    if (currentFragmentHost != null)
                    {
                        currentFragmentHost.CloseUpToView((hint as CloseUpToViewPresentationHint).ViewModelType);

                        return;
                    }
                }

            }

            base.ChangePresentation(hint);
        }

        public BaseActivityViewModel CurrentActivityViewModel
        {
            get
            {
                return (this.Activity as MvxActivity).DataContext as BaseActivityViewModel;
            }
        }

        public MvxViewModel CurrentFragmentViewModel
        {
            get
            {
                IFragmentHost host = this.Activity as IFragmentHost;
                return host.CurrentFragment.DataContext as MvxViewModel;
            }
        }

    }

}