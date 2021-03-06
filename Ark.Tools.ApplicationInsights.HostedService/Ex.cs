﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System;
using System.Reflection;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Ark.Tools.AspNetCore.ApplicationInsights;

namespace Ark.Tools.ApplicationInsights.HostedService
{
    public static partial class Ex
    {
        public static IHostBuilder AddApplicationInsightsForHostedService(this IHostBuilder builder)
        {
            return builder.ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<ITelemetryInitializer, GlobalInfoTelemetryInitializer>();

                services.AddApplicationInsightsTelemetryProcessor<ArkSkipUselessSpamTelemetryProcessor>();

                services.AddApplicationInsightsTelemetryWorkerService(o =>
                {
                    o.AddAutoCollectedMetricExtractor = true;
                    o.ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
                    o.InstrumentationKey = ctx.Configuration["ApplicationInsights:InstrumentationKey"] ?? Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                    o.EnableAdaptiveSampling = true;
                    o.EnableHeartbeat = true;
                    o.AddAutoCollectedMetricExtractor = true;
                    o.DeveloperMode = Debugger.IsAttached;
                });
                services.AddSingleton<ITelemetryProcessorFactory>(
                    new SkipSqlDatabaseDependencyFilterFactory(ctx.Configuration.GetConnectionString(NLog.NLogDefaultConfigKeys.SqlConnStringName)));
#if NET5_0
                services.Configure<SnapshotCollectorConfiguration>(o =>
                {
                });
                services.Configure<SnapshotCollectorConfiguration>(ctx.Configuration.GetSection(nameof(SnapshotCollectorConfiguration)));
                services.AddSnapshotCollector();
#endif
            });
        }
    }
}
