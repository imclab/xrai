// Physics VFX Collider
// Enables VFX particles to collide with AR mesh (floor, walls, objects)
// Uses AR Mesh Manager for LiDAR-based collision surfaces

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;

namespace MetavidoVFX.HandTracking
{
    /// <summary>
    /// Provides physics collision data to VFX Graph from AR mesh.
    /// Particles can bounce off LiDAR-detected surfaces (floor, walls, furniture).
    /// </summary>
    public class PhysicsVFXCollider : MonoBehaviour
    {
        [Header("AR Mesh")]
        [SerializeField] private ARMeshManager meshManager;
        [SerializeField] private ARPlaneManager planeManager;

        [Header("VFX Targets")]
        [SerializeField] private VisualEffect[] vfxTargets;

        [Header("Collision Settings")]
        [SerializeField] private float floorHeight = 0f;
        [SerializeField] private float bounciness = 0.5f;
        [SerializeField] private float friction = 0.2f;
        [SerializeField] private LayerMask collisionLayers = -1;

        [Header("Collision Planes (auto-detected)")]
        [SerializeField] private int maxCollisionPlanes = 8;

        // Runtime collision plane data
        private List<Vector3> planePositions = new List<Vector3>();
        private List<Vector3> planeNormals = new List<Vector3>();

        // VFX property names
        private const string PROP_FLOOR_HEIGHT = "FloorHeight";
        private const string PROP_BOUNCINESS = "CollisionBounciness";
        private const string PROP_FRICTION = "CollisionFriction";
        private const string PROP_PLANE_COUNT = "CollisionPlaneCount";
        private const string PROP_PLANE_POSITIONS = "CollisionPlanePositions";
        private const string PROP_PLANE_NORMALS = "CollisionPlaneNormals";

        void Start()
        {
            // Find managers if not assigned
            if (meshManager == null)
                meshManager = FindFirstObjectByType<ARMeshManager>();

            if (planeManager == null)
                planeManager = FindFirstObjectByType<ARPlaneManager>();

            // Find all VFX in scene if not assigned
            if (vfxTargets == null || vfxTargets.Length == 0)
            {
                vfxTargets = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            }
        }

        void Update()
        {
            UpdateCollisionPlanes();
            PushToVFX();
        }

        void UpdateCollisionPlanes()
        {
            planePositions.Clear();
            planeNormals.Clear();

            // Add floor plane
            planePositions.Add(new Vector3(0, floorHeight, 0));
            planeNormals.Add(Vector3.up);

            // Add AR planes
            if (planeManager != null)
            {
                foreach (var plane in planeManager.trackables)
                {
                    if (planePositions.Count >= maxCollisionPlanes) break;

                    planePositions.Add(plane.center);

                    // Determine plane normal based on alignment
                    Vector3 normal = plane.alignment switch
                    {
                        UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp => Vector3.up,
                        UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalDown => Vector3.down,
                        UnityEngine.XR.ARSubsystems.PlaneAlignment.Vertical => plane.normal,
                        _ => plane.normal
                    };

                    planeNormals.Add(normal);
                }
            }

            // Add mesh-based collision (sample mesh centers as collision points)
            if (meshManager != null && meshManager.meshes != null)
            {
                foreach (var meshFilter in meshManager.meshes)
                {
                    if (planePositions.Count >= maxCollisionPlanes) break;

                    if (meshFilter.sharedMesh != null)
                    {
                        // Use mesh bounds center as approximate collision point
                        Vector3 center = meshFilter.transform.TransformPoint(meshFilter.sharedMesh.bounds.center);

                        // Estimate normal from mesh orientation (simplified)
                        Vector3 normal = meshFilter.transform.up;

                        planePositions.Add(center);
                        planeNormals.Add(normal);
                    }
                }
            }
        }

        void PushToVFX()
        {
            foreach (var vfx in vfxTargets)
            {
                if (vfx == null) continue;

                // Basic collision parameters
                if (vfx.HasFloat(PROP_FLOOR_HEIGHT))
                    vfx.SetFloat(PROP_FLOOR_HEIGHT, floorHeight);

                if (vfx.HasFloat(PROP_BOUNCINESS))
                    vfx.SetFloat(PROP_BOUNCINESS, bounciness);

                if (vfx.HasFloat(PROP_FRICTION))
                    vfx.SetFloat(PROP_FRICTION, friction);

                // Number of collision planes
                if (vfx.HasInt(PROP_PLANE_COUNT))
                    vfx.SetInt(PROP_PLANE_COUNT, planePositions.Count);

                // Individual plane data (VFX Graph typically uses indexed properties)
                for (int i = 0; i < Mathf.Min(planePositions.Count, maxCollisionPlanes); i++)
                {
                    string posName = $"CollisionPlane{i}_Position";
                    string normName = $"CollisionPlane{i}_Normal";

                    if (vfx.HasVector3(posName))
                        vfx.SetVector3(posName, planePositions[i]);

                    if (vfx.HasVector3(normName))
                        vfx.SetVector3(normName, planeNormals[i]);
                }

                // Also push as primary collision plane (most common usage)
                if (planePositions.Count > 0)
                {
                    if (vfx.HasVector3("CollisionPlanePosition"))
                        vfx.SetVector3("CollisionPlanePosition", planePositions[0]);

                    if (vfx.HasVector3("CollisionPlaneNormal"))
                        vfx.SetVector3("CollisionPlaneNormal", planeNormals[0]);
                }
            }
        }

        /// <summary>
        /// Manually set floor height (useful for calibration)
        /// </summary>
        public void SetFloorHeight(float height)
        {
            floorHeight = height;
        }

        /// <summary>
        /// Detect floor height from lowest AR plane
        /// </summary>
        public void AutoDetectFloor()
        {
            if (planeManager == null) return;

            float lowestY = float.MaxValue;
            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp)
                {
                    if (plane.center.y < lowestY)
                    {
                        lowestY = plane.center.y;
                    }
                }
            }

            if (lowestY < float.MaxValue)
            {
                floorHeight = lowestY;
                Debug.Log($"[PhysicsVFX] Auto-detected floor at Y={floorHeight:F2}");
            }
        }

        /// <summary>
        /// Add a VFX target at runtime
        /// </summary>
        public void AddVFXTarget(VisualEffect vfx)
        {
            var list = new List<VisualEffect>(vfxTargets);
            if (!list.Contains(vfx))
            {
                list.Add(vfx);
                vfxTargets = list.ToArray();
            }
        }

        /// <summary>
        /// Get current collision data for debugging
        /// </summary>
        public (Vector3[] positions, Vector3[] normals) GetCollisionData()
        {
            return (planePositions.ToArray(), planeNormals.ToArray());
        }

        void OnDrawGizmosSelected()
        {
            // Visualize collision planes
            Gizmos.color = Color.cyan;
            for (int i = 0; i < planePositions.Count; i++)
            {
                Gizmos.DrawWireSphere(planePositions[i], 0.1f);
                Gizmos.DrawRay(planePositions[i], planeNormals[i] * 0.3f);
            }

            // Visualize floor
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(0, floorHeight, 0), new Vector3(5, 0.01f, 5));
        }
    }
}
