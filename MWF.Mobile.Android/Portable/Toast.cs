using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidWidget = Android.Widget;
using MWF.Mobile.Core.Portable;

namespace MWF.Mobile.Android.Portable
{
    public class Toast
        : IToast
    {
        public void Show(string text)
        {
            Context context = Application.Context;
            var duration = AndroidWidget.ToastLength.Short;
            var toast = AndroidWidget.Toast.MakeText(context, text, duration);
            toast.Show();
        }
    }
}