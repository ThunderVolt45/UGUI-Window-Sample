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
        private const string PrefabResourcePath = "Sample/UGUIWindowSwitcher";

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
                        return _instance;
                    }

                    var switcherPrefab = Resources.Load<UGUIWindowSwitcher>(PrefabResourcePath);
                    if (switcherPrefab == null)
                    {
                        UGUIWindowLog.LogWarning($"UGUIWindowSwitcher prefab was not found at Resources/{PrefabResourcePath}.prefab.");
                        return null;
                    }

                    _instance = Instantiate(switcherPrefab);
                    _instance.name = switcherPrefab.name;
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

        [Header("UI References")]
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private TMP_Text titleText;

        [Header("Colors")]
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color normalItemColor = new Color(0.18f, 0.2f, 0.24f, 0.96f);
        [SerializeField] private Color selectedItemColor = new Color(0.3f, 0.5f, 0.86f, 1f);
        [SerializeField] private Color minimizedItemColor = new Color(0.12f, 0.13f, 0.16f, 0.9f);

        private readonly List<UGUIWindow> candidates = new();
        private readonly List<Image> itemBackgrounds = new();
        private readonly List<Image> itemIcons = new();
        private readonly List<Outline> itemOutlines = new();

        private UGUIWindowManager subscribedManager;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool isSubscribed;
        private bool isSwitching;
        private int selectedIndex;
        private bool hasRequiredReferences;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            rectTransform = transform as RectTransform;
            InitializeFromPrefab();
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

        public void AttachTo(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            transform.SetParent(parent, false);
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
            if (!hasRequiredReferences)
            {
                return;
            }

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
            RebuildOverlayItems();
            RefreshSelection();
            RefreshOverlayLayout();
            ShowOverlay();
        }

        private void MoveSelection(int direction)
        {
            if (candidates.Count <= 1)
            {
                RefreshSelection();
                RefreshOverlayLayout();
                return;
            }

            selectedIndex = WrapIndex(selectedIndex + direction, candidates.Count);
            RefreshSelection();
            RefreshOverlayLayout();
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
            RefreshOverlayLayout();
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
            if (iconContainer == null)
            {
                return;
            }

            itemBackgrounds.Clear();
            itemIcons.Clear();
            itemOutlines.Clear();

            for (int i = iconContainer.childCount - 1; i >= 0; i--)
            {
                var child = iconContainer.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
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

        private void RefreshOverlayLayout()
        {
            if (iconContainer == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(iconContainer);

            var parentRect = iconContainer.parent as RectTransform;
            while (parentRect != null && parentRect != rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                parentRect = parentRect.parent as RectTransform;
            }

            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }

            Canvas.ForceUpdateCanvases();
        }

        private void InitializeFromPrefab()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            ConfigureRootRect();

            ApplyTopMostCanvasOrder();

            canvasGroup = GetComponent<CanvasGroup>();

            var background = GetComponent<Image>();
            if (background != null)
            {
                background.color = overlayColor;
            }

            hasRequiredReferences = ResolvePrefabReferences();
        }

        private bool ResolvePrefabReferences()
        {
            if (iconContainer == null)
            {
                iconContainer = transform.Find("Panel/IconContainer") as RectTransform;
            }

            if (titleText == null)
            {
                titleText = transform.Find("Panel/Title")?.GetComponent<TMP_Text>();
            }

            if (iconContainer != null && titleText != null)
            {
                return true;
            }

            UGUIWindowLog.LogWarning($"{name} is missing IconContainer or Title references. Window switching overlay will be disabled.", this);
            return false;
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
            if (canvasGroup == null)
            {
                return;
            }

            ApplyTopMostCanvasOrder();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            transform.SetAsLastSibling();
        }

        private void HideOverlay()
        {
            if (canvasGroup == null)
            {
                return;
            }

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
