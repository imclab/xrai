// StrokePoint.cs - Data structure for brush stroke points (spec-012)
// GPU-aligned struct for GraphicsBuffer usage

using System.Runtime.InteropServices;
using UnityEngine;

namespace MetavidoVFX.Painting
{
    /// <summary>
    /// Represents a single point in a brush stroke.
    /// Aligned for GPU buffer usage.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StrokePoint
    {
        /// <summary>World position of the stroke point.</summary>
        public Vector3 Position;

        /// <summary>Direction of movement at this point.</summary>
        public Vector3 Direction;

        /// <summary>Brush width at this point.</summary>
        public float Width;

        /// <summary>RGBA color at this point.</summary>
        public Color Color;

        /// <summary>Time when this point was recorded.</summary>
        public float Timestamp;

        /// <summary>Padding for GPU alignment (to 64 bytes).</summary>
        private float _padding1;
        private float _padding2;

        /// <summary>
        /// Calculate the stride of this struct for GPU buffers.
        /// </summary>
        public static int Stride => Marshal.SizeOf<StrokePoint>();

        /// <summary>
        /// Create a stroke point with default values.
        /// </summary>
        public static StrokePoint Default => new StrokePoint
        {
            Position = Vector3.zero,
            Direction = Vector3.forward,
            Width = 0.02f,
            Color = Color.white,
            Timestamp = 0f
        };

        /// <summary>
        /// Create a stroke point at position with color.
        /// </summary>
        public static StrokePoint Create(Vector3 position, Vector3 direction, float width, Color color)
        {
            return new StrokePoint
            {
                Position = position,
                Direction = direction,
                Width = width,
                Color = color,
                Timestamp = Time.time
            };
        }

        /// <summary>
        /// Interpolate between two stroke points.
        /// </summary>
        public static StrokePoint Lerp(StrokePoint a, StrokePoint b, float t)
        {
            return new StrokePoint
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Direction = Vector3.Slerp(a.Direction, b.Direction, t),
                Width = Mathf.Lerp(a.Width, b.Width, t),
                Color = Color.Lerp(a.Color, b.Color, t),
                Timestamp = Mathf.Lerp(a.Timestamp, b.Timestamp, t)
            };
        }
    }
}
