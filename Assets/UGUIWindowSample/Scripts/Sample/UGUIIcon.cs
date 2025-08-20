using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(Image))]
    public class UGUIIcon : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image imageIcon;
        [SerializeField] private TMP_Text textIcon;
        
        [Header("Icon Settings")]
        [Tooltip("아이콘을 클릭했을 때 생성할 윈도우의 이름")]
        public string targetClassName = "UGUIWindow";

        [Tooltip("더블클릭으로 인정할 클릭 간의 최대 시간 (초)")]
        public float doubleClickThreshold = 0.3f;

        [Header("Event Listener")]
        [Space(5f)]
        public UnityEvent OnDoubleClick;

        private float lastClickTime = -1f; // 마지막 클릭 시간을 기록하는 변수 (-1로 초기화)

        #region Initialize
        private void Start()
        {
            OnDoubleClick.AddListener(OnDoubleClickIcon);
        }
        #endregion

        #region Event
        private void OnDoubleClickIcon()
        {
            Type targetWindowType = Type.GetType($"UGUIWindow.{targetClassName}", true);
            UGUIWindowManager.CreateWindow(targetWindowType);
        }
        #endregion

        #region Event Listener
        public void OnPointerClick(PointerEventData eventData)
        {
            UGUIWindowLog.Log("Click!");

            float currentTime = Time.time;

            // 마지막 클릭 후 doubleClickThreshold 안에 다시 클릭했는지 확인
            if (currentTime - lastClickTime <= doubleClickThreshold)
            {
                UGUIWindowLog.Log("Double Click!");

                // 연결된 이벤트들을 모두 호출합니다.
                OnDoubleClick?.Invoke();

                // 마지막 클릭 시간을 초기화하여 세 번째 클릭이 또 더블클릭으로 인식되는 것을 방지합니다.
                lastClickTime = -1f;
            }
            else
            {
                // 첫 번째 클릭이거나, 이전 클릭 후 시간이 많이 지남
                lastClickTime = currentTime;
            }
        }
        #endregion
    }
}
