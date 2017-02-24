using MWF.Mobile.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Models
{
    public class CheckInOutData
    {
        public QRData qrData { get; set; }
        public string actualIMEI { get; set; }
        public CheckInOutActions actualActionPerformed { get; set; }
        public string termsAndConditions { get; set; }
        public string signature { get; set; }
        public string driverName { get; set; }
    }
}
