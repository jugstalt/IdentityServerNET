#nullable enable

using IdentityServerNET.Abstractions.EmailSender;
using IdentityServerNET.Services.UI;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace IdentityServerNET.Services.EmailSender;

public class MailTemplateService : IMailTemplateService
{
    private readonly string _templatesPath;
    private readonly UICustomizationService? _uiCustomization;

    public MailTemplateService(IConfiguration configuration, UICustomizationService? uiCustomization = null)
    {
        var configured = configuration["IdentityServer:Mail:TemplatesPath"];
        _templatesPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "custom", "mails")
            : Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured);
        _uiCustomization = uiCustomization;
    }

    public string Render(string templateName, Dictionary<string, string> variables)
    {
        var html = LoadTemplate(templateName);
        foreach (var (key, value) in variables)
            html = html.Replace($"{{{{{key}}}}}", value ?? "");

        var settings = _uiCustomization?.LoadAsync().GetAwaiter().GetResult();
        var primary = settings?.PrimaryColor ?? "#0094ff";
        var darker = AdjustBrightness(primary, 0.78f);
        var logoTag = _uiCustomization?.GetLogoBase64TagAsync().GetAwaiter().GetResult() ?? "";

        html = html
            .Replace("{{primaryColor}}", primary)
            .Replace("{{primaryDarkerColor}}", darker)
            .Replace("{{logoBase64Tag}}", logoTag);

        return html;
    }

    private static string AdjustBrightness(string hex, float factor)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "#" + hex;
        int r = Convert.ToInt32(hex[..2], 16);
        int g = Convert.ToInt32(hex[2..4], 16);
        int b = Convert.ToInt32(hex[4..6], 16);
        r = Math.Clamp((int)(r * factor), 0, 255);
        g = Math.Clamp((int)(g * factor), 0, 255);
        b = Math.Clamp((int)(b * factor), 0, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private string LoadTemplate(string templateName)
    {
        var filePath = Path.Combine(_templatesPath, $"{templateName}.html");
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        return DefaultTemplates.TryGetValue(templateName, out var built)
            ? built
            : DefaultTemplates["generic"];
    }

    public static readonly Dictionary<string, string> DefaultTemplates = new()
    {
        ["confirm-email"] = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1.0">
            <title>{{subject}}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#eef6ff;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#eef6ff;padding:48px 16px;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">
                    <tr>
                      <td style="background:linear-gradient(135deg,{{primaryColor}} 0%,{{primaryDarkerColor}} 100%);border-radius:12px 12px 0 0;padding:36px 48px;text-align:center;">
                        {{logoBase64Tag}}
                        <p style="color:rgba(255,255,255,.75);font-size:11px;letter-spacing:2px;text-transform:uppercase;margin:0 0 8px 0;">Identity &amp; Access Management</p>
                        <h1 style="color:#fff;font-size:26px;font-weight:700;margin:0;letter-spacing:-0.5px;">{{applicationName}}</h1>
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#fff;padding:48px;border-radius:0 0 12px 12px;box-shadow:0 4px 24px rgba(0,148,255,.12);">
                        <div style="width:60px;height:60px;background:#e6f4ff;border-radius:50%;margin:0 auto 24px auto;text-align:center;line-height:60px;font-size:30px;">✉️</div>
                        <h2 style="color:#0d1117;font-size:22px;font-weight:700;text-align:center;margin:0 0 12px 0;">Confirm your email address</h2>
                        <p style="color:#5a6272;font-size:15px;line-height:1.7;text-align:center;margin:0 0 32px 0;">
                          Thanks for signing up! Please verify your email address to activate your account and get started.
                        </p>
                        <table cellpadding="0" cellspacing="0" style="margin:0 auto 32px auto;">
                          <tr>
                            <td style="background:linear-gradient(135deg,{{primaryColor}} 0%,{{primaryDarkerColor}} 100%);border-radius:8px;box-shadow:0 4px 12px rgba(0,148,255,.35);">
                              <a href="{{link}}" style="display:inline-block;padding:16px 44px;color:#fff;font-size:15px;font-weight:600;text-decoration:none;border-radius:8px;letter-spacing:0.2px;">Confirm Email Address</a>
                            </td>
                          </tr>
                        </table>
                        <p style="color:#9ca3af;font-size:12px;text-align:center;margin:0 0 6px 0;">Button not working? Copy this link into your browser:</p>
                        <p style="color:{{primaryColor}};font-size:12px;word-break:break-all;text-align:center;margin:0 0 32px 0;">{{link}}</p>
                        <hr style="border:none;border-top:1px solid #eef6ff;margin:0 0 20px 0;">
                        <p style="color:#c0c8d0;font-size:12px;text-align:center;margin:0;">If you didn't create an account, you can safely ignore this email.</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:24px;text-align:center;">
                        <p style="color:#9ca3af;font-size:12px;margin:0;">&copy; {{year}} {{applicationName}}. All rights reserved.</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """,

        ["reset-password"] = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1.0">
            <title>{{subject}}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#eef6ff;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#eef6ff;padding:48px 16px;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">
                    <tr>
                      <td style="background:linear-gradient(135deg,{{primaryColor}} 0%,{{primaryDarkerColor}} 100%);border-radius:12px 12px 0 0;padding:36px 48px;text-align:center;">
                        {{logoBase64Tag}}
                        <p style="color:rgba(255,255,255,.75);font-size:11px;letter-spacing:2px;text-transform:uppercase;margin:0 0 8px 0;">Identity &amp; Access Management</p>
                        <h1 style="color:#fff;font-size:26px;font-weight:700;margin:0;letter-spacing:-0.5px;">{{applicationName}}</h1>
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#fff;padding:48px;border-radius:0 0 12px 12px;box-shadow:0 4px 24px rgba(0,148,255,.12);">
                        <div style="width:60px;height:60px;background:#e6f4ff;border-radius:50%;margin:0 auto 24px auto;text-align:center;line-height:60px;font-size:30px;">🔐</div>
                        <h2 style="color:#0d1117;font-size:22px;font-weight:700;text-align:center;margin:0 0 12px 0;">Reset your password</h2>
                        <p style="color:#5a6272;font-size:15px;line-height:1.7;text-align:center;margin:0 0 6px 0;">
                          We received a request to reset the password for the account associated with <strong style="color:#0d1117;">{{email}}</strong>.
                        </p>
                        <p style="color:#9ca3af;font-size:13px;text-align:center;margin:0 0 32px 0;">This link expires in <strong>1 hour</strong>. If you didn't request this, no action is needed.</p>
                        <table cellpadding="0" cellspacing="0" style="margin:0 auto 32px auto;">
                          <tr>
                            <td style="background:linear-gradient(135deg,{{primaryColor}} 0%,{{primaryDarkerColor}} 100%);border-radius:8px;box-shadow:0 4px 12px rgba(0,148,255,.35);">
                              <a href="{{link}}" style="display:inline-block;padding:16px 44px;color:#fff;font-size:15px;font-weight:600;text-decoration:none;border-radius:8px;letter-spacing:0.2px;">Reset My Password</a>
                            </td>
                          </tr>
                        </table>
                        <p style="color:#9ca3af;font-size:12px;text-align:center;margin:0 0 6px 0;">Button not working? Copy this link into your browser:</p>
                        <p style="color:{{primaryColor}};font-size:12px;word-break:break-all;text-align:center;margin:0 0 32px 0;">{{link}}</p>
                        <hr style="border:none;border-top:1px solid #eef6ff;margin:0 0 20px 0;">
                        <p style="color:#c0c8d0;font-size:12px;text-align:center;margin:0;">For your security: if you did not request a password reset, please secure your account immediately.</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:24px;text-align:center;">
                        <p style="color:#9ca3af;font-size:12px;margin:0;">&copy; {{year}} {{applicationName}}. All rights reserved.</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """,

        ["generic"] = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1.0">
            <title>{{subject}}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#eef6ff;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#eef6ff;padding:48px 16px;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">
                    <tr>
                      <td style="background:linear-gradient(135deg,{{primaryColor}} 0%,{{primaryDarkerColor}} 100%);border-radius:12px 12px 0 0;padding:36px 48px;text-align:center;">
                        {{logoBase64Tag}}
                        <p style="color:rgba(255,255,255,.75);font-size:11px;letter-spacing:2px;text-transform:uppercase;margin:0 0 8px 0;">Identity &amp; Access Management</p>
                        <h1 style="color:#fff;font-size:26px;font-weight:700;margin:0;">{{applicationName}}</h1>
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#fff;padding:48px;border-radius:0 0 12px 12px;box-shadow:0 4px 24px rgba(0,148,255,.12);">
                        <h2 style="color:#0d1117;font-size:20px;font-weight:700;margin:0 0 16px 0;">{{subject}}</h2>
                        <p style="color:#5a6272;font-size:15px;line-height:1.7;margin:0 0 32px 0;">{{content}}</p>
                        <table cellpadding="0" cellspacing="0" style="margin:0 auto 32px auto;">
                          <tr>
                            <td style="background:linear-gradient(135deg,{{primaryColor}} 0%,{{primaryDarkerColor}} 100%);border-radius:8px;box-shadow:0 4px 12px rgba(0,148,255,.35);">
                              <a href="{{link}}" style="display:inline-block;padding:16px 44px;color:#fff;font-size:15px;font-weight:600;text-decoration:none;border-radius:8px;">Open Link</a>
                            </td>
                          </tr>
                        </table>
                        <p style="color:{{primaryColor}};font-size:12px;word-break:break-all;text-align:center;margin:0;">{{link}}</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:24px;text-align:center;">
                        <p style="color:#9ca3af;font-size:12px;margin:0;">&copy; {{year}} {{applicationName}}. All rights reserved.</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """
    };
}
