using UnityEngine;
using UnityEditor;
using System.Linq;

namespace XRRAI.Editor
{
    public static class CleanMissingScripts
    {
        [MenuItem("H3M/Cleanup/Remove Missing Scripts from All Prefabs")]
        public static void CleanAllPrefabs()
        {
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PaintAR" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            int totalRemoved = 0;
            int prefabsFixed = 0;

            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                int removed = RemoveMissingScriptsFromPrefab(prefab, path);
                if (removed > 0)
                {
                    totalRemoved += removed;
                    prefabsFixed++;
                    Debug.Log($"Removed {removed} missing scripts from: {path}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Done! Removed {totalRemoved} missing scripts from {prefabsFixed} prefabs.");
        }

        [MenuItem("H3M/Cleanup/Remove Missing Scripts from Selected")]
        public static void CleanSelected()
        {
            GameObject[] selected = Selection.gameObjects;
            int totalRemoved = 0;

            foreach (GameObject go in selected)
            {
                string path = AssetDatabase.GetAssetPath(go);
                if (string.IsNullOrEmpty(path)) continue;

                int removed = RemoveMissingScriptsFromPrefab(go, path);
                totalRemoved += removed;
                Debug.Log($"Removed {removed} missing scripts from: {path}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Done! Removed {totalRemoved} missing scripts total.");
        }

        private static int RemoveMissingScriptsFromPrefab(GameObject prefab, string path)
        {
            int removed = 0;

            // Get all transforms including nested
            var transforms = prefab.GetComponentsInChildren<Transform>(true);

            foreach (var t in transforms)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count > 0)
                {
                    Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove Missing Scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    removed += count;
                }
            }

            if (removed > 0)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
            }

            return removed;
        }
    }
}
