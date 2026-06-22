#nullable enable

using IdentityServerNET.Abstractions.EmailSender;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public class EmailSenderProxy : IEmailSender
{
    private static readonly Regex _hrefRegex = new(@"href='([^']+)'", RegexOptions.Compiled);

    private readonly ICustomEmailSender _customEmailSender;
    private readonly IMailTemplateService? _templateService;
    private readonly string _applicationName;

    public EmailSenderProxy(
        ICustomEmailSender customEmailSender,
        IConfiguration configuration,
        IMailTemplateService? templateService = null)
    {
        _customEmailSender = customEmailSender;
        _templateService = templateService;
        _applicationName = configuration["IdentityServer:ApplicationTitle"] ?? "IdentityServer NET";
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (_templateService is not null)
            htmlMessage = ApplyTemplate(email, subject, htmlMessage);

        return _customEmailSender.SendEmailAsync(email, subject, htmlMessage);
    }

    private string ApplyTemplate(string email, string subject, string htmlMessage)
    {
        var templateName = subject switch
        {
            "Confirm your email" => "confirm-email",
            "Reset Password"     => "reset-password",
            _                    => "generic"
        };

        var link = ExtractLink(htmlMessage) ?? "#";

        var variables = new Dictionary<string, string>
        {
            ["applicationName"] = _applicationName,
            ["subject"]         = subject,
            ["email"]           = email,
            ["link"]            = link,
            ["content"]         = htmlMessage,
            ["year"]            = DateTime.UtcNow.Year.ToString()
        };

        return _templateService!.Render(templateName, variables);
    }

    private static string? ExtractLink(string htmlMessage)
    {
        var match = _hrefRegex.Match(htmlMessage);
        return match.Success
            ? WebUtility.HtmlDecode(match.Groups[1].Value)
            : null;
    }
}
