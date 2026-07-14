using IdentityServerNET.Abstractions.SigningCredential;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.SigningCredential;

internal sealed class SigningCredentialRenewalBackgroundService : BackgroundService
{
    private readonly ISigningCredentialCertificateStorage _certificateStorage;
    private readonly SigningCredentialRenewalOptions _options;
    private readonly ILogger<SigningCredentialRenewalBackgroundService> _logger;

    public SigningCredentialRenewalBackgroundService(
            ISigningCredentialCertificateStorage certificateStorage,
            IOptions<SigningCredentialRenewalOptions> options,
            ILogger<SigningCredentialRenewalBackgroundService> logger)
    {
        _certificateStorage = certificateStorage;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.CheckInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await _certificateStorage.RenewCertificatesAsync(_options.RenewIfOlderThanDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Signing credential renewal check failed.");
            }
        }
    }
}
