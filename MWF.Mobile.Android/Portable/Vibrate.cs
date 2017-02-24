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
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid.Platform;
using MWF.Mobile.Core.Portable;

namespace MWF.Mobile.Android.Portable
{
    public class Vibrate: 
        IVibrate
    {
        protected Activity CurrentActivity
        {
            get { return Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity; }
        }

        public void VibrateDevice()
        {
            Vibrator vibrate = (Vibrator)this.CurrentActivity.GetSystemService(Context.VibratorService);
            vibrate.Vibrate(500);
        }

        public void VibrateDevice(long length)
        {
            Vibrator vibrate = (Vibrator)this.CurrentActivity.GetSystemService(Context.VibratorService);
            vibrate.Vibrate(length);
        }
    }
}