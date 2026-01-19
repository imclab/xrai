using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// A helper component for optimizing image processing in Unity by managing frame skipping 
    /// and downscaling operations. v1.1.1
    /// </summary>
    /// <remarks>
    /// The <see cref="ImageOptimizationHelper"/> class provides utilities to enhance performance 
    /// during image processing tasks. It includes features such as:
    /// <list type="bullet">
    ///     <item><description>Downscaling images to reduce computational load</description></item>
    ///     <item><description>Skipping frames to optimize processing frequency</description></item>
    /// </list>
    /// This class is particularly useful when working with high-resolution images or real-time 
    /// processing where performance is a priority.
    /// </remarks>
    /// <example>
    /// Attach this component to a GameObject to enable image optimization:
    /// <code>
    /// // Example usage of the ImageOptimizationHelper component
    /// ImageOptimizationHelper imageHelper = gameObject.AddComponent&lt;ImageOptimizationHelper&gt;();
    /// imageHelper.downscaleRatio = 2.0f;
    /// imageHelper.frameSkippingRatio = 3;
    ///
    /// // Check if the current frame should be skipped
    /// if (!imageHelper.IsCurrentFrameSkipped())
    /// {
    ///     // Perform operations on the downscaled image
    ///     Mat optimizedMat = imageHelper.GetDownScaleMat(originalMat);
    /// }
    /// </code>
    /// </example>
    public class ImageOptimizationHelper : MonoBehaviour
    {

        [SerializeField, FormerlySerializedAs("downscaleRatio"), TooltipAttribute("Set the ratio of down scaling.")]
        protected float _downscaleRatio = 2f;

        /// <summary>
        /// The downscale ratio.
        /// </summary>
        public virtual float downscaleRatio
        {
            get { return _downscaleRatio; }
            set { _downscaleRatio = Mathf.Clamp(value, 1f, float.MaxValue); }
        }


        [SerializeField, FormerlySerializedAs("frameSkippingRatio"), TooltipAttribute("Set the ratio of frame skipping.")]
        protected int _frameSkippingRatio = 2;

        /// <summary>
        /// The frame skipping ratio.
        /// </summary>
        public virtual int frameSkippingRatio
        {
            get { return _frameSkippingRatio; }
            set { _frameSkippingRatio = (int)Mathf.Clamp(value, 1f, float.MaxValue); }
        }

        /// <summary>
        /// The frame count.
        /// </summary>
        protected int frameCount = 0;

        /// <summary>
        /// The downscale frame mat.
        /// </summary>
        protected Mat downScaleFrameMat;

        protected void OnValidate()
        {
            _downscaleRatio = Mathf.Clamp(_downscaleRatio, 1f, float.MaxValue);
            _frameSkippingRatio = (int)Mathf.Clamp(_frameSkippingRatio, 1f, float.MaxValue);
        }

        /// <summary>
        /// Indicates whether the current frame is skipped.
        /// </summary>
        /// <returns><c>true</c>, if the current frame is skipped, <c>false</c> otherwise.</returns>
        public virtual bool IsCurrentFrameSkipped()
        {
            frameCount++;

            return (frameCount % frameSkippingRatio != 0) ? true : false;
        }

        /// <summary>
        /// Gets the mat that downscaled the original mat.
        /// if downscaleRatio == 1 , return originalMat. 
        /// </summary>
        /// <returns>The downscale mat.</returns>
        /// <param name="originalMat">Original mat.</param>
        public virtual Mat GetDownScaleMat(Mat originalMat)
        {
            if (Mathf.Approximately(_downscaleRatio, 1f))
                return originalMat;

            if (downScaleFrameMat == null)
                downScaleFrameMat = new Mat();

            Imgproc.resize(originalMat, downScaleFrameMat, new Size(), 1.0 / _downscaleRatio, 1.0 / _downscaleRatio, Imgproc.INTER_LINEAR);

            return downScaleFrameMat;
        }

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        public virtual void Dispose()
        {
            frameCount = 0;

            if (downScaleFrameMat != null)
            {
                downScaleFrameMat.Dispose();
                downScaleFrameMat = null;
            }
        }
    }
}