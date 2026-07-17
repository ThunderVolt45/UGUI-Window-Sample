using System.Collections.Generic;
using TMPro;
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

        [Header("Pinned Apps")]
        [Tooltip("작업 표시줄에 항상 고정해 둘 앱의 창 클래스명(예: MyWindow). 여기 적힌 순서대로 왼쪽부터 놓이며, " +
                 "창이 없어도 남아 클릭하면 실행된다. 실행 중인 창은 자기 앱의 핀 아이콘에 자동으로 붙는다.")]
        [SerializeField] private List<string> pinnedApps = new();

        [Header("Tooltip")]
        [Tooltip("호버한 아이콘 위에 앱 이름을 띄우는 툴팁. 비워두면 런타임에 기본 툴팁을 만든다.")]
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private TMP_Text tooltipLabel;
        [Tooltip("작업 표시줄 위쪽 끝과 툴팁 사이 간격(px).")]
        [SerializeField] private float tooltipGap = 8f;
        [Tooltip("툴팁 배경이 글자 주위로 확보하는 여백(px, 가로/세로).")]
        [SerializeField] private Vector2 tooltipPadding = new Vector2(10f, 6f);
        [SerializeField] private float tooltipFadeSpeed = 20f;

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

        // 창별 아이콘. 핀 아이콘에 붙은 창도 여기 등록되므로, 창 → 아이콘 조회는 항상 이 딕셔너리로 한다.
        private readonly Dictionary<UGUIWindow, UGUITaskIcon> icons = new();

        // 앱 클래스명 → 핀 아이콘. 창이 없어도 살아 있는 런처들.
        private readonly Dictionary<string, UGUITaskIcon> pinnedIcons = new();

        private CanvasGroup tooltipGroup;
        private string tooltipShownText;

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
            BuildPinnedIcons();
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
            UpdateTooltip();
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
                icons.Add(window, AcquireIcon(window));
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

            // 핀 아이콘은 창이 닫혀도 런처로 남고, 창 전용 아이콘만 작업 표시줄에서 사라진다.
            if (icon.IsPinned)
            {
                icon.Unbind();
            }
            else
            {
                Destroy(icon.gameObject);
            }
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
                icons.Add(window, AcquireIcon(window));
            }

            RefreshItems(null);
        }

        // 열린 창에 붙일 아이콘을 고른다. 자기 앱의 핀 아이콘이 비어 있으면 그것을 재사용하고,
        // 없으면 창 전용 아이콘을 새로 만들어 핀 아이콘들 뒤에 붙인다.
        private UGUITaskIcon AcquireIcon(UGUIWindow window)
        {
            // 다중 인스턴스 앱은 창마다 아이콘을 따로 둔다. 창 여러 개가 핀 아이콘 하나를 공유하면
            // 어느 창이 포커스인지 표현할 수 없기 때문이다. 핀 아이콘은 런처로 남는다.
            if (!window.allowMultipleInstance
                && pinnedIcons.TryGetValue(window.GetType().Name, out var pinnedIcon)
                && pinnedIcon != null
                && !pinnedIcon.IsRunning)
            {
                pinnedIcon.Bind(window);
                return pinnedIcon;
            }

            return CreateIcon(window);
        }

        private void BuildPinnedIcons()
        {
            foreach (var appClassName in pinnedApps)
            {
                if (string.IsNullOrWhiteSpace(appClassName) || pinnedIcons.ContainsKey(appClassName))
                {
                    continue;
                }

                UGUITaskIcon icon = taskIconPrefab != null
                    ? Instantiate(taskIconPrefab, iconContainer)
                    : CreateDefaultIcon();

                icon.name = $"{appClassName} Icon (Pinned)";
                icon.InitializePinned(appClassName);

                pinnedIcons.Add(appClassName, icon);
            }
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

            EnsureTooltip();
        }

        private void EnsureTooltip()
        {
            if (tooltipRoot == null)
            {
                tooltipRoot = CreateDefaultTooltip();
            }

            // 작업 표시줄 하단 중앙 기준 → anchoredPosition.x에 아이콘의 로컬 x를 그대로 넣을 수 있다.
            tooltipRoot.anchorMin = new Vector2(0.5f, 0f);
            tooltipRoot.anchorMax = new Vector2(0.5f, 0f);
            tooltipRoot.pivot = new Vector2(0.5f, 0f);

            tooltipGroup = tooltipRoot.GetComponent<CanvasGroup>();
            if (tooltipGroup == null)
            {
                tooltipGroup = tooltipRoot.gameObject.AddComponent<CanvasGroup>();
            }

            tooltipGroup.alpha = 0f;
            // 툴팁이 포인터를 가로채면 아이콘 호버가 끊겨 툴팁이 깜빡인다.
            tooltipGroup.blocksRaycasts = false;
            tooltipGroup.interactable = false;

            if (tooltipLabel == null)
            {
                tooltipLabel = tooltipRoot.GetComponentInChildren<TMP_Text>(true);
            }
        }

        // 프리팹에 툴팁이 지정되지 않았을 때의 기본 툴팁.
        // iconContainer가 아니라 작업 표시줄 루트에 붙인다 — 컨테이너의 LayoutGroup이 툴팁을
        // 아이콘처럼 한 칸으로 취급하지 않게 하기 위해서다.
        private RectTransform CreateDefaultTooltip()
        {
            var tooltipObject = new GameObject(
                "TaskIconTooltip",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));

            tooltipObject.transform.SetParent(transform, false);

            var background = tooltipObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.13f, 0.16f, 0.95f);
            background.raycastTarget = false;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            labelObject.transform.SetParent(tooltipObject.transform, false);

            var labelRect = labelObject.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.color = Color.white;
            label.raycastTarget = false;

            tooltipLabel = label;

            return tooltipObject.transform as RectTransform;
        }

        // 호버 판정은 UGUITaskIcon이 포인터 이벤트로 내리므로(레이캐스트와 같은 좌표계),
        // 여기서는 그 결과를 읽기만 하고 포인터를 다시 계산하지 않는다.
        private void UpdateTooltip()
        {
            if (tooltipRoot == null || tooltipGroup == null)
            {
                return;
            }

            UGUITaskIcon hovered = FindHoveredIcon();

            // 작업 표시줄 자체가 숨어 있으면(전체화면 등) 이름도 같이 감춘다.
            bool show = hovered != null && taskBarShouldShow;

            if (show)
            {
                string label = hovered.DisplayName;
                if (label != tooltipShownText)
                {
                    tooltipShownText = label;
                    tooltipLabel.text = label;
                    ResizeTooltipToText();
                    tooltipRoot.SetAsLastSibling();
                }

                // 아이콘 X 중심을 작업 표시줄 로컬 좌표로 구한다. anchoredPosition을 쓰지 않는 이유는
                // 아이콘이 LayoutGroup 아래에 있어 앵커 기준이 다를 수 있기 때문이다.
                float iconX = rectTransform.InverseTransformPoint(hovered.RectTransform.position).x;
                tooltipRoot.anchoredPosition = new Vector2(iconX, taskBarHeight + tooltipGap);
            }

            float target = show ? 1f : 0f;
            if (!Mathf.Approximately(tooltipGroup.alpha, target))
            {
                float t = 1f - Mathf.Exp(-tooltipFadeSpeed * Time.unscaledDeltaTime);
                tooltipGroup.alpha = Mathf.Lerp(tooltipGroup.alpha, target, t);
                if (Mathf.Abs(tooltipGroup.alpha - target) < 0.004f)
                {
                    tooltipGroup.alpha = target;
                }
            }
        }

        private UGUITaskIcon FindHoveredIcon()
        {
            foreach (var icon in pinnedIcons.Values)
            {
                if (icon != null && icon.IsHovered)
                {
                    return icon;
                }
            }

            foreach (var icon in icons.Values)
            {
                if (icon != null && icon.IsHovered)
                {
                    return icon;
                }
            }

            return null;
        }

        // LayoutGroup/ContentSizeFitter 대신 직접 크기를 준다.
        private void ResizeTooltipToText()
        {
            if (tooltipLabel == null)
            {
                return;
            }

            Vector2 textSize = tooltipLabel.GetPreferredValues(tooltipShownText);
            tooltipRoot.sizeDelta = new Vector2(
                textSize.x + tooltipPadding.x * 2f,
                textSize.y + tooltipPadding.y * 2f);
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
