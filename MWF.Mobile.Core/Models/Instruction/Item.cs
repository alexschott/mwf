using System;
using System.Collections.Generic;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models.Attributes;
using Newtonsoft.Json;
using System.Xml.Serialization;
using MWF.Mobile.Core.Converters;

namespace MWF.Mobile.Core.Models.Instruction
{
    public class Item : IBlueSphereEntity
    {
        private List<string> _barcodesList;

        public Item()
        {
            ID = Guid.NewGuid();
            _barcodesList = new List<string>();
        }

        [Unique]
        [PrimaryKey]
        [XmlIgnore]
        public Guid ID { get; set; }

        [JsonProperty("description")]
        [XmlElement("description")]
        public string Description { get; set; }

        [JsonProperty("id")]
        [XmlElement("id")]
        public string ItemId { get; set; }

        [Ignore]
        [XmlIgnore]
        public string ItemIdFormatted { get { return ItemId.Replace("Order", ""); } }

        [JsonProperty("quantity")]
        [XmlElement("quantity")]
        public string Quantity { get; set; }

        [JsonProperty("weight")]
        [XmlElement("weight")]
        public string Weight { get; set; }

        [JsonProperty("title")]
        [XmlElement("title")]
        public string Title { get; set; }

        [JsonProperty("deliveryordernumber")]
        [XmlIgnore]
        public string DeliveryOrderNumber { get; set; }

        [JsonProperty("businesstype")]
        [XmlIgnore]
        public string BusinessType { get; set; }

        [JsonProperty("deliverytype")]
        [XmlIgnore]
        public string GoodsType { get; set; }

        [ForeignKey(typeof(Order))]
        [XmlIgnore]
        [JsonIgnore]
        public Guid OrderId { get; set; }

        [JsonProperty("cases")]
        [XmlElement("cases")]
        public string Cases { get; set; }
        
        [JsonProperty("pallets")]
        [XmlElement("pallets")]
        public string Pallets { get; set; }

        [ChildRelationship(typeof(ItemAdditional), RelationshipCardinality.OneToZeroOrOne)]
        [JsonProperty("additional")]
        [XmlElement("additional")]
        public ItemAdditional Additional { get; set; }

        [JsonProperty("barcodes")]
        [JsonConverter(typeof(JsonWrappedListConverter<string>))]
        [Ignore]
        public List<string> BarcodesList
        {
            get
            {
                var cleanString = this.Barcodes ?? "";
                return new List<string>(cleanString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }
            set
            {
                this.Barcodes = string.Join("\n", value);
            }
        }

        public string Barcodes { get; set; }

    }
}