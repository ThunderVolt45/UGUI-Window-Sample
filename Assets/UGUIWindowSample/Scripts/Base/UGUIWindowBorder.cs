using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    public enum UGUIBorderPosition
    {
        North,
        South,
        East,
        West
    }

    [RequireComponent(typeof(Image))]
    public class UGUIWindowBorder : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UGUIBorderPosition borderPosition = UGUIBorderPosition.North;

        private UGUIWindowManager windowManager;
        private UGUIWindow parentWindow;

        private RectTransform windowTransform;
        private Vector2 minimumWindowSize;
        private bool isDragging = false;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            windowManager = UGUIWindowManager.Instance;
            parentWindow = GetComponentInParent<UGUIWindow>();
        }

        #region Interface
        public void OnPointerDown(PointerEventData eventData)
        {
            parentWindow.OnGetFocus();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;

            // 드래그 중에 필요한 컴포넌트와 값들을 캐싱한다.
            windowTransform = parentWindow.transform as RectTransform;
            minimumWindowSize = parentWindow.minimumWindowSize;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 크기가 고정된 창이라면 처리하지 않는다
            if (!parentWindow.isResizable) return;

            // 드래그 중이 아니라면 처리하지 않는다
            if (!isDragging) return;

            Vector2 pointerDelta = new Vector2(
                eventData.delta.x * windowManager.ScreenMultiplierWidth,
                eventData.delta.y * windowManager.ScreenMultiplierHeight
            );

            // 윈도우의 크기를 조절한다. 이 때 윈도우가 최소 크기보다 작아지지 않도록 처리한다.
            switch (borderPosition)
            {
                case UGUIBorderPosition.North:
                    if (windowTransform.sizeDelta.y + pointerDelta.y >= minimumWindowSize.y)
                    {
                        windowTransform.sizeDelta += new Vector2(0, pointerDelta.y);
                        windowTransform.anchoredPosition += new Vector2(0, pointerDelta.y / 2);
                    }
                    break;
                case UGUIBorderPosition.South:
                    if (windowTransform.sizeDelta.y - pointerDelta.y >= minimumWindowSize.y)
                    {
                        windowTransform.sizeDelta -= new Vector2(0, pointerDelta.y);
                        windowTransform.anchoredPosition += new Vector2(0, pointerDelta.y / 2);
                    }
                    break;
                case UGUIBorderPosition.East:
                    if (windowTransform.sizeDelta.x - pointerDelta.x >= minimumWindowSize.x)
                    {
                        windowTransform.sizeDelta -= new Vector2(pointerDelta.x, 0);
                        windowTransform.anchoredPosition += new Vector2(pointerDelta.x / 2, 0);
                    }
                    break;
                case UGUIBorderPosition.West:
                    if (windowTransform.sizeDelta.x + pointerDelta.x >= minimumWindowSize.x)
                    {
                        windowTransform.sizeDelta += new Vector2(pointerDelta.x, 0);
                        windowTransform.anchoredPosition += new Vector2(pointerDelta.x / 2, 0);
                    }
                    break;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }
        #endregion
    }
}