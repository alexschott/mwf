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
using ZXing.Mobile;
using System.Threading.Tasks;

namespace MWF.Mobile.Android.Views.Fragments
{
    public class CheckOutQRCodeFragment : BaseFragment
    {
        private bool _scanning = false;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {            
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_CheckOutQRCode, null);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            this.Activity.ActionBar.Show();
            base.OnViewCreated(view, savedInstanceState);
            var scanAgainButton = view.FindViewById<Button>(Resource.Id.buttonScanAgain);
            scanAgainButton.Click += ScanAgainButton_Click;

            Task.Run(() => startScanning());
        }

        private void ScanAgainButton_Click(object sender, EventArgs e)
        {
            if (!_scanning) Task.Run(() => startScanning());
        }

        private async void startScanning()
        {
            _scanning = true;
            Core.ViewModels.CheckOutQRCodeViewModel viewModel = (this.ViewModel as Core.ViewModels.CheckOutQRCodeViewModel);
            viewModel.IsBusy = true;

            var scanner = new ZXing.Mobile.MobileBarcodeScanner(this.Activity);
            scanner.UseCustomOverlay = true;
            scanner.CustomOverlay = LayoutInflater.FromContext(this.Activity).Inflate(Resource.Layout.ZXingCustomLayout, null);
            var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
            options.PossibleFormats = new List<ZXing.BarcodeFormat>() { ZXing.BarcodeFormat.QR_CODE };            
            await scanner.Scan(options).ContinueWith(t =>
            {
                if (t.Result != null)
                {
                    viewModel.ScannedQRCode = t.Result.Text;
                }
                else
                {
                    viewModel.Message = "The Check Out QR code could not be scanned.";
                    Task.Delay(500).ContinueWith(async dummy => await viewModel.OnBackButtonPressedAsync());
                }
            });
            viewModel.IsBusy = false;
            _scanning = false;
        }
    }
}