using Unity.Entities;
using UnityEngine;

namespace Unity.MegaCity.Traffic
{
    /// <summary>
    /// Bake all the components required for the VehicleTraffic
    /// </summary>
    public class VehicleTrafficAuthoring : MonoBehaviour
    {
    }

    [BakingVersion("Abdul", 1)]
    public class VehicleTrafficBaker : Baker<VehicleTrafficAuthoring>
    {
        public override void Bake(VehicleTrafficAuthoring authoring)
        {
            AddComponent<VehiclePathing>();
            AddComponent<VehicleTargetPosition>();
            AddComponent<VehiclePhysicsState>();
        }
    }
}
