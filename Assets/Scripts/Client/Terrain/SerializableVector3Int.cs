using System;
using UnityEngine;

namespace Client.Terrain
{
    [Serializable]
    public struct SerializableVector3Int
    {
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private int z;

        public SerializableVector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SerializableVector3Int(Vector3Int value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        public int X
        {
            readonly get => x;
            set => x = value;
        }

        public int Y
        {
            readonly get => y;
            set => y = value;
        }

        public int Z
        {
            readonly get => z;
            set => z = value;
        }

        public readonly Vector3Int ToVector3Int()
        {
            return new Vector3Int(x, y, z);
        }

        public static SerializableVector3Int FromVector3Int(Vector3Int value)
        {
            return new SerializableVector3Int(value);
        }
    }
}
