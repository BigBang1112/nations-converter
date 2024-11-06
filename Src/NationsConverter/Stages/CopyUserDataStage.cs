using System.IO.Compression;

namespace NationsConverter.Stages;

internal sealed class CopyUserDataStage
{
    private readonly NationsConverterConfig config;
    private readonly string runningDir;
    private readonly string? userDataPackFilePath;

    public CopyUserDataStage(NationsConverterConfig config, string runningDir, string? userDataPackFilePath)
    {
        this.config = config;
        this.runningDir = runningDir;
        this.userDataPackFilePath = userDataPackFilePath;
    }

    public void Copy()
    {
        if (string.IsNullOrWhiteSpace(config.UserDataFolder))
        {
            throw new InvalidOperationException("UserDataFolder is not set");
        }

        CopyUserDataDirectory(Path.Combine(runningDir, "UserData"), config.UserDataFolder);

        if (userDataPackFilePath is not null)
        {
            ZipFile.ExtractToDirectory(userDataPackFilePath, Path.Combine(config.UserDataFolder), overwriteFiles: true);
        }
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
