using System;
using UnityEngine;

namespace Client.Terrain
{
    [Serializable]
    public struct SerializableRect
    {
        [SerializeField] private float x;
        [SerializeField] private float y;
        [SerializeField] private float width;
        [SerializeField] private float height;

        public SerializableRect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float X
        {
            readonly get => x;
            set => x = value;
        }

        public float Y
        {
            readonly get => y;
            set => y = value;
        }

        public float Width
        {
            readonly get => width;
            set => width = value;
        }

        public float Height
        {
            readonly get => height;
            set => height = value;
        }

        public readonly Rect ToRect()
        {
            return new Rect(x, y, width, height);
        }

        public static SerializableRect FromRect(Rect rect)
        {
            return new SerializableRect(rect.x, rect.y, rect.width, rect.height);
        }
    }
}
