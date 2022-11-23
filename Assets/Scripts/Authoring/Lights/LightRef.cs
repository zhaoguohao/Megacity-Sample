using Unity.Entities;
using UnityEngine;

namespace Unity.MegaCity.Lights
{
    /// <summary>
    /// Creates SharedLight component, used later by the light pool system
    /// </summary>
    public class LightRef : MonoBehaviour
    {
        public GameObject LightReference;
    }

    [BakingVersion("Abdul", 1)]
    public class LightRefBaker : Baker<LightRef>
    {
        public override void Bake(LightRef authoring)
        {
            AddSharedComponentManaged(new SharedLight { Value = authoring.LightReference });
        }
    }
}
