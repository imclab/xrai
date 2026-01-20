using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using MyakuMyakuAR;
using MetavidoVFX;
using TextureSource;

namespace MetavidoVFX.Editor
{
    /// <summary>
    /// Editor utility to set up MyakuMyaku VFX in scene.
    /// </summary>
    public static class MyakuMyakuSetup
    {
        [MenuItem("H3M/MyakuMyaku/Setup MyakuMyaku VFX (AR Foundation)")]
        public static void SetupMyakuMyakuVFX()
        {
            // Find or create GameObject
            var go = GameObject.Find("MyakuMyaku_VFX");
            if (go == null)
            {
                go = new GameObject("MyakuMyaku_VFX");
                Debug.Log("[MyakuMyakuSetup] Created MyakuMyaku_VFX GameObject");
            }

            // Add VisualEffect component
            var vfx = go.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                vfx = go.AddComponent<VisualEffect>();
                Debug.Log("[MyakuMyakuSetup] Added VisualEffect component");
            }

            // Load and assign VFX asset
            var vfxAsset = Resources.Load<VisualEffectAsset>("VFX/Myaku/myakumyaku_ar_myaku");
            if (vfxAsset != null)
            {
                vfx.visualEffectAsset = vfxAsset;
                Debug.Log($"[MyakuMyakuSetup] Assigned VFX asset: {vfxAsset.name}");
            }
            else
            {
                Debug.LogWarning("[MyakuMyakuSetup] Could not find VFX asset at Resources/VFX/Myaku/myakumyaku_ar_myaku");
            }

            // Add MyakuMyakuBinder (AR Foundation approach)
            var binder = go.GetComponent<MyakuMyakuBinder>();
            if (binder == null)
            {
                binder = go.AddComponent<MyakuMyakuBinder>();
                Debug.Log("[MyakuMyakuSetup] Added MyakuMyakuBinder component");
            }

            // Select the object
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);

            Debug.Log("[MyakuMyakuSetup] MyakuMyaku VFX setup complete (AR Foundation mode)!");
        }

        [MenuItem("H3M/MyakuMyaku/Setup MyakuMyaku VFX (YOLO11)")]
        public static void SetupMyakuMyakuYOLO()
        {
            // Find or create GameObject
            var go = GameObject.Find("MyakuMyaku_VFX");
            if (go == null)
            {
                go = new GameObject("MyakuMyaku_VFX");
                Debug.Log("[MyakuMyakuSetup] Created MyakuMyaku_VFX GameObject");
            }

            // Remove AR Foundation binder if exists
            var oldBinder = go.GetComponent<MyakuMyakuBinder>();
            if (oldBinder != null)
            {
                Object.DestroyImmediate(oldBinder);
                Debug.Log("[MyakuMyakuSetup] Removed MyakuMyakuBinder");
            }

            // Add VisualEffect component
            var vfx = go.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                vfx = go.AddComponent<VisualEffect>();
                Debug.Log("[MyakuMyakuSetup] Added VisualEffect component");
            }

            // Load and assign VFX asset
            var vfxAsset = Resources.Load<VisualEffectAsset>("VFX/Myaku/myakumyaku_ar_myaku");
            if (vfxAsset != null)
            {
                vfx.visualEffectAsset = vfxAsset;
                Debug.Log($"[MyakuMyakuSetup] Assigned VFX asset: {vfxAsset.name}");
            }

            // Add VirtualTextureSource (required for YOLO11 to get AR camera feed)
            var texSource = go.GetComponent<VirtualTextureSource>();
            if (texSource == null)
            {
                texSource = go.AddComponent<VirtualTextureSource>();
                Debug.Log("[MyakuMyakuSetup] Added VirtualTextureSource");
            }

            // Wire ARFoundationTextureSource to VirtualTextureSource
            var arTexSource = AssetDatabase.LoadAssetAtPath<BaseTextureSource>("Assets/Settings/ARFoundationTextureSource.asset");
            if (arTexSource != null)
            {
                var texSourceSo = new SerializedObject(texSource);
                var sourceProp = texSourceSo.FindProperty("source");
                if (sourceProp != null)
                {
                    sourceProp.objectReferenceValue = arTexSource;
                    texSourceSo.ApplyModifiedProperties();
                    Debug.Log("[MyakuMyakuSetup] Wired ARFoundationTextureSource to VirtualTextureSource");
                }
            }
            else
            {
                Debug.LogWarning("[MyakuMyakuSetup] ARFoundationTextureSource.asset not found in Assets/Settings/");
            }

            // Add YOLO11 controller
            var yolo = go.GetComponent<Yolo11SegARController>();
            if (yolo == null)
            {
                yolo = go.AddComponent<Yolo11SegARController>();
                Debug.Log("[MyakuMyakuSetup] Added Yolo11SegARController");
            }

            // Disable debug UI (no UI elements assigned)
            var yoloSo = new SerializedObject(yolo);
            var showDebugProp = yoloSo.FindProperty("showDebugUI");
            if (showDebugProp != null)
            {
                showDebugProp.boolValue = false;
                yoloSo.ApplyModifiedProperties();
                Debug.Log("[MyakuMyakuSetup] Disabled YOLO11 debug UI");
            }

            // Remove Yolo11VFXBinder if exists (MainController handles binding)
            var oldVfxBinder = go.GetComponent<Yolo11VFXBinder>();
            if (oldVfxBinder != null)
            {
                Object.DestroyImmediate(oldVfxBinder);
                Debug.Log("[MyakuMyakuSetup] Removed Yolo11VFXBinder (using MainController instead)");
            }

            // Add MainController (binds YOLO → VFX + postVfxMaterial)
            var main = go.GetComponent<MainController>();
            if (main == null)
            {
                main = go.AddComponent<MainController>();
                Debug.Log("[MyakuMyakuSetup] Added MainController");
            }

            // Wire MainController
            var mainSo = new SerializedObject(main);

            // Wire VFX reference
            var vfxProp = mainSo.FindProperty("vfx");
            if (vfxProp != null)
            {
                vfxProp.objectReferenceValue = vfx;
            }

            // Wire postVfxMaterial
            var postVfxMat = Resources.Load<Material>("VFX/Myaku/Shaders/M_Metaball2DPass");
            var matProp = mainSo.FindProperty("postVfxMaterial");
            if (matProp != null && postVfxMat != null)
            {
                matProp.objectReferenceValue = postVfxMat;
                Debug.Log("[MyakuMyakuSetup] Wired postVfxMaterial");
            }
            else if (postVfxMat == null)
            {
                Debug.LogWarning("[MyakuMyakuSetup] Could not find M_Metaball2DPass material");
            }

            mainSo.ApplyModifiedProperties();
            Debug.Log("[MyakuMyakuSetup] Wired MainController to VFX");

            // Select the object
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);

            Debug.Log("[MyakuMyakuSetup] MyakuMyaku VFX setup complete (YOLO11 mode)!");
            Debug.Log("[MyakuMyakuSetup] Note: YOLO11 model will download on first run (~11MB)");
        }

        [MenuItem("H3M/MyakuMyaku/Verify MyakuMyaku Setup")]
        public static void VerifySetup()
        {
            var go = GameObject.Find("MyakuMyaku_VFX");
            if (go == null)
            {
                Debug.LogError("[MyakuMyakuSetup] MyakuMyaku_VFX not found in scene");
                return;
            }

            var vfx = go.GetComponent<VisualEffect>();
            if (vfx == null)
            {
                Debug.LogError("[MyakuMyakuSetup] VisualEffect component missing");
                return;
            }

            if (vfx.visualEffectAsset == null)
            {
                Debug.LogError("[MyakuMyakuSetup] VFX asset not assigned");
                return;
            }

            // Check for YOLO11 mode first
            var yolo = go.GetComponent<Yolo11SegARController>();
            var mainController = go.GetComponent<MainController>();
            var texSource = go.GetComponent<VirtualTextureSource>();

            if (yolo != null)
            {
                // YOLO11 mode
                if (mainController == null)
                    Debug.LogWarning("[MyakuMyakuSetup] MainController missing - run Setup YOLO11");
                if (texSource == null)
                    Debug.LogWarning("[MyakuMyakuSetup] VirtualTextureSource missing - camera won't feed YOLO");

                Debug.Log($"[MyakuMyakuSetup] YOLO11 mode verified: VFX={vfx.visualEffectAsset.name}, " +
                          $"YOLO={yolo != null}, MainController={mainController != null}, TextureSource={texSource != null}");
                return;
            }

            // Check for AR Foundation mode
            var arBinder = go.GetComponent<MyakuMyakuBinder>();
            if (arBinder == null)
            {
                Debug.LogWarning("[MyakuMyakuSetup] No binder found - run Setup (AR Foundation) or Setup (YOLO11)");
                return;
            }

            Debug.Log($"[MyakuMyakuSetup] AR Foundation mode verified: VFX={vfx.visualEffectAsset.name}, Binder={arBinder != null}");
        }

        [MenuItem("H3M/MyakuMyaku/Re-wire YOLO11 Components")]
        public static void RewireYolo11()
        {
            var go = GameObject.Find("MyakuMyaku_VFX");
            if (go == null)
            {
                Debug.LogError("[MyakuMyakuSetup] MyakuMyaku_VFX not found in scene");
                return;
            }

            var yolo = go.GetComponent<Yolo11SegARController>();
            var main = go.GetComponent<MainController>();
            var vfx = go.GetComponent<VisualEffect>();

            if (yolo == null || main == null || vfx == null)
            {
                Debug.LogError("[MyakuMyakuSetup] YOLO11 components not found - run Setup YOLO11 first");
                return;
            }

            // Wire MainController
            var mainSo = new SerializedObject(main);

            // Wire VFX reference
            var vfxProp = mainSo.FindProperty("vfx");
            if (vfxProp != null)
            {
                vfxProp.objectReferenceValue = vfx;
            }

            // Wire postVfxMaterial
            var postVfxMat = Resources.Load<Material>("VFX/Myaku/Shaders/M_Metaball2DPass");
            var matProp = mainSo.FindProperty("postVfxMaterial");
            if (matProp != null && postVfxMat != null)
            {
                matProp.objectReferenceValue = postVfxMat;
            }

            mainSo.ApplyModifiedProperties();

            // Disable debug UI
            var yoloSo = new SerializedObject(yolo);
            var showDebugProp = yoloSo.FindProperty("showDebugUI");
            if (showDebugProp != null)
            {
                showDebugProp.boolValue = false;
                yoloSo.ApplyModifiedProperties();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("[MyakuMyakuSetup] YOLO11 components re-wired successfully");
        }
    }
}
