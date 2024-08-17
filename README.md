# Nations Converter 2

This is where the new TMUF is being made.

## Build

Build the solution with Visual Studio 2022 or by using `dotnet build`.

This project started using the GBX.NET nightly builds for more comfortable development, but this requires a bit more setup now.

To retrieve the nightly build of GBX.NET appropriately, you need to add GitHub package source of the `BigBang1112` account and authentication to it, as unfortunately, GitHub Packages require access token even for just downloading the package.

In GitHub settings -> Developer Settings at the bottom -> Personal access tokens -> Tokens (classic) -> Generate new token (classic), you can generate the access token. Setting Expiration to **No expiration** should be the best option, as you only have to enable **`read:packages`**, which if this token would leak, would not cause much harm anyway. Then go to `%appdata%/NuGet` and modify the `NuGet.Config` file to include that secret:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="BigBang1112" value="https://nuget.pkg.github.com/bigbang1112/index.json" /> <!-- add this -->
  </packageSources>
  <packageSourceCredentials>
    <BigBang1112> <!-- add this -->
      <add key="Username" value="your_github_username" /> <!-- gh username -->
      <add key="ClearTextPassword" value="your_generated_token" /> <!-- that generated token -->
    </BigBang1112>
  </packageSourceCredentials>
</configuration>
```

This should be enough for a successful build.