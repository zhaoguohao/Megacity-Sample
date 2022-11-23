using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.MegaCity.Traffic
{
    /// <summary>
    /// Utility jobs to clear native containers
    /// </summary>
    [BurstCompile]
    public struct ClearArrayJob<T> : IJobParallelFor where T : struct
    {
        [WriteOnly] public NativeArray<T> Data;

        public void Execute(int index)
        {
            Data[index] = default;
        }
    }

    [BurstCompile]
    public struct ClearHashJob<T> : IJob where T : unmanaged
    {
        [WriteOnly] public NativeMultiHashMap<int, T> Hash;

        public void Execute()
        {
            Hash.Clear();
        }
    }

    [BurstCompile]
    public struct DisposeArrayJob<T> : IJob where T : struct
    {
        [WriteOnly] [DeallocateOnJobCompletion]
        public NativeArray<T> Data;

        public void Execute()
        {
        }
    }
}
