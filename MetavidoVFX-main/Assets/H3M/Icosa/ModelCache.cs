// ModelCache - LRU disk cache for downloaded 3D models (spec-009)
// Manages local storage with automatic eviction when exceeding size limit

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace XRRAI.VoiceToObject
{
    /// <summary>
    /// LRU cache for downloaded 3D models with automatic eviction.
    /// Stores models in Application.persistentDataPath/ModelCache/
    /// </summary>
    public class ModelCache
    {
        const string CACHE_FOLDER = "ModelCache";
        const string INDEX_FILE = "index.json";

        string _cacheRoot;
        long _maxCacheBytes;
        CacheIndex _index;

        public string CacheRoot => _cacheRoot;
        public long MaxCacheBytes => _maxCacheBytes;
        public long CurrentSizeBytes => _index?.TotalSize ?? 0;
        public int CachedModelCount => _index?.Entries.Count ?? 0;

        public ModelCache(long maxCacheMB = 500)
        {
            _cacheRoot = Path.Combine(Application.persistentDataPath, CACHE_FOLDER);
            _maxCacheBytes = maxCacheMB * 1024 * 1024;

            if (!Directory.Exists(_cacheRoot))
                Directory.CreateDirectory(_cacheRoot);

            LoadIndex();
        }

        /// <summary>
        /// Check if a model is cached.
        /// </summary>
        public bool HasModel(string modelId, string source)
        {
            var key = GetCacheKey(modelId, source);
            return _index.Entries.ContainsKey(key) && File.Exists(GetModelPath(key));
        }

        /// <summary>
        /// Get cached model path. Returns null if not cached.
        /// Updates last access time for LRU.
        /// </summary>
        public string GetModelPath(string modelId, string source)
        {
            var key = GetCacheKey(modelId, source);
            if (!_index.Entries.TryGetValue(key, out var entry))
                return null;

            var path = GetModelPath(key);
            if (!File.Exists(path))
            {
                _index.Entries.Remove(key);
                SaveIndex();
                return null;
            }

            // Update LRU timestamp
            entry.LastAccess = DateTime.UtcNow.Ticks;
            SaveIndex();

            return path;
        }

        /// <summary>
        /// Add a model to the cache. Evicts old entries if needed.
        /// </summary>
        public string AddModel(string modelId, string source, byte[] data, ModelMetadata metadata = null)
        {
            var key = GetCacheKey(modelId, source);
            var path = GetModelPath(key);
            var size = data.LongLength;

            // Evict if needed
            EnsureCapacity(size);

            // Write file
            File.WriteAllBytes(path, data);

            // Update index
            _index.Entries[key] = new CacheEntry
            {
                ModelId = modelId,
                Source = source,
                FilePath = path,
                Size = size,
                LastAccess = DateTime.UtcNow.Ticks,
                Metadata = metadata
            };
            _index.TotalSize += size;

            SaveIndex();
            return path;
        }

        /// <summary>
        /// Get the path where a model would be stored (for direct download).
        /// </summary>
        public string GetTargetPath(string modelId, string source)
        {
            var key = GetCacheKey(modelId, source);
            return GetModelPath(key);
        }

        /// <summary>
        /// Register a model that was downloaded directly to disk.
        /// </summary>
        public void RegisterModel(string modelId, string source, ModelMetadata metadata = null)
        {
            var key = GetCacheKey(modelId, source);
            var path = GetModelPath(key);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ModelCache] File not found: {path}");
                return;
            }

            var size = new FileInfo(path).Length;
            EnsureCapacity(size);

            _index.Entries[key] = new CacheEntry
            {
                ModelId = modelId,
                Source = source,
                FilePath = path,
                Size = size,
                LastAccess = DateTime.UtcNow.Ticks,
                Metadata = metadata
            };
            _index.TotalSize += size;

            SaveIndex();
        }

        /// <summary>
        /// Remove a specific model from cache.
        /// </summary>
        public void RemoveModel(string modelId, string source)
        {
            var key = GetCacheKey(modelId, source);
            if (!_index.Entries.TryGetValue(key, out var entry))
                return;

            var path = GetModelPath(key);
            if (File.Exists(path))
                File.Delete(path);

            _index.TotalSize -= entry.Size;
            _index.Entries.Remove(key);
            SaveIndex();
        }

        /// <summary>
        /// Clear entire cache.
        /// </summary>
        public void ClearCache()
        {
            foreach (var entry in _index.Entries.Values)
            {
                if (File.Exists(entry.FilePath))
                    File.Delete(entry.FilePath);
            }

            _index = new CacheIndex();
            SaveIndex();

            Debug.Log("[ModelCache] Cache cleared");
        }

        /// <summary>
        /// Get all cached models metadata.
        /// </summary>
        public List<CacheEntry> GetAllEntries()
        {
            return _index.Entries.Values.ToList();
        }

        void EnsureCapacity(long requiredBytes)
        {
            if (_index.TotalSize + requiredBytes <= _maxCacheBytes)
                return;

            // Evict oldest 20% until we have space
            var sorted = _index.Entries.Values
                .OrderBy(e => e.LastAccess)
                .ToList();

            int evictCount = Math.Max(1, sorted.Count / 5);
            long freedBytes = 0;

            for (int i = 0; i < evictCount && _index.TotalSize + requiredBytes - freedBytes > _maxCacheBytes; i++)
            {
                var entry = sorted[i];
                if (File.Exists(entry.FilePath))
                    File.Delete(entry.FilePath);

                freedBytes += entry.Size;
                _index.Entries.Remove(GetCacheKey(entry.ModelId, entry.Source));
            }

            _index.TotalSize -= freedBytes;
            Debug.Log($"[ModelCache] Evicted {evictCount} models, freed {freedBytes / 1024}KB");
        }

        string GetCacheKey(string modelId, string source)
        {
            return $"{source}_{modelId}";
        }

        string GetModelPath(string key)
        {
            return Path.Combine(_cacheRoot, $"{key}.glb");
        }

        void LoadIndex()
        {
            var indexPath = Path.Combine(_cacheRoot, INDEX_FILE);
            if (File.Exists(indexPath))
            {
                try
                {
                    var json = File.ReadAllText(indexPath);
                    _index = JsonUtility.FromJson<CacheIndex>(json) ?? new CacheIndex();
                }
                catch
                {
                    _index = new CacheIndex();
                }
            }
            else
            {
                _index = new CacheIndex();
            }

            // Validate entries
            ValidateIndex();
        }

        void SaveIndex()
        {
            var indexPath = Path.Combine(_cacheRoot, INDEX_FILE);
            var json = JsonUtility.ToJson(_index, true);
            File.WriteAllText(indexPath, json);
        }

        void ValidateIndex()
        {
            var keysToRemove = new List<string>();
            long actualSize = 0;

            foreach (var kvp in _index.Entries)
            {
                if (!File.Exists(kvp.Value.FilePath))
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    actualSize += kvp.Value.Size;
                }
            }

            foreach (var key in keysToRemove)
                _index.Entries.Remove(key);

            _index.TotalSize = actualSize;

            if (keysToRemove.Count > 0)
            {
                SaveIndex();
                Debug.Log($"[ModelCache] Cleaned {keysToRemove.Count} orphaned entries");
            }
        }
    }

    [Serializable]
    public class CacheIndex
    {
        public Dictionary<string, CacheEntry> Entries = new();
        public long TotalSize;
    }

    [Serializable]
    public class CacheEntry
    {
        public string ModelId;
        public string Source;
        public string FilePath;
        public long Size;
        public long LastAccess;
        public ModelMetadata Metadata;
    }

    [Serializable]
    public class ModelMetadata
    {
        public string Name;
        public string Author;
        public string License;
        public string ThumbnailUrl;
        public string ViewerUrl;
    }
}
