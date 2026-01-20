// VFXNoCull - Prevents VFX from being culled by setting large bounds
// Add to any VFX that needs to always render regardless of camera position

using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
[RequireComponent(typeof(VisualEffect))]
public class VFXNoCull : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Size of bounds (centered on VFX position)")]
    [SerializeField] float _boundsSize = 1000f;

    [Tooltip("Update bounds every frame (for moving VFX)")]
    [SerializeField] bool _updateEveryFrame = true;

    VisualEffect _vfx;
    VFXRenderer _renderer;

    void OnEnable()
    {
        _vfx = GetComponent<VisualEffect>();
        _renderer = GetComponent<VFXRenderer>();
        SetInfiniteBounds();
    }

    void LateUpdate()
    {
        if (_updateEveryFrame)
        {
            SetInfiniteBounds();
        }
    }

    void SetInfiniteBounds()
    {
        if (_renderer != null)
        {
            // Set very large local bounds so VFX is never culled
            var bounds = new Bounds(Vector3.zero, Vector3.one * _boundsSize);
            _renderer.localBounds = bounds;
        }
    }

    [ContextMenu("Force Update Bounds")]
    public void ForceUpdateBounds()
    {
        OnEnable();
        Debug.Log($"[VFXNoCull] Set bounds to {_boundsSize}m for {gameObject.name}");
    }
}
