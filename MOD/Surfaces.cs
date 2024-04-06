using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraDetailingTools
{
    internal class Surfaces
    {
        internal static int GetRendererPriorityByCat(string cat)
        {
            return cat switch
            {
                "Ground" => -100,
                "Grass" => -99,
                "Sand" => -98,
                "Concrete" => -97,
                "Wood" => -97,
                "Pavement" => -96,
                "Tiles" => -95,
                _ => -100
            };
        }

        internal static string GetCatByRendererPriority(int i)
        {
            return i switch
            {
                -100 => "Ground",
                -99 => "Grass", //"Grass",
                -98 => "Sand", //"Sand",
                -97 => "Concrete",
                -96 => "Pavement", //"Pavement",
                -95 => "Tiles",
                _ => "Misc"
            };
        }
    }
}
