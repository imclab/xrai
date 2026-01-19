using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Spawns and manages Convai characters in AR space.
/// </summary>
public class ConvaiCharacterSpawner : MonoBehaviour
{
    // Prefab for the Convai character
    [SerializeField] private GameObject _characterPrefab;

    private ARRaycastManager _arRaycastManager;
    private ARPlaneManager _arPlaneManager;
    private ARTrackedImageManager _arTrackedImageManager;

    // Flag to determine if character spawn mode is active
    private bool _isSpawnModeActive = true;

    // Flag indicating whether the maximum character count has been reached
    private bool _isMaxCharacterCountReached;

    // Counter for the number of characters spawned
    private int _spawnedCharacterCount;

    // Maximum number of characters allowed to be spawned
    private const int MAX_CHARACTER_COUNT = 5;

    // Event triggered when a character is spawned
    public Action OnCharacterSpawned;

    /// <summary>
    /// Initializes necessary components and finds AR-related managers.
    /// </summary>
    private void Awake()
    {
        _arRaycastManager = FindObjectOfType<ARRaycastManager>();
        _arPlaneManager = FindObjectOfType<ARPlaneManager>();
        _arTrackedImageManager = FindObjectOfType<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += ConvaiInputManager_OnTouchScreen;
        _arTrackedImageManager.trackedImagesChanged += ARTrackedImageManager_TrackedImagesChanged;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= ConvaiInputManager_OnTouchScreen;
        EnhancedTouchSupport.Disable();
        _arTrackedImageManager.trackedImagesChanged -= ARTrackedImageManager_TrackedImagesChanged;
    }

    /// <summary>
    /// Handles touch screen events to place characters on planes.
    /// </summary>
    /// <param name="finger">Information about the touch input.</param>
    private void ConvaiInputManager_OnTouchScreen(Finger finger)
    {
        if (IsPointerOverUIObject(finger)) return;
        if (!_isSpawnModeActive) return;
        if (_isMaxCharacterCountReached) return;
        TryPlaceCharacterOnPlane(finger.screenPosition);
    }

    /// <summary>
    /// Handles tracked image changes, spawning characters on added images.
    /// </summary>
    /// <param name="eventArgs">Event arguments containing information about tracked image changes.</param>
    private void ARTrackedImageManager_TrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            // Handle Added Event
            GameObject character = SpawnCharacter();
            character.transform.parent = trackedImage.transform;
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            // Handle Update Event
        }

        foreach (var removedImage in eventArgs.removed)
        {
            // Handle Removed Event
        }
    }

    /// <summary>
    /// Tries to place a character on a detected AR plane at the specified screen position.
    /// </summary>
    /// <param name="touchPosition">The screen position of the touch input.</param>
    private void TryPlaceCharacterOnPlane(Vector2 touchPosition)
    {
        List<ARRaycastHit> _results = new List<ARRaycastHit>();
        if (_arRaycastManager.Raycast(touchPosition, _results, TrackableType.PlaneWithinPolygon))
        {
            GameObject character = SpawnCharacter();
            character.transform.position = _results[0].pose.position;
            Debug.Log("<color=green> Character Spawned! </color>");
        }
        else
        {
            Debug.Log("<color=red> Character Did Not Spawn! </color>");
        }
    }

    /// <summary>
    /// Spawns a character and updates related counters and flags.
    /// </summary>
    /// <returns>The spawned character GameObject.</returns>
    private GameObject SpawnCharacter()
    {
        if (_isMaxCharacterCountReached) return null;
        GameObject character = Instantiate(_characterPrefab);
        _spawnedCharacterCount++;
        if (_spawnedCharacterCount == MAX_CHARACTER_COUNT)
        {
            _isMaxCharacterCountReached = true;
            TogglePlaneManager(false);
        }

        OnCharacterSpawned?.Invoke();

        return character;
    }

    /// <summary>
    /// Toggles the AR Plane Manager to activate or deactivate AR planes.
    /// </summary>
    /// <param name="value">The value indicating whether to activate or deactivate AR planes.</param>
    private void TogglePlaneManager(bool value)
    {
        // Deactivate/Activate All Existing Planes.
        _arPlaneManager.SetTrackablesActive(value);

        // Disable/Activate the AR Plane Manager.
        _arPlaneManager.enabled = value;
    }

    /// <summary>
    /// Sets the spawn mode, enabling or disabling character spawning.
    /// </summary>
    /// <param name="value">The value indicating whether character spawn mode is active.</param>
    public void SetSpawnMode(bool value)
    {
        _isSpawnModeActive = value;

        if (_isSpawnModeActive)
        {
            TogglePlaneManager(true);
        }
        else
        {
            TogglePlaneManager(false);
        }
    }

    /// <summary>
    /// Checks if the pointer is over any UI object.
    /// </summary>
    /// <param name="finger">Information about the touch input.</param>
    /// <returns>True if the pointer is over a UI object; otherwise, false.</returns>
    private bool IsPointerOverUIObject(Finger finger)
    {
        PointerEventData eventDataCurrentPosition = new(EventSystem.current);
        eventDataCurrentPosition.position = finger.screenPosition;
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}