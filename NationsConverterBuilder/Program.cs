using Microsoft.Extensions.FileProviders;
using NationsConverterBuilder;
using NationsConverterBuilder.Services;

GBX.NET.Gbx.LZO = new GBX.NET.LZO.MiniLZO();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = int.MaxValue;
});

builder.Services.AddDirectoryBrowser();

builder.Services.AddScoped<SetupService>();
builder.Services.AddScoped<GeneralBuildService>();
builder.Services.AddScoped<ItemMakerService>();
builder.Services.AddScoped<UvModifierService>();

builder.Services.AddOptions<InitOptions>().Bind(builder.Configuration.GetSection("Init"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions()
{
    ServeUnknownFileTypes = true
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "data")),
    RequestPath = "/data",
});

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
