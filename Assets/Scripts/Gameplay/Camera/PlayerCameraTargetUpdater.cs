using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Update the hybrid camera target with player position camera target in order for the virtual camera to follow it
    /// </summary>
    public struct PlayerCameraTarget : IComponentData
    {
    }

    public struct PlayerHybridCameraTarget : IComponentData
    {
        public float TargetFollowDamping;
    }

    [BurstCompile]
    public partial struct UpdateCameraTargetJob : IJobEntity
    {
        public LocalToWorld LocalToWorld;
        public float DeltaTime;

        public void Execute(
            ref TransformAspect transformAspect,
            in PlayerHybridCameraTarget playerHybridCameraTarget)
        {
            transformAspect.WorldPosition = math.lerp(transformAspect.WorldPosition, LocalToWorld.Position,
                DeltaTime * playerHybridCameraTarget.TargetFollowDamping);
            transformAspect.WorldRotation = math.slerp(transformAspect.WorldRotation, LocalToWorld.Rotation,
                DeltaTime * playerHybridCameraTarget.TargetFollowDamping);
        }
    }


    [BurstCompile]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct PlayerCameraTargetUpdater : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerCameraTarget>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var player = GetSingletonEntity<PlayerCameraTarget>();
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var localToWorld = state.EntityManager.GetComponentData<LocalToWorld>(player);
            if (HybridCameraManager.Instance == null)
                return;

            HybridCameraManager.Instance.SetPlayerCameraPosition(localToWorld.Position, deltaTime);
            HybridCameraManager.Instance.SetPlayerCameraRotation(localToWorld.Rotation, deltaTime);
        }
    }
}
