using System.Text.Json;
using GBFR.InfinityFullAssist.Configuration;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class PackagingMetadataTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ModMetadataHasExpectedIdentityDependenciesAndIcon()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(
                RepositoryRoot,
                "src",
                "GBFR.InfinityFullAssist",
                "ModConfig.json")));
        var root = document.RootElement;

        Assert.Equal(
            "gbfr.qol.infinityfullassist",
            root.GetProperty("ModId").GetString());
        Assert.Equal(
            "Infinity Assist Unlock",
            root.GetProperty("ModName").GetString());
        Assert.Equal("AkieGZH", root.GetProperty("ModAuthor").GetString());
        Assert.Equal("1.2.3", root.GetProperty("ModVersion").GetString());
        Assert.Contains(
            "Assist Mode and Full Assist Mode",
            root.GetProperty("ModDescription").GetString(),
            StringComparison.Ordinal);
        Assert.Equal(
            "GBFR.InfinityFullAssist.dll",
            root.GetProperty("ModDll").GetString());
        Assert.Equal("Preview.png", root.GetProperty("ModIcon").GetString());

        var dependencies = root
            .GetProperty("ModDependencies")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.True(dependencies.SetEquals(
        [
            "Reloaded.Memory.SigScan.ReloadedII",
            "reloaded.sharedlib.hooks",
            "gbfrelink.utility.manager"
        ]));
    }

    [Fact]
    public void UserConfigurationSerializesOnlyThePublicSettings()
    {
        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(new Config()));
        var propertyNames = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            [
                "Enabled",
                "EnableAssistMode",
                "EnableOnlineSessions",
                "DiagnosticLogging",
                "IgnoreVersionCheck"
            ],
            propertyNames);
        Assert.True(document.RootElement.GetProperty("Enabled").GetBoolean());
        Assert.False(
            document.RootElement.GetProperty("EnableAssistMode").GetBoolean());
        Assert.True(
            document.RootElement
                .GetProperty("EnableOnlineSessions")
                .GetBoolean());
        Assert.False(
            document.RootElement.GetProperty("DiagnosticLogging").GetBoolean());
        Assert.False(
            document.RootElement.GetProperty("IgnoreVersionCheck").GetBoolean());
    }

    [Fact]
    public void ErSignatureFileContainsTheVerifiedRuntimePatterns()
    {
        var text = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "GBFR.InfinityFullAssist",
            "Signatures",
            "granblue_fantasy_relink_er.ini"));

        Assert.Contains("[Scans]", text, StringComparison.Ordinal);
        Assert.Contains(FullAssistGateHook.GateSignature, text, StringComparison.Ordinal);
        Assert.Contains(
            FullAssistGateHook.AssistDisableHandlerSignature,
            text,
            StringComparison.Ordinal);
        Assert.Contains(
            FullAssistGateHook.OnlineQuestModeSignature,
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalJsonPinsTheVerifiedSdk()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryRoot, "global.json")));
        var sdk = document.RootElement.GetProperty("sdk");

        Assert.Equal("9.0.316", sdk.GetProperty("version").GetString());
        Assert.Equal("latestPatch", sdk.GetProperty("rollForward").GetString());
        Assert.False(sdk.GetProperty("allowPrerelease").GetBoolean());
    }

    [Fact]
    public void ReadmesLinkToEachOther()
    {
        var english = File.ReadAllText(
            Path.Combine(RepositoryRoot, "README.md"));
        var chinese = File.ReadAllText(
            Path.Combine(RepositoryRoot, "README.zh-CN.md"));

        Assert.Contains("[简体中文](README.zh-CN.md)", english, StringComparison.Ordinal);
        Assert.Contains("[English](README.md)", chinese, StringComparison.Ordinal);
        Assert.Contains("EnableAssistMode", english, StringComparison.Ordinal);
        Assert.Contains("EnableAssistMode", chinese, StringComparison.Ordinal);
        Assert.Contains("EnableOnlineSessions", english, StringComparison.Ordinal);
        Assert.Contains("EnableOnlineSessions", chinese, StringComparison.Ordinal);
        Assert.Contains("1.2.3", english, StringComparison.Ordinal);
        Assert.Contains("1.2.3", chinese, StringComparison.Ordinal);
        Assert.Contains(
            "Assist Mode and Full Assist Mode",
            english,
            StringComparison.Ordinal);
        Assert.Contains("战斗辅助模式和战斗托管模式", chinese, StringComparison.Ordinal);
        Assert.Contains(
            "Infinity-Assist-Unlock-1.2.3.zip",
            english,
            StringComparison.Ordinal);
        Assert.Contains(
            "Infinity-Assist-Unlock-1.2.3.zip",
            chinese,
            StringComparison.Ordinal);
        Assert.Contains("Configure Mod", english, StringComparison.Ordinal);
        Assert.Contains("Mod 配置", chinese, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseScriptUsesThePublicModNameForTheArchive()
    {
        var script = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "build",
            "Pack-Release.ps1"));

        Assert.Contains(
            "Infinity-Assist-Unlock-$($metadata.ModVersion).zip",
            script,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "GBFR.InfinityFullAssist.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate GBFR.InfinityFullAssist.sln.");
    }
}
