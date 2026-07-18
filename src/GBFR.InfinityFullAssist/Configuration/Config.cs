using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GBFR.InfinityFullAssist.Configuration;

public sealed class Config
{
    [DisplayName("Enabled")]
    [Description("Unlock the built-in Full Assist gate for Infinity quests on the verified game build.")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [DisplayName("Diagnostic Logging")]
    [Description("Log build validation and one decision record per quest entry. Does not change behavior.")]
    [DefaultValue(false)]
    public bool DiagnosticLogging { get; set; }

    [JsonIgnore]
    [Browsable(false)]
    public string? FilePath { get; internal set; }
}
