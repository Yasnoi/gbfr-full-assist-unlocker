using System.Security.Cryptography;

namespace GBFR.InfinityFullAssist.Core;

public sealed class BuildVerifier
{
    public static readonly Version SupportedApplicationVersion = new(2, 0, 3);
    public const string SupportedSha256 =
        "1BBBEC61AAB7F75FE328CF6BFE0247EBDBCEC6C404CEC12C032B8FFA41D22102";

    public BuildVerificationStatus Verify(in BuildIdentity identity)
    {
        if (identity.ApplicationVersion != SupportedApplicationVersion)
        {
            return BuildVerificationStatus.Unsupported;
        }

        return string.Equals(
            identity.Sha256,
            SupportedSha256,
            StringComparison.OrdinalIgnoreCase)
            ? BuildVerificationStatus.Verified
            : BuildVerificationStatus.Unverified;
    }

    public static string ComputeSha256(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        using var stream = File.OpenRead(executablePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
