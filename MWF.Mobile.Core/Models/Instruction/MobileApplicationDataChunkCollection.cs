using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
    [XmlRoot("mobiledatachunks")]
    public class MobileApplicationDataChunkCollection
    {
        [XmlElement("mobiledatachunk")]
        public List<MobileApplicationDataChunk> MobileApplicationDataChunkCollectionObject { get; set; }
    }
}
