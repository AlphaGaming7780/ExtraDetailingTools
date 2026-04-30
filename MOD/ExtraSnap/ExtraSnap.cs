using Game;
using Game.Prefabs;
using Game.Tools;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using Unity.Jobs;

namespace ExtraDetailingTools.ExtraSnap
{
    public class ExtraSnapBase
    {
        private static readonly Dictionary<Type, ExtraSnapBase> _extraSnaps = new();

        public static TExtraSnap RegisterInstance<TExtraSnap>() where TExtraSnap : ExtraSnapBase, new()
        {
            var type = typeof(TExtraSnap);
            if (_extraSnaps.ContainsKey(type))
                throw new InvalidOperationException($"{type.Name} instance is already registered.");

            var instance = new TExtraSnap();
            _extraSnaps[type] = instance;
            return instance;
        }

        public static bool TryGetInstance<TExtraSnap>(out TExtraSnap instance) where TExtraSnap : ExtraSnapBase
        {
            var type = typeof(TExtraSnap);
            if (_extraSnaps.TryGetValue(type, out var found))
            {
                instance = (TExtraSnap)found;
                return true;
            }
            instance = null;
            return false;
        }

        public static TExtraSnap GetInstance<TExtraSnap>() where TExtraSnap : ExtraSnapBase
        {
            var type = typeof(TExtraSnap);
            if (_extraSnaps.TryGetValue(type, out var instance))
                return (TExtraSnap)instance;

            throw new InvalidOperationException($"{type.Name} instance not found. Make sure it has been initialized.");
        }
    }

    public abstract class ExtraSnapBase<TTool, TSnap> : ExtraSnapBase, IDisposable
        where TTool : ToolBaseSystem
        where TSnap : Enum
    {
        private readonly Harmony _harmony;

        protected static ExtraSnapBase<TTool, TSnap> _instance;

        static ExtraSnapBase()
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

        internal protected ExtraSnapBase()
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

            _harmony = new Harmony(GetType().FullName);

            Patch();
        }

        private void Patch()
        {
            var snapMethod = typeof(TTool).GetMethod("SnapControlPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (snapMethod == null)
                throw new MissingMethodException(typeof(TTool).Name, "SnapControlPoint");

            var raycastMethod = typeof(TTool).GetMethod("InitializeRaycast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (raycastMethod == null)
                throw new MissingMethodException(typeof(TTool).Name, "InitializeRaycast");

            _harmony.Patch(
                snapMethod,
                postfix: new HarmonyMethod(typeof(ExtraSnapBase<TTool, TSnap>).GetMethod(nameof(PostfixSnapControlPoint), BindingFlags.Static | BindingFlags.NonPublic))
            );

            _harmony.Patch(
                raycastMethod,
                postfix: new HarmonyMethod(typeof(ExtraSnapBase<TTool, TSnap>).GetMethod(nameof(PostfixInitializeRaycast), BindingFlags.Static | BindingFlags.NonPublic))
            );
        }

        public void Dispose()
        {
            _harmony?.UnpatchAll(_harmony.Id);
        }

        private static void PostfixSnapControlPoint(ref JobHandle __result)
        {
            var instance = _instance;
            if (instance == null)
                return;

            __result = instance.SnapControlPoint(__result);
        }

        private static void PostfixInitializeRaycast()
        {
            var instance = _instance;
            if (instance == null)
                return;

            instance.InitializeRaycast();
        }


        protected TTool _Tool;

        protected EntityManager EntityManager => _Tool.EntityManager;

        protected PrefabSystem _PrefabSystem;

        protected ToolRaycastSystem _ToolRaycastSystem;

        protected ToolSystem _ToolSystem;

        public TSnap ExtraSnap;

        protected bool IsEditor => _ToolSystem.actionMode.IsEditor();

        protected abstract void InitializeRaycast();

        protected abstract JobHandle SnapControlPoint(JobHandle inputDeps);


        protected Snap GetActualToolSnap() 
        {
            _Tool.GetAvailableSnapMask(out var onMask, out var offMask);
            return ToolBaseSystem.GetActualSnap(_Tool.selectedSnap, onMask, offMask);
        }

    }
}
