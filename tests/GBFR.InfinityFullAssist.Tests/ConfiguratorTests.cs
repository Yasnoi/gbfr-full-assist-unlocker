using System.Text.Json;
using GBFR.InfinityFullAssist.Configuration;
using Reloaded.Mod.Interfaces;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class ConfiguratorTests
{
    [Fact]
    public void AssemblyExportsOneReloadedConfigurator()
    {
        var configuratorTypes = typeof(Config).Assembly
            .GetTypes()
            .Where(type =>
                type.IsPublic &&
                !type.IsAbstract &&
                typeof(IConfiguratorV3).IsAssignableFrom(type))
            .ToArray();

        Assert.Equal([typeof(Configurator)], configuratorTypes);
    }

    [Fact]
    public void SettingsCanBeSavedBeforeTheGameStarts()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"gbfr-infinity-configurator-{Guid.NewGuid():N}");

        try
        {
            var configurator = new Configurator();
            configurator.SetConfigDirectory(directory);

            var configurable = Assert.Single(
                configurator.GetConfigurations());
            var config = Assert.IsType<Config>(configurable);

            Assert.Equal("Mod Configuration", config.ConfigName);
            Assert.True(config.Enabled);
            Assert.False(config.EnablePartialAssist);
            Assert.False(config.DiagnosticLogging);
            Assert.NotNull(config.Save);

            config.Enabled = false;
            config.EnablePartialAssist = true;
            config.DiagnosticLogging = true;
            config.Save();

            var path = Path.Combine(directory, "Config.json");
            using var document = JsonDocument.Parse(
                File.ReadAllBytes(path));
            var root = document.RootElement;
            Assert.False(root.GetProperty("Enabled").GetBoolean());
            Assert.True(
                root.GetProperty("EnablePartialAssist").GetBoolean());
            Assert.True(
                root.GetProperty("DiagnosticLogging").GetBoolean());

            using var runtimeStore = new ConfigStore(directory);
            Assert.False(runtimeStore.Current.Enabled);
            Assert.True(runtimeStore.Current.EnablePartialAssist);
            Assert.True(runtimeStore.Current.DiagnosticLogging);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
