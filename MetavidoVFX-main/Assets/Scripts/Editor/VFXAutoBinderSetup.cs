// VFXAutoBinderSetup - Editor tool to auto-add appropriate binders to VFX
// Menu: H3M > VFX > Auto-Setup Binders

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using XRRAI.VFXBinders;
using System.Collections.Generic;

namespace XRRAI.Editor
{
    public class VFXAutoBinderSetup : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<VFXAnalysis> _analyses = new List<VFXAnalysis>();
        private bool _includeInactive = true;

        private class VFXAnalysis
        {
            public VisualEffect vfx;
            public string name;
            public bool needsAR;
            public bool needsAudio;
            public bool needsHand;
            public bool hasARBinder;
            public bool hasLegacyARBinder;
            public bool hasAudioBinder;
            public bool hasHandBinder;
            public bool hasPropertyBinder;
            public List<string> detectedProperties = new List<string>();
        }

        [MenuItem("H3M/VFX/Auto-Setup Binders", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<VFXAutoBinderSetup>("VFX Binder Setup");
            window.minSize = new Vector2(450, 400);
            window.AnalyzeScene();
        }

        [MenuItem("H3M/VFX/Add Binders to Selected", false, 201)]
        public static void AddBindersToSelected()
        {
            int count = 0;
            foreach (var go in Selection.gameObjects)
            {
                var vfx = go.GetComponent<VisualEffect>();
                if (vfx != null)
                {
                    VFXBinderUtility.SetupVFXAuto(vfx);
                    count++;
                    EditorUtility.SetDirty(go);
                }
            }
            Debug.Log($"[VFXAutoBinderSetup] Added binders to {count} VFX");
        }

        /// <summary>
        /// One-click setup: Add binders to ALL VFX in scene that need them
        /// No UI window required - runs immediately
        /// </summary>
        [MenuItem("H3M/VFX/Auto-Setup ALL VFX (One-Click)", false, 199)]
        public static void AutoSetupAllVFX()
        {
            var allVFX = FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int setupCount = 0;
            int skippedCount = 0;

            Debug.Log($"[VFXAutoBinderSetup] Scanning {allVFX.Length} VFX in scene...");

            foreach (var vfx in allVFX)
            {
                // Detect what bindings this VFX needs
                var preset = VFXBinderUtility.DetectPreset(vfx);

                if (preset == VFXBinderPreset.None)
                {
                    skippedCount++;
                    continue;
                }

                // Check if already has required binders
                bool needsSetup = false;
                bool hasAR = vfx.GetComponent<VFXARBinder>() != null;
                
                // Use reflection for legacy binder to avoid compile dependency
                var legacyType = System.Type.GetType("XRRAI.VFXBinders.VFXARDataBinder, Assembly-CSharp");
                bool hasLegacyAR = legacyType != null && vfx.GetComponent(legacyType) != null;
                
                bool hasAudio = vfx.GetComponent<VFXAudioDataBinder>() != null;

                switch (preset)
                {
                    case VFXBinderPreset.AROnly:
                    case VFXBinderPreset.ARWithAudio:
                    case VFXBinderPreset.ARWithHand:
                    case VFXBinderPreset.Full:
                        needsSetup = !hasAR || hasLegacyAR;
                        break;
                    case VFXBinderPreset.AudioOnly:
                        needsSetup = !hasAudio;
                        break;
                }

                if (!needsSetup)
                {
                    skippedCount++;
                    continue;
                }

                // Setup the VFX
                Undo.RecordObject(vfx.gameObject, "Auto-Setup VFX Binders");
                VFXBinderUtility.SetupVFXAuto(vfx);
                EditorUtility.SetDirty(vfx.gameObject);
                setupCount++;

                Debug.Log($"  [{preset}] {vfx.gameObject.name}");
            }

            Debug.Log($"[VFXAutoBinderSetup] ✓ Setup {setupCount} VFX | Skipped {skippedCount} (no AR properties or already setup)");

            if (setupCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("VFX Auto-Binder Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Analyzes all VFX in the scene and recommends/adds appropriate data binders " +
                "(AR depth, audio, hand tracking) based on their exposed properties.",
                MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            _includeInactive = EditorGUILayout.Toggle("Include Inactive", _includeInactive);
            if (GUILayout.Button("Refresh Analysis", GUILayout.Width(120)))
            {
                AnalyzeScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Summary
            int needsSetup = 0;
            foreach (var a in _analyses)
            {
                if ((a.needsAR && !a.hasARBinder) ||
                    (a.needsAudio && !a.hasAudioBinder) ||
                    (a.needsHand && !a.hasHandBinder))
                    needsSetup++;
            }

            EditorGUILayout.LabelField($"Found {_analyses.Count} VFX | {needsSetup} need binder setup");

            EditorGUILayout.Space();

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup All Missing Binders"))
            {
                SetupAllMissing();
            }
            if (GUILayout.Button("Remove All Binders"))
            {
                RemoveAllBinders();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Scrollable list
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var analysis in _analyses)
            {
                DrawVFXAnalysis(analysis);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawVFXAnalysis(VFXAnalysis a)
        {
            bool needsAny = (a.needsAR && !a.hasARBinder) ||
                           (a.needsAudio && !a.hasAudioBinder) ||
                           (a.needsHand && !a.hasHandBinder);

            Color bgColor = needsAny ? new Color(1f, 0.9f, 0.8f) : new Color(0.9f, 1f, 0.9f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bgColor;

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(a.name, EditorStyles.boldLabel);
            if (a.vfx != null && GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = a.vfx.gameObject;
            }
            EditorGUILayout.EndHorizontal();

            // Properties detected
            EditorGUI.indentLevel++;
            if (a.detectedProperties.Count > 0)
            {
                EditorGUILayout.LabelField($"Properties: {string.Join(", ", a.detectedProperties)}", EditorStyles.miniLabel);
            }

            // Status icons
            EditorGUILayout.BeginHorizontal();

            // AR Status
            DrawBinderStatus("AR", a.needsAR, a.hasARBinder);
            DrawBinderStatus("Audio", a.needsAudio, a.hasAudioBinder);
            DrawBinderStatus("Hand", a.needsHand, a.hasHandBinder);

            GUILayout.FlexibleSpace();

            // Setup button for individual VFX
            if (needsAny && GUILayout.Button("Setup", GUILayout.Width(60)))
            {
                SetupVFX(a);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        void DrawBinderStatus(string label, bool needed, bool hasBinder)
        {
            string status;
            Color color;

            if (!needed)
            {
                status = $"{label}: -";
                color = Color.gray;
            }
            else if (hasBinder)
            {
                status = $"{label}: \u2713";
                color = Color.green;
            }
            else
            {
                status = $"{label}: \u2717";
                color = Color.red;
            }

            GUI.contentColor = color;
            EditorGUILayout.LabelField(status, GUILayout.Width(70));
            GUI.contentColor = Color.white;
        }

        void AnalyzeScene()
        {
            _analyses.Clear();

            var findMode = _includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            var allVFX = FindObjectsByType<VisualEffect>(findMode, FindObjectsSortMode.None);

            foreach (var vfx in allVFX)
            {
                var analysis = AnalyzeVFX(vfx);
                _analyses.Add(analysis);
            }

            // Sort: needs setup first, then by name
            _analyses.Sort((a, b) =>
            {
                bool aNeedsSetup = (a.needsAR && !a.hasARBinder) || (a.needsAudio && !a.hasAudioBinder) || (a.needsHand && !a.hasHandBinder);
                bool bNeedsSetup = (b.needsAR && !b.hasARBinder) || (b.needsAudio && !b.hasAudioBinder) || (b.needsHand && !b.hasHandBinder);
                if (aNeedsSetup != bNeedsSetup) return bNeedsSetup.CompareTo(aNeedsSetup);
                return a.name.CompareTo(b.name);
            });
        }

        VFXAnalysis AnalyzeVFX(VisualEffect vfx)
        {
            var a = new VFXAnalysis
            {
                vfx = vfx,
                name = vfx.gameObject.name
            };

            // Detect AR needs
            if (vfx.HasTexture("DepthMap") || vfx.HasTexture("DepthTexture"))
            {
                a.needsAR = true;
                a.detectedProperties.Add("DepthMap");
            }
            if (vfx.HasTexture("StencilMap") || vfx.HasTexture("HumanStencil"))
            {
                a.needsAR = true;
                a.detectedProperties.Add("StencilMap");
            }
            if (vfx.HasTexture("PositionMap"))
            {
                a.needsAR = true;
                a.detectedProperties.Add("PositionMap");
            }
            if (vfx.HasTexture("ColorMap") || vfx.HasTexture("ColorTexture"))
            {
                a.needsAR = true;
                a.detectedProperties.Add("ColorMap");
            }
            if (vfx.HasMatrix4x4("InverseView") || vfx.HasVector4("RayParams"))
            {
                a.needsAR = true;
                a.detectedProperties.Add("InverseView/RayParams");
            }

            // Detect Audio needs
            if (vfx.HasFloat("AudioVolume"))
            {
                a.needsAudio = true;
                a.detectedProperties.Add("AudioVolume");
            }
            if (vfx.HasFloat("AudioBass") || vfx.HasFloat("AudioMid") || vfx.HasFloat("AudioTreble"))
            {
                a.needsAudio = true;
                a.detectedProperties.Add("AudioBands");
            }

            // Detect Hand needs
            if (vfx.HasVector3("HandPosition") || vfx.HasVector3("HandVelocity"))
            {
                a.needsHand = true;
                a.detectedProperties.Add("HandPosition/Velocity");
            }
            if (vfx.HasFloat("BrushWidth") || vfx.HasBool("IsPinching"))
            {
                a.needsHand = true;
                a.detectedProperties.Add("BrushWidth/Pinch");
            }

            // Check existing binders
            a.hasPropertyBinder = vfx.GetComponent<VFXPropertyBinder>() != null;
            a.hasARBinder = vfx.GetComponent<VFXARBinder>() != null;
            
            // Use reflection for legacy binder
            var legacyType = System.Type.GetType("XRRAI.VFXBinders.VFXARDataBinder, Assembly-CSharp");
            a.hasLegacyARBinder = legacyType != null && vfx.GetComponent(legacyType) != null;
            
            a.hasAudioBinder = vfx.GetComponent<VFXAudioDataBinder>() != null;

            if (a.hasLegacyARBinder && !a.hasARBinder)
            {
                a.detectedProperties.Add("LegacyARBinder");
            }

            // Check for hand binder via reflection (compilation order issue)
            var handType = System.Type.GetType("XRRAI.VFXBinders.VFXHandDataBinder, Assembly-CSharp");
            if (handType != null)
            {
                a.hasHandBinder = vfx.GetComponent(handType) != null;
            }

            return a;
        }

        void SetupVFX(VFXAnalysis a)
        {
            if (a.vfx == null) return;

            Undo.RecordObject(a.vfx.gameObject, "Setup VFX Binders");

            // Ensure VFXPropertyBinder
            if (!a.hasPropertyBinder)
            {
                Undo.AddComponent<VFXPropertyBinder>(a.vfx.gameObject);
            }

            // Add missing binders
            if (a.needsAR && !a.hasARBinder)
            {
                // Use reflection for legacy binder
                var legacyType = System.Type.GetType("XRRAI.VFXBinders.VFXARDataBinder, Assembly-CSharp");
                var legacy = legacyType != null ? a.vfx.GetComponent(legacyType) : null;
                
                if (legacy != null)
                {
                    Undo.DestroyObjectImmediate(legacy);
                }
                Undo.AddComponent<VFXARBinder>(a.vfx.gameObject);
            }
            if (a.needsAudio && !a.hasAudioBinder)
            {
                Undo.AddComponent<VFXAudioDataBinder>(a.vfx.gameObject);
            }
            if (a.needsHand && !a.hasHandBinder)
            {
                VFXBinderUtility.AddHandBinder(a.vfx.gameObject);
            }

            EditorUtility.SetDirty(a.vfx.gameObject);
            AnalyzeScene(); // Refresh
        }

        void SetupAllMissing()
        {
            int count = 0;
            foreach (var a in _analyses)
            {
                bool needsAny = (a.needsAR && !a.hasARBinder) ||
                               (a.needsAudio && !a.hasAudioBinder) ||
                               (a.needsHand && !a.hasHandBinder);
                if (needsAny)
                {
                    SetupVFX(a);
                    count++;
                }
            }
            Debug.Log($"[VFXAutoBinderSetup] Setup binders for {count} VFX");
            AnalyzeScene();
        }

        void RemoveAllBinders()
        {
            int count = 0;
            foreach (var a in _analyses)
            {
                if (a.vfx == null) continue;
                VFXBinderUtility.ClearBinders(a.vfx.gameObject);
                EditorUtility.SetDirty(a.vfx.gameObject);
                count++;
            }
            Debug.Log($"[VFXAutoBinderSetup] Removed binders from {count} VFX");
            AnalyzeScene();
        }
    }
}
