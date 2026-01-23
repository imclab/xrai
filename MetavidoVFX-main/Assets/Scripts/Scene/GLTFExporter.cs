// GLTFExporter.cs - Runtime glTF/GLB export for XRRAI scenes
// Part of Spec 016: XRRAI Scene Format & Cross-Platform Export
//
// Exports brush strokes as triangle meshes to glTF 2.0 format.
// Supports both .gltf (JSON + binary) and .glb (single binary) output.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XRRAI.BrushPainting;

namespace XRRAI.Scene
{
    /// <summary>
    /// Runtime glTF/GLB exporter for XRRAI scenes.
    /// Converts brush strokes to triangle meshes with PBR materials.
    /// </summary>
    public class GLTFExporter : MonoBehaviour
    {
        [Header("Export Settings")]
        [SerializeField] bool _embedTextures = true;
        [SerializeField] bool _includeNormals = true;
        [SerializeField] bool _includeColors = true;

        // glTF data structures
        class GLTFRoot
        {
            public GLTFAsset asset = new();
            public List<GLTFScene> scenes = new();
            public List<GLTFNode> nodes = new();
            public List<GLTFMesh> meshes = new();
            public List<GLTFMaterial> materials = new();
            public List<GLTFAccessor> accessors = new();
            public List<GLTFBufferView> bufferViews = new();
            public List<GLTFBuffer> buffers = new();
            public int scene = 0;
        }

        class GLTFAsset
        {
            public string version = "2.0";
            public string generator = "XRRAI/MetavidoVFX";
        }

        class GLTFScene
        {
            public string name;
            public List<int> nodes = new();
        }

        class GLTFNode
        {
            public string name;
            public int? mesh;
            public float[] translation;
            public float[] rotation;
            public float[] scale;
            public List<int> children;
        }

        class GLTFMesh
        {
            public string name;
            public List<GLTFPrimitive> primitives = new();
        }

        class GLTFPrimitive
        {
            public Dictionary<string, int> attributes = new();
            public int? indices;
            public int? material;
            public int mode = 4; // TRIANGLES
        }

        class GLTFMaterial
        {
            public string name;
            public GLTFPBRMetallicRoughness pbrMetallicRoughness;
            public bool doubleSided = true;
            public string alphaMode = "OPAQUE";
        }

        class GLTFPBRMetallicRoughness
        {
            public float[] baseColorFactor;
            public float metallicFactor = 0f;
            public float roughnessFactor = 0.5f;
        }

        class GLTFAccessor
        {
            public int bufferView;
            public int byteOffset;
            public int componentType; // 5126 = FLOAT, 5123 = UNSIGNED_SHORT
            public int count;
            public string type; // VEC3, VEC4, SCALAR
            public float[] min;
            public float[] max;
        }

        class GLTFBufferView
        {
            public int buffer;
            public int byteOffset;
            public int byteLength;
            public int? target; // 34962 = ARRAY_BUFFER, 34963 = ELEMENT_ARRAY_BUFFER
        }

        class GLTFBuffer
        {
            public int byteLength;
            public string uri; // Only for .gltf, not .glb
        }

        /// <summary>
        /// Export XRRAI scene to glTF/GLB file
        /// </summary>
        public async Task<bool> ExportAsync(XRRAIScene scene, string filepath)
        {
            try
            {
                var gltf = new GLTFRoot();
                var binaryData = new List<byte>();

                // Setup scene
                gltf.scenes.Add(new GLTFScene { name = scene.scene.name });

                // Find brush manager to get actual meshes
                var brushManager = BrushManager.Instance;
                if (brushManager == null)
                {
                    Debug.LogWarning("[GLTFExporter] BrushManager not found, exporting from data only");
                }

                // Export strokes as meshes
                int nodeIndex = 0;
                foreach (var stroke in brushManager?.Strokes ?? Array.Empty<BrushStroke>())
                {
                    if (stroke == null || !stroke.IsFinalized) continue;

                    var mesh = stroke.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null || mesh.vertexCount == 0) continue;

                    // Create material
                    int materialIndex = gltf.materials.Count;
                    var color = stroke.Color;
                    gltf.materials.Add(new GLTFMaterial
                    {
                        name = $"Material_{materialIndex}",
                        pbrMetallicRoughness = new GLTFPBRMetallicRoughness
                        {
                            baseColorFactor = new[] { color.r, color.g, color.b, color.a },
                            metallicFactor = 0f,
                            roughnessFactor = 0.5f
                        },
                        alphaMode = color.a < 1f ? "BLEND" : "OPAQUE"
                    });

                    // Export mesh
                    int meshIndex = ExportMesh(gltf, binaryData, mesh, $"Stroke_{nodeIndex}", materialIndex);

                    // Create node
                    var t = stroke.transform;
                    gltf.nodes.Add(new GLTFNode
                    {
                        name = $"Stroke_{nodeIndex}",
                        mesh = meshIndex,
                        translation = new[] { t.position.x, t.position.y, t.position.z },
                        rotation = new[] { t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w },
                        scale = new[] { t.localScale.x, t.localScale.y, t.localScale.z }
                    });

                    gltf.scenes[0].nodes.Add(nodeIndex);
                    nodeIndex++;
                }

                if (gltf.nodes.Count == 0)
                {
                    Debug.LogWarning("[GLTFExporter] No meshes to export");
                    return false;
                }

                // Finalize buffer
                gltf.buffers.Add(new GLTFBuffer { byteLength = binaryData.Count });

                // Write file
                bool isGLB = filepath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase);
                await Task.Run(() =>
                {
                    if (isGLB)
                        WriteGLB(filepath, gltf, binaryData.ToArray());
                    else
                        WriteGLTF(filepath, gltf, binaryData.ToArray());
                });

                Debug.Log($"[GLTFExporter] Exported {gltf.nodes.Count} meshes to {filepath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GLTFExporter] Export failed: {ex}");
                return false;
            }
        }

        int ExportMesh(GLTFRoot gltf, List<byte> binaryData, Mesh mesh, string name, int materialIndex)
        {
            var primitive = new GLTFPrimitive { material = materialIndex };

            // Positions
            var positions = mesh.vertices;
            Vector3 posMin = positions[0], posMax = positions[0];
            foreach (var p in positions)
            {
                posMin = Vector3.Min(posMin, p);
                posMax = Vector3.Max(posMax, p);
            }

            int posAccessor = AddAccessor(gltf, binaryData, positions, posMin, posMax);
            primitive.attributes["POSITION"] = posAccessor;

            // Normals
            if (_includeNormals && mesh.normals.Length > 0)
            {
                int normAccessor = AddAccessor(gltf, binaryData, mesh.normals);
                primitive.attributes["NORMAL"] = normAccessor;
            }

            // Colors
            if (_includeColors && mesh.colors.Length > 0)
            {
                int colorAccessor = AddAccessor(gltf, binaryData, mesh.colors);
                primitive.attributes["COLOR_0"] = colorAccessor;
            }

            // Indices
            var indices = mesh.triangles;
            int indexAccessor = AddIndexAccessor(gltf, binaryData, indices);
            primitive.indices = indexAccessor;

            // Create mesh
            int meshIndex = gltf.meshes.Count;
            gltf.meshes.Add(new GLTFMesh
            {
                name = name,
                primitives = new List<GLTFPrimitive> { primitive }
            });

            return meshIndex;
        }

        int AddAccessor(GLTFRoot gltf, List<byte> binaryData, Vector3[] data, Vector3? min = null, Vector3? max = null)
        {
            int byteOffset = binaryData.Count;

            foreach (var v in data)
            {
                binaryData.AddRange(BitConverter.GetBytes(v.x));
                binaryData.AddRange(BitConverter.GetBytes(v.y));
                binaryData.AddRange(BitConverter.GetBytes(v.z));
            }

            // Pad to 4-byte alignment
            while (binaryData.Count % 4 != 0)
                binaryData.Add(0);

            int bufferViewIndex = gltf.bufferViews.Count;
            gltf.bufferViews.Add(new GLTFBufferView
            {
                buffer = 0,
                byteOffset = byteOffset,
                byteLength = data.Length * 12,
                target = 34962 // ARRAY_BUFFER
            });

            int accessorIndex = gltf.accessors.Count;
            var accessor = new GLTFAccessor
            {
                bufferView = bufferViewIndex,
                byteOffset = 0,
                componentType = 5126, // FLOAT
                count = data.Length,
                type = "VEC3"
            };

            if (min.HasValue && max.HasValue)
            {
                accessor.min = new[] { min.Value.x, min.Value.y, min.Value.z };
                accessor.max = new[] { max.Value.x, max.Value.y, max.Value.z };
            }

            gltf.accessors.Add(accessor);
            return accessorIndex;
        }

        int AddAccessor(GLTFRoot gltf, List<byte> binaryData, Color[] data)
        {
            int byteOffset = binaryData.Count;

            foreach (var c in data)
            {
                binaryData.AddRange(BitConverter.GetBytes(c.r));
                binaryData.AddRange(BitConverter.GetBytes(c.g));
                binaryData.AddRange(BitConverter.GetBytes(c.b));
                binaryData.AddRange(BitConverter.GetBytes(c.a));
            }

            // Pad to 4-byte alignment
            while (binaryData.Count % 4 != 0)
                binaryData.Add(0);

            int bufferViewIndex = gltf.bufferViews.Count;
            gltf.bufferViews.Add(new GLTFBufferView
            {
                buffer = 0,
                byteOffset = byteOffset,
                byteLength = data.Length * 16,
                target = 34962
            });

            int accessorIndex = gltf.accessors.Count;
            gltf.accessors.Add(new GLTFAccessor
            {
                bufferView = bufferViewIndex,
                byteOffset = 0,
                componentType = 5126, // FLOAT
                count = data.Length,
                type = "VEC4"
            });

            return accessorIndex;
        }

        int AddIndexAccessor(GLTFRoot gltf, List<byte> binaryData, int[] indices)
        {
            int byteOffset = binaryData.Count;
            bool useShort = indices.Length < 65536;

            if (useShort)
            {
                foreach (var i in indices)
                    binaryData.AddRange(BitConverter.GetBytes((ushort)i));
            }
            else
            {
                foreach (var i in indices)
                    binaryData.AddRange(BitConverter.GetBytes((uint)i));
            }

            // Pad to 4-byte alignment
            while (binaryData.Count % 4 != 0)
                binaryData.Add(0);

            int bufferViewIndex = gltf.bufferViews.Count;
            gltf.bufferViews.Add(new GLTFBufferView
            {
                buffer = 0,
                byteOffset = byteOffset,
                byteLength = indices.Length * (useShort ? 2 : 4),
                target = 34963 // ELEMENT_ARRAY_BUFFER
            });

            int accessorIndex = gltf.accessors.Count;
            gltf.accessors.Add(new GLTFAccessor
            {
                bufferView = bufferViewIndex,
                byteOffset = 0,
                componentType = useShort ? 5123 : 5125, // UNSIGNED_SHORT or UNSIGNED_INT
                count = indices.Length,
                type = "SCALAR"
            });

            return accessorIndex;
        }

        void WriteGLTF(string filepath, GLTFRoot gltf, byte[] binaryData)
        {
            // Write binary buffer
            string binPath = Path.ChangeExtension(filepath, ".bin");
            File.WriteAllBytes(binPath, binaryData);
            gltf.buffers[0].uri = Path.GetFileName(binPath);

            // Write JSON
            string json = JsonUtility.ToJson(gltf, true);
            File.WriteAllText(filepath, json);
        }

        void WriteGLB(string filepath, GLTFRoot gltf, byte[] binaryData)
        {
            // GLB format: 12-byte header + JSON chunk + BIN chunk
            string json = JsonUtility.ToJson(gltf);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            // Pad JSON to 4-byte alignment
            int jsonPadding = (4 - (jsonBytes.Length % 4)) % 4;
            byte[] paddedJson = new byte[jsonBytes.Length + jsonPadding];
            Array.Copy(jsonBytes, paddedJson, jsonBytes.Length);
            for (int i = 0; i < jsonPadding; i++)
                paddedJson[jsonBytes.Length + i] = 0x20; // space

            // Pad binary to 4-byte alignment
            int binPadding = (4 - (binaryData.Length % 4)) % 4;
            byte[] paddedBin = new byte[binaryData.Length + binPadding];
            Array.Copy(binaryData, paddedBin, binaryData.Length);

            int totalLength = 12 + 8 + paddedJson.Length + 8 + paddedBin.Length;

            using var stream = new FileStream(filepath, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            // GLB header
            writer.Write(0x46546C67); // "glTF" magic
            writer.Write(2); // Version
            writer.Write(totalLength);

            // JSON chunk
            writer.Write(paddedJson.Length);
            writer.Write(0x4E4F534A); // "JSON"
            writer.Write(paddedJson);

            // BIN chunk
            writer.Write(paddedBin.Length);
            writer.Write(0x004E4942); // "BIN\0"
            writer.Write(paddedBin);
        }
    }
}
