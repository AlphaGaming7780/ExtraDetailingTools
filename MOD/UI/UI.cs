using Colossal.UI.Binding;
using Extra;
using Game.Rendering;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using System;

namespace ExtraDetailingTools
{
	internal partial class UI : UISystemBase
	{
        private static GetterValueBinding<bool> showMarker;
        RenderingSystem renderingSystem;
        protected override void OnCreate()
		{
			base.OnCreate();

            renderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();

            AddBinding(showMarker = new GetterValueBinding<bool>("edt", "showmarker", () => renderingSystem.markersVisible));
            AddBinding(new TriggerBinding("edt", "showmarker", new Action(ShowMarker)));

            AddBinding(new TriggerBinding("edt", "updateshowmarker", () => { showMarker.Update(); }));

        }

        private void ShowMarker()
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
