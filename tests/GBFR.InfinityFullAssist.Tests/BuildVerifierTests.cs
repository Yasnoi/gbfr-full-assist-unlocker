using GBFR.InfinityFullAssist.Core;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class BuildVerifierTests
{
    private readonly BuildVerifier _verifier = new();

    [Fact]
    public void ExactVersionAndHashAreVerified()
    {
        var identity = new BuildIdentity(
            BuildVerifier.SupportedApplicationVersion,
            BuildVerifier.SupportedSha256.ToLowerInvariant());

        Assert.Equal(
            BuildVerificationStatus.Verified,
            _verifier.Verify(identity));
    }

    [Fact]
    public void DifferentVersionIsUnsupported()
    {
        var identity = new BuildIdentity(new Version(2, 0, 5), BuildVerifier.SupportedSha256);

        Assert.Equal(
            BuildVerificationStatus.Unsupported,
            _verifier.Verify(identity));
    }

    [Fact]
    public void DifferentHashIsUnverifiedButNotUnsupported()
    {
        var identity = new BuildIdentity(BuildVerifier.SupportedApplicationVersion, new string('0', 64));

        Assert.Equal(
            BuildVerificationStatus.Unverified,
            _verifier.Verify(identity));
    }

    [Fact]
    public void UnavailableHashIsUnverifiedButNotUnsupported()
    {
        var identity = new BuildIdentity(
            BuildVerifier.SupportedApplicationVersion,
            Sha256: null);

        Assert.Equal(
            BuildVerificationStatus.Unverified,
            _verifier.Verify(identity));
    }
}
