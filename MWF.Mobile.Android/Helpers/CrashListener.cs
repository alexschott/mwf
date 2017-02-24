using System;
using System.Collections.Generic;
using System.Linq;
using MWF.Mobile.Core.Services;

namespace MWF.Mobile.Android.Helpers
{

    public class CrashListener : HockeyApp.CrashManagerListener
    {

        public CrashListener(IDeviceInfo deviceInfo)
        {
            _deviceIdentifier = deviceInfo.GetDeviceIdentifier();
        }

        private readonly string _deviceIdentifier = null;
        private string _description = null;

        public override string UserID
        {
            get { return _deviceIdentifier; }
        }

        public override string Contact
        {
            get { return _deviceIdentifier; }
        }

        public override string Description
        {
            get { return _description; }
        }

        public void SetDescription(string value)
        {
            this._description = value;
        }

    }

}