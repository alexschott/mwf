using SQLite.Net.Attributes;
using MWF.Mobile.Core.Models.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{

    public class ScannedDelivery
    {
        public ScannedDelivery()
        {
            Barcodes = new List<Barcode>();
        }

        [XmlElement("CustomerName")]
        public string CustomerName { get; set; }

        [XmlElement("HasCustomerSigned")]
        public bool HasCustomerSigned { get; set; }

        [XmlArray("Barcodes")]
        [XmlArrayItem(typeof(Barcode), ElementName = "barcode")]
        public List<Barcode> Barcodes { get; set; }

    }

}
