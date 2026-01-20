using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to find and remove legacy H3M components from scenes.
/// Legacy components have been moved to H3M/_Legacy/ and replaced by:
/// - HologramSource → ARDepthSource
/// - HologramRenderer → VFXARBinder
/// - HologramAnchor → HologramPlacer
/// - VFXBinderManager → ARDepthSource
/// - VFXARDataBinder → VFXARBinder
/// </summary>
public class LegacyComponentCleanup : EditorWindow
{
    private Vector2 _scrollPos;
    private List<ComponentInfo> _legacyComponents = new List<ComponentInfo>();
    private bool _scanned = false;

    private struct ComponentInfo
    {
        public GameObject GameObject;
        public string ComponentType;
        public bool Selected;
    }

    [MenuItem("H3M/Legacy/Find Legacy Components in Scene")]
    public static void ShowWindow()
    {
        var window = GetWindow<LegacyComponentCleanup>("Legacy Cleanup");
        window.minSize = new Vector2(400, 300);
        window.ScanScene();
    }

    [MenuItem("H3M/Legacy/Remove All Legacy Components (Auto)")]
    public static void RemoveAllLegacyAuto()
    {
        if (!EditorUtility.DisplayDialog("Remove Legacy Components",
            "This will remove all legacy H3M components from the current scene:\n\n" +
            "- HologramSource (H3M.Core)\n" +
            "- HologramRenderer (H3M.Core)\n" +
            "- HologramAnchor (H3M.Core)\n" +
            "- VFXBinderManager\n" +
            "- VFXARDataBinder\n\n" +
            "The scene will be marked dirty but NOT saved.\n\n" +
            "Continue?", "Remove", "Cancel"))
        {
            return;
        }

        int removed = RemoveLegacyComponents();

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[LegacyCleanup] Removed {removed} legacy components. Save the scene to persist changes.");
        }
        else
        {
            Debug.Log("[LegacyCleanup] No legacy components found in scene.");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Legacy Component Cleanup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool finds and removes legacy H3M components that have been superseded by the Hybrid Bridge Pipeline.\n\n" +
            "Replacements:\n" +
            "• HologramSource → ARDepthSource\n" +
            "• HologramRenderer → VFXARBinder\n" +
            "• HologramAnchor → HologramPlacer\n" +
            "• VFXBinderManager → ARDepthSource\n" +
            "• VFXARDataBinder → VFXARBinder",
            MessageType.Info);

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Scene", GUILayout.Height(30)))
        {
            ScanScene();
        }

        GUI.enabled = _legacyComponents.Any(c => c.Selected);
        if (GUILayout.Button("Remove Selected", GUILayout.Height(30)))
        {
            RemoveSelected();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (_scanned)
        {
            if (_legacyComponents.Count == 0)
            {
                EditorGUILayout.HelpBox("No legacy components found in scene.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Found {_legacyComponents.Count} legacy components:");

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                for (int i = 0; i < _legacyComponents.Count; i++)
                {
                    var info = _legacyComponents[i];
                    EditorGUILayout.BeginHorizontal("box");

                    info.Selected = EditorGUILayout.Toggle(info.Selected, GUILayout.Width(20));
                    _legacyComponents[i] = info;

                    if (GUILayout.Button(info.GameObject.name, EditorStyles.linkLabel))
                    {
                        Selection.activeGameObject = info.GameObject;
                        EditorGUIUtility.PingObject(info.GameObject);
                    }

                    EditorGUILayout.LabelField(info.ComponentType, GUILayout.Width(150));

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All"))
                {
                    for (int i = 0; i < _legacyComponents.Count; i++)
                    {
                        var info = _legacyComponents[i];
                        info.Selected = true;
                        _legacyComponents[i] = info;
                    }
                }
                if (GUILayout.Button("Select None"))
                {
                    for (int i = 0; i < _legacyComponents.Count; i++)
                    {
                        var info = _legacyComponents[i];
                        info.Selected = false;
                        _legacyComponents[i] = info;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void ScanScene()
    {
        _legacyComponents.Clear();
        _scanned = true;

        // Find all MonoBehaviours and check for legacy types
        var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var behaviour in allBehaviours)
        {
            if (behaviour == null) continue;

            string typeName = behaviour.GetType().FullName;

            // Check for legacy types
            if (IsLegacyType(typeName))
            {
                _legacyComponents.Add(new ComponentInfo
                {
                    GameObject = behaviour.gameObject,
                    ComponentType = behaviour.GetType().Name,
                    Selected = true
                });
            }
        }

        Debug.Log($"[LegacyCleanup] Scan complete. Found {_legacyComponents.Count} legacy components.");
    }

    private bool IsLegacyType(string typeName)
    {
        return typeName == "H3M.Core.HologramSource" ||
               typeName == "H3M.Core.HologramRenderer" ||
               typeName == "H3M.Core.HologramAnchor" ||
               typeName == "MetavidoVFX.VFX.VFXBinderManager" ||
               typeName == "MetavidoVFX.VFX.Binders.VFXARDataBinder" ||
               typeName == "VFXBinderManager" ||
               typeName == "VFXARDataBinder";
    }

    private void RemoveSelected()
    {
        int removed = 0;

        foreach (var info in _legacyComponents.Where(c => c.Selected))
        {
            if (info.GameObject == null) continue;

            var components = info.GameObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (IsLegacyType(comp.GetType().FullName))
                {
                    Undo.DestroyObjectImmediate(comp);
                    removed++;
                }
            }
        }

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[LegacyCleanup] Removed {removed} legacy components.");
        }

        ScanScene(); // Refresh the list
    }

    private static int RemoveLegacyComponents()
    {
        int removed = 0;
        var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var behaviour in allBehaviours)
        {
            if (behaviour == null) continue;

            string typeName = behaviour.GetType().FullName;

            if (typeName == "H3M.Core.HologramSource" ||
                typeName == "H3M.Core.HologramRenderer" ||
                typeName == "H3M.Core.HologramAnchor" ||
                typeName == "MetavidoVFX.VFX.VFXBinderManager" ||
                typeName == "MetavidoVFX.VFX.Binders.VFXARDataBinder" ||
                typeName == "VFXBinderManager" ||
                typeName == "VFXARDataBinder")
            {
                Undo.DestroyObjectImmediate(behaviour);
                removed++;
            }
        }

        return removed;
    }
}
