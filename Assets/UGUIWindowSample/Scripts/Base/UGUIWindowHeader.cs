using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(Image))]
    public class UGUIWindowHeader : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Header Components")]
        public TMP_Text windowTitle;
        public Button buttonExit;
        public Button buttonMaximize;
        public Button buttonMinimize;

        private UGUIWindowManager windowManager;
        private UGUIWindow parentWindow;

        private RectTransform windowTransform;
        private bool isDragging = false;
        private Vector2 normalizedPointerPositionInHeader = new Vector2(0.5f, 0.5f);

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            windowManager = UGUIWindowManager.Instance;
            parentWindow = GetComponentInParent<UGUIWindow>();

            // 버튼 이벤트 리스너 부착
            buttonExit.onClick.AddListener(parentWindow.Close);
            buttonMaximize.onClick.AddListener(MaximizeOrRestoreWindow);
            buttonMinimize.onClick.AddListener(parentWindow.Minimize);
        }

        private void MaximizeOrRestoreWindow()
        {
            switch (parentWindow.WindowMode)
            {
                case UGUIWindowMode.Windowed:
                case UGUIWindowMode.Minimized:
                    parentWindow.Maximize();
                    break;
                case UGUIWindowMode.Maximized:
                    parentWindow.RestoreWindow();
                    break;
                default:
                    UGUIWindowLog.LogError($"Window Mode {parentWindow.WindowMode} is undefined!");
                    break;
            }
        }

        #region Settings
        public void SetTitle(string title)
        {
            windowTitle.text = title;
        }

        public void SetExitButtonActive(bool exitButton)
        {
            buttonExit.gameObject.SetActive(exitButton);
        }

        public void SetMaximizeButtonActive(bool maximizeButton)
        {
            buttonMaximize.gameObject.SetActive(maximizeButton);
        }
        #endregion

        #region Pointer Event
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2)
            {
                switch (parentWindow.WindowMode)
                {
                    case UGUIWindowMode.Maximized:
                        parentWindow.WindowMode = UGUIWindowMode.Windowed;
                        break;
                    default:
                        parentWindow.WindowMode = UGUIWindowMode.Maximized;
                        break;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            parentWindow.Focus();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;

            // 드래그 중에 필요한 컴포넌트와 값들을 캐싱한다.
            windowTransform = parentWindow.transform as RectTransform;
            CachePointerPositionInHeader(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 드래그 중이 아니라면 처리하지 않는다
            if (!isDragging) return;

            if (parentWindow.WindowMode == UGUIWindowMode.Maximized)
            {
                RestoreWindowAtPointer(eventData);
                return;
            }

            // 움직일 수 없는 창이라면 처리하지 않는다
            if (!parentWindow.isMovable) return;

            RectTransform parentTransform = windowTransform.parent as RectTransform;
            Vector2 pointerDelta = windowManager.GetPointerDeltaInRect(eventData, parentTransform);

            windowTransform.anchoredPosition += pointerDelta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;

            if (parentWindow.WindowMode == UGUIWindowMode.Maximized) return;

            parentWindow.MemorizeLastWindowState();
        }

        private void CachePointerPositionInHeader(PointerEventData eventData)
        {
            RectTransform headerTransform = transform as RectTransform;
            if (headerTransform == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    headerTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointerPosition))
            {
                normalizedPointerPositionInHeader = new Vector2(0.5f, 0.5f);
                return;
            }

            Rect headerRect = headerTransform.rect;
            float normalizedX = headerRect.width > 0f
                ? Mathf.InverseLerp(headerRect.xMin, headerRect.xMax, localPointerPosition.x)
                : 0.5f;
            float normalizedY = headerRect.height > 0f
                ? Mathf.InverseLerp(headerRect.yMin, headerRect.yMax, localPointerPosition.y)
                : 0.5f;

            normalizedPointerPositionInHeader = new Vector2(
                Mathf.Clamp01(normalizedX),
                Mathf.Clamp01(normalizedY)
            );
        }

        private void RestoreWindowAtPointer(PointerEventData eventData)
        {
            parentWindow.RestoreWindow();
            windowTransform = parentWindow.transform as RectTransform;

            RectTransform parentTransform = windowTransform.parent as RectTransform;
            RectTransform headerTransform = transform as RectTransform;
            if (parentTransform == null || headerTransform == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 pointerPositionInParent))
            {
                return;
            }

            Rect headerRect = headerTransform.rect;
            Vector2 headerGrabPoint = new Vector2(
                Mathf.Lerp(headerRect.xMin, headerRect.xMax, normalizedPointerPositionInHeader.x),
                Mathf.Lerp(headerRect.yMin, headerRect.yMax, normalizedPointerPositionInHeader.y)
            );

            Vector2 headerGrabPointInParent =
                parentTransform.InverseTransformPoint(headerTransform.TransformPoint(headerGrabPoint));

            windowTransform.anchoredPosition += pointerPositionInParent - headerGrabPointInParent;
        }
        #endregion
    }
}
