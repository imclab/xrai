using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.BrushPainting
{
    public enum BrushType { Shuriken, VFXGraph, GeometryBased }

    [CreateAssetMenu(fileName = "NewBrush", menuName = "H3M/Painting/Brush Descriptor")]
    public class H3MBrushDescriptor : ScriptableObject
    {
        [Header("Brush Identity")]
        public string brushName = "New Brush";
        public Sprite icon;
        public string category = "General"; // Fire, Plasma, Smoke, Magic, etc.

        [Header("Brush Type")]
        public BrushType brushType = BrushType.Shuriken;

        [Header("Assets")]
        public GameObject shurikenPrefab; // For Shuriken particles
        public VisualEffectAsset vfxAsset; // For VFX Graph

        [Header("Emission Properties")]
        [Range(10, 5000)]
        public float baseSpawnRate = 500f;
        public bool pressureSensitive = true;
        [Range(0.1f, 10f)]
        public float pressureMultiplier = 2f;

        [Header("Visual Properties")]
        public Color tintColor = Color.white;
        [Range(0.1f, 5f)]
        public float sizeScale = 1f;

        [Header("Trail Properties")]
        public bool hasTrail = false;
        public float trailTime = 1f;
    }
}
