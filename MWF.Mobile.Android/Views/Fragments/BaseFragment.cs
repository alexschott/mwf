using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.OS;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Droid.FullFragging.Fragments;
using MWF.Mobile.Core.ViewModels;
using MWF.Mobile.Core.Portable;
using Android.Views.InputMethods;
using Android.Content;

namespace MWF.Mobile.Android.Views.Fragments
{
    public class BaseFragment : MvxFragment
    {
        protected MvxFragment CurrentFragment;


        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            var myActivity = (BaseActivityView)this.Activity;

            base.OnViewCreated(view, savedInstanceState);
        }

        public override void OnPause()
        {
            base.OnPause();
            this.HideKeyboard();
            this.SetVisible(false);
            
        }

        public override void OnResume()
        {
            base.OnResume();
            this.SetVisible(true);
            
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            var disposableViewModel = this.ViewModel as Core.Portable.IDisposable;

            if (disposableViewModel != null)
                disposableViewModel.Dispose();

        }


        protected void HideKeyboard()
        {
            InputMethodManager mgr = (InputMethodManager)this.Activity.GetSystemService(Context.InputMethodService);
            mgr.HideSoftInputFromWindow(this.View.WindowToken, 0);
        }

        protected void ShowSoftKeyboard()
        {
            InputMethodManager mgr = (InputMethodManager)this.Activity.GetSystemService(Context.InputMethodService);
            mgr.ShowSoftInput(this.View, ShowFlags.Forced);
            mgr.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //This closes the side menu when a new fragment is created
            SetHasOptionsMenu(true);
        }

        private void SetVisible(bool value)
        {
            var visibleViewModel = this.ViewModel as Core.Portable.IVisible;

            if (visibleViewModel != null)
                visibleViewModel.IsVisible = value;
        }
    }
}
