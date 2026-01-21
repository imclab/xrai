using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard shortcuts and rapid testing utilities for VFX.
/// Press 1-9 for quick VFX, Space to cycle, C for categories, A for all toggle.
///
/// Goal: Auto populate HUD-UI-VFX for rapid testing of all VFX
/// </summary>
public class VFXTestHarness : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] bool _enableKeyboardShortcuts = true;
    [SerializeField] float _autoCycleInterval = 3f;

    [Header("Favorites (indices 1-9)")]
    [SerializeField] List<VisualEffect> _favorites = new List<VisualEffect>();

    [Header("Debug")]
    [SerializeField] bool _verboseLogging = true;

    // State
    List<VisualEffect> _allVFX = new List<VisualEffect>();
    Dictionary<string, List<VisualEffect>> _categories = new Dictionary<string, List<VisualEffect>>();
    int _currentIndex = 0;
    int _currentCategoryIndex = 0;
    string[] _categoryNames;
    bool _autoCycleEnabled = false;
    float _lastCycleTime;
    bool _allEnabled = false;

    VFXPipelineDashboard _dashboard;

    void Start()
    {
        RefreshVFXList();
        _dashboard = FindFirstObjectByType<VFXPipelineDashboard>();

        if (_verboseLogging)
        {
            Debug.Log($"[VFXTestHarness] Found {_allVFX.Count} VFX in {_categories.Count} categories");
            Debug.Log("[VFXTestHarness] Shortcuts: 1-9=Favorites, Space=Next, C=Category, A=All, Tab=Dashboard");
        }
    }

    void Update()
    {
        if (!_enableKeyboardShortcuts) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Number keys 1-9 for favorites
        if (keyboard.digit1Key.wasPressedThisFrame) SelectFavorite(0);
        if (keyboard.digit2Key.wasPressedThisFrame) SelectFavorite(1);
        if (keyboard.digit3Key.wasPressedThisFrame) SelectFavorite(2);
        if (keyboard.digit4Key.wasPressedThisFrame) SelectFavorite(3);
        if (keyboard.digit5Key.wasPressedThisFrame) SelectFavorite(4);
        if (keyboard.digit6Key.wasPressedThisFrame) SelectFavorite(5);
        if (keyboard.digit7Key.wasPressedThisFrame) SelectFavorite(6);
        if (keyboard.digit8Key.wasPressedThisFrame) SelectFavorite(7);
        if (keyboard.digit9Key.wasPressedThisFrame) SelectFavorite(8);

        // Space - Cycle to next VFX
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            CycleNext();
        }

        // C - Cycle categories
        if (keyboard.cKey.wasPressedThisFrame)
        {
            CycleCategory();
        }

        // A - Toggle all on/off
        if (keyboard.aKey.wasPressedThisFrame)
        {
            ToggleAll();
        }

        // P - Toggle auto-cycle (profiling mode)
        if (keyboard.pKey.wasPressedThisFrame)
        {
            ToggleAutoCycle();
        }

        // R - Refresh VFX list
        if (keyboard.rKey.wasPressedThisFrame)
        {
            RefreshVFXList();
        }

        // Auto-cycle
        if (_autoCycleEnabled && Time.time - _lastCycleTime > _autoCycleInterval)
        {
            CycleNext();
            _lastCycleTime = Time.time;
        }
    }

    public void RefreshVFXList()
    {
        _allVFX.Clear();
        _categories.Clear();

        var vfxArray = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
        _allVFX.AddRange(vfxArray.OrderBy(v => v.name));

        // Categorize by naming convention: {effect}_{datasource}_{target}_{origin}
        foreach (var vfx in _allVFX)
        {
            string category = InferCategory(vfx.name);
            if (!_categories.ContainsKey(category))
            {
                _categories[category] = new List<VisualEffect>();
            }
            _categories[category].Add(vfx);
        }

        _categoryNames = _categories.Keys.OrderBy(k => k).ToArray();

        if (_verboseLogging)
        {
            Debug.Log($"[VFXTestHarness] Refreshed: {_allVFX.Count} VFX, Categories: {string.Join(", ", _categoryNames)}");
        }
    }

    string InferCategory(string vfxName)
    {
        string lower = vfxName.ToLower();

        // Check for common patterns
        if (lower.Contains("people") || lower.Contains("human") || lower.Contains("body"))
            return "People";
        if (lower.Contains("hand") || lower.Contains("joint") || lower.Contains("keypoint"))
            return "Hands";
        if (lower.Contains("audio") || lower.Contains("sound") || lower.Contains("wave"))
            return "Audio";
        if (lower.Contains("env") || lower.Contains("mesh") || lower.Contains("world"))
            return "Environment";
        if (lower.Contains("face"))
            return "Face";

        // Check by source
        if (lower.Contains("rcam")) return "Rcam";
        if (lower.Contains("metavido")) return "Metavido";
        if (lower.Contains("nncam")) return "NNCam";
        if (lower.Contains("akvfx")) return "Akvfx";

        return "Other";
    }

    void SelectFavorite(int index)
    {
        // Auto-populate favorites if empty
        if (_favorites.Count == 0 || index >= _favorites.Count)
        {
            if (index < _allVFX.Count)
            {
                SoloVFX(_allVFX[index]);
                if (_verboseLogging)
                    Debug.Log($"[VFXTestHarness] Selected VFX #{index + 1}: {_allVFX[index].name}");
            }
        }
        else
        {
            SoloVFX(_favorites[index]);
            if (_verboseLogging)
                Debug.Log($"[VFXTestHarness] Selected Favorite #{index + 1}: {_favorites[index].name}");
        }
    }

    void CycleNext()
    {
        if (_allVFX.Count == 0) return;

        _currentIndex = (_currentIndex + 1) % _allVFX.Count;
        SoloVFX(_allVFX[_currentIndex]);

        if (_verboseLogging)
            Debug.Log($"[VFXTestHarness] Cycled to [{_currentIndex + 1}/{_allVFX.Count}]: {_allVFX[_currentIndex].name}");
    }

    void CycleCategory()
    {
        if (_categoryNames == null || _categoryNames.Length == 0) return;

        _currentCategoryIndex = (_currentCategoryIndex + 1) % _categoryNames.Length;
        string category = _categoryNames[_currentCategoryIndex];

        // Enable all in category, disable others
        foreach (var vfx in _allVFX)
        {
            vfx.enabled = _categories[category].Contains(vfx);
        }

        if (_verboseLogging)
            Debug.Log($"[VFXTestHarness] Category: {category} ({_categories[category].Count} VFX)");
    }

    void ToggleAll()
    {
        _allEnabled = !_allEnabled;

        foreach (var vfx in _allVFX)
        {
            vfx.enabled = _allEnabled;
        }

        if (_verboseLogging)
            Debug.Log($"[VFXTestHarness] All VFX: {(_allEnabled ? "ENABLED" : "DISABLED")}");
    }

    void ToggleAutoCycle()
    {
        _autoCycleEnabled = !_autoCycleEnabled;
        _lastCycleTime = Time.time;

        if (_verboseLogging)
            Debug.Log($"[VFXTestHarness] Auto-cycle: {(_autoCycleEnabled ? "ON" : "OFF")}");
    }

    void SoloVFX(VisualEffect target)
    {
        foreach (var vfx in _allVFX)
        {
            vfx.enabled = (vfx == target);
        }
    }

    #region Public API

    public void EnableCategory(string category)
    {
        if (!_categories.ContainsKey(category)) return;

        foreach (var vfx in _categories[category])
        {
            vfx.enabled = true;
        }
    }

    public void DisableCategory(string category)
    {
        if (!_categories.ContainsKey(category)) return;

        foreach (var vfx in _categories[category])
        {
            vfx.enabled = false;
        }
    }

    public void DisableAll()
    {
        foreach (var vfx in _allVFX)
        {
            vfx.enabled = false;
        }
        _allEnabled = false;
    }

    public void EnableAll()
    {
        foreach (var vfx in _allVFX)
        {
            vfx.enabled = true;
        }
        _allEnabled = true;
    }

    public List<VisualEffect> GetAllVFX() => _allVFX;
    public Dictionary<string, List<VisualEffect>> GetCategories() => _categories;

    #endregion

    #region Editor Helpers

    [ContextMenu("Refresh VFX List")]
    void EditorRefresh() => RefreshVFXList();

    [ContextMenu("Log All VFX")]
    void LogAllVFX()
    {
        RefreshVFXList();
        Debug.Log($"[VFXTestHarness] All VFX ({_allVFX.Count}):");
        foreach (var cat in _categories)
        {
            Debug.Log($"  [{cat.Key}] ({cat.Value.Count}):");
            foreach (var vfx in cat.Value)
            {
                var binder = vfx.GetComponent<VFXARBinder>();
                Debug.Log($"    - {vfx.name} | Binder: {(binder != null ? "Yes" : "No")} | Enabled: {vfx.enabled}");
            }
        }
    }

    [ContextMenu("Auto-Add Favorites from Top 9")]
    void AutoAddFavorites()
    {
        RefreshVFXList();
        _favorites.Clear();
        _favorites.AddRange(_allVFX.Take(9));
        Debug.Log($"[VFXTestHarness] Added {_favorites.Count} favorites from top VFX");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    #endregion
}
