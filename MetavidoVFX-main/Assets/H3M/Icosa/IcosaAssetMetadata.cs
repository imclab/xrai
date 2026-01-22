using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace XRRAI.VoiceToObject
{
    /// <summary>
    /// Attribution metadata attached to imported 3D models.
    /// Spec-009: Automatic CC license compliance.
    /// </summary>
    public class IcosaAssetMetadata : MonoBehaviour
    {
        [Header("Asset Info")]
        [SerializeField] string _assetId;
        [SerializeField] string _displayName;
        [SerializeField] string _authorName;
        [SerializeField] string _authorUrl;
        [SerializeField] ModelSource _source;

        [Header("License")]
        [SerializeField] string _license;
        [SerializeField] string _licenseUrl;
        [SerializeField] string _sourceUrl;

        [Header("Technical")]
        [SerializeField] string _downloadDate;
        [SerializeField] long _fileSizeBytes;

        public string AssetId => _assetId;
        public string DisplayName => _displayName;
        public string AuthorName => _authorName;
        public ModelSource Source => _source;
        public string License => _license;

        public void Initialize(UnifiedSearchResult result)
        {
            _assetId = result.AssetId;
            _displayName = result.DisplayName;
            _authorName = result.AuthorName;
            _source = result.Source;
            _license = result.License;
            _licenseUrl = result.LicenseUrl;
            _sourceUrl = result.SourceUrl;
            _downloadDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Generate attribution text for this model.
        /// </summary>
        public string GenerateAttribution()
        {
            var sb = new StringBuilder();
            sb.Append($"\"{_displayName}\"");
            if (!string.IsNullOrEmpty(_authorName))
                sb.Append($" by {_authorName}");
            if (!string.IsNullOrEmpty(_sourceUrl))
                sb.Append($" ({_sourceUrl})");
            if (!string.IsNullOrEmpty(_license))
                sb.Append($" - {_license}");
            return sb.ToString();
        }

        /// <summary>
        /// Check if commercial use is allowed.
        /// </summary>
        public bool IsCommercialAllowed()
        {
            if (string.IsNullOrEmpty(_license)) return false;
            string lower = _license.ToLower();
            return !lower.Contains("-nc") && !lower.Contains("noncommercial");
        }

        /// <summary>
        /// Generate attributions for all models in scene.
        /// </summary>
        public static string GenerateAllAttributions()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## 3D Model Attributions\n");

            var all = FindObjectsOfType<IcosaAssetMetadata>();
            foreach (var meta in all)
            {
                sb.AppendLine($"- {meta.GenerateAttribution()}");
            }

            return sb.ToString();
        }
    }
}
