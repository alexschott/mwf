using System;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using MWF.Mobile.Core.Models.Attributes;
using MWF.Mobile.Core.Services;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models.Instruction
{
    public class Additional : IBlueSphereEntity
    {
        public Additional()
        {
            ID = Guid.NewGuid();
            //If the xml isn't sent down for customer and signature confirmation then they are set to true. (Currently happens for Bomford style)
            CustomerNameRequiredForCollection = true;
            CustomerNameRequiredForDelivery = true;
            CustomerSignatureRequiredForCollection = true;
            CustomerSignatureRequiredForDelivery = true;
        }

        [Unique]
        [PrimaryKey]
        public Guid ID { get; set; }

        [ChildRelationship(typeof(Trailer), RelationshipCardinality.OneToZeroOrOne)]
        [JsonProperty("trailerid")]
        public Trailer Trailer { get; set; }

        [JsonProperty("istrailerconfirmationenabled")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool IsTrailerConfirmationEnabled { get; set; }

        [JsonProperty("customernamerequiredforcollection")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool CustomerNameRequiredForCollection { get; set; }

        [JsonProperty("customernamerequiredfordelivery")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool CustomerNameRequiredForDelivery { get; set; }

        [JsonProperty("customersignaturerequiredforcollection")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool CustomerSignatureRequiredForCollection { get; set; }

        [JsonProperty("customersignaturerequiredfordelivery")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool CustomerSignatureRequiredForDelivery { get; set; }

        [ForeignKey(typeof(Order))]
        public Guid OrderId { get; set; }
    }
}