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
using SignaturePad;
using System.IO;
using AndroidGraphics = Android.Graphics;

namespace MWF.Mobile.Android.Views.Fragments
{

    public class InstructionSignatureFragment : BaseFragment
    {
        SignaturePadView signaturePad;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            var view = this.BindingInflate(Resource.Layout.Fragment_InstructionSignature, null);

            signaturePad = view.FindViewById<SignaturePadView>(Resource.Id.signature_instructionView);

            var doneButton = view.FindViewById<Button>(Resource.Id.ButtonAdvanceSignatureInstruction);
            doneButton.Click += this.DoneButton_Click;

            var signatureToggleButton = view.FindViewById<Button>(Resource.Id.ButtonSignatureToggle);
            signatureToggleButton.Click += this.SignatureToggleButton_Click;

            var set = this.CreateBindingSet<InstructionSignatureFragment, InstructionSignatureViewModel>();
            set.Bind(signatureToggleButton).For(b => b.Enabled).To(vm => vm.IsSignatureToggleButtonEnabled);
            set.Apply();

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var viewModel = (InstructionSignatureViewModel)ViewModel;
            signaturePad.Clear();

            this.MimicSignaturePadDisabled(!viewModel.IsSignaturePadEnabled);

            signaturePad.BackgroundColor = AndroidGraphics.Color.Rgb(204, 207, 209); // Match the color of an EditText
            signaturePad.SignaturePrompt.Text = string.Empty;
            signaturePad.ClearLabel.TextSize = 20.0f;
        }

        void SignatureToggleButton_Click(object sender, EventArgs e)
        {
            var viewModel = (InstructionSignatureViewModel)ViewModel;
            signaturePad.Clear();

            viewModel.IsSignaturePadEnabled = !viewModel.IsSignaturePadEnabled;
            this.MimicSignaturePadDisabled(!viewModel.IsSignaturePadEnabled);
            
            viewModel.RaisePropertyChanged(() => viewModel.SignatureToggleButtonLabel);
        }

        void DoneButton_Click(object sender, EventArgs e)
        {
            var viewModel = (InstructionSignatureViewModel)ViewModel;

            if (signaturePad.IsBlank || !viewModel.IsSignaturePadEnabled)
                viewModel.CustomerSignatureEncodedImage = null;
            else
            {
                var image = signaturePad.GetImage(AndroidGraphics.Color.Black, AndroidGraphics.Color.White, 0.5f, shouldCrop: false);

                using (var ms = new MemoryStream())
                {
                    image.Compress(AndroidGraphics.Bitmap.CompressFormat.Png, 0, ms);
                    viewModel.CustomerSignatureEncodedImage = Convert.ToBase64String(ms.ToArray());
                }
            }

            viewModel.InstructionDoneCommand.Execute(null);
        }

        /// <summary>
        /// This method changes the stroke color to the same as the background to make it seem
        /// like the signature pad is deactivated. (I was unable to disable the signature pad)
        /// </summary>
        private void MimicSignaturePadDisabled(bool isDisabled)
        {
            if (isDisabled)
            {
                signaturePad.StrokeColor = AndroidGraphics.Color.Rgb(204, 207, 209);
                signaturePad.ClearLabel.Text = string.Empty;
                signaturePad.Caption.Text = string.Empty;
                signaturePad.SignatureLine.Visibility = ViewStates.Invisible;
            }
            else
            {
                signaturePad.StrokeColor = AndroidGraphics.Color.Black;
                signaturePad.ClearLabel.Text = "Clear";
                signaturePad.Caption.Text = "Sign Here";
                signaturePad.SignatureLine.Visibility = ViewStates.Visible;
            }
        }
          
    }

}