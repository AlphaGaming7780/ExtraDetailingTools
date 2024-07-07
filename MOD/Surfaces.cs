
namespace ExtraDetailingTools;

internal class Surfaces
{
    internal static string GetCatByRendererPriority(int i)
    {
        return i switch
        {
            -100 => "Ground",
            -99 => "Grass",
            -98 => "Sand",
            -97 => "Concrete",
            -96 => "Pavement",
            -95 => "Tiles",
            _ => "Misc"
        };
    }
}
