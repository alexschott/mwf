using System;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using MWF.Mobile.Core.Models.Attributes;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
    public class ItemAdditional : IBlueSphereEntity
    {
        public ItemAdditional()
        {
            ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]

        [XmlIgnore]
        public Guid ID { get; set; }

        [ChildRelationship(typeof(ConfirmQuantity), RelationshipCardinality.OneToZeroOrOne)]
        [JsonProperty("Confirm_Quantity")]
        [XmlElement("Confirm_Quantity")]
        public ConfirmQuantity ConfirmQuantity { get; set; }

        
        [ChildRelationship(typeof(DeliveryDescription), RelationshipCardinality.OneToZeroOrOne)]
        [JsonProperty("Delivery_Description")]
        [XmlElement("Delivery_Description")]
        public DeliveryDescription DeliveryDescription { get; set; }

        [JsonProperty("BarcodeScanRequiredForDelivery")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        [XmlIgnore]
        public bool BarcodeScanRequiredForDelivery { get; set; }

        [JsonProperty("BarcodeScanRequiredForCollection")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        [XmlIgnore]
        public bool BarcodeScanRequiredForCollection { get; set; }

        [JsonProperty("BypassCleanClausedScreen")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        [XmlIgnore]
        public bool BypassCleanClausedScreen { get; set; }

        [JsonProperty("BypassCommentsScreen")]
        [JsonConverter(typeof(JsonBooleanConverter))]
        [XmlIgnore]
        public bool BypassCommentsScreen { get; set; }

        [ForeignKey(typeof(Item))]
        [XmlIgnore]
        public Guid ItemId { get; set; }
    }
}