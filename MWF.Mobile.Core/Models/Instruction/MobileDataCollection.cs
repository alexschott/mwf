using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
    [XmlRoot("mobiledatum")]
    public class MobileDataCollection
    {
        [XmlElement("mobiledata")]
        public List<MobileData> MobileDataCollectionObject { get; set; }
    }
}
