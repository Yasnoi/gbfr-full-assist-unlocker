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
    [Description("Allow the built-in Full Assist Mode and Partial Assist Mode in Infinity quests on the verified game build.")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [DisplayName("Enable Partial Assist Mode")]
    [Description("Allow the built-in Partial Assist Mode in addition to Full Assist Mode.")]
    [DefaultValue(false)]
    public bool EnablePartialAssist { get; set; }

    [DisplayName("Diagnostic Logging")]
    [Description("Log build validation and one decision record per quest entry. Does not change behavior.")]
    [DefaultValue(false)]
    public bool DiagnosticLogging { get; set; }

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
        var config = (File.Exists(filePath)
            ? JsonSerializer.Deserialize<Config>(
                File.ReadAllBytes(filePath),
                SerializerOptions)
            : new Config()) ?? new Config();

        config.FilePath = filePath;
        config.ConfigName = configName;
        config.Save = config.SaveToFile;
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
