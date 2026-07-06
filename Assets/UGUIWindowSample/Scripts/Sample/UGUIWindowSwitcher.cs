using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UGUIWindowSwitcher : MonoBehaviour
    {
        private static UGUIWindowSwitcher _instance;

        public static UGUIWindowSwitcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UGUIWindowSwitcher>(FindObjectsInactive.Include);

                    if (_instance != null)
                    {
                        _instance.gameObject.SetActive(true);
                    }
                    else
                    {
                        var switcherObject = new GameObject(
                            "UGUIWindowSwitcher",
                            typeof(RectTransform),
                            typeof(Canvas),
                            typeof(CanvasGroup),
                            typeof(CanvasRenderer),
                            typeof(Image),
                            typeof(GraphicRaycaster),
                            typeof(UGUIWindowSwitcher));

                        _instance = switcherObject.GetComponent<UGUIWindowSwitcher>();
                    }
                }

                return _instance;
            }
        }

        [Header("Input")]
        [SerializeField] private Key modifierKey = Key.LeftCtrl;
        [SerializeField] private Key alternateModifierKey = Key.RightCtrl;
        [SerializeField] private Key switchKey = Key.Backquote;
        [SerializeField] private Key reverseModifierKey = Key.LeftShift;
        [SerializeField] private Key alternateReverseModifierKey = Key.RightShift;

        [Header("Layout")]
        [SerializeField] private float iconSize = 64f;
        [SerializeField] private float iconSpacing = 10f;
        [SerializeField] private int sortingOrder = short.MaxValue;

        private readonly List<UGUIWindow> candidates = new();
        private readonly List<Image> itemBackgrounds = new();
        private readonly List<Image> itemIcons = new();
        private readonly List<Outline> itemOutlines = new();

        private UGUIWindowManager subscribedManager;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private RectTransform iconContainer;
        private TMP_Text titleText;
        private bool isSubscribed;
        private bool isSwitching;
        private int selectedIndex;

        private readonly Color overlayColor = new Color(0f, 0f, 0f, 0.45f);
        private readonly Color panelColor = new Color(0.08f, 0.09f, 0.11f, 0.92f);
        private readonly Color normalItemColor = new Color(0.18f, 0.2f, 0.24f, 0.96f);
        private readonly Color selectedItemColor = new Color(0.3f, 0.5f, 0.86f, 1f);
        private readonly Color minimizedItemColor = new Color(0.12f, 0.13f, 0.16f, 0.9f);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            rectTransform = transform as RectTransform;
            EnsureDefaultLayout();
            HideOverlay();
        }

        private void OnEnable()
        {
            ConfigureRootRect();
            SubscribeToManager();
        }

        private void OnDisable()
        {
            UnsubscribeFromManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromManager();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool modifierHeld = IsPressed(keyboard, modifierKey) || IsPressed(keyboard, alternateModifierKey);
            bool reverseHeld = IsPressed(keyboard, reverseModifierKey) || IsPressed(keyboard, alternateReverseModifierKey);
            bool switchPressed = WasPressedThisFrame(keyboard, switchKey);

            if (!isSwitching)
            {
                if (modifierHeld && switchPressed)
                {
                    BeginSwitch(reverseHeld ? -1 : 1);
                }

                return;
            }

            if (WasPressedThisFrame(keyboard, Key.Escape))
            {
                CancelSwitch();
                return;
            }

            if (switchPressed)
            {
                MoveSelection(reverseHeld ? -1 : 1);
            }

            if (!modifierHeld)
            {
                CommitSwitch();
            }
        }

        public void AttachToDesktop(UGUIDesktop desktop)
        {
            if (desktop == null)
            {
                return;
            }

            transform.SetParent(desktop.transform, false);
            ConfigureRootRect();
            ApplyTopMostCanvasOrder();
            transform.SetAsLastSibling();
        }

        private void SubscribeToManager()
        {
            if (isSubscribed)
            {
                return;
            }

            subscribedManager = UGUIWindowManager.Instance;
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.OnManagedWindowOpened.AddListener(HandleWindowListChanged);
            subscribedManager.OnManagedWindowClosed.AddListener(HandleWindowListChanged);
            subscribedManager.OnManagedWindowFocused.AddListener(HandleWindowListChanged);
            subscribedManager.OnManagedWindowMinimized.AddListener(HandleWindowListChanged);
            isSubscribed = true;
        }

        private void UnsubscribeFromManager()
        {
            if (!isSubscribed || subscribedManager == null)
            {
                return;
            }

            subscribedManager.OnManagedWindowOpened.RemoveListener(HandleWindowListChanged);
            subscribedManager.OnManagedWindowClosed.RemoveListener(HandleWindowListChanged);
            subscribedManager.OnManagedWindowFocused.RemoveListener(HandleWindowListChanged);
            subscribedManager.OnManagedWindowMinimized.RemoveListener(HandleWindowListChanged);
            subscribedManager = null;
            isSubscribed = false;
        }

        private void BeginSwitch(int direction)
        {
            RefreshCandidates(null);
            if (candidates.Count == 0)
            {
                return;
            }

            var focusedWindow = subscribedManager != null ? subscribedManager.GetFocusedWindow() : null;
            int focusedIndex = candidates.IndexOf(focusedWindow);

            if (candidates.Count == 1)
            {
                selectedIndex = 0;
            }
            else if (focusedIndex >= 0)
            {
                selectedIndex = WrapIndex(focusedIndex + direction, candidates.Count);
            }
            else
            {
                selectedIndex = direction >= 0 ? 0 : candidates.Count - 1;
            }

            isSwitching = true;
            ShowOverlay();
            RebuildOverlayItems();
            RefreshSelection();
        }

        private void MoveSelection(int direction)
        {
            if (candidates.Count <= 1)
            {
                RefreshSelection();
                return;
            }

            selectedIndex = WrapIndex(selectedIndex + direction, candidates.Count);
            RefreshSelection();
        }

        private void CommitSwitch()
        {
            if (subscribedManager != null && candidates.Count > 1 && selectedIndex >= 0 && selectedIndex < candidates.Count)
            {
                subscribedManager.FocusWindow(candidates[selectedIndex]);
            }

            isSwitching = false;
            HideOverlay();
        }

        private void CancelSwitch()
        {
            isSwitching = false;
            HideOverlay();
        }

        private void HandleWindowListChanged(UGUIWindow changedWindow)
        {
            if (!isSwitching)
            {
                return;
            }

            UGUIWindow selectedWindow = selectedIndex >= 0 && selectedIndex < candidates.Count
                ? candidates[selectedIndex]
                : null;

            RefreshCandidates(selectedWindow);
            if (candidates.Count == 0)
            {
                CancelSwitch();
                return;
            }

            RebuildOverlayItems();
            RefreshSelection();
        }

        private void RefreshCandidates(UGUIWindow preferredSelection)
        {
            candidates.Clear();

            if (subscribedManager == null)
            {
                subscribedManager = UGUIWindowManager.Instance;
            }

            if (subscribedManager == null)
            {
                selectedIndex = -1;
                return;
            }

            candidates.AddRange(subscribedManager.GetSwitchableWindows());

            if (candidates.Count == 0)
            {
                selectedIndex = -1;
                return;
            }

            if (preferredSelection != null)
            {
                int preferredIndex = candidates.IndexOf(preferredSelection);
                if (preferredIndex >= 0)
                {
                    selectedIndex = preferredIndex;
                    return;
                }
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, candidates.Count - 1);
        }

        private void RebuildOverlayItems()
        {
            itemBackgrounds.Clear();
            itemIcons.Clear();
            itemOutlines.Clear();

            for (int i = iconContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(iconContainer.GetChild(i).gameObject);
            }

            foreach (var window in candidates)
            {
                CreateItem(window);
            }
        }

        private void CreateItem(UGUIWindow window)
        {
            var itemObject = new GameObject(
                $"{window.name} Switch Item",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(Outline));

            itemObject.transform.SetParent(iconContainer, false);

            var itemRect = itemObject.transform as RectTransform;
            itemRect.sizeDelta = new Vector2(iconSize, iconSize);

            var layoutElement = itemObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            var background = itemObject.GetComponent<Image>();
            background.color = normalItemColor;

            var outline = itemObject.GetComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);
            outline.effectColor = Color.clear;

            var iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            iconObject.transform.SetParent(itemObject.transform, false);

            var iconRect = iconObject.transform as RectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8f, 8f);
            iconRect.offsetMax = new Vector2(-8f, -8f);

            var icon = iconObject.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.sprite = window.WindowIcon;
            icon.enabled = window.WindowIcon != null;
            icon.preserveAspect = true;

            itemBackgrounds.Add(background);
            itemIcons.Add(icon);
            itemOutlines.Add(outline);
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < itemBackgrounds.Count; i++)
            {
                var window = candidates[i];
                bool selected = i == selectedIndex;
                bool minimized = window.WindowMode == UGUIWindowMode.Minimized;

                itemBackgrounds[i].color = selected ? selectedItemColor : minimized ? minimizedItemColor : normalItemColor;
                itemOutlines[i].effectColor = selected ? Color.white : Color.clear;

                if (itemIcons[i] != null)
                {
                    itemIcons[i].sprite = window.WindowIcon;
                    itemIcons[i].enabled = window.WindowIcon != null;
                }
            }

            if (titleText != null && selectedIndex >= 0 && selectedIndex < candidates.Count)
            {
                var selectedWindow = candidates[selectedIndex];
                titleText.text = string.IsNullOrWhiteSpace(selectedWindow.WindowTitle)
                    ? selectedWindow.name
                    : selectedWindow.WindowTitle;
            }
        }

        private void EnsureDefaultLayout()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            ConfigureRootRect();

            ApplyTopMostCanvasOrder();

            canvasGroup = GetComponent<CanvasGroup>();

            var background = GetComponent<Image>();
            background.color = overlayColor;

            if (iconContainer == null || titleText == null)
            {
                CreateOverlayContent();
            }
        }

        private void CreateOverlayContent()
        {
            var panelObject = new GameObject(
                "Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));

            panelObject.transform.SetParent(transform, false);

            var panelRect = panelObject.transform as RectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(360f, 120f);

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = panelColor;

            var panelLayout = panelObject.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(16, 16, 14, 14);
            panelLayout.spacing = 12f;
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = false;
            panelLayout.childForceExpandHeight = false;

            var fitter = panelObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            iconContainer = CreateIconContainer(panelObject.transform);
            titleText = CreateTitle(panelObject.transform);
        }

        private RectTransform CreateIconContainer(Transform parent)
        {
            var containerObject = new GameObject(
                "IconContainer",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));

            containerObject.transform.SetParent(parent, false);

            var containerRect = containerObject.transform as RectTransform;
            containerRect.sizeDelta = new Vector2(0f, iconSize);

            var layout = containerObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = iconSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = containerObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return containerRect;
        }

        private TMP_Text CreateTitle(Transform parent)
        {
            var titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(parent, false);

            var titleRect = titleObject.transform as RectTransform;
            titleRect.sizeDelta = new Vector2(320f, 28f);

            var title = titleObject.AddComponent<TextMeshProUGUI>();
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
            title.fontSize = 18f;
            title.raycastTarget = false;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.textWrappingMode = TextWrappingModes.NoWrap;

            return title;
        }

        private void ConfigureRootRect()
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void ShowOverlay()
        {
            ApplyTopMostCanvasOrder();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            transform.SetAsLastSibling();
        }

        private void HideOverlay()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private bool IsPressed(Keyboard keyboard, Key key)
        {
            var control = keyboard[key];
            return control != null && control.isPressed;
        }

        private void ApplyTopMostCanvasOrder()
        {
            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        private bool WasPressedThisFrame(Keyboard keyboard, Key key)
        {
            var control = keyboard[key];
            return control != null && control.wasPressedThisFrame;
        }

        private int WrapIndex(int index, int count)
        {
            return (index % count + count) % count;
        }
    }
}
