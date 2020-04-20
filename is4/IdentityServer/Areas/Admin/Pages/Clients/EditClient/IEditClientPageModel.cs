using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient
{
    public interface IEditClientPageModel
    {
        public Client CurrentClient { get; set; }
    }
}
