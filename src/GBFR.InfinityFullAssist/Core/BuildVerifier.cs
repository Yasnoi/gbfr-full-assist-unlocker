using System.Security.Cryptography;

namespace GBFR.InfinityFullAssist.Core;

public sealed class BuildVerifier
{
    public static readonly Version SupportedApplicationVersion = new(2, 0, 2);
    public const string SupportedSha256 =
        "63340832BCF731FBC97796F686B05C988418E83D451D4A49B2244A85D00E297F";

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
