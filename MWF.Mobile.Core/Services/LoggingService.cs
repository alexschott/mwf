using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Models;
using MWF.Mobile.Core.Models.GatewayServiceRequest;
using MWF.Mobile.Core.Portable;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Repositories.Interfaces;

namespace MWF.Mobile.Core.Services
{
    public class LoggingService
       : ILoggingService
    {

        private readonly ILogMessageRepository _loggedRepository = null;
        private readonly IDeviceInfo _deviceInfo = null;
        private readonly IGatewayService _gatewayService = null;
        private readonly IReachability _reachability = null;
        private readonly ICloseApplication _closeApplication = null;
        private bool _isSubmitting = false;
        private const int MAX_MESSAGE_LENGTH = 6000;
        
        private MvxSubscriptionToken _notificationToken;


        public LoggingService(
            IRepositories repositories, 
            IGatewayService gatewayService, 
            IDeviceInfo deviceInfo,
            IReachability reachability,
            ICloseApplication closeApplication)
        {
            _loggedRepository = repositories.LogMessageRepository;
            _gatewayService = gatewayService;
            _deviceInfo = deviceInfo;
            _reachability = reachability;
            _closeApplication = closeApplication;

            _notificationToken = Mvx.Resolve<IMvxMessenger>().Subscribe<Messages.TopLevelExceptionHandlerMessage>(async m =>
                await this.LogAndCloseAsync(m.TopLevelException)
            );
        }

		public LogMessage GetExceptionLogMessage(Exception exception)
		{
			return new LogMessage
			{
				LogDateTime = DateTime.Now.ToLocalTime(),
				Message = string.Format("{0} - {1}: {2}", Enums.LogType.Error.ToString(), exception.Message, exception.StackTrace),
				LogType = Enums.LogType.Error
			};
		}

        public Task LogEventAsync(Exception exception)
        {
            var loggedException = this.GetExceptionLogMessage(exception);
            return this.InsertLogMessage(loggedException);
        }

        public Task LogEventAsync(Models.LogMessage exceptionLogMessage)
		{
            return this.InsertLogMessage(exceptionLogMessage);
        }

        public Task LogEventAsync(string eventDescription, Enums.LogType type)
        {
            var loggedEvent = new LogMessage
            {
                LogDateTime = DateTime.Now.ToLocalTime(),
                Message = string.Format("{0} - {1}", type.ToString(), eventDescription),
                LogType = type
            };

            return this.InsertLogMessage(loggedEvent);
        }

        public Task LogEventAsync(string eventDescription, Enums.LogType type, params object[] args)
        {
            var loggedEvent = new LogMessage
            {
                LogDateTime = DateTime.Now.ToLocalTime(),
                Message = string.Format("{0} - {1}", type.ToString(), string.Format(eventDescription, args)),
                LogType = type
            };

            return this.InsertLogMessage(loggedEvent);
        }

        public async Task UploadLoggedEventsAsync()
        {
            if (_isSubmitting)
                return;

            _isSubmitting = true;

            try
            {
                var events = await _loggedRepository.GetAllAsync();

                if (events != null && events.Count() > 0)
                    events = events.OrderBy(e => e.LogDateTime);

                if (events != null && events.Any())
                {
                    if (!_reachability.IsConnected())
                        return;

                    var deviceIdentifier = _deviceInfo.GetDeviceIdentifier();

                    foreach (var e in events)
                    {
                        int messageLength = e.Message.Length;
                        var deviceMessage = new DeviceLogMessage
                        {
                            Message = e.Message.Substring(0, Math.Min(messageLength, MAX_MESSAGE_LENGTH)),
                            LogDateTime = e.LogDateTime,
                            DeviceIdentifier = deviceIdentifier
                        };

						var isException = false;

						try
                        {
                            var response = await _gatewayService.PostLogMessageAsync(deviceMessage);
                            isException = !response.Succeeded;
                        }
                        catch
                        {
							isException = true;
                        }

						if (isException)
							await this.HandleLoggingFailureAsync(e, events.ToList());

                        await _loggedRepository.DeleteAsync(e);
                    }
                }
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        private async Task HandleLoggingFailureAsync(LogMessage failedLogMessage, List<LogMessage> currentMessages)
        {
            if (!currentMessages.Any(m => m.LogType == Enums.LogType.LogFailure))
            {
                string logMessage = ("An error occured trying to upload at least one log message. Those log messages have been deleted.");
                await this.LogEventAsync(logMessage, Enums.LogType.LogFailure);
            }
        }

        private async Task LogAndCloseAsync(Exception ex)
        {
            await this.LogEventAsync(ex);
            _closeApplication.CloseApp();
        }

        private Task InsertLogMessage(LogMessage logMessage)
        {
            try
            {
                return _loggedRepository.InsertAsync(logMessage);
            }
            catch (Exception ex)
            {
                MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "LogMessageRepository", "InsertAsync", ex.StackTrace);
                throw;
            }
        }

    }

}
