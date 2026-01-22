// Velocity VFX Binder - Samples velocity texture and applies to VFX particles
// Provides an alternative approach when VFX graph doesn't have internal velocity sampling

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace XRRAI
{
    /// <summary>
    /// Binds velocity data from a RenderTexture to VFX Graph properties.
    /// Use this when you want velocity-driven particles but can't edit the VFX graph directly.
    ///
    /// Exposed Properties Required in VFX:
    /// - Velocity (Vector3): Applied per-frame from texture center sample
    /// - VelocityScale (float, optional): Multiplier for velocity effect
    /// </summary>
    [AddComponentMenu("VFX/Property Binders/Velocity Binder")]
    [VFXBinder("People Occlusion/Velocity")]
    public class VelocityVFXBinder : VFXBinderBase
    {
        [Tooltip("The velocity texture from PeopleOcclusionVFXManager")]
        public RenderTexture velocityTexture;

        [Tooltip("VFX property name for velocity vector")]
        public ExposedProperty velocityProperty = "Velocity";

        [Tooltip("VFX property name for velocity magnitude")]
        public ExposedProperty speedProperty = "Speed";

        [Tooltip("Sample position in UV space (0.5, 0.5 = center)")]
        public Vector2 samplePosition = new Vector2(0.5f, 0.5f);

        [Tooltip("Velocity multiplier")]
        public float velocityScale = 1.0f;

        // Readback buffer
        private Texture2D readbackTexture;
        private Color[] pixelBuffer;
        private int sampleX, sampleY;

        public override bool IsValid(VisualEffect component)
        {
            return velocityTexture != null &&
                   component.HasVector3(velocityProperty);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (velocityTexture != null)
            {
                // Create small readback texture for CPU sampling
                readbackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (readbackTexture != null)
            {
                Destroy(readbackTexture);
                readbackTexture = null;
            }
        }

        public override void UpdateBinding(VisualEffect component)
        {
            // Validate textures exist and are usable
            if (velocityTexture == null || !velocityTexture.IsCreated() ||
                velocityTexture.width <= 0 || velocityTexture.height <= 0 ||
                readbackTexture == null)
                return;

            // Set active FIRST, then validate bounds against active texture
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = velocityTexture;

            try
            {
                // Validate active texture dimensions
                int activeWidth = RenderTexture.active.width;
                int activeHeight = RenderTexture.active.height;

                if (activeWidth <= 0 || activeHeight <= 0)
                    return;

                // Calculate sample coordinates with bounds validation
                sampleX = Mathf.Clamp((int)(samplePosition.x * activeWidth), 0, activeWidth - 1);
                sampleY = Mathf.Clamp((int)(samplePosition.y * activeHeight), 0, activeHeight - 1);

                // Read single pixel from GPU (expensive - use sparingly)
                readbackTexture.ReadPixels(new Rect(sampleX, sampleY, 1, 1), 0, 0, false);
                readbackTexture.Apply();

                Color velocityColor = readbackTexture.GetPixel(0, 0);
                Vector3 velocity = new Vector3(velocityColor.r, velocityColor.g, velocityColor.b) * velocityScale;
                float speed = velocityColor.a * velocityScale;

                // Apply to VFX
                component.SetVector3(velocityProperty, velocity);

                if (component.HasFloat(speedProperty))
                {
                    component.SetFloat(speedProperty, speed);
                }
            }
            catch (System.Exception)
            {
                // Silently handle ReadPixels errors (texture state race condition)
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        public override string ToString()
        {
            return $"Velocity Binder : '{velocityProperty}' <- '{(velocityTexture != null ? velocityTexture.name : "(null)")}'";
        }
    }
}
