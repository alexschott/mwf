using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
   [XmlRoot("order")]
    public class MobileApplicationDataChunkContentOrder
    {
        [XmlElement("activities")]
        public List<MobileApplicationDataChunkContentActivities> MobileApplicationDataChunkContentOrderActivities { get; set; }
    }
}
