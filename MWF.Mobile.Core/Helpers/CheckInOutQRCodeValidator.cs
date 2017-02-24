using Cirrious.CrossCore;
using MWF.Mobile.Core.Enums;
using MWF.Mobile.Core.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Helpers
{
    public static class CheckInOutQRCodeValidator
    {
        public static bool IsValidQRCode(string qrCode, CheckInOutActions currentAction)
        {
            bool valid = false;
            try
            {
                Models.QRData qrData = JsonConvert.DeserializeObject<Models.QRData>(qrCode);
                if (qrData.deviceId != 0 && 
                    qrData.driverId != 0 &&
                    !string.IsNullOrEmpty(qrData.phoneNumber) &&
                    qrData.imei == Mvx.Resolve<IDeviceInfo>().IMEI &&
                    qrData.actionPerformed == (int)currentAction)
                    valid = true;
            }
            catch
            {
            }
            return valid;
        }
    }
}
