using IdentityServer.Legacy;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Exceptions;
using IdentityServer.Legacy.Models;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.UserInteraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker.EditVaultSecret
{
    public class EditVaultSecretPageModel : SecurePageModel, IEditVaultSecretPageModel
    {
        public EditVaultSecretPageModel(
            ISecretsVaultDbContext roleDbContext)
        {
            _secretsVaultDb = roleDbContext;
        }

        protected ISecretsVaultDbContext _secretsVaultDb = null;

        async protected Task LoadCurrentSecretAsync(string lockerName, string id)
        {
            this.LockerName = lockerName;
            this.CurrentSecret = (await _secretsVaultDb.GetVaultSecretsAsync(lockerName, CancellationToken.None))
                                        .Where(l => l.Name == id)
                                        .FirstOrDefault();
        }

        public string LockerName { get; private set; }
        public VaultSecret CurrentSecret { get; private set; }
    }
}
