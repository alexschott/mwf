using System;
using Cirrious.CrossCore.Platform;
using Android.Util;

namespace MWF.Mobile.Android.Helpers
{

    public class DebugTrace : IMvxTrace
    {

        public void Trace(MvxTraceLevel level, string tag, Func<string> message)
        {
            Trace(level, tag, message());
        }

        public void Trace(MvxTraceLevel level, string tag, string message)
        {
            switch (level)
            {
                case MvxTraceLevel.Diagnostic:
                    Log.Debug(tag, message);
                    break;
                case MvxTraceLevel.Warning:
                    Log.Warn(tag, message);
                    break;
                case MvxTraceLevel.Error:
                    Log.Error(tag, message);
                    break;
                default:
                    Log.Info(tag, message);
                    break;
            }
        }

        public void Trace(MvxTraceLevel level, string tag, string message, params object[] args)
        {
            try
            {
                Trace(level, tag, string.Format(message, args));
            }
            catch (FormatException)
            {
                Trace(MvxTraceLevel.Error, tag, "Exception during trace of {0} {1} {2}", level, message);
            }
        }

    }

}
