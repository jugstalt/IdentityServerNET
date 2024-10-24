using IdentityServerNET.Abstractions.SigningCredential;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.SigningCredential;

public class SigningCredentialCertificateInMemoryStorage : ISigningCredentialCertificateStorage
{
    private readonly ICertificateFactory _certificateFactory;
    private static ConcurrentDictionary<long, X509Certificate2> _certificates = null;

    public SigningCredentialCertificateInMemoryStorage(ICertificateFactory certificateFactory)
    {
        _certificateFactory = certificateFactory;
        _certificates = _certificates ?? new ConcurrentDictionary<long, X509Certificate2>();
    }

    public Task<X509Certificate2> GetCertificateAsync(string subject)
    {
        string key = subject;
        foreach (var s in subject.Split(',').Select(s => s.Trim()))
        {
            if (s.ToLower().StartsWith("cn="))
            {
                key = subject.Substring(3).Trim();
                break;
            }
        }

        if (long.TryParse(key, out long ticks) && _certificates.ContainsKey(ticks))
        {
            return Task.FromResult(_certificates[ticks]);
        }

        return null;
    }

    public Task<IEnumerable<X509Certificate2>> GetCertificatesAsync()
    {
        var fromTicks = DateTime.Now.AddDays(-60).Ticks;

        return Task.FromResult<IEnumerable<X509Certificate2>>(
            _certificates
                    .Where(kv => kv.Key > fromTicks)
                    .Select(kv => kv.Value)
                    .ToArray()
                );
    }

    public Task<X509Certificate2> GetRandomCertificateAsync(int maxAgeInDays)
    {
        var fromTicks = DateTime.Now.AddDays(-60).Ticks;

        var certsInTime =
            _certificates
                    .Where(kv => kv.Key > fromTicks)
                    .Select(kv => kv.Value)
                    .ToArray();

        if (certsInTime.Length == 0)
        {
            return Task.FromResult<X509Certificate2>(null);
        }

        var random = new Random();

        return Task.FromResult(certsInTime[random.Next(certsInTime.Length)]);
    }

    public Task RenewCertificatesAsync(int ifOlderThanDays = 60)
    {
        var fromTicks = DateTime.Now.AddDays(-ifOlderThanDays).Ticks;

        var newestCert = _certificates
                            .Where(kv => kv.Key > fromTicks)
                            .OrderByDescending(kv => kv.Key)
                            .Select(kv => kv.Value)
                            .FirstOrDefault();

        if (newestCert is null)
        {
            long ticks = DateTime.Now.Ticks;
            int expireDays = 36500;

            newestCert = _certificateFactory.CreateNewX509Certificate(ticks.ToString(), expireDays);
            _certificates.TryAdd(ticks, newestCert);
        }

        return Task.FromResult(newestCert);
    }
}
