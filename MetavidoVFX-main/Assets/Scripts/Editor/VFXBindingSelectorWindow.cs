// VFXBindingSelectorWindow - Editor window for selecting optional bindings
// Shows available bindings and lets user choose which to enable

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;

public class VFXBindingSelectorWindow : EditorWindow
{
    private VFXPipelineAuditResult _result;
    private VisualEffect _vfx;
    private Dictionary<string, bool> _selectedBindings = new();
    private Vector2 _scrollPos;

    public static void ShowWindow(VFXPipelineAuditResult result)
    {
        var window = GetWindow<VFXBindingSelectorWindow>("VFX Binding Selector");
        window._result = result;
        window._vfx = result.VFX;
        window.InitializeSelections();
        window.minSize = new Vector2(350, 400);
        window.Show();
    }

    [MenuItem("H3M/VFX Pipeline Master/Audit & Fix/Select Optional Bindings...")]
    static void ShowForSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null || !selected.TryGetComponent<VisualEffect>(out var vfx))
        {
            EditorUtility.DisplayDialog("No VFX Selected",
                "Select a GameObject with VisualEffect component first.", "OK");
            return;
        }

        var result = VFXPipelineAuditor.AuditSingleVFX(vfx);
        ShowWindow(result);
    }

    void InitializeSelections()
    {
        _selectedBindings.Clear();
        foreach (var binding in _result.OptionalBindings)
        {
            _selectedBindings[binding] = false;
        }
    }

    void OnGUI()
    {
        if (_result == null || _vfx == null)
        {
            EditorGUILayout.HelpBox("No VFX selected. Use menu or context menu.", MessageType.Info);
            return;
        }

        // Header
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"VFX: {_result.VFXName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Asset: {_result.AssetName}", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);

        // Configured bindings (read-only)
        EditorGUILayout.LabelField("Configured Bindings", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (_result.ConfiguredBindings.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var binding in _result.ConfiguredBindings)
                {
                    EditorGUILayout.LabelField($"  ✓ {binding}", EditorStyles.miniLabel);
                }
            }
        }

        EditorGUILayout.Space(5);

        // Missing required bindings (auto-fix available)
        if (_result.MissingBindings.Count > 0)
        {
            EditorGUILayout.LabelField("Missing Required Bindings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var binding in _result.MissingBindings)
                {
                    EditorGUILayout.LabelField($"  ✗ {binding} (will be auto-added)", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.Space(5);
        }

        // Optional bindings (selectable)
        EditorGUILayout.LabelField("Optional Bindings (select to add)", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(200));

            if (_result.OptionalBindings.Count == 0)
            {
                EditorGUILayout.LabelField("No optional bindings available", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var binding in _result.OptionalBindings)
                {
                    bool selected = _selectedBindings.ContainsKey(binding) && _selectedBindings[binding];
                    bool newSelected = EditorGUILayout.ToggleLeft(GetBindingDescription(binding), selected);
                    _selectedBindings[binding] = newSelected;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // Quick select buttons
        EditorGUILayout.Space(5);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select All"))
            {
                foreach (var key in new List<string>(_selectedBindings.Keys))
                    _selectedBindings[key] = true;
            }
            if (GUILayout.Button("Select None"))
            {
                foreach (var key in new List<string>(_selectedBindings.Keys))
                    _selectedBindings[key] = false;
            }
        }

        EditorGUILayout.Space(10);

        // Apply button
        int selectedCount = 0;
        foreach (var kvp in _selectedBindings)
            if (kvp.Value) selectedCount++;

        int totalToAdd = _result.MissingBindings.Count + selectedCount;

        GUI.backgroundColor = totalToAdd > 0 ? new Color(0.5f, 0.8f, 0.5f) : Color.gray;
        if (GUILayout.Button($"Apply & Wire All ({totalToAdd} bindings)", GUILayout.Height(30)))
        {
            ApplySelectedBindings();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Selected bindings will be automatically enabled on VFXARBinder and wired to data sources.",
            MessageType.Info);
    }

    string GetBindingDescription(string binding)
    {
        return binding switch
        {
            "DepthMap" => "DepthMap - AR depth texture",
            "StencilMap" => "StencilMap - Human segmentation mask",
            "PositionMap" => "PositionMap - World positions (GPU computed)",
            "ColorMap" => "ColorMap - Camera RGB texture",
            "VelocityMap" => "VelocityMap - Motion vectors",
            "RayParams" => "RayParams - UV to ray parameters",
            "InverseView" => "InverseView - Camera pose matrix",
            "DepthRange" => "DepthRange - Near/far clipping",
            "Throttle" => "Throttle - Intensity control (0-1)",
            "Audio" => "Audio - Audio reactive bands",
            "AnchorPos" => "AnchorPos - Hologram anchor position",
            "HologramScale" => "HologramScale - Hologram scale factor",
            _ => binding
        };
    }

    void ApplySelectedBindings()
    {
        if (_vfx == null) return;

        var go = _vfx.gameObject;

        // Ensure VFXARBinder exists
        var binder = go.GetComponent<VFXARBinder>();
        if (binder == null)
        {
            binder = Undo.AddComponent<VFXARBinder>(go);
            Debug.Log($"[VFXBindingSelector] Added VFXARBinder to {go.name}");
        }

        Undo.RecordObject(binder, "Apply VFX Bindings");

        int appliedCount = 0;

        // Apply missing required bindings
        foreach (var binding in _result.MissingBindings)
        {
            EnableBindingOnBinder(binder, binding);
            appliedCount++;
        }

        // Apply selected optional bindings
        foreach (var kvp in _selectedBindings)
        {
            if (kvp.Value)
            {
                EnableBindingOnBinder(binder, kvp.Key);
                appliedCount++;
            }
        }

        EditorUtility.SetDirty(binder);
        EditorUtility.SetDirty(go);

        Debug.Log($"[VFXBindingSelector] Applied {appliedCount} bindings to {go.name}");

        // Refresh audit result
        _result = VFXPipelineAuditor.AuditSingleVFX(_vfx);
        InitializeSelections();

        EditorUtility.DisplayDialog("Bindings Applied",
            $"Successfully applied {appliedCount} bindings to {go.name}.\n\nAll bindings are now auto-wired to ARDepthSource.",
            "OK");
    }

    void EnableBindingOnBinder(VFXARBinder binder, string binding)
    {
        switch (binding)
        {
            case "DepthMap": binder.BindDepthMap = true; break;
            case "StencilMap": binder.BindStencilMap = true; break;
            case "PositionMap": binder.BindPositionMap = true; break;
            case "ColorMap": binder.BindColorMap = true; break;
            case "VelocityMap": binder.BindVelocityMap = true; break;
            case "RayParams": binder.BindRayParams = true; break;
            case "InverseView": binder.BindInverseView = true; break;
            case "DepthRange": binder.BindDepthRange = true; break;
            case "Throttle": binder.BindThrottle = true; break;
            case "Audio": binder.BindAudio = true; break;
            case "AnchorPos": binder.BindAnchorPos = true; break;
            case "HologramScale": binder.BindHologramScale = true; break;
        }
    }
}
#endif
