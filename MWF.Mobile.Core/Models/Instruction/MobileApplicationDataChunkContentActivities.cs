using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
    public class MobileApplicationDataChunkContentActivities
    {
        [XmlElement("activity")]
        public List<MobileApplicationDataChunkContentActivity> MobileApplicationDataChunkContentActivitiesObject { get; set; }
    }
}
