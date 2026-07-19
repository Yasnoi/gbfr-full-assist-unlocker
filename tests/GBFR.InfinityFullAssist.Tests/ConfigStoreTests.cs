using System.Text.Json;
using GBFR.InfinityFullAssist.Configuration;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class ConfigStoreTests
{
    [Fact]
    public void ExistingConfigIsMigratedWithPartialAssistDisabled()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"gbfr-infinity-assist-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "Config.json");
            File.WriteAllText(
                path,
                """
                {
                  "Enabled": true,
                  "DiagnosticLogging": true
                }
                """);

            using var store = new ConfigStore(directory);

            Assert.True(store.Current.Enabled);
            Assert.False(store.Current.EnablePartialAssist);
            Assert.True(store.Current.DiagnosticLogging);

            using var document = JsonDocument.Parse(
                File.ReadAllBytes(path));
            Assert.False(document.RootElement
                .GetProperty("EnablePartialAssist")
                .GetBoolean());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
