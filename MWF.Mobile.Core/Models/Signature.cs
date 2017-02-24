using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite.Net.Attributes;
using Newtonsoft.Json;
using MWF.Mobile.Core.Models.Attributes;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models
{

    // Model class which holds the driver signature for a safety check
    public class Signature: IBlueSphereEntity
    {

        public Signature()
        {
            this.ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]
        [XmlAttribute("id")]
        public Guid ID { get; set; }

        [XmlAttribute("title")]
        public string Title { get; set; }

        [XmlAttribute("encoded")]
        public string EncodedSignature { get; set; }

        [XmlAttribute("encodedimage")]
        public string EncodedImage { get; set; }

        [ForeignKey(typeof(SafetyCheckData))]
        [XmlIgnore]
        public Guid SafetyCheckDataID { get; set; }

    }

}
