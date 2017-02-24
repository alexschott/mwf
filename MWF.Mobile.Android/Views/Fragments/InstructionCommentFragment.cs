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
using MWF.Mobile.Core.Converters;

namespace MWF.Mobile.Android.Views.Fragments
{

    public class InstructionCommentFragment : BaseFragment
    {

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // MVVMCross fragment boilerplate code
            var ignored = base.OnCreateView(inflater, container, savedInstanceState);
            return this.BindingInflate(Resource.Layout.Fragment_InstructionComment, null);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            var submitButton = (Button)view.FindViewById(Resource.Id.ButtonAdvanceInstructionComment);
            var set = this.CreateBindingSet<InstructionCommentFragment, InstructionCommentViewModel>();
            set.Bind(submitButton).For(b => b.Enabled).To(vm => vm.CommentText).WithConversion(new StringHasLengthConverter(), null);
            set.Apply();
        }

        public override void OnResume()
        {
            base.OnResume();

            var commentText = (EditText)this.View.FindViewById(Resource.Id.commentText);
            commentText.RequestFocus();
            this.ShowSoftKeyboard();
        }

    }

}