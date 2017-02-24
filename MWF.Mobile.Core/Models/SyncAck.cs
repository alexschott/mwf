using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models
{
    
    public class SyncAck
    {
        [XmlAttribute("MobileApplicationDataID")]
        public Guid MobileApplicationDataID { get; set; }

        [XmlAttribute("SyncAck")]
        public byte SyncAckState { get; set; }
    }

}
