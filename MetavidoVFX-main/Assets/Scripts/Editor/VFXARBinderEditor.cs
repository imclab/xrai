// VFXARBinderEditor - Custom Inspector for VFXARBinder
// Enhanced with comprehensive pipeline audit (VFX + all associated components)

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using MetavidoVFX.VFX;

[CustomEditor(typeof(VFXARBinder))]
public class VFXARBinderEditor : Editor
{
    SerializedProperty _autoBindOnStart;
    bool _showAuditResults = false;
    VFXPipelineAuditResult _lastAuditResult;

    void OnEnable()
    {
        _autoBindOnStart = serializedObject.FindProperty("_autoBindOnStart");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var binder = (VFXARBinder)target;
        var vfx = binder.GetComponent<VisualEffect>();

        // Pipeline Status Header
        DrawPipelineStatus(binder, vfx);

        // Audit & Fix Section
        EditorGUILayout.Space(5);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Pipeline Audit & Fix", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Quick Audit button
                if (GUILayout.Button("Audit", GUILayout.Height(25), GUILayout.Width(60)))
                {
                    AuditPipeline(binder, vfx, fix: false);
                }

                // Audit & Fix button
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f);
                if (GUILayout.Button("Audit & Fix All", GUILayout.Height(25)))
                {
                    AuditPipeline(binder, vfx, fix: true);
                }
                GUI.backgroundColor = Color.white;

                // Optional bindings selector
                if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(25)))
                {
                    var result = VFXPipelineAuditor.AuditSingleVFX(vfx);
                    VFXBindingSelectorWindow.ShowWindow(result);
                }

                // Auto-bind toggle
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayout.ToggleLeft("Auto-Bind", _autoBindOnStart.boolValue, GUILayout.Width(80));
                if (EditorGUI.EndChangeCheck())
                {
                    _autoBindOnStart.boolValue = newValue;
                    if (newValue)
                    {
                        binder.AutoDetectBindings();
                        AuditPipeline(binder, vfx, fix: false);
                    }
                }
            }

            // Show audit results
            if (_showAuditResults && _lastAuditResult != null)
            {
                EditorGUILayout.Space(5);
                DrawAuditResults(_lastAuditResult);
            }
        }

        EditorGUILayout.Space(5);

        // Draw default inspector
        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspector();
    }

    void DrawPipelineStatus(VFXARBinder binder, VisualEffect vfx)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // VFX status
            bool hasAsset = vfx?.visualEffectAsset != null;
            GUILayout.Label(hasAsset ? "●" : "○", GetStatusStyle(hasAsset), GUILayout.Width(16));
            GUILayout.Label("VFX", GUILayout.Width(30));

            // ARDepthSource status
            bool hasSource = ARDepthSource.Instance != null;
            GUILayout.Label(hasSource ? "●" : "○", GetStatusStyle(hasSource), GUILayout.Width(16));
            GUILayout.Label("Source", GUILayout.Width(45));

            // VFXCategory status
            bool hasCategory = binder.GetComponent<VFXCategory>() != null;
            GUILayout.Label(hasCategory ? "●" : "○", GetStatusStyle(hasCategory), GUILayout.Width(16));
            GUILayout.Label("Category", GUILayout.Width(55));

            // VFXNoCull status
            bool hasNoCull = binder.GetComponent<VFXNoCull>() != null;
            GUILayout.Label(hasNoCull ? "●" : "○", GetStatusStyle(hasNoCull), GUILayout.Width(16));
            GUILayout.Label("NoCull", GUILayout.Width(50));

            // Binding count
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Bindings: {binder.BoundCount}", EditorStyles.miniLabel);
        }
    }

    GUIStyle GetStatusStyle(bool isGood)
    {
        var style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = isGood ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.4f, 0.2f);
        return style;
    }

    void AuditPipeline(VFXARBinder binder, VisualEffect vfx, bool fix)
    {
        if (vfx == null)
        {
            Debug.LogWarning("[VFXARBinderEditor] No VisualEffect component found!");
            return;
        }

        // Run comprehensive audit
        _lastAuditResult = VFXPipelineAuditor.AuditSingleVFX(vfx);

        // Check scene infrastructure
        _lastAuditResult.HasARDepthSourceInScene = ARDepthSource.Instance != null;
        _lastAuditResult.HasAudioBridgeInScene = Object.FindFirstObjectByType<AudioBridge>() != null;

        if (fix && _lastAuditResult.HasIssues)
        {
            VFXPipelineAuditor.FixSingleVFXIssues(_lastAuditResult);

            // Re-audit after fixes
            _lastAuditResult = VFXPipelineAuditor.AuditSingleVFX(vfx);
            _lastAuditResult.HasARDepthSourceInScene = ARDepthSource.Instance != null;
            _lastAuditResult.HasAudioBridgeInScene = Object.FindFirstObjectByType<AudioBridge>() != null;
        }

        _showAuditResults = true;

        // Log summary
        string action = fix ? "Audit & Fix" : "Audit";
        Debug.Log($"[VFXARBinderEditor] {action} complete for {vfx.name}: " +
                  $"{_lastAuditResult.ConfiguredBindings.Count} bindings, " +
                  $"{_lastAuditResult.Issues.Count} issues" +
                  (fix && _lastAuditResult.FixesApplied > 0 ? $", {_lastAuditResult.FixesApplied} fixes applied" : ""));
    }

    void DrawAuditResults(VFXPipelineAuditResult result)
    {
        // Status summary
        var statusColor = result.IsHealthy ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.8f, 0.4f, 0.2f);
        GUI.backgroundColor = statusColor;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.backgroundColor = Color.white;

            // VFX info
            EditorGUILayout.LabelField($"VFX: {result.AssetName}", EditorStyles.boldLabel);

            // Components
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawComponentStatus("VFXARBinder", result.HasVFXARBinder);
                DrawComponentStatus("VFXCategory", result.HasVFXCategory);
                DrawComponentStatus("VFXNoCull", result.HasVFXNoCull);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawComponentStatus("NNCamBinder", result.HasNNCamBinder);
                DrawComponentStatus("AudioBinder", result.HasAudioBinder);
                DrawComponentStatus("PhysicsBinder", result.HasPhysicsBinder);
            }

            // Bindings
            EditorGUILayout.Space(3);
            if (result.ConfiguredBindings.Count > 0)
            {
                EditorGUILayout.LabelField($"✓ Configured ({result.ConfiguredBindings.Count}):", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  {string.Join(", ", result.ConfiguredBindings)}", EditorStyles.miniLabel);
            }

            if (result.MissingBindings.Count > 0)
            {
                EditorGUILayout.LabelField($"✗ Missing ({result.MissingBindings.Count}):", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"  {string.Join(", ", result.MissingBindings)}", EditorStyles.miniLabel);
            }

            // Issues
            if (result.Issues.Count > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Issues:", EditorStyles.boldLabel);
                foreach (var issue in result.Issues)
                {
                    EditorGUILayout.LabelField($"⚠ {issue}", EditorStyles.wordWrappedMiniLabel);
                }
            }

            // Recommendations
            if (result.Recommendations.Count > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Recommendations:", EditorStyles.miniLabel);
                foreach (var rec in result.Recommendations)
                {
                    EditorGUILayout.LabelField($"→ {rec}", EditorStyles.wordWrappedMiniLabel);
                }
            }

            // Fixes applied
            if (result.FixesApplied > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"✓ {result.FixesApplied} fixes applied", EditorStyles.boldLabel);
            }

            // Healthy status
            if (result.IsHealthy)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("✓ Pipeline healthy - no issues found", EditorStyles.boldLabel);
            }
        }
    }

    void DrawComponentStatus(string name, bool exists)
    {
        var style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = exists ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
        string icon = exists ? "✓" : "○";
        GUILayout.Label($"{icon} {name}", style, GUILayout.Width(100));
    }
}
#endif
