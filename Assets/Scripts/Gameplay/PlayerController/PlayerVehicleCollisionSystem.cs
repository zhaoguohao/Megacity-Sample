using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using static  Unity.Entities.SystemAPI;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// This system handles collision events for rigid bodies with the PlayerVehicle custom tag
    /// </summary>
    [BurstCompile]
    struct PlayerVehicleCollisionJob : ICollisionEventsJob
    {
        [ReadOnly] public NativeSlice<RigidBody> Bodies;
        [ReadOnly] public ComponentLookup<PhysicsVelocity> PhysicsVelocities;
        public ComponentLookup<VehicleThrust> VehicleThrusts;

        public void Execute(CollisionEvent collisionEvent)
        {
            TryCutThrottleIfPlayerVehicle(collisionEvent.BodyIndexA, collisionEvent.EntityA, collisionEvent.Normal);
            TryCutThrottleIfPlayerVehicle(collisionEvent.BodyIndexB, collisionEvent.EntityB, collisionEvent.Normal);
        }

        bool TryCutThrottleIfPlayerVehicle(int bodyIndex, Entity entity, float3 normal)
        {
            // custom body tag 0 is for PlayerVehicle
            // vehicles with this tag are expected to have PlayerVehicleState and PhysicsVelocity
            if ((Bodies[bodyIndex].CustomTags & (1 << 0)) == 0)
            {
                // TODO: play a sound if one of the bodies was a player car
                return false;
            }
            var state = VehicleThrusts[entity];
            // cut thrust in proportion to angle angle of attack
            var scalar = 1f - math.abs(math.dot(normal, math.normalizesafe(PhysicsVelocities[entity].Linear)));
            state.Thrust *= scalar;
            VehicleThrusts[entity] = state;
            return true;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    partial struct PlayerVehicleCollisionSystem : ISystem
    {
        private ComponentLookup<PhysicsVelocity> _physicsVelocities;
        private ComponentLookup<VehicleThrust> _vehicleThrust;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<SimulationSingleton>();
            _physicsVelocities = state.GetComponentLookup<PhysicsVelocity>(true);
            _vehicleThrust = state.GetComponentLookup<VehicleThrust>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorldSingleton = GetSingleton<PhysicsWorldSingleton>();
            var simulationSingleton = GetSingleton<SimulationSingleton>();
            _physicsVelocities.Update(ref state);
            _vehicleThrust.Update(ref state);
            state.Dependency = new PlayerVehicleCollisionJob
            {
                Bodies = physicsWorldSingleton.Bodies,
                PhysicsVelocities = _physicsVelocities,
                VehicleThrusts = _vehicleThrust
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }
}
