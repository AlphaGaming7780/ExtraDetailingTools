using Unity.Entities;

namespace ExtraDetailingTools.Systems
{
    // Flushed at PostTool: after any caller has had a chance to request a duplicate during ToolUpdate
    // (earlier in the same frame), but before Modification1 (GenerateObjectsSystem), so the resulting
    // CreationDefinition entities are guaranteed visible in time to be processed this same frame.
    public partial class DuplicateEntityBarrier : EntityCommandBufferSystem
    {
    }
}
