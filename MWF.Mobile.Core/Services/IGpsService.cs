using Cirrious.MvvmCross.Plugins.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MWF.Mobile.Core.Enums;

namespace MWF.Mobile.Core.Services
{
    public interface IGpsService
    {
        string GetSmpData(ReportReason reportReason);
        double? GetLongitude();
        double? GetLatitude();
    }
}
