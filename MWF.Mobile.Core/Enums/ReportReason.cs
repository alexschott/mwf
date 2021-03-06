namespace MWF.Mobile.Core.Enums
{

    public enum ReportReason
    {
        NewUnit = 0x00,
        ActiveReport = 0x01,
        DistanceReport = 0x02,
        SleepReport = 0x03,
        Requested = 0x04,
        Idle = 0x05,
        IgnitionOn = 0x06,
        IgnitionOff = 0x07,
        DriverLogOn = 0x08,
        DriverLogOff = 0x09,
        RawPowerLoss = 0x11,
        RawPowerRestore = 0x12,
        DeviceDocked = 0x13,
        DeviceUndocked = 0x14,
        GritterOff = 0x15,
        GritterOn = 0x16,
        SkipLocation = 0x17,
        PowerTakeOffOn = 0x18,
        PowerTakeOffOff = 0x19,
        DataReceived = 0x20,
        Overrevving = 0x21,
        HarshBraking = 0x22,
        Speeding = 0x23,
        ChangeInDirection = 0x24,
        IdleStart = 0x25,
        Input1Low = 0x30,
        Input2Low = 0x31,
        Input3Low = 0x32,
        Input1High = 0x33,
        Input2High = 0x34,
        Input3High = 0x35,
        Input1Pulse = 0x36,
        Input2Pulse = 0x37,
        Input3Pulse = 0x38,
        WeightThreshold = 0x3C,
        WeightIncrease = 0x3D,
        WeightDecrease = 0x3E,
        WeightTimedReport = 0x3F,
        Driver1StateChange = 0x40,
        Driver1CardChange = 0x41,
        Driver1TimeChange = 0x42,
        Driver2StateChange = 0x43,
        Driver2CardChange = 0x44,
        Driver2TimeChange = 0x45,
        TachographReport = 0x46,
        FuelGain = 0x47,
        FuelLoss = 0x48,
        ApplicationActivity = 0x82,
        SafetyReport = 0x85,
        VehicleAccident = 0x86,
        RoundStart = 0x96,
        RoundSuspend = 0x97,
        RoundComplete = 0x98,
        RoundCancel = 0x99,
        LegStart = 0x9A,
        LegSuspend = 0x9B,
        LegComplete = 0x9C,
        LegCancel = 0x9D,
        LegItem = 0x9E,
        TipIn = 0x9F,
        TipOut = 0xA0,
        RoundIssue = 0xA1,
        NonPresent = 0xA2,
        Begin = 0xAA,
        Drive = 0xAB,
        OnSite = 0xAC,
        Cancel = 0xAD,
        Suspend = 0xAE,
        Resume = 0xAF,
        Complete = 0xB0,
        Problem = 0xB1,
        ManifestComplete = 0xB2,
        OffSite = 0xB3,
        Comment = 0xB4,
        End = 0xB5,
        Accept = 0xB6,
        WaterAdded = 0xB7,
        Loaded = 0xB8,
        Tip = 0xB9,
        Trailer = 0xBA,
    }

}