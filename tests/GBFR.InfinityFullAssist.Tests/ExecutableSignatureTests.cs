using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class ExecutableSignatureTests
{
    private const string DefaultExecutablePath =
        @"D:\Steam\steamapps\common\Granblue Fantasy Relink\granblue_fantasy_relink.exe";

    [Fact]
    public void VerifiedExecutableContainsEachRuntimeSignatureExactlyOnce()
    {
        var executablePath =
            Environment.GetEnvironmentVariable("GBFR_TEST_EXECUTABLE") ??
            DefaultExecutablePath;
        if (!File.Exists(executablePath))
        {
            return;
        }

        var executable = File.ReadAllBytes(executablePath);

        AssertUniqueAt(
            executable,
            FullAssistGateHook.GateSignature,
            expectedRawOffset: 0x218690);
        AssertUniqueAt(
            executable,
            FullAssistGateHook.AssistDisableHandlerSignature,
            expectedRawOffset: 0x3207E30);
        AssertUniqueAt(
            executable,
            FullAssistGateHook.OnlineQuestModeSignature,
            expectedRawOffset: 0x3217900);
    }

    private static void AssertUniqueAt(
        byte[] executable,
        string signature,
        int expectedRawOffset)
    {
        var tokens = signature.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pattern = tokens
            .Select(token => token is "?" or "??"
                ? (byte?)null
                : Convert.ToByte(token, 16))
            .ToArray();
        var matches = new List<int>();
        var fixedRuns = new List<(int Offset, byte[] Bytes)>();
        var runStart = -1;
        for (var index = 0; index <= pattern.Length; index++)
        {
            if (index < pattern.Length && pattern[index].HasValue)
            {
                runStart = runStart < 0 ? index : runStart;
                continue;
            }

            if (runStart >= 0)
            {
                fixedRuns.Add((
                    runStart,
                    pattern[runStart..index].Select(value => value!.Value).ToArray()));
                runStart = -1;
            }
        }

        var anchor = fixedRuns.MaxBy(run => run.Bytes.Length);
        var searchOffset = 0;
        while (searchOffset <= executable.Length - anchor.Bytes.Length)
        {
            var relative = executable
                .AsSpan(searchOffset)
                .IndexOf(anchor.Bytes);
            if (relative < 0)
            {
                break;
            }

            var anchorAt = searchOffset + relative;
            var start = anchorAt - anchor.Offset;
            searchOffset = anchorAt + 1;
            if (start < 0 || start + pattern.Length > executable.Length)
            {
                continue;
            }

            var matchesHere = true;
            for (var offset = 0; offset < pattern.Length; offset++)
            {
                if (pattern[offset] is { } expected &&
                    executable[start + offset] != expected)
                {
                    matchesHere = false;
                    break;
                }
            }

            if (matchesHere)
            {
                matches.Add(start);
            }
        }

        Assert.Equal([expectedRawOffset], matches);
    }
}
