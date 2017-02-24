using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Portable
{
    public interface IVibrate
    {
        void VibrateDevice();
        void VibrateDevice(long length);
    }
}
