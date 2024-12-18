﻿using IdentityServerNET.Models;

namespace IdentityServerNET;

public class KnownRoles
{
    public const string UserAdministrator = "identityserver-user-administrator";
    public const string RoleAdministrator = "identityserver-role-administrator";
    public const string ResourceAdministrator = "identityserver-resource-administrator";
    public const string ClientAdministrator = "identityserver-client-administrator";
    public const string SecretsVaultAdministrator = "identityserver-secretsvault-administrator";
    public const string SigningAdministrator = "identityserver-signing-administrator";

    public ApplicationRole UserAdministratorRole()
    {
        return new ApplicationRole()
        {
            Id = KnownRoles.UserAdministrator,
            Name = KnownRoles.UserAdministrator,
            Description = "Role to administrate user accounts"
        };
    }

    public ApplicationRole RoleAdministratorRole()
    {
        return new ApplicationRole()
        {
            Id = KnownRoles.RoleAdministrator,
            Name = KnownRoles.RoleAdministrator,
            Description = "Role to administrate user roles"
        };
    }

    public ApplicationRole ResourceAdministratorRole()
    {
        return new ApplicationRole()
        {
            Id = KnownRoles.ResourceAdministrator,
            Name = KnownRoles.ResourceAdministrator,
            Description = "Role to administrate resources (APIs and Identities)"
        };
    }

    public ApplicationRole ClientAdministratorRole()
    {
        return new ApplicationRole()
        {
            Id = KnownRoles.ClientAdministrator,
            Name = KnownRoles.ClientAdministrator,
            Description = "Role to administrate clients"
        };
    }

    public ApplicationRole SecretsVaultAdministratorRole()
    {
        return new ApplicationRole()
        {
            Id = KnownRoles.SecretsVaultAdministrator,
            Name = KnownRoles.SecretsVaultAdministrator,
            Description = "Role to administrate secret vault"
        };
    }

    public ApplicationRole SigningAdministratorRole()
    {
        return new ApplicationRole()
        {
            Id = KnownRoles.SigningAdministrator,
            Name = KnownRoles.SigningAdministrator,
            Description = "Role to sign payload with signung UI"
        };
    }
}
