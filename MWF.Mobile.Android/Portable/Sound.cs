using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MWF.Mobile.Core.Portable;
using Android.Media;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid.Platform;

namespace MWF.Mobile.Android.Portable
{
    public class Sound
        : ISound
    {

        protected Activity CurrentActivity
        {
            get { return Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity; }
        }

        private enum _soundTracks { testSound = Resource.Raw.Kalimba, refreshSound, clickSound };

        public void Play()
        {
            //TODO: Implement some form of enum.

            var notification = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
            Ringtone r = RingtoneManager.GetRingtone(CurrentActivity, notification);
            r.Play();

        }
    }
}