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
using SignaturePad;
using MWF.Mobile.Core.ViewModels;
using AndroidGraphics = Android.Graphics;
using System.IO;

namespace MWF.Mobile.Android.Views.Fragments
{
    public class CheckOutSignatureFragment : BaseFragment
    {
        private SignaturePadView _signaturePad;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_CheckOutSignature, null);            
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var completeButton = view.FindViewById<Button>(Resource.Id.buttonComplete);
            completeButton.Click += CompleteButton_Click;

            _signaturePad = this.View.FindViewById<SignaturePadView>(Resource.Id.driverSignatureView);
            _signaturePad.BackgroundColor = AndroidGraphics.Color.Rgb(204, 207, 209);
            _signaturePad.SignaturePrompt.Text = string.Empty;
            _signaturePad.ClearLabel.TextSize = 20.0f;
            _signaturePad.StrokeColor = AndroidGraphics.Color.Black;
        }

        private void CompleteButton_Click(object sender, EventArgs e)
        {
            CheckOutSignatureViewModel viewModel = this.ViewModel as CheckOutSignatureViewModel;

            if (_signaturePad.IsBlank) viewModel.DriverSignature = null;
            else
            {
                var image = _signaturePad.GetImage(AndroidGraphics.Color.Black,
                    AndroidGraphics.Color.White, 0.5f, shouldCrop: false);
                using (var ms = new MemoryStream())
                {
                    image.Compress(AndroidGraphics.Bitmap.CompressFormat.Png, 0, ms);
                    viewModel.DriverSignature = Convert.ToBase64String(ms.ToArray());
                }
                viewModel.CompleteCommand.Execute(null);
            }
        }
    }
}