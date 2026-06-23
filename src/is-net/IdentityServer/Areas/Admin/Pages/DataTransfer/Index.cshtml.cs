#nullable enable

using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using IdentityServerNET.Models.DataTransfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.DataTransfer;

public class IndexModel : SecurePageModel
{
    private readonly IConfiguration _configuration;
    private readonly IAdminUserDbContext? _userDb;
    private readonly IAdminRoleDbContext? _roleDb;
    private readonly IClientDbContextModify? _clientDb;
    private readonly IResourceDbContextModify? _resourceDb;

    public IndexModel(
        IConfiguration configuration,
        IUserDbContext? userDb = null,
        IRoleDbContext? roleDb = null,
        IClientDbContext? clientDb = null,
        IResourceDbContext? resourceDb = null)
    {
        _configuration = configuration;
        _userDb = userDb as IAdminUserDbContext;
        _roleDb = roleDb as IAdminRoleDbContext;
        _clientDb = clientDb as IClientDbContextModify;
        _resourceDb = resourceDb as IResourceDbContextModify;
    }

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    public ImportSummary? LastImport { get; set; }

    public IActionResult OnGet()
    {
        if (!_configuration.AllowDataTransfer())
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!_configuration.AllowDataTransfer())
            return NotFound();

        var export = new IdentityServerExportModel
        {
            ExportedAt = DateTime.UtcNow.ToString("O"),
            SourceOrigin = _configuration["IdentityServer:PublicOrigin"] ?? ""
        };

        if (_userDb != null)
            export.Users = await LoadAllUsersAsync();

        if (_roleDb != null)
            export.Roles = await LoadAllRolesAsync();

        if (_clientDb != null)
            export.Clients = await _clientDb.GetAllClients();

        if (_resourceDb != null)
        {
            export.ApiResources = await _resourceDb.GetAllApiResources();
            export.IdentityResources = await _resourceDb.GetAllIdentityResources();
        }

        var json = JsonConvert.SerializeObject(export, Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);
        var filename = $"is-net-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(bytes, "application/json", filename);
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (!_configuration.AllowDataTransfer())
            return NotFound();

        if (ImportFile is null || ImportFile.Length == 0)
        {
            StatusMessage = "Error: No file selected.";
            return Page();
        }

        IdentityServerExportModel export;
        try
        {
            using var stream = ImportFile.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            export = JsonConvert.DeserializeObject<IdentityServerExportModel>(json)
                     ?? throw new Exception("File could not be parsed as an IdentityServer export.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: Could not read import file — {ex.Message}";
            return Page();
        }

        var summary = new ImportSummary();

        // Roles first — users may reference them
        if (_roleDb != null)
        {
            foreach (var role in export.Roles ?? [])
            {
                try
                {
                    var existing = await _roleDb.FindByNameAsync(role.NormalizedName ?? role.Name.ToUpperInvariant(), CancellationToken.None);
                    if (existing != null) { summary.RolesSkipped++; continue; }
                    role.Id ??= Guid.NewGuid().ToString();
                    await _roleDb.CreateAsync(role, CancellationToken.None);
                    summary.RolesImported++;
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Role '{role.Name}': {ex.Message}");
                }
            }
        }

        if (_userDb != null)
        {
            foreach (var user in export.Users ?? [])
            {
                try
                {
                    var existing = await _userDb.FindByNameAsync(user.NormalizedUserName ?? user.UserName?.ToUpperInvariant(), CancellationToken.None);
                    if (existing != null) { summary.UsersSkipped++; continue; }
                    user.Id ??= Guid.NewGuid().ToString();
                    await _userDb.CreateAsync(user, CancellationToken.None);
                    summary.UsersImported++;
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"User '{user.UserName}': {ex.Message}");
                }
            }
        }

        if (_clientDb != null)
        {
            foreach (var client in export.Clients ?? [])
            {
                try
                {
                    var existing = await ((IClientDbContext)_clientDb).FindClientByIdAsync(client.ClientId);
                    if (existing != null) { summary.ClientsSkipped++; continue; }
                    await _clientDb.AddClientAsync(client);
                    summary.ClientsImported++;
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Client '{client.ClientId}': {ex.Message}");
                }
            }
        }

        if (_resourceDb != null)
        {
            foreach (var api in export.ApiResources ?? [])
            {
                try
                {
                    var existing = await ((IResourceDbContext)_resourceDb).FindApiResourceAsync(api.Name);
                    if (existing != null) { summary.ResourcesSkipped++; continue; }
                    await _resourceDb.AddApiResourceAsync(api);
                    summary.ResourcesImported++;
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"API resource '{api.Name}': {ex.Message}");
                }
            }

            foreach (var identity in export.IdentityResources ?? [])
            {
                try
                {
                    var existing = await ((IResourceDbContext)_resourceDb).FindIdentityResource(identity.Name);
                    if (existing != null) { summary.ResourcesSkipped++; continue; }
                    await _resourceDb.AddIdentityResourceAsync(identity);
                    summary.ResourcesImported++;
                }
                catch (Exception ex)
                {
                    summary.Errors.Add($"Identity resource '{identity.Name}': {ex.Message}");
                }
            }
        }

        LastImport = summary;

        StatusMessage = summary.Errors.Count == 0
            ? $"Import completed: {summary.UsersImported} users, {summary.RolesImported} roles, " +
              $"{summary.ClientsImported} clients, {summary.ResourcesImported} resources imported."
            : $"Import completed with {summary.Errors.Count} error(s). See details below.";

        TempData["ImportSummaryJson"] = JsonConvert.SerializeObject(summary);

        return RedirectToPage();
    }

    // ------------------------------------------------------------------

    private async Task<List<ApplicationUser>> LoadAllUsersAsync()
    {
        var result = new List<ApplicationUser>();
        const int batch = 200;
        int skip = 0;
        IEnumerable<ApplicationUser> page;
        do
        {
            page = await _userDb!.GetUsersAsync(batch, skip, CancellationToken.None);
            result.AddRange(page);
            skip += batch;
        }
        while (page.Count() == batch);
        return result;
    }

    private async Task<List<ApplicationRole>> LoadAllRolesAsync()
    {
        var result = new List<ApplicationRole>();
        const int batch = 200;
        int skip = 0;
        IEnumerable<ApplicationRole> page;
        do
        {
            page = await _roleDb!.GetRolesAsync(batch, skip, CancellationToken.None);
            result.AddRange(page);
            skip += batch;
        }
        while (page.Count() == batch);
        return result;
    }
}

public class ImportSummary
{
    public int UsersImported { get; set; }
    public int UsersSkipped { get; set; }
    public int RolesImported { get; set; }
    public int RolesSkipped { get; set; }
    public int ClientsImported { get; set; }
    public int ClientsSkipped { get; set; }
    public int ResourcesImported { get; set; }
    public int ResourcesSkipped { get; set; }
    public List<string> Errors { get; set; } = [];
}
