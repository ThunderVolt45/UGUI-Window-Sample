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
        [SerializeField] private Image imageBackground;
        [SerializeField] private TMP_Text textIcon;
        
        [Header("Icon Settings")]
        [Tooltip("아이콘을 클릭했을 때 생성할 윈도우의 이름")]
        public string targetClassName = "UGUIWindow";

        [Tooltip("더블클릭으로 인정할 클릭 간의 최대 시간 (초)")]
        public float doubleClickThreshold = 0.3f;

        [Header("Event Listener")]
        [Space(5f)]
        public UnityEvent OnDoubleClickIcon;

        private float lastClickTime = -1f; // 마지막 클릭 시간을 기록하는 변수 (-1로 초기화)

        #region Initialize
        private void Awake()
        {
            ResolveReferences();
            ApplyTargetWindowIcon();
        }

        private void Start()
        {
            OnDoubleClickIcon.AddListener(OpenWindow);
        }
        #endregion

        #region Functions
        private void ResolveReferences()
        {
            if (imageBackground == null)
            {
                imageBackground = GetComponent<Image>();
            }

            if (imageIcon == null)
            {
                imageIcon = GetComponent<Image>();
            }

            if (imageIcon == imageBackground)
            {
                Image[] childImages = GetComponentsInChildren<Image>(true);
                foreach (Image childImage in childImages)
                {
                    if (childImage != imageBackground)
                    {
                        imageIcon = childImage;
                        break;
                    }
                }
            }
        }

        private void ApplyTargetWindowIcon()
        {
            if (imageIcon == null || string.IsNullOrWhiteSpace(targetClassName))
            {
                return;
            }

            var windowPrefab = Resources.Load<GameObject>($"Windows/{targetClassName}");
            if (windowPrefab == null || !windowPrefab.TryGetComponent(out UGUIWindow targetWindow))
            {
                return;
            }

            Sprite windowIcon = targetWindow.WindowIcon;
            if (windowIcon == null)
            {
                return;
            }

            imageIcon.sprite = windowIcon;
            imageIcon.enabled = true;
            imageIcon.preserveAspect = true;
        }

        private void OpenWindow()
        {
            Type targetWindowType = Type.GetType($"UGUIWindow.{targetClassName}", true);
            UGUIWindowManager.CreateWindow(targetWindowType);
        }
        #endregion

        #region Event Listener
        public void Focus()
        {
            imageBackground.enabled = true;

            var desktop = GetComponentInParent<UGUIDesktop>();
            desktop.OnIconClicked?.Invoke(this);
        }

        public void Divert()
        {
            imageBackground.enabled = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 포커스를 획득합니다.
            Focus();

            UGUIWindowLog.Log("Click!");
            float currentTime = Time.time;

            // 마지막 클릭 후 doubleClickThreshold 안에 다시 클릭했는지 확인
            if (currentTime - lastClickTime <= doubleClickThreshold)
            {
                UGUIWindowLog.Log("Double Click!");

                // 연결된 이벤트들을 모두 호출합니다.
                OnDoubleClickIcon?.Invoke();

                // 마지막 클릭 시간을 초기화하여 세 번째 클릭이 또 더블클릭으로 인식되는 것을 방지합니다.
                lastClickTime = -1f;

                // 포커스를 해제합니다.
                Divert();
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
