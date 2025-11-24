using IdentityServerNET.Extensions;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Release'")] 
    readonly string Configuration = "Release"; //IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Platform to build - win-x64/linux-x64")]
    readonly string Platform = SystemInfo.IsLinux ? "linux-x64" : "win-x64";

    [Parameter("Versions‑/Build‑Label")]
    readonly string Version = String.Join(".", typeof(IdentityServerNET.Models.ApplicationUser).Assembly.GetName().Version.ToString().Split(".")[0..3]);

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
        });

    Target Test => _ => _
        .Before(Compile)
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .Before(DeployCleanIt)
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Information($"Compile IdentityServer.Net {Version} for platform {Platform}");

            (RootDirectory / "publish" / Platform / "identityserver" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "is-net" / "IdentityServer" / "IdentityServer.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                //.SetOutputDirectory(RootDirectory / "publish" / Platform / "cms" / "artifacts")
                .SetPublishProfile(Platform)
                .SetRuntime(Platform)
                //.EnableNoRestore()
            );
        });

    Target DeployCleanIt => _ => _
        .Executes(() =>
        {
            Log.Information($"Cleaning up IdentityServer.Net {Version} for platform {Platform}");

            var globs = new[]
            {
                "identityserver/artifacts/_config/**/*.*",
            };

            foreach (var pattern in globs)
            {
                foreach (var globFile in (RootDirectory / "publish" / Platform).GlobFiles(pattern))
                {
                    Log.Information($"Deleting {globFile}");
                    globFile.DeleteFile();
                }
            }
        });

    AbsolutePath DeployRoot => (AbsolutePath)(SystemInfo.IsLinux ? "~/deploy/identityserver-net" : @"c:\deploy\identityserver-net");
    AbsolutePath DeployDir => DeployRoot / Version;
    AbsolutePath DownloadDir => DeployRoot / "download";

    Target Deploy => _ => _
        .DependsOn(Test)
        .DependsOn(Compile)
        .DependsOn(DeployCleanIt)
        .Executes(() =>
        {
            Log.Information($"Deploy IdentityServer.Net {Version} for platform {Platform}");

            if (Platform.Equals("linux-x64", StringComparison.Ordinal))
            {
                Log.Information($"Builder docker images: {Version}");

                string platformDir = RootDirectory / "publish" / Platform;

                ProcessTasks.StartProcess("docker",
                    $"build -t identityserver-net-base:{Version} -f Dockerfile .",
                    workingDirectory: Path.Combine(platformDir, "identityserver"),
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();

                // tag base to latest
                ProcessTasks.StartProcess("docker",
                    $"tag identityserver-net-base:{Version} identityserver-net-base:latest",
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();

                ProcessTasks.StartProcess("docker",
                   $"build -t identityserver-net:{Version} -f Dockerfile .",
                   workingDirectory: Path.Combine(platformDir, "identityserver", "default"),
                   logger: (oType, txt) =>
                   {
                       Log.Information($"{txt}");
                   })
                   .AssertZeroExitCode();

                ProcessTasks.StartProcess("docker",
                   $"build -t identityserver-net-dev:{Version} -f Dockerfile .",
                   workingDirectory: Path.Combine(platformDir, "identityserver", "dev"),
                   logger: (oType, txt) =>
                   {
                       Log.Information($"{txt}");
                   })
                   .AssertZeroExitCode();

                // tag default and dev to latest
                ProcessTasks.StartProcess("docker",
                    $"tag identityserver-net:{Version} identityserver-net:latest",
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();

                ProcessTasks.StartProcess("docker",
                    $"tag identityserver-net-dev:{Version} identityserver-net-dev:latest",
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();

                return;
            }

            Log.Information($"Deploy ZIP File: {Version}");

            // 1. Mirror Source (ROBOMIRROR‑Äquivalent)
            Log.Information("Copy identityserver-net");
            (DeployDir / Version).CreateOrCleanDirectory();
            (RootDirectory / "publish" / Platform / "identityserver" / "artifacts").CopyToDirectory(
                DeployDir / Version,
                ExistsPolicy.MergeAndOverwrite);
            (DeployDir / Version / "artifacts").Rename("identityserver-net");

            // 2. Overrides
            //Log.Information("Copy identityserver-net overrides");
            //(RootDirectory / "publish" / Platform / "identityserver" / "override" / "_setup").CopyToDirectory(
            //                         DeployDir / Version / "identityserver-net",
            //                         ExistsPolicy.MergeAndOverwrite);

            // 5. ZIP‑Archiv erstellen
            if (!Directory.Exists(DownloadDir))
            {
                DownloadDir.CreateDirectory();
            }

            var platform = Platform.Replace("-x64", "64");
            var zipFile = DownloadDir / $"identityserver-net-{platform}-{Version}.zip";

            Log.Information($"Zip Directory: {DeployDir} => {zipFile}");
            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }

            DeployDir.ZipTo(
                zipFile,
                compressionLevel: CompressionLevel.SmallestSize,
                fileMode: FileMode.CreateNew);

            // 6. cleanup temp directory (rmdir /s /q)
            DeployDir.DeleteDirectory();
        });
}
