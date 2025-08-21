using UnityEngine;

namespace UGUIWindow
{
    public class UGUIWindowState
    {
        #region Position & Size
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        #endregion

        #region State
        public bool hasHeader;
        public bool hasBorder;
        // public bool hasExitButton;
        // public bool hasMaximizeButton;
        public bool isMovable;
        public bool isResizable;
        #endregion

        #region Constructor
        public UGUIWindowState()
        {

        }

        /// <summary>
        /// UGUIWindowState에 Window의 상태를 기록하는 생성자
        /// </summary>
        /// <param name="targetWindowToSave">State에 기록할 Window</param>
        public UGUIWindowState(UGUIWindow targetWindowToSave)
        {
            RectTransform windowTransform = targetWindowToSave.transform as RectTransform;

            anchorMin = windowTransform.anchorMin;
            anchorMax = windowTransform.anchorMax;
            anchoredPosition = windowTransform.anchoredPosition;
            sizeDelta = windowTransform.sizeDelta;

            hasHeader = targetWindowToSave.HasHeader;
            hasBorder = targetWindowToSave.HasBorder;
            // hasExitButton = targetWindowToSave.HasExitButton;
            // hasMaximizeButton = targetWindowToSave.HasMaximizeButton;

            isMovable = targetWindowToSave.isMovable;
            isResizable = targetWindowToSave.isResizable;
        }
        #endregion

        #region Functions
        /// <summary>
        /// State에 기록된 값을 Window로 복원하는 함수
        /// </summary>
        /// <param name="targetWindowToRestore">복원할 Window</param>
        public void RestoreWindowFromState(UGUIWindow targetWindowToRestore)
        {
            RectTransform windowTransform = targetWindowToRestore.transform as RectTransform;

            windowTransform.anchorMin = anchorMin;
            windowTransform.anchorMax = anchorMax;
            windowTransform.anchoredPosition = anchoredPosition;
            windowTransform.sizeDelta = sizeDelta;

            targetWindowToRestore.HasHeader = hasHeader;
            targetWindowToRestore.HasBorder = hasBorder;
            // targetWindowToRestore.HasExitButton = hasExitButton;
            // targetWindowToRestore.HasMaximizeButton = hasMaximizeButton;

            targetWindowToRestore.isMovable = isMovable;
            targetWindowToRestore.isResizable = isResizable;
        }
        #endregion
    }
}
