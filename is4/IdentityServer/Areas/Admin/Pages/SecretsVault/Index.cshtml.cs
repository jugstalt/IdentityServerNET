using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault
{
    public class IndexModel : SecurePageModel
    {
        public IEnumerable<SecretContainer> SecretContainers { get; set; }

        [BindProperty]
        public ContainerInputModel ContainerInput { get; set; }

        public class ContainerInputModel
        {
            public string ContainerName { get; set; }
        }

        public void OnGet()
        {
        }
    }
}
