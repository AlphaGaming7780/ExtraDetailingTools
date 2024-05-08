
namespace ExtraDetailingTools
{
    internal class Decals
    {
        internal static string GetCatByDecalName(string decalName)
        {
            if (decalName.ToLower().Contains("parking")) return "Parking";
            if (decalName.ToLower().Contains("arrow")) return "RoadMarkings";
            if (decalName.ToLower().Contains("lanemarkings")) return "RoadMarkings";
            return "Misc";
        }
    }
}
