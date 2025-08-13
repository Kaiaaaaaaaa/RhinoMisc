using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;

namespace RhinoMisc.Fov.UI
{
    [Guid("E8A7B3F1-2E24-4D0E-A5B0-5F8A4C0C1B9D")]
    public class FovPanel : Panel
    {
        // Your panel GUID (generate a new one)
        public static Guid PanelId => typeof(FovPanel).GUID;

        const int MinFovDeg = 5;    // sensible floor to avoid extreme distortion
        const int MaxFovDeg = 120;   // Rhino’s command allows wide angles too

        readonly NumericStepper _deg;   // exact integer degrees
        readonly Button _minus;
        readonly Button _plus;
        readonly Button _sync;

        public FovPanel()
        {
            // Controls
            _deg = new NumericStepper
            {
                MinValue = MinFovDeg,
                MaxValue = MaxFovDeg,
                DecimalPlaces = 0,
                Increment = 1,
                Width = 80
            };
            _deg.ValueChanged += (_, __) => ApplyFov((int)_deg.Value);

            _minus = new Button { Text = "–1°" };
            _plus = new Button { Text = "+1°" };
            _minus.Click += (_, __) => { _deg.Value = Clamp((int)_deg.Value - 1); ApplyFov((int)_deg.Value); };
            _plus.Click += (_, __) => { _deg.Value = Clamp((int)_deg.Value + 1); ApplyFov((int)_deg.Value); };

            _sync = new Button { Text = "Sync from view" };
            _sync.Click += (_, __) => ReadFovIntoControl();

            var content = new DynamicLayout { Padding = new Padding(10), Spacing = new Size(8, 8) };
            content.AddRow(new Label { Text = "Field of View (°)", VerticalAlignment = VerticalAlignment.Center }, _deg, _minus, _plus);
            content.AddRow(_sync);
            Content = content;

            // Initialize from current active view
            ReadFovIntoControl();
        }

        static int Clamp(int v) => Math.Max(MinFovDeg, Math.Min(MaxFovDeg, v));

        void ReadFovIntoControl()
        {
            var doc = RhinoDoc.ActiveDoc;
            var view = doc?.Views?.ActiveView;
            if (view == null) return;

            var vpi = new ViewportInfo(view.ActiveViewport);
            // vpi.CameraAngle is HALF of the smaller-dimension FOV, in radians
            double halfAngleRad = vpi.CameraAngle;
            int fovDeg = (int)Math.Round(RhinoMath.ToDegrees(halfAngleRad * 2.0));
            _deg.Value = Clamp(fovDeg);
        }

        static void EnsurePerspective(RhinoView view)
        {
            var vp = view.ActiveViewport;
            if (!vp.IsPerspectiveProjection)
                vp.ChangeToPerspectiveProjection(true, vp.Camera35mmLensLength);
        }

        void ApplyFov(int fovDeg)
        {
            var doc = RhinoDoc.ActiveDoc;
            var view = doc?.Views?.ActiveView;
            if (view == null) return;

            EnsurePerspective(view);

            var vp = view.ActiveViewport;
            var vpi = new ViewportInfo(vp);

            // Rhino uses HALF of the smaller FOV dimension (radians)
            double halfAngleRad = RhinoMath.ToRadians(fovDeg * 0.5);
            vpi.CameraAngle = halfAngleRad;

            // Apply and redraw
            vp.SetViewProjection(vpi, true);
            view.Redraw();

            RhinoApp.WriteLine($"FOV set to {fovDeg}° (half-angle {halfAngleRad * 180.0 / Math.PI:0.###}°).");
        }
    }
}
