using GBX.NET.Engines.Game;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace NationsConverter.Stages;

internal sealed class CopyUserDataStage
{
    private readonly NationsConverterConfig config;
    private readonly string runningDir;
    private readonly CGameCtnChallenge mapOut;
    private readonly ILogger logger;

    public CopyUserDataStage(NationsConverterConfig config, string runningDir, CGameCtnChallenge mapOut, ILogger logger)
    {
        this.config = config;
        this.runningDir = runningDir;
        this.mapOut = mapOut;
        this.logger = logger;
    }

    public void Copy()
    {
        if (string.IsNullOrWhiteSpace(config.UserDataFolder))
        {
            throw new InvalidOperationException("UserDataFolder is not set");
        }

        if (mapOut.EmbeddedZipData is null)
        {
            logger.LogInformation("No embedded data in the map to copy to UserDataFolder.");
            return;
        }

        using var zipStream = new MemoryStream(mapOut.EmbeddedZipData);
        ZipFile.ExtractToDirectory(zipStream, config.UserDataFolder, overwriteFiles: true);

        // maybe in the future, allow copying the whole UserData directory
    }

    private static void CopyUserDataDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        var dirs = dir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        var isUserDataRoot = sourceDir.EndsWith("UserData", StringComparison.OrdinalIgnoreCase);

        foreach (var file in dir.GetFiles())
        {
            if (isUserDataRoot && file.Name.EndsWith(".zip"))
            {
                continue;
            }

            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: true);
        }

        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyUserDataDirectory(subDir.FullName, newDestinationDir);
        }
    }
}
