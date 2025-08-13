using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using RhinoMisc.Fov.UI;

namespace kkRhinoMisc.Fov.Commands
{
    public class ShowFovPanelCommand : Command
    {
        public static Guid PanelId => RhinoMisc.Fov.UI.FovPanel.PanelId;

        public override string EnglishName => "FOVPanel";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Panels.OpenPanel(PanelId); // shows (or focuses) the panel
            return Result.Success;
        }
    }
}
