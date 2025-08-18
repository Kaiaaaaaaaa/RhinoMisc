using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;

namespace kkRhinoMisc.Commands
{
    public class RhGetElevation : Command
    {
        public override string EnglishName => "GetElevation";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // --- Save and unlock/show all layers ---
            var layerTable = doc.Layers;
            var originalLayerStates = new List<(int Index, bool IsVisible, bool IsLocked)>();
            for (int i = 0; i < layerTable.Count; i++)
            {
                var layer = layerTable[i];
                originalLayerStates.Add((i, layer.IsVisible, layer.IsLocked));
                if (!layer.IsVisible || layer.IsLocked)
                {
                    layer.IsVisible = true;
                    layer.IsLocked = false;
                }
            }
            doc.Views.Redraw();

            try
            {
                // --- Prompt for click ---
                var getPoint = new GetPoint();
                getPoint.SetCommandPrompt("[RhinoMisc] Click a object in the viewport to measure elevation");
                getPoint.Get();

                if (getPoint.CommandResult() != Result.Success)
                    return getPoint.CommandResult();

                var clickedPoint = getPoint.Point();
                var view = getPoint.View();
                if (view == null)
                {
                    RhinoApp.WriteLine("[RhinoMisc] No view found.");
                    return Result.Failure;
                }

                // --- Ray from camera to click ---
                var camLocation = view.ActiveViewport.CameraLocation;
                var rayDir = clickedPoint - camLocation;
                rayDir.Unitize();
                var ray = new Ray3d(camLocation, rayDir);

                // --- Find geometry hit by ray ---
                var geometryList = new List<GeometryBase>();
                foreach (var obj in doc.Objects)
                {
                    if (obj.IsSelectable() && obj.Visible && !obj.IsLocked)
                        geometryList.Add(obj.Geometry);
                }

                var hits = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometryList, 1);
                if (hits == null || hits.Length == 0)
                {
                    RhinoApp.WriteLine("[RhinoMisc] No object was hit.");
                    return Result.Nothing;
                }

                var hitPoint = hits[0];
                RhinoApp.WriteLine("###################");
                RhinoApp.WriteLine($"Elevation (Z): {hitPoint.Z:0.###}");
                RhinoApp.WriteLine("###################");
                return Result.Success;
            }
            finally
            {
                // --- Restore all layer states ---
                foreach (var (index, isVisible, isLocked) in originalLayerStates)
                {
                    var layer = layerTable[index];
                    layer.IsVisible = isVisible;
                    layer.IsLocked = isLocked;
                }
                doc.Views.Redraw();
            }
        }
    }
}
