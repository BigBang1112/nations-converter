# Nations Converter 2

This is where the new TMUF is being made.

## Build

Build the solution with Visual Studio 2022 or by using `dotnet build`.

This project started using the GBX.NET nightly builds for more comfortable development, but this requires a bit more setup now.

To retrieve the nightly build of GBX.NET appropriately, you need to add the https://nuget.gbx.tools/ package source.

Go to `%appdata%/NuGet` and modify the `NuGet.Config` file to include the package source:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.gbx.tools" value="https://nuget.gbx.tools/v3/index.json" /> <!-- add this -->
  </packageSources>
</configuration>
```

This should be enough for a successful build.