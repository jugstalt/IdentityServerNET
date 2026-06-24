using Aspire.Hosting.ApplicationModel;
using System;

namespace IdentityServerNET.Aspire.Hosting.Utilitities;

public sealed class MailPitResource(string name) : ContainerResource(name)
{
    internal const string SmtpEndpointName = "smtp";
    internal const string HttpEndpointName = "http";

    public string ContainerName = $"{name}-{Guid.NewGuid():N}";
    public int ContainerHttpPort = 8025;
    public int ContainerSmtpPort = 1025;

    private EndpointReference? _smtpReference;

    public EndpointReference SmtpEndpoint =>
        _smtpReference ??= new(this, SmtpEndpointName);
}
