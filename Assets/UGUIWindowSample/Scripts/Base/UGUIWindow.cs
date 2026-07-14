using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UGUIWindow
{
    public enum UGUIWindowMode
    {
        Windowed,
        Maximized,
        Minimized
    }

    [RequireComponent(typeof(UGUIWindowView))]
    public class UGUIWindow : MonoBehaviour, IPointerDownHandler
    {
        private enum UGUIWindowLayoutMode
        {
            Windowed,
            Maximized
        }

        #region Inspector Fields
        [Header("Window Mode")]
        [SerializeField] private UGUIWindowMode _windowMode = UGUIWindowMode.Windowed;

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

        [Tooltip("윈도우를 움직일 수 있나요?")]
        public bool isMovable = false;

        [Tooltip("윈도우의 크기를 조절할 수 있나요?")]
        public bool isResizable = false;

        [Tooltip("윈도우가 가져야 할 최소 크기")]
        public Vector2 minimumWindowSize = new Vector2(100, 100);

        [Tooltip("작업 표시줄 등에 표시할 윈도우 아이콘")]
        [SerializeField] private Sprite windowIcon;

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

        #region Properties
        public UGUIWindowMode WindowMode
        {
            get
            {
                if (_isMinimized)
                {
                    return UGUIWindowMode.Minimized;
                }

                return _layoutMode == UGUIWindowLayoutMode.Maximized
                    ? UGUIWindowMode.Maximized
                    : UGUIWindowMode.Windowed;
            }
            set
            {
                if (WindowMode != value)
                {
                    ChangeWindowMode(value);
                }
            }
        }

        public bool HasHeader
        {
            get { return _hasHeader; }
            set
            {
                if (_hasHeader != value)
                {
                    _hasHeader = value;
                    view.SetHeaderActive(_hasHeader);
                }
            }
        }

        public bool HasBorder
        {
            get { return _hasBorder; }
            set
            {
                if (_hasBorder != value)
                {
                    _hasBorder = value;
                    view.SetBorderActive(_hasBorder);
                }
            }
        }

        public bool HasExitButton
        {
            get { return _hasExitButton; }
            set
            {
                if (_hasExitButton != value)
                {
                    _hasExitButton = value;
                    view.SetExitButtonActive(_hasExitButton);
                }
            }
        }

        public bool HasMaximizeButton
        {
            get { return _hasMaximizeButton; }
            set
            {
                if (_hasMaximizeButton != value)
                {
                    _hasMaximizeButton = value;
                    view.SetMaximizeButtonActive(_hasMaximizeButton);
                }
            }
        }
        
        public RectTransform RectTransform { get { return view.RectTransform; } }

        public string WindowTitle { get; private set; }

        public Sprite WindowIcon
        {
            get { return windowIcon; }
            set { windowIcon = value; }
        }
        #endregion

        #region Variables
        private UGUIWindowManager windowManager;
        private UGUIWindowView view;

#if UNITY_EDITOR
        private UGUIWindowMode _prevWindowMode;
        private bool _prevHasHeaderState;
        private bool _prevHasBorderState;
        private bool _prevHasExitButtonState;
        private bool _prevHasMaximizeButtonState;
#endif

        private UGUIWindowLayoutMode _layoutMode = UGUIWindowLayoutMode.Windowed;
        private bool _isMinimized;
        private UGUIWindowState _windowedRestoreState;
        #endregion

        #region Initialize
        protected virtual void Awake()
        {
            windowManager = UGUIWindowManager.Instance;
            view = GetComponent<UGUIWindowView>();
            InitializeRuntimeWindowMode();

            view.SetHeaderActive(_hasHeader);
            view.SetBorderActive(_hasBorder);
            view.SetExitButtonActive(_hasExitButton);
            view.SetMaximizeButtonActive(_hasMaximizeButton);

            _windowedRestoreState = new UGUIWindowState(this);
#if UNITY_EDITOR
            _prevWindowMode = _windowMode;
            _prevHasHeaderState = _hasHeader;
            _prevHasBorderState = _hasBorder;
            _prevHasExitButtonState = _hasExitButton;
            _prevHasMaximizeButtonState = _hasMaximizeButton;
#endif
        }

        protected virtual void OnEnable()
        {
            _ = view.Fade(0f, 1f, 0.9f, 1f);
        }
        #endregion

        #region Inspector
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (view == null)
            {
                view = GetComponent<UGUIWindowView>();
            }

            if (_hasHeader != _prevHasHeaderState)
            {
                view.SetHeaderActive(_hasHeader);
                _prevHasHeaderState = _hasHeader;
            }

            if (_hasBorder != _prevHasBorderState)
            {
                view.SetBorderActive(_hasBorder);
                _prevHasBorderState = _hasBorder;
            }

            if (_hasExitButton != _prevHasExitButtonState)
            {
                view.SetExitButtonActive(_hasExitButton);
                _prevHasExitButtonState = _hasExitButton;
            }

            if (_hasMaximizeButton != _prevHasMaximizeButtonState)
            {
                view.SetMaximizeButtonActive(_hasMaximizeButton);
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

        #region Window - Setter
        public void SetWindowTitle(string title)
        {
            WindowTitle = title;
            view.SetTitle(title);
        }
        #endregion

        #region Window - Control
        public void Open()
        {
            view.SetActive(true);
            OnOpenWindow?.Invoke(this);
        }

        public async void Close()
        {
            OnCloseWindow?.Invoke(this);

            await view.Fade(1f, 0f, 1f, 0.9f);
            view.SetActive(false);

            if (allowMultipleInstance || !useObjectPooling)
            {
                Destroy(gameObject);
            }
        }

        public void Focus()
        {
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

            _layoutMode = UGUIWindowLayoutMode.Maximized;
            _isMinimized = false;
            SynchronizeWindowMode();

            ApplyMaximizedLayout();

            HasBorder = false;
            isMovable = false;

            Focus();
        }

        public void RestoreWindow()
        {
            if (!isResizable)
            {
                UGUIWindowLog.LogError($"This Window {GetType()} cannot be resized!");
                return;
            }

            _layoutMode = UGUIWindowLayoutMode.Windowed;
            _isMinimized = false;
            SynchronizeWindowMode();

            view.ApplyRestoredState(_windowedRestoreState);

            HasBorder = _windowedRestoreState.hasBorder;
            isMovable = _windowedRestoreState.isMovable;
        }

        public void Minimize()
        {
            _isMinimized = true;
            SynchronizeWindowMode();
            OnMinimizeWindow?.Invoke(this);
        }

        public void RestoreFromMinimized()
        {
            if (!_isMinimized)
            {
                Focus();
                return;
            }

            _isMinimized = false;
            SynchronizeWindowMode();
            Open();

            if (_layoutMode == UGUIWindowLayoutMode.Maximized)
            {
                ApplyMaximizedLayout();
            }

            Focus();
        }

        public void RefreshMaximizedLayout()
        {
            if (_isMinimized || _layoutMode != UGUIWindowLayoutMode.Maximized)
            {
                return;
            }

            ApplyMaximizedLayout();
        }

        public void Move(int x, int y)
        {
            RectTransform.anchoredPosition = new Vector2(x, y);
            MemorizeLastWindowState();
        }

        public void Resize(int width, int height)
        {
            RectTransform.sizeDelta = new Vector2(width, height);
            MemorizeLastWindowState();
        }

        public void SetAnchor(Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform.anchorMin = anchorMin;
            RectTransform.anchorMax = anchorMax;
            MemorizeLastWindowState();
        }
        #endregion

        #region Window - Etc
        public void MemorizeLastWindowState()
        {
            _windowedRestoreState = new UGUIWindowState(this);
        }

        private void InitializeRuntimeWindowMode()
        {
            _layoutMode = _windowMode == UGUIWindowMode.Maximized
                ? UGUIWindowLayoutMode.Maximized
                : UGUIWindowLayoutMode.Windowed;
            _isMinimized = _windowMode == UGUIWindowMode.Minimized;
        }

        private void SynchronizeWindowMode()
        {
            _windowMode = WindowMode;
#if UNITY_EDITOR
            _prevWindowMode = _windowMode;
#endif
        }

        private void ApplyMaximizedLayout()
        {
            float headerHeight = 0f;
            if (view.windowHeader != null)
            {
                RectTransform headerTransform = view.windowHeader.transform as RectTransform;
                headerHeight = headerTransform.anchoredPosition.y;
            }

            Vector2 offsetMin = windowManager != null ? windowManager.MaximizedWindowOffsetMin : Vector2.zero;
            Vector2 offsetMax = windowManager != null ? windowManager.MaximizedWindowOffsetMax : Vector2.zero;

            view.ApplyMaximizedState(headerHeight, offsetMin, offsetMax);
        }
        #endregion

        #region Interface
        public void OnPointerDown(PointerEventData eventData)
        {
            OnFocusWindow?.Invoke(this);
        }
        #endregion
    }
}
