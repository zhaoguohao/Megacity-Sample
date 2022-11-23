using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.MegaCity.Streaming
{
    /// <summary>
    /// Creates two lists of Entities from the ones that got inside of camera range and the entities that got outside.
    /// Using the camera position and StreamingLogicConfig parameters create the lists using 2 different jobs to fill 2 different arrays.
    /// By using BuildCommandBufferJob adds to each entity (SceneSectionData) a component to add or remove it from the Scene.
    /// </summary>
    [Serializable]
    public struct StreamingConfig : IComponentData
    {
        public float DistanceForStreamingIn;
        public float DistanceForStreamingOut;
        public Hash128 PlayerSectionGUID;
        public Hash128 TrafficSectionGUID;
    }

    [BurstCompile]
    public partial struct StreamSubScenesIn : IJobEntity
    {
        public NativeList<Entity> AddRequestList;
        public float3 CameraPosition;
        public float MaxDistanceSquared;

        public void Execute(Entity entity, in SceneSectionData sceneData)
        {
            AABB boundingVolume = sceneData.BoundingVolume;
            var distanceSq = boundingVolume.DistanceSq(CameraPosition);
            if (distanceSq < MaxDistanceSquared)
                AddRequestList.Add(entity);
        }
    }

    [BurstCompile]
    public partial struct StreamSubScenesOut : IJobEntity
    {
        public NativeList<Entity> RemoveRequestList;
        public float3 CameraPosition;
        public float MaxDistanceSquared;
        public Hash128 PlayerSectionGUID;
        public Hash128 TrafficSectionGUID;

        public void Execute(Entity entity, in SceneSectionData sceneData)
        {
            if (sceneData.SceneGUID == PlayerSectionGUID)
                return;

            if (sceneData.SceneGUID == TrafficSectionGUID)
                return;

            AABB boundingVolume = sceneData.BoundingVolume;
            var distanceSq = boundingVolume.DistanceSq(CameraPosition);
            if (distanceSq > MaxDistanceSquared)
                RemoveRequestList.Add(entity);
        }
    }

    [BurstCompile]
    public struct BuildCommandBufferJob : IJob
    {
        public EntityCommandBuffer CommandBuffer;
        public NativeArray<Entity> AddRequestArray;
        public NativeArray<Entity> RemoveRequestArray;

        public void Execute()
        {
            foreach (var entity in AddRequestArray)
            {
                CommandBuffer.AddComponent(entity, new RequestSceneLoaded { LoadFlags = SceneLoadFlags.LoadAdditive });
            }

            foreach (var entity in RemoveRequestArray)
            {
                CommandBuffer.RemoveComponent<RequestSceneLoaded>(entity);
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct StreamingLogicSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StreamingConfig>();
            _query = state.GetEntityQuery(ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<StreamingConfig>());
        }

        public void OnDestroy(ref SystemState state)
        {
        }


        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBufferSystem =
                state.World.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();

            var streamingLogicConfig = _query.GetSingleton<StreamingConfig>();
            var cameraPosition = _query.GetSingleton<LocalToWorld>().Position;

            var addRequestList = new NativeList<Entity>(Allocator.TempJob);
            var removeRequestList = new NativeList<Entity>(Allocator.TempJob);

            var streamIn = new StreamSubScenesIn
            {
                AddRequestList = addRequestList,
                CameraPosition = cameraPosition,
                MaxDistanceSquared = streamingLogicConfig.DistanceForStreamingIn *
                                     streamingLogicConfig.DistanceForStreamingIn
            };

            state.Dependency = streamIn.Schedule(state.Dependency);

            var streamOut = new StreamSubScenesOut
            {
                RemoveRequestList = removeRequestList,
                CameraPosition = cameraPosition,
                MaxDistanceSquared = streamingLogicConfig.DistanceForStreamingOut *
                                     streamingLogicConfig.DistanceForStreamingOut,
                PlayerSectionGUID = streamingLogicConfig.PlayerSectionGUID,
                TrafficSectionGUID = streamingLogicConfig.TrafficSectionGUID,
            };
            state.Dependency = streamOut.Schedule(state.Dependency);
            state.Dependency = new BuildCommandBufferJob
            {
                CommandBuffer = entityCommandBufferSystem.CreateCommandBuffer(),
                AddRequestArray = addRequestList.AsDeferredJobArray(),
                RemoveRequestArray = removeRequestList.AsDeferredJobArray()
            }.Schedule(state.Dependency);
            entityCommandBufferSystem.AddJobHandleForProducer(state.Dependency);
            addRequestList.Dispose(state.Dependency);
            removeRequestList.Dispose(state.Dependency);
        }
    }
}
