using Unity.Entities;
using Unity.Physics;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Set of components required for player vehicle movement and control
    /// </summary>
    public struct PlayerVehicleSettings : IComponentData
    {
        public float Acceleration;
        public float Deceleration;
        public float MaxSpeed;
        public float MinSpeed;
        public float DragBreakForce;
        public float PitchForce;
        public float YawKickBack;
        public PhysicsDamping Damping;

        public float RollAutoLevelVelocity;
        public float PitchAutoLevelVelocity;
        public float MaxBankAngle;
        public float MaxYawAngle;
        public float MaxPitchAngle;
        public float BankVolatility;
        public float ManualRollMaxSpeed;
        public float ManualRollAcceleration;
        public float SteeringSpeed;
        public float InvMaxVelocity;

        public bool InvertPitch;

        public float TargetFollowDamping;
        public float TargetSqLerpThreshold;
    }

    public struct PlayerVehicleCameraSettings : IComponentData
    {
        public float FollowCameraZBreakZoom;
        public float FollowCameraZBreakSpeed;
        public float FollowCameraZFollow;
        public float FollowCameraZOffset;
    }

    public struct VehicleThrust : IComponentData
    {
        public float Thrust;
    }

    public struct VehicleRoll : IComponentData
    {
        public float BankAmount;
        public float ManualRollValue;
        public float ManualRollSpeed;
    }

    public struct VehicleBraking : IComponentData
    {
        public float YawBreakRotation;
        public float PitchPseudoBraking;
    }
}
