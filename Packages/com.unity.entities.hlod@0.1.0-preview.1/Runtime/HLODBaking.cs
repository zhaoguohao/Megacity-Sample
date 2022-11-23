using Unity.Collections;
using Unity.Rendering;
using UnityEngine;
using Unity.Entities.Hybrid.Baking;

namespace Unity.Entities.HLOD
{
    [TemporaryBakingType]
    struct HLODBakingData : IComponentData
    {
        public Entity Group;
        public int LODMask;
    }

    [TemporaryBakingType]
    struct HLODSectionOneBakingData : IBufferElementData
    {
        public Entity parentEntity;
    }
    
    [TemporaryBakingType]
    struct HLODSectionZeroBakingData : IBufferElementData
    {
        public Entity parentEntity;
    }

    class HLODBaker : Baker<HLOD>
    {
        public override void Bake(HLOD authoring)
        {
            AddComponent<HLODComponent>(GetEntity());

            if (!authoring.autoLODSections)
                return;

            var bufferZero = AddBuffer<HLODSectionZeroBakingData>();
            var bufferOne = AddBuffer<HLODSectionOneBakingData>();

            var lodCount = authoring.HLODParents.Length;

            // collect all children
            for (int i = 0; i < lodCount; ++i)
            {
                var hlodParent = authoring.HLODParents[i];
                if (hlodParent == null)
                {
                    Debug.LogError("The HLODParent is null. HLOD's HLODParent array is not allowed to have empty entries.");
                    return;
                }

                // The lowest LOD is assigned to Section 0, everything else gets Section 1
                if (i == lodCount - 1)
                {
                    bufferZero.Add(new HLODSectionZeroBakingData() {parentEntity = GetEntity(hlodParent)});

                    foreach (Transform child in GetComponentsInChildren<Transform>(hlodParent))
                        bufferZero.Add(new HLODSectionZeroBakingData() {parentEntity = GetEntity(child)});
                }
                else
                {
                    bufferOne.Add(new HLODSectionOneBakingData() {parentEntity = GetEntity(hlodParent)});

                    foreach (Transform child in GetComponentsInChildren<Transform>(hlodParent))
                        bufferOne.Add(new HLODSectionOneBakingData() {parentEntity = GetEntity(child)});
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class HLODBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities.ForEach((in SceneSection sceneSection,
                in DynamicBuffer<HLODSectionZeroBakingData> zeroBakingBuffer,
                in DynamicBuffer<HLODSectionOneBakingData> oneBakingBuffer) =>
            {
                // Prepare Section Data
                var tempSceneSection = sceneSection;
                tempSceneSection.Section = 0;

                // Update every Entity that needs Section 0 (lowest LOD)
                foreach (var child in zeroBakingBuffer)
                {
                    ecb.SetSharedComponent(child.parentEntity, tempSceneSection);
                    var additionalEntities =
                        EntityManager.GetBuffer<AdditionalEntitiesBakingData>(child.parentEntity, true);

                    foreach (var additionalEntity in additionalEntities)
                        ecb.SetSharedComponent(additionalEntity.Value, tempSceneSection);
                }

                tempSceneSection.Section = 1;

                // Update every Entity that needs Section 1
                foreach (var child in oneBakingBuffer)
                {
                    ecb.SetSharedComponent(child.parentEntity, tempSceneSection);
                    var additionalEntities =
                        EntityManager.GetBuffer<AdditionalEntitiesBakingData>(child.parentEntity, true);

                    foreach (var additionalEntity in additionalEntities)
                        ecb.SetSharedComponent(additionalEntity.Value, tempSceneSection);
                }
            }).WithStructuralChanges().Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }

    class MeshRendererBaker : Baker<MeshRenderer>
    {
        private int FindInLODs(LODGroup lodGroup, Renderer authoring)
        {
            if (lodGroup != null)
            {
                var lodGroupLODs = lodGroup.GetLODs();

                // Find the renderer inside the LODGroup
                for (int i = 0; i < lodGroupLODs.Length; ++i)
                {
                    foreach (var renderer in lodGroupLODs[i].renderers)
                    {
                        if (renderer == authoring)
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        public override void Bake(MeshRenderer authoring)
        {
            var lodGroup = GetComponentInParent<LODGroup>();

            var index = FindInLODs(lodGroup, authoring);
            if (index != -1)
                return;

            var hLod = GetComponentInParent<HLOD>();
            var hLodGroup = GetComponentInParent<HLODParent>();

            if (hLod != null && hLodGroup != null)
            {
                var lodParentIndex = hLod.GetLODParentIndex(hLodGroup);
                if (lodParentIndex == -1)
                    Debug.LogWarning("HLOD does not contain this LOD Group", hLodGroup);

                var bakingData = new HLODBakingData {Group = GetEntity(hLod), LODMask = 1 << lodParentIndex};
                AddComponent(bakingData);
            }
        }
    }


    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class HLODPostProcessSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref MeshLODComponent lodComponent,
                in DynamicBuffer<AdditionalEntitiesBakingData> additionalData, in HLODBakingData bakingData) =>
            {
                lodComponent.Group = bakingData.Group;
                lodComponent.LODMask = bakingData.LODMask;

                foreach (var data in additionalData)
                {
                    EntityManager.AddComponentData(data.Value, lodComponent);
                }
            }).WithoutBurst().WithStructuralChanges().Run();
        }
    }
}
