using cohtml.Net;
using Colossal.UI.Binding;
using Extra;
using Game.Rendering;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using HarmonyLib;
using System;

namespace ExtraDetailingTools
{
	internal partial class UI : UISystemBase
	{
        private static GetterValueBinding<bool> showMarker;
        static RenderingSystem renderingSystem;

        protected override void OnCreate()
		{
			base.OnCreate();

            renderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();

            AddBinding(showMarker = new GetterValueBinding<bool>("edt", "markersvisible", () => renderingSystem.markersVisible));
            AddBinding(new TriggerBinding("edt", "togglemarkers", new Action(ShowMarker)));
            AddBinding(new TriggerBinding<bool>("edt", "forceshowmarkers", new Action<bool>(ShowMarker)));

            AddBinding(new TriggerBinding("edt", "updatemarkersvisible", () => { showMarker.Update(); }));

        }

        private static void ShowMarker(bool value)
        {
            renderingSystem.markersVisible = value;
            showMarker.Update();
        }

        private static void ShowMarker()
        {
            renderingSystem.markersVisible = !renderingSystem.markersVisible;
            showMarker.Update();
        }

        [HarmonyPatch(typeof(ToolUISystem), "OnToolChanged", typeof(ToolBaseSystem))]
        class ToolUISystem_OnToolChanged
        {
            static bool showMarker = false;
            private static bool Prefix(ToolBaseSystem tool)
            {

                if(tool is AreaToolSystem || tool is ObjectToolSystem || tool is NetToolSystem)
                {
                    showMarker = true;
                } else if(showMarker)
                {
                    showMarker = false;
                    ShowMarker(false);
                }
                return true;
            }
        }

    }
}
