using Game.Tools;
using System;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace ExtraDetailingTools
{
    internal static class AnarchyBridge
    {
        private static bool _initialized;
        private static Type _bridgeType;

        // Cached MethodInfo for each bridge method
        private static MethodInfo _tryAddToolSystem;
        private static MethodInfo _tryAddAnarchyComponent;
        private static MethodInfo _tryAddTransformLockComponent;
        private static MethodInfo _addAnarchyComponent_Entities;
        private static MethodInfo _addAnarchyComponent_Query;
        private static MethodInfo _addTransformLockComponent_Entities;
        private static MethodInfo _addTransformLockComponent_Query;
        private static MethodInfo _removeAnarchyComponent_Single;
        private static MethodInfo _removeAnarchyComponent_Entities;
        private static MethodInfo _removeAnarchyComponent_Query;
        private static MethodInfo _removeTransformLockComponent_Single;
        private static MethodInfo _removeTransformLockComponent_Entities;
        private static MethodInfo _removeTransformLockComponent_Query;
        private static MethodInfo _getAnarchyComponentType;
        private static MethodInfo _getTransformLockComponentType;

        public static bool IsAvailable => GetBridgeType() != null;

        public static bool Initialize(bool force = false)
        {
            if (_initialized && !force) return GetBridgeType() != null;
            _initialized = false; // Reset initialization to allow re-attempt
            return GetBridgeType() != null;
        }

        private static Type GetBridgeType()
        {
            if (_bridgeType != null) return _bridgeType;
            if (_initialized) return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.Equals("Anarchy", StringComparison.Ordinal))
                    continue;

                _bridgeType = assembly.GetType("Anarchy.Bridge.AnarchyBridge");
                if (_bridgeType != null)
                {
                    CacheAllMethods();
                }
                else
                {
                    EDT.Logger.Warn("Couldn't locate Anarchy Bridge type.");
                }

                break;
            }

            _initialized = true;
            return _bridgeType;
        }

        private static void CacheAllMethods()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            _tryAddToolSystem             = _bridgeType.GetMethod("TryAddToolSystem",             flags, null, new[] { typeof(ToolBaseSystem) }, null);
            _tryAddAnarchyComponent       = _bridgeType.GetMethod("TryAddAnarchyComponent",       flags, null, new[] { typeof(Entity) }, null);
            _tryAddTransformLockComponent = _bridgeType.GetMethod("TryAddTransformLockComponent", flags, null, new[] { typeof(Entity), typeof(Game.Objects.Transform) }, null);

            _addAnarchyComponent_Entities       = _bridgeType.GetMethod("AddAnarchyComponent",       flags, null, new[] { typeof(NativeArray<Entity>) }, null);
            _addAnarchyComponent_Query          = _bridgeType.GetMethod("AddAnarchyComponent",       flags, null, new[] { typeof(EntityQuery) }, null);
            _addTransformLockComponent_Entities = _bridgeType.GetMethod("AddTransformLockComponent", flags, null, new[] { typeof(NativeArray<Entity>) }, null);
            _addTransformLockComponent_Query    = _bridgeType.GetMethod("AddTransformLockComponent", flags, null, new[] { typeof(EntityQuery) }, null);

            _removeAnarchyComponent_Single         = _bridgeType.GetMethod("RemoveAnarchyComponent",       flags, null, new[] { typeof(Entity) }, null);
            _removeAnarchyComponent_Entities       = _bridgeType.GetMethod("RemoveAnarchyComponent",       flags, null, new[] { typeof(NativeArray<Entity>) }, null);
            _removeAnarchyComponent_Query          = _bridgeType.GetMethod("RemoveAnarchyComponent",       flags, null, new[] { typeof(EntityQuery) }, null);
            _removeTransformLockComponent_Single   = _bridgeType.GetMethod("RemoveTransformLockComponent", flags, null, new[] { typeof(Entity) }, null);
            _removeTransformLockComponent_Entities = _bridgeType.GetMethod("RemoveTransformLockComponent", flags, null, new[] { typeof(NativeArray<Entity>) }, null);
            _removeTransformLockComponent_Query    = _bridgeType.GetMethod("RemoveTransformLockComponent", flags, null, new[] { typeof(EntityQuery) }, null);

            _getAnarchyComponentType       = _bridgeType.GetMethod("GetAnarchyComponentType",       flags, null, Type.EmptyTypes, null);
            _getTransformLockComponentType = _bridgeType.GetMethod("GetTransformLockComponentType", flags, null, Type.EmptyTypes, null);
        }

        private static bool InvokeBool(MethodInfo method, string name, params object[] args)
        {
            if (method == null)
            {
                EDT.Logger.Warn($"AnarchyBridge: Method '{name}' not found.");
                return false;
            }

            try
            {
                return method.Invoke(null, args) is bool result && result;
            }
            catch (Exception ex)
            {
                EDT.Logger.Error($"AnarchyBridge.{name} failed: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
        }

        private static void InvokeVoid(MethodInfo method, string name, params object[] args)
        {
            if (method == null)
            {
                EDT.Logger.Warn($"AnarchyBridge: Method '{name}' not found.");
                return;
            }

            try
            {
                method.Invoke(null, args);
            }
            catch (Exception ex)
            {
                EDT.Logger.Error($"AnarchyBridge.{name} failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ── Misc ──

        public static bool IsEnabled()
        {
            if (GetBridgeType() == null) return false;
            return true; // Placeholder for potential future functionality, currently just checks for bridge presence
        }

        // ── Tool Registration ──

        public static bool TryAddToolSystem(ToolBaseSystem tool)
        {
            if (GetBridgeType() == null) return false;
            return InvokeBool(_tryAddToolSystem, nameof(TryAddToolSystem), tool);
        }

        // ── Anarchy Component (PreventOverride) ──

        public static bool TryAddAnarchyComponent(Entity entity)
        {
            if (GetBridgeType() == null) return false;
            return InvokeBool(_tryAddAnarchyComponent, nameof(TryAddAnarchyComponent), entity);
        }

        public static void AddAnarchyComponent(NativeArray<Entity> entities)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_addAnarchyComponent_Entities, nameof(AddAnarchyComponent), entities);
        }

        public static void AddAnarchyComponent(EntityQuery query)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_addAnarchyComponent_Query, nameof(AddAnarchyComponent), query);
        }

        public static void RemoveAnarchyComponent(Entity entity)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_removeAnarchyComponent_Single, nameof(RemoveAnarchyComponent), entity);
        }

        public static void RemoveAnarchyComponent(NativeArray<Entity> entities)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_removeAnarchyComponent_Entities, nameof(RemoveAnarchyComponent), entities);
        }

        public static void RemoveAnarchyComponent(EntityQuery query)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_removeAnarchyComponent_Query, nameof(RemoveAnarchyComponent), query);
        }

        // ── Transform Lock (TransformRecord) ──

        public static bool TryAddTransformLockComponent(Entity entity, Game.Objects.Transform transform)
        {
            if (GetBridgeType() == null) return false;
            return InvokeBool(_tryAddTransformLockComponent, nameof(TryAddTransformLockComponent), entity, transform);
        }

        public static void AddTransformLockComponent(NativeArray<Entity> entities)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_addTransformLockComponent_Entities, nameof(AddTransformLockComponent), entities);
        }

        public static void AddTransformLockComponent(EntityQuery query)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_addTransformLockComponent_Query, nameof(AddTransformLockComponent), query);
        }

        public static void RemoveTransformLockComponent(Entity entity)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_removeTransformLockComponent_Single, nameof(RemoveTransformLockComponent), entity);
        }

        public static void RemoveTransformLockComponent(NativeArray<Entity> entities)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_removeTransformLockComponent_Entities, nameof(RemoveTransformLockComponent), entities);
        }

        public static void RemoveTransformLockComponent(EntityQuery query)
        {
            if (GetBridgeType() == null) return;
            InvokeVoid(_removeTransformLockComponent_Query, nameof(RemoveTransformLockComponent), query);
        }

        // ── Component Types ──

        public static ComponentType GetAnarchyComponentType()
        {
            if (GetBridgeType() == null || _getAnarchyComponentType == null)
            {
                EDT.Logger.Warn("AnarchyBridge: Method 'GetAnarchyComponentType' not found.");
                return default;
            }

            try
            {
                return (ComponentType)_getAnarchyComponentType.Invoke(null, null);
            }
            catch (Exception ex)
            {
                EDT.Logger.Error($"AnarchyBridge.GetAnarchyComponentType failed: {ex.InnerException?.Message ?? ex.Message}");
                return default;
            }
        }

        public static ComponentType GetTransformLockComponentType()
        {
            if (GetBridgeType() == null || _getTransformLockComponentType == null)
            {
                EDT.Logger.Warn("AnarchyBridge: Method 'GetTransformLockComponentType' not found.");
                return default;
            }

            try
            {
                return (ComponentType)_getTransformLockComponentType.Invoke(null, null);
            }
            catch (Exception ex)
            {
                EDT.Logger.Error($"AnarchyBridge.GetTransformLockComponentType failed: {ex.InnerException?.Message ?? ex.Message}");
                return default;
            }
        }
    }
}
