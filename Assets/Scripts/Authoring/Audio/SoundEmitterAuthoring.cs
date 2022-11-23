// #define DRAW_AUDIO_GIZMOS // <- get gizmos for changing cone properties, min/max distances in sound emitters (probably only relevant for small scenes).

using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.MegaCity.Audio
{
    /// <summary>
    /// Takes ECSoundEmitterComponent and convert them all to ECSoundEmitter based on ECSoundEmitterDefinitionAsset
    /// Also, it can return the definition asset settings.
    /// </summary>

    [TemporaryBakingType]
    public struct SoundEmitterBakingData : IComponentData
    {
        public UnityObjectRef<SoundEmitterAuthoring> Authoring;
    }

    [BakingVersion("Abdul", 1)]
    public class SoundEmitterBaker: Baker<SoundEmitterAuthoring>
    {
        public override void Bake(SoundEmitterAuthoring authoring)
        {
            AddComponent(new SoundEmitterBakingData { Authoring = authoring });
        }
    }

    public class SoundEmitterAuthoring : MonoBehaviour
    {
        public SoundEmitterDefinitionAsset definition;
        public float volume
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.volume;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.volume = value;
                    definition.Reflect(World.DefaultGameObjectInjectionWorld);
                }
            }
        }

        public float coneAngle
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.coneAngle;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.coneAngle = value;
                    definition.Reflect(World.DefaultGameObjectInjectionWorld);
                }
            }
        }

        public float coneTransition
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.coneTransition;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.coneTransition = value;
                    definition.Reflect(World.DefaultGameObjectInjectionWorld);
                }
            }
        }

        public float minDist
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.minDist;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.minDist = value;
                    definition.Reflect(World.DefaultGameObjectInjectionWorld);
                }
            }
        }

        public float maxDist
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.maxDist;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.maxDist = value;
                    definition.Reflect(World.DefaultGameObjectInjectionWorld);
                }
            }
        }

        public float falloffMode
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.falloffMode;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.falloffMode = value;
                    definition.Reflect(World.DefaultGameObjectInjectionWorld);
                }
            }
        }

        void Start()
        {
        }

#if UNITY_EDITOR && DRAW_AUDIO_GIZMOS
        void DrawGizmos(float alpha)
        {
            if (definition == null)
                return;

            var v = definition.data;

            alpha *= 0.5f;

            Gizmos.color = new Color(0.0f, 1.0f, 0.0f, alpha);
            Gizmos.DrawWireSphere(transform.position, v.minDist);

            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, alpha);
            Gizmos.DrawWireSphere(transform.position, v.maxDist);

            var v1 = transform.right;
            var v2 = transform.up;
            var v3 = transform.forward;

            float angle1Deg = v.coneAngle;
            float halfAngle1Rad = angle1Deg * Mathf.PI / 360.0f;
            float c1 = 0.5f * v.maxDist * Mathf.Cos(halfAngle1Rad);
            float s1 = 0.5f * v.maxDist * Mathf.Sin(halfAngle1Rad);
            var p1a = v1 * c1 + v2 * s1;
            var p1b = v1 * c1 + v3 * s1;

            float angle2Deg = v.coneAngle - v.coneTransition;
            float halfAngle2Rad = angle2Deg * Mathf.PI / 360.0f;
            float c2 = 0.5f * v.maxDist * Mathf.Cos(halfAngle2Rad);
            float s2 = 0.5f * v.maxDist * Mathf.Sin(halfAngle2Rad);
            var p2a = v1 * c2 + v2 * s2;
            var p2b = v1 * c2 + v3 * s2;

            var col1 = new Color(1.0f, 1.0f, 0.0f, 0.1f * alpha);
            var col2 = new Color(0.0f, 1.0f, 1.0f, 0.1f * alpha);

            Handles.color = col1;
            Handles.DrawSolidArc(transform.position, v1, p1a, 360.0f, v.maxDist);

            Handles.color = col2;
            Handles.DrawSolidArc(transform.position, v1, p2a, 360.0f, v.maxDist);

            Handles.color = col1;
            Handles.DrawSolidArc(transform.position, v3, p1a, -angle1Deg, v.maxDist);

            Handles.color = col1;
            Handles.DrawSolidArc(transform.position, v2, p1b, angle1Deg, v.maxDist);

            Handles.color = col2;
            Handles.DrawSolidArc(transform.position, v3, p2a, -angle2Deg, v.maxDist);

            Handles.color = col2;
            Handles.DrawSolidArc(transform.position, v2, p2b, angle2Deg, v.maxDist);
        }

        [DrawGizmo(GizmoType.Pickable)]
        void OnDrawGizmos()
        {
            DrawGizmos(0.5f);
        }

        void OnDrawGizmosSelected()
        {
            DrawGizmos(0.25f);
        }

#endif
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [BakingVersion("Abdul", 1)]
    internal partial class SoundEmitterBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref SoundEmitterBakingData soundEmitterBakingData) =>
            {
                var data = soundEmitterBakingData.Authoring.Value;
                SoundEmitter emitter = new SoundEmitter();

                emitter.position = data.transform.position;
                emitter.coneDirection = -data.transform.right;

                if (data.definition != null)
                {
                    emitter.definitionIndex = data.definition.data.definitionIndex;
                    data.definition.Reflect(World);
                }

                EntityManager.AddComponentData(entity, emitter);

            }).WithStructuralChanges().Run();
        }
    }
}
