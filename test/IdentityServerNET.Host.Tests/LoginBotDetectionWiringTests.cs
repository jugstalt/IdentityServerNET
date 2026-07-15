using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// The password step (<c>AccountController.LoginPassword</c>) already tar-pits repeated failures via
/// <see cref="ILoginBotDetection"/>, but the second-factor steps (<c>LoginWith2fa</c>,
/// <c>LoginWithRecoveryCode</c>) did not - an attacker who got past a leaked/weak password could
/// brute-force the 2FA code or recovery code with no friction at all. These tests confirm both pages
/// now feed failures into the same username-keyed bot-detection store, and that a successful sign-in
/// clears it again.
/// </summary>
[Collection(HostTestCollection.Name)]
public class LoginBotDetectionWiringTests
{
    private const string LoginPath = "/Account/Login";
    private const string LoginPasswordPath = "/Account/LoginPassword";
    private const string TwoFactorPath = "/Identity/Account/LoginWith2fa";
    private const string RecoveryCodePath = "/Identity/Account/LoginWithRecoveryCode";
    private const string IdentityCookiePrefix = ".AspNetCore.Identity.Application";
    private const string Password = "Passw0rd!";

    private readonly HostApplicationFactory _factory;

    public LoginBotDetectionWiringTests(HostApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task TwoFactor_Repeated_Invalid_Codes_Mark_User_Suspicious()
    {
        const string userName = "botdetection-2fa@identityserver.net";
        await SeedUserWithTwoFactorEnabledAsync(userName, Password);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await PostIdentifierStepAsync(client, userName);
        var passwordToken = await GetAntiforgeryTokenAsync(client, LoginPasswordPath);
        using var _ = await client.PostAsync(LoginPasswordPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"] = userName,
                ["Password"] = Password,
                ["button"] = "login",
                ["__RequestVerificationToken"] = passwordToken
            }));

        var twoFactorToken = await GetAntiforgeryTokenAsync(client, TwoFactorPath);

        // MaxFailCount defaults to 3 - submit exactly that many invalid codes (not a 4th, which would
        // additionally trigger the tar-pit delay inside BlockSuspicousUser and just slow the test down
        // without adding coverage here).
        for (var i = 0; i < 3; i++)
        {
            using var response = await client.PostAsync(TwoFactorPath, new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Input.TwoFactorCode"] = "000000",
                    ["__RequestVerificationToken"] = twoFactorToken
                }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var botDetection = scope.ServiceProvider.GetRequiredService<ILoginBotDetection>();
        Assert.True(await botDetection.IsSuspiciousUserAsync(userName));
    }

    [Fact]
    public async Task RecoveryCode_Repeated_Invalid_Codes_Mark_User_Suspicious()
    {
        const string userName = "botdetection-recovery@identityserver.net";
        await SeedUserWithTwoFactorEnabledAsync(userName, Password);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await PostIdentifierStepAsync(client, userName);
        var passwordToken = await GetAntiforgeryTokenAsync(client, LoginPasswordPath);
        using var _ = await client.PostAsync(LoginPasswordPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"] = userName,
                ["Password"] = Password,
                ["button"] = "login",
                ["__RequestVerificationToken"] = passwordToken
            }));

        var recoveryToken = await GetAntiforgeryTokenAsync(client, RecoveryCodePath);

        for (var i = 0; i < 3; i++)
        {
            using var response = await client.PostAsync(RecoveryCodePath, new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Input.RecoveryCode"] = "xxxx-yyyy-zzzz-invalid",
                    ["__RequestVerificationToken"] = recoveryToken
                }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var botDetection = scope.ServiceProvider.GetRequiredService<ILoginBotDetection>();
        Assert.True(await botDetection.IsSuspiciousUserAsync(userName));
    }

    [Fact]
    public async Task RecoveryCode_Successful_SignIn_Clears_Suspicious_State()
    {
        const string userName = "botdetection-recovery-success@identityserver.net";
        var recoveryCode = await SeedUserWithTwoFactorEnabledAsync(userName, Password, generateRecoveryCode: true);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await PostIdentifierStepAsync(client, userName);
        var passwordToken = await GetAntiforgeryTokenAsync(client, LoginPasswordPath);
        using var _ = await client.PostAsync(LoginPasswordPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"] = userName,
                ["Password"] = Password,
                ["button"] = "login",
                ["__RequestVerificationToken"] = passwordToken
            }));

        // MaxFailCount defaults to 3 - reach it exactly so IsSuspiciousUserAsync flips to true below.
        var recoveryToken = await GetAntiforgeryTokenAsync(client, RecoveryCodePath);
        for (var i = 0; i < 3; i++)
        {
            using var failResponse = await client.PostAsync(RecoveryCodePath, new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Input.RecoveryCode"] = "xxxx-yyyy-zzzz-invalid",
                    ["__RequestVerificationToken"] = recoveryToken
                }));
            Assert.Equal(HttpStatusCode.OK, failResponse.StatusCode);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var botDetection = scope.ServiceProvider.GetRequiredService<ILoginBotDetection>();
            Assert.True(await botDetection.IsSuspiciousUserAsync(userName));
        }

        // The next request now also hits the tar-pit guard (BlockSuspicousUser) before checking the
        // code, so this one takes ~BlockSuspiciousUserSeconds longer than the earlier failures.
        using var successResponse = await client.PostAsync(RecoveryCodePath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.RecoveryCode"] = recoveryCode!,
                ["__RequestVerificationToken"] = recoveryToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, successResponse.StatusCode);
        Assert.Contains(
            GetSetCookies(successResponse),
            c => c.StartsWith(IdentityCookiePrefix, StringComparison.OrdinalIgnoreCase));

        using var verifyScope = _factory.Services.CreateScope();
        var botDetectionAfter = verifyScope.ServiceProvider.GetRequiredService<ILoginBotDetection>();
        Assert.False(await botDetectionAfter.IsSuspiciousUserAsync(userName));
    }

    // -----------------------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------------------

    private async Task<string?> SeedUserWithTwoFactorEnabledAsync(string userName, string password, bool generateRecoveryCode = false)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userDb = sp.GetRequiredService<IUserDbContext>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userDb.FindByNameAsync(userName, CancellationToken.None);
        if (existing is null)
        {
            var newUser = new ApplicationUser
            {
                UserName = userName,
                Email = userName,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);
            var createResult = await userDb.CreateAsync(newUser, CancellationToken.None);
            Assert.True(createResult.Succeeded,
                "Seeding the test user failed: " +
                string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }

        var user = await userManager.FindByNameAsync(userName);
        Assert.NotNull(user);

        await userManager.SetTwoFactorEnabledAsync(user, true);

        if (!generateRecoveryCode)
        {
            return null;
        }

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 5);
        return codes!.First();
    }

    private static async Task PostIdentifierStepAsync(HttpClient client, string userName)
    {
        var identifierToken = await GetAntiforgeryTokenAsync(client, LoginPath);

        using var identifierResponse = await client.PostAsync(LoginPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"] = userName,
                ["button"] = "identify",
                ["__RequestVerificationToken"] = identifierToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, identifierResponse.StatusCode);
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
