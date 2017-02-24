using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Android.Views.Fragments
{

    public class ManifestFragment : BaseFragment
    {

        private IMenu optionsMenu;

        public ManifestViewModel ManifestViewModel
        {
            get { return (ManifestViewModel)ViewModel; }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_Manifest, null);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.manifest_activity_actions, menu);
            base.OnCreateOptionsMenu(menu, inflater);

            this.optionsMenu = menu;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_refresh:
                    SetRefreshActionButtonState(true);
                    ManifestViewModel.RefreshListCommand.Execute(null);
                    SetRefreshActionButtonState(false);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnResume()
        {
            base.OnResume();

            // Refresh the view to ensure that any instructions that have changed status (e.g. not-started to in-progress)
            // are shown in the correct section
            ManifestViewModel viewModel = this.DataContext as ManifestViewModel;
            if (viewModel!=null)
            {
                viewModel.RefreshStatusesCommand.Execute(null);
            }
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
