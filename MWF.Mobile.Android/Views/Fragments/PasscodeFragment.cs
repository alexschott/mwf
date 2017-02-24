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

    public class PasscodeFragment : BaseFragment
    {

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_Passcode, null);
        }

        public override void OnResume()
        {
            base.OnResume();

            EditText passcodeText = (EditText)this.View.FindViewById(Resource.Id.editTextPasscode);
            passcodeText.RequestFocus();
            this.ShowSoftKeyboard();
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //Hide the action bar
            this.Activity.ActionBar.Hide();

            base.OnViewCreated(view, savedInstanceState);

            var passCodeBox = view.FindViewById<EditText>(Resource.Id.editTextPasscode);
            passCodeBox.EditorAction += PassCodeBoxOnEditorAction;
        }

        private void PassCodeBoxOnEditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            e.Handled = false;

            if (e.ActionId == ImeAction.Done)
            {
                var viewModel = this.ViewModel as PasscodeViewModel;
                
                viewModel.LoginCommand.Execute(null);
                e.Handled = true;
            }
        }

    }

}