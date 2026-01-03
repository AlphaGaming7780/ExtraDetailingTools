using Colossal.Reflection.Tests;
using ExtraDetailingTools.ExtraSnap;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;
using static Game.Rendering.Debug.RenderPrefabRenderer;
using static UnityEngine.GridBrushBase;

namespace ExtraDetailingTools.ExtraSnap
{
    public abstract class ExtraSnap<TTool, TSnap>
        where TTool : ToolBaseSystem
        where TSnap : Enum
    {
        protected static ExtraSnap<TTool, TSnap> _instance;

        static ExtraSnap()
        {
            var type = typeof(TSnap);

            // 1) Must be [Flags]
            if (!type.IsDefined(typeof(FlagsAttribute), inherit: false))
            {
                throw new InvalidOperationException(
                    $"{type.Name} must be marked with [Flags]"
                );
            }

            // 2) Underlying type must be uint
            if (Enum.GetUnderlyingType(type) != typeof(uint))
            {
                throw new InvalidOperationException(
                    $"{type.Name} must have uint as underlying type"
                );
            }
        }

        protected ExtraSnap()
        {
            _instance = this;

            _Tool = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<TTool>();

            _ToolSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<ToolSystem>();

            _PrefabSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<PrefabSystem>();

            _ToolRaycastSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<ToolRaycastSystem>();
        }

        [HarmonyPatch]
        private class Patch_SnapControlPoint
        {
            static MethodBase TargetMethod()
            {
                return typeof(TTool).GetMethod(
                    "SnapControlPoint",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
            }

            static void Postfix(ref JobHandle __result)
            {
                if (_instance == null)
                    return;

                __result = _instance.SnapControlPoint(__result);
            }
        }

        [HarmonyPatch]
        private class Patch_InitializeRaycast
        {
            static MethodBase TargetMethod()
            {
                return typeof(TTool).GetMethod(
                    "InitializeRaycast",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
            }

            static void Postfix(ref JobHandle __result)
            {
                if (_instance == null)
                    return;

                _instance.InitializeRaycast();
            }
        }


        protected TTool _Tool;

        protected EntityManager EntityManager => _Tool.EntityManager;

        protected PrefabSystem _PrefabSystem;

        protected ToolRaycastSystem _ToolRaycastSystem;

        protected ToolSystem _ToolSystem;

        public TSnap ESnap;

        protected abstract void InitializeRaycast();

        protected abstract JobHandle SnapControlPoint(JobHandle inputDeps);


        protected Snap GetActualToolSnap() 
        {
            _Tool.GetAvailableSnapMask(out var onMask, out var offMask);
            return ToolBaseSystem.GetActualSnap(_Tool.selectedSnap, onMask, offMask);
        }

    }
}
