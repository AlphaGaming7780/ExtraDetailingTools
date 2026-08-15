using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
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
    public abstract class ExtraSnapBase : IDisposable, IJsonWritable
    {
        protected ExtraSnapUISystem m_ExtraSnapUISystem;

        public bool IsDirty { get; internal set; } = true;

        public abstract ToolBaseSystem Tool { get; }

        protected ExtraSnapBase()
        {
            m_ExtraSnapUISystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<ExtraSnapUISystem>();
        }

        protected TriggerBinding AddTriggerBinding(string key, Action action) => m_ExtraSnapUISystem.AddTriggerBinding(this, key, action);
        protected TriggerBinding<T> AddTriggerBinding<T>(string key, Action<T> action, IReader<T> reader = null) => m_ExtraSnapUISystem.AddTriggerBinding<T>(this, key, action, reader);

        public abstract void Dispose();
        public abstract void Write(IJsonWriter writer);

        public abstract void SetExtraSnap(uint snapValue);

        protected void MarkDirty()
        {
            IsDirty = true;
        }
    }

    public abstract class ExtraSnapBase<TTool, TSnap> : ExtraSnapBase
        where TTool : ToolBaseSystem
        where TSnap : Enum
    {
        private readonly Harmony _harmony;

        protected static ExtraSnapBase<TTool, TSnap> m_Instance;

        protected TTool m_Tool;

        protected EntityManager EntityManager => m_Tool.EntityManager;

        protected PrefabSystem m_PrefabSystem;

        protected ToolRaycastSystem m_ToolRaycastSystem;

        protected ToolSystem m_ToolSystem;

        protected TSnap m_SnapOnMask;

        protected TSnap m_SnapOffMask;

        protected TSnap m_SelectedSnap;

        public override ToolBaseSystem Tool => m_Tool;

        protected bool IsEditor => m_ToolSystem.actionMode.IsEditor();

        protected abstract void InitializeRaycast();

        protected abstract JobHandle SnapControlPoint(JobHandle inputDeps);

        static ExtraSnapBase()
        {
            Type type = typeof(TSnap);

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
            m_Instance = this;

            m_Tool = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<TTool>();

            m_ToolSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<ToolSystem>();

            m_PrefabSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<PrefabSystem>();

            m_ToolRaycastSystem = World.DefaultGameObjectInjectionWorld
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

        public override void Dispose()
        {
            _harmony?.UnpatchAll(_harmony.Id);
        }

        private static void PostfixSnapControlPoint(ref JobHandle __result)
        {
            var instance = m_Instance;
            if (instance == null)
                return;

            __result = instance.SnapControlPoint(__result);
        }

        private static void PostfixInitializeRaycast()
        {
            var instance = m_Instance;
            if (instance == null)
                return;

            instance.GetAvailableSnapMask(out TSnap onMask, out TSnap offMask);
            if ((uint)(object)onMask != (uint)(object)instance.m_SnapOnMask || (uint)(object)offMask != (uint)(object)instance.m_SnapOffMask)
            {
                instance.m_SnapOnMask = onMask;
                instance.m_SnapOffMask = offMask;
                instance.MarkDirty();
            }
            instance.InitializeRaycast();
        }

        protected Snap GetActualToolSnap() 
        {
            m_Tool.GetAvailableSnapMask(out var onMask, out var offMask);
            return ToolBaseSystem.GetActualSnap(m_Tool.selectedSnap, onMask, offMask);
        }

        protected virtual void GetAvailableSnapMask(out TSnap onMask, out TSnap offMask)
        {
            onMask = default;
            offMask = default;
        }

        protected TSnap GetActualSnap() => GetActualSnap(m_SelectedSnap, m_SnapOnMask, m_SnapOffMask);

        protected TSnap GetActualSnap(TSnap selectedSnap, TSnap onMask, TSnap offMask)
        {
            uint selected = (uint)(object)selectedSnap;
            uint on = (uint)(object)onMask;
            uint off = (uint)(object)offMask;
            return (TSnap)Enum.ToObject(typeof(TSnap), (selected | ~off) & on);
        }

        public sealed override void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("SnapOnMask");
            writer.Write((uint)(object)m_SnapOnMask);
            writer.PropertyName("SnapOffMask");
            writer.Write((uint)(object)m_SnapOffMask);
            writer.PropertyName("SelectedSnap");
            writer.Write((uint)(object)m_SelectedSnap);
            OnWrite(writer);
            writer.TypeEnd();
        }

        public virtual void OnWrite(IJsonWriter writer) { }

        public override void SetExtraSnap(uint snapValue)
        {
            if (snapValue == (uint)(object)m_SelectedSnap) return;
            m_SelectedSnap = (TSnap)Enum.ToObject(typeof(TSnap), snapValue);
            MarkDirty();
        }

    }
}
