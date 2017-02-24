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
using MWF.Mobile.Core.Portable;
using Cirrious.CrossCore.Droid.Platform;
using Cirrious.CrossCore;

namespace MWF.Mobile.Android.Portable
{
    public class CloseApplication
        :ICloseApplication
    {
        public void CloseApp()
        {
            var topActivity = Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity;
            //Intent myIntent = new Intent();
            
            //myIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            topActivity.Finish();
        }
    }
}