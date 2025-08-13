using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Rhino;
using Rhino.Commands;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Render;
using Rhino.UI;

namespace kkRhinoMisc.Commands
{
    public class RhClip : Command
    {
        public RhClip()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static RhClip Instance { get; private set; }

        public override string EnglishName => "kkRhClip";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp"
            );

            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            FileInfo newestDwgFile = null;

            // Get all DWG files in the folder
            FileInfo[] dwgFiles = directoryInfo.GetFiles("*.dwg");
            RhinoApp.WriteLine(@"[kkRhinoMisc] Using folder path: " + folderPath);

            // Find the newest DWG file based on last modified timestamp
            try
            {
                if (dwgFiles != null && dwgFiles.Any())
                {
                    newestDwgFile = dwgFiles.OrderByDescending(f => f.LastWriteTime).First();
                    RhinoApp.WriteLine($"[kkRhinoMisc] Found newest DWG file: {newestDwgFile.Name}");

                    // Attempt to import the geometry from the file
                    bool importSuccess = doc.Import(newestDwgFile.FullName);

                    if (importSuccess)
                    {
                        RhinoApp.WriteLine("[kkRhinoMisc] Geometry imported successfully, yay! :D");
                        return Result.Success;
                    }
                    else
                    {
                        RhinoApp.WriteLine("[kkRhinoMisc] Failed to import geometry :C");
                        return Result.Failure;
                    }
                }
                else
                {
                    RhinoApp.WriteLine("[kkRhinoMisc] No DWG files found in the folder.");
                    return Result.Nothing; // or Result.Cancel if more appropriate
                }
            }
            catch (InvalidOperationException ex)
            {
                // This catches the case where First() is called on an empty sequence
                RhinoApp.WriteLine($"[kkRhinoMisc] No DWG files available to process:\n{ex.Message}");
                return Result.Failure;
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                RhinoApp.WriteLine($"[kkRhinoMisc] An error occurred while processing DWG files:\n{ex.Message}");
                return Result.Failure;
            }
        }
    }
}