using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Android.Views.Fragments
{
    public class CustomerCodeFragment : BaseFragment
    {
        private View _view;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            _view = this.BindingInflate(Resource.Layout.Fragment_CustomerCode, null);
            return _view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //Hide the action bar
            this.Activity.ActionBar.Hide();
            
            base.OnViewCreated(view, savedInstanceState);

            var customerCodeBox = view.FindViewById<EditText>(Resource.Id.CustomerCodeBox);
            customerCodeBox.EditorAction += CustomerCodeBoxOnEditorAction;
        }

        private void CustomerCodeBoxOnEditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            e.Handled = false;

            if (e.ActionId == ImeAction.Done)
            {
                var viewModel = this.ViewModel as CustomerCodeViewModel;
                
                viewModel.EnterCodeCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}