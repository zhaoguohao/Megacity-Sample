using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Unity.MegaCity.Streaming
{

    /// <summary>
    /// Configures the streaming in/out distances based on player position in the scene
    /// </summary>

    public class StreamingConfigAuthoring : MonoBehaviour
    {
#if UNITY_EDITOR
        public float StreamingInDistance = 600;
        public float StreamingOutDistance = 800;
        public SceneAsset PlayerScene;
        public SceneAsset TrafficScene;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, StreamingInDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, StreamingOutDistance);
        }
#endif
    }

#if UNITY_EDITOR
    [BakingVersion("Abdul", 1)]
    public class StreamingConfigAuthoringBaker : Baker<StreamingConfigAuthoring>
    {
        public override void Bake(StreamingConfigAuthoring authoring)
        {
            var config = new StreamingConfig()
            {
                DistanceForStreamingIn = authoring.StreamingInDistance,
                DistanceForStreamingOut = authoring.StreamingOutDistance,
                PlayerSectionGUID =
                    new Unity.Entities.Hash128(
                        AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(authoring.PlayerScene))),
                TrafficSectionGUID =
                    new Unity.Entities.Hash128(
                        AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(authoring.TrafficScene))),
            };
            AddComponent(config);
        }
    }
#endif
}
