using System;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp"
        );

        string newestDwgFilePath = FindNewestDwgFile(folderPath);

        if (!string.IsNullOrEmpty(newestDwgFilePath))
        {
            Console.WriteLine($"Newest DWG file: {newestDwgFilePath}");

            Console.WriteLine(newestDwgFilePath);


        }
        else
        {
            Console.WriteLine("No DWG files found in the folder.");
        }
    }

    static string FindNewestDwgFile(string folderPath)
    {
        try
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

            // Get all DWG files in the folder
            FileInfo[] dwgFiles = directoryInfo.GetFiles("*.dwg");

            string ClipString = folderPath;

            if (dwgFiles.Length > 0)
            {
                // Find the newest DWG file based on last modified timestamp
                FileInfo newestDwgFile = dwgFiles.OrderByDescending(f => f.LastWriteTime).First();

                return newestDwgFile.FullName;
            }


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");

        }

        return null;
    }
}
