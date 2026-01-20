using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// AI-assist editor tool that validates VFX Graph bindings and catches common errors.
/// Runs automatically on play mode enter and can be triggered manually.
/// </summary>
public class VFXBindingValidator : EditorWindow
{
    private Vector2 scrollPos;
    private List<ValidationResult> results = new List<ValidationResult>();
    private bool autoValidateOnPlay = true;

    private struct ValidationResult
    {
        public string vfxName;
        public string issue;
        public MessageType severity;
        public GameObject gameObject;
    }

    [MenuItem("Tools/VFX/Binding Validator")]
    public static void ShowWindow()
    {
        GetWindow<VFXBindingValidator>("VFX Validator");
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var results = ValidateAllVFX();
            var errors = results.Where(r => r.severity == MessageType.Error).ToList();
            if (errors.Count > 0)
            {
                Debug.LogWarning($"[VFXValidator] Found {errors.Count} VFX binding issues. Open Tools > VFX > Binding Validator for details.");
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate All VFX", GUILayout.Height(30)))
        {
            results = ValidateAllVFX();
        }
        if (GUILayout.Button("Validate Selection", GUILayout.Height(30)))
        {
            results = ValidateSelection();
        }
        EditorGUILayout.EndHorizontal();

        autoValidateOnPlay = EditorGUILayout.Toggle("Auto-validate on Play", autoValidateOnPlay);

        EditorGUILayout.Space();

        // Summary
        var errors = results.Count(r => r.severity == MessageType.Error);
        var warnings = results.Count(r => r.severity == MessageType.Warning);
        var info = results.Count(r => r.severity == MessageType.Info);

        EditorGUILayout.LabelField($"Results: {errors} errors, {warnings} warnings, {info} info", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // Results list
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var result in results)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox($"[{result.vfxName}] {result.issue}", result.severity);
            if (result.gameObject != null && GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = result.gameObject;
                EditorGUIUtility.PingObject(result.gameObject);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private static List<ValidationResult> ValidateAllVFX()
    {
        var results = new List<ValidationResult>();
        var vfxComponents = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

        foreach (var vfx in vfxComponents)
        {
            results.AddRange(ValidateVFX(vfx));
        }

        return results;
    }

    private static List<ValidationResult> ValidateSelection()
    {
        var results = new List<ValidationResult>();
        foreach (var go in Selection.gameObjects)
        {
            var vfx = go.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                results.AddRange(ValidateVFX(vfx));
            }
        }
        return results;
    }

    private static List<ValidationResult> ValidateVFX(VisualEffect vfx)
    {
        var results = new List<ValidationResult>();
        var vfxName = vfx.gameObject.name;
        var go = vfx.gameObject;

        // Check 1: VFX Asset assigned
        if (vfx.visualEffectAsset == null)
        {
            results.Add(new ValidationResult
            {
                vfxName = vfxName,
                issue = "No VFX Asset assigned",
                severity = MessageType.Error,
                gameObject = go
            });
            return results;
        }

        // Check 2: Validate exposed properties have binders
        var binders = go.GetComponents<MonoBehaviour>()
            .Where(c => c != null && c.GetType().Name.Contains("Binder"))
            .ToList();

        if (binders.Count == 0)
        {
            // Check if VFX has texture properties that need binding
            if (HasExposedProperty(vfx, "ColorMap") || HasExposedProperty(vfx, "DepthMap"))
            {
                results.Add(new ValidationResult
                {
                    vfxName = vfxName,
                    issue = "VFX has texture properties but no binder component",
                    severity = MessageType.Warning,
                    gameObject = go
                });
            }
        }

        // Check 3: Validate common property names
        var commonProps = new[] { "ColorMap", "DepthMap", "StencilMap", "CameraMatrix", "InverseViewMatrix" };
        foreach (var prop in commonProps)
        {
            if (HasExposedProperty(vfx, prop))
            {
                // Check if property is bound (has a texture/matrix set)
                if (prop.Contains("Map"))
                {
                    var tex = vfx.GetTexture(prop);
                    if (tex == null && Application.isPlaying)
                    {
                        results.Add(new ValidationResult
                        {
                            vfxName = vfxName,
                            issue = $"Property '{prop}' is exposed but has no texture bound",
                            severity = MessageType.Warning,
                            gameObject = go
                        });
                    }
                }
            }
        }

        // Check 4: Validate VFXARBinder configuration
        var arBinder = go.GetComponent("VFXARBinder") as MonoBehaviour;
        if (arBinder != null)
        {
            var vfxField = arBinder.GetType().GetField("targetVFX", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (vfxField != null)
            {
                var targetVFX = vfxField.GetValue(arBinder) as VisualEffect;
                if (targetVFX == null)
                {
                    results.Add(new ValidationResult
                    {
                        vfxName = vfxName,
                        issue = "VFXARBinder has no targetVFX assigned",
                        severity = MessageType.Error,
                        gameObject = go
                    });
                }
                else if (targetVFX != vfx)
                {
                    results.Add(new ValidationResult
                    {
                        vfxName = vfxName,
                        issue = "VFXARBinder targetVFX doesn't match this VFX component",
                        severity = MessageType.Warning,
                        gameObject = go
                    });
                }
            }
        }

        // Check 5: Performance warning for high particle counts
        if (vfx.aliveParticleCount > 100000)
        {
            results.Add(new ValidationResult
            {
                vfxName = vfxName,
                issue = $"High particle count: {vfx.aliveParticleCount:N0} particles",
                severity = MessageType.Warning,
                gameObject = go
            });
        }

        // Check 6: Inactive VFX that should be enabled
        if (!vfx.enabled && go.activeInHierarchy)
        {
            results.Add(new ValidationResult
            {
                vfxName = vfxName,
                issue = "VFX component is disabled on active GameObject",
                severity = MessageType.Info,
                gameObject = go
            });
        }

        return results;
    }

    private static bool HasExposedProperty(VisualEffect vfx, string propertyName)
    {
        return vfx.HasTexture(propertyName) ||
               vfx.HasMatrix4x4(propertyName) ||
               vfx.HasFloat(propertyName) ||
               vfx.HasVector3(propertyName) ||
               vfx.HasInt(propertyName) ||
               vfx.HasBool(propertyName);
    }
}
