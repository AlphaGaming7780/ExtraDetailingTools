using Game.UI;
using Game.UI.InGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools
{
    internal partial class UI : UISystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            SelectedInfoUISystem selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            selectedInfoUISystem.AddMiddleSection(World.GetOrCreateSystemManaged<TransformSection>());

        }
    }
}
