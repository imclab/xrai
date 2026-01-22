using UnityEngine;
using UnityEngine.UIElements;

namespace MetavidoVFX.Recording
{
    /// <summary>
    /// UI for hologram recording with record button, timer, and status display.
    /// Uses UI Toolkit for responsive mobile-friendly interface.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class RecordingUI : MonoBehaviour
    {
        [SerializeField] private RecordingController _recordingController;
        [SerializeField] private VisualTreeAsset _uiAsset;

        private UIDocument _document;
        private Button _recordButton;
        private Label _timerLabel;
        private Label _statusLabel;
        private VisualElement _recordIndicator;

        void Awake()
        {
            _document = GetComponent<UIDocument>();

            if (_recordingController == null)
                _recordingController = FindObjectOfType<RecordingController>();
        }

        void OnEnable()
        {
            if (_uiAsset != null)
            {
                _document.visualTreeAsset = _uiAsset;
            }
            else
            {
                // Create UI programmatically
                CreateUI();
            }

            BindUI();
            SubscribeEvents();
        }

        void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void CreateUI()
        {
            var root = _document.rootVisualElement;
            root.Clear();

            // Container
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.bottom = 50;
            container.style.left = 0;
            container.style.right = 0;
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.Center;
            root.Add(container);

            // Timer
            _timerLabel = new Label("00:00");
            _timerLabel.name = "timer-label";
            _timerLabel.style.fontSize = 32;
            _timerLabel.style.color = Color.white;
            _timerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _timerLabel.style.marginBottom = 10;
            _timerLabel.style.display = DisplayStyle.None;
            container.Add(_timerLabel);

            // Record button container
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.alignItems = Align.Center;
            container.Add(buttonContainer);

            // Record button
            _recordButton = new Button(OnRecordButtonClicked);
            _recordButton.name = "record-button";
            _recordButton.style.width = 80;
            _recordButton.style.height = 80;
            _recordButton.style.borderTopLeftRadius = 40;
            _recordButton.style.borderTopRightRadius = 40;
            _recordButton.style.borderBottomLeftRadius = 40;
            _recordButton.style.borderBottomRightRadius = 40;
            _recordButton.style.backgroundColor = new Color(0.8f, 0.1f, 0.1f);
            _recordButton.style.borderTopWidth = 4;
            _recordButton.style.borderBottomWidth = 4;
            _recordButton.style.borderLeftWidth = 4;
            _recordButton.style.borderRightWidth = 4;
            _recordButton.style.borderTopColor = Color.white;
            _recordButton.style.borderBottomColor = Color.white;
            _recordButton.style.borderLeftColor = Color.white;
            _recordButton.style.borderRightColor = Color.white;
            buttonContainer.Add(_recordButton);

            // Record indicator (inner circle)
            _recordIndicator = new VisualElement();
            _recordIndicator.name = "record-indicator";
            _recordIndicator.style.width = 50;
            _recordIndicator.style.height = 50;
            _recordIndicator.style.borderTopLeftRadius = 25;
            _recordIndicator.style.borderTopRightRadius = 25;
            _recordIndicator.style.borderBottomLeftRadius = 25;
            _recordIndicator.style.borderBottomRightRadius = 25;
            _recordIndicator.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
            _recordIndicator.style.position = Position.Absolute;
            _recordIndicator.style.left = 15;
            _recordIndicator.style.top = 15;
            _recordButton.Add(_recordIndicator);

            // Status label
            _statusLabel = new Label("Tap to record");
            _statusLabel.name = "status-label";
            _statusLabel.style.fontSize = 16;
            _statusLabel.style.color = Color.white;
            _statusLabel.style.marginTop = 10;
            container.Add(_statusLabel);
        }

        private void BindUI()
        {
            var root = _document.rootVisualElement;

            if (_recordButton == null)
                _recordButton = root.Q<Button>("record-button");
            if (_timerLabel == null)
                _timerLabel = root.Q<Label>("timer-label");
            if (_statusLabel == null)
                _statusLabel = root.Q<Label>("status-label");
            if (_recordIndicator == null)
                _recordIndicator = root.Q<VisualElement>("record-indicator");
        }

        private void SubscribeEvents()
        {
            if (_recordingController == null) return;

            _recordingController.OnRecordingStarted.AddListener(OnRecordingStarted);
            _recordingController.OnRecordingStopped.AddListener(OnRecordingStopped);
            _recordingController.OnRecordingProgress.AddListener(OnRecordingProgress);
            _recordingController.OnRecordingSaved.AddListener(OnRecordingSaved);
            _recordingController.OnRecordingError.AddListener(OnRecordingError);
        }

        private void UnsubscribeEvents()
        {
            if (_recordingController == null) return;

            _recordingController.OnRecordingStarted.RemoveListener(OnRecordingStarted);
            _recordingController.OnRecordingStopped.RemoveListener(OnRecordingStopped);
            _recordingController.OnRecordingProgress.RemoveListener(OnRecordingProgress);
            _recordingController.OnRecordingSaved.RemoveListener(OnRecordingSaved);
            _recordingController.OnRecordingError.RemoveListener(OnRecordingError);
        }

        private void OnRecordButtonClicked()
        {
            _recordingController?.ToggleRecording();
        }

        private void OnRecordingStarted()
        {
            _timerLabel.style.display = DisplayStyle.Flex;
            _statusLabel.text = "Recording...";

            // Change to stop icon (square)
            _recordIndicator.style.borderTopLeftRadius = 5;
            _recordIndicator.style.borderTopRightRadius = 5;
            _recordIndicator.style.borderBottomLeftRadius = 5;
            _recordIndicator.style.borderBottomRightRadius = 5;
            _recordIndicator.style.width = 30;
            _recordIndicator.style.height = 30;
            _recordIndicator.style.left = 25;
            _recordIndicator.style.top = 25;
        }

        private void OnRecordingStopped()
        {
            _statusLabel.text = "Saving...";
        }

        private void OnRecordingProgress(float duration)
        {
            int minutes = (int)(duration / 60);
            int seconds = (int)(duration % 60);
            _timerLabel.text = $"{minutes:D2}:{seconds:D2}";
        }

        private void OnRecordingSaved(string path)
        {
            _timerLabel.style.display = DisplayStyle.None;
            _statusLabel.text = $"Saved! {path}";

            // Reset to record icon (circle)
            _recordIndicator.style.borderTopLeftRadius = 25;
            _recordIndicator.style.borderTopRightRadius = 25;
            _recordIndicator.style.borderBottomLeftRadius = 25;
            _recordIndicator.style.borderBottomRightRadius = 25;
            _recordIndicator.style.width = 50;
            _recordIndicator.style.height = 50;
            _recordIndicator.style.left = 15;
            _recordIndicator.style.top = 15;

            // Reset after delay
            Invoke(nameof(ResetStatus), 3f);
        }

        private void OnRecordingError(string error)
        {
            _timerLabel.style.display = DisplayStyle.None;
            _statusLabel.text = $"Error: {error}";
            _statusLabel.style.color = new Color(1f, 0.3f, 0.3f);

            Invoke(nameof(ResetStatus), 3f);
        }

        private void ResetStatus()
        {
            _statusLabel.text = "Tap to record";
            _statusLabel.style.color = Color.white;
        }
    }
}
