using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients;

public class ClientsModel : AdminPageModel
{
    private IClientDbContextModify _clientDb = null;
    public ClientsModel(IClientDbContext clientDbContext)
    {
        _clientDb = clientDbContext as IClientDbContextModify;
    }

    async public Task<IActionResult> OnGetAsync()
    {
        if (_clientDb != null)
        {
            this.Clients = await _clientDb.GetAllClients();

            Input = new NewClient();
        }

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        string clientId = Input.ClientId.Trim().ToLower();

        return await SecureHandlerAsync(async () =>
        {
            if (_clientDb != null)
            {
                var client = new ClientModel()
                {
                    ClientId = clientId,
                    ClientName = Input.ClientName?.Trim(),
                };

                #region Apply Client Templates

                client.ApplyTemplate(Input.ClientType, Input.ClientUrl, String.IsNullOrEmpty(Input.ApiScopes) ? null : Input.ApiScopes.Split(' '));

                #endregion

                await _clientDb.AddClientAsync(client);
            }
        }
        , onFinally: () => RedirectToPage("EditClient/Index", new { id = clientId })
        , successMessage: "Client successfully created"
        , onException: (ex) => RedirectToPage());
    }

    public IEnumerable<ClientModel> Clients { get; set; }

    [BindProperty]
    public NewClient Input { get; set; }

    public enum ClientTemplateType
    {
        Empty,
        WebApplication,
        ApiClient,
        JavascriptClient
    }

    public class NewClient
    {
        [Required, MinLength(3), RegularExpression(@"^[a-z0-9_\-\.]+$", ErrorMessage = "Only lowercase letters, numbers,-,_,.")]
        [DisplayName("Client Id")]
        public string ClientId { get; set; }

        [DisplayName("Client Name")]
        public string ClientName { get; set; }

        [DisplayName("Client Template")]
        [EnumDataType(typeof(ClientTemplateType))]
        public ClientTemplateType ClientType { get; set; }

        [DisplayName("Client Url")]
        [RegularExpression(@"^(https?:\/\/)([a-z\d\.-]+)(:\d+)?([\/\w \.-]*)*\/?$", ErrorMessage = "Invalid Url")]
        public string ClientUrl { get; set; }

        [DisplayName("Api Scopes")]
        public string ApiScopes { get; set; }
    }
}
