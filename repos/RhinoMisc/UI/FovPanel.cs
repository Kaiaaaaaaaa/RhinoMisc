using System;
using System.Runtime.InteropServices;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using System.Linq;
using System.Threading;

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
        readonly TextBox _thisLayer;

        bool _suppress; // prevents event feedback when syncing controls

        // For not getting stuck when rhino is looping through objectx
        private int _pendingLayerUpdate;
        private int _idleArmed;
        private EventHandler _idleHandler;

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

            _thisLayer = new TextBox { ReadOnly = true };

            RhinoDoc.SelectObjects += OnSelectionChanged;
            RhinoDoc.DeselectObjects += OnSelectionChanged;
            RhinoDoc.DeselectAllObjects += OnDeselectAll;
            RhinoDoc.ModifyObjectAttributes += OnModifyAttrs;

            // Idle checker for layer update
            _idleHandler = (s, e) =>
            {
                try
                {
                    if (Interlocked.Exchange(ref _pendingLayerUpdate, 0) == 1)
                    {
                        Application.Instance.AsyncInvoke(UpdateLayerFromSelection);
                        //RhinoApp.WriteLine("[kkRhinoMisc] *ping, I'm idle (and safe try/finally)!");
                    }
                }
                finally
                {
                    RhinoApp.Idle -= _idleHandler;
                    Interlocked.Exchange(ref _idleArmed, 0);
                }
            };

            UpdateLayerFromSelection();

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
            // Row 2: Layer label
            var row2 = new TableLayout
            {
                Spacing = new Size(8, 0),
                Rows =
                {
                    new TableRow(
                        new TableCell(new Label { Text = "Selected object layer:", VerticalAlignment = VerticalAlignment.Center })
                    )
                }
            };
            // Row 3: Text box showing selected object layer
            var row3 = new TableLayout
            {
                Spacing = new Size(8, 0),
                Rows =
                {
                    new TableRow(
                        new TableCell(_thisLayer, true)
                    )
                }
            };

            // Main layout constructor
            var layout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Padding = new Padding(10),
                Spacing = 8,
                Items =
                {
                    new StackLayoutItem(row1, HorizontalAlignment.Stretch),
                    new StackLayoutItem(_slider, HorizontalAlignment.Stretch),
                    new StackLayoutItem(row2, HorizontalAlignment.Stretch),
                    new StackLayoutItem(row3, HorizontalAlignment.Stretch)
                }
            };

            Content = layout;

            ReadFovIntoControls();
        }

        // -------------------------------
        //      FOV helper functions
        // -------------------------------

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

            // Printing of the degrees, leave off when not debugging I think:
            //RhinoApp.WriteLine($"[kkRhinoMisc] FOV set to {fovDeg}° (half-angle {halfAngleRad * 180.0 / Math.PI:0.###}°).");
        }

        // -------------------------------
        //  Object layer helper functions
        // -------------------------------
        void UpdateLayerFromSelection()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return;

            // Only “real” objects by default
            var selected = doc.Objects.GetSelectedObjects(includeLights: false, includeGrips: false).ToList();

            if (selected.Count == 0)
            {
                _thisLayer.Text = "(no object selected)";
                return;
            }

            // Get distinct layer indices of selection
            var layerIndices = selected.Select(o => o.Attributes.LayerIndex).Distinct().ToList();

            if (layerIndices.Count == 1)
            {
                var layer = doc.Layers.FindIndex(layerIndices[0]);
                _thisLayer.Text = layer != null
                    ? FormatLayerPath(layer.FullPath)
                    : "(layer not found :c)";
            }
            else
            {
                _thisLayer.Text = "Objects on multiple layers";
            }
        }

        // Long layer string truncator
        private static string FormatLayerPath(string fullPath, int maxTotal = 60)
        {
            //RhinoApp.WriteLine($"[kkRhinoMisc] Length is: {fullPath.Length}");
            if (string.IsNullOrEmpty(fullPath) || fullPath.Length <= maxTotal)
                return fullPath;

            var parts = fullPath.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length == 0) return fullPath;

            const string sep = " :: ";

            int n = parts.Length;
            int sepTotal = sep.Length * (n - 1);

            // available characters for the parts
            int budget = maxTotal - sepTotal;
            if (budget < n) budget = n; // at least 1 char per part
            //RhinoApp.WriteLine($"[kkRhinoMisc] CharBudget is: {budget}");

            // simple floor division
            int perSegment = budget / n;

            for (int i = 0; i < n; i++)
            {
                if (parts[i].Length > perSegment)
                {
                    parts[i] = parts[i].Substring(0, perSegment) + "*";
                }
            }

            return string.Join(sep, parts);
        }

        // Handlers
        void OnSelectionChanged(object sender, RhinoObjectSelectionEventArgs e)
        {
            System.Threading.Interlocked.Exchange(ref _pendingLayerUpdate, 1);
            if (System.Threading.Interlocked.Exchange(ref _idleArmed, 1) == 0)
                RhinoApp.Idle += _idleHandler;
        }

        void OnDeselectAll(object sender, RhinoDeselectAllObjectsEventArgs e)
        {
            System.Threading.Interlocked.Exchange(ref _pendingLayerUpdate, 1);
            if (System.Threading.Interlocked.Exchange(ref _idleArmed, 1) == 0)
                RhinoApp.Idle += _idleHandler;
        }

        void OnModifyAttrs(object sender, RhinoModifyObjectAttributesEventArgs e)
        {
            // Batch attribute-change storms: mark pending, arm idle once
            if (e?.RhinoObject == null) return;

            var active = RhinoDoc.ActiveDoc;
            if (active == null || e.RhinoObject.Document != active) return;

            if (e.RhinoObject.IsSelected(false) > 0 &&
                e.OldAttributes.LayerIndex != e.NewAttributes.LayerIndex)
            {
                System.Threading.Interlocked.Exchange(ref _pendingLayerUpdate, 1);
                if (System.Threading.Interlocked.Exchange(ref _idleArmed, 1) == 0)
                    RhinoApp.Idle += _idleHandler;
            }
        }

        // Unsubscribe - .NET method for UI closing gracefull
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (System.Threading.Interlocked.Exchange(ref _idleArmed, 0) == 1)
                    RhinoApp.Idle -= _idleHandler;

                RhinoDoc.SelectObjects -= OnSelectionChanged;
                RhinoDoc.DeselectObjects -= OnSelectionChanged;
                RhinoDoc.DeselectAllObjects -= OnDeselectAll;
                RhinoDoc.ModifyObjectAttributes -= OnModifyAttrs;
                //RhinoApp.WriteLine("[kkRhinoMisc] I have unsubscribed!");
            }
            base.Dispose(disposing);
        }
    }
}
