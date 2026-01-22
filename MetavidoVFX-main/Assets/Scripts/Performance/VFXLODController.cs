// VFX LOD Controller - Distance-based quality and culling
// Works alongside VFXAutoOptimizer for fine-grained control

using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.Performance
{
    /// <summary>
    /// Attach to individual VFX for distance-based LOD and culling.
    /// Automatically reduces quality or disables VFX based on camera distance.
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public class VFXLODController : MonoBehaviour
    {
        [Header("LOD Distances")]
        [SerializeField] private float lodDistance1 = 2f;   // Full quality
        [SerializeField] private float lodDistance2 = 5f;   // Reduced quality
        [SerializeField] private float lodDistance3 = 10f;  // Minimum quality
        [SerializeField] private float cullDistance = 15f;  // Disable VFX

        [Header("Quality Multipliers per LOD")]
        [SerializeField] private float lod0Quality = 1.0f;
        [SerializeField] private float lod1Quality = 0.7f;
        [SerializeField] private float lod2Quality = 0.4f;
        [SerializeField] private float lod3Quality = 0.2f;

        [Header("Settings")]
        [SerializeField] private bool autoCull = true;
        [SerializeField] private float updateInterval = 0.2f;
        [SerializeField] private Transform referenceCamera;

        private VisualEffect _vfx;
        private float _lastUpdateTime;
        private int _currentLOD = 0;
        private float _baseSpawnRate;
        private bool _hasSpawnRate;
        private bool _isCulled;

        public int CurrentLOD => _currentLOD;
        public bool IsCulled => _isCulled;
        public float DistanceToCamera { get; private set; }

        void Awake()
        {
            _vfx = GetComponent<VisualEffect>();

            // Store original spawn rate
            if (_vfx.HasFloat("SpawnRate"))
            {
                _hasSpawnRate = true;
                _baseSpawnRate = _vfx.GetFloat("SpawnRate");
            }
        }

        void Start()
        {
            if (referenceCamera == null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    referenceCamera = cam.transform;
                }
            }
        }

        void Update()
        {
            if (Time.time - _lastUpdateTime < updateInterval)
                return;

            _lastUpdateTime = Time.time;
            UpdateLOD();
        }

        void UpdateLOD()
        {
            if (referenceCamera == null) return;

            DistanceToCamera = Vector3.Distance(transform.position, referenceCamera.position);

            int newLOD;
            float qualityMultiplier;

            if (DistanceToCamera > cullDistance && autoCull)
            {
                // Beyond cull distance
                if (!_isCulled)
                {
                    _vfx.enabled = false;
                    _isCulled = true;
                }
                return;
            }
            else if (_isCulled)
            {
                // Re-enable if was culled
                _vfx.enabled = true;
                _isCulled = false;
            }

            // Determine LOD level
            if (DistanceToCamera <= lodDistance1)
            {
                newLOD = 0;
                qualityMultiplier = lod0Quality;
            }
            else if (DistanceToCamera <= lodDistance2)
            {
                newLOD = 1;
                qualityMultiplier = lod1Quality;
            }
            else if (DistanceToCamera <= lodDistance3)
            {
                newLOD = 2;
                qualityMultiplier = lod2Quality;
            }
            else
            {
                newLOD = 3;
                qualityMultiplier = lod3Quality;
            }

            // Only update if LOD changed
            if (newLOD != _currentLOD)
            {
                _currentLOD = newLOD;
                ApplyLOD(qualityMultiplier);
            }
        }

        void ApplyLOD(float quality)
        {
            if (_hasSpawnRate)
            {
                _vfx.SetFloat("SpawnRate", _baseSpawnRate * quality);
            }

            if (_vfx.HasFloat("QualityMultiplier"))
            {
                _vfx.SetFloat("QualityMultiplier", quality);
            }

            if (_vfx.HasFloat("LODLevel"))
            {
                _vfx.SetFloat("LODLevel", _currentLOD);
            }
        }

        /// <summary>
        /// Force a specific LOD level
        /// </summary>
        public void SetLOD(int lod)
        {
            _currentLOD = Mathf.Clamp(lod, 0, 3);

            float quality = _currentLOD switch
            {
                0 => lod0Quality,
                1 => lod1Quality,
                2 => lod2Quality,
                _ => lod3Quality
            };

            ApplyLOD(quality);
        }

        void OnDrawGizmosSelected()
        {
            // Visualize LOD distances
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, lodDistance1);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, lodDistance2);

            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(transform.position, lodDistance3);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, cullDistance);
        }
    }
}
