// H3MStreamMetadata - Metadata for hologram video streams
// Contains camera position, rotation, projection, and depth range for remote holograms
//
// Based on Rcam3-WebRTC Metadata system

using UnityEngine;
using System;
using System.Text;
using System.Xml;

namespace MetavidoVFX.H3M.Network
{
    /// <summary>
    /// Metadata transmitted alongside hologram video streams.
    /// Contains all information needed to reconstruct 3D positions from depth maps.
    /// </summary>
    [Serializable]
    public struct H3MStreamMetadata
    {
        // Camera properties
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
        public Matrix4x4 ProjectionMatrix;
        public Vector2 DepthRange;

        // Hologram state
        public bool IsActive;
        public float Timestamp;

        // Constructor
        public H3MStreamMetadata(
            Vector3 cameraPosition,
            Quaternion cameraRotation,
            Matrix4x4 projectionMatrix,
            Vector2 depthRange,
            bool isActive = true)
        {
            CameraPosition = cameraPosition;
            CameraRotation = cameraRotation;
            ProjectionMatrix = projectionMatrix;
            DepthRange = depthRange;
            IsActive = isActive;
            Timestamp = Time.time;
        }

        /// <summary>
        /// Default initial metadata for unconnected state.
        /// </summary>
        public static readonly H3MStreamMetadata Default = new H3MStreamMetadata(
            Vector3.zero,
            Quaternion.identity,
            Matrix4x4.identity,
            new Vector2(0.2f, 3.2f),
            false
        );

        /// <summary>
        /// Create metadata from current AR camera state.
        /// </summary>
        public static H3MStreamMetadata FromCamera(Camera camera, Vector2 depthRange)
        {
            return new H3MStreamMetadata(
                camera.transform.position,
                camera.transform.rotation,
                camera.projectionMatrix,
                depthRange,
                true
            );
        }

        /// <summary>
        /// Get inverse view matrix for VFX binding.
        /// </summary>
        public Matrix4x4 GetInverseView()
        {
            return Matrix4x4.TRS(CameraPosition, CameraRotation, Vector3.one);
        }

        /// <summary>
        /// Get inverse projection parameters for VFX binding.
        /// Returns (0, 0, tan(fov/2)*aspect, tan(fov/2))
        /// </summary>
        public Vector4 GetInverseProjection()
        {
            var m = ProjectionMatrix;
            return new Vector4(0, 0, 1f / m[0, 0], 1f / m[1, 1]);
        }

        /// <summary>
        /// Get RayParams for VFX (same as InverseProjection).
        /// </summary>
        public Vector4 GetRayParams() => GetInverseProjection();

        #region Serialization

        /// <summary>
        /// Serialize to XML string for transmission.
        /// </summary>
        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("<h3m-stream>");

            // Camera position
            sb.Append("<pos>");
            sb.Append($"{CameraPosition.x:F4},{CameraPosition.y:F4},{CameraPosition.z:F4}");
            sb.Append("</pos>");

            // Camera rotation
            sb.Append("<rot>");
            sb.Append($"{CameraRotation.x:F4},{CameraRotation.y:F4},{CameraRotation.z:F4},{CameraRotation.w:F4}");
            sb.Append("</rot>");

            // Projection matrix (essential elements only)
            sb.Append("<proj>");
            sb.Append($"{ProjectionMatrix[0,0]:F4},{ProjectionMatrix[1,1]:F4},");
            sb.Append($"{ProjectionMatrix[2,2]:F4},{ProjectionMatrix[2,3]:F4},");
            sb.Append($"{ProjectionMatrix[3,2]:F4}");
            sb.Append("</proj>");

            // Depth range
            sb.Append("<depth>");
            sb.Append($"{DepthRange.x:F3},{DepthRange.y:F3}");
            sb.Append("</depth>");

            // State
            sb.Append($"<active>{(IsActive ? 1 : 0)}</active>");
            sb.Append($"<time>{Timestamp:F3}</time>");

            sb.Append("</h3m-stream>");
            return sb.ToString();
        }

        /// <summary>
        /// Deserialize from XML string.
        /// </summary>
        public static H3MStreamMetadata Deserialize(string xml)
        {
            try
            {
                var metadata = new H3MStreamMetadata();
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                // Parse position
                var posNode = doc.SelectSingleNode("//pos");
                if (posNode != null)
                {
                    var values = posNode.InnerText.Split(',');
                    if (values.Length >= 3)
                    {
                        metadata.CameraPosition = new Vector3(
                            float.Parse(values[0]),
                            float.Parse(values[1]),
                            float.Parse(values[2])
                        );
                    }
                }

                // Parse rotation
                var rotNode = doc.SelectSingleNode("//rot");
                if (rotNode != null)
                {
                    var values = rotNode.InnerText.Split(',');
                    if (values.Length >= 4)
                    {
                        metadata.CameraRotation = new Quaternion(
                            float.Parse(values[0]),
                            float.Parse(values[1]),
                            float.Parse(values[2]),
                            float.Parse(values[3])
                        );
                    }
                }

                // Parse projection matrix
                var projNode = doc.SelectSingleNode("//proj");
                if (projNode != null)
                {
                    var values = projNode.InnerText.Split(',');
                    if (values.Length >= 5)
                    {
                        var m = Matrix4x4.identity;
                        m[0,0] = float.Parse(values[0]);
                        m[1,1] = float.Parse(values[1]);
                        m[2,2] = float.Parse(values[2]);
                        m[2,3] = float.Parse(values[3]);
                        m[3,2] = float.Parse(values[4]);
                        metadata.ProjectionMatrix = m;
                    }
                }

                // Parse depth range
                var depthNode = doc.SelectSingleNode("//depth");
                if (depthNode != null)
                {
                    var values = depthNode.InnerText.Split(',');
                    if (values.Length >= 2)
                    {
                        metadata.DepthRange = new Vector2(
                            float.Parse(values[0]),
                            float.Parse(values[1])
                        );
                    }
                }

                // Parse state
                var activeNode = doc.SelectSingleNode("//active");
                if (activeNode != null)
                {
                    metadata.IsActive = activeNode.InnerText == "1";
                }

                var timeNode = doc.SelectSingleNode("//time");
                if (timeNode != null)
                {
                    metadata.Timestamp = float.Parse(timeNode.InnerText);
                }

                return metadata;
            }
            catch (Exception e)
            {
                Debug.LogError($"[H3MStreamMetadata] Failed to parse: {e.Message}");
                return Default;
            }
        }

        #endregion

        public override string ToString()
        {
            return $"H3MStream[pos={CameraPosition}, active={IsActive}]";
        }
    }
}
