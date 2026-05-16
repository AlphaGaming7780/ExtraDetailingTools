using ExtraDetailingTools.Systems.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace ExtraDetailingTools.Components
{
    public struct TransformGizmosHandle : IComponentData
    {
        public TransformGizmoTool.Handle Handle;

        public TransformGizmosHandle(TransformGizmoTool.Handle handle)
        {
            Handle = handle;
        }

    }
}
