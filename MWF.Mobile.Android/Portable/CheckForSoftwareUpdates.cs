using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid.Platform;
using MWF.Mobile.Android.Helpers;
using MWF.Mobile.Core.Portable;

namespace MWF.Mobile.Android.Portable
{
    public class CheckForSoftwareUpdates
        : ICheckForSoftwareUpdates
    {
        protected Activity CurrentActivity
        {
            get { return Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity; }
        }

        public void Check()
        {
            HockeyApp.UpdateManager.Unregister();
            HockeyApp.UpdateManager.Register(CurrentActivity, HockeyAppConstants.AppID, true);
        }
    }
}