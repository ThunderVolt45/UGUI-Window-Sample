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

        [Header("FullScreen Visibility")]
        [Tooltip("전체화면인 Window가 있을 때 작업 표시줄을 숨길지 여부")]
        [SerializeField] private bool hideOnFullScreen = true;

        [Tooltip("전체화면일 때 화면 아래쪽 이 범위 안으로 포인터가 들어오면 작업 표시줄이 다시 나온다. " +
                 "Canvas 좌표가 아니라 실제 화면 픽셀이라, 화면 배율이나 창 크기가 바뀌어도 잡히는 폭은 같다.")]
        [SerializeField] private float fullScreenRevealZonePixels = 32f;

        [Tooltip("작업 표시줄이 숨고 나타나는 페이드 속도. 클수록 빠름.")]
        [SerializeField] private float visibilityFadeSpeed = 14f;

        private readonly Dictionary<UGUIWindow, UGUITaskIcon> icons = new();

        private UGUIWindowManager subscribedManager;
        private UGUIWindowManager maximizedWindowAreaManager;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvas;
        private bool isSubscribed;
        private bool registeredMaximizedWindowArea;
        private bool taskBarShouldShow = true;

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
            UpdateTaskBarVisibility(true); // 활성화 시점 상태로 즉시 스냅(첫 프레임 플래시 방지)
        }

        private void Update()
        {
            // 전체화면 여부와 포인터 위치는 매 프레임 바뀔 수 있으므로 여기서 다시 판단한다.
            UpdateTaskBarVisibility(false);
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

            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (iconContainer == null)
            {
                iconContainer = CreateDefaultContainer();
            }
        }

        /// <summary>
        /// 전체화면인 Window가 있으면 작업 표시줄을 숨기고, 화면 아래쪽에 포인터를 대면 다시 꺼낸다.
        /// instant=true면 페이드 없이 즉시 적용한다.
        /// </summary>
        private void UpdateTaskBarVisibility(bool instant)
        {
            taskBarShouldShow = !IsHiddenByFullScreen() || IsPointerInRevealZone();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    return;
                }
            }

            float target = taskBarShouldShow ? 1f : 0f;

            if (instant)
            {
                canvasGroup.alpha = target;
            }
            else if (!Mathf.Approximately(canvasGroup.alpha, target))
            {
                float t = 1f - Mathf.Exp(-visibilityFadeSpeed * Time.unscaledDeltaTime);
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, t);

                if (Mathf.Abs(canvasGroup.alpha - target) < 0.004f)
                {
                    canvasGroup.alpha = target;
                }
            }
            else
            {
                return;
            }

            bool visible = canvasGroup.alpha > 0.01f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }

        private bool IsHiddenByFullScreen()
        {
            if (!hideOnFullScreen)
            {
                return false;
            }

            var manager = subscribedManager != null ? subscribedManager : UGUIWindowManager.Instance;
            return manager != null && manager.HasFullScreenWindow;
        }

        // 숨은 작업 표시줄은 raycast를 받지 못하므로, 포인터 이벤트가 아니라 좌표로 판정한다.
        private bool IsPointerInRevealZone()
        {
            var parentRect = rectTransform != null ? rectTransform.parent as RectTransform : null;
            if (parentRect == null)
            {
                return false;
            }

            if (!UGUIWindowManager.TryGetPointerScreenPosition(out Vector2 screenPosition))
            {
                return false;
            }

            // Canvas가 ScreenSpaceOverlay이므로 카메라는 null.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPosition,
                    null,
                    out Vector2 localPointer))
            {
                return false;
            }

            // 이미 나와 있으면 감지 범위를 작업 표시줄 높이까지 넓혀,
            // 포인터를 그 위에 올려둔 채로 아이콘을 누를 수 있게 한다.
            Rect area = parentRect.rect;
            float zoneHeight = taskBarShouldShow
                ? taskBarHeight
                : UGUIWindowManager.PixelsToCanvasUnits(fullScreenRevealZonePixels, canvas);

            return localPointer.y <= area.yMin + zoneHeight
                && localPointer.x >= area.xMin
                && localPointer.x <= area.xMax;
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
