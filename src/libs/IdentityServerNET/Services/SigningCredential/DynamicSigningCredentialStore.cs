using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServerNET.Abstractions.SigningCredential;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.SigningCredential;

internal sealed class DynamicSigningCredentialStore : ISigningCredentialStore, IValidationKeysStore
{
    private readonly ISigningCredentialCertificateStorage _certificateStorage;
    private readonly SigningCredentialRenewalOptions _options;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private DateTimeOffset _cachedAtUtc = DateTimeOffset.MinValue;
    private SigningCredentials _cachedSigningCredentials;
    private IReadOnlyList<SecurityKeyInfo> _cachedValidationKeys = Array.Empty<SecurityKeyInfo>();

    public DynamicSigningCredentialStore(
            ISigningCredentialCertificateStorage certificateStorage,
            IOptions<SigningCredentialRenewalOptions> options)
    {
        _certificateStorage = certificateStorage;
        _options = options.Value;
    }

    public async Task<SigningCredentials> GetSigningCredentialsAsync()
    {
        await EnsureFreshAsync();
        return _cachedSigningCredentials;
    }

    public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
    {
        await EnsureFreshAsync();
        return _cachedValidationKeys;
    }

    private async Task EnsureFreshAsync()
    {
        if (IsCacheFresh())
        {
            return;
        }

        await _refreshLock.WaitAsync();
        try
        {
            if (IsCacheFresh())
            {
                return; // someone else refreshed while we were waiting for the lock
            }

            var certificatesByAge = (await _certificateStorage.GetCertificatesAsync())
                .OrderByDescending(cert => cert.NotBefore)
                .ToList();

            if (certificatesByAge.Count == 0)
            {
                return; // keep serving the previous snapshot rather than going key-less
            }

            _cachedSigningCredentials = new SigningCredentials(ToSecurityKey(certificatesByAge[0]), SecurityAlgorithms.RsaSha256);
            _cachedValidationKeys = certificatesByAge
                .Select(cert => new SecurityKeyInfo
                {
                    Key = ToSecurityKey(cert),
                    SigningAlgorithm = SecurityAlgorithms.RsaSha256
                })
                .ToArray();
            _cachedAtUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool IsCacheFresh()
        => _cachedSigningCredentials is not null && DateTimeOffset.UtcNow - _cachedAtUtc < _options.CacheDuration;

    private static X509SecurityKey ToSecurityKey(X509Certificate2 certificate)
    {
        var key = new X509SecurityKey(certificate);
        key.KeyId += SecurityAlgorithms.RsaSha256;
        return key;
    }
}
