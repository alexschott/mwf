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

    public class InstructionAddDeliveriesFragment : BaseFragment
    {

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            var view = this.BindingInflate(Resource.Layout.Fragment_InstructionAddDeliveries, null);
            var doneButton = (Button)view.FindViewById(Resource.Id.DoneButton);
            var set = this.CreateBindingSet<InstructionAddDeliveriesFragment, InstructionAddDeliveriesViewModel>();
            set.Bind(doneButton).For(b => b.Enabled).To(vm => vm.DoneButtonEnabled);
            set.Apply();

            return view;
        }  
    }
}