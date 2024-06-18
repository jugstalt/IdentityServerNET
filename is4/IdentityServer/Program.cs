using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace IdentityServer
{
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
                    var settingsPrefix = Environment.GetEnvironmentVariable("IDENTITY_SERVER_SETTINGS_PREFIX");

                    if (string.IsNullOrEmpty(settingsPrefix))
                    {
                        settingsPrefix = "default";
                    }

                    var configFile = $"_config/{settingsPrefix}.identityserver.nova.json";
                    Console.WriteLine($"Use config file: {configFile} ({(System.IO.File.Exists(configFile) ? "exits" : "not- exits")})");
                    config.AddJsonFile(configFile,
                        optional: true,
                        reloadOnChange: false);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseSetting(WebHostDefaults.ApplicationKey, typeof(Program).Assembly.GetName().Name)
                        .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, string.Join(";", StartupAssemblies));

                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog();
    }
}