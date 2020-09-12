using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog;

using TwitchPlanner.Services;
using System;

namespace PerformerPlanner
{
    class Program
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
                Host.CreateDefaultBuilder(args)
                   .ConfigureHostConfiguration(config =>
                   {
                       config.AddEnvironmentVariables();

                       if (args != null)
                       {
                          // enviroment from command line
                          // e.g.: dotnet run --environment "Staging"
                          config.AddCommandLine(args);
                       }
                   })
                   .ConfigureAppConfiguration((context, builder) =>
                   {
                       IHostEnvironment env = context.HostingEnvironment;

                       IConfigurationBuilder configurationBuilder = builder.SetBasePath(AppContext.BaseDirectory);
                       configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                           .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)

                       // Override config by env, using like Logging:Level or Logging__Level
                       .AddEnvironmentVariables();
                   })
                   .ConfigureServices(services =>
                   {
                       services.AddSingleton<IHostedService, BrowserChromeService>();
                   })
                   .ConfigureLogging(loggingBuilder => 
                   {
                       loggingBuilder.AddConsole(options =>
                       {
                           options.TimestampFormat = "[HH:mm:ss hh:mm:ss] ";
                       });
                   })
                   .Build()
                   .Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal: " + ex.Message);
                _log.Fatal(ex, ex.Message);
            }
        }
    }
}
