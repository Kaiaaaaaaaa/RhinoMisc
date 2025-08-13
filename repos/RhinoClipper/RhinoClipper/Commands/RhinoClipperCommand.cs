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

            //  string folderPath = @"C:\Users\kaigil\AppData\Local\Temp";

            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

            // Get all DWG files in the folder
            FileInfo[] dwgFiles = directoryInfo.GetFiles("*.dwg");

            // Find the newest DWG file based on last modified timestamp
            FileInfo newestDwgFile = dwgFiles.OrderByDescending(f => f.LastWriteTime).First();


            RhinoApp.WriteLine(@"[kkRhinoMisc] Using folder path: " + folderPath);
            RhinoApp.WriteLine(@"[kkRhinoMisc] File path with full name is: " + newestDwgFile.FullName);

            // Attempt to import the geometry from the file

            bool importSuccess = doc.Import(newestDwgFile.FullName);
            //   Rhino.RhinoApp.RunScript("_-AgutImportDXF 10 _Enter", true);

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
    }
}