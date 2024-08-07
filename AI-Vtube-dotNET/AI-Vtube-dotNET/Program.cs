﻿using AI_Vtube_dotNET.Core;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using NLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AI_Vtube_dotNET.Livestream;
using AI_Vtube_dotNET.Livestream.Impl;

namespace AI_Vtube_dotNET;
internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

            using var servicesProvider = new ServiceCollection()
                .AddSingleton<Runtime>()
                .AddScoped<LiveClientManager>()
                .AddScoped<ILivestreamPlatform, TwitchManager>()
                .AddScoped<IConfiguration>(_ => config) // WHAT???
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    loggingBuilder.AddNLog();
                }).BuildServiceProvider();

            //Startup the runtime
            var runner = servicesProvider.GetRequiredService<Runtime>();
            await runner.RunAsync();

            //TODO: Remove when we have a runtime loop
            Console.WriteLine("Press ANY key to exit");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            // NLog: catch any exception and log it.
            logger.Fatal(ex, "Runtime exited with an exception: {ex}", ex.Message);
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }
}