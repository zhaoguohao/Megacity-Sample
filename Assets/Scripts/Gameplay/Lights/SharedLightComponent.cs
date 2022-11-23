using System;
using Unity.Entities;
using UnityEngine;

namespace Unity.MegaCity.Lights
{
    /// <summary>
    /// Defines the ShareLight which is a ISharedComponentData
    /// This is used in the LightPoolSystem
    /// </summary>
    [Serializable]
    public struct SharedLight : ISharedComponentData, IEquatable<SharedLight>
    {
        public GameObject Value;

        public bool Equals(SharedLight other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return object.ReferenceEquals(Value, null) ? 0 : Value.GetHashCode();
        }
    }
}
