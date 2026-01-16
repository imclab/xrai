// VFX Auto-Optimizer - Standalone performance manager
// Monitors FPS and particle counts, auto-adjusts VFX settings to maintain target framerate
// Tries to maximize fidelity while ensuring smooth 60fps performance

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace MetavidoVFX.Performance
{
    /// <summary>
    /// Automatically optimizes VFX performance to maintain target FPS.
    /// Monitors particle counts and adjusts spawn rates, capacities, and quality settings.
    /// </summary>
    public class VFXAutoOptimizer : MonoBehaviour
    {
        [Header("Performance Targets")]
        [SerializeField] private float targetFPS = 60f;
        [SerializeField] private float criticalFPS = 30f;
        [SerializeField] private float recoveryFPS = 55f;

        [Header("Monitoring")]
        [SerializeField] private float sampleInterval = 0.5f;
        [SerializeField] private int sampleCount = 10;
        [SerializeField] private bool trackParticleCounts = true;

        [Header("Optimization Levels")]
        [Tooltip("How aggressively to reduce quality when FPS drops")]
        [Range(0.1f, 1f)]
        [SerializeField] private float reductionStep = 0.1f;
        [Tooltip("How quickly to restore quality when FPS is good")]
        [Range(0.01f, 0.1f)]
        [SerializeField] private float recoveryStep = 0.02f;

        [Header("Limits")]
        [SerializeField] private float minQualityMultiplier = 0.1f;
        [SerializeField] private float maxQualityMultiplier = 1.0f;
        [SerializeField] private int maxTotalParticles = 500000;

        [Header("Debug")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool logOptimizations = false;

        // Current state
        private float _currentFPS;
        private float _averageFPS;
        private float _minFPS;
        private float _maxFPS;
        private int _totalParticleCount;
        private int _activeVFXCount;
        private float _qualityMultiplier = 1f;
        private OptimizationState _state = OptimizationState.Optimal;

        // Tracking
        private Queue<float> _fpsSamples = new Queue<float>();
        private float _lastSampleTime;
        private float _frameCount;
        private List<VFXInstance> _trackedVFX = new List<VFXInstance>();
        private float _lastParticleCountTime;

        // Original VFX settings (for restoration)
        private Dictionary<VisualEffect, VFXOriginalSettings> _originalSettings = new Dictionary<VisualEffect, VFXOriginalSettings>();

        public enum OptimizationState
        {
            Optimal,        // At target FPS, max quality
            Degrading,      // Reducing quality to improve FPS
            Critical,       // Emergency mode, minimum quality
            Recovering      // FPS good, slowly restoring quality
        }

        // Public properties for external monitoring
        public float CurrentFPS => _currentFPS;
        public float AverageFPS => _averageFPS;
        public float MinFPS => _minFPS;
        public float MaxFPS => _maxFPS;
        public int TotalParticleCount => _totalParticleCount;
        public int ActiveVFXCount => _activeVFXCount;
        public float QualityMultiplier => _qualityMultiplier;
        public OptimizationState State => _state;

        struct VFXInstance
        {
            public VisualEffect vfx;
            public int lastParticleCount;
        }

        struct VFXOriginalSettings
        {
            public float spawnRate;
            public int capacity;
            public bool hasSpawnRate;
            public bool hasCapacity;
        }

        void Start()
        {
            _lastSampleTime = Time.realtimeSinceStartup;
            _minFPS = float.MaxValue;
            _maxFPS = 0f;

            // Find all VFX in scene
            RefreshVFXList();

            Debug.Log($"[VFXOptimizer] Initialized - Target: {targetFPS}fps, Tracking {_trackedVFX.Count} VFX");
        }

        void Update()
        {
            TrackFPS();

            if (trackParticleCounts && Time.time - _lastParticleCountTime > 0.1f)
            {
                CountParticles();
                _lastParticleCountTime = Time.time;
            }

            // Run optimization logic every sample interval
            if (Time.realtimeSinceStartup - _lastSampleTime >= sampleInterval)
            {
                UpdateAverageFPS();
                OptimizePerformance();
                _lastSampleTime = Time.realtimeSinceStartup;
                _frameCount = 0;
            }
        }

        void TrackFPS()
        {
            _frameCount++;
            _currentFPS = 1f / Time.unscaledDeltaTime;

            // Clamp to reasonable range
            _currentFPS = Mathf.Clamp(_currentFPS, 1f, 200f);
        }

        void UpdateAverageFPS()
        {
            float avgFrameTime = (Time.realtimeSinceStartup - _lastSampleTime) / Mathf.Max(1, _frameCount);
            float sampleFPS = 1f / avgFrameTime;

            _fpsSamples.Enqueue(sampleFPS);
            while (_fpsSamples.Count > sampleCount)
            {
                _fpsSamples.Dequeue();
            }

            // Calculate average
            float sum = 0f;
            _minFPS = float.MaxValue;
            _maxFPS = 0f;

            foreach (float fps in _fpsSamples)
            {
                sum += fps;
                if (fps < _minFPS) _minFPS = fps;
                if (fps > _maxFPS) _maxFPS = fps;
            }

            _averageFPS = sum / _fpsSamples.Count;
        }

        void CountParticles()
        {
            _totalParticleCount = 0;
            _activeVFXCount = 0;

            for (int i = _trackedVFX.Count - 1; i >= 0; i--)
            {
                var instance = _trackedVFX[i];

                if (instance.vfx == null)
                {
                    _trackedVFX.RemoveAt(i);
                    continue;
                }

                if (!instance.vfx.enabled || !instance.vfx.gameObject.activeInHierarchy)
                    continue;

                _activeVFXCount++;
                int particleCount = instance.vfx.aliveParticleCount;
                _totalParticleCount += particleCount;

                instance.lastParticleCount = particleCount;
                _trackedVFX[i] = instance;
            }
        }

        void OptimizePerformance()
        {
            OptimizationState previousState = _state;

            // Determine state based on FPS
            if (_averageFPS < criticalFPS)
            {
                _state = OptimizationState.Critical;
            }
            else if (_averageFPS < targetFPS - 5f)
            {
                _state = OptimizationState.Degrading;
            }
            else if (_averageFPS >= recoveryFPS && _qualityMultiplier < maxQualityMultiplier)
            {
                _state = OptimizationState.Recovering;
            }
            else if (_qualityMultiplier >= maxQualityMultiplier - 0.01f)
            {
                _state = OptimizationState.Optimal;
            }

            // Also check particle count threshold
            if (_totalParticleCount > maxTotalParticles && _state == OptimizationState.Optimal)
            {
                _state = OptimizationState.Degrading;
            }

            // Apply optimization based on state
            switch (_state)
            {
                case OptimizationState.Critical:
                    // Emergency reduction
                    AdjustQuality(-reductionStep * 2f);
                    break;

                case OptimizationState.Degrading:
                    // Gradual reduction
                    AdjustQuality(-reductionStep);
                    break;

                case OptimizationState.Recovering:
                    // Slow recovery
                    AdjustQuality(recoveryStep);
                    break;

                case OptimizationState.Optimal:
                    // No changes needed
                    break;
            }

            if (logOptimizations && previousState != _state)
            {
                Debug.Log($"[VFXOptimizer] State: {_state}, FPS: {_averageFPS:F1}, Quality: {_qualityMultiplier:P0}, Particles: {_totalParticleCount}");
            }
        }

        void AdjustQuality(float delta)
        {
            float previousMultiplier = _qualityMultiplier;
            _qualityMultiplier = Mathf.Clamp(_qualityMultiplier + delta, minQualityMultiplier, maxQualityMultiplier);

            if (Mathf.Abs(_qualityMultiplier - previousMultiplier) < 0.001f)
                return;

            // Apply to all tracked VFX
            foreach (var instance in _trackedVFX)
            {
                if (instance.vfx == null || !instance.vfx.enabled)
                    continue;

                ApplyQualityToVFX(instance.vfx);
            }

            if (logOptimizations)
            {
                Debug.Log($"[VFXOptimizer] Quality adjusted: {previousMultiplier:P0} -> {_qualityMultiplier:P0}");
            }
        }

        void ApplyQualityToVFX(VisualEffect vfx)
        {
            // Store original settings if not already stored
            if (!_originalSettings.ContainsKey(vfx))
            {
                StoreOriginalSettings(vfx);
            }

            var original = _originalSettings[vfx];

            // Apply scaled spawn rate
            if (original.hasSpawnRate)
            {
                float scaledRate = original.spawnRate * _qualityMultiplier;
                vfx.SetFloat("SpawnRate", scaledRate);
            }

            // Apply scaled capacity (requires reinit)
            // Note: Changing capacity at runtime is expensive, only do on major changes
            if (original.hasCapacity && Mathf.Abs(_qualityMultiplier - 1f) > 0.3f)
            {
                // Only reduce capacity for significant optimization
                // This is expensive so we do it sparingly
            }

            // Common VFX quality parameters
            if (vfx.HasFloat("QualityMultiplier"))
            {
                vfx.SetFloat("QualityMultiplier", _qualityMultiplier);
            }

            if (vfx.HasFloat("ParticleScale"))
            {
                // Slightly reduce particle size at lower quality
                vfx.SetFloat("ParticleScale", Mathf.Lerp(0.5f, 1f, _qualityMultiplier));
            }

            if (vfx.HasFloat("SimulationSpeed"))
            {
                // Maintain simulation speed but could reduce for optimization
                vfx.SetFloat("SimulationSpeed", 1f);
            }
        }

        void StoreOriginalSettings(VisualEffect vfx)
        {
            var settings = new VFXOriginalSettings();

            if (vfx.HasFloat("SpawnRate"))
            {
                settings.hasSpawnRate = true;
                settings.spawnRate = vfx.GetFloat("SpawnRate");
            }

            // Note: Can't easily get capacity at runtime, would need serialized lookup
            settings.hasCapacity = false;

            _originalSettings[vfx] = settings;
        }

        /// <summary>
        /// Refresh the list of tracked VFX (call when VFX are added/removed)
        /// </summary>
        public void RefreshVFXList()
        {
            _trackedVFX.Clear();
            var allVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

            foreach (var vfx in allVFX)
            {
                _trackedVFX.Add(new VFXInstance { vfx = vfx, lastParticleCount = 0 });
            }

            Debug.Log($"[VFXOptimizer] Refreshed - tracking {_trackedVFX.Count} VFX instances");
        }

        /// <summary>
        /// Register a new VFX for tracking
        /// </summary>
        public void RegisterVFX(VisualEffect vfx)
        {
            if (vfx == null) return;

            // Check if already tracked
            foreach (var instance in _trackedVFX)
            {
                if (instance.vfx == vfx) return;
            }

            _trackedVFX.Add(new VFXInstance { vfx = vfx, lastParticleCount = 0 });
        }

        /// <summary>
        /// Force quality level (0-1)
        /// </summary>
        public void SetQualityMultiplier(float multiplier)
        {
            _qualityMultiplier = Mathf.Clamp(multiplier, minQualityMultiplier, maxQualityMultiplier);

            foreach (var instance in _trackedVFX)
            {
                if (instance.vfx != null && instance.vfx.enabled)
                {
                    ApplyQualityToVFX(instance.vfx);
                }
            }
        }

        /// <summary>
        /// Reset to maximum quality
        /// </summary>
        public void ResetToMaxQuality()
        {
            SetQualityMultiplier(maxQualityMultiplier);
            _state = OptimizationState.Optimal;
        }

        void OnGUI()
        {
            if (!showDebugUI) return;

            // Performance overlay
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperLeft;

            Color stateColor = _state switch
            {
                OptimizationState.Optimal => Color.green,
                OptimizationState.Recovering => Color.cyan,
                OptimizationState.Degrading => Color.yellow,
                OptimizationState.Critical => Color.red,
                _ => Color.white
            };

            string fpsColor = _averageFPS >= targetFPS ? "green" : (_averageFPS >= criticalFPS ? "yellow" : "red");

            string info = $"<color=white><b>VFX Performance</b></color>\n" +
                         $"<color={fpsColor}>FPS: {_averageFPS:F1}</color> (min: {_minFPS:F0}, max: {_maxFPS:F0})\n" +
                         $"Particles: {_totalParticleCount:N0}\n" +
                         $"Active VFX: {_activeVFXCount}\n" +
                         $"Quality: {_qualityMultiplier:P0}\n" +
                         $"<color=#{ColorUtility.ToHtmlStringRGB(stateColor)}>State: {_state}</color>";

            GUI.Label(new Rect(10, 10, 200, 120), info, style);
        }

        void OnValidate()
        {
            targetFPS = Mathf.Max(30f, targetFPS);
            criticalFPS = Mathf.Clamp(criticalFPS, 15f, targetFPS - 10f);
            recoveryFPS = Mathf.Clamp(recoveryFPS, criticalFPS + 5f, targetFPS);
        }
    }
}
