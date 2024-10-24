﻿using IdentityServerNET.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IUserRoleDbContext : IUserDbContext
{
    Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken);
    Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken);
    Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken);
}
