using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using MWF.Mobile.Core.ViewModels;
using Android.Views.InputMethods;
using MWF.Mobile.Android.Controls;

namespace MWF.Mobile.Android.Views.Fragments
{

    public class VehicleListFragment : BaseFragment
    {

        private SearchView _searchView;
        private IMenu optionsMenu;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_VehicleListView, null);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.vehicle_activity_actions, menu);
            base.OnCreateOptionsMenu(menu, inflater);

            this.optionsMenu = menu;

            var searchItem = menu.FindItem(Resource.Id.vehicle_action_search).ActionView;
            _searchView = searchItem.JavaCast<SearchView>();

            _searchView.QueryTextChange += (s, e) => ((VehicleListViewModel)this.ViewModel).VehicleSearchText = e.NewText;

        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.action_refresh:
                    SetRefreshActionButtonState(true);
                    ((VehicleListViewModel)ViewModel).RefreshListCommand.Execute(null);
                    SetRefreshActionButtonState(false);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetHasOptionsMenu(true);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //Closes the android keyboard if its still up.
            InputMethodManager mgr = (InputMethodManager)this.Activity.GetSystemService(Context.InputMethodService);
            mgr.HideSoftInputFromWindow(this.View.WindowToken, 0);
            this.Activity.ActionBar.Show();
            base.OnViewCreated(view, savedInstanceState);
        }

        public void SetRefreshActionButtonState(bool refreshing)
        {
            if (optionsMenu != null)
            {
                var refreshItem = optionsMenu.FindItem(Resource.Id.action_refresh);
                if (refreshItem != null)
                {
                    if (refreshing)
                    {
                        refreshItem.SetActionView(Resource.Menu.actionbar_indeterminate_progress);
                    }
                    else
                    {
                        refreshItem.SetActionView(null);
                    }
                }
            }
        }

    }

}