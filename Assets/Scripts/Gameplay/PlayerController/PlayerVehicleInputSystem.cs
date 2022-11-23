using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.MegaCity.CameraManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Capture the user input and apply them to a component for later uses
    /// </summary>
    public struct PlayerVehicleInput : IComponentData
    {
        public float3 ControlDirection;
        public float3 GamepadDirection;

        public float RightTrigger; // acceleration
        public float LeftTrigger; // brake
        public float RightTrigger2; // manual roll to right
        public float LeftTrigger2; // manual roll to left
        public float RightStickClick; // recenter camera
    }

    [BurstCompile]
    internal partial struct PlayerVehicleInputJob : IJobEntity
    {
        public PlayerVehicleInput CollectedInput;

        private void Execute(in PlayerVehicleSettings vehicleSettings, ref PlayerVehicleInput inputSentToEntity)
        {
            if (CollectedInput.ControlDirection.x == 0 && math.any(CollectedInput.GamepadDirection))
            {
                CollectedInput.ControlDirection = CollectedInput.GamepadDirection;
                if (vehicleSettings.InvertPitch)
                {
                    CollectedInput.ControlDirection.x = -CollectedInput.ControlDirection.x;
                }
            }

            inputSentToEntity = CollectedInput;
        }
    }

    [BurstCompile]
    public partial struct PlayerVehicleInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (HybridCameraManager.Instance == null)
                return;
            if (HybridCameraManager.Instance.m_CameraTargetMode != HybridCameraManager.CameraTargetMode.FollowPlayer)
                return;
            var input = new PlayerVehicleInput
            {
                RightTrigger = Input.GetAxis("RightTrigger"),
                LeftTrigger = Input.GetAxis("LeftTrigger"),
                RightTrigger2 = Input.GetAxis("RightTrigger2"),
                LeftTrigger2 = Input.GetAxis("LeftTrigger2"),
                RightStickClick = Input.GetAxis("RightStick_Click"),
                ControlDirection = Input.GetMouseButton(1)
                    ? new float3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0)
                    : new float3(-Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), 0),
                GamepadDirection = !Input.GetMouseButton(1)
                    ? new float3(-Input.GetAxis("Vertical_Gamepad"), Input.GetAxis("Horizontal"), 0)
                    : float3.zero
            };
            // Hide and lock cursor when right mouse button pressed
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (Input.GetMouseButtonUp(1))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.H))
            {
                EditorApplication.isPaused = true;
            }
#endif
            var job = new PlayerVehicleInputJob { CollectedInput = input };
            state.Dependency = job.Schedule(state.Dependency);
        }
    }
}
