// MainMenuUI.cs - UI Toolkit main menu for spec scene selection
// Displays grid of spec demo buttons matching VFXToggleUI style

using UnityEngine;
using UnityEngine.UIElements;

namespace MetavidoVFX.UI.Navigation
{
    /// <summary>
    /// UI Toolkit main menu for selecting spec demo scenes.
    /// Auto-generates buttons for all spec scenes.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Layout")]
        [SerializeField] private int _columns = 2;

        #endregion

        #region Private Fields

        private UIDocument _document;
        private VisualElement _root;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            CreateUI();
        }

        #endregion

        #region UI Creation

        private void CreateUI()
        {
            if (_document == null || _document.rootVisualElement == null) return;

            _root = _document.rootVisualElement;
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;
            _root.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f);

            // Container panel
            var container = new VisualElement { name = "menu-container" };
            container.style.width = new Length(90, LengthUnit.Percent);
            container.style.maxWidth = 800;
            container.style.paddingTop = container.style.paddingBottom = 30;
            container.style.paddingLeft = container.style.paddingRight = 20;

            // Title
            var title = new Label("MetavidoVFX Spec Demos");
            title.style.fontSize = 32;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 30;
            container.Add(title);

            // Button grid
            var grid = new VisualElement { name = "button-grid" };
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.Center;

            // Create buttons for each spec scene
            for (int i = 0; i < SceneNavigator.SpecScenes.Length; i++)
            {
                var btn = CreateSpecButton(i);
                grid.Add(btn);
            }

            container.Add(grid);

            // Version text
            var version = new Label($"Unity {Application.unityVersion}");
            version.style.fontSize = 12;
            version.style.color = new Color(1, 1, 1, 0.4f);
            version.style.unityTextAlign = TextAnchor.MiddleCenter;
            version.style.marginTop = 30;
            container.Add(version);

            _root.Add(container);
        }

        private Button CreateSpecButton(int specIndex)
        {
            var btn = new Button(() => OnSpecButtonClick(specIndex));
            btn.text = SceneNavigator.SpecDisplayNames[specIndex];
            btn.name = $"spec-btn-{specIndex}";

            // Styling
            btn.style.width = new Length(100f / _columns - 2, LengthUnit.Percent);
            btn.style.minWidth = 180;
            btn.style.height = 50;
            btn.style.marginTop = btn.style.marginBottom = 6;
            btn.style.marginLeft = btn.style.marginRight = 6;
            btn.style.fontSize = 13;
            btn.style.color = Color.white;
            btn.style.backgroundColor = new Color(0.2f, 0.35f, 0.6f);
            btn.style.borderTopWidth = btn.style.borderBottomWidth =
                btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
                btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 6;

            // Hover effect via USS class manipulation
            btn.RegisterCallback<MouseEnterEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.3f, 0.45f, 0.75f);
                btn.style.scale = new Scale(new Vector2(1.02f, 1.02f));
            });
            btn.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.2f, 0.35f, 0.6f);
                btn.style.scale = new Scale(Vector2.one);
            });

            return btn;
        }

        private void OnSpecButtonClick(int specIndex)
        {
            Debug.Log($"[MainMenuUI] Loading spec {specIndex}: {SceneNavigator.SpecDisplayNames[specIndex]}");
            SceneNavigator.Instance.LoadSpecScene(specIndex);
        }

        #endregion
    }
}
