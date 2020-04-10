﻿using IdentityServer.Legacy.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class EditClientModel : PageModel, IEditClientModel
    {
        public EditClientModel(IClientDbContext clientDbContext)
        {
            _clientDb = clientDbContext as IClientDbContextModify;
        }

        #region IEditClientModel

        public Client CurrentClient { get; set; }

        #endregion

        async public Task LoadCurrentClientAsync(string id)
        {
            this.CurrentClient = await _clientDb.FindClientByIdAsync(id);
        }

        protected IClientDbContextModify _clientDb = null;
    }
}
