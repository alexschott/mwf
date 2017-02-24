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
using Android.Telephony;
using Android.Provider;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Android.Portable
{
    public class DeviceInfo : IDeviceInfo
    {

        /// <summary>
        /// Gets an unique identifier for the device
        /// </summary>
        /// <returns>Either IMEI, SerialNumber or AndroidId depending on what is available</returns>
        public string GetDeviceIdentifier()
        {
            return AndroidId;
        }

        public string IMEI
        {
            get
            {
                var globals = Mvx.Resolve<IMvxAndroidGlobals>();
                TelephonyManager tm = (TelephonyManager)globals.ApplicationContext.GetSystemService(Context.TelephonyService);
                return tm.DeviceId ?? string.Empty;
            }
        }

        public string OsVersion
        {
            get { return Build.VERSION.Release; }
        }

        public string Manufacturer
        {
            get { return Build.Manufacturer; }
        }

        public string Model
        {
            get { return Build.Model; }
        }

        public string Platform
        {
            get { return "Android"; }
        }

        public string SerialNumber
        {
            get
            {
                return Build.Serial;
            }
        }

        public string AndroidId
        {
            get
            {
                var globals = Mvx.Resolve<IMvxAndroidGlobals>();
                return Settings.Secure.GetString(globals.ApplicationContext.ContentResolver, Settings.Secure.AndroidId);
            }
        }

        public string GatewayPassword
        {
            get { return "fleetwoodmobile"; }
        }

        public string MobileApplication
        {
            get { return "Orchestrator"; }
        }

        public string SoftwareVersion { get; internal set; }

        public string DatabasePath
        {
            get { return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal); }
        }

    }

}
