// VFXModeController - Runtime mode switching for VFX (spec-007)
// Coordinates mode changes between UI, VFXARBinder, VFXCategory, and demand-driven systems

using UnityEngine;
using System.Collections.Generic;
using XRRAI.VFXBinders;

/// <summary>
/// Centralized controller for VFX mode switching.
/// Provides batch mode changes, UI integration, and demand-driven resource coordination.
/// </summary>
public class VFXModeController : MonoBehaviour
{
    public static VFXModeController Instance { get; private set; }

    [Header("Default Mode")]
    [Tooltip("Initial mode applied to all VFX on start")]
    [SerializeField] VFXCategoryType _defaultMode = VFXCategoryType.People;

    [Header("Mode Switching")]
    [Tooltip("Allow runtime mode switching via UI/API")]
    [SerializeField] bool _enableRuntimeModeSwitch = true;

    [Tooltip("Log mode changes for debugging")]
    [SerializeField] bool _verboseLogging = false;

    [Header("Demand-Driven Resources")]
    [Tooltip("Only allocate ColorMap when mode requires it")]
    [SerializeField] bool _enableDemandDrivenColorMap = true;

    [Tooltip("Only enable beat detection when Audio mode active")]
    [SerializeField] bool _enableDemandDrivenAudio = true;

    // Current global mode (affects new VFX)
    VFXCategoryType _currentGlobalMode;
    public VFXCategoryType CurrentGlobalMode => _currentGlobalMode;

    // Track all binders for batch operations
    readonly List<VFXARBinder> _registeredBinders = new List<VFXARBinder>();

    // Mode change event for UI updates
    public event System.Action<VFXCategoryType> OnModeChanged;

    void Awake()
    {
        Instance = this;
        _currentGlobalMode = _defaultMode;
    }

    void Start()
    {
        // Auto-discover all VFXARBinders in scene
        RefreshBinderList();

        // Apply default mode to all
        if (_verboseLogging)
            Debug.Log($"[VFXModeController] Applying default mode: {_defaultMode}");

        SetGlobalMode(_defaultMode);
    }

    /// <summary>
    /// Refresh the list of registered binders by scanning the scene.
    /// </summary>
    public void RefreshBinderList()
    {
        _registeredBinders.Clear();
        _registeredBinders.AddRange(FindObjectsByType<VFXARBinder>(FindObjectsSortMode.None));

        if (_verboseLogging)
            Debug.Log($"[VFXModeController] Found {_registeredBinders.Count} VFXARBinders");
    }

    /// <summary>
    /// Register a binder manually (for dynamically spawned VFX).
    /// </summary>
    public void RegisterBinder(VFXARBinder binder)
    {
        if (binder != null && !_registeredBinders.Contains(binder))
        {
            _registeredBinders.Add(binder);

            // Apply current global mode to new binder
            binder.SetMode(_currentGlobalMode);

            if (_verboseLogging)
                Debug.Log($"[VFXModeController] Registered binder: {binder.name}");
        }
    }

    /// <summary>
    /// Unregister a binder (when VFX destroyed).
    /// </summary>
    public void UnregisterBinder(VFXARBinder binder)
    {
        _registeredBinders.Remove(binder);
    }

    /// <summary>
    /// Set mode for ALL registered VFX.
    /// Returns report of which VFX used fallback modes (T-012).
    /// </summary>
    public ModeChangeReport SetGlobalMode(VFXCategoryType mode)
    {
        var report = new ModeChangeReport
        {
            RequestedMode = mode,
            SuccessfulCount = 0,
            FallbackCount = 0,
            FallbackVFX = new List<(string name, VFXCategoryType actualMode)>()
        };

        if (!_enableRuntimeModeSwitch && Application.isPlaying)
        {
            Debug.LogWarning("[VFXModeController] Runtime mode switching is disabled");
            return report;
        }

        _currentGlobalMode = mode;

        // Apply to all binders, tracking fallbacks
        foreach (var binder in _registeredBinders)
        {
            if (binder != null)
            {
                bool usedRequestedMode = binder.SetMode(mode);
                if (usedRequestedMode)
                {
                    report.SuccessfulCount++;
                }
                else
                {
                    report.FallbackCount++;
                    report.FallbackVFX.Add((binder.name, binder.CurrentMode));
                }
            }
        }

        // Update demand-driven resources
        UpdateDemandDrivenResources(mode);

        // Notify listeners
        OnModeChanged?.Invoke(mode);

        if (_verboseLogging)
        {
            string fallbackInfo = report.FallbackCount > 0 ? $" ({report.FallbackCount} used fallback)" : "";
            Debug.Log($"[VFXModeController] Global mode set to: {mode} ({_registeredBinders.Count} VFX updated{fallbackInfo})");
        }

        return report;
    }

    /// <summary>
    /// Set mode for a specific VFX (overrides global).
    /// </summary>
    public void SetVFXMode(VFXARBinder binder, VFXCategoryType mode)
    {
        if (binder == null) return;

        if (!_enableRuntimeModeSwitch && Application.isPlaying)
        {
            Debug.LogWarning("[VFXModeController] Runtime mode switching is disabled");
            return;
        }

        binder.SetMode(mode);

        if (_verboseLogging)
            Debug.Log($"[VFXModeController] VFX '{binder.name}' mode set to: {mode}");
    }

    /// <summary>
    /// Get all VFX that support a specific mode.
    /// </summary>
    public List<VFXARBinder> GetVFXSupportingMode(VFXCategoryType mode)
    {
        var result = new List<VFXARBinder>();
        foreach (var binder in _registeredBinders)
        {
            if (binder != null && binder.SupportsMode(mode))
            {
                result.Add(binder);
            }
        }
        return result;
    }

    /// <summary>
    /// Cycle through available modes.
    /// </summary>
    public void CycleMode()
    {
        var values = System.Enum.GetValues(typeof(VFXCategoryType));
        int currentIndex = (int)_currentGlobalMode;
        int nextIndex = (currentIndex + 1) % values.Length;
        SetGlobalMode((VFXCategoryType)values.GetValue(nextIndex));
    }

    /// <summary>
    /// Update demand-driven resources based on mode.
    /// </summary>
    void UpdateDemandDrivenResources(VFXCategoryType mode)
    {
        // Demand-driven ColorMap
        if (_enableDemandDrivenColorMap)
        {
            bool needsColorMap = mode != VFXCategoryType.Environment;

            var source = ARDepthSource.Instance;
            if (source != null)
            {
                source.RequestColorMap(needsColorMap);

                if (_verboseLogging)
                    Debug.Log($"[VFXModeController] ColorMap requested: {needsColorMap}");
            }
        }

        // Demand-driven audio (future: AudioBridge.EnableBeatDetection)
        if (_enableDemandDrivenAudio)
        {
            bool needsAudio = mode == VFXCategoryType.Audio || mode == VFXCategoryType.Hybrid;

            // Future: AudioBridge.Instance?.SetBeatDetectionEnabled(needsAudio);
            if (_verboseLogging)
                Debug.Log($"[VFXModeController] Audio required: {needsAudio}");
        }
    }

    /// <summary>
    /// Get mode statistics for UI display.
    /// </summary>
    public ModeStats GetModeStats()
    {
        var stats = new ModeStats
        {
            CurrentMode = _currentGlobalMode,
            TotalVFX = _registeredBinders.Count,
            ActiveVFX = 0,
            ModeSupportCounts = new Dictionary<VFXCategoryType, int>()
        };

        foreach (VFXCategoryType mode in System.Enum.GetValues(typeof(VFXCategoryType)))
        {
            stats.ModeSupportCounts[mode] = 0;
        }

        foreach (var binder in _registeredBinders)
        {
            if (binder == null) continue;

            if (binder.gameObject.activeInHierarchy)
                stats.ActiveVFX++;

            foreach (var mode in binder.GetSupportedModes())
            {
                stats.ModeSupportCounts[mode]++;
            }
        }

        return stats;
    }

    [ContextMenu("Debug Mode Controller")]
    void DebugModeController()
    {
        RefreshBinderList();
        var stats = GetModeStats();

        Debug.Log("=== VFXModeController Debug ===");
        Debug.Log($"Current Global Mode: {stats.CurrentMode}");
        Debug.Log($"Total VFX: {stats.TotalVFX}, Active: {stats.ActiveVFX}");
        Debug.Log("Mode Support:");
        foreach (var kv in stats.ModeSupportCounts)
        {
            Debug.Log($"  {kv.Key}: {kv.Value} VFX");
        }
    }

    [ContextMenu("Cycle Mode")]
    void DebugCycleMode()
    {
        CycleMode();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Statistics about VFX modes for UI display.
    /// </summary>
    public struct ModeStats
    {
        public VFXCategoryType CurrentMode;
        public int TotalVFX;
        public int ActiveVFX;
        public Dictionary<VFXCategoryType, int> ModeSupportCounts;
    }

    /// <summary>
    /// Report from SetGlobalMode showing which VFX used fallback modes (T-012).
    /// </summary>
    public struct ModeChangeReport
    {
        public VFXCategoryType RequestedMode;
        public int SuccessfulCount;
        public int FallbackCount;
        public List<(string name, VFXCategoryType actualMode)> FallbackVFX;

        public bool AllSuccessful => FallbackCount == 0;

        public override string ToString()
        {
            if (AllSuccessful)
                return $"Mode '{RequestedMode}': {SuccessfulCount} VFX updated successfully";

            var fallbacks = string.Join(", ", FallbackVFX.ConvertAll(f => $"{f.name}→{f.actualMode}"));
            return $"Mode '{RequestedMode}': {SuccessfulCount} OK, {FallbackCount} fallback ({fallbacks})";
        }
    }
}
