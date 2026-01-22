// SketchfabClient - Sketchfab Download API wrapper for 3D model search (spec-009)
// Searches CC-licensed models and downloads glTF/GLB for AR placement

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace XRRAI.VoiceToObject
{
    /// <summary>
    /// Sketchfab Download API client for searching and downloading 3D models.
    /// Requires API token from Sketchfab account.
    /// </summary>
    public class SketchfabClient
    {
        const string API_BASE = "https://api.sketchfab.com/v3";
        const string SEARCH_ENDPOINT = "/search";
        const string MODELS_ENDPOINT = "/models";

        string _apiToken;
        int _timeout = 30;

        public SketchfabClient(string apiToken)
        {
            _apiToken = apiToken;
        }

        /// <summary>
        /// Search for downloadable 3D models by keyword.
        /// Only returns CC-licensed models that allow downloads.
        /// </summary>
        public async Task<List<SketchfabModel>> SearchModelsAsync(string query, int count = 24)
        {
            if (string.IsNullOrEmpty(_apiToken))
            {
                Debug.LogWarning("[SketchfabClient] No API token configured");
                return new List<SketchfabModel>();
            }

            var url = $"{API_BASE}{SEARCH_ENDPOINT}?type=models&q={UnityWebRequest.EscapeURL(query)}" +
                      $"&downloadable=true&count={count}&sort_by=-likeCount";

            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Token {_apiToken}");
            request.timeout = _timeout;

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SketchfabClient] Search failed: {request.error}");
                return new List<SketchfabModel>();
            }

            return ParseSearchResults(request.downloadHandler.text);
        }

        /// <summary>
        /// Get download URL for a specific model.
        /// </summary>
        public async Task<string> GetDownloadUrlAsync(string modelId)
        {
            var url = $"{API_BASE}{MODELS_ENDPOINT}/{modelId}/download";

            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Token {_apiToken}");
            request.timeout = _timeout;

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SketchfabClient] Download URL failed: {request.error}");
                return null;
            }

            return ParseDownloadUrl(request.downloadHandler.text);
        }

        /// <summary>
        /// Download a GLB file to local path.
        /// </summary>
        public async Task<bool> DownloadModelAsync(string downloadUrl, string localPath, Action<float> onProgress = null)
        {
            var request = UnityWebRequest.Get(downloadUrl);
            request.downloadHandler = new DownloadHandlerFile(localPath);
            request.timeout = 120; // 2 minutes for large models

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                onProgress?.Invoke(request.downloadProgress);
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SketchfabClient] Download failed: {request.error}");
                return false;
            }

            onProgress?.Invoke(1f);
            return true;
        }

        List<SketchfabModel> ParseSearchResults(string json)
        {
            var results = new List<SketchfabModel>();

            try
            {
                // Simple JSON parsing without external dependencies
                // Format: {"results": [{"uid": "...", "name": "...", ...}]}
                var startIdx = json.IndexOf("\"results\":");
                if (startIdx < 0) return results;

                var arrayStart = json.IndexOf('[', startIdx);
                var arrayEnd = json.LastIndexOf(']');
                if (arrayStart < 0 || arrayEnd < 0) return results;

                var arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);

                // Parse individual models
                int depth = 0;
                int modelStart = -1;

                for (int i = 0; i < arrayContent.Length; i++)
                {
                    if (arrayContent[i] == '{')
                    {
                        if (depth == 0) modelStart = i;
                        depth++;
                    }
                    else if (arrayContent[i] == '}')
                    {
                        depth--;
                        if (depth == 0 && modelStart >= 0)
                        {
                            var modelJson = arrayContent.Substring(modelStart, i - modelStart + 1);
                            var model = ParseModel(modelJson);
                            if (model != null)
                                results.Add(model);
                            modelStart = -1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SketchfabClient] Parse error: {e.Message}");
            }

            return results;
        }

        SketchfabModel ParseModel(string json)
        {
            var model = new SketchfabModel();
            model.Uid = ExtractJsonString(json, "uid");
            model.Name = ExtractJsonString(json, "name");
            model.Description = ExtractJsonString(json, "description");
            model.AuthorUsername = ExtractJsonString(json, "username");
            model.License = ExtractJsonString(json, "license");
            model.ThumbnailUrl = ExtractThumbnailUrl(json);
            model.ViewerUrl = ExtractJsonString(json, "viewerUrl");
            return string.IsNullOrEmpty(model.Uid) ? null : model;
        }

        string ExtractJsonString(string json, string key)
        {
            var pattern = $"\"{key}\":";
            var idx = json.IndexOf(pattern);
            if (idx < 0) return null;

            var valueStart = idx + pattern.Length;
            while (valueStart < json.Length && (json[valueStart] == ' ' || json[valueStart] == '\t'))
                valueStart++;

            if (valueStart >= json.Length) return null;

            if (json[valueStart] == '"')
            {
                var valueEnd = json.IndexOf('"', valueStart + 1);
                if (valueEnd < 0) return null;
                return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
            }
            else if (json[valueStart] == 'n' && json.Substring(valueStart, 4) == "null")
            {
                return null;
            }

            return null;
        }

        string ExtractThumbnailUrl(string json)
        {
            // Look for thumbnails.images array with size >= 200
            var thumbIdx = json.IndexOf("\"thumbnails\"");
            if (thumbIdx < 0) return null;

            var imagesIdx = json.IndexOf("\"images\"", thumbIdx);
            if (imagesIdx < 0) return null;

            var urlIdx = json.IndexOf("\"url\":", imagesIdx);
            if (urlIdx < 0) return null;

            return ExtractJsonString(json.Substring(urlIdx - 10), "url");
        }

        string ParseDownloadUrl(string json)
        {
            // Format: {"gltf": {"url": "...", "size": 123, "expires": 456}}
            var gltfIdx = json.IndexOf("\"gltf\"");
            if (gltfIdx < 0) return null;

            return ExtractJsonString(json.Substring(gltfIdx), "url");
        }
    }

    /// <summary>
    /// Sketchfab model metadata.
    /// </summary>
    [Serializable]
    public class SketchfabModel
    {
        public string Uid;
        public string Name;
        public string Description;
        public string AuthorUsername;
        public string License;
        public string ThumbnailUrl;
        public string ViewerUrl;
        public string Source => "Sketchfab";
    }
}
