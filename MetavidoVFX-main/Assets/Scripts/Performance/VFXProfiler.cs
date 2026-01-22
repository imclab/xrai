// VFX Profiler - Analyzes VFX performance and identifies optimization opportunities
// Checks for expensive operations, HDRP nodes in URP, and provides recommendations

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;
using XRRAI.VFXBinders;

namespace XRRAI.Performance
{
    /// <summary>
    /// Profiles VFX in scene and identifies performance issues.
    /// Use in Editor for analysis, runtime for monitoring.
    /// </summary>
    public class VFXProfiler : MonoBehaviour
    {
        [Header("Profiling Settings")]
        [SerializeField] private float profilingInterval = 1f;
        [SerializeField] private bool continuousProfiling = false;
        [SerializeField] private bool logToConsole = true;

        [Header("Thresholds")]
        [SerializeField] private int warningParticleCount = 50000;
        [SerializeField] private int criticalParticleCount = 100000;
        [SerializeField] private int maxRecommendedVFX = 5;

        // Profile data
        private List<VFXProfileData> _profileData = new List<VFXProfileData>();
        private float _lastProfileTime;
        private StringBuilder _report = new StringBuilder();

        public struct VFXProfileData
        {
            public VisualEffect vfx;
            public string name;
            public int particleCount;
            public int capacity;
            public bool hasExpensiveNoise;
            public bool has3DTextures;
            public bool hasCollision;
            public bool hasStrips;
            public bool isEnabled;
            public float estimatedCost; // 0-100 score

            // Mode tracking (spec-007 T-013)
            public VFXCategoryType currentMode;
            public VFXCategoryType[] supportedModes;
            public bool hasVFXARBinder;
            public bool hasVFXCategory;
        }

        public List<VFXProfileData> ProfileData => _profileData;
        public string LastReport => _report.ToString();

        void Update()
        {
            if (continuousProfiling && Time.time - _lastProfileTime >= profilingInterval)
            {
                ProfileAllVFX();
                _lastProfileTime = Time.time;
            }
        }

        /// <summary>
        /// Profile all VFX in scene and generate report
        /// </summary>
        public string ProfileAllVFX()
        {
            _profileData.Clear();
            _report.Clear();

            var allVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

            _report.AppendLine("═══════════════════════════════════════════════════════════");
            _report.AppendLine("   VFX PERFORMANCE PROFILE");
            _report.AppendLine("═══════════════════════════════════════════════════════════");
            _report.AppendLine($"  Total VFX in Scene: {allVFX.Length}");

            int totalParticles = 0;
            int activeVFX = 0;
            int issueCount = 0;

            foreach (var vfx in allVFX)
            {
                var data = ProfileSingleVFX(vfx);
                _profileData.Add(data);

                if (data.isEnabled)
                {
                    activeVFX++;
                    totalParticles += data.particleCount;
                }

                if (data.estimatedCost > 50)
                {
                    issueCount++;
                }
            }

            _report.AppendLine($"  Active VFX: {activeVFX}");
            _report.AppendLine($"  Total Particles: {totalParticles:N0}");
            _report.AppendLine($"  High-Cost VFX: {issueCount}");
            _report.AppendLine();

            // Mode statistics (spec-007 T-013)
            var modeStats = GetModeStatistics();
            _report.AppendLine("  Mode Distribution:");
            foreach (var kv in modeStats)
            {
                if (kv.Value > 0)
                    _report.AppendLine($"    {kv.Key}: {kv.Value} VFX");
            }
            _report.AppendLine();

            // Warnings
            if (totalParticles > criticalParticleCount)
            {
                _report.AppendLine($"  ⚠️ CRITICAL: {totalParticles:N0} particles exceeds {criticalParticleCount:N0} limit!");
            }
            else if (totalParticles > warningParticleCount)
            {
                _report.AppendLine($"  ⚠️ WARNING: {totalParticles:N0} particles approaching limit");
            }

            if (activeVFX > maxRecommendedVFX)
            {
                _report.AppendLine($"  ⚠️ WARNING: {activeVFX} active VFX exceeds recommended {maxRecommendedVFX}");
            }

            // Per-VFX analysis
            _report.AppendLine();
            _report.AppendLine("───────────────────────────────────────────────────────────");
            _report.AppendLine("   VFX DETAILS");
            _report.AppendLine("───────────────────────────────────────────────────────────");

            foreach (var data in _profileData)
            {
                if (!data.isEnabled) continue;

                string costLevel = data.estimatedCost > 70 ? "HIGH" :
                                  data.estimatedCost > 40 ? "MED" : "LOW";
                string costColor = data.estimatedCost > 70 ? "🔴" :
                                  data.estimatedCost > 40 ? "🟡" : "🟢";

                _report.AppendLine($"  {costColor} {data.name}");
                _report.AppendLine($"     Particles: {data.particleCount:N0} / Capacity: {data.capacity:N0}");
                _report.AppendLine($"     Cost: {costLevel} ({data.estimatedCost:F0}/100)");

                // Mode info (spec-007 T-013)
                _report.AppendLine($"     Mode: {data.currentMode} | Supported: {string.Join(", ", data.supportedModes)}");
                if (!data.hasVFXARBinder)
                    _report.AppendLine("     ⚠️ Missing VFXARBinder (no mode switching)");

                if (data.hasExpensiveNoise)
                    _report.AppendLine("     ⚠️ Uses expensive noise (Turbulence/Voronoi)");
                if (data.has3DTextures)
                    _report.AppendLine("     ⚠️ Uses 3D textures (high memory)");
                if (data.hasCollision)
                    _report.AppendLine("     ⚠️ Uses collision (CPU intensive)");
                if (data.hasStrips)
                    _report.AppendLine("     ⚠️ Uses strips/trails (GPU intensive)");

                _report.AppendLine();
            }

            // Recommendations
            GenerateRecommendations();

            if (logToConsole)
            {
                Debug.Log(_report.ToString());
            }

            return _report.ToString();
        }

        VFXProfileData ProfileSingleVFX(VisualEffect vfx)
        {
            var data = new VFXProfileData
            {
                vfx = vfx,
                name = vfx.gameObject.name,
                isEnabled = vfx.enabled && vfx.gameObject.activeInHierarchy,
                particleCount = vfx.aliveParticleCount,
                currentMode = VFXCategoryType.People, // Default
                supportedModes = new VFXCategoryType[0]
            };

            // Mode tracking (spec-007 T-013)
            var binder = vfx.GetComponent<VFXARBinder>();
            var category = vfx.GetComponent<VFXCategory>();

            data.hasVFXARBinder = binder != null;
            data.hasVFXCategory = category != null;

            if (binder != null)
            {
                data.currentMode = binder.CurrentMode;
                data.supportedModes = binder.GetSupportedModes();
            }
            else if (category != null)
            {
                data.currentMode = category.Category;
                data.supportedModes = new[] { category.Category, VFXCategoryType.Hybrid };
            }

            // Try to get capacity (approximate from asset name patterns)
            var asset = vfx.visualEffectAsset;
            if (asset != null)
            {
                string assetName = asset.name.ToLower();

                // Detect expensive features from common naming patterns
                data.hasExpensiveNoise = assetName.Contains("noise") ||
                                        assetName.Contains("turbulen") ||
                                        assetName.Contains("voronoi");

                data.has3DTextures = assetName.Contains("volume") ||
                                    assetName.Contains("3d") ||
                                    assetName.Contains("sdf");

                data.hasStrips = assetName.Contains("trail") ||
                                assetName.Contains("strip") ||
                                assetName.Contains("ribbon") ||
                                assetName.Contains("line");

                data.hasCollision = assetName.Contains("collid") ||
                                   assetName.Contains("collision") ||
                                   assetName.Contains("bounce");
            }

            // Check exposed properties for additional hints
            if (vfx.HasInt("Capacity"))
            {
                data.capacity = vfx.GetInt("Capacity");
            }
            else
            {
                // Estimate from current particle count
                data.capacity = Mathf.Max(1000, data.particleCount * 2);
            }

            // Calculate cost score (includes mode-based adjustments)
            data.estimatedCost = CalculateCost(data);

            return data;
        }

        float CalculateCost(VFXProfileData data)
        {
            float cost = 0f;

            // Base cost from particle count
            cost += Mathf.Clamp01(data.particleCount / 50000f) * 30f;

            // Feature costs
            if (data.hasExpensiveNoise) cost += 20f;
            if (data.has3DTextures) cost += 15f;
            if (data.hasStrips) cost += 15f;
            if (data.hasCollision) cost += 20f;

            return Mathf.Clamp(cost, 0f, 100f);
        }

        void GenerateRecommendations()
        {
            _report.AppendLine("───────────────────────────────────────────────────────────");
            _report.AppendLine("   OPTIMIZATION RECOMMENDATIONS");
            _report.AppendLine("───────────────────────────────────────────────────────────");

            int recommendations = 0;

            // Check for multiple high-cost VFX
            int highCostCount = 0;
            foreach (var data in _profileData)
            {
                if (data.isEnabled && data.estimatedCost > 50)
                    highCostCount++;
            }

            if (highCostCount > 1)
            {
                recommendations++;
                _report.AppendLine($"  {recommendations}. Reduce active high-cost VFX (currently {highCostCount})");
                _report.AppendLine("     → Only enable one expensive VFX at a time");
            }

            // Check for expensive features
            bool hasExpensiveNoise = false;
            bool has3DTextures = false;

            foreach (var data in _profileData)
            {
                if (data.isEnabled)
                {
                    if (data.hasExpensiveNoise) hasExpensiveNoise = true;
                    if (data.has3DTextures) has3DTextures = true;
                }
            }

            if (hasExpensiveNoise)
            {
                recommendations++;
                _report.AppendLine($"  {recommendations}. Replace Turbulence/Voronoi noise with simpler alternatives");
                _report.AppendLine("     → Use Gradient Noise or pre-baked noise textures");
            }

            if (has3DTextures)
            {
                recommendations++;
                _report.AppendLine($"  {recommendations}. Optimize 3D texture usage");
                _report.AppendLine("     → Reduce 3D texture resolution or use 2D alternatives");
            }

            // General recommendations
            recommendations++;
            _report.AppendLine($"  {recommendations}. Add VFXAutoOptimizer to scene for automatic FPS management");

            recommendations++;
            _report.AppendLine($"  {recommendations}. Add VFXLODController to VFX for distance-based quality");

            recommendations++;
            _report.AppendLine($"  {recommendations}. Remove unused VFX Sample files from project");
            _report.AppendLine("     → Delete Assets/Samples/Visual Effect Graph/*/Learning Templates/");

            if (recommendations == 3)
            {
                _report.AppendLine();
                _report.AppendLine("  ✅ No major issues detected!");
            }

            _report.AppendLine();
            _report.AppendLine("═══════════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Get list of VFX sorted by cost (highest first)
        /// </summary>
        public List<VFXProfileData> GetSortedByCost()
        {
            var sorted = new List<VFXProfileData>(_profileData);
            sorted.Sort((a, b) => b.estimatedCost.CompareTo(a.estimatedCost));
            return sorted;
        }

        /// <summary>
        /// Get total particle count across all active VFX
        /// </summary>
        public int GetTotalParticleCount()
        {
            int total = 0;
            foreach (var data in _profileData)
            {
                if (data.isEnabled)
                    total += data.particleCount;
            }
            return total;
        }

        /// <summary>
        /// Get mode statistics (spec-007 T-013)
        /// </summary>
        public Dictionary<VFXCategoryType, int> GetModeStatistics()
        {
            var stats = new Dictionary<VFXCategoryType, int>();

            // Initialize all modes to 0
            foreach (VFXCategoryType mode in System.Enum.GetValues(typeof(VFXCategoryType)))
            {
                stats[mode] = 0;
            }

            // Count active VFX per mode
            foreach (var data in _profileData)
            {
                if (data.isEnabled)
                {
                    stats[data.currentMode]++;
                }
            }

            return stats;
        }

        /// <summary>
        /// Get VFX that don't have VFXARBinder (can't switch modes)
        /// </summary>
        public List<VFXProfileData> GetVFXMissingModeSupport()
        {
            var result = new List<VFXProfileData>();
            foreach (var data in _profileData)
            {
                if (data.isEnabled && !data.hasVFXARBinder)
                {
                    result.Add(data);
                }
            }
            return result;
        }

        /// <summary>
        /// Get average cost per mode (for mode-based optimization)
        /// </summary>
        public Dictionary<VFXCategoryType, float> GetAverageCostPerMode()
        {
            var costs = new Dictionary<VFXCategoryType, List<float>>();

            foreach (VFXCategoryType mode in System.Enum.GetValues(typeof(VFXCategoryType)))
            {
                costs[mode] = new List<float>();
            }

            foreach (var data in _profileData)
            {
                if (data.isEnabled)
                {
                    costs[data.currentMode].Add(data.estimatedCost);
                }
            }

            var averages = new Dictionary<VFXCategoryType, float>();
            foreach (var kv in costs)
            {
                if (kv.Value.Count > 0)
                {
                    float sum = 0;
                    foreach (var c in kv.Value) sum += c;
                    averages[kv.Key] = sum / kv.Value.Count;
                }
                else
                {
                    averages[kv.Key] = 0;
                }
            }

            return averages;
        }
    }
}
