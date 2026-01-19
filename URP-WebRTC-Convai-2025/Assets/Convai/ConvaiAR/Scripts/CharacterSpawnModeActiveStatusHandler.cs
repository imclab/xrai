using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the UI toggle for activating and deactivating the character spawn mode.
/// </summary>
public class CharacterSpawnModeActiveStatusHandler : MonoBehaviour
{
    // UI toggle for character spawn mode
    [SerializeField] Toggle _characterSpawnModeActiveStatusToggle;

    // Reference to the ConvaiCharacterSpawner
    private ConvaiCharacterSpawner _convaiCharacterSpawner;

    /// <summary>
    /// Initializes necessary components and subscribes to events.
    /// </summary>
    private void Awake()
    {
        _convaiCharacterSpawner = FindObjectOfType<ConvaiCharacterSpawner>();

        // ToggleSpawnMode is called when character spawn mode changes.
        _characterSpawnModeActiveStatusToggle.onValueChanged.AddListener(ToggleSpawnMode);
    }

    /// <summary>
    /// Subscribes to events when the script is enabled.
    /// </summary>
    private void OnEnable()
    {
        _convaiCharacterSpawner.OnCharacterSpawned += ConvaiCharacterSpawner_OnCharacterSpawned;
    }

    /// <summary>
    /// Unsubscribes from events to prevent issues when the script is disabled.
    /// </summary>
    private void OnDisable()
    {
        _convaiCharacterSpawner.OnCharacterSpawned -= ConvaiCharacterSpawner_OnCharacterSpawned;
    }

    /// <summary>
    /// Handles the event when a character is spawned, resetting the toggle and deactivating the spawn mode.
    /// </summary>
    private void ConvaiCharacterSpawner_OnCharacterSpawned()
    {
        _characterSpawnModeActiveStatusToggle.isOn = false;
        ToggleSpawnMode(false);
    }

    /// <summary>
    /// Toggles the character spawn mode based on the UI toggle value.
    /// </summary>
    /// <param name="value">The value of the UI toggle.</param>
    private void ToggleSpawnMode(bool value)
    {
        _convaiCharacterSpawner.SetSpawnMode(value);
    }
}