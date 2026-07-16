using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

        /// <summary>
        /// macOS 초록 버튼과 같은 규칙: 그냥 누르면 전체화면, Option(Alt)을 누른 채로 누르면 확대.
        /// </summary>
        private void MaximizeOrRestoreWindow()
        {
            if (IsZoomModifierHeld())
            {
                ToggleZoom();
                return;
            }

            ToggleFullScreen();
        }

        private static bool IsZoomModifierHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
        }

        /// <summary>확대(zoom) ↔ 창 모드 토글. 전체화면 상태에서 부르면 창 모드로 빠져나온다.</summary>
        private void ToggleZoom()
        {
            switch (parentWindow.WindowMode)
            {
                case UGUIWindowMode.Maximized:
                case UGUIWindowMode.FullScreen:
                    parentWindow.RestoreWindow();
                    break;
                default:
                    parentWindow.Maximize();
                    break;
            }
        }

        /// <summary>전체화면 ↔ 직전 레이아웃 토글.</summary>
        private void ToggleFullScreen()
        {
            if (parentWindow.WindowMode == UGUIWindowMode.FullScreen)
            {
                parentWindow.ExitFullScreen();
                return;
            }

            parentWindow.EnterFullScreen();
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
            // macOS와 동일하게 타이틀바 더블클릭은 확대(zoom)이지 전체화면이 아니다.
            if (eventData.clickCount == 2)
            {
                ToggleZoom();
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

            // 확대·전체화면 상태에서 헤더를 끌면 포인터 위치에서 창 모드로 떨어져 나온다.
            if (parentWindow.WindowMode == UGUIWindowMode.Maximized ||
                parentWindow.WindowMode == UGUIWindowMode.FullScreen)
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

            if (parentWindow.WindowMode == UGUIWindowMode.Maximized ||
                parentWindow.WindowMode == UGUIWindowMode.FullScreen)
            {
                return;
            }

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
