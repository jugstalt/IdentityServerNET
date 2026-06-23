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
using System.Security.Claims;
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

    [BindProperty]
    public IFormFile? ImportCsvFile { get; set; }

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

    // ------------------------------------------------------------------
    // CSV import

    public async Task<IActionResult> OnPostImportCsvAsync()
    {
        if (!_configuration.AllowDataTransfer())
            return NotFound();

        if (ImportCsvFile is null || ImportCsvFile.Length == 0)
        {
            StatusMessage = "Error: No CSV file selected.";
            return Page();
        }

        if (_userDb is null)
        {
            StatusMessage = "Error: No user database available.";
            return Page();
        }

        string csvContent;
        using (var stream = ImportCsvFile.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            csvContent = await reader.ReadToEndAsync();

        List<ApplicationUser> users;
        List<string> parseErrors;
        try
        {
            (users, parseErrors) = ParseUsersCsv(csvContent);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: Could not parse CSV — {ex.Message}";
            return Page();
        }

        var summary = new ImportSummary();
        summary.Errors.AddRange(parseErrors);

        foreach (var user in users)
        {
            try
            {
                var existing = await _userDb.FindByEmailAsync(user.NormalizedEmail, CancellationToken.None);
                if (existing != null) { summary.UsersSkipped++; continue; }
                await _userDb.CreateAsync(user, CancellationToken.None);
                summary.UsersImported++;
            }
            catch (Exception ex)
            {
                summary.Errors.Add($"User '{user.UserName}': {ex.Message}");
            }
        }

        StatusMessage = summary.Errors.Count == 0
            ? $"CSV import completed: {summary.UsersImported} users imported, {summary.UsersSkipped} skipped."
            : $"CSV import completed with {summary.Errors.Count} error(s): {summary.UsersImported} imported, {summary.UsersSkipped} skipped.";

        TempData["ImportSummaryJson"] = JsonConvert.SerializeObject(summary);
        return RedirectToPage();
    }

    private static (List<ApplicationUser> users, List<string> errors) ParseUsersCsv(string csv)
    {
        var users = new List<ApplicationUser>();
        var errors = new List<string>();

        var lines = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return (users, errors);

        var headers = SplitCsvLine(lines[0])
            .Select(h => h.Trim().ToLowerInvariant())
            .ToArray();

        int ColIdx(params string[] names)
        {
            foreach (var name in names)
            {
                var idx = Array.IndexOf(headers, name);
                if (idx >= 0) return idx;
            }
            return -1;
        }

        int emailIdx     = ColIdx("email");
        int hashIdx      = ColIdx("passwordhash", "password_hash");
        int firstIdx     = ColIdx("firstname", "first_name", "givenname", "given_name");
        int lastIdx      = ColIdx("lastname", "last_name", "familyname", "family_name");
        int rolesIdx     = ColIdx("roles", "role");

        if (emailIdx < 0)
            throw new Exception("Required column 'Email' not found in CSV header.");
        if (hashIdx < 0)
            throw new Exception("Required column 'PasswordHash' not found in CSV header.");

        for (int i = 1; i < lines.Length; i++)
        {
            var cells = SplitCsvLine(lines[i]);
            if (cells.Length <= emailIdx) continue;

            var email = cells[emailIdx].Trim();
            if (string.IsNullOrWhiteSpace(email)) continue;

            var hash = hashIdx < cells.Length ? cells[hashIdx].Trim() : "";
            if (string.IsNullOrWhiteSpace(hash))
            {
                errors.Add($"Line {i + 1}: '{email}' skipped — no PasswordHash.");
                continue;
            }

            string firstName, lastName;
            if (firstIdx >= 0 && firstIdx < cells.Length && !string.IsNullOrWhiteSpace(cells[firstIdx]))
            {
                firstName = cells[firstIdx].Trim();
                lastName  = lastIdx >= 0 && lastIdx < cells.Length ? cells[lastIdx].Trim() : "";
            }
            else
            {
                (firstName, lastName) = DeriveNameFromEmail(email);
            }

            var roleList = rolesIdx >= 0 && rolesIdx < cells.Length
                ? cells[rolesIdx].Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(r => r.Trim())
                                 .Where(r => r.Length > 0)
                                 .ToList()
                : new List<string>();

            var user = new ApplicationUser
            {
                Id                 = Guid.NewGuid().ToString(),
                UserName           = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email              = email,
                NormalizedEmail    = email.ToUpperInvariant(),
                EmailConfirmed     = true,
                PasswordHash       = hash,
                Roles              = roleList.Count > 0 ? roleList : null,
            };

            user.Claims = new List<Claim>
            {
                new Claim("given_name",  Capitalize(firstName)),
                new Claim("family_name", Capitalize(lastName)),
            };

            users.Add(user);
        }

        return (users, errors);
    }

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ';' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return [.. fields];
    }

    private static (string firstName, string lastName) DeriveNameFromEmail(string email)
    {
        var at = email.IndexOf('@');
        var local  = at > 0 ? email[..at]      : email;
        var domain = at > 0 ? email[(at + 1)..] : "";

        var sepIdx = local.IndexOfAny(['.', '-']);
        if (sepIdx > 0)
            return (local[..sepIdx], local[(sepIdx + 1)..]);

        return (local, domain);
    }

    private static string Capitalize(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
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
