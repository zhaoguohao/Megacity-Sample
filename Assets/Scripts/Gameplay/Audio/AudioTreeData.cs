using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.MegaCity.Audio
{
    /// <summary>
    /// Audio system settings component authored based on the constant Audio Master class settings.
    /// </summary>
    public struct AudioSystemSettings : IComponentData
    {
        public bool DebugMode;
        public float MaxDistance;
        public float MaxSqDistance;
        public int MaxVehicles;
        public int ClosestEmitterPerClipCount;
    }
    public struct TreeDataCollected : ICleanupComponentData
    {
    }
    /// <summary>
    /// Initialize and Dispose the KDTree Data for SoundPoolSystem
    /// </summary>
    public struct AudioTree : IDisposable
    {
        public KDTree Tree;
        public NativeArray<KDTree.Neighbour> Results;
        public NativeReference<int> ResultsCount;
        public NativeList<float3> EmittersPosition;
        public NativeList<int> DefinitionIndices;
        public Color DebugLineColor;

        public void Initialize(int maxResults)
        {
            DebugLineColor = UnityEngine.Random.ColorHSV();
            EmittersPosition = new NativeList<float3>(Allocator.Persistent);
            DefinitionIndices = new NativeList<int>(Allocator.Persistent);
            Results = new NativeArray<KDTree.Neighbour>(maxResults, Allocator.Persistent);
            ResultsCount = new NativeReference<int>(Allocator.Persistent);
        }

        public void Dispose()
        {
            if(EmittersPosition.IsCreated)
                EmittersPosition.Dispose();

            if(Results.IsCreated)
                Results.Dispose();

            if(ResultsCount.IsCreated)
                ResultsCount.Dispose();

            if(DefinitionIndices.IsCreated)
                DefinitionIndices.Dispose();

            if(Tree.IsCreated)
                Tree.Dispose();
        }
    }

    /// <summary>
    /// Gets the nearest neighbours based on a position and range from the incoming tree
    /// </summary>
    [BurstCompile]
    public struct GetEntriesInRangeJob : IJob
    {
        [ReadOnly]
        public KDTree Tree;
        public float3 QueryPosition;
        public float Range;

        public NativeReference<int> ResultsCount;
        [NativeSetThreadIndex] private int ThreadIndex;

        // output
        public NativeArray<KDTree.Neighbour> Neighbours;

        public void Execute()
        {
            ResultsCount.Value = Tree.GetEntriesInRange(QueryPosition, Range, ref Neighbours, ThreadIndex);
        }
    }

    /// <summary>
    /// Adds new Entries to the KDTree, based on the BlobAsset (Building)
    /// </summary>
    [BurstCompile]
    struct CopyDataToTree : IJob
    {
        [ReadOnly] public NativeArray<AudioBlobRef> blobs;
        public KDTree tree;
        public int defIdx;

        public void Execute()
        {
            int treeIdx = 0;

            for (int blobIdx = 0; blobIdx < blobs.Length; blobIdx++)
            {
                ref var indexBeg = ref blobs[blobIdx].Data.Value.DefIndexBeg;

                if (defIdx < indexBeg.Length)
                {
                    ref var indexEnd = ref blobs[blobIdx].Data.Value.DefIndexEnd;
                    ref var emitters = ref blobs[blobIdx].Data.Value.Emitters;

                    var beg = indexBeg[defIdx];
                    var end = indexEnd[defIdx];

                    for (int emitterIdx = beg; emitterIdx < end; emitterIdx++)
                    {
                        tree.AddEntry(treeIdx, emitters[emitterIdx].Position);
                        treeIdx += 1;
                    }
                }
            }
        }
    }
}
