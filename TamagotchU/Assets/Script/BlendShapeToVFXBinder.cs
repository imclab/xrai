// 7/24/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class BlendShapeToVFXBinder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SkinnedMeshRenderer containing the blend shapes.")]
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [Tooltip("The VisualEffect component to update.")]
    public VisualEffect visualEffect;

    [Header("Settings")]
    [Tooltip("The index of the blend shape key to bind.")]
    public int blendShapeKeyIndex = 2;

    [Tooltip("The name of the VisualEffect property to bind to.")]
    public string vfxPropertyName = "ThornScale";

    [Tooltip("The duration of the blend shape animation in seconds.")]
    public float debugShapekeyDuration = 1.0f;

    private Coroutine blendShapeCoroutine;

    private void Update()
    {
        if (skinnedMeshRenderer == null || visualEffect == null)
        {
            Debug.LogWarning("SkinnedMeshRenderer or VisualEffect is not assigned.");
            return;
        }

        // Get the blend shape weight for the specified key index
        float blendShapeValue = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeKeyIndex);

        // Set the value to the VisualEffect property
        if (visualEffect.HasFloat(vfxPropertyName))
        {
            visualEffect.SetFloat(vfxPropertyName, blendShapeValue);
        }
        else
        {
            Debug.LogWarning($"VisualEffect does not have a property named '{vfxPropertyName}'.");
        }

        // Check for the 'S' key press to start the coroutine
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (blendShapeCoroutine != null)
            {
                StopCoroutine(blendShapeCoroutine);
            }
            blendShapeCoroutine = StartCoroutine(AnimateBlendShape(0, 100, debugShapekeyDuration));
        }
    }

    private IEnumerator AnimateBlendShape(float startValue, float endValue, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Interpolate the blend shape value
            float currentValue = Mathf.Lerp(startValue, endValue, t);
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeKeyIndex, currentValue);

            yield return null;
        }

        // Ensure the final value is set
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeKeyIndex, endValue);
    }
}
