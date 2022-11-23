using Unity.Collections;
using Unity.Entities;

namespace Unity.MegaCity.Audio
{
    /// <summary>
    /// The system gets all ECSoundEmitterComponent which belongs to the buildings.
    /// Then builds a hashmap based on the [definitionIndex] as a key and insert a new SingleEmitterData as a value.
    /// By using the hashmap build an AudioBlobRef data and add that to the Scene.
    /// Each Scene has a AudioBlobRef with a map of every single emitter.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [UpdateAfter(typeof(SoundEmitterBakingSystem))]
    [BakingVersion("Abdul", 1)]
    public partial class SoundEmitterBlobBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            var query = GetEntityQuery(typeof(SoundEmitterBakingData));
            var entityCount = query.CalculateEntityCount();
            var hashMap = new NativeMultiHashMap<int, SingleEmitterData>(entityCount, Allocator.Temp);
            var highestIndex = 0;

            var rootSceneEntity = Entity.Null;
            Entities.ForEach((Entity entity, in SoundEmitterBakingData soundEmitterBakingData) =>
            {
                var data = soundEmitterBakingData.Authoring.Value;
                if (rootSceneEntity == Entity.Null)
                {
                    //The first entity of the scene is used to store the AudioBlobRef [Blob Asset]
                    //This is necessary per scene to keep one asset per scene since each scene is streamed (load/unload)
                    rootSceneEntity = entity;
                }
                var definitionIndex = data.definition == null ? 0 : data.definition.data.definitionIndex;
                var emitterData = new SingleEmitterData
                {
                    Position = data.transform.position,
                    Direction = data.transform.right,
                };

                hashMap.Add(definitionIndex, emitterData);
                if (definitionIndex > highestIndex) highestIndex = definitionIndex;
            }).WithoutBurst().Run();

            // Don't create audio blob assets for sub-scenes with no ECSoundEmitterComponent
            if (rootSceneEntity == Entity.Null)
                return;

            ref var allEmitterData = ref blobBuilder.ConstructRoot<AllEmitterData>();
            var defInxBegBuilder = blobBuilder.Allocate(ref allEmitterData.DefIndexBeg, highestIndex + 1);
            var defInxEndBuilder = blobBuilder.Allocate(ref allEmitterData.DefIndexEnd, highestIndex + 1);
            var emitterBuilder = blobBuilder.Allocate(ref allEmitterData.Emitters, entityCount);

            int emitterIndex = 0;
            for (int definitionIndex = 0; definitionIndex < highestIndex; definitionIndex++)
            {
                defInxBegBuilder[definitionIndex] = emitterIndex;
                var enumerator = hashMap.GetValuesForKey(definitionIndex);
                while (enumerator.MoveNext())
                {
                    emitterBuilder[emitterIndex] = enumerator.Current;
                    emitterIndex++;
                }
                defInxEndBuilder[definitionIndex] = emitterIndex;
            }

            var blobRef = blobBuilder.CreateBlobAssetReference<AllEmitterData>(Allocator.Persistent);

            EntityManager.AddComponentData(rootSceneEntity, new AudioBlobRef
                {
                    Data = blobRef
                });
        }
    }
}
