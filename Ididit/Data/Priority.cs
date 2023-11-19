using System.ComponentModel;

namespace Ididit.Data;

public enum Priority
{
    [Description("None")]
    None,

    [Description("Very low")]
    VeryLow,

    [Description("Low")]
    Low,

    [Description("Medium")]
    Medium,

    [Description("High")]
    High,

    [Description("Very high")]
    VeryHigh
}
