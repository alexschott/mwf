using System;
using System.Collections.Generic;
using System.Linq;

namespace MWF.Mobile.Android.Helpers
{

    public class AndroidLogReader
    {
        public string ReadLog(string tag)
        {
            var cmd = "logcat -d";

            if (!string.IsNullOrWhiteSpace(tag))
            {
                cmd += " -s " + tag;
            }

            var process = Java.Lang.Runtime.GetRuntime().Exec(cmd);

            using (var sr = new System.IO.StreamReader(process.InputStream))
            {
                return sr.ReadToEnd();
            }
        }

    }

}