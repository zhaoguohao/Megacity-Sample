using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Add required tag components for cosmetic physics
    /// </summary>
    public class PlayerVehicleCosmeticPhysicsAuthoring : MonoBehaviour
    {
    }

    [BakingVersion("Abdul", 1)]
    public class PlayerVehicleCosmeticPhysicsBaker : Baker<PlayerVehicleCosmeticPhysicsAuthoring>
    {
        public override void Bake(PlayerVehicleCosmeticPhysicsAuthoring authoring)
        {
            AddComponent<PlayerVehicleCosmeticPhysics>();
        }
    }
}
