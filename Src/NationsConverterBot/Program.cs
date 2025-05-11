using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using NationsConverterBot;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Serilog.Sinks.SystemConsole.Themes;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddHttpClient();

    services.AddSingleton(TimeProvider.System);

    // Configure Discord bot
    services.AddSingleton(new DiscordSocketConfig()
    {
        LogLevel = LogSeverity.Verbose
    });

    // Add Discord bot client and Interaction Framework
    services.AddSingleton<DiscordSocketClient>();
    services.AddSingleton<InteractionService>(provider => new(provider.GetRequiredService<DiscordSocketClient>(), new()
    {
        LogLevel = LogSeverity.Verbose
    }));

    // Add startup
    services.AddHostedService<Startup>();

    // Add services
    services.AddSingleton<IDiscordBot, DiscordBot>();

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, applyThemeToRedirectedOutput: true)
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            options.Protocol = context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]?.ToLowerInvariant() switch
            {
                "grpc" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc,
                "http/protobuf" or null or "" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf,
                _ => throw new NotSupportedException($"OTLP protocol {context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]} is not supported")
            };
            options.Headers = context.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(x => x[0], x => x[1]) ?? [];
        })
        .CreateLogger();

    services.AddSerilog();

    services.AddOpenTelemetry()
        .WithMetrics(options =>
        {
            options
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddOtlpExporter();

            options.AddMeter("System.Net.Http");
        })
        .WithTracing(options =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                options.SetSampler<AlwaysOnSampler>();
            }

            options
                .AddHttpClientInstrumentation()
                .AddOtlpExporter();
        });
});

// Use Serilog
builder.UseSerilog();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{SourceContext} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

await builder.Build().RunAsync();