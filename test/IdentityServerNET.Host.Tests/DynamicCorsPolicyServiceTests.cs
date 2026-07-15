using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// <see cref="DynamicCorsPolicyService"/> is IdentityServer4's <c>ICorsPolicyService</c> resolved
/// against the real client store instead of the (always-empty) static allow-list that
/// <c>DefaultCorsPolicyService</c> falls back to - without it, a client's <c>AllowedCorsOrigins</c>
/// configured in the admin UI was silently never enforced.
/// </summary>
public class DynamicCorsPolicyServiceTests
{
    private sealed class FakeClientDb : IClientDbContext, IClientDbContextModify
    {
        public readonly List<ClientModel> Store = new();
        public Task<ClientModel?> FindClientByIdAsync(string clientId) => Task.FromResult(Store.FirstOrDefault(c => c.ClientId == clientId));
        public Task AddClientAsync(ClientModel client) { Store.Add(client); return Task.CompletedTask; }
        public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null) => Task.CompletedTask;
        public Task RemoveClientAsync(ClientModel client) => Task.CompletedTask;
        public Task<IEnumerable<ClientModel>> GetAllClients() => Task.FromResult<IEnumerable<ClientModel>>(Store.ToArray());
    }

    private sealed class FakeReadOnlyClientDb : IClientDbContext
    {
        public Task<ClientModel?> FindClientByIdAsync(string clientId) => Task.FromResult<ClientModel?>(null);
    }

    private static DynamicCorsPolicyService CreateSut(FakeClientDb clientDb) =>
        new(clientDb, NullLogger<DynamicCorsPolicyService>.Instance);

    [Fact]
    public async Task Origin_Configured_On_A_Client_Is_Allowed()
    {
        var clientDb = new FakeClientDb();
        clientDb.Store.Add(new ClientModel
        {
            ClientId = "spa-client",
            AllowedCorsOrigins = new List<string> { "https://spa.example.com" }
        });

        var sut = CreateSut(clientDb);

        Assert.True(await sut.IsOriginAllowedAsync("https://spa.example.com"));
    }

    [Fact]
    public async Task Origin_Not_Configured_On_Any_Client_Is_Denied()
    {
        var clientDb = new FakeClientDb();
        clientDb.Store.Add(new ClientModel
        {
            ClientId = "spa-client",
            AllowedCorsOrigins = new List<string> { "https://spa.example.com" }
        });

        var sut = CreateSut(clientDb);

        Assert.False(await sut.IsOriginAllowedAsync("https://evil.example.com"));
    }

    [Fact]
    public async Task Origin_Comparison_Ignores_Path_And_Is_Case_Insensitive()
    {
        var clientDb = new FakeClientDb();
        clientDb.Store.Add(new ClientModel
        {
            ClientId = "spa-client",
            // A trailing path/query on the configured entry must not defeat the scheme+authority match.
            AllowedCorsOrigins = new List<string> { "https://Spa.Example.com/callback?x=1" }
        });

        var sut = CreateSut(clientDb);

        Assert.True(await sut.IsOriginAllowedAsync("https://spa.example.com"));
    }

    [Fact]
    public async Task Client_With_No_AllowedCorsOrigins_Does_Not_Throw()
    {
        // ClientModel defaults AllowedCorsOrigins to an empty (never null) list, but
        // DynamicCorsPolicyService also guards against a null collection defensively - cover both.
        var clientDb = new FakeClientDb();
        clientDb.Store.Add(new ClientModel { ClientId = "empty-cors-client" });
        clientDb.Store.Add(new ClientModel { ClientId = "null-cors-client", AllowedCorsOrigins = null! });

        var sut = CreateSut(clientDb);

        Assert.False(await sut.IsOriginAllowedAsync("https://spa.example.com"));
    }

    [Fact]
    public async Task Client_Store_That_Cannot_List_Clients_Denies_Everything()
    {
        var sut = new DynamicCorsPolicyService(new FakeReadOnlyClientDb(), NullLogger<DynamicCorsPolicyService>.Instance);

        Assert.False(await sut.IsOriginAllowedAsync("https://spa.example.com"));
    }
}
