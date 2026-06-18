using System;

namespace IdentityServerNET.Models;

/// <summary>
/// Serialisation-friendly representation of a stored WebAuthn/Passkey credential.
/// Binary fields are encoded as regular Base64 strings so they round-trip through
/// all JSON-based storage backends (FileBlob, LiteDb, HttpProxy, InMemory).
/// The mapping to/from <c>Microsoft.AspNetCore.Identity.UserPasskeyInfo</c>
/// (byte[] fields) is performed in <c>UserStoreProxy</c> using
/// <c>Convert.ToBase64String</c> / <c>Convert.FromBase64String</c>.
/// </summary>
public class StoredPasskeyCredential
{
    /// <summary>Base64-encoded WebAuthn credential ID.</summary>
    public string CredentialId { get; set; } = "";

    /// <summary>Base64-encoded COSE public key.</summary>
    public string PublicKey { get; set; } = "";

    /// <summary>Optional display name chosen by the user during registration.</summary>
    public string? Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public uint SignCount { get; set; }

    public string[]? Transports { get; set; }

    public bool IsUserVerified { get; set; }

    public bool IsBackupEligible { get; set; }

    public bool IsBackedUp { get; set; }

    /// <summary>Base64-encoded attestation object (stored for audit; may be null).</summary>
    public string? AttestationObject { get; set; }

    /// <summary>Base64-encoded client data JSON (stored for audit; may be null).</summary>
    public string? ClientDataJson { get; set; }
}
