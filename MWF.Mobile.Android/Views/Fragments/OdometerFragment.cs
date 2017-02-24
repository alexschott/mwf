using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Droid.FullFragging.Fragments;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using Cirrious.MvvmCross.Binding.BindingContext;
using Android.Views.InputMethods;
using MWF.Mobile.Core.Converters;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Android.Views.Fragments
{
    public class OdometerFragment : BaseFragment
    {
        private View _view;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {

            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            _view = this.BindingInflate(Resource.Layout.Fragment_Odometer, null);
            return _view;
        }

        public override void OnResume()
        {
            base.OnResume();
            EditText odometerText = (EditText)this.View.FindViewById(Resource.Id.odometerText);            
            odometerText.RequestFocus();
            this.ShowSoftKeyboard();
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            var submitButton = (Button)view.FindViewById(Resource.Id.submit);
            var set = this.CreateBindingSet<OdometerFragment, OdometerViewModel>();
            set.Bind(submitButton).For(b => b.Enabled).To(vm => vm.OdometerValue).WithConversion(new StringHasLengthConverter(), null);
            set.Apply();
        }
    }
}