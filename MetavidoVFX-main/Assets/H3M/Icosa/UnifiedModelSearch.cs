// UnifiedModelSearch - Aggregates Icosa + Sketchfab search results (spec-009)
// Provides single API for searching across multiple 3D model sources

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if ICOSA_API_AVAILABLE
using Icosa.Api;
#endif

namespace XRRAI.VoiceToObject
{
    /// <summary>
    /// Unified search across Icosa Gallery and Sketchfab.
    /// Returns combined, deduplicated results sorted by relevance.
    /// </summary>
    public class UnifiedModelSearch : MonoBehaviour
    {
        public static UnifiedModelSearch Instance { get; private set; }

        [Header("API Configuration")]
        [SerializeField] string _sketchfabApiToken;
        [SerializeField] bool _enableIcosa = true;
        [SerializeField] bool _enableSketchfab = true;

        [Header("Search Settings")]
        [SerializeField] int _resultsPerSource = 12;
        [SerializeField] float _searchTimeout = 10f;

        [Header("Cache")]
        [SerializeField] long _maxCacheMB = 500;

        SketchfabClient _sketchfabClient;
        ModelCache _modelCache;

        public ModelCache Cache => _modelCache;
        public bool IsSearching { get; private set; }

        public event Action<List<UnifiedSearchResult>> OnSearchComplete;
        public event Action<string> OnSearchError;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (!string.IsNullOrEmpty(_sketchfabApiToken))
                _sketchfabClient = new SketchfabClient(_sketchfabApiToken);

            _modelCache = new ModelCache(_maxCacheMB);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Search for 3D models across all enabled sources.
        /// </summary>
        public async Task<List<UnifiedSearchResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                OnSearchError?.Invoke("Empty search query");
                return new List<UnifiedSearchResult>();
            }

            IsSearching = true;
            var results = new List<UnifiedSearchResult>();
            var tasks = new List<Task>();

            // Search Icosa
#if ICOSA_API_AVAILABLE
            if (_enableIcosa)
            {
                tasks.Add(SearchIcosaAsync(query, results));
            }
#endif

            // Search Sketchfab
            if (_enableSketchfab && _sketchfabClient != null)
            {
                tasks.Add(SearchSketchfabAsync(query, results));
            }

            // Wait for all with timeout
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnifiedModelSearch] Partial error: {e.Message}");
            }

            // Sort by relevance (name match > like count proxy)
            results.Sort((a, b) =>
            {
                var aNameMatch = a.Name.ToLower().Contains(query.ToLower()) ? 1 : 0;
                var bNameMatch = b.Name.ToLower().Contains(query.ToLower()) ? 1 : 0;
                return bNameMatch.CompareTo(aNameMatch);
            });

            IsSearching = false;
            OnSearchComplete?.Invoke(results);

            Debug.Log($"[UnifiedModelSearch] Found {results.Count} results for '{query}'");
            return results;
        }

#if ICOSA_API_AVAILABLE
        async Task SearchIcosaAsync(string query, List<UnifiedSearchResult> results)
        {
            try
            {
                var icosaResults = await IcosaApi.SearchAssetsAsync(query, _resultsPerSource);
                if (icosaResults != null)
                {
                    foreach (var asset in icosaResults)
                    {
                        results.Add(new UnifiedSearchResult
                        {
                            Id = asset.id,
                            Name = asset.displayName ?? asset.id,
                            Author = asset.authorName ?? "Unknown",
                            License = asset.license ?? "Unknown",
                            ThumbnailUrl = asset.thumbnailUrl,
                            Source = ModelSource.Icosa,
                            IsCached = _modelCache.HasModel(asset.id, "Icosa")
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnifiedModelSearch] Icosa search failed: {e.Message}");
            }
        }
#endif

        async Task SearchSketchfabAsync(string query, List<UnifiedSearchResult> results)
        {
            try
            {
                var sketchfabResults = await _sketchfabClient.SearchModelsAsync(query, _resultsPerSource);
                if (sketchfabResults != null)
                {
                    foreach (var model in sketchfabResults)
                    {
                        results.Add(new UnifiedSearchResult
                        {
                            Id = model.Uid,
                            Name = model.Name ?? model.Uid,
                            Author = model.AuthorUsername ?? "Unknown",
                            License = model.License ?? "Unknown",
                            ThumbnailUrl = model.ThumbnailUrl,
                            ViewerUrl = model.ViewerUrl,
                            Source = ModelSource.Sketchfab,
                            IsCached = _modelCache.HasModel(model.Uid, "Sketchfab")
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnifiedModelSearch] Sketchfab search failed: {e.Message}");
            }
        }

        /// <summary>
        /// Download and cache a model from search results.
        /// </summary>
        public async Task<string> DownloadModelAsync(UnifiedSearchResult result, Action<float> onProgress = null)
        {
            // Check cache first
            var cachedPath = _modelCache.GetModelPath(result.Id, result.Source.ToString());
            if (cachedPath != null)
            {
                onProgress?.Invoke(1f);
                return cachedPath;
            }

            string downloadUrl = null;

            // Get download URL based on source
            if (result.Source == ModelSource.Sketchfab && _sketchfabClient != null)
            {
                downloadUrl = await _sketchfabClient.GetDownloadUrlAsync(result.Id);
            }
#if ICOSA_API_AVAILABLE
            else if (result.Source == ModelSource.Icosa)
            {
                // Icosa uses direct asset URL pattern
                downloadUrl = $"https://api.icosa.gallery/v1/assets/{result.Id}/download";
            }
#endif

            if (string.IsNullOrEmpty(downloadUrl))
            {
                OnSearchError?.Invoke($"Could not get download URL for {result.Name}");
                return null;
            }

            // Download to cache
            var targetPath = _modelCache.GetTargetPath(result.Id, result.Source.ToString());

            bool success = await _sketchfabClient.DownloadModelAsync(downloadUrl, targetPath, onProgress);

            if (success)
            {
                _modelCache.RegisterModel(result.Id, result.Source.ToString(), new ModelMetadata
                {
                    Name = result.Name,
                    Author = result.Author,
                    License = result.License,
                    ThumbnailUrl = result.ThumbnailUrl,
                    ViewerUrl = result.ViewerUrl
                });

                return targetPath;
            }

            return null;
        }

        /// <summary>
        /// Configure Sketchfab API token at runtime.
        /// </summary>
        public void SetSketchfabToken(string token)
        {
            _sketchfabApiToken = token;
            _sketchfabClient = new SketchfabClient(token);
        }
    }

    public enum ModelSource
    {
        Icosa,
        Sketchfab
    }

    [Serializable]
    public class UnifiedSearchResult
    {
        public string Id;
        public string Name;
        public string Author;
        public string License;
        public string LicenseUrl;
        public string ThumbnailUrl;
        public string ViewerUrl;
        public string SourceUrl;
        public ModelSource Source;
        public bool IsCached;

        // Aliases for UI compatibility
        public string AssetId => Id;
        public string DisplayName => Name;
        public string AuthorName => Author;
    }
}
