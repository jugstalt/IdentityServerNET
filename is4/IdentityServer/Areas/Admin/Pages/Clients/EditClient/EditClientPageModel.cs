using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient
{
    public class EditClientPageModel : AdminPageModel, IEditClientPageModel
    {
        public EditClientPageModel(IClientDbContext clientDbContext)
        {
            _clientDb = clientDbContext as IClientDbContextModify;
        }

        #region IEditClientModel

        public ClientModel CurrentClient { get; set; }

        #endregion

        async public Task LoadCurrentClientAsync(string id)
        {
            this.CurrentClient = await _clientDb.FindClientByIdAsync(id);
        }

        protected IClientDbContextModify _clientDb = null;
    }
}
