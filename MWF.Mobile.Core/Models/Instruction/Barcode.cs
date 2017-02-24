using MWF.Mobile.Core.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
    public class Barcode
    {
        [XmlText]
        public string BarcodeText { get; set; }

        [XmlAttribute("IsScanned")]
        public bool IsScanned { get; set; }

        [XmlAttribute("IsDelivered")]
        public bool IsDelivered { get; set; }

        [XmlAttribute("OrderId")]
        public string OrderID { get; set; }

        [XmlAttribute("DeliveryStatusCode")]
        public string DeliveryStatusCode { get; set; }

        [XmlAttribute("DamageStatusCode")]
        public string DamageStatusCode { get; set; }

        [XmlAttribute("DeliveryStatusNote")]
        public string DeliveryStatusNote { get; set; }

        [ForeignKey(typeof(Item))]
        [XmlIgnore]
        public Guid ItemId { get; set; }
    }
}
