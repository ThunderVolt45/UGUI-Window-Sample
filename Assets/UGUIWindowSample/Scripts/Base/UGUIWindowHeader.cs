using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(Image))]
    public class UGUIWindowHeader : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
        public void OnPointerDown(PointerEventData eventData)
        {
            parentWindow.Focus();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;

            // 드래그 중에 필요한 컴포넌트와 값들을 캐싱한다.
            windowTransform = parentWindow.transform as RectTransform;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 움직일 수 없는 창이라면 처리하지 않는다
            if (!parentWindow.isMovable) return;

            // 드래그 중이 아니라면 처리하지 않는다
            if (!isDragging) return;

            Vector2 pointerDelta = new Vector2(
                eventData.delta.x * windowManager.ScreenMultiplierWidth,
                eventData.delta.y * windowManager.ScreenMultiplierHeight
            );

            windowTransform.anchoredPosition += pointerDelta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            parentWindow.MemorizeLastWindowState();
        }
        #endregion
    }
}
