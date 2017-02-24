using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Net.Attributes;
using MWF.Mobile.Core.Converters;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Models.Attributes;
using Newtonsoft.Json;

namespace MWF.Mobile.Core.Models.Instruction
{

    [Table("InstructionOrder")]
    public class Order : IBlueSphereEntity
    {
        
        public Order()
        {
            ID = Guid.NewGuid();
        }

        private Guid _ID;

        [Unique]
        [PrimaryKey]
        public Guid ID 
        {
            get { return _ID; }
            set { _ID = value; }
        }

        [JsonProperty("id")]
        public string OrderId { get; set; }

        [JsonProperty("routeid")]
        public string RouteId { get; set; }

        [JsonProperty("title")]
        public string RouteTitle { get; set; }

        [JsonProperty("routedate")]
        public DateTime RouteDate { get; set; }

        [JsonProperty("sequence")]
        public int Sequence { get; set; }
        
        [JsonProperty("type")]
        public InstructionType Type { get; set; }
        
        [JsonProperty("priority")]
        public string Priority { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("description2")]
        public string Description2 { get; set; }

        [JsonProperty("arrive")]
        public DateTime Arrive { get; set; }

        [JsonProperty("depart")]
        public DateTime Depart { get; set; }

        [ChildRelationship(typeof(Additional), RelationshipCardinality.OneToZeroOrOne)]
        [JsonProperty("additional")]
        public Additional Additional { get; set; }

        [ChildRelationship(typeof(Address))]
        [JsonProperty("addresses")]
        [JsonConverter(typeof(JsonWrappedListConverter<Address>))]
        public List<Address> Addresses { get; set; }

        [ChildRelationship(typeof(Instruction))]
        [JsonProperty("instructions")]
        [JsonConverter(typeof(JsonWrappedListConverter<Instruction>))]
        public List<Instruction> Instructions { get; set; }

        [ChildRelationship(typeof(Item))]
        [JsonProperty("items")]
        [JsonConverter(typeof(JsonWrappedListConverter<Item>))]
        public List<Item> Items { get; set; }

        [ForeignKey(typeof(MobileData))]
        public Guid MobileDataId { get; set; }

        public bool IsBarcodeScanRequired()
        {
            return this.Type == Enums.InstructionType.Deliver && this.Items.Any(i => i.Additional.BarcodeScanRequiredForDelivery);
        }

        public bool HasBarcodes()
        {
            return this.Items.Any(i => i.BarcodesList.Any());
        }

    }

}