using Certes.Acme;

namespace GrayMint.Common.Acme;

/// <summary>Where an <see cref="AcmeClient"/> orders its certificates.</summary>
public static class AcmeDirectories
{
    /// <summary>Let's Encrypt, whose certificates every browser trusts.</summary>
    public static Uri LetsEncrypt => WellKnownServers.LetsEncryptV2;

    /// <summary>Let's Encrypt's staging, with far higher limits and certificates nothing trusts: for trying a setup out.</summary>
    public static Uri LetsEncryptStaging => WellKnownServers.LetsEncryptStagingV2;
}
