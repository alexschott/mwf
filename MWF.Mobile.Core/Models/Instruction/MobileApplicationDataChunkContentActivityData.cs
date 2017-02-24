using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MWF.Mobile.Core.Models.Instruction
{
    public class MobileApplicationDataChunkContentActivityData
    {
        public MobileApplicationDataChunkContentActivityData()
        {
            Order = new List<Item>();
        }

        //This is used for sending back the updated order to bluesphere
        [XmlArrayItem(typeof(Item), ElementName = "item")]
        public List<Item> Order { get; set; }


        [XmlElement(typeof(Trailer), ElementName = "TrailerID")]
        public Trailer Trailer { get; set; }
    }
}
