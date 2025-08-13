using System;
using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
using RhinoMisc.Fov.UI;

namespace kkRhinoMisc
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class kkRhinoMiscPlugin : Rhino.PlugIns.PlugIn
    {
        public kkRhinoMiscPlugin()
        {
            Instance = this;
            RhinoApp.WriteLine("[kkRhinoMisc] plugin loaded.");
        }


        ///<summary>Gets the only instance of the RhinoClipperPlugin plug-in.</summary>
        public static kkRhinoMiscPlugin Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Register a dockable panel that hosts our Eto content.
            Panels.RegisterPanel(this, typeof(RhinoMisc.Fov.UI.FovPanel), "FOV", null /* icon */);
            return LoadReturnCode.Success;
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.
    }
}