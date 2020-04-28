using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy.Exceptions;
using IdentityServer.Legacy.Models;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IdentityServer.Legacy.Extensions;

namespace IdentityServer.Legacy.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class SecretsVaultController : ControllerBase
    {
        private readonly ISecretsVaultDbContext _secretsVaultDb;

        public SecretsVaultController(ISecretsVaultDbContext secretsVaultDb)
        {
            _secretsVaultDb = secretsVaultDb;
        }

        [HttpGet]
        async public Task<IActionResult> Get(string path)
        {
            try
            {
                string[] pathParts = path.Split('/');

                if (pathParts.Length < 2 || pathParts.Length > 3)
                {
                    throw new StatusMessageException($"Invalid path: { path }");
                }

                if(!this.User.GetScopes().Contains($"secrets-vault.{ pathParts[0] }")) {
                    return Unauthorized();
                }

                VaultSecretVersion secretVersion = null;
                if (pathParts.Length == 3)
                {
                    if (!long.TryParse(pathParts[2], out long versionTimeStamp))
                    {
                        throw new StatusMessageException($"Invalid version time stamp: { pathParts[2] }");
                    }

                    secretVersion = await _secretsVaultDb.GetSecretVersionAsync(pathParts[0], pathParts[1], versionTimeStamp, CancellationToken.None);
                }
                else
                {
                    secretVersion = (await _secretsVaultDb.GetVaultSecretVersionsAsync(pathParts[0], pathParts[1], CancellationToken.None))
                                        .OrderByDescending(s => s.VersionTimeStamp)
                                        .FirstOrDefault();
                }

                if(secretVersion==null)
                {
                    throw new StatusMessageException($"Secret { path } not found");
                }

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
            catch (Exception ex)
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        xxx=ex.Message,
                        errorMessage = "Internal error."
                    });
            }
        }
    }
}
