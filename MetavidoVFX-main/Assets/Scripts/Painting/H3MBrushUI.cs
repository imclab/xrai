using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace XRRAI.BrushPainting
{
    public class H3MBrushUI : MonoBehaviour
    {
        [Header("Dependencies")]
        public H3MParticleBrushManager brushManager;
        public H3MBrushCatalog brushCatalog;

        [Header("UI References")]
        public RectTransform buttonContainer;
        public GameObject buttonPrefab; // Needs a Button and Text component

        void Start()
        {
            if (brushManager == null) brushManager = FindObjectOfType<H3MParticleBrushManager>();

            // If no catalog assigned, try to find one
            if (brushCatalog == null && brushManager != null)
            {
                brushCatalog = brushManager.brushCatalog;
            }

            GenerateButtons();
        }

        void GenerateButtons()
        {
            if (brushCatalog == null || buttonPrefab == null || buttonContainer == null)
            {
                Debug.LogWarning("H3MBrushUI: Missing references.");
                return;
            }

            // Clear existing
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            // Create buttons for each brush
            foreach (var brush in brushCatalog.allBrushes)
            {
                GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
                btnObj.name = $"Btn_{brush.brushName}";

                // Setup Text
                var text = btnObj.GetComponentInChildren<Text>();
                if (text) text.text = brush.brushName;

                // Setup Click
                var btn = btnObj.GetComponent<Button>();
                if (btn)
                {
                    string brushName = brush.brushName; // Capture for lambda
                    btn.onClick.AddListener(() => OnBrushSelected(brushName));
                }
            }
        }

        void OnBrushSelected(string brushName)
        {
            Debug.Log($"Selected Brush: {brushName}");
            if (brushManager != null)
            {
                brushManager.SelectBrush(brushName);
            }
        }
    }
}
