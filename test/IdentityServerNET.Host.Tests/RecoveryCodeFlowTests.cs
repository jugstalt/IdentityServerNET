using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// End-to-end tests for the second-factor recovery-code sign-in flow.
///
/// Key invariant under test:
///   <see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/> succeeds whenever the
///   <c>TwoFactorUserId</c> cookie is present and the user has valid recovery codes — it does
///   <em>not</em> require <c>TwoFactorEnabled == true</c>. This is the mechanism that lets
///   passkey-only users (where <c>TwoFactorEnabled</c> is <see langword="false"/>) fall back to a
///   recovery code when their passkey is unavailable, because the passkey-only login branch in
///   <c>AccountController</c> already sets that cookie (lines 202–206).
/// </summary>
[Collection(HostTestCollection.Name)]
public class RecoveryCodeFlowTests
{
    private const string LoginPath           = "/Account/Login";
    private const string RecoveryCodePath    = "/Identity/Account/LoginWithRecoveryCode";
    private const string IdentityCookiePrefix = ".AspNetCore.Identity.Application";
    private const string Password            = "Passw0rd!";

    private readonly HostApplicationFactory _factory;

    public RecoveryCodeFlowTests(HostApplicationFactory factory) => _factory = factory;

    /// <summary>
    /// Happy-path: a user with TOTP 2FA enabled (which also sets the TwoFactorUserId cookie
    /// during <c>PasswordSignInAsync</c>) can complete sign-in with a valid recovery code.
    /// </summary>
    [Fact]
    public async Task RecoveryCode_Signs_In_After_TOTP_TwoFactor()
    {
        const string userName = "recovery-code-success@identityserver.net";

        var recoveryCode = await SeedUserWithRecoveryCodeAsync(userName, Password);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Step 1 – POST credentials; SignInManager sets the TwoFactorUserId cookie (RequiresTwoFactor).
        var loginToken = await GetAntiforgeryTokenAsync(client, LoginPath);
        using var loginResponse = await client.PostAsync(LoginPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"]                   = userName,
                ["Password"]                   = Password,
                ["button"]                     = "login",
                ["__RequestVerificationToken"] = loginToken
            }));

        // Should redirect toward the 2FA page (the TwoFactorUserId cookie is now in the jar).
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        // Step 2 – GET the recovery-code page (the cookie jar carries the TwoFactorUserId cookie).
        var recoveryToken = await GetAntiforgeryTokenAsync(client, RecoveryCodePath);

        // Step 3 – POST the recovery code.
        using var recoveryResponse = await client.PostAsync(RecoveryCodePath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.RecoveryCode"]         = recoveryCode,
                ["__RequestVerificationToken"] = recoveryToken
            }));

        // Successful recovery-code sign-in redirects …
        Assert.Equal(HttpStatusCode.Redirect, recoveryResponse.StatusCode);

        // … and issues the Identity application cookie.
        Assert.Contains(
            GetSetCookies(recoveryResponse),
            c => c.StartsWith(IdentityCookiePrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Negative-path: an invalid recovery code is rejected and no sign-in cookie is issued.
    /// </summary>
    [Fact]
    public async Task RecoveryCode_With_Wrong_Code_Is_Rejected()
    {
        const string userName = "recovery-code-failure@identityserver.net";

        await SeedUserWithRecoveryCodeAsync(userName, Password);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginToken = await GetAntiforgeryTokenAsync(client, LoginPath);
        using var _ = await client.PostAsync(LoginPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"]                   = userName,
                ["Password"]                   = Password,
                ["button"]                     = "login",
                ["__RequestVerificationToken"] = loginToken
            }));

        var recoveryToken = await GetAntiforgeryTokenAsync(client, RecoveryCodePath);
        using var recoveryResponse = await client.PostAsync(RecoveryCodePath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.RecoveryCode"]         = "xxxx-yyyy-zzzz-invalid",
                ["__RequestVerificationToken"] = recoveryToken
            }));

        // Invalid code re-renders the page (HTTP 200).
        Assert.Equal(HttpStatusCode.OK, recoveryResponse.StatusCode);

        // No Identity application cookie must be issued.
        Assert.DoesNotContain(
            GetSetCookies(recoveryResponse),
            c => c.StartsWith(IdentityCookiePrefix, StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Seeds a confirmed user, enables TOTP 2FA, and returns one freshly generated recovery code.
    /// Idempotent: if the user already exists the existing recovery code set is replaced and the
    /// first new code is returned.
    /// </summary>
    private async Task<string> SeedUserWithRecoveryCodeAsync(string userName, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var sp            = scope.ServiceProvider;
        var userDb        = sp.GetRequiredService<IUserDbContext>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var userManager   = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // Create the user if it does not already exist (mirrors SeedConfirmedUserAsync).
        var existing = await userDb.FindByNameAsync(userName, CancellationToken.None);
        if (existing is null)
        {
            var newUser = new ApplicationUser
            {
                UserName       = userName,
                Email          = userName,
                EmailConfirmed = true,
                SecurityStamp  = Guid.NewGuid().ToString()
            };
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);
            var createResult = await userDb.CreateAsync(newUser, CancellationToken.None);
            Assert.True(createResult.Succeeded,
                "Seeding the test user failed: " +
                string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }

        var user = await userManager.FindByNameAsync(userName);
        Assert.NotNull(user);

        // Enable two-factor so PasswordSignInAsync returns RequiresTwoFactor (sets the cookie).
        await userManager.SetTwoFactorEnabledAsync(user, true);

        // Generate a fresh set of recovery codes and return the first one.
        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 5);
        return codes.First();
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.Singleline);

        Assert.True(match.Success, $"Could not locate the antiforgery token on {path}.");
        return match.Groups[1].Value;
    }

    private static IEnumerable<string> GetSetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values
            : Enumerable.Empty<string>();
}
