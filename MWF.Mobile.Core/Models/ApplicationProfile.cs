using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models
{

    [JsonConverter(typeof(JsonWrappedItemConverter<ApplicationProfile>))]
    public class ApplicationProfile : IBlueSphereEntity
    {

        [Unique]
        [JsonProperty("@id")]
        [PrimaryKey]
        public Guid ID { get; set; }

        [JsonProperty("@title")]
        public string Title { get; set; }

        [JsonProperty("@intlink")]
        public int IntLink { get; set; }

        [JsonProperty("@poll")]
        public int PollingTime { get; set; }

        [JsonProperty("@poll2")]
        public int PollingTime2 { get; set; }

        [JsonProperty("@pollstart")]
        public int PollingStart { get; set; }

        [JsonProperty("@pollstop")]
        public int PollingStop { get; set; }

        [JsonProperty("@pollgprs")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool PollOnGPRS { get; set; }

        [JsonProperty("@pollquantity")]
        public int PollingQuantity { get; set; }

        [JsonProperty("@upload")]
        public int UploadTime { get; set; }

        [JsonProperty("@upload2")]
        public int UploadTime2 { get; set; }

        [JsonProperty("@uploadstart")]
        public int UploadStart { get; set; }

        [JsonProperty("@uploadstop")]
        public int UploadStop { get; set; }

        [JsonProperty("@uploadgprs")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool UploadGPRS { get; set; }

        [JsonProperty("@uploadquantity")]
        public int UploadQuantity { get; set; }

        [JsonProperty("@download")]
        public int DownloadTime { get; set; }

        [JsonProperty("@download2")]
        public int DownloadTime2 { get; set; }

        [JsonProperty("@downloadstart")]
        public int DownloadStart { get; set; }

        [JsonProperty("@downloadstop")]
        public int DownloadStop { get; set; }

        [JsonProperty("@downloadgprs")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool DownloadGPRS { get; set; }

        [JsonProperty("@downloadquantity")]
        public int DownloadQuantity { get; set; }

        [JsonProperty("@events")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool OnEvents { get; set; }

        [JsonProperty("@events_delay")]
        public int OnEventsDelay { get; set; }

        [JsonProperty("@retention")]
        public int DataRetention { get; set; }

        [JsonProperty("@span")]
        public int DataSpan { get; set; }

        [JsonProperty("@display_retention")]
        public int DisplayRetention { get; set; }

        [JsonProperty("@display_span")]
        public int DisplaySpan { get; set; }

        [JsonProperty("@weekends")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool IncludeWeekends { get; set; }

        [JsonProperty("@timeout")]
        public int Timeout { get; set; }

        [JsonProperty("@instantmsg")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool UseInstantMessaging { get; set; }

        [JsonProperty("@transactions")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool UseTransactions { get; set; }

        [JsonIgnore]
        public DateTime LastVehicleAndDriverSync { get; set; }

        [JsonProperty("@devicecheckinoutrequired")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool DeviceCheckInOutRequired { get; set; }

        [JsonProperty("@devicemanagementapiurl")]
        public string DeviceManagementAPIURL { get; set; }

        [JsonProperty("@devicecheckouttermsandconditions")]
        public string DeviceCheckOutTermsAndConditions { get; set; }
    }

}
