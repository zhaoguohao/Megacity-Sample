using Unity.Mathematics;
using UnityEngine;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Create camera target authoring component in order to
    /// allow the game object camera to follow the player camera target entity
    /// </summary>
    public class HybridCameraManager : MonoBehaviour
    {
        public enum CameraTargetMode
        {
            None,
            FollowPlayer,
            DollyTrack
        }

        public CameraTargetMode m_CameraTargetMode;
        public float m_TargetFollowDamping = 5.0f;
        public Transform m_PlayerCameraTarget;
        public Transform m_DollyCameraTarget;
        public static HybridCameraManager Instance;

        private void Awake()
        {
            if (Instance != null)
                Destroy(Instance);
            else
                Instance = this;
        }

        public void SetPlayerCameraPosition(float3 position, float deltaTime)
        {
            m_PlayerCameraTarget.position =
                math.lerp(m_PlayerCameraTarget.position, position, deltaTime * m_TargetFollowDamping);
        }

        public void SetPlayerCameraRotation(quaternion rotation, float deltaTime)
        {
            m_PlayerCameraTarget.rotation =
                math.slerp(m_PlayerCameraTarget.rotation, rotation, deltaTime * m_TargetFollowDamping);
        }

        public float3 GetDollyCameraPosition()
        {
            return m_DollyCameraTarget.position;
        }

        public quaternion GetDollyCameraRotation()
        {
            return m_DollyCameraTarget.rotation;
        }
    }
}
