// XRRAIScene.cs - Data model for XRRAI scene format v1.0
// Part of Spec 016: XRRAI Scene Format & Cross-Platform Export
//
// Defines serializable data structures for scenes containing
// strokes, holograms, VFX instances, AR anchors, and imported assets.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRRAI.Scene
{
    /// <summary>
    /// Root container for XRRAI scene format v1.0
    /// </summary>
    [Serializable]
    public class XRRAIScene
    {
        public string xrrai = "1.0";
        public string generator = "MetavidoVFX/XRRAI";
        public string created;
        public string modified;

        public SceneMetadata scene = new();
        public List<SceneNode> nodes = new();
        public List<BrushDefinition> brushes = new();
        public List<XRRAIStrokeData> strokes = new();
        public List<HologramData> holograms = new();
        public List<VFXInstanceData> vfxInstances = new();
        public List<AnchorData> anchors = new();
        public List<LayerData> layers = new();
        public AssetReferences assets = new();
        public Dictionary<string, object> extensions = new();

        public XRRAIScene()
        {
            created = DateTime.UtcNow.ToString("o");
            modified = created;
        }

        public void MarkModified()
        {
            modified = DateTime.UtcNow.ToString("o");
        }
    }

    /// <summary>
    /// Scene metadata (name, description, bounds)
    /// </summary>
    [Serializable]
    public class SceneMetadata
    {
        public string name = "Untitled Scene";
        public string description = "";
        public List<string> tags = new();
        public BoundsData bounds = new();
        public string upAxis = "Y";
        public string units = "meters";
    }

    [Serializable]
    public class BoundsData
    {
        public float[] min = { -10f, -2f, -10f };
        public float[] max = { 10f, 5f, 10f };

        public static BoundsData FromUnityBounds(Bounds b)
        {
            return new BoundsData
            {
                min = new[] { b.min.x, b.min.y, b.min.z },
                max = new[] { b.max.x, b.max.y, b.max.z }
            };
        }

        public Bounds ToUnityBounds()
        {
            var center = new Vector3(
                (min[0] + max[0]) / 2f,
                (min[1] + max[1]) / 2f,
                (min[2] + max[2]) / 2f);
            var size = new Vector3(
                max[0] - min[0],
                max[1] - min[1],
                max[2] - min[2]);
            return new Bounds(center, size);
        }
    }

    /// <summary>
    /// Scene hierarchy node
    /// </summary>
    [Serializable]
    public class SceneNode
    {
        public string id;
        public string name;
        public string parentId;
        public List<string> children = new();
        public TransformData transform = new();

        public static string GenerateId() => $"node_{Guid.NewGuid():N}".Substring(0, 16);
    }

    [Serializable]
    public class TransformData
    {
        public float[] position = { 0, 0, 0 };
        public float[] rotation = { 0, 0, 0, 1 }; // Quaternion xyzw
        public float[] scale = { 1, 1, 1 };

        public static TransformData FromTransform(Transform t)
        {
            return new TransformData
            {
                position = new[] { t.localPosition.x, t.localPosition.y, t.localPosition.z },
                rotation = new[] { t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w },
                scale = new[] { t.localScale.x, t.localScale.y, t.localScale.z }
            };
        }

        public void ApplyToTransform(Transform t)
        {
            t.localPosition = new Vector3(position[0], position[1], position[2]);
            t.localRotation = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
            t.localScale = new Vector3(scale[0], scale[1], scale[2]);
        }
    }

    /// <summary>
    /// Brush definition (material, geometry, audio reactive settings)
    /// </summary>
    [Serializable]
    public class BrushDefinition
    {
        public string id;
        public string name;
        public string guid; // For .tilt compatibility
        public string material;
        public string geometry = "flat"; // flat, tube, particles
        public AudioReactiveSettings audioReactive;

        public static string GenerateId() => $"brush_{Guid.NewGuid():N}".Substring(0, 16);
    }

    [Serializable]
    public class AudioReactiveSettings
    {
        public bool enabled;
        public float sizeMultiplier = 0.5f;
        public float colorHueShift = 0f;
        public float emissionMultiplier = 1f;
        public int frequencyBand = 1; // 0-7 for 8-band
    }

    /// <summary>
    /// Brush stroke data (points, color, size)
    /// </summary>
    [Serializable]
    public class XRRAIStrokeData
    {
        public string id;
        public string brushId;
        public string nodeId;
        public float[] color = { 1, 1, 1, 1 }; // RGBA
        public float size = 0.02f;
        public string layerId;
        public string mirrorGroupId;
        public List<StrokePoint> points = new();

        // Binary reference for large scenes (optional)
        public string pointsRef;

        public static string GenerateId() => $"stroke_{Guid.NewGuid():N}"[..16];

        public Color GetUnityColor()
        {
            return new Color(color[0], color[1], color[2], color[3]);
        }

        public void SetColor(Color c)
        {
            color = new[] { c.r, c.g, c.b, c.a };
        }
    }

    [Serializable]
    public class StrokePoint
    {
        public float[] p; // Position xyz
        public float[] r; // Rotation quaternion xyzw
        public float s;   // Pressure/size
        public int t;     // Timestamp ms

        public StrokePoint() { }

        public StrokePoint(Vector3 pos, Quaternion rot, float pressure, int timestamp)
        {
            p = new[] { pos.x, pos.y, pos.z };
            r = new[] { rot.x, rot.y, rot.z, rot.w };
            s = pressure;
            t = timestamp;
        }

        public Vector3 Position => new(p[0], p[1], p[2]);
        public Quaternion Rotation => new(r[0], r[1], r[2], r[3]);
        public float Pressure => s;
        public int Timestamp => t;
    }

    /// <summary>
    /// Hologram configuration
    /// </summary>
    [Serializable]
    public class HologramData
    {
        public string id;
        public string type = "live"; // live, recorded, remote
        public string nodeId;
        public HologramSource source;
        public string quality = "medium"; // low, medium, high, ultra
        public string anchorId;
        public string recordingPath; // For recorded type

        public static string GenerateId() => $"holo_{Guid.NewGuid():N}".Substring(0, 16);
    }

    [Serializable]
    public class HologramSource
    {
        public string type = "ARDepthSource";
        public string colorFormat = "R8G8B8A8";
        public string depthFormat = "R16";
    }

    /// <summary>
    /// VFX Graph instance
    /// </summary>
    [Serializable]
    public class VFXInstanceData
    {
        public string id;
        public string assetPath;
        public string nodeId;
        public Dictionary<string, object> parameters = new();
        public Dictionary<string, string> bindings = new();

        public static string GenerateId() => $"vfx_{Guid.NewGuid():N}".Substring(0, 16);
    }

    /// <summary>
    /// AR anchor
    /// </summary>
    [Serializable]
    public class AnchorData
    {
        public string id;
        public string type = "plane"; // plane, image, object, face
        public string classification = "unknown"; // floor, wall, ceiling, table, seat
        public float[] position = { 0, 0, 0 };
        public float[] rotation = { 0, 0, 0, 1 };
        public float[] size = { 1, 1 };
        public float confidence = 1f;
        public bool persistent = true;
        public string nativeId;

        public static string GenerateId() => $"anchor_{Guid.NewGuid():N}".Substring(0, 16);

        public Vector3 GetPosition() => new(position[0], position[1], position[2]);
        public Quaternion GetRotation() => new(rotation[0], rotation[1], rotation[2], rotation[3]);
    }

    /// <summary>
    /// Layer for organization
    /// </summary>
    [Serializable]
    public class LayerData
    {
        public string id;
        public string name = "Layer 1";
        public bool visible = true;
        public bool locked;

        public static string GenerateId() => $"layer_{Guid.NewGuid():N}".Substring(0, 16);
    }

    /// <summary>
    /// External asset references
    /// </summary>
    [Serializable]
    public class AssetReferences
    {
        public List<ModelAsset> models = new();
        public List<TextureAsset> textures = new();
        public List<AudioAsset> audio = new();
    }

    [Serializable]
    public class ModelAsset
    {
        public string id;
        public string path;
        public string source; // icosa, sketchfab, local
        public string sourceId;
        public string license;

        public static string GenerateId() => $"model_{Guid.NewGuid():N}".Substring(0, 16);
    }

    [Serializable]
    public class TextureAsset
    {
        public string id;
        public string path;

        public static string GenerateId() => $"tex_{Guid.NewGuid():N}".Substring(0, 16);
    }

    [Serializable]
    public class AudioAsset
    {
        public string id;
        public string path;
        public float duration;

        public static string GenerateId() => $"audio_{Guid.NewGuid():N}".Substring(0, 16);
    }
}
