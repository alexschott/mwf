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
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Droid.Platform;

namespace MWF.Mobile.Android.Portable
{
    public class Upload : IUpload
    {
        public async Task<bool> UploadFileAsync(System.Uri address,string username, string password, string path)
        {
            string dirPath = GetParentUriString(address);

            WebClient wc = new WebClient();
            var credentials = new NetworkCredential(username, password);
            wc.Credentials = credentials;
            bool success = false;
            try
            {
                // If ftp directory for device doesn't exist then create it
                if (!(await FtpDirectoryExistsAsync(dirPath, credentials)))
                {
                    bool createdDirOK = await CreateFtpDirAsync(dirPath, credentials);
                    if (!createdDirOK)
                    {
                        return false;
                    }
                }

                // upload the file
                await wc.UploadFileTaskAsync(address, path);
                success = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return success;


        }

        /// <summary>
        /// Get the parent directory of the file specified in the uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        static string GetParentUriString(System.Uri uri)
        {
            var uriString = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments.Last().Length);
            return uriString.Substring(0, uriString.LastIndexOf('/'));
        }


        /// <summary>
        /// Checks whether the the specified directory exists at the ftp location
        /// </summary>
        /// <param name="path"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private async Task<bool> FtpDirectoryExistsAsync(string path, NetworkCredential credentials)
        {
            try
            {
                var request = WebRequest.Create(path);
                request.Credentials = credentials;
                request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

                WebResponse response = await request.GetResponseAsync();

            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    return false;
                else
                    return true;
            }
            return true;
        }

        /// <summary>
        /// creates the directory as specified in the path at the ftp location
        /// </summary>
        /// <param name="path"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private async Task<bool> CreateFtpDirAsync(string path, NetworkCredential credentials)
        {
                WebRequest ftpReq = WebRequest.Create(path) as FtpWebRequest;
                ftpReq.Method = WebRequestMethods.Ftp.MakeDirectory;
                ftpReq.Credentials = credentials;
                WebResponse response = await ftpReq.GetResponseAsync();
                FtpWebResponse ftpResponse = response as FtpWebResponse;
                return ftpResponse.StatusCode == FtpStatusCode.CommandOK || ftpResponse.StatusCode == FtpStatusCode.PathnameCreated;
   
        }

    }
}