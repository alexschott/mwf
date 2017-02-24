using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Cirrious.MvvmCross.Binding.Droid.BindingContext;
using Cirrious.MvvmCross.Binding.BindingContext;
using MWF.Mobile.Android.Controls;
using MWF.Mobile.Core.ViewModels;
using SignaturePad;
using AndroidGraphics = Android.Graphics;

namespace MWF.Mobile.Android.Views.Fragments
{

    public class SafetyCheckSignatureFragment : BaseFragment
    {

        private SignaturePadView _signaturePad;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            var view = this.BindingInflate(Resource.Layout.Fragment_SafetyCheckSignature, null);

            _signaturePad = view.FindViewById<SignaturePadView>(Resource.Id.signature_view);
            _signaturePad.StrokeColor = AndroidGraphics.Color.Black;
            _signaturePad.BackgroundColor = AndroidGraphics.Color.Rgb(204, 207, 209); // Match the color of an EditText
            _signaturePad.SignaturePrompt.Text = string.Empty;
            _signaturePad.ClearLabel.TextSize = 20.0f;

            var doneButton = view.FindViewById<Button>(Resource.Id.button_done);
            doneButton.Click += this.DoneButton_Click;

            return view;
        }

        private async void DoneButton_Click(object sender, EventArgs e)
        {
            //TODO: can we achieve this using data binding?
            var viewModel = (SafetyCheckSignatureViewModel)ViewModel;

            if (_signaturePad.IsBlank)
                viewModel.SignatureEncodedImage = null;
            else
            {
                var image = _signaturePad.GetImage(AndroidGraphics.Color.Black, AndroidGraphics.Color.White, 0.5f, shouldCrop: false);

                using (var ms = new MemoryStream())
                {
                    image.Compress(AndroidGraphics.Bitmap.CompressFormat.Png, 0, ms);
                    viewModel.SignatureEncodedImage = Convert.ToBase64String(ms.ToArray());
                }
            }

            await viewModel.DoneAsync();
        }

    }

}
