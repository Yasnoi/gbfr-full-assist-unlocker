using System.Text.Json;
using GBFR.InfinityFullAssist.Configuration;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class ConfigStoreTests
{
    [Fact]
    public void LegacyPartialAssistSettingIsMigratedToAssistMode()
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
                  "EnablePartialAssist": true,
                  "DiagnosticLogging": true
                }
                """);

            using var store = new ConfigStore(directory);

            Assert.True(store.Current.Enabled);
            Assert.True(store.Current.EnableAssistMode);
            Assert.True(store.Current.DiagnosticLogging);

            using var document = JsonDocument.Parse(
                File.ReadAllBytes(path));
            Assert.True(document.RootElement
                .GetProperty("EnableAssistMode")
                .GetBoolean());
            Assert.False(document.RootElement.TryGetProperty(
                "EnablePartialAssist",
                out _));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
