// NNCam VFX Subgraph Swapper
// Editor utility to swap "Get Keypoint" subgraph with "Get Keypoint World" subgraph
// This adds world-space positioning support to keypoint-based VFX

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

namespace MetavidoVFX.NNCam.Editor
{
    public static class NNCamVFXSubgraphSwapper
    {
        // Subgraph GUIDs
        const string GET_KEYPOINT_GUID = "d165f6ed4dc68443e9de23907e00d7bb";
        const string GET_KEYPOINT_WORLD_GUID = "69374374b83d4400db2bb56f0970b48d";

        [MenuItem("H3M/NNCam/Upgrade Eyes VFX to World-Space")]
        public static void UpgradeEyesVFXToWorldSpace()
        {
            string vfxPath = "Assets/Resources/VFX/NNCam2/eyes_any_nncam2.vfx";
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);

            if (vfxAsset == null)
            {
                Debug.LogError($"[NNCamVFXSubgraphSwapper] Could not load VFX at: {vfxPath}");
                return;
            }

            // VFX Graph API doesn't expose subgraph swapping directly
            // We need to open the VFX in the editor and guide the user
            Debug.Log("[NNCamVFXSubgraphSwapper] Opening eyes_any_nncam2.vfx in VFX Graph Editor...");
            Debug.Log("[NNCamVFXSubgraphSwapper] Manual wiring steps:");
            Debug.Log("  1. Find the 'Get Keypoint' subgraph node (near position 86, 922)");
            Debug.Log("  2. Delete it");
            Debug.Log("  3. Right-click > Create Node > search 'Get Keypoint World'");
            Debug.Log("  4. Connect: KeypointBuffer -> Buffer input");
            Debug.Log("  5. Connect: Eye index constant -> Index input");
            Debug.Log("  6. Connect: PositionMap (exposed property) -> PositionMap input");
            Debug.Log("  7. Connect outputs to existing Set Position/Set Attribute nodes");
            Debug.Log("  8. Save (Ctrl+S)");

            // Open the VFX in editor
            AssetDatabase.OpenAsset(vfxAsset);

            // Also select the Get Keypoint World subgraph for easy drag-drop
            string subgraphPath = "Assets/VFX/NNCam2/Get Keypoint World.vfxoperator";
            var subgraph = AssetDatabase.LoadAssetAtPath<Object>(subgraphPath);
            if (subgraph != null)
            {
                EditorGUIUtility.PingObject(subgraph);
                Debug.Log($"[NNCamVFXSubgraphSwapper] Pinged subgraph at: {subgraphPath}");
                Debug.Log("[NNCamVFXSubgraphSwapper] TIP: Drag the highlighted 'Get Keypoint World' subgraph into the VFX Graph!");
            }
        }

        [MenuItem("H3M/NNCam/Check Subgraph Availability")]
        public static void CheckSubgraphAvailability()
        {
            Debug.Log("[NNCamVFXSubgraphSwapper] Checking NNCam2 subgraphs...");

            // Check Get Keypoint
            string getKeypointPath = AssetDatabase.GUIDToAssetPath(GET_KEYPOINT_GUID);
            if (!string.IsNullOrEmpty(getKeypointPath))
            {
                Debug.Log($"  [OK] Get Keypoint: {getKeypointPath}");
            }
            else
            {
                Debug.LogWarning("  [MISSING] Get Keypoint subgraph not found!");
            }

            // Check Get Keypoint World
            string getKeypointWorldPath = AssetDatabase.GUIDToAssetPath(GET_KEYPOINT_WORLD_GUID);
            if (!string.IsNullOrEmpty(getKeypointWorldPath))
            {
                Debug.Log($"  [OK] Get Keypoint World: {getKeypointWorldPath}");
            }
            else
            {
                Debug.LogWarning("  [MISSING] Get Keypoint World subgraph not found!");
            }

            // Check eyes VFX
            string eyesVfxPath = "Assets/Resources/VFX/NNCam2/eyes_any_nncam2.vfx";
            var eyesVfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(eyesVfxPath);
            if (eyesVfx != null)
            {
                Debug.Log($"  [OK] Eyes VFX: {eyesVfxPath}");

                // Check if it has PositionMap exposed by reading the YAML
                string fullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), eyesVfxPath);
                if (System.IO.File.Exists(fullPath))
                {
                    string content = System.IO.File.ReadAllText(fullPath);
                    bool hasPositionMap = content.Contains("name: PositionMap");
                    if (hasPositionMap)
                    {
                        Debug.Log("  [OK] Eyes VFX has PositionMap property exposed");
                    }
                    else
                    {
                        Debug.LogWarning("  [WARN] Eyes VFX does NOT have PositionMap property - add it first!");
                    }
                }
            }
            else
            {
                Debug.LogError($"  [MISSING] Eyes VFX not found at: {eyesVfxPath}");
            }
        }

        [MenuItem("H3M/NNCam/Wire PositionMap to Eyes VFX (Automated)")]
        public static void WirePositionMapToEyesVFX()
        {
            // This attempts to modify the VFX YAML directly
            // WARNING: VFX YAML is complex; this is a best-effort automation

            string vfxPath = "Assets/Resources/VFX/NNCam2/eyes_any_nncam2.vfx";
            string fullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), vfxPath);

            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError($"[NNCamVFXSubgraphSwapper] VFX file not found: {fullPath}");
                return;
            }

            string content = System.IO.File.ReadAllText(fullPath);

            // Check if already using Get Keypoint World
            if (content.Contains(GET_KEYPOINT_WORLD_GUID))
            {
                Debug.Log("[NNCamVFXSubgraphSwapper] Eyes VFX already uses 'Get Keypoint World' subgraph!");
                return;
            }

            if (!content.Contains(GET_KEYPOINT_GUID))
            {
                Debug.LogWarning("[NNCamVFXSubgraphSwapper] Eyes VFX doesn't use 'Get Keypoint' subgraph - nothing to swap");
                return;
            }

            // The swap requires changing:
            // 1. Subgraph GUID reference
            // 2. Third input slot from float to Texture2D (different script GUID)
            // 3. SubgraphDependencies entry
            // 4. Creating PositionMap parameter node link

            // This is complex because the slot types have different script GUIDs:
            // - Float slot: f780aa281814f9842a7c076d436932e7
            // - Texture2D slot: a9f510a700bf40e4fafc50a9bbd936d7 (or similar)

            Debug.LogWarning("[NNCamVFXSubgraphSwapper] Automated YAML swapping is risky.");
            Debug.LogWarning("Please use 'Upgrade Eyes VFX to World-Space' menu for guided manual wiring.");
            Debug.LogWarning("The subgraph input types differ (float vs Texture2D), requiring careful slot rewiring.");

            // Open the manual wiring helper instead
            UpgradeEyesVFXToWorldSpace();
        }
    }
}
