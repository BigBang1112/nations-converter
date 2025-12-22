using NationsConverterWeb.Configuration;

GBX.NET.Gbx.LZO = new GBX.NET.LZO.MiniLZO();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDomainServices();
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddCacheServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MigrateDatabase();
}

app.UseMiddleware();

app.Run();
