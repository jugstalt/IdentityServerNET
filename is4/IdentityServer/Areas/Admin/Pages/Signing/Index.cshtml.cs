using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services.Signing;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Signing;

public class IndexModel : SecurePageModel
{
    private readonly CustomTokenService _customToken;

    public IndexModel(CustomTokenService customToken)
    {
        _customToken = customToken;
    }

    [BindProperty]
    [DisplayName("Payload [Json]")]
    public string PayloadJson { get; set; }

    [BindProperty]
    [DisplayName("Expires [sec]")]
    public int Expires { get; set; }

    public string PayloadToken { get; set; }

    public void OnGet()
    {
        if (String.IsNullOrEmpty(PayloadJson))
        {
            PayloadJson =
@"{
  ""sub"":""1"",
  ""name"":""user1"",
  ""roles"":""""
}";
        }
    }

    async public Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            if (!ModelState.IsValid)
            {
                throw new StatusMessageException($"Type a valid payload.");
            }

            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(this.PayloadJson);

                NameValueCollection claims = new NameValueCollection();

                foreach (string key in dict.Keys)
                {
                    if (!key.Equals("lifetime", StringComparison.InvariantCultureIgnoreCase))
                    {
                        claims[key] = dict[key];
                    }
                }

                var token = _customToken.CreateCustomToken(claims, this.Expires > 0 ? this.Expires : 3600);
                PayloadToken = await _customToken.CreateSecurityTokenAsync(token);
            }
            catch (Exception ex)
            {
                throw new StatusMessageException(ex.Message);
            }
        }
        , onFinally: () => Page()
        , successMessage: ""
        , onException: (ex) => Page());
    }
}
