using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Move the player mesh to allow nice cosmetic effects like rolling and breaking the car
    /// </summary>
    public struct PlayerVehicleCosmeticPhysics : IComponentData
    {
    }

    [WithAll(typeof(PlayerVehicleCosmeticPhysics))]
    internal partial struct VehicleRollJob : IJobEntity
    {
        public VehicleBraking VehicleBraking;
        public VehicleRoll VehicleRoll;

        public void Execute(ref TransformAspect transformAspect)
        {
            var roll = VehicleRoll.ManualRollValue != 0 ? VehicleRoll.ManualRollValue : VehicleRoll.BankAmount;
            var eulerZXY = math.radians(new float3(VehicleBraking.PitchPseudoBraking, VehicleBraking.YawBreakRotation, roll));
            transformAspect.LocalRotation = quaternion.EulerZXY(eulerZXY);
        }
    }

    [BurstCompile]
    [UpdateBefore(typeof(PlayerVehicleControlSystem))]
    public partial struct PlayerVehicleCosmeticPhysicsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VehicleBraking>();
            state.RequireForUpdate<VehicleRoll>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var vehicleBraking = GetSingleton<VehicleBraking>();
            var vehicleRoll = GetSingleton<VehicleRoll>();
            var rollJob = new VehicleRollJob
            {
                VehicleBraking = vehicleBraking,
                VehicleRoll = vehicleRoll
            };
            state.Dependency = rollJob.ScheduleParallel(state.Dependency);
        }
    }
}
