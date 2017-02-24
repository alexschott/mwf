using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using Cirrious.MvvmCross.Binding.Droid.Views;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Android.Views.Fragments
{

    public class SafetyCheckFragment : BaseFragment
    {

        private ListView _itemList;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.SafetyCheckView, null);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            _itemList = (ListView)view.FindViewById(Resource.Id.SafetyListView);
            _itemList.ItemClick += itemList_ItemClick;
            RegisterForContextMenu(_itemList);

            var checksDoneButton = (Button)view.FindViewById(Resource.Id.checksdonebutton);

            var set = this.CreateBindingSet<SafetyCheckFragment, SafetyCheckViewModel>();
            set.Bind(checksDoneButton).For(b => b.Enabled).To(vm => vm.CanSafetyChecksBeCompleted);
            set.Apply();

            base.OnViewCreated(view, savedInstanceState);
        }

        private void itemList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ((ListView)sender).ShowContextMenuForChild(e.View);
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo info)
        {
            var menuInfo = (AdapterView.AdapterContextMenuInfo)info;
            var safetyCheckItem = ((SafetyCheckItemViewModel)((MvxListItemView)menuInfo.TargetView).DataContext);

            menu.SetHeaderTitle(safetyCheckItem.Title);

            foreach (var status in safetyCheckItem.AvailableStatuses)
            {
                menu.Add(0, (int)status.Key, 0, status.Value);
            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            var safetyCheckItemViewModel = (SafetyCheckItemViewModel)((MvxListItemView)((AdapterView.AdapterContextMenuInfo)item.MenuInfo).TargetView).DataContext;
            safetyCheckItemViewModel.CheckStatus = (Core.Enums.SafetyCheckStatus)item.ItemId;
            return base.OnOptionsItemSelected(item);
        }

    }

}
