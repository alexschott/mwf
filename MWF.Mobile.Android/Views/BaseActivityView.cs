using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Renderscripts;
using Android.Widget;
using Cirrious.CrossCore;
using Cirrious.CrossCore.WeakSubscription;
using Cirrious.MvvmCross.Droid.FullFragging;
using Cirrious.MvvmCross.Droid.FullFragging.Fragments;
using Cirrious.MvvmCross.Droid.Views;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Core.ViewModels;
using Cirrious.MvvmCross.Binding.BindingContext;
using MWF.Mobile.Android.Views.Fragments;
using Type = System.Type;
using MWF.Mobile.Core.ViewModels.Interfaces;
using Android.Support.V4.Widget;
using Android.Content;
using MWF.Mobile.Android.Portable;
using MWF.Mobile.Android.Helpers;
using MWF.Mobile.Android.Controls;
using Android.Views;
using Cirrious.CrossCore.Platform;

namespace MWF.Mobile.Android.Views
{

    [Activity]
    public abstract class BaseActivityView
        : MvxActivity, Presenters.IFragmentHost
    {

        public event EventHandler OnFragmentChanged;

        #region Protected/Private Fields

        protected IDictionary<Type, Type> _supportedFragmentViewModels;

        #endregion Protected/Private Fields

        #region Construction

        protected BaseActivityViewModel BaseActivityViewModel
        {
            get { return (BaseActivityViewModel)this.ViewModel; }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.BaseActivityViewModel.WeakSubscribe(() => this.BaseActivityViewModel.InitialViewModel, (s, e) => this.PopulateFrameLayoutWithInitialViewModelFragment());

            this.PopulateFrameLayoutWithInitialViewModelFragment();
        }

        private void PopulateFrameLayoutWithInitialViewModelFragment()
        {
            var initialViewModel = this.BaseActivityViewModel.InitialViewModel;

            if (initialViewModel != null)
            {
                var fragmentType = GetFragmentTypeForViewModel(initialViewModel.GetType());

                var fragment = (MvxFragment)Activator.CreateInstance(fragmentType);
                fragment.ViewModel = initialViewModel;
                this.NavigateToFragment(fragment, addToBackStack: false);
            }
        }

        public override void StartActivity(Intent intent)
        {
            //Close the current activity after you create a new one. Meaning you can close the app from anywhere.
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.PutExtra("EXIT", true);
            base.StartActivity(intent);
        }

        #endregion Construction

        #region Public Methods

        public async override void OnBackPressed()
        {
            if (CurrentFragment.DataContext != null)
            {
                if (CurrentFragment.DataContext is IBackButtonHandler)
                {
                    var continueBack = await (CurrentFragment.DataContext as IBackButtonHandler).OnBackButtonPressedAsync();

                    if (continueBack)
                        base.OnBackPressed();
                }
            }
            else
            {
                base.OnBackPressed();
            }

            this.FragmentChanged();
        }

        public virtual void FragmentChanged(MvxFragment fragment = null)
        {
            this.SetActivityTitleFromFragment(fragment ?? this.CurrentFragment);

            if (this.OnFragmentChanged != null)
                this.OnFragmentChanged(this, EventArgs.Empty);
        }

        #endregion Public Methods

        #region Fragment Host

        public abstract IDictionary<Type, Type> SupportedFragmentViewModels { get; }

        public abstract int FragmentHostID { get; }

        public bool Show(MvxViewModelRequest request)
        {
            // At this point simply display any supported fragment in the FrameLayout.
            // In future we may change this for different device form factors, for example to display tiled fragments.
            MvxFragment fragment = null;

            if (SupportedFragmentViewModels.ContainsKey(request.ViewModelType))
            {
                var fragmentType = SupportedFragmentViewModels[request.ViewModelType];
                fragment = (MvxFragment)Activator.CreateInstance(fragmentType);
                fragment.LoadViewModelFrom(request, null);
            }

            if (fragment == null)
                return false;

            this.NavigateToFragment(fragment, addToBackStack: true);

            return true;
        }

        /// <summary>
        /// Given a view model attempts to close the fragment associated with it. If the fragment
        /// associated is currently being displayed in the fragment host then the backstack
        /// is popped.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public bool Close(IMvxViewModel viewModel)
        {
            var fragmentTypeToClose = SupportedFragmentViewModels[viewModel.GetType()];

            if (CurrentFragment != null && CurrentFragment.GetType() == fragmentTypeToClose)
            {
                FragmentManager.PopBackStackImmediate();

                this.FragmentChanged();

                return true;
            }
            else return false;
        }

        /// <summary>
        /// Remove all fragments from the back stack up to, but not including, the initial view.
        /// </summary>
        public void CloseToInitialView()
        {
            var backStackEntryCount = FragmentManager.BackStackEntryCount;

            for (var i = 0; i < backStackEntryCount; i++)
            {
                FragmentManager.PopBackStackImmediate();
            }

            this.FragmentChanged();
        }

        /// <summary>
        /// Remove all fragments from the back stack up to, but not including the specified view.
        /// </summary>
        /// <remarks>
        /// If the specified view is not found then all fragments will be removed up to, but not including, the initial view.
        /// </remarks>
        public void CloseUpToView(Type viewModelType)
        {
            var targetFragmentType = SupportedFragmentViewModels[viewModelType];
            var backStackEntryCount = FragmentManager.BackStackEntryCount;

            for (var i = 0; i < backStackEntryCount; i++)
            {
                if (CurrentFragment.GetType() == targetFragmentType)
                    break;

                FragmentManager.PopBackStackImmediate();
            }

            this.FragmentChanged();
        }

        // Current Fragment in the fragment host. Note although the id being used appears to be that of the 
        // original container, it gets replaced during a show by the new fragment *but* keeps its old id.
        public MvxFragment CurrentFragment
        {
            get { return FragmentManager.FindFragmentById(this.FragmentHostID) as MvxFragment; }
        }

        #endregion Fragment Host

        #region Private/Protected Methods

        public override bool OnCreateOptionsMenu(global::Android.Views.IMenu menu)
        {
            this.MenuInflater.Inflate(Resource.Menu.main_activity_actions, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private Type GetFragmentTypeForViewModel(Type viewModelType)
        {
            return SupportedFragmentViewModels[viewModelType];
        }

        protected void SetActivityTitleFromFragment(MvxFragment fragment)
        {
            var fragmentViewModel = fragment.DataContext as BaseFragmentViewModel;
            this.ActionBar.Title = fragmentViewModel == null ? string.Empty : fragmentViewModel.FragmentTitle;
        }

        private void NavigateToFragment(MvxFragment fragment, bool addToBackStack)
        {
            try
            {
                var transaction = FragmentManager.BeginTransaction();

                // Note: this *replaces* the actual fragment host specified in Page_StartUp.axml
                // but keeps the same id
                transaction.Replace(this.FragmentHostID, fragment);

                if (addToBackStack)
                    transaction.AddToBackStack(null);

                transaction.Commit();

                this.FragmentChanged(fragment);
            }
            catch (Java.Lang.IllegalStateException ex)
            {
                MvxTrace.Warning("IllegalStateException occurred on fragment change: {0}", ex.Message);
                // If the app is in the background, for example if the user has pressed the device Home button or put the device into sleep mode before the fragment change occurs,
                // then an error will be thrown: "java.lang.IllegalStateException: Can not perform this action after onSaveInstanceState".
                // If this happens then swallow the exception rather than crash the app, in which case the user will need to re-trigger the fragment change when they resume the app.
            }
        }

        //protected override void OnSaveInstanceState(Bundle outState)
        //{
        //    // Store the info service data so we can pick up where we left off when the activity is recreated
        //    var infoService = Mvx.Resolve<Core.Services.IInfoService>();

        //    outState.PutString("CurrentDriverID", infoService.CurrentDriverID.ToString());
        //    outState.PutString("CurrentDriverDisplayName", infoService.CurrentDriverDisplayName);
        //    outState.PutString("CurrentVehicleID", infoService.CurrentVehicleID.ToString());
        //    outState.PutString("CurrentVehicleRegistration", infoService.CurrentVehicleRegistration);
        //    outState.PutString("CurrentTrailerID", infoService.CurrentTrailerID.ToString());
        //    outState.PutString("CurrentTrailerRegistration", infoService.CurrentTrailerRegistration);
        //    outState.PutInt("Mileage", infoService.Mileage);

        //    base.OnSaveInstanceState(outState);
        //}

        //protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        //{
        //    base.OnRestoreInstanceState(savedInstanceState);

        //    var infoService = Mvx.Resolve<Core.Services.IInfoService>();

        //    if (!infoService.CurrentDriverID.HasValue)
        //    {
        //        infoService.CurrentDriverID = NullableGuidParse(savedInstanceState.GetString("CurrentDriverID"));
        //        infoService.CurrentDriverDisplayName = savedInstanceState.GetString("CurrentDriverDisplayName");
        //        infoService.CurrentVehicleID = NullableGuidParse(savedInstanceState.GetString("CurrentVehicleID"));
        //        infoService.CurrentVehicleRegistration = savedInstanceState.GetString("CurrentVehicleRegistration");
        //        infoService.CurrentTrailerID = NullableGuidParse(savedInstanceState.GetString("CurrentTrailerID"));
        //        infoService.CurrentTrailerRegistration = savedInstanceState.GetString("CurrentTrailerRegistration");
        //        infoService.Mileage = savedInstanceState.GetInt("Mileage");
        //    }
        //}

        private Guid? NullableGuidParse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return Guid.Parse(input);
        }

        #endregion Private/Protected Methods

    }

}
