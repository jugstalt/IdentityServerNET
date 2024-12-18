﻿using IdentityServerNET.Models.IdentityServerWrappers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IResourceDbContext
{
    Task<ApiResourceModel?> FindApiResourceAsync(string name);
    Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames);
    Task<IEnumerable<ApiResourceModel>> GetAllApiResources();

    Task<IdentityResourceModel?> FindIdentityResource(string name);
    Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources();
}
