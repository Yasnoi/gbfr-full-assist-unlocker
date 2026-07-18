using GBFR.InfinityFullAssist.Core;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class BuildVerifierTests
{
    private readonly BuildVerifier _verifier = new();

    [Fact]
    public void ExactVersionAndHashAreAccepted()
    {
        var identity = new BuildIdentity(
            BuildVerifier.SupportedApplicationVersion,
            BuildVerifier.SupportedSha256.ToLowerInvariant());

        Assert.True(_verifier.IsSupported(identity));
    }

    [Fact]
    public void DifferentVersionIsRejected()
    {
        var identity = new BuildIdentity(new Version(2, 0, 3), BuildVerifier.SupportedSha256);

        Assert.False(_verifier.IsSupported(identity));
    }

    [Fact]
    public void DifferentHashIsRejected()
    {
        var identity = new BuildIdentity(BuildVerifier.SupportedApplicationVersion, new string('0', 64));

        Assert.False(_verifier.IsSupported(identity));
    }
}
