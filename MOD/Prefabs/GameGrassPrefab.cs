using Game.Prefabs;
using System;
using UnityEngine;
using static Colossal.AssetPipeline.Importers.DidimoImporter.DidimoData;

namespace ExtraDetailingTools.Prefabs
{
    [ComponentMenu("EDT/", new Type[]
    {

    })]
    public class GameGrassPrefab : GrassPrefabBase 
    {
        public Vector2 CropSize = new Vector2(100, 100);
        public float FoliageCoverage = 750;
        public AnimationCurve ScaleOverDistance = new(new Keyframe[] { new Keyframe(0.38731906f, 1, 0, 0, 0, 0), new Keyframe(566.2241f, 0.6515317f, -0.0018226481f, -0.0029498516f, 0, 0), new Keyframe(650.7942f, 0, -0.007311484f, -0.007311484f, 0, 0) } );
    }
}
