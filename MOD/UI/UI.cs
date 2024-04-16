using Game.Tools;
using Game.UI;
using Game.UI.InGame;

namespace ExtraDetailingTools
{
	internal partial class UI : UISystemBase
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			SelectedInfoUISystem selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
			selectedInfoUISystem.AddMiddleSection(World.GetOrCreateSystemManaged<TransformSection>());

			EDT.toolRaycastSystem = World.GetOrCreateSystemManaged<ToolRaycastSystem>();

		}
	}
}
