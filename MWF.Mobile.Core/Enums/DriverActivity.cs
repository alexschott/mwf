using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Enums
{
    public enum DriverActivity
    {

        LogOn = 1,
        LogOff = 2,
        ClockIn = 3,
        ClockOut = 4,
        FuelVisit = 5,
        Begin = 9,
        Drive = 10,
        OnSite = 11,
        Cancel = 12,
        Suspend = 13,
        Resume = 14,
        Complete = 15,
        Problem = 16,
        ManifestComplete = 17,
        OffSite = 18,
        Comment = 19,
        End = 20,
        Accept = 21,
        WaterAdded = 22,
        Loaded = 23,
        Tip = 24,
        Trailer = 25,
        ScannedBarcodes = 26
    }
}
