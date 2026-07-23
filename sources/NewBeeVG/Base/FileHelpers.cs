using System.IO.Compression;

namespace NewBeeVG;

internal class FileHelpers
{
    public static Stream? LoadStream(string basePath, string fileName)
    {
        FileInfo baseFileInfo = new FileInfo(basePath);
        if (baseFileInfo.Exists == true)
        {
            return LoadFromZipFile(basePath, fileName);
        }
        else
        {
            string fullPath = Path.Combine(basePath, fileName);
            if (File.Exists(fullPath))
            {
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            }
            else
            {
                Console.WriteLine($"File does not exist: {fullPath}");
                return null;
            }
        }
    }

    public static Stream? LoadFromZipFile(string basePath, string fileName)
    {
        try
        {
            using var zipStream = File.OpenRead(basePath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var entry = archive.GetEntry(fileName);
            if (entry != null)
            {
                var entryStream = entry.Open();
                var memoryStream = new MemoryStream();
                entryStream.CopyTo(memoryStream);
                memoryStream.Position = 0; // Reset position to the beginning
                return memoryStream;
            }
            else
            {
                throw new FileNotFoundException($"File {fileName} not found in zip archive {basePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading from zip file: {ex.Message}");
            throw;
        }
    }
}
