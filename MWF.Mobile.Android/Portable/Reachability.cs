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
using Android.Net;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid;

namespace MWF.Mobile.Android.Portable
{
    class Reachability : IReachability
    {

        private ConnectivityManager _connectivityManager;

        protected ConnectivityManager ConnectivityManager
        {
            get
            {
                _connectivityManager = _connectivityManager ?? (ConnectivityManager)(Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext.GetSystemService(Context.ConnectivityService));
                return _connectivityManager;
            }
        }

        public bool IsConnected()
        {
            var activeConnection = this.ConnectivityManager.ActiveNetworkInfo;
            return ((activeConnection != null) && activeConnection.IsConnected);
        }
    }
}