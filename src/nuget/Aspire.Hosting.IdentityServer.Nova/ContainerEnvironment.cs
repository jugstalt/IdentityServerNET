using System;

namespace Aspire.Hosting.IdentityServer.Nova;

static public class ContainerEnvironment
{
    public const string DockerDesktopHost = "docker.for.mac.localhost";

    public static string HostName = Environment.MachineName;
    public static string HostAddress = DockerDesktopHost;
}
