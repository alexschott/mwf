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

namespace MWF.Mobile.Core.Services
{

    public class ImageUploadService : IImageUploadService
    {
        
        private readonly Repositories.IRepositories _repositories = null;
        private readonly IGpsService _gpsService = null;
        private readonly ILoggingService _loggingService = null;
        private readonly IReachability _reachability = null;
        private readonly IHttpService _httpService = null;

        public ImageUploadService(
            Repositories.IRepositories repositories,
            IGpsService gpsService,
            ILoggingService loggingService,
            IReachability reachability,
            IHttpService httpService)
        {
            _repositories = repositories;
            _gpsService = gpsService;
            _loggingService = loggingService;
            _reachability = reachability;
            _httpService = httpService;
        }

        /// <summary>
        /// This method sends photos and comments to bluesphere, if the sender is on an
        /// instruction page then the instruction will be associated with the photos
        /// </summary>
        /// <param name="comment">The comment for the photos</param>
        /// <param name="photos">The collection of photos to be sent up</param>
        public async Task SendPhotoAndCommentAsync(string comment, List<Image> photos, Guid driverID, string driverDisplayName, IEnumerable<MobileData> mobileDatas)
        {

            if (!_reachability.IsConnected())
                return;

            Mvx.Resolve<IToast>().Show("Now uploading images");

            Encoding encoding = Encoding.UTF8;
            int uploadedCount = 0;

            var config = await _repositories.ConfigRepository.GetAsync();

            if (config == null && string.IsNullOrWhiteSpace(config.HEUrl))
            {
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync("Your HE Url has not been setup, you cannot upload images unless it has been setup.");
                return;
            }

            UploadCameraImageObject imageUpload = new UploadCameraImageObject();
            imageUpload.Smp = _gpsService.GetSmpData(Enums.ReportReason.Comment);
            imageUpload.ID = Guid.NewGuid();
            imageUpload.DriverTitle = driverDisplayName;
            imageUpload.DriverId = driverID;
            imageUpload.Pictures = photos;
            imageUpload.Comment = comment;
            imageUpload.DateTimeOfUpload = DateTime.Now;

            //If the user is not on the manifest screen they should be on an instruction page
            if (mobileDatas != null)
            {
                imageUpload.MobileApplicationIDs = mobileDatas.Select(md => md.ID).ToList(); 
                imageUpload.OrderIDs = string.Join(",", mobileDatas.SelectMany( md => md.Order.Items.Select(i => i.ItemIdFormatted)));
            }

            foreach (var image in imageUpload.Pictures)
            {
                var postParameters = new Dictionary<string, string>();
                postParameters.Add("filename", image.Filename);
                postParameters.Add("fileformat", "jpg");
                postParameters.Add("MwfInternalId", image.ID.ToString());
                postParameters.Add("DriverName", imageUpload.DriverTitle);
                postParameters.Add("PhotoComment", string.IsNullOrEmpty(imageUpload.Comment) ? string.Empty : imageUpload.Comment);
                postParameters.Add("PhotoDateTime", imageUpload.DateTimeOfUpload.ToString("o"));
                postParameters.Add("Latitude", (_gpsService.GetLatitude() ?? 0d).ToString());
                postParameters.Add("Longitude", (_gpsService.GetLongitude() ?? 0d).ToString());
                postParameters.Add("MobileApplicationDataIds", (imageUpload.MobileApplicationIDs == null) ? string.Empty : string.Join(",", imageUpload.MobileApplicationIDs.Select( x => x.ToString())));
                postParameters.Add("HEOrderIds", string.IsNullOrWhiteSpace(imageUpload.OrderIDs) ? string.Empty : imageUpload.OrderIDs.ToString());

                Uri postUrl = new Uri(string.Format("{0}/Mwf/ReceiveMwfPhoto.aspx", config.HEUrl));

                using (var request = new HttpRequestMessage(HttpMethod.Post, postUrl.AbsoluteUri))
                using (var fileContent = new ByteArrayContent(image.Bytes))
                {
                    var formContent = new MultipartFormDataContent();

                    foreach (var postParameter in postParameters)
                    {
                        formContent.Add(new StringContent(postParameter.Value), postParameter.Key);
                    }

                    fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                    {
                        FileName = image.Filename,
                    };

                    formContent.Add(fileContent);

                    request.Content = formContent;
                    try
                    {

                        var response = await _httpService.SendAsyncPlainResponse<HttpResponseMessage>(request);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            await _loggingService.LogEventAsync("Image sent successfully.", Enums.LogType.Info);
                            uploadedCount++;
                        }
                        else
                            await _loggingService.LogEventAsync(string.Format("Image failed to send, Status Code: {0}.", response.StatusCode), Enums.LogType.Error);
                    }
                    catch(Exception ex)
                    {
                        throw;
                    }
                }
            }

            if (uploadedCount != imageUpload.Pictures.Count)
                await Mvx.Resolve<ICustomUserInteraction>().AlertAsync(string.Format("Only {0} of {1} were uploaded successful.", uploadedCount, imageUpload.Pictures.Count), title: "Upload Failed");
            else
                Mvx.Resolve<IToast>().Show("Successfully uploaded images");
        }

    }

}
