using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models
{
    
    public class GatewayQueueItem
        : IBlueSphereEntity
    {

        public GatewayQueueItem()
        {
            this.ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]
        public Guid ID { get; set; }

        public string JsonSerializedRequestContent { get; set; }

        public DateTime QueuedDateTime { get; set; }

    }

}
