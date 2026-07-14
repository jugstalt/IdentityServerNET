using IdentityServerNET;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Services.SecretsVault;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Api.Controllers;

[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer-Secrets,Identity.Application")]
[ApiController]
public class SecretsVaultController : ControllerBase
{
    private readonly SecretsVaultManager _secretsVaultManager;

    public SecretsVaultController(SecretsVaultManager secretsVaultManager)
    {
        _secretsVaultManager = secretsVaultManager;
    }

    [HttpGet]
    async public Task<IActionResult> Get(string path)
    {
        try
        {
            string[] pathParts = path.Split('/');

            // The locker name itself carries its realm (e.g. "my-locker@acme") — the required role is
            // derived directly from it, no extra lookup needed. For a global locker this is unchanged
            // (AddRealmNamespace(null) is a no-op).
            var lockerRealm = pathParts[0].GetRealm();
            var requiredRole = KnownRoles.SecretsVaultAdministrator.AddRealmNamespace(lockerRealm);

            if (!this.User.GetScopes().Contains($"secrets-vault.{pathParts[0]}") &&
                !this.User.IsInRole(requiredRole))
            {
                throw new StatusMessageException($"Unauthorized user or client \"{this.User.GetUsernameOrClientId()}\"");
                //return Unauthorized();
            }

            VaultSecretVersion secretVersion = await _secretsVaultManager.GetSecretVersion(path);

            return new JsonResult(
                new
                {
                    success = true,
                    path = path,
                    secret = secretVersion
                });
        }
        catch (StatusMessageException sme)
        {
            return new JsonResult(
                new
                {
                    success = false,
                    errorMessage = sme.Message
                });
        }
        catch /*(Exception ex)*/
        {
            return new JsonResult(
                new
                {
                    success = false,
                    errorMessage = "Internal error."
                });
        }
    }
}
