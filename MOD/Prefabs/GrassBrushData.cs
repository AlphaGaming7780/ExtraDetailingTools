using ExtraDetailingTools.Systems.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace ExtraDetailingTools.Prefabs
{
    public struct GrassBrushData : IComponentData
    {
        public GrassToolSystem.State m_State;
    }
}
