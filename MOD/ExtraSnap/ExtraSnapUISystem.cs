using Colossal.UI.Binding;
using Game.Tools;
using Game.UI;
using Game.Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;

namespace ExtraDetailingTools.ExtraSnap
{
    public partial class ExtraSnapUISystem : UISystemBase
    {

        private readonly Dictionary<Type, ExtraSnapBase> m_ExtraSnaps = new();

        private ExtraSnapBase m_ActiveExtraSnap;

        private ToolSystem m_ToolSystem;


        private GetterValueBinding<ExtraSnapBase> m_ActiveExtraSnapBinding;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            AddBinding(m_ActiveExtraSnapBinding = new GetterValueBinding<ExtraSnapBase>("EDT", "ActiveExtraSnap", () => m_ActiveExtraSnap, ValueWriters.Nullable(new ValueWriter<ExtraSnapBase>()), AlwaysChangedComparer.Instance));
            AddBinding(new TriggerBinding<uint>("EDT", "SetExtraSnap", (v) => m_ActiveExtraSnap?.SetExtraSnap(v)));

        }

        protected override void OnDestroy()
        {
            m_ToolSystem.EventToolChanged -= OnToolChanged;
            UnregisterAllInstances();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (m_ActiveExtraSnap != null && m_ActiveExtraSnap.IsDirty)
            {
                m_ActiveExtraSnapBinding.Update();
                m_ActiveExtraSnap.IsDirty = false;
            }

        }

        private void OnToolChanged(ToolBaseSystem newTool)
        {
            m_ActiveExtraSnap = m_ExtraSnaps.Values.FirstOrDefault(instance => instance.Tool == newTool);
            m_ActiveExtraSnapBinding.Update();
        }

        // ExtraSnapBase instances are mutated in place, never replaced - m_ActiveExtraSnap is the same
        // object reference every frame. GetterValueBinding<T>'s default comparer (EqualityComparer<T>.
        // Default, which for a plain class falls back to reference equality) would see "same reference"
        // and silently skip every push after the very first one, no matter how many times we call
        // .Update(). We already gate when to call .Update() via IsDirty ourselves, so the binding's own
        // internal check should never be the one deciding - this comparer just always says "changed".
        private class AlwaysChangedComparer : EqualityComparer<ExtraSnapBase>
        {
            public static readonly AlwaysChangedComparer Instance = new();
            public override bool Equals(ExtraSnapBase x, ExtraSnapBase y) => false;
            public override int GetHashCode(ExtraSnapBase obj) => 0;
        }

        internal TriggerBinding AddTriggerBinding(ExtraSnapBase extraSnapBase, string key, Action action) => AddTriggerBinding(new TriggerBinding("EDT", $"{extraSnapBase.GetType().FullName}.{key}", action));
        internal TriggerBinding<T> AddTriggerBinding<T>(ExtraSnapBase extraSnapBase, string key, Action<T> action) => AddTriggerBinding(new TriggerBinding<T>("EDT", $"{extraSnapBase.GetType().FullName}.{key}", action));

        private T AddTriggerBinding<T>(T triggerBinding) where T : BindingBase
        {
            AddBinding(triggerBinding);
            return triggerBinding;
        }

        public TExtraSnap RegisterInstance<TExtraSnap>() where TExtraSnap : ExtraSnapBase, new()
        {
            var type = typeof(TExtraSnap);
            if (m_ExtraSnaps.ContainsKey(type))
                throw new InvalidOperationException($"{type.Name} instance is already registered.");

            var instance = new TExtraSnap();
            m_ExtraSnaps[type] = instance;
            return instance;
        }

        public void UnregisterInstance<TExtraSnap>() where TExtraSnap : ExtraSnapBase
        {
            var type = typeof(TExtraSnap);
            if (!m_ExtraSnaps.ContainsKey(type))
                throw new InvalidOperationException($"{type.Name} instance is not registered.");

            m_ExtraSnaps[type].Dispose();
            m_ExtraSnaps.Remove(type);
        }

        public void UnregisterAllInstances()
        {
            foreach (var instance in m_ExtraSnaps.Values)
            {
                instance.Dispose();
            }
            m_ExtraSnaps.Clear();
        }

        public bool TryGetInstance<TExtraSnap>(out TExtraSnap instance) where TExtraSnap : ExtraSnapBase
        {
            var type = typeof(TExtraSnap);
            if (m_ExtraSnaps.TryGetValue(type, out var found))
            {
                instance = (TExtraSnap)found;
                return true;
            }
            instance = null;
            return false;
        }

        public TExtraSnap GetInstance<TExtraSnap>() where TExtraSnap : ExtraSnapBase
        {
            var type = typeof(TExtraSnap);
            if (m_ExtraSnaps.TryGetValue(type, out var instance))
                return (TExtraSnap)instance;

            throw new InvalidOperationException($"{type.Name} instance not found. Make sure it has been initialized.");
        }

    }
}
