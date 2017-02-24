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
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid.Platform;

namespace MWF.Mobile.Android.Portable
{
    public class LaunchPhone: ILaunchPhone
    {

        protected Activity CurrentActivity
        {
            get { return Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity; }
        }

        public void Launch()
        {
            
            // Launches phone app with contacts tab selected

            Intent intent = new Intent();
            intent.SetComponent(new ComponentName("com.android.contacts", "com.android.contacts.DialtactsContactsEntryActivity"));
            intent.SetAction("android.intent.action.MAIN");
            intent.AddCategory("android.intent.category.LAUNCHER");
            intent.AddCategory("android.intent.category.DEFAULT");
            this.CurrentActivity.StartActivity(intent);
            
        }
    }
}