using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using Cirrious.MvvmCross.ViewModels;
using MWF.Mobile.Android.Controls;
using MWF.Mobile.Core.ViewModels;

namespace MWF.Mobile.Android.Views.Fragments
{
    public class BarcodeFragment : BaseFragment
    {
        private KeyboardlessEditText _barcodeInput;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_Barcode, null);
        }

        public override void OnResume()
        {
            base.OnResume();

            _barcodeInput = (KeyboardlessEditText)this.View.FindViewById(Resource.Id.BarcodeInput);   

            _barcodeInput.RequestFocus();
        }
        
         public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var checksDoneButton = (Button)view.FindViewById(Resource.Id.ButtonCompleteScanning);
            var set = this.CreateBindingSet<BarcodeFragment, BarcodeScanningViewModel>();
            //set.Bind(checksDoneButton).For(b => b.Enabled).To(vm => vm.CanScanningBeCompleted);
            set.Apply();

            var viewModel = this.ViewModel as IMvxNotifyPropertyChanged;
            viewModel.PropertyChanged += viewModel_PropertyChanged;
        }

         void viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
         {
             if (e.PropertyName == "RequestBarcodeFocus")
             {
                 _barcodeInput.RequestFocus();
                 _barcodeInput.PerformClick();
                 _barcodeInput.SetCursorVisible(true);
             }
         }

    }
    
}