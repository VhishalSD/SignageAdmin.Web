namespace SignageApp.Services;

public sealed class StoragePaths
{
    public string RootPath { get; }
    public string SlidesPath => Path.Combine(RootPath, "data", "slides.json");
    public string UploadsPath => Path.Combine(RootPath, "uploads");

    private StoragePaths(string rootPath)
    {
        RootPath = rootPath;
    }

    public static StoragePaths Create(string webRootPath, bool useProjectData, string localApplicationData,
        string? dataRoot = null)
    {
        if (!Path.IsPathFullyQualified(webRootPath) || (useProjectData && !Directory.Exists(webRootPath)))
        {
            throw new InvalidOperationException("De webroot voor opslag of migratie moet een bestaande map met een absoluut pad zijn.");
        }

        string rootPath = webRootPath;
        if (!useProjectData)
        {
            if (dataRoot == null && !Path.IsPathFullyQualified(localApplicationData))
            {
                throw new InvalidOperationException("De lokale gebruikersgegevensmap is niet beschikbaar.");
            }

            rootPath = dataRoot ?? Path.Combine(localApplicationData, "Signage Beheer");
            if (!Path.IsPathFullyQualified(rootPath))
            {
                throw new InvalidOperationException("De gebruikersgegevensmap moet een absoluut pad zijn.");
            }
            MigrateIfMissing(webRootPath, rootPath);
        }

        Directory.CreateDirectory(Path.Combine(rootPath, "data"));
        Directory.CreateDirectory(Path.Combine(rootPath, "uploads"));
        return new StoragePaths(rootPath);
    }

    private static void MigrateIfMissing(string legacyWebRoot, string destination)
    {
        // Existing user storage is authoritative, even when the installer contains newer files.
        if (Directory.Exists(destination))
        {
            return;
        }

        if (!Directory.Exists(legacyWebRoot))
        {
            throw new InvalidOperationException($"De migratiebron '{legacyWebRoot}' bestaat niet. Gebruikersopslag is niet aangemaakt.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string staging = destination + ".migrating-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        try
        {
            Directory.CreateDirectory(Path.Combine(staging, "data"));
            try
            {
                File.Copy(Path.Combine(legacyWebRoot, "data", "slides.json"),
                    Path.Combine(staging, "data", "slides.json"), overwrite: false);
            }
            catch (FileNotFoundException)
            {
                // Only genuinely absent storage may be initialized by SlideService.
            }
            catch (DirectoryNotFoundException) when (!Directory.Exists(Path.Combine(legacyWebRoot, "data")))
            {
            }

            CopyUploads(Path.Combine(legacyWebRoot, "uploads"), Path.Combine(staging, "uploads"));

            // Publish the complete copy in one rename; never merge into or overwrite existing storage.
            try
            {
                Directory.Move(staging, destination);
            }
            catch (IOException) when (Directory.Exists(destination))
            {
                // Another process initialized storage first. Its data wins.
            }
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }

    private static void CopyUploads(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        string[] files;
        string[] directories;
        try
        {
            files = Directory.GetFiles(source);
            directories = Directory.GetDirectories(source);
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        foreach (string file in files)
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: false);
        }

        foreach (string directory in directories)
        {
            CopyUploads(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
