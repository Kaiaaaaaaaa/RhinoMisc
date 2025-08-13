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
        public static Guid PanelId => typeof(FovPanel).GUID;

        // Bounds + default (adjust to taste)
        const int MinFovDeg = 5;
        const int MaxFovDeg = 100;
        const int DefaultFovDeg = 27;

        // UI
        readonly NumericStepper _deg;   // exact integer degrees
        readonly Slider _slider;        // same range as stepper
        readonly Button _minus;
        readonly Button _plus;
        readonly Button _reset;

        bool _suppress; // prevents event feedback when syncing controls

        public FovPanel()
        {
            // Controls
            _deg = new NumericStepper
            {
                MinValue = MinFovDeg,
                MaxValue = MaxFovDeg,
                DecimalPlaces = 0,
                Increment = 1,
                Width = 70  // Reduced width to make room for reset button
            };
            _deg.ValueChanged += (_, __) =>
            {
                if (_suppress) return;
                SetUi(Clamp((int)_deg.Value), apply: true);
            };

            _minus = new Button { Text = "–1°", Width = 35 };  // Fixed width
            _plus = new Button { Text = "+1°", Width = 35 };   // Fixed width
            _minus.Click += (_, __) => SetUi(Clamp((int)_deg.Value - 1), apply: true);
            _plus.Click += (_, __) => SetUi(Clamp((int)_deg.Value + 1), apply: true);

            _reset = new Button { Text = "Reset", Width = 50 };  // Fixed width, shorter text
            _reset.Click += (_, __) => SetUi(DefaultFovDeg, apply: true);

            _slider = new Slider
            {
                MinValue = MinFovDeg,
                MaxValue = MaxFovDeg,
                Value = DefaultFovDeg,
                TickFrequency = 0,
                Height = 22,
            };
            _slider.ValueChanged += (_, __) =>
            {
                if (_suppress) return;
                SetUi(Clamp(_slider.Value), apply: true);
            };

            // Row 1: Label + controls distributed evenly
            var row1 = new TableLayout
            {
                Spacing = new Size(8, 0),
                Rows =
                {
                    new TableRow(
                        new TableCell(new Label { Text = "Field of View (°)", VerticalAlignment = VerticalAlignment.Center }),
                        new TableCell(_deg),
                        new TableCell(_minus),
                        new TableCell(_plus),
                        new TableCell(_reset)
                    )
                }
            };

            // Main layout: row 1 + full-width slider
            var layout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Padding = new Padding(10),
                Spacing = 8,
                Items =
                {
                    new StackLayoutItem(row1, HorizontalAlignment.Stretch),
                    new StackLayoutItem(_slider, HorizontalAlignment.Stretch)
                }
            };

            Content = layout;

            // Target window size - remove container approach
            Size = new Size(360, 80);
            MinimumSize = new Size(360, 80);

            // Override SizeHint if available to force dimensions
            try
            {
                // Some Eto implementations support this
                if (this.GetType().GetProperty("PreferredSize") != null)
                    this.GetType().GetProperty("PreferredSize")?.SetValue(this, new Size(360, 80));
            }
            catch { /* Ignore if not supported */ }

            // Initialize from active view
            ReadFovIntoControls();
        }

        static int Clamp(int v) => Math.Max(MinFovDeg, Math.Min(MaxFovDeg, v));

        void ReadFovIntoControls()
        {
            var doc = RhinoDoc.ActiveDoc;
            var view = doc?.Views?.ActiveView;
            if (view == null)
            {
                SetUi(DefaultFovDeg, apply: false);
                return;
            }

            var vpi = new ViewportInfo(view.ActiveViewport);
            // vpi.CameraAngle is HALF of smaller-dimension FOV, radians
            double halfAngleRad = vpi.CameraAngle;
            int fovDeg = (int)Math.Round(RhinoMath.ToDegrees(halfAngleRad * 2.0));
            SetUi(Clamp(fovDeg), apply: false);
        }

        void SetUi(int fovDeg, bool apply)
        {
            _suppress = true;
            _deg.Value = fovDeg;
            _slider.Value = fovDeg;
            _suppress = false;

            if (apply)
                ApplyFov(fovDeg);
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

            vp.SetViewProjection(vpi, true);
            view.Redraw();

            RhinoApp.WriteLine($"FOV set to {fovDeg}° (half-angle {halfAngleRad * 180.0 / Math.PI:0.###}°).");
        }
    }
}