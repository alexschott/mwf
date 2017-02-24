using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.Res;

namespace MWF.Mobile.Android.Helpers
{
    public class DebugDBHelper
    {

        public void CopyDebugDatabaseIfPresent(AssetManager assets)
        {
            try
            {
                using (var stream = assets.Open("db.sql"))
                {
                    byte[] dbBytes = ReadFully(stream);
                    string destPath = Path.Combine(GetDefaultBasePath(), "db.sql");
                    File.WriteAllBytes(destPath, dbBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[8 * 1024 * 1024]; //8 mb
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private string GetDefaultBasePath()
        {
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        }
    }
}