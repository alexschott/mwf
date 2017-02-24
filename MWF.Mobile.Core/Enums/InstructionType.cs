using System;
using System.Reflection;
using System.ComponentModel;

namespace MWF.Mobile.Core.Enums
{
    public enum InstructionType
    {
        Collect = 1,
        Deliver = 2,
        MessageWithPoint = 5,
        OrderMessage = 9,
        TrunkTo = 10,
        ProceedFrom = 12
    }
}