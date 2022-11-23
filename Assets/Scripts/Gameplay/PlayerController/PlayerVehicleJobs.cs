using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Set of jobs to control the player car movement and breaking
    /// </summary>
    [BurstCompile]
    internal partial struct VehicleBankingJob : IJobEntity
    {
        public void Execute(
            ref VehicleRoll vehicleRoll,
            in VehicleVolatilityCurves curves,
            in WorldTransform worldTransform,
            in PhysicsVelocity velocity,
            in PlayerVehicleSettings vehicleSettings,
            in PlayerVehicleInput controlInput)
        {
            vehicleRoll.BankAmount = CalculateBanking(ref curves.BankVolatilityCurve.Value, in worldTransform,
                in velocity,
                in vehicleSettings);
            RollVehicle(in vehicleSettings, in controlInput, ref vehicleRoll);
        }

        private float CalculateBanking(
            ref AnimationCurveBlob bankVolatilityCurve,
            in WorldTransform worldTransform,
            in PhysicsVelocity velocity,
            in PlayerVehicleSettings vehicleSettings)
        {
            var vehicleVelocityLocal = math.rotate(worldTransform.ToMatrix(), velocity.Linear);
            var momentumRaw = vehicleVelocityLocal.x * vehicleSettings.InvMaxVelocity * vehicleSettings.BankVolatility;
            var momentum = math.clamp(momentumRaw, -1, 1);
            var bankAmount = vehicleSettings.MaxBankAngle * math.sign(momentum) *
                             bankVolatilityCurve.Evaluate(math.abs(momentum));

            return bankAmount;
        }

        private void RollVehicle(
            in PlayerVehicleSettings vehicleSettings,
            in PlayerVehicleInput input,
            ref VehicleRoll vehicleRoll)
        {
            if (input.RightTrigger2 > 0 || input.LeftTrigger2 > 0)
            {
                if (vehicleRoll.ManualRollValue == 0)
                {
                    vehicleRoll.ManualRollValue = vehicleRoll.BankAmount;
                }

                vehicleRoll.ManualRollSpeed += input.RightTrigger2 > 0
                    ? -vehicleSettings.ManualRollAcceleration
                    : vehicleSettings.ManualRollAcceleration;

                if (math.abs(vehicleRoll.ManualRollSpeed) > vehicleSettings.ManualRollMaxSpeed)
                {
                    vehicleRoll.ManualRollSpeed =
                        vehicleSettings.ManualRollMaxSpeed * math.sign(vehicleRoll.ManualRollSpeed);
                }

                vehicleRoll.ManualRollValue += vehicleRoll.ManualRollSpeed;
                vehicleRoll.ManualRollValue %= 360;
            }
            else if (math.abs(vehicleRoll.ManualRollValue) > 0)
            {
                var bA = (vehicleRoll.BankAmount + 360) % 360;
                var mR = (vehicleRoll.ManualRollValue + 360) % 360;

                var outerRot = bA > mR ? -mR - (360 - bA) : bA + (360 - mR);
                var innerRot = bA - mR;

                var sD = vehicleRoll.ManualRollSpeed * vehicleRoll.ManualRollSpeed /
                         (2 * vehicleSettings.ManualRollAcceleration);

                // stopping distance if going wrong direction
                if (innerRot * vehicleRoll.ManualRollValue < 0)
                {
                    innerRot += math.sign(innerRot) * sD;
                }

                if (outerRot * vehicleRoll.ManualRollValue < 0)
                {
                    outerRot += math.sign(outerRot) * sD;
                }

                // overshoot distance if sD > ((ManualRollSpeed * ManualRollSpeed) / (2 * ManualRollAcceleration) > distanceToTarget)
                if (sD > math.abs(innerRot))
                {
                    innerRot += math.sign(innerRot) * (sD - math.abs(innerRot));
                }

                if (sD > math.abs(outerRot))
                {
                    outerRot += math.sign(outerRot) * (sD - math.abs(outerRot));
                }

                var target = math.abs(outerRot) < math.abs(innerRot)
                    ? bA > mR ? -mR - (360 - bA) : bA + (360 - mR)
                    : bA - mR;

                if (math.abs(target) < math.abs(vehicleRoll.ManualRollSpeed) &&
                    math.abs(vehicleRoll.ManualRollSpeed) <= 1)
                {
                    vehicleRoll.ManualRollValue = 0;
                    vehicleRoll.ManualRollSpeed = 0;
                    return;
                }

                if (vehicleRoll.ManualRollSpeed * vehicleRoll.ManualRollSpeed /
                    (2 * vehicleSettings.ManualRollAcceleration) > math.abs(target)) // s = (v^2-u^2) / 2a
                {
                    if (vehicleRoll.ManualRollSpeed > 0)
                    {
                        vehicleRoll.ManualRollSpeed -= vehicleSettings.ManualRollAcceleration;
                    }
                    else
                    {
                        vehicleRoll.ManualRollSpeed += vehicleSettings.ManualRollAcceleration;
                    }
                }
                else
                {
                    vehicleRoll.ManualRollSpeed += target < 0
                        ? -vehicleSettings.ManualRollAcceleration
                        : vehicleSettings.ManualRollAcceleration;

                    if (math.abs(vehicleRoll.ManualRollSpeed) > vehicleSettings.ManualRollMaxSpeed)
                    {
                        vehicleRoll.ManualRollSpeed =
                            vehicleSettings.ManualRollMaxSpeed * math.sign(vehicleRoll.ManualRollSpeed);
                    }
                }

                vehicleRoll.ManualRollValue += vehicleRoll.ManualRollSpeed;
                vehicleRoll.ManualRollValue %= 360;
            }
        }
    }

    [BurstCompile]
    internal partial struct VehicleBreakingPseudoPhysicsJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in WorldTransform worldTransform,
            in PhysicsVelocity velocity,
            in VehicleVolatilityCurves curves,
            in VehicleThrust vehicleThrust,
            ref PlayerVehicleCameraSettings cameraSettings,
            ref PlayerVehicleSettings vehicleSettings,
            ref VehicleBraking vehicleBraking)
        {
            if (controlInput.LeftTrigger > 0 && vehicleThrust.Thrust >= 0)
            {
                ApplyBreakingPseudoPhysics(
                    in velocity, in worldTransform, in vehicleSettings, DeltaTime, ref cameraSettings,
                    ref curves.PitchVolatilityCurve.Value, ref curves.YawVolatilityCurve.Value,
                    ref vehicleBraking);
            }
            else if (math.abs(vehicleBraking.PitchPseudoBraking) > 0.01f ||
                     math.abs(vehicleBraking.YawBreakRotation) > 0.01f ||
                     math.abs(cameraSettings.FollowCameraZOffset - cameraSettings.FollowCameraZFollow) > 0.01f)
            {
                RevertBreakingPseudoPhysics(in vehicleSettings, DeltaTime, ref cameraSettings, ref vehicleBraking);
            }
        }

        void RevertBreakingPseudoPhysics(
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref PlayerVehicleCameraSettings cameraSettings,
            ref VehicleBraking vehicleBraking)
        {
            var noYaw = vehicleBraking.YawBreakRotation > 180f ? 360f : 0f;
            vehicleBraking.YawBreakRotation =
                math.lerp(vehicleBraking.YawBreakRotation, noYaw, deltaTime * vehicleSettings.YawKickBack);

            vehicleBraking.PitchPseudoBraking =
                math.lerp(vehicleBraking.PitchPseudoBraking, 0f, deltaTime * vehicleSettings.PitchForce);

            if (math.abs(cameraSettings.FollowCameraZOffset - cameraSettings.FollowCameraZFollow) > 0.01f)
            {
                cameraSettings.FollowCameraZOffset = math.lerp(cameraSettings.FollowCameraZOffset,
                    cameraSettings.FollowCameraZFollow, deltaTime * cameraSettings.FollowCameraZBreakSpeed);
            }
        }

        private void ApplyBreakingPseudoPhysics(
            in PhysicsVelocity velocity,
            in WorldTransform worldTransform,
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref PlayerVehicleCameraSettings cameraSettings,
            ref AnimationCurveBlob pitchVolatilityCurve,
            ref AnimationCurveBlob yawVolatilityCurve,
            ref VehicleBraking vehicleBraking)
        {
            var vehicleVelocityLocal = math.rotate(worldTransform.ToMatrix(), velocity.Linear);
            vehicleVelocityLocal.y = 0;

            var localSpeed = math.length(vehicleVelocityLocal);
            var relSpeed = math.clamp(localSpeed * vehicleSettings.InvMaxVelocity, 0, 1);
            var pitchAmount = vehicleSettings.MaxPitchAngle * pitchVolatilityCurve.Evaluate(math.abs(relSpeed));
            var lerpTime = pitchAmount > vehicleBraking.PitchPseudoBraking
                ? deltaTime * vehicleSettings.PitchForce * (localSpeed * vehicleSettings.InvMaxVelocity)
                : deltaTime * vehicleSettings.PitchForce;
            vehicleBraking.PitchPseudoBraking = math.lerp(vehicleBraking.PitchPseudoBraking, pitchAmount, lerpTime);

            if (pitchAmount > vehicleBraking.PitchPseudoBraking)
            {
                cameraSettings.FollowCameraZOffset = math.lerp(cameraSettings.FollowCameraZOffset,
                    cameraSettings.FollowCameraZFollow + cameraSettings.FollowCameraZBreakZoom,
                    localSpeed * vehicleSettings.InvMaxVelocity * deltaTime * cameraSettings.FollowCameraZBreakSpeed);
            }
            else
            {
                cameraSettings.FollowCameraZOffset = math.lerp(cameraSettings.FollowCameraZOffset,
                    cameraSettings.FollowCameraZFollow, deltaTime * cameraSettings.FollowCameraZBreakSpeed);
            }

            var momentum = math.clamp(vehicleVelocityLocal.x * vehicleSettings.InvMaxVelocity, -1, 1);
            var targetYawAmount = vehicleSettings.MaxYawAngle * -math.sign(momentum) *
                                  yawVolatilityCurve.Evaluate(math.abs(momentum));
            vehicleBraking.YawBreakRotation = math.lerp(vehicleBraking.YawBreakRotation, targetYawAmount, deltaTime);
        }
    }

    [BurstCompile]
    internal partial struct ThrustJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            ref VehicleThrust vehicleThrust,
            ref PhysicsDamping damping)
        {
            CalculateThrust(in controlInput, in vehicleSettings, DeltaTime, ref vehicleThrust, ref damping);
            CalculateThrustDepreciation(in controlInput, in vehicleSettings, DeltaTime, ref vehicleThrust);
        }

        private void CalculateThrust(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref VehicleThrust vehicleThrust,
            ref PhysicsDamping damping)
        {
            if (controlInput.RightTrigger > 0)
            {
                vehicleThrust.Thrust += vehicleSettings.Acceleration * deltaTime * controlInput.RightTrigger;
            }

            if (controlInput.LeftTrigger > 0)
            {
                damping.Linear = vehicleSettings.DragBreakForce * vehicleSettings.Damping.Linear;
            }
            else
            {
                damping.Linear = vehicleSettings.Damping.Linear;
            }

            var maxThrust = vehicleSettings.MaxSpeed * controlInput.RightTrigger;
            if (vehicleThrust.Thrust > maxThrust)
            {
                vehicleThrust.Thrust = maxThrust;
            }

            if (vehicleThrust.Thrust < vehicleSettings.MinSpeed)
            {
                vehicleThrust.Thrust = vehicleSettings.MinSpeed;
            }
        }

        private void CalculateThrustDepreciation(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref VehicleThrust vehicleThrust)
        {
            if (controlInput.RightTrigger > 0)
            {
                return;
            }

            if (vehicleThrust.Thrust > 0)
            {
                vehicleThrust.Thrust -= vehicleSettings.Deceleration * deltaTime;
            }

            if (vehicleThrust.Thrust < 0)
            {
                vehicleThrust.Thrust = 0;
            }
        }
    }


    [BurstCompile]
    internal partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            in VehicleThrust vehicleThrust,
            in LocalToWorld localToWorld,
            in PhysicsMass mass,
            ref PhysicsVelocity velocity)
        {
            velocity.Angular += controlInput.ControlDirection * vehicleSettings.SteeringSpeed * DeltaTime *
                                mass.InverseInertia;
            velocity.Linear += localToWorld.Forward * vehicleThrust.Thrust * DeltaTime * mass.InverseMass;
            if (math.lengthsq(velocity.Linear) > vehicleSettings.MaxSpeed * vehicleSettings.MaxSpeed)
            {
                velocity.Linear = math.normalize(velocity.Linear) * vehicleSettings.MaxSpeed;
            }
        }
    }


    [BurstCompile]
    internal partial struct AutoLevelJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            in LocalToWorld localToWorld,
            in PhysicsMass mass,
            ref PhysicsVelocity velocity)
        {
            AutoLevelCar(in controlInput, in vehicleSettings, in localToWorld, in mass, DeltaTime, ref velocity);
        }

        private void AutoLevelCar(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            in LocalToWorld localToWorld,
            in PhysicsMass mass,
            float deltaTime,
            ref PhysicsVelocity velocity)
        {
            if (controlInput.RightTrigger2 > 0 || controlInput.LeftTrigger2 > 0)
            {
                return;
            }

            velocity.Angular += math.forward() * math.dot(localToWorld.Right, math.up()) *
                                -vehicleSettings.RollAutoLevelVelocity * deltaTime * mass.InverseInertia;

            if (controlInput.RightTrigger > 0 ||
                controlInput.LeftTrigger > 0 ||
                controlInput.ControlDirection.x > 0 ||
                controlInput.ControlDirection.y > 0)
            {
                return;
            }

            velocity.Angular += math.right() * math.dot(localToWorld.Forward, math.up()) *
                                vehicleSettings.PitchAutoLevelVelocity * deltaTime * mass.InverseInertia;
        }
    }
}
