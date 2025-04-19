using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace NationsConverterWeb.Configuration;

public static class TelemetryConfiguration
{
    public static void AddTelemetryServices(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, applyThemeToRedirectedOutput: true)
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = config["OTEL_EXPORTER_OTLP_ENDPOINT"];
                options.Protocol = config["OTEL_EXPORTER_OTLP_PROTOCOL"]?.ToLowerInvariant() switch
                {
                    "grpc" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc,
                    "http/protobuf" or null or "" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf,
                    _ => throw new NotSupportedException($"OTLP protocol {config["OTEL_EXPORTER_OTLP_PROTOCOL"]} is not supported")
                };
                options.Headers = config["OTEL_EXPORTER_OTLP_HEADERS"]?
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
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter();

                options.AddMeter("System.Net.Http");
            })
            .WithTracing(options =>
            {
                if (environment.IsDevelopment())
                {
                    options.SetSampler<AlwaysOnSampler>();
                }

                options
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter();
            });

        services.AddMetrics();
    }
}
