using AI_Vtube_dotNET.Core;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace AI_Vtube_dotNET;
internal class Program
{
    private static void Main(string[] args)
    {
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
            //TODO: For later when we want config settings
            //var config = new ConfigurationBuilder()
            //   .SetBasePath(System.IO.Directory.GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
            //   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //.Build();

            using var servicesProvider = new ServiceCollection()
                .AddSingleton<Runtime>() // Runner is the custom class
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    loggingBuilder.AddNLog();
                }).BuildServiceProvider();

            //Startup the runtime
            var runner = servicesProvider.GetRequiredService<Runtime>();
            runner.RunAsync();

            //TODO: Remove when we have a runtime loop
            Console.WriteLine("Press ANY key to exit");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            // NLog: catch any exception and log it.
            logger.Error(ex, "Runtime exited with an exception.");
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }


        //ServiceCollection services = CreateServiceProvider();

        //ServiceProvider provider = services.BuildServiceProvider();

        //Runtime runner = ActivatorUtilities.CreateInstance<Runtime>(provider);

        //runner.RunAsync();
    }
}