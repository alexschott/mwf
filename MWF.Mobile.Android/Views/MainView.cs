using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Widget;
using Cirrious.MvvmCross.Binding.Droid.Views;
using Cirrious.MvvmCross.Droid.FullFragging;
using Cirrious.MvvmCross.Droid.FullFragging.Fragments;
using Cirrious.MvvmCross.ViewModels;
using Android.Content.PM;
using MWF.Mobile.Android.Helpers;
using MWF.Mobile.Core.ViewModels;
using Android.Support.V4.App;
using System.Windows.Input;


namespace MWF.Mobile.Android.Views
{

    [Activity(ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainView
        : BaseActivityView, Presenters.IFragmentHost
    {
        #region Private/Protected Fields

        private DrawerLayout _drawer;
        private MvxListView _drawerList;
        private RelativeLayout _relativeLayout;
        private global::Android.Support.V4.App.ActionBarDrawerToggle _drawerToggle;
        protected static IDictionary<Type, Type> _supportedViewModels = new Dictionary<Type, Type>
            {
                { typeof(Core.ViewModels.AboutViewModel), typeof(Fragments.AboutFragment)},
                { typeof(Core.ViewModels.DiagnosticsViewModel), typeof(Fragments.DiagnosticsFragment)},
                { typeof(Core.ViewModels.BarcodeScanningViewModel), typeof(Fragments.BarcodeFragment)},
                { typeof(Core.ViewModels.BarcodeStatusViewModel), typeof(Fragments.BarcodeStatusFragment)},
                { typeof(Core.ViewModels.DisplaySafetyCheckViewModel), typeof(Fragments.DisplaySafetyCheckFragment)},
                { typeof(Core.ViewModels.InboxViewModel), typeof(Fragments.InboxFragment)},
                { typeof(Core.ViewModels.InstructionViewModel), typeof(Fragments.InstructionFragment) },
                { typeof(Core.ViewModels.InstructionAddDeliveriesViewModel), typeof(Fragments.InstructionAddDeliveriesFragment)},
                { typeof(Core.ViewModels.InstructionClausedViewModel), typeof(Fragments.InstructionClausedFragment)},
                { typeof(Core.ViewModels.InstructionCommentViewModel), typeof(Fragments.InstructionCommentFragment)},
                { typeof(Core.ViewModels.InstructionOnSiteViewModel), typeof(Fragments.InstructionOnSiteFragment)},
                { typeof(Core.ViewModels.InstructionSafetyCheckViewModel), typeof(Fragments.SafetyCheckFragment)},
                { typeof(Core.ViewModels.InstructionSignatureViewModel), typeof(Fragments.InstructionSignatureFragment) },
                { typeof(Core.ViewModels.InstructionTrailerViewModel), typeof(Fragments.TrailerListFragment)},
                { typeof(Core.ViewModels.InstructionTrunkProceedViewModel), typeof(Fragments.InstructionTrunkProceedFragment)},
                { typeof(Core.ViewModels.OdometerViewModel), typeof(Fragments.OdometerFragment)},
                { typeof(Core.ViewModels.OrderViewModel), typeof(Fragments.OrderFragment)},
                { typeof(Core.ViewModels.ManifestViewModel), typeof(Fragments.ManifestFragment) },
                { typeof(Core.ViewModels.MessageViewModel), typeof(Fragments.MessageFragment) },
                { typeof(Core.ViewModels.ModalCameraViewModel),typeof(Fragments.ModalCameraFragment)},
                { typeof(Core.ViewModels.ReviseQuantityViewModel), typeof(Fragments.ReviseQuantityFragment)},
                { typeof(Core.ViewModels.SafetyCheckViewModel), typeof(Fragments.SafetyCheckFragment)},
                { typeof(Core.ViewModels.SafetyCheckSignatureViewModel), typeof(Fragments.SafetyCheckSignatureFragment) },
                { typeof(Core.ViewModels.SidebarCameraViewModel),typeof(Fragments.SideBarCameraFragment)}, 
			    { typeof(Core.ViewModels.SafetyCheckFaultViewModel), typeof(Fragments.SafetyCheckFaultFragment)},
                { typeof(Core.ViewModels.InstructionSafetyCheckSignatureViewModel), typeof(Fragments.SafetyCheckSignatureFragment) },
                { typeof(Core.ViewModels.ConfirmTimesViewModel), typeof(Fragments.ConfirmTimesFragment) }
            };


        #endregion

        #region Construction
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Page_Main);

            this.ActionBar.SetDisplayHomeAsUpEnabled(true);
            this.ActionBar.SetHomeButtonEnabled(true);

            //Navigation Draw
            _drawer = this.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            _drawerList = this.FindViewById<MvxListView>(Resource.Id.left_drawer);

            _relativeLayout = this.FindViewById<RelativeLayout>(Resource.Id.relative_layout);

            _drawerToggle = new CustomActionBarDrawerToggle(this, this._drawer, Resource.Drawable.ic_drawer_light, Resource.String.drawer_open,
                                                      Resource.String.drawer_close);

            _drawer.DrawerOpened += delegate
            {
                this.InvalidateOptionsMenu();
            };

            _drawer.DrawerClosed += delegate
            {
                this.InvalidateOptionsMenu();
            };

            this._drawer.SetDrawerListener(this._drawerToggle);

        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            this._drawerToggle.SyncState();
        }

        #endregion

        #region Fragment Host

        public override IDictionary<Type, Type> SupportedFragmentViewModels
        {
            get { return _supportedViewModels; }
        }

        public override int FragmentHostID { get { return Resource.Id.fragment_host_main; } }

        #endregion Fragment host

        #region Options Menu

        public override bool OnPrepareOptionsMenu(global::Android.Views.IMenu menu)
        {
            var drawerOpen = this._drawer.IsDrawerOpen(this._relativeLayout);

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(global::Android.Views.IMenuItem item)
        {

            if (this._drawerToggle.OnOptionsItemSelected(item))
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void InvalidateOptionsMenu()
        {

            base.InvalidateOptionsMenu();
            _drawer.CloseDrawers();

        }

        #endregion

    }

}