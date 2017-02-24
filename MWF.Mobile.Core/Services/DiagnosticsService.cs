using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.Instruction;
using MWF.Mobile.Core.Portable;
using System.IO;

namespace MWF.Mobile.Core.Services
{

    public class DiagnosticsService : IDiagnosticsService
    {
        
        private readonly Repositories.IRepositories _repositories = null;
        private readonly ILoggingService _loggingService = null;
        private readonly IReachability _reachability = null;
        private readonly IDeviceInfo _deviceInfo = null;
        private readonly IUpload _upload = null;

        public DiagnosticsService(
            Repositories.IRepositories repositories,
            ILoggingService loggingService,
            IReachability reachability,
            IDeviceInfo deviceInfo,
            IUpload upload)
        {
            _repositories = repositories;
            _loggingService = loggingService;
            _reachability = reachability;
            _deviceInfo = deviceInfo;
            _upload = upload;
        }

        /// <summary>
        /// This method uploads the database to the FTP server under the Android Device ID
        /// </summary>
        /// <param name="databasePath">The locations of the mySql Database</param>
        public async Task<bool> UploadDiagnosticsAsync(string databasePath)
        {

            if (!_reachability.IsConnected())
                return false;

            var config = await _repositories.ConfigRepository.GetAsync();

            if (config == null || 
                string.IsNullOrWhiteSpace(config.FtpUrl) ||
                string.IsNullOrWhiteSpace(config.FtpUsername) ||
                string.IsNullOrWhiteSpace(config.FtpPassword))
            {
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("Your FTP credentials have not been set up, you cannot upload support data until they have been set up.");
                return false;
            }

            bool success = false;
			LogMessage exceptionMsg = null;

            try
            {
                var uriString = string.Format("{0}/{1}/{2}",config.FtpUrl ,_deviceInfo.AndroidId, Path.GetFileName(databasePath));
                var uri = new Uri(uriString);
                success = await _upload.UploadFileAsync(uri ,config.FtpUsername, config.FtpPassword, databasePath);
            }
            catch (Exception ex)
            {
				exceptionMsg = _loggingService.GetExceptionLogMessage(ex);
            }

			if (exceptionMsg != null)
				await _loggingService.LogEventAsync(exceptionMsg);

            return success;
           
        }

    }

}
