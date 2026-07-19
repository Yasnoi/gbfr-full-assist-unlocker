using Reloaded.Mod.Interfaces;

namespace GBFR.InfinityFullAssist.Configuration;

public sealed class Configurator : IConfiguratorV3
{
    private IConfigurable[]? _configurations;

    public string? ModFolder { get; private set; }
    public string? ConfigFolder { get; private set; }
    public ConfiguratorContext Context { get; private set; }

    public void SetModDirectory(string modDirectory) =>
        ModFolder = modDirectory;

    public void SetConfigDirectory(string configDirectory)
    {
        ConfigFolder = configDirectory;
        _configurations = null;
    }

    public void SetContext(in ConfiguratorContext context) =>
        Context = context;

    public IConfigurable[] GetConfigurations()
    {
        if (_configurations is not null)
        {
            return _configurations;
        }

        if (string.IsNullOrWhiteSpace(ConfigFolder))
        {
            throw new InvalidOperationException(
                "Reloaded-II did not provide a configuration directory.");
        }

        _configurations =
        [
            Config.FromFile(
                Path.Combine(ConfigFolder, "Config.json"),
                "Mod Configuration")
        ];
        return _configurations;
    }

    public bool TryRunCustomConfiguration() => false;

    public void Migrate(string oldDirectory, string newDirectory)
    {
        var oldPath = Path.Combine(oldDirectory, "Config.json");
        var newPath = Path.Combine(newDirectory, "Config.json");
        if (!File.Exists(oldPath) || File.Exists(newPath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(newDirectory);
            File.Move(oldPath, newPath);
        }
        catch (IOException)
        {
            // Reloaded-II can still create a fresh configuration.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the old file in place if the launcher cannot migrate it.
        }
    }
}
