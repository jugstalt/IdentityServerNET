using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using Microsoft.Extensions.Configuration;
using NuGet.Packaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services;

public class DevMigrationService
{
    private readonly MigrationModel _model;
    private readonly IResourceDbContextModify _resourceDb;

    public DevMigrationService(
            IConfiguration config,
            IResourceDbContext resourceDb = null)
    {
        _model = new MigrationModel();

        config.Bind("IdentityServer:Migrations", _model); 
        
        _resourceDb = resourceDb as IResourceDbContextModify;
    }

    async public Task<bool> MigrateAsync()
    {
        foreach (var identityResource in _model.IdentityResouces ?? [])
        {
            if (await _resourceDb.FindIdentityResource(identityResource.Name) is null)
            {
                await _resourceDb.AddIdentityResourceAsync(new IdentityResourceModel()
                {
                    Name = identityResource.Name
                });
            }
        }

        foreach (var apiResouce in _model.ApiResources ?? [])
        {
            if (await _resourceDb.FindApiResourceAsync(apiResouce.Name) is null)
            {
                List<ScopeModel> scopes = new List<ScopeModel>()
                {
                    new ScopeModel() { Name = apiResouce.Name }
                };

                if(apiResouce.Scopes.Any())
                {
                    scopes.AddRange(
                        apiResouce.Scopes.Select(scope => new ScopeModel()
                        {
                            Name = $"{apiResouce.Name}.{scope.Name}",
                        })
                    );
                }

                await _resourceDb.AddApiResourceAsync(new ApiResourceModel()
                {
                    Name = apiResouce.Name,
                    Scopes = scopes
                });
            }
        }

        return true;
    }
}
