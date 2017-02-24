using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Droid.FullFragging.Fragments;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Android.Views.Fragments
{
    public class InboxFragment : BaseFragment
    {
        private IMenu optionsMenu;

        public InboxViewModel InboxViewModel
        {
            get { return (InboxViewModel)ViewModel; }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_Inbox, null);
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
                    InboxViewModel.RefreshMessagesCommand.Execute(null);
                    SetRefreshActionButtonState(false);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
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