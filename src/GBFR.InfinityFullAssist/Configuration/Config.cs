using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reloaded.Mod.Interfaces;

namespace GBFR.InfinityFullAssist.Configuration;

public sealed class Config : IConfigurable
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { WriteIndented = true };

    [DisplayName("Enabled")]
    [Description("Allow the built-in Assist Mode and Full Assist Mode in Infinity quests on the verified game build.")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [DisplayName("Enable Assist Mode")]
    [Description("Allow the built-in Assist Mode in addition to Full Assist Mode.")]
    [DefaultValue(false)]
    public bool EnableAssistMode { get; set; }

    [DisplayName("Enable in Online Sessions")]
    [Description("Allow the built-in Assist Mode and Full Assist Mode in online Infinity quest sessions, including for Hosts and Guests.")]
    [DefaultValue(true)]
    public bool EnableOnlineSessions { get; set; } = true;

    [DisplayName("Diagnostic Logging")]
    [Description("Log build validation and one decision record per quest entry. Does not change behavior.")]
    [DefaultValue(false)]
    public bool DiagnosticLogging { get; set; }

    [DisplayName("Ignore Version Check")]
    [Description("Keep the mod active on unrecognized game versions and rely on runtime signature validation. Enable this to survive minor game updates; the hook still installs only when every required signature matches exactly.")]
    [DefaultValue(false)]
    public bool IgnoreVersionCheck { get; set; }

    [JsonIgnore]
    [Browsable(false)]
    public string? FilePath { get; private set; }

    [JsonIgnore]
    [Browsable(false)]
    public string? ConfigName { get; private set; }

    [JsonIgnore]
    [Browsable(false)]
    public Action? Save { get; private set; }

    internal static Config FromFile(string filePath, string configName)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException(
                "The configuration path must include a directory.",
                nameof(filePath));
        }

        Directory.CreateDirectory(directory);
        var config = File.Exists(filePath)
            ? Deserialize(File.ReadAllBytes(filePath))
            : new Config();

        config.FilePath = filePath;
        config.ConfigName = configName;
        config.Save = config.SaveToFile;
        return config;
    }

    internal static Config Deserialize(ReadOnlyMemory<byte> json)
    {
        var config = JsonSerializer.Deserialize<Config>(
            json.Span,
            SerializerOptions) ?? new Config();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty(nameof(EnableAssistMode), out _) &&
            root.TryGetProperty("EnablePartialAssist", out var legacySetting) &&
            legacySetting.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            config.EnableAssistMode = legacySetting.GetBoolean();
        }

        return config;
    }

    private void SaveToFile()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            throw new InvalidOperationException(
                "The configuration file path is unavailable.");
        }

        File.WriteAllText(
            FilePath,
            JsonSerializer.Serialize(this, SerializerOptions));
    }
}
