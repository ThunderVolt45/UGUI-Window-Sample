using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [ExecuteAlways]
    [RequireComponent(typeof(ScrollRect))]
    public class UGUIWindowContent : MonoBehaviour, IPointerDownHandler
    {
        [Header("Scroll Settings")]
        [SerializeField] private bool enableHorizontalScroll = false;
        [SerializeField] private bool enableVerticalScroll = true;
        [SerializeField] private Vector2 minimumContentSize = Vector2.zero;
        [SerializeField] private float scrollbarThickness = 12f;

        [Header("Scroll Components")]
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private Scrollbar horizontalScrollbar;
        [SerializeField] private Scrollbar verticalScrollbar;

        private UGUIWindow parentWindow;
        private RectTransform rectTransform;
        private ScrollRect scrollRect;
        private bool isDirty = true;
        private bool isApplyingLayout = false;
        private bool hasLastRectSize = false;
        private Vector2 lastRectSize;

        private const float OverflowEpsilon = 0.01f;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            SetDirty();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isApplyingLayout)
            {
                return;
            }

            SetDirty();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                RefreshScrollState();
            }
#endif
        }

        private void LateUpdate()
        {
            EnsureInitialized();

#if UNITY_EDITOR
            if (!Application.isPlaying && rectTransform != null)
            {
                Vector2 currentRectSize = rectTransform.rect.size;
                if (!hasLastRectSize || currentRectSize != lastRectSize)
                {
                    SetDirty();
                }
            }
#endif

            if (!isDirty)
            {
                return;
            }

            RefreshScrollState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureInitialized();
            SetDirty();
        }
#endif

        public void SetDirty()
        {
            isDirty = true;
        }

        private void ConfigureScrollRect()
        {
            if (scrollRect == null)
            {
                return;
            }

            ConfigureScrollbar(horizontalScrollbar, false);
            ConfigureScrollbar(verticalScrollbar, true);

            scrollRect.viewport = viewport;
            scrollRect.content = scrollContent;
            scrollRect.horizontalScrollbar = horizontalScrollbar;
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        private void EnsureInitialized()
        {
            if (parentWindow == null)
            {
                parentWindow = GetComponentInParent<UGUIWindow>();
            }

            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
            }

            ResolveMissingReferences();
            ConfigureScrollRect();
        }

        private void ResolveMissingReferences()
        {
            if (viewport == null)
            {
                viewport = transform.Find("Viewport") as RectTransform;
            }

            if (scrollContent == null)
            {
                scrollContent = transform.Find("Viewport/ScrollContent") as RectTransform;
            }

            if (horizontalScrollbar == null)
            {
                Transform horizontalScrollbarTransform = transform.Find("HorizontalScrollbar");
                horizontalScrollbar = horizontalScrollbarTransform != null ? horizontalScrollbarTransform.GetComponent<Scrollbar>() : null;
            }

            if (verticalScrollbar == null)
            {
                Transform verticalScrollbarTransform = transform.Find("VerticalScrollbar");
                verticalScrollbar = verticalScrollbarTransform != null ? verticalScrollbarTransform.GetComponent<Scrollbar>() : null;
            }
        }

        private static void ConfigureScrollbar(Scrollbar scrollbar, bool isVertical)
        {
            if (scrollbar == null)
            {
                return;
            }

            scrollbar.direction = isVertical ? Scrollbar.Direction.BottomToTop : Scrollbar.Direction.LeftToRight;

            RectTransform slidingArea = scrollbar.transform.Find("Sliding Area") as RectTransform;
            RectTransform handle = slidingArea != null ? slidingArea.Find("Handle") as RectTransform : null;

            if (slidingArea != null)
            {
                slidingArea.anchorMin = Vector2.zero;
                slidingArea.anchorMax = Vector2.one;
                slidingArea.offsetMin = Vector2.zero;
                slidingArea.offsetMax = Vector2.zero;
            }

            if (handle == null)
            {
                return;
            }

            scrollbar.handleRect = handle;

            Graphic handleGraphic = handle.GetComponent<Graphic>();
            if (handleGraphic != null)
            {
                scrollbar.targetGraphic = handleGraphic;
            }
        }

        private void RefreshScrollState()
        {
            EnsureInitialized();

            if (rectTransform == null || viewport == null || scrollContent == null || scrollRect == null)
            {
                return;
            }

            isDirty = false;
            lastRectSize = rectTransform.rect.size;
            hasLastRectSize = true;
            ConfigureScrollRect();

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

            Vector2 availableSize = rectTransform.rect.size;
            Vector2 requiredSize = GetRequiredContentSize();
            float horizontalScrollbarHeight = GetScrollbarThickness(horizontalScrollbar);
            float verticalScrollbarWidth = GetScrollbarThickness(verticalScrollbar);
            bool showHorizontalScrollbar = false;
            bool showVerticalScrollbar = false;

            for (int i = 0; i < 3; i++)
            {
                Vector2 viewportSize = new Vector2(
                    Mathf.Max(0f, availableSize.x - (showVerticalScrollbar ? verticalScrollbarWidth : 0f)),
                    Mathf.Max(0f, availableSize.y - (showHorizontalScrollbar ? horizontalScrollbarHeight : 0f))
                );

                bool nextHorizontalScrollbar = enableHorizontalScroll && requiredSize.x > viewportSize.x + OverflowEpsilon;
                bool nextVerticalScrollbar = enableVerticalScroll && requiredSize.y > viewportSize.y + OverflowEpsilon;

                if (nextHorizontalScrollbar == showHorizontalScrollbar &&
                    nextVerticalScrollbar == showVerticalScrollbar)
                {
                    break;
                }

                showHorizontalScrollbar = nextHorizontalScrollbar;
                showVerticalScrollbar = nextVerticalScrollbar;
            }

            Vector2 finalViewportSize = new Vector2(
                Mathf.Max(0f, availableSize.x - (showVerticalScrollbar ? verticalScrollbarWidth : 0f)),
                Mathf.Max(0f, availableSize.y - (showHorizontalScrollbar ? horizontalScrollbarHeight : 0f))
            );

            Vector2 finalContentSize = new Vector2(
                showHorizontalScrollbar ? Mathf.Max(requiredSize.x, finalViewportSize.x) : finalViewportSize.x,
                showVerticalScrollbar ? Mathf.Max(requiredSize.y, finalViewportSize.y) : finalViewportSize.y
            );

            isApplyingLayout = true;

            ApplyViewportLayout(showHorizontalScrollbar, showVerticalScrollbar, horizontalScrollbarHeight, verticalScrollbarWidth);
            ApplyScrollbarLayout(horizontalScrollbar, showHorizontalScrollbar, showVerticalScrollbar, horizontalScrollbarHeight, verticalScrollbarWidth, false);
            ApplyScrollbarLayout(verticalScrollbar, showVerticalScrollbar, showHorizontalScrollbar, verticalScrollbarWidth, horizontalScrollbarHeight, true);
            ApplyScrollContentLayout(finalContentSize);

            scrollRect.horizontal = showHorizontalScrollbar;
            scrollRect.vertical = showVerticalScrollbar;

            if (!showHorizontalScrollbar)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
            }

            if (!showVerticalScrollbar)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

            isApplyingLayout = false;
        }

        private Vector2 GetRequiredContentSize()
        {
            return new Vector2(
                Mathf.Max(minimumContentSize.x, LayoutUtility.GetMinWidth(scrollContent)),
                Mathf.Max(minimumContentSize.y, LayoutUtility.GetMinHeight(scrollContent))
            );
        }

        private float GetScrollbarThickness(Scrollbar scrollbar)
        {
            if (scrollbar == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, scrollbarThickness);
        }

        private void ApplyViewportLayout(bool hasHorizontalScrollbar, bool hasVerticalScrollbar, float horizontalScrollbarHeight, float verticalScrollbarWidth)
        {
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = new Vector2(0f, hasHorizontalScrollbar ? horizontalScrollbarHeight : 0f);
            viewport.offsetMax = new Vector2(hasVerticalScrollbar ? -verticalScrollbarWidth : 0f, 0f);
        }

        private static void ApplyScrollbarLayout(Scrollbar scrollbar, bool isActive, bool hasOppositeScrollbar, float thickness, float oppositeThickness, bool isVertical)
        {
            if (scrollbar == null)
            {
                return;
            }

            scrollbar.gameObject.SetActive(isActive);

            RectTransform scrollbarTransform = scrollbar.transform as RectTransform;
            if (scrollbarTransform == null)
            {
                return;
            }

            if (isVertical)
            {
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scrollbarTransform.anchorMin = new Vector2(1f, 0f);
                scrollbarTransform.anchorMax = new Vector2(1f, 1f);
                scrollbarTransform.pivot = new Vector2(1f, 0.5f);
                scrollbarTransform.offsetMin = new Vector2(-thickness, hasOppositeScrollbar ? oppositeThickness : 0f);
                scrollbarTransform.offsetMax = Vector2.zero;
            }
            else
            {
                scrollbar.direction = Scrollbar.Direction.LeftToRight;
                scrollbarTransform.anchorMin = Vector2.zero;
                scrollbarTransform.anchorMax = new Vector2(1f, 0f);
                scrollbarTransform.pivot = new Vector2(0.5f, 0f);
                scrollbarTransform.offsetMin = Vector2.zero;
                scrollbarTransform.offsetMax = new Vector2(hasOppositeScrollbar ? -oppositeThickness : 0f, thickness);
            }
        }

        private void ApplyScrollContentLayout(Vector2 contentSize)
        {
            scrollContent.anchorMin = new Vector2(0f, 1f);
            scrollContent.anchorMax = new Vector2(0f, 1f);
            scrollContent.pivot = new Vector2(0f, 1f);
            scrollContent.sizeDelta = contentSize;
        }

        #region Interface
        public void OnPointerDown(PointerEventData eventData)
        {
            if (parentWindow != null)
            {
                parentWindow.Focus();
            }
        }
        #endregion
    }
}
