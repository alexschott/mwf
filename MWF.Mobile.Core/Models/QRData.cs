using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models
{
    public class QRData
    {
        public int deviceId { get; set; }
        public string imei { get; set; }
        public string phoneNumber { get; set; }
        public int actionPerformed { get; set; }
        public int driverId { get; set; }
    }
}
