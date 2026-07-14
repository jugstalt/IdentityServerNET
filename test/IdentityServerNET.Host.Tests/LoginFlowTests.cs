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
/// Approach B – interactive login flow: boots the <em>real</em> host (<see cref="Program"/>) and
/// exercises the actual ASP.NET Core MVC login pipeline end-to-end, i.e. the
/// <c>AccountController.Login</c> / <c>AccountController.LoginPassword</c> GET/POST actions
/// including antiforgery protection and <c>SignInManager.PasswordSignInAsync</c>.
///
/// Login is a two-step flow (identifier, then password — see <c>AccountController.Login</c> /
/// <c>LoginPassword</c>): step 1 posts <c>Username</c> + <c>button=identify</c> to
/// <see cref="LoginPath"/> and redirects to <see cref="LoginPasswordPath"/>; step 2 posts
/// <c>Username</c> + <c>Password</c> + <c>button=login</c> there.
///
/// A known, e-mail-confirmed user is seeded through the host's own
/// <see cref="UserManager{ApplicationUser}"/> (the in-memory user store is process-wide), so the
/// test drives a genuine browser-style login: fetch each page, read its antiforgery token,
/// post the credentials and assert on the resulting redirect / authentication cookie.
///
/// Like the rest of this project these tests boot the production host and are therefore kept
/// separate from the environment-independent Approach A protocol suite.
/// </summary>
[Collection(HostTestCollection.Name)]
public class LoginFlowTests
{
    // The host maps the default MVC route ({controller=Home}/{action=Index}); the login form
    // posts back to the same URL it is served from.
    private const string LoginPath = "/Account/Login";
    private const string LoginPasswordPath = "/Account/LoginPassword";

    // ASP.NET Core Identity's application cookie (issued by SignInManager on success).
    private const string IdentityCookiePrefix = ".AspNetCore.Identity.Application";

    // Satisfies the default Identity password policy (upper/lower/digit/non-alphanumeric, len >= 6).
    private const string Password = "Passw0rd!";

    private readonly HostApplicationFactory _factory;

    public LoginFlowTests(HostApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_With_Valid_Credentials_Issues_Authentication_Cookie()
    {
        const string userName = "flow-login-success@identityserver.net";
        await SeedConfirmedUserAsync(userName, Password);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await PostIdentifierStepAsync(client, userName);

        var passwordToken = await GetAntiforgeryTokenAsync(client, LoginPasswordPath);

        using var response = await client.PostAsync(LoginPasswordPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"] = userName,
                ["Password"] = Password,
                ["button"] = "login",
                ["__RequestVerificationToken"] = passwordToken
            }));

        // A successful local login redirects (to ~/ when there is no OIDC auth context) ...
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // ... and issues the ASP.NET Core Identity application cookie.
        Assert.Contains(
            GetSetCookies(response),
            c => c.StartsWith(IdentityCookiePrefix, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Is_Rejected()
    {
        const string userName = "flow-login-failure@identityserver.net";
        await SeedConfirmedUserAsync(userName, Password);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await PostIdentifierStepAsync(client, userName);

        var passwordToken = await GetAntiforgeryTokenAsync(client, LoginPasswordPath);

        using var response = await client.PostAsync(LoginPasswordPath, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"] = userName,
                ["Password"] = "the-wrong-password",
                ["button"] = "login",
                ["__RequestVerificationToken"] = passwordToken
            }));

        // A failed login re-renders the password view (HTTP 200) ...
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // ... and must NOT issue an authentication cookie.
        Assert.DoesNotContain(
            GetSetCookies(response),
            c => c.StartsWith(IdentityCookiePrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Step 1 of the two-step login: posts the identifier and follows the redirect into
    /// <see cref="LoginPasswordPath"/>, leaving the <c>LoginPendingUsername</c> TempData cookie
    /// (and the antiforgery cookie for the password page) in the client's cookie jar.
    /// </summary>
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

    /// <summary>
    /// Seeds an e-mail-confirmed user the same way the host's own <c>SetupService</c> does:
    /// hash the password with the registered <see cref="IPasswordHasher{ApplicationUser}"/> and
    /// persist the user through <see cref="IUserDbContext"/>. Going through
    /// <c>UserManager.CreateAsync(user, password)</c> is not possible here because this project's
    /// <c>UserStoreProxy</c> writes the password hash before the user row exists in the in-memory
    /// store (which throws "Unknown user"). Idempotent so the process-wide store can be reused.
    /// </summary>
    private async Task SeedConfirmedUserAsync(string userName, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userDb = sp.GetRequiredService<IUserDbContext>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        if (await userDb.FindByNameAsync(userName, CancellationToken.None) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
            EmailConfirmed = true, // the host requires a confirmed account to sign in
            SecurityStamp = Guid.NewGuid().ToString()
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        var result = await userDb.CreateAsync(user, CancellationToken.None);

        Assert.True(
            result.Succeeded,
            "Seeding the test user failed: " +
            string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
    }

    /// <summary>
    /// Fetches the login page and extracts the hidden antiforgery token. The GET response also
    /// sets the matching antiforgery cookie, which the cookie-aware test client carries into the
    /// subsequent POST automatically.
    /// </summary>
    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.Singleline);

        Assert.True(match.Success, "Could not locate the antiforgery token on the login page.");
        return match.Groups[1].Value;
    }

    private static IEnumerable<string> GetSetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values
            : Enumerable.Empty<string>();
}
