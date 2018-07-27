using System;

namespace MSBuildUI.Items
{
    [Flags]
    public enum BuildState
    {
        InProgress = 0x1000,
        ColorMask = 0x100F,
        Inactive = 0x0000,
        Waiting = 0x0001,
        Success = 0x0002,
        Warning = 0x0003,
        Error = 0x0004,
    }
}