//#define USE_OCCUPANCY_DEBUG
//#define USE_DEBUG_LINES // Enable here and at the top of VehicleMovementJob.cs

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MegaCity.Gameplay;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.Traffic
{
    /// <summary>
    ///     The system collects all entities with VehiclePrefabData attached.
    ///     Also creates a collection with All entities with RoadSection to create the traffic.
    ///     Reference the Player game object and Rigidbody to move the player's vehicle.
    ///     Move all vehicles including player's vehicle to the next position based on the occupation.
    ///     With all this data create 2 HashMaps to manage the occupation and lane slots.
    /// </summary>
    [BurstCompile]
    public partial struct TrafficSystem : ISystem
    {
        private Entity _player;

        private bool doneOneTimeInit;
        private float _TransformRemain;

        private EntityQuery m_CarGroup;
        private EntityQuery m_VehiclePrefabQuery;
        private TrafficSettingsData trafficSettings;

        private float3 m_PlayerPosition;
        private float3 m_PlayerVelocity;

        private NativeMultiHashMap<int, VehicleCell> _Cells;
        private NativeMultiHashMap<int, VehicleSlotData> _VehicleMap;

#if UNITY_EDITOR && USE_DEBUG_LINES
        [Inject] private DebugLineSystem _DebugLines;
#endif
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_CarGroup = state.GetEntityQuery(ComponentType.ReadOnly<VehiclePhysicsState>());
            m_VehiclePrefabQuery = state.GetEntityQuery(ComponentType.ReadOnly<VehiclePrefabData>());

            state.RequireForUpdate<PlayerVehicleInput>();
            state.RequireForUpdate<TrafficSettingsData>();
            state.RequireForUpdate<RoadSectionBlobRef>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (doneOneTimeInit)
            {
#if UNITY_EDITOR && USE_OCCUPANCY_DEBUG
                OccupancyDebug.queueSlots.Dispose();
                OccupancyDebug.roadSections.Dispose();
#endif
            }
            if(_VehicleMap.IsCreated)
                _VehicleMap.Dispose();
            if(_Cells.IsCreated)
                _Cells.Dispose();
        }
#if UNITY_EDITOR && USE_OCCUPANCY_DEBUG
        void OneTimeSetup()
        {
            OccupancyDebug.queueSlots =
 new NativeArray<Occupation>(numSections * Constants.RoadIndexMultiplier, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            OccupancyDebug.roadSections =
 new NativeArray<RoadSection>(numSections, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int a = 0; a < roadSections.Length; a++)
            {
                OccupancyDebug.roadSections[a] = roadSections[a];
            }

            doneOneTimeInit = true;
        }
#endif
        public void OnUpdate(ref SystemState state)
        {
            if (_player == Entity.Null)
            {
                _player = GetSingletonEntity<PlayerVehicleInput>();
                trafficSettings = GetSingleton<TrafficSettingsData>();
                _Cells = new NativeMultiHashMap<int, VehicleCell>(trafficSettings.PoolCellVehicleSize, Allocator.Persistent);
                _VehicleMap = new NativeMultiHashMap<int, VehicleSlotData>(trafficSettings.PoolCellVehicleSize, Allocator.Persistent);
            }
#if UNITY_EDITOR && USE_OCCUPANCY_DEBUG
            if (!doneOneTimeInit)
            {
                OneTimeSetup();
                return;
            }
#endif
            var roadSectionBlobRef = GetSingleton<RoadSectionBlobRef>();
            var numSections = roadSectionBlobRef.Data.Value.RoadSections.Length;

            var spawnEcb = state.World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            var despawnEcb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();

            var vehiclePool = m_VehiclePrefabQuery.ToComponentDataArray<VehiclePrefabData>(Allocator.TempJob);

            if (vehiclePool.Length == 0)
            {
                return;
            }
#if UNITY_EDITOR && USE_DEBUG_LINES
            var debugLines = _DebugLines.Lines.ToConcurrent();
#endif
            var queueSlots = new NativeArray<Occupation>(numSections * Constants.RoadIndexMultiplier, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            // Setup job dependencies
            var clearCombineJobHandle = ClearOccupationAndVehicleMap(queueSlots);
            var occupationFillDeps =
                MoveVehiclesAndSetOccupations(clearCombineJobHandle, queueSlots, roadSectionBlobRef, ref state);
            var occupationGapDeps = FillOccupationGaps(occupationFillDeps, queueSlots, roadSectionBlobRef);

            // Sample occupation ahead of each vehicle and slow down to not run into cars in front
            // Also signal if a lane change is wanted.
            var moderatorDeps = new VehicleSpeedModerate
            {
                Occupancy = queueSlots,
                RoadSectionBlobRef = roadSectionBlobRef,
                DeltaTimeSeconds = state.WorldUnmanaged.Time.DeltaTime
            }.ScheduleParallel(occupationGapDeps);

            // Pick concrete new lanes for cars switching lanes
            var laneSwitchDeps = new LaneSwitch
            {
                Occupancy = queueSlots,
                RoadSectionBlobRef = roadSectionBlobRef
            }.ScheduleParallel(moderatorDeps);

            // Despawn cars that have run out of road
            var despawnDeps = new VehicleDespawnJob
                {
                    EntityCommandBuffer = despawnEcb.CreateCommandBuffer().AsParallelWriter()
                }
                .ScheduleParallel(laneSwitchDeps);
            despawnEcb.AddJobHandleForProducer(despawnDeps);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, despawnDeps);

            var carCount = m_CarGroup.CalculateEntityCount();
            if (carCount < trafficSettings.MaxCars)
            {
                var spawn = new VehicleSpawnJob
                {
                    VehiclePool = vehiclePool,
                    RoadSectionBlobRef = roadSectionBlobRef,
                    Occupation = queueSlots,
                    EntityCommandBuffer = spawnEcb.CreateCommandBuffer().AsParallelWriter()
                }.ScheduleParallel(occupationGapDeps);
                spawnEcb.AddJobHandleForProducer(spawn);

                state.Dependency = JobHandle.CombineDependencies(state.Dependency, spawn);
            }

#if UNITY_EDITOR && USE_OCCUPANCY_DEBUG
            spawnDeps.Complete();
            laneSwitchDeps.Complete();
            for (int a = 0; a < queueSlots.Length; a++)
            {
                OccupancyDebug.queueSlots[a] = queueSlots[a];
            }
#endif
            state.Dependency = MoveVehicles(state.Dependency, ref state);

            // Get rid of occupation data
            state.Dependency = new DisposeArrayJob<Occupation>
            {
                Data = queueSlots
            }.Schedule(JobHandle.CombineDependencies(state.Dependency, laneSwitchDeps));

            state.Dependency = new DisposeArrayJob<VehiclePrefabData>
            {
                Data = vehiclePool
            }.Schedule(JobHandle.CombineDependencies(state.Dependency, laneSwitchDeps));
        }

        private JobHandle ClearOccupationAndVehicleMap(NativeArray<Occupation> queueSlots)
        {
            var clearDeps = new ClearArrayJob<Occupation>
            {
                Data = queueSlots
            }.Schedule(queueSlots.Length, 512);
            var clearHash2Job = new ClearHashJob<VehicleSlotData> { Hash = _VehicleMap }.Schedule();
            return JobHandle.CombineDependencies(clearDeps, clearHash2Job);
        }

        private JobHandle MoveVehiclesAndSetOccupations(JobHandle jobHandle, NativeArray<Occupation> queueSlots,
            RoadSectionBlobRef roadSectionBlobRef, ref SystemState state)
        {
            // Move vehicles along path, compute banking
            var pathingDeps = new VehiclePathUpdate
            {
                RoadSectionBlobRef = roadSectionBlobRef,
                DeltaTimeSeconds = state.WorldUnmanaged.Time.DeltaTime * trafficSettings.GlobalSpeedFactor
            }.ScheduleParallel(jobHandle);
            // Move vehicles that have completed their curve to the next curve (or an off ramp)
            var pathLinkDeps =
                new VehiclePathLinkUpdate
                {
                    RoadSectionBlobRef = roadSectionBlobRef
                }.ScheduleParallel(pathingDeps);
            // Move from lane to lane. PERF: Opportunity to not do for every vehicle.
            var lanePositionDeps = new VehicleLanePosition
                {
                    RoadSectionBlobRef = roadSectionBlobRef,
                    DeltaTimeSeconds = state.WorldUnmanaged.Time.DeltaTime
                }
                .ScheduleParallel(pathLinkDeps);

            var laneCombineJobHandle = JobHandle.CombineDependencies(jobHandle, lanePositionDeps);
            // Compute what cells (of the 16 for each road section) is covered by each vehicle
            var occupationAliasingDeps = new OccupationAliasing
                {
                    OccupancyToVehicleMap = _VehicleMap.AsParallelWriter(),
                    RoadSectionBlobRef = roadSectionBlobRef
                }
                .ScheduleParallel(laneCombineJobHandle);
            return new OccupationFill2
            {
                Occupations = queueSlots,
                _VehicleMap = _VehicleMap
            }.Schedule(_VehicleMap, 32,
                occupationAliasingDeps);
        }

        private JobHandle FillOccupationGaps(JobHandle occupationFillDeps, NativeArray<Occupation> queueSlots,
            RoadSectionBlobRef roadSectionBlobRef)
        {
            // Back-fill the information:
            // |   A      B     |
            // |AAAABBBBBBB     |
            var sections = roadSectionBlobRef.Data.Value.RoadSections.Length;
            var occupationGapDeps =
                new OccupationGapFill
                {
                    Occupations = queueSlots
                }.Schedule(sections, 16, occupationFillDeps);
            occupationGapDeps = new OccupationGapAdjustmentJob
                {
                    Occupations = queueSlots,
                    RoadSectionBlobRef = roadSectionBlobRef
                }
                .Schedule(sections, 32, occupationGapDeps);
            occupationGapDeps =
                new OccupationGapFill2
                {
                    Occupations = queueSlots
                }.Schedule(sections, 16, occupationGapDeps);
            return occupationGapDeps;
        }

        private JobHandle MoveVehicles(JobHandle spawnDeps, ref SystemState state)
        {
            var stepsTaken = 0;
            var timeStep = 1.0f / 60.0f;
            var mainCamera = Camera.main;

            JobHandle finalPosition;
            float3 camPos = default;

            _TransformRemain += state.WorldUnmanaged.Time.DeltaTime;

            GetPlayerPosition(ref state);

            var movementDeps = AssignVehicleToCells(timeStep, spawnDeps, ref stepsTaken, ref state);

            // Modifies Vehicles transform based on cells and vehicle data.
            if (mainCamera != null)
            {
                camPos = mainCamera.transform.position;
            }

            if (stepsTaken > 0)
            {
                finalPosition = new VehicleTransformJob().ScheduleParallel(movementDeps);
            }
            else
            {
                finalPosition = movementDeps;
            }

            return finalPosition;
        }

        private void GetPlayerPosition(ref SystemState state)
        {
            //Assigns the position and velocity based on PlayerVehicleInput entity
            if (_player != Entity.Null)
            {
                m_PlayerPosition = state.EntityManager.GetComponentData<WorldTransform>(_player).Position;
                m_PlayerVelocity = state.EntityManager.GetComponentData<PhysicsVelocity>(_player).Linear;
            }
        }

        private JobHandle AssignVehicleToCells(float timeStep, JobHandle spawnDeps, ref int stepsTaken,
            ref SystemState state)
        {
            var playerPosJobScheduled = false;
            var movementDeps = JobHandle.CombineDependencies(spawnDeps, state.Dependency);
            while (_TransformRemain >= timeStep)
            {
                var clearHashJob = new ClearHashJob<VehicleCell> { Hash = _Cells }.Schedule(movementDeps);
                var hashJob = new VehicleHashJob { CellMap = _Cells.AsParallelWriter() }.ScheduleParallel(clearHashJob);


                if (!playerPosJobScheduled)
                {
                    GetPlayerPosition(ref state);
                    playerPosJobScheduled = true;
                }

                hashJob = new PlayerHashJob
                {
                    CellMap = _Cells,
                    Pos = m_PlayerPosition,
                    Velocity = m_PlayerVelocity
                }.Schedule(hashJob);

                movementDeps = new VehicleMovementJob
                {
                    TimeStep = timeStep,
                    Cells = _Cells,
#if UNITY_EDITOR && USE_DEBUG_LINES
                    DebugLines = debugLines
#endif
                }.ScheduleParallel(hashJob);

                _TransformRemain -= timeStep;
                ++stepsTaken;
            }

            return movementDeps;
        }
    }
}
