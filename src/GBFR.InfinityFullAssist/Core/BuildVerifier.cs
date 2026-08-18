using System.Security.Cryptography;

namespace GBFR.InfinityFullAssist.Core;

public sealed class BuildVerifier
{
    public static readonly Version SupportedApplicationVersion = new(2, 0, 4);
    public const string SupportedSha256 =
        "F827F3C13CAA90B290FAB2FE7E28165A80448FDE0A3F7A96D79DAC6B8343FF2A";

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
