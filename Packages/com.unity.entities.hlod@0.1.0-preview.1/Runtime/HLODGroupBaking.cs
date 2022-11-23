using Unity.Rendering;
using UnityEngine;

namespace Unity.Entities.HLOD
{
    public struct HLODComponent : IComponentData
    {
    }

    [TemporaryBakingType]
    public struct HLODParentInfo : IComponentData
    {
        public Entity    ParentGroup;
        public int       ParentMask;
    }

    class HLODGroupBaker : Baker<LODGroup>
    {
        public override void Bake(LODGroup authoring)
        {
            if (authoring.lodCount > 8)
            {
                Debug.LogWarning("LODGroup has more than 8 LOD - Not supported", authoring);
                return;
            }

            // HLOD data
            var hLod = GetComponentInParent<HLOD>();
            var hLodGroup = GetComponentInParent<HLODParent>();
            if (hLod != null && hLodGroup != null)
            {
                // We can't set the ParentGroup in the HLOD entity to itself
                if (hLod.gameObject != authoring.gameObject)
                {
                    var lodParentIndex = hLod.GetLODParentIndex(hLodGroup);
                    if (lodParentIndex == -1)
                        Debug.LogWarning("HLOD does not contain this LOD Group", hLodGroup);

                    var parentInfo = new HLODParentInfo{ParentGroup = GetEntity(hLod), ParentMask = 1 << lodParentIndex};
                    AddComponent(parentInfo);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class HLODPostProcessGroupBakerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref MeshLODGroupComponent lodComponent, in HLODParentInfo parentInfo) =>
            {
                lodComponent.ParentGroup = parentInfo.ParentGroup;
                lodComponent.ParentMask = parentInfo.ParentMask;

            }).WithoutBurst().WithStructuralChanges().Run();
        }
    }
}
