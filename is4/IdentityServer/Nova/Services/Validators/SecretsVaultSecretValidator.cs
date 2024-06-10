using IdentityServer.Nova.Services.SecretsVault;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.Validators
{
    public class SecretsVaultSecretValidator : ISecretValidator
    {
        private SecretsVaultManager _secretsVaultManager;

        public SecretsVaultSecretValidator(SecretsVaultManager secretsVaultManager)
        {
            _secretsVaultManager = secretsVaultManager;
        }

        async public Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
        {
            foreach (var secret in secrets
                     .Where(s => "SecretsVault-Secret".Equals(s.Type, StringComparison.CurrentCultureIgnoreCase)))
            {
                var secretsVersion = await _secretsVaultManager.GetSecretVersion(secret.Value);
                if (parsedSecret.Credential?.ToString() == secretsVersion.Secret)
                {
                    return new SecretValidationResult()
                    {
                        Success = true
                    };
                }
            }

            var result = new SecretValidationResult()
            {
                Success = false
            };

            return result;
        }
    }
}
