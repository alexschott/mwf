using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.Plugins.Messenger;
using MWF.Mobile.Core.Repositories;
using MWF.Mobile.Core.Utilities;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Services
{

    public class GatewayQueuedService : IGatewayQueuedService
    {

        private readonly IDeviceInfo _deviceInfo = null;
        private readonly IHttpService _httpService = null;
        private readonly Repositories.IGatewayQueueItemRepository _queueItemRepository = null;
        private readonly Portable.IReachability _reachability = null;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILoggingService _loggingService = null;
        private readonly IMvxMessenger _messenger = null;


        private readonly string _gatewayDeviceRequestUrl = null;

        private Timer _timer;

        private bool _isSubmitting = false;
        private bool _submitAgainOnCompletion = false;

        public GatewayQueuedService(
            IDeviceInfo deviceInfo,
            IHttpService httpService,
            Portable.IReachability reachability,
            IRepositories repositories,
            ILoggingService loggingService,
            IMvxMessenger messenger)
        {
            _deviceInfo = deviceInfo;
            _httpService = httpService;
            _queueItemRepository = repositories.GatewayQueueItemRepository;
            _reachability = reachability;
            _loggingService = loggingService;
            _messenger = messenger;


            //TODO: read this from config or somewhere?

            _gatewayDeviceRequestUrl = "http://blueport.gateway.fleetwoodmobile.net:7090/api/gateway/devicerequest";


            //Local url will need to change the station number
            //_gatewayDeviceRequestUrl = "http://192.168.3.133:17337/api/gateway/devicerequest";

            _deviceRepository = repositories.DeviceRepository;
        }

        public void StartQueueTimer()
        {
            if (_timer == null)
                _timer = new Timer(async state => await this.UploadQueueAsync(), null, 60000);
        }

        public void StopQueueTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        /// <summary>
        /// Add a single-action command without data to the queue and trigger submission to the MWF Mobile gateway web service
        /// </summary>
        /// <remarks>
        /// Note that submission will only occur if the GatewayQueueTimerService has been started (i.e. by first calling StartQueueTimer())
        /// </remarks>
        public async Task AddToQueueAsync(string command, Models.GatewayServiceRequest.Parameter[] parameters = null)
        {
            var content = await CreateRequestContentAsync(command, parameters);
            await this.AddToQueueAsync(content);
        }

        /// <summary>
        /// Add a single-action command with data to the queue and trigger submission to the MWF Mobile gateway web service
        /// </summary>
        /// <remarks>
        /// Note that submission will only occur if the GatewayQueueTimerService has been started (i.e. by first calling StartQueueTimer())
        /// </remarks>
        public async Task AddToQueueAsync<TData>(string command, TData data, Models.GatewayServiceRequest.Parameter[] parameters = null)
            where TData : class
        {
            var content = await CreateRequestContentAsync(command, data, parameters);
            await this.AddToQueueAsync(content);
        }

        public async Task AddToQueueAsync<TData>(IEnumerable<Models.GatewayServiceRequest.Action<TData>> actions) where TData : class
        {
            var content = await CreateRequestContentAsync(actions);
            await this.AddToQueueAsync(content);
        }

        private async Task AddToQueueAsync(Models.GatewayServiceRequest.Content requestContent)
        {
			Models.LogMessage exceptionMsg = null;

			try
            {
                var serializedContent = JsonConvert.SerializeObject(requestContent);
                var queueItem = new Models.GatewayQueueItem { ID = Guid.NewGuid(), JsonSerializedRequestContent = serializedContent, QueuedDateTime = DateTime.Now };

                try
                {
                    await _queueItemRepository.InsertAsync(queueItem);
                }
                catch (Exception ex)
                {
                    MvxTrace.Error("\"{0}\" in {1}.{2}\n{3}", ex.Message, "GatewayQueueItemRepository", "InsertAsync", ex.StackTrace);
                    throw;
                }

                // Always attempt to sync with the MWF Mobile Gateway service whenever items are added to the queue (providing the GatewayQueueTimerService has been started)
                await UploadQueueAsync();
            }
            catch (Exception ex)
            {
				exceptionMsg = _loggingService.GetExceptionLogMessage(ex);
            }

			if (exceptionMsg != null)
				await _loggingService.LogEventAsync(exceptionMsg);
        }

        public async Task UploadQueueAsync()
        {
			Models.LogMessage exceptionMsg = null;

            try {
                await SubmitQueueAsync();
                await _loggingService.UploadLoggedEventsAsync();
            }
            catch (Exception ex)
            {
				exceptionMsg = _loggingService.GetExceptionLogMessage(ex);
            }

			if (exceptionMsg != null)
				await _loggingService.LogEventAsync(exceptionMsg);
        }

        object syncLock = new object();

        private async Task SubmitQueueAsync()
        {
            Mvx.Trace("Submit Queue");

            if (_isSubmitting)
            {
                _submitAgainOnCompletion = true;
                return;
            }

            _isSubmitting = true;
			Models.LogMessage exceptionMsg = null;

            try
            {
                if (!_reachability.IsConnected())
                    return;

                IEnumerable<Models.GatewayQueueItem> queuedItems = await _queueItemRepository.GetAllInQueueOrderAsync();

                if (queuedItems != null)
                {
                    foreach (var queuedItem in queuedItems)
                    {
                        var submitted = true;

                        await this.ServiceCallAsync(queuedItem.JsonSerializedRequestContent);
                        await _queueItemRepository.DeleteAsync(queuedItem);

                        //TODO: should we attempt remaining items if one fails or bail out at this point?
                        if (!submitted)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
				exceptionMsg = _loggingService.GetExceptionLogMessage(ex);
            }
            finally
            {
                _isSubmitting = false;
                _timer.Reset();
            }

			if (exceptionMsg != null)
				await _loggingService.LogEventAsync(exceptionMsg);

            if (_submitAgainOnCompletion)
            {
                _submitAgainOnCompletion = false;
                await SubmitQueueAsync();
            }
        }

        private async Task<bool> ServiceCallAsync(string jsonSerializedRequestContent)
        {
            var response = await this.PostAsync(jsonSerializedRequestContent);

            if (!response.Succeeded && response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _messenger.Publish(new Messages.InvalidLicenseNotificationMessage(this));
                return false;
            }

            return response.Succeeded && response.Content.Actions.Any() && response.Content.Actions.All(a => a.Ack);
        }

        private Task<HttpResult<Models.GatewayServiceResponse.Response>> PostAsync(string jsonSerializedRequestContent)
        {
            return _httpService.PostJsonAsync<Models.GatewayServiceResponse.Response>(jsonSerializedRequestContent, _gatewayDeviceRequestUrl);
        }

        /// <summary>
        /// Create a single-action request's content with data - the data will be serialized as xml
        /// </summary>
        private Task<Models.GatewayServiceRequest.Content> CreateRequestContentAsync<TData>(string command, TData data, IEnumerable<Models.GatewayServiceRequest.Parameter> parameters = null)
            where TData : class
        {
            return this.CreateRequestContentAsync(new[]
            {
                new Core.Models.GatewayServiceRequest.Action<TData>
                {
                    Command = command,
                    Data = data,
                    Parameters = parameters,
                }
            });
        }

        /// <summary>
        /// Create the request content, allowing multiple actions per request and serializing the data
        /// </summary>
        private Task<Models.GatewayServiceRequest.Content> CreateRequestContentAsync<TData>(IEnumerable<Models.GatewayServiceRequest.Action<TData>> actions)
            where TData : class
        {
            var xmlSerializedActions = actions.Select(a => new Models.GatewayServiceRequest.Action
            {
                Command = a.Command,
                ContentXml = XmlSerialize(a.Data),
                Parameters = a.Parameters,
            });

            return CreateRequestContentAsync(xmlSerializedActions);
        }

        /// <summary>
        /// Create the request content, allowing multiple actions per request
        /// </summary>
        private async Task<Models.GatewayServiceRequest.Content> CreateRequestContentAsync(IEnumerable<Models.GatewayServiceRequest.Action> actions)
        {
            Models.Device device = await _deviceRepository.GetAllAsync().ContinueWith(x => x.Result.FirstOrDefault()) ;
            var deviceIdentifier = device == null ? _deviceInfo.GetDeviceIdentifier() : device.DeviceIdentifier;

            return new Core.Models.GatewayServiceRequest.Content
            {
                DeviceIdentifier = deviceIdentifier,
                Password = _deviceInfo.GatewayPassword,
                MobileApplication = _deviceInfo.MobileApplication,
                Actions = actions,
            };
        }

        private static string XmlSerialize<TData>(TData data)
            where TData : class
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(TData));
            var settings = new System.Xml.XmlWriterSettings { OmitXmlDeclaration = true };
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            var stringBuilder = new StringBuilder();

            using (var xmlWriter = System.Xml.XmlWriter.Create(stringBuilder, settings))
            {
                serializer.Serialize(xmlWriter, data, namespaces);
            }

            return stringBuilder.ToString();
        }

    }

}
