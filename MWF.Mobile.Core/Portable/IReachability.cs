using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MWF.Mobile.Core.Portable
{


    public interface IReachability
    {
        /// <summary>
        /// Checks if the device has a network connection
        /// </summary>
        /// <returns>true if the device is connected to a network</returns>
        bool IsConnected();
    }
}