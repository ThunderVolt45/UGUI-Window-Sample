using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UGUITaskBar : MonoBehaviour
    {
        private static UGUITaskBar _instance;

        public static UGUITaskBar Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UGUITaskBar>(FindObjectsInactive.Include);

                    if (_instance != null)
                    {
                        _instance.gameObject.SetActive(true);
                    }
                    else
                    {
                        var taskBarObject = new GameObject(
                            "UGUITaskBar",
                            typeof(RectTransform),
                            typeof(Canvas),
                            typeof(CanvasRenderer),
                            typeof(Image),
                            typeof(GraphicRaycaster),
                            typeof(UGUITaskBar));

                        _instance = taskBarObject.GetComponent<UGUITaskBar>();
                    }
                }

                return _instance;
            }
        }

        [Header("UI Elements")]
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private UGUITaskIcon taskIconPrefab = null;

        [Header("Layout")]
        [SerializeField] private float taskBarHeight = 48f;
        [SerializeField] private float iconSize = 36f;
        [SerializeField] private float iconSpacing = 6f;
        [SerializeField] private int sortingOrder = 10;

        private readonly Dictionary<UGUIWindow, UGUITaskIcon> icons = new();

        private UGUIWindowManager subscribedManager;
        private UGUIWindowManager maximizedWindowAreaManager;
        private RectTransform rectTransform;
        private bool isSubscribed;
        private bool registeredMaximizedWindowArea;

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
        }

        private void OnEnable()
        {
            ConfigureTaskBarRect();
            SubscribeToManager();
            RebuildFromManager();
        }

        private void OnDisable()
        {
            UnsubscribeFromManager();
            ClearMaximizedWindowArea();
        }

        private void OnDestroy()
        {
            UnsubscribeFromManager();

            if (_instance == this)
            {
                _instance = null;
            }

            ClearMaximizedWindowArea();
        }

        public void AttachToDesktop(UGUIDesktop desktop)
        {
            if (desktop == null)
            {
                return;
            }

            transform.SetParent(desktop.transform, false);
            ConfigureTaskBarRect();
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

            subscribedManager.OnManagedWindowOpened.AddListener(HandleWindowOpened);
            subscribedManager.OnManagedWindowClosed.AddListener(HandleWindowClosed);
            subscribedManager.OnManagedWindowFocused.AddListener(HandleWindowFocused);
            subscribedManager.OnManagedWindowMinimized.AddListener(HandleWindowMinimized);

            isSubscribed = true;
        }

        private void UnsubscribeFromManager()
        {
            if (!isSubscribed || subscribedManager == null)
            {
                return;
            }

            subscribedManager.OnManagedWindowOpened.RemoveListener(HandleWindowOpened);
            subscribedManager.OnManagedWindowClosed.RemoveListener(HandleWindowClosed);
            subscribedManager.OnManagedWindowFocused.RemoveListener(HandleWindowFocused);
            subscribedManager.OnManagedWindowMinimized.RemoveListener(HandleWindowMinimized);

            subscribedManager = null;
            isSubscribed = false;
        }

        private void RebuildFromManager()
        {
            if (subscribedManager == null)
            {
                return;
            }

            foreach (var window in subscribedManager.ManagedVisibleWindows)
            {
                HandleWindowOpened(window);
            }
        }

        private void HandleWindowOpened(UGUIWindow window)
        {
            if (window == null)
            {
                return;
            }

            if (!icons.ContainsKey(window))
            {
                icons.Add(window, CreateIcon(window));
            }

            RefreshItems(window);
        }

        private void HandleWindowClosed(UGUIWindow window)
        {
            if (window == null || !icons.TryGetValue(window, out var icon))
            {
                return;
            }

            icons.Remove(window);
            Destroy(icon.gameObject);
        }

        private void HandleWindowFocused(UGUIWindow window)
        {
            RefreshItems(window);
        }

        private void HandleWindowMinimized(UGUIWindow window)
        {
            if (window == null)
            {
                return;
            }

            if (!icons.ContainsKey(window))
            {
                icons.Add(window, CreateIcon(window));
            }

            RefreshItems(null);
        }

        private UGUITaskIcon CreateIcon(UGUIWindow window)
        {
            UGUITaskIcon icon = taskIconPrefab != null
                ? Instantiate(taskIconPrefab, iconContainer)
                : CreateDefaultIcon();

            icon.name = $"{window.name} Icon";
            icon.Initialize(window);

            return icon;
        }

        private UGUITaskIcon CreateDefaultIcon()
        {
            var iconObject = new GameObject(
                "TaskIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(UGUITaskIcon));

            iconObject.transform.SetParent(iconContainer, false);

            var iconRect = iconObject.transform as RectTransform;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            var background = iconObject.GetComponent<Image>();
            background.color = new Color(0.18f, 0.2f, 0.24f, 0.96f);

            var layoutElement = iconObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0f;

            var windowIconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            windowIconObject.transform.SetParent(iconObject.transform, false);

            var windowIconRect = windowIconObject.transform as RectTransform;
            windowIconRect.anchorMin = Vector2.zero;
            windowIconRect.anchorMax = Vector2.one;
            windowIconRect.offsetMin = new Vector2(5f, 5f);
            windowIconRect.offsetMax = new Vector2(-5f, -5f);

            var windowIconImage = windowIconObject.GetComponent<Image>();
            windowIconImage.raycastTarget = false;
            windowIconImage.enabled = false;
            windowIconImage.preserveAspect = true;

            var icon = iconObject.GetComponent<UGUITaskIcon>();
            icon.SetReferences(background, windowIconImage);

            return icon;
        }

        private void EnsureDefaultLayout()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            ConfigureTaskBarRect();

            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (iconContainer == null)
            {
                iconContainer = CreateDefaultContainer();
            }
        }

        private RectTransform CreateDefaultContainer()
        {
            var containerObject = new GameObject(
                "IconContainer",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));

            containerObject.transform.SetParent(transform, false);

            var containerRect = containerObject.transform as RectTransform;
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(8f, 6f);
            containerRect.offsetMax = new Vector2(-8f, -6f);

            var layout = containerObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = iconSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return containerRect;
        }

        private void ConfigureTaskBarRect()
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, taskBarHeight);

            var manager = UGUIWindowManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.SetMaximizedWindowOffsets(
                new Vector2(0f, taskBarHeight),
                Vector2.zero);
            maximizedWindowAreaManager = manager;
            registeredMaximizedWindowArea = true;
        }

        private void ClearMaximizedWindowArea()
        {
            if (!registeredMaximizedWindowArea)
            {
                return;
            }

            if (maximizedWindowAreaManager != null)
            {
                maximizedWindowAreaManager.ClearMaximizedWindowOffsets();
            }

            maximizedWindowAreaManager = null;
            registeredMaximizedWindowArea = false;
        }

        private void RefreshItems(UGUIWindow focusedWindow)
        {
            foreach (var icon in icons.Values)
            {
                icon.Refresh(icon.TargetWindow == focusedWindow);
            }
        }
    }
}
