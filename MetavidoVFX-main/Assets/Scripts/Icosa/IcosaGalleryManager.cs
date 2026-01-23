// IcosaGalleryManager.cs - Icosa Gallery REST API integration
// Part of Spec 016: XRRAI Scene Format & Cross-Platform Export
//
// Handles authentication, upload, and asset management for Icosa Gallery.
// Uses device code flow for authentication (no OAuth redirect needed).

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace XRRAI.VoiceToObject
{
    /// <summary>
    /// Singleton manager for Icosa Gallery API integration.
    /// Handles authentication, upload, and asset listing.
    /// </summary>
    public class IcosaGalleryManager : MonoBehaviour
    {
        public static IcosaGalleryManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] string _apiBaseUrl = "https://api.icosa.gallery/v1";
        [SerializeField] string _clientId = "metavidovfx";
        [SerializeField] float _uploadTimeout = 120f;

        // Events
        public event Action<string> OnAuthenticationComplete;
        public event Action<string> OnAuthenticationFailed;
        public event Action<UploadResult> OnUploadComplete;
        public event Action<string> OnUploadFailed;
        public event Action<float> OnUploadProgress;

        // State
        string _accessToken;
        string _refreshToken;
        DateTime _tokenExpiry;
        bool _isAuthenticating;
        bool _isUploading;

        // Properties
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;
        public bool IsAuthenticating => _isAuthenticating;
        public bool IsUploading => _isUploading;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadStoredTokens();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #region Authentication

        /// <summary>
        /// Start device code authentication flow
        /// </summary>
        public async Task<DeviceCodeResponse> StartDeviceCodeFlowAsync()
        {
            if (_isAuthenticating)
            {
                Debug.LogWarning("[IcosaGalleryManager] Already authenticating");
                return null;
            }

            _isAuthenticating = true;

            try
            {
                var form = new WWWForm();
                form.AddField("client_id", _clientId);
                form.AddField("scope", "read write");

                using var request = UnityWebRequest.Post($"{_apiBaseUrl}/oauth/device/code", form);
                await SendWebRequestAsync(request);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[IcosaGalleryManager] Device code request failed: {request.error}");
                    OnAuthenticationFailed?.Invoke(request.error);
                    return null;
                }

                var response = JsonUtility.FromJson<DeviceCodeResponse>(request.downloadHandler.text);
                Debug.Log($"[IcosaGalleryManager] Device code: {response.user_code}");
                Debug.Log($"[IcosaGalleryManager] Verify at: {response.verification_uri}");

                // Start polling for token
                StartCoroutine(PollForToken(response));

                return response;
            }
            finally
            {
                // Don't set _isAuthenticating = false here, wait for polling to complete
            }
        }

        IEnumerator PollForToken(DeviceCodeResponse deviceCode)
        {
            float elapsed = 0f;
            float pollInterval = deviceCode.interval > 0 ? deviceCode.interval : 5f;
            float maxWait = deviceCode.expires_in > 0 ? deviceCode.expires_in : 300f;

            while (elapsed < maxWait)
            {
                yield return new WaitForSeconds(pollInterval);
                elapsed += pollInterval;

                var form = new WWWForm();
                form.AddField("client_id", _clientId);
                form.AddField("device_code", deviceCode.device_code);
                form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:device_code");

                using var request = UnityWebRequest.Post($"{_apiBaseUrl}/oauth/token", form);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var tokenResponse = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                    _accessToken = tokenResponse.access_token;
                    _refreshToken = tokenResponse.refresh_token;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60); // 1 min buffer

                    SaveTokens();
                    _isAuthenticating = false;

                    Debug.Log("[IcosaGalleryManager] Authentication successful");
                    OnAuthenticationComplete?.Invoke(_accessToken);
                    yield break;
                }

                // Check for pending authorization (expected during polling)
                if (request.responseCode == 400)
                {
                    var error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                    if (error.error == "authorization_pending")
                        continue;

                    // Other errors
                    Debug.LogError($"[IcosaGalleryManager] Auth error: {error.error_description}");
                    OnAuthenticationFailed?.Invoke(error.error_description);
                    _isAuthenticating = false;
                    yield break;
                }
            }

            Debug.LogError("[IcosaGalleryManager] Authentication timed out");
            OnAuthenticationFailed?.Invoke("Authentication timed out");
            _isAuthenticating = false;
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        public async Task<bool> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_refreshToken))
                return false;

            var form = new WWWForm();
            form.AddField("client_id", _clientId);
            form.AddField("refresh_token", _refreshToken);
            form.AddField("grant_type", "refresh_token");

            using var request = UnityWebRequest.Post($"{_apiBaseUrl}/oauth/token", form);
            await SendWebRequestAsync(request);

            if (request.result == UnityWebRequest.Result.Success)
            {
                var tokenResponse = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                _accessToken = tokenResponse.access_token;
                _refreshToken = tokenResponse.refresh_token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60);

                SaveTokens();
                Debug.Log("[IcosaGalleryManager] Token refreshed");
                return true;
            }

            Debug.LogError($"[IcosaGalleryManager] Token refresh failed: {request.error}");
            return false;
        }

        /// <summary>
        /// Clear authentication
        /// </summary>
        public void Logout()
        {
            _accessToken = null;
            _refreshToken = null;
            _tokenExpiry = DateTime.MinValue;
            PlayerPrefs.DeleteKey("IcosaAccessToken");
            PlayerPrefs.DeleteKey("IcosaRefreshToken");
            PlayerPrefs.DeleteKey("IcosaTokenExpiry");
            Debug.Log("[IcosaGalleryManager] Logged out");
        }

        void SaveTokens()
        {
            PlayerPrefs.SetString("IcosaAccessToken", _accessToken ?? "");
            PlayerPrefs.SetString("IcosaRefreshToken", _refreshToken ?? "");
            PlayerPrefs.SetString("IcosaTokenExpiry", _tokenExpiry.ToString("o"));
            PlayerPrefs.Save();
        }

        void LoadStoredTokens()
        {
            _accessToken = PlayerPrefs.GetString("IcosaAccessToken", "");
            _refreshToken = PlayerPrefs.GetString("IcosaRefreshToken", "");

            var expiryStr = PlayerPrefs.GetString("IcosaTokenExpiry", "");
            if (DateTime.TryParse(expiryStr, out var expiry))
                _tokenExpiry = expiry;

            if (!string.IsNullOrEmpty(_accessToken))
                Debug.Log("[IcosaGalleryManager] Loaded stored tokens");
        }

        #endregion

        #region Upload

        /// <summary>
        /// Upload a GLB file to Icosa Gallery
        /// </summary>
        public async Task<UploadResult> UploadAsync(string filepath, AssetMetadata metadata)
        {
            if (!IsAuthenticated)
            {
                // Try refresh
                if (!await RefreshTokenAsync())
                {
                    OnUploadFailed?.Invoke("Not authenticated");
                    return null;
                }
            }

            if (_isUploading)
            {
                OnUploadFailed?.Invoke("Upload already in progress");
                return null;
            }

            if (!File.Exists(filepath))
            {
                OnUploadFailed?.Invoke($"File not found: {filepath}");
                return null;
            }

            _isUploading = true;

            try
            {
                byte[] fileData = await Task.Run(() => File.ReadAllBytes(filepath));
                string fileName = Path.GetFileName(filepath);

                var form = new List<IMultipartFormSection>
                {
                    new MultipartFormFileSection("file", fileData, fileName, "model/gltf-binary"),
                    new MultipartFormDataSection("name", metadata.name ?? "Untitled"),
                    new MultipartFormDataSection("description", metadata.description ?? ""),
                    new MultipartFormDataSection("license", metadata.license ?? "CC-BY"),
                    new MultipartFormDataSection("visibility", metadata.isPrivate ? "private" : "public")
                };

                if (metadata.tags != null && metadata.tags.Count > 0)
                {
                    form.Add(new MultipartFormDataSection("tags", string.Join(",", metadata.tags)));
                }

                using var request = UnityWebRequest.Post($"{_apiBaseUrl}/users/me/assets", form);
                request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
                request.timeout = (int)_uploadTimeout;

                var operation = request.SendWebRequest();

                // Track progress
                while (!operation.isDone)
                {
                    OnUploadProgress?.Invoke(request.uploadProgress);
                    await Task.Delay(100);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[IcosaGalleryManager] Upload failed: {request.error}");
                    Debug.LogError($"[IcosaGalleryManager] Response: {request.downloadHandler.text}");
                    OnUploadFailed?.Invoke(request.error);
                    return null;
                }

                var response = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                var result = new UploadResult
                {
                    assetId = response.id,
                    publishUrl = $"https://icosa.gallery/upload/{response.id}",
                    viewUrl = $"https://icosa.gallery/view/{response.id}",
                    thumbnailUrl = response.thumbnail_url
                };

                Debug.Log($"[IcosaGalleryManager] Upload complete: {result.viewUrl}");
                OnUploadComplete?.Invoke(result);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IcosaGalleryManager] Upload error: {ex.Message}");
                OnUploadFailed?.Invoke(ex.Message);
                return null;
            }
            finally
            {
                _isUploading = false;
            }
        }

        #endregion

        #region Asset Management

        /// <summary>
        /// Get list of user's assets
        /// </summary>
        public async Task<List<AssetInfo>> GetUserAssetsAsync(int limit = 20, int offset = 0)
        {
            if (!IsAuthenticated && !await RefreshTokenAsync())
                return null;

            using var request = UnityWebRequest.Get($"{_apiBaseUrl}/users/me/assets?limit={limit}&offset={offset}");
            request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
            await SendWebRequestAsync(request);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[IcosaGalleryManager] Get assets failed: {request.error}");
                return null;
            }

            var response = JsonUtility.FromJson<AssetsListResponse>(request.downloadHandler.text);
            return response.assets;
        }

        /// <summary>
        /// Delete an asset
        /// </summary>
        public async Task<bool> DeleteAssetAsync(string assetId)
        {
            if (!IsAuthenticated && !await RefreshTokenAsync())
                return false;

            using var request = UnityWebRequest.Delete($"{_apiBaseUrl}/users/me/assets/{assetId}");
            request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
            await SendWebRequestAsync(request);

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[IcosaGalleryManager] Asset deleted: {assetId}");
                return true;
            }

            Debug.LogError($"[IcosaGalleryManager] Delete failed: {request.error}");
            return false;
        }

        #endregion

        #region Helpers

        Task SendWebRequestAsync(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();
            operation.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class DeviceCodeResponse
    {
        public string device_code;
        public string user_code;
        public string verification_uri;
        public string verification_uri_complete;
        public int expires_in;
        public int interval;
    }

    [Serializable]
    public class TokenResponse
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string error;
        public string error_description;
    }

    [Serializable]
    public class AssetMetadata
    {
        public string name;
        public string description;
        public List<string> tags;
        public string license = "CC-BY";
        public bool isPrivate;
    }

    [Serializable]
    public class UploadResponse
    {
        public string id;
        public string name;
        public string thumbnail_url;
        public string status;
    }

    [Serializable]
    public class UploadResult
    {
        public string assetId;
        public string publishUrl;
        public string viewUrl;
        public string thumbnailUrl;
    }

    [Serializable]
    public class AssetInfo
    {
        public string id;
        public string name;
        public string description;
        public string thumbnail_url;
        public string visibility;
        public string created_at;
        public string updated_at;
    }

    [Serializable]
    public class AssetsListResponse
    {
        public List<AssetInfo> assets;
        public int total;
    }

    #endregion
}
