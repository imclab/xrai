using UnityEngine;
using UnityEngine.UIElements;
using VJUITK;

namespace Fluo {

/// <summary>
/// Direct bridge from the migrated Control UI to the Visualizer logic.
/// Replaces the old WebcamController which used NDI/OSC.
/// </summary>
public sealed class ControlUIBridge : MonoBehaviour
{
    [SerializeField] UIDocument _uiDocument = null;

    void Start()
    {
        if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        // Example: Link UI buttons directly to VFX controllers
        // This is a placeholder for specific button UI mappings
        
        // root.Q<VJButton>("button-name").Clicked += () => { ... };
        
        Debug.Log("ControlUIBridge initialized. Ready to bind UI elements to VFX parameters.");
    }
}

} // namespace Fluo
