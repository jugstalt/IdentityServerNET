using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.BotDetection;

public class IndexModel : SecurePageModel
{
    private readonly ILoginBotDetection _loginBotDetection;

    public IndexModel(ILoginBotDetection loginBotDetection = null)
    {
        _loginBotDetection = loginBotDetection;
    }

    // ILoginBotDetection is optional DI-wide (see ServiceCollectionExtensions), so the page must degrade
    // gracefully instead of throwing if a deployment somehow doesn't have it registered.
    public bool HasBotDetection => _loginBotDetection != null;

    public IReadOnlyCollection<LoginBotDetectionEntry> SuspiciousUsers { get; set; } = Array.Empty<LoginBotDetectionEntry>();
    public IReadOnlyCollection<LoginBotDetectionEntry> SuspiciousIps { get; set; } = Array.Empty<LoginBotDetectionEntry>();

    async public Task<IActionResult> OnGetAsync()
    {
        if (_loginBotDetection != null)
        {
            SuspiciousUsers = await _loginBotDetection.GetSuspiciousUsersAsync();
            SuspiciousIps = await _loginBotDetection.GetSuspiciousIpsAsync();
        }

        return Page();
    }

    async public Task<IActionResult> OnGetClearUserAsync(string key)
    {
        return await SecureHandlerAsync(async () =>
        {
            if (_loginBotDetection == null)
            {
                throw new StatusMessageException("Bot detection is not enabled.");
            }

            await _loginBotDetection.RemoveSuspiciousUserAsync(key);
        },
        onFinally: () => RedirectToPage(),
        successMessage: $"Cleared '{key}'.");
    }

    async public Task<IActionResult> OnGetClearIpAsync(string key)
    {
        return await SecureHandlerAsync(async () =>
        {
            if (_loginBotDetection == null)
            {
                throw new StatusMessageException("Bot detection is not enabled.");
            }

            // Unlike the username path, clearing an IP is ONLY ever done here (an explicit admin
            // action) - the login flow itself never calls this, see ILoginBotDetection.IsSuspiciousIpAsync.
            await _loginBotDetection.RemoveSuspiciousIpAsync(key);
        },
        onFinally: () => RedirectToPage(),
        successMessage: $"Cleared '{key}'.");
    }
}
