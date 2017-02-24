using SQLite.Net.Attributes;
using MWF.Mobile.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models
{
    public class LogMessage : IBlueSphereEntity
    {
        public LogMessage()
        {
            this.ID = Guid.NewGuid();
        }

        [Unique]
        [PrimaryKey]
        public Guid ID { get; set; }

        public DateTime LogDateTime { get; set; }

        public string Message { get; set; }

        public LogType LogType { get; set; }
    }
}
