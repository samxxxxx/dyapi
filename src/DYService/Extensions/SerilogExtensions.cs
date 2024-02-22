using Exceptionless;
using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Exceptionless.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService.Extensions
{
    public static class SerilogExtensions
    {
        /// <summary>
        /// 启用Serilog日志 + exceptionless 日志ui
        /// </summary>
        /// <param name="builder"></param>
        public static void UseSeriLog(this IApplicationBuilder builder)
        {
            var configuration = builder.ApplicationServices.GetRequiredService<IConfiguration>();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Exceptionless()
                .ReadFrom.Configuration(configuration)//配置
                .Enrich.FromLogContext()
                .CreateLogger();

            ExceptionlessClient.Default.Startup();
            ReadFromConfiguration(ExceptionlessClient.Default.Configuration, configuration);

            Log.Information("serilog started!");
        }

        private static void ReadFromConfiguration(ExceptionlessConfiguration config, IConfiguration settings)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var section = settings.GetSection("Exceptionless");
            if (section == null)
                return;

            if (Boolean.TryParse(section["Enabled"], out bool enabled) && !enabled)
                config.Enabled = false;

            string apiKey = section["ApiKey"];
            if (!String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE")
                config.ApiKey = apiKey;

            string serverUrl = section["ServerUrl"];
            if (!String.IsNullOrEmpty(serverUrl))
                config.ServerUrl = serverUrl;

            if (TimeSpan.TryParse(section["QueueMaxAge"], out var queueMaxAge))
                config.QueueMaxAge = queueMaxAge;

            if (Int32.TryParse(section["QueueMaxAttempts"], out int queueMaxAttempts))
                config.QueueMaxAttempts = queueMaxAttempts;

            string storagePath = section["StoragePath"];
            if (!String.IsNullOrEmpty(storagePath))
                config.Resolver.Register(typeof(IObjectStorage), () => new FolderObjectStorage(config.Resolver, storagePath));

            string storageSerializer = section["StorageSerializer"];
            if (!String.IsNullOrEmpty(storageSerializer))
            {
                try
                {
                    var serializerType = Type.GetType(storageSerializer);
                    if (!typeof(IStorageSerializer).IsAssignableFrom(serializerType))
                    {
                        config.Resolver.GetLog().Error($"The storage serializer {storageSerializer} does not implemented interface {typeof(IStorageSerializer)}.", typeof(ExceptionlessConfigurationExtensions).ToString());
                    }
                    else
                    {
                        config.Resolver.Register(typeof(IStorageSerializer), serializerType);
                    }
                }
                catch (Exception ex)
                {
                    config.Resolver.GetLog().Error($"The storage serializer {storageSerializer} type could not be resolved: ${ex.Message}", typeof(ExceptionlessConfigurationExtensions).ToString(), ex);
                }
            }

            if (Boolean.TryParse(section["EnableLogging"], out bool enableLogging) && enableLogging)
            {
                string logPath = section["LogPath"];
                if (!String.IsNullOrEmpty(logPath))
                    config.UseFileLogger(logPath);
                else if (!String.IsNullOrEmpty(storagePath))
                    config.UseFileLogger(System.IO.Path.Combine(storagePath, "exceptionless.log"));
            }

            if (Boolean.TryParse(section["IncludePrivateInformation"], out bool includePrivateInformation) && !includePrivateInformation)
                config.IncludePrivateInformation = false;

            if (Boolean.TryParse(section["ProcessQueueOnCompletedRequest"], out bool processQueueOnCompletedRequest) && processQueueOnCompletedRequest)
                config.ProcessQueueOnCompletedRequest = true;

            foreach (var tag in section.GetSection("DefaultTags").GetChildren())
                config.DefaultTags.Add(tag.Value);

            foreach (var data in section.GetSection("DefaultData").GetChildren())
                if (data.Value != null)
                    config.DefaultData[data.Key] = data.Value;

            foreach (var setting in section.GetSection("Settings").GetChildren())
                if (setting.Value != null)
                    config.Settings[setting.Key] = setting.Value;
        }
    }
}
