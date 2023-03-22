using IdentityServer.Legacy.Exceptions;
using IdentityServer.Legacy.Extensions;
using IdentityServer.Legacy.Models;
using IdentityServer.Legacy.Services.SecretsVault;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
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

                if (!this.User.GetScopes().Contains($"secrets-vault.{pathParts[0]}") &&
                    !this.User.IsInRole(KnownRoles.SecretsVaultAdministrator))
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
}
