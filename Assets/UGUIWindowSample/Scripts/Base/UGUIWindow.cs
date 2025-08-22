using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    public enum UGUIWindowMode
    {
        Windowed,
        Maximized,
        Minimized
    }

    [RequireComponent(typeof(Image))]
    public class UGUIWindow : MonoBehaviour, IPointerDownHandler
    {
        #region Inspector Fields
        [Header("Window Mode")]
        [SerializeField] private UGUIWindowMode _windowMode = UGUIWindowMode.Windowed;

        [Header("Base Components")]
        public UGUIWindowHeader windowHeader;
        public List<UGUIWindowBorder> windowBorders;
        public List<UGUIWindowEdge> windowEdges;

        [Header("Base Settings")]
        [Space(3f)]
        [Tooltip("윈도우가 중복 생성될 수 있나요? (중복 생성을 허용할 경우 오브젝트 풀링 기능은 비활성화됩니다.)")]
        public bool allowMultipleInstance = false;

        [Tooltip("오브젝트 풀링 기능을 사용할까요? (윈도우의 중복 생성을 허용했을 경우 이 설정은 무시됩니다.)")]
        public bool useObjectPooling = true;

        [Tooltip("윈도우가 헤더를 갖나요?")]
        [SerializeField] private bool _hasHeader = false;

        [Tooltip("윈도우가 경계를 갖나요?")]
        [SerializeField] private bool _hasBorder = false;

        [Tooltip("윈도우가 나가기 버튼을 갖나요? 헤더가 존재하는 경우에만 활성화됩니다.")]
        [SerializeField] private bool _hasExitButton = false;

        [Tooltip("윈도우가 최대화/복원 버튼을 갖나요? 헤더가 존재하고 크기를 조절할 수 있는 경우에만 활성화됩니다.")]
        [SerializeField] private bool _hasMaximizeButton = false;

        // [Tooltip("윈도우가 최소화 버튼을 갖나요? 헤더가 존재하는 경우에만 활성화됩니다.")]
        // public bool hasMinimizeButton = false;

        [Tooltip("윈도우를 움직일 수 있나요?")]
        public bool isMovable = false;

        [Tooltip("윈도우의 크기를 조절할 수 있나요?")]
        public bool isResizable = false;

        // [Tooltip("윈도우의 경계 크기")]
        // public float borderSize = 4f;

        [Tooltip("윈도우가 가져야 할 최소 크기")]
        public Vector2 minimumWindowSize = new Vector2(100, 100);

        [Header("Window Events")]
        [Space(5f)]
        [Tooltip("윈도우가 열릴 때 호출할 이벤트")]
        public UnityEvent<UGUIWindow> OnOpenWindow;

        [Tooltip("윈도우가 닫힐 때 호출할 이벤트")]
        public UnityEvent<UGUIWindow> OnCloseWindow;

        [Tooltip("윈도우가 포커스를 받았을 때 호출할 이벤트")]
        public UnityEvent<UGUIWindow> OnFocusWindow;

        [Tooltip("윈도우가 최소화될 때 호출할 이벤트")]
        public UnityEvent<UGUIWindow> OnMinimizeWindow;
        #endregion

        #region properties
        public UGUIWindowMode WindowMode
        {
            get { return _windowMode; }
            set
            {
                if (_windowMode != value) // 값이 진짜로 바뀌었는 지 확인
                {
                    // 상태 변경, 값은 반드시 ChangeWindowMode()를 통해서만 변경되어야 한다.
                    ChangeWindowMode(value);
                }
            }
        }

        public bool HasHeader
        {
            get { return _hasHeader; }
            set
            {
                if (_hasHeader != value) // 값이 진짜로 바뀌었는 지 확인
                {
                    // 헤더 상태 변경
                    _hasHeader = value;
                    SetWindowHeader(_hasHeader);
                }
            }
        }

        public bool HasBorder
        {
            get { return _hasBorder; }
            set
            {
                if (_hasBorder != value) // 값이 진짜로 바뀌었는 지 확인
                {
                    // 경계 상태 변경
                    _hasBorder = value;
                    SetWindowBorder(_hasBorder);
                }
            }
        }

        public bool HasExitButton
        {
            get { return _hasExitButton; }
            set
            {
                if (_hasExitButton != value) // 값이 진짜로 바뀌었는 지 확인
                {
                    // 버튼 상태 변경
                    _hasExitButton = value;
                    SetWindowExitButton(_hasExitButton);
                }
            }
        }

        public bool HasMaximizeButton
        {
            get { return _hasMaximizeButton; }
            set
            {
                if (_hasMaximizeButton != value) // 값이 진짜로 바뀌었는 지 확인
                {
                    // 버튼 상태 변경
                    _hasMaximizeButton = value;
                    SetWindowMaximizeButton(_hasMaximizeButton);
                }
            }
        }
        #endregion

        #region Variables
        // UGUIWindowManager의 인스턴스
        private UGUIWindowManager windowManager;

#if UNITY_EDITOR
        private UGUIWindowMode _prevWindowMode;
        private bool _prevHasHeaderState;
        private bool _prevHasBorderState;
        private bool _prevHasExitButtonState;
        private bool _prevHasMaximizeButtonState;
#endif

        // 윈도우의 이전 상태를 기록하는 클래스
        [SerializeField] private UGUIWindowState _lastWindowState;
        #endregion

        #region Initialize
        protected virtual void Awake()
        {
            // 윈도우 매니저의 인스턴스를 가져옴
            windowManager = UGUIWindowManager.Instance;

            // 윈도우의 각 컴포넌트를 초기화
            SetWindowHeader(_hasHeader);
            SetWindowBorder(_hasBorder);
            SetWindowExitButton(_hasExitButton);

            // 현재 윈도우의 상태를 기록함
            _lastWindowState = new UGUIWindowState(this);
#if UNITY_EDITOR
            _prevWindowMode = _windowMode;
            _prevHasHeaderState = _hasHeader;
            _prevHasBorderState = _hasBorder;
            _prevHasExitButtonState = _hasExitButton;
            _prevHasMaximizeButtonState = _hasMaximizeButton;
#endif
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected virtual void Start()
        {

        }
        #endregion

        #region Inspector
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_hasHeader != _prevHasHeaderState)
            {
                SetWindowHeader(_hasHeader);
                _prevHasHeaderState = _hasHeader;
            }

            if (_hasBorder != _prevHasBorderState)
            {
                SetWindowBorder(_hasBorder);
                _prevHasBorderState = _hasBorder;
            }

            if (_hasExitButton != _prevHasExitButtonState)
            {
                SetWindowExitButton(_hasExitButton);
                _prevHasExitButtonState = _hasExitButton;
            }

            if (_hasMaximizeButton != _prevHasMaximizeButtonState)
            {
                SetWindowMaximizeButton(_hasMaximizeButton);
                _prevHasMaximizeButtonState = _hasMaximizeButton;
            }

            if (_windowMode != _prevWindowMode)
            {
                ChangeWindowMode(_windowMode);
                _prevWindowMode = _windowMode;
            }
        }
#endif
        #endregion

        #region Window - Setting
        public void SetWindowTitle(string title)
        {
            if (windowHeader != null)
            {
                windowHeader.SetTitle(title);
            }
        }

        private void SetWindowExitButton(bool hasExitButton)
        {
            if (windowHeader != null)
            {
                windowHeader.SetExitButtonActive(hasExitButton);
            }
        }

        private void SetWindowMaximizeButton(bool hasMaximizeButton)
        {
            if (windowHeader != null)
            {
                windowHeader.SetMaximizeButtonActive(hasMaximizeButton);
            }
        }

        private void SetWindowHeader(bool hasHeader)
        {
            if (windowHeader != null)
            {
                windowHeader.gameObject.SetActive(hasHeader);
            }
        }

        private void SetWindowBorder(bool hasBorder)
        {
            foreach (var border in windowBorders)
            {
                border.gameObject.SetActive(hasBorder);
            }

            foreach (var edge in windowEdges)
            {
                edge.gameObject.SetActive(hasBorder);
            }
        }
        #endregion

        #region Window - Control
        public void Open()
        {
            gameObject.SetActive(true);

            // 윈도우가 열렸음을 윈도우 매니저에 알림
            OnOpenWindow?.Invoke(this);
        }

        public void Close()
        {
            gameObject.SetActive(false);

            // 윈도우가 닫혔음을 윈도우 매니저에 알림
            OnCloseWindow?.Invoke(this);

            // 오브젝트 풀링 기능을 사용하지 않는다면 자폭
            if (allowMultipleInstance || !useObjectPooling)
            {
                Destroy(gameObject);
            }
        }

        public void Focus()
        {
            // 윈도우가 포커스를 얻었음을 윈도우 매니저에 알림
            OnFocusWindow?.Invoke(this);
        }

        public void ChangeWindowMode(UGUIWindowMode windowMode)
        {
            switch (windowMode)
            {
                case UGUIWindowMode.Windowed:
                    RestoreWindow();
                    Open();
                    break;
                case UGUIWindowMode.Maximized:
                    Maximize();
                    Open();
                    break;
                case UGUIWindowMode.Minimized:
                    Minimize();
                    break;
                default:
                    UGUIWindowLog.LogError($"Change Window Mode to {windowMode} is undefined!");
                    break;
            }
        }

        public void Maximize()
        {
            if (!isResizable)
            {
                UGUIWindowLog.LogError($"This Window {GetType()} cannot be resized!");
                return;
            }

            // 창 모드 변경
            _windowMode = UGUIWindowMode.Maximized;
#if UNITY_EDITOR
            _prevWindowMode = _windowMode;
#endif

            // 계산 준비
            RectTransform windowTransform = transform as RectTransform;
            float headerHeight = 0f;

            if (windowHeader != null)
            {
                RectTransform headerTransform = windowHeader.transform as RectTransform;
                headerHeight = headerTransform.anchoredPosition.y;
            }

            // Window의 크기를 최대화한다.
            windowTransform.anchorMin = Vector2.zero;
            windowTransform.anchorMax = Vector2.one;
            windowTransform.anchoredPosition = new Vector2(0, -headerHeight / 2);
            windowTransform.sizeDelta = new Vector2(0, -headerHeight);

            // 경계를 없애고 움직일 수 없게 고정한다.
            HasBorder = false;
            isMovable = false;

            // 포커스 획득
            Focus();
        }

        public void RestoreWindow()
        {
            if (!isResizable)
            {
                UGUIWindowLog.LogError($"This Window {GetType()} cannot be resized!");
                return;
            }

            // 창 모드 변경
            _windowMode = UGUIWindowMode.Windowed;
#if UNITY_EDITOR
            _prevWindowMode = _windowMode;
#endif

            // 윈도우를 이전 상태로 되돌린다.
            _lastWindowState.RestoreWindowFromState(this);
        }

        public void Minimize()
        {
            // 윈도우 최소화
            _windowMode = UGUIWindowMode.Minimized;
#if UNITY_EDITOR
            _prevWindowMode = _windowMode;
#endif

            // 윈도우가 최소화되었음을 윈도우 매니저에 알림
            OnMinimizeWindow?.Invoke(this);
        }
        #endregion

        #region Window - Etc
        public void MemorizeLastWindowState()
        {
            _lastWindowState = new UGUIWindowState(this);
        }
        #endregion

        #region Interface
        public void OnPointerDown(PointerEventData eventData)
        {
            // 윈도우가 포커스를 얻었음을 윈도우 매니저에 알림
            OnFocusWindow?.Invoke(this);
        }
        #endregion
    }
}
