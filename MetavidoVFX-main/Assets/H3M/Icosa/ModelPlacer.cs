using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace XRRAI.VoiceToObject
{
    /// <summary>
    /// AR placement for downloaded 3D models.
    /// Spec-009: Places models on detected planes or at fixed distance.
    /// </summary>
    public class ModelPlacer : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] ARRaycastManager _raycastManager;
        [SerializeField] Camera _arCamera;

        [Header("Placement Settings")]
        [SerializeField] float _defaultDistance = 1.5f;
        [SerializeField] float _modelScale = 0.3f;
        [SerializeField] LayerMask _placementLayers = -1;

        [Header("Dependencies")]
        [SerializeField] UnifiedModelSearch _searchManager;

        List<ARRaycastHit> _hits = new List<ARRaycastHit>();
        GameObject _currentModel;

        void Awake()
        {
            if (_arCamera == null)
                _arCamera = Camera.main;
            if (_raycastManager == null)
                _raycastManager = FindObjectOfType<ARRaycastManager>();
            if (_searchManager == null)
                _searchManager = FindObjectOfType<UnifiedModelSearch>();
        }

        /// <summary>
        /// Download and place a model from search result.
        /// </summary>
        public async Task<GameObject> PlaceModelAsync(UnifiedSearchResult result)
        {
            if (result == null) return null;

            // Get placement position
            Vector3 position = GetPlacementPosition();
            Quaternion rotation = GetPlacementRotation(position);

            // Check cache first
            string cachedPath = _searchManager?.Cache?.GetModelPath(result.AssetId, result.Source.ToString());
            GameObject model = null;

            if (!string.IsNullOrEmpty(cachedPath))
            {
                // Load from cache
                model = await LoadModelFromCacheAsync(cachedPath, result);
            }
            else
            {
                // Download and cache
                model = await DownloadAndLoadModelAsync(result);
            }

            if (model != null)
            {
                model.transform.position = position;
                model.transform.rotation = rotation;
                model.transform.localScale = Vector3.one * _modelScale;

                // Attach metadata
                var metadata = model.AddComponent<IcosaAssetMetadata>();
                metadata.Initialize(result);

                _currentModel = model;
                Debug.Log($"[ModelPlacer] Placed: {result.DisplayName} at {position}");
            }

            return model;
        }

        Vector3 GetPlacementPosition()
        {
            if (_arCamera == null)
                return Vector3.forward * _defaultDistance;

            // Try AR plane hit
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (_raycastManager != null && _raycastManager.Raycast(screenCenter, _hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                return _hits[0].pose.position;
            }

            // Fallback: fixed distance in front of camera
            return _arCamera.transform.position + _arCamera.transform.forward * _defaultDistance;
        }

        Quaternion GetPlacementRotation(Vector3 position)
        {
            if (_arCamera == null) return Quaternion.identity;

            Vector3 toCamera = _arCamera.transform.position - position;
            toCamera.y = 0;
            if (toCamera.sqrMagnitude > 0.001f)
                return Quaternion.LookRotation(toCamera);
            return Quaternion.identity;
        }

        async Task<GameObject> LoadModelFromCacheAsync(string path, UnifiedSearchResult result)
        {
            // Use GLTFast or UnityGLTF to load
#if GLTFAST_AVAILABLE
            var gltf = new GLTFast.GltfImport();
            bool success = await gltf.Load(path);
            if (success)
            {
                var go = new GameObject(result.DisplayName);
                await gltf.InstantiateMainSceneAsync(go.transform);
                return go;
            }
#endif
            // Fallback: create placeholder
            return CreatePlaceholder(result.DisplayName);
        }

        async Task<GameObject> DownloadAndLoadModelAsync(UnifiedSearchResult result)
        {
            // Download via appropriate API
            byte[] modelData = null;

            if (result.Source == ModelSource.Sketchfab)
            {
                // Sketchfab download requires OAuth - simplified for now
                Debug.LogWarning("[ModelPlacer] Sketchfab download requires API token");
            }
            else if (result.Source == ModelSource.Icosa)
            {
#if ICOSA_API_AVAILABLE
                // Use Icosa API
                var api = new Icosa.Api.IcosaApi();
                var download = await api.GetAssetDownloadAsync(result.AssetId);
                // Download file...
#endif
            }

            // Cache if successful
            if (modelData != null && _searchManager?.Cache != null)
            {
                string cachedPath = _searchManager.Cache.AddModel(result.AssetId, result.Source.ToString(), modelData);
                return await LoadModelFromCacheAsync(cachedPath, result);
            }

            // Fallback placeholder
            return CreatePlaceholder(result.DisplayName);
        }

        GameObject CreatePlaceholder(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = Vector3.one * 0.2f;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            }

            return go;
        }

        /// <summary>
        /// Remove the currently placed model.
        /// </summary>
        public void RemoveCurrentModel()
        {
            if (_currentModel != null)
            {
                Destroy(_currentModel);
                _currentModel = null;
            }
        }
    }
}
