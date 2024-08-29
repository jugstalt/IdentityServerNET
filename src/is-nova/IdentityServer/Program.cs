using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Linq;

namespace IdentityServer;

public class Program
{
    public static int Main(string[] args)
    {
        //
        // IdentiyServer QuickStart
        //
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
            .Enrich.FromLogContext()
            // uncomment to write to Azure diagnostics stream
            //.WriteTo.File(
            //    @"D:\home\LogFiles\Application\identityserver.txt",
            //    fileSizeLimitBytes: 1_000_000,
            //    rollOnFileSizeLimit: true,
            //    shared: true,
            //    flushToDiskInterval: TimeSpan.FromSeconds(1))
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate)
            .CreateLogger();

        try
        {
            Log.Information("Starting host...");
            CreateHostBuilder(args).Build().Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static string[] StartupAssemblies = new[]
    {
        "IdentityServer.Nova"
    };

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                #region Custom App config
                
                var customAppConfig = args.FirstOrDefault(arg => arg.StartsWith("--customAppSettings="))?.Split('=')[1];
                if (!string.IsNullOrEmpty(customAppConfig))
                {
                    string customAppConfigFile = $"appsettings.{customAppConfig}.json";
                    Console.WriteLine($"Using custom app config file: {customAppConfigFile} ({(System.IO.File.Exists(customAppConfigFile) ? "exits" : "not exist")})");
                    config.AddJsonFile(customAppConfigFile, optional: true, reloadOnChange: false);
                }

                #endregion

                #region _config/...json File

                var settingsPrefix = Environment.GetEnvironmentVariable("IDENTITY_SERVER_SETTINGS_PREFIX");

                if (string.IsNullOrEmpty(settingsPrefix))
                {
                    settingsPrefix = "default";
                }

                var configFile = $"_config/{settingsPrefix}.identityserver.nova.json";
                Console.WriteLine($"Using config file: {configFile} ({(System.IO.File.Exists(configFile) ? "exits" : "not exist")})");
                config.AddJsonFile(configFile,
                    optional: true,
                    reloadOnChange: false);

                #endregion
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseSetting(WebHostDefaults.ApplicationKey, typeof(Program).Assembly.GetName().Name);

                webBuilder.UseStartup<Startup>();
            })
            .UseSerilog();
}