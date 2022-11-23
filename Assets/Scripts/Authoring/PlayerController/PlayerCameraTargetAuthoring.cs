using Unity.Entities;
using Unity.MegaCity.CameraManagement;
using UnityEngine;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Create tag component for the player camera target
    /// </summary>
    public class PlayerCameraTargetAuthoring : MonoBehaviour
    {
    }

    [BakingVersion("Abdul", 1)]
    public class PlayerCameraTargetBaker : Baker<PlayerCameraTargetAuthoring>
    {
        public override void Bake(PlayerCameraTargetAuthoring authoring)
        {
            AddComponent<PlayerCameraTarget>();
        }
    }
}
