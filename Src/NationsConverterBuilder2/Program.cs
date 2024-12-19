using NationsConverterBuilder2;
using NationsConverterBuilder2.Services;

GBX.NET.Gbx.LZO = new GBX.NET.LZO.Lzo();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<SetupService>();
builder.Services.AddScoped<InitStageService>();
builder.Services.AddScoped<ItemMakerService>();
builder.Services.AddScoped<UvModifierService>();

builder.Services.AddOptions<InitOptions>().Bind(builder.Configuration.GetSection("Init"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/init", async (InitStageService initStageService, SetupService setupService, CancellationToken cancellationToken) =>
{
    await setupService.SetupCollectionsAsync(cancellationToken);
    await initStageService.BuildAsync(cancellationToken);
})
.WithName("Init")
.WithOpenApi();

app.Run();