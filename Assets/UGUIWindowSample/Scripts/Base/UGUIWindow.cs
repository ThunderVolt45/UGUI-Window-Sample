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
        Minimized,

        // 기존 프리팹에 직렬화된 값이 밀리지 않도록 반드시 끝에 추가할 것.
        FullScreen
    }

    [RequireComponent(typeof(UGUIWindowView))]
    public class UGUIWindow : MonoBehaviour, IPointerDownHandler
    {
        private enum UGUIWindowLayoutMode
        {
            Windowed,
            Maximized,
            FullScreen
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

        [Tooltip("창 제목. 비워두면 클래스명을 사용합니다. windowIcon과 마찬가지로, 창이 열리기 전에도 " +
                 "(작업 표시줄에 핀 고정된 앱의 이름 등) 프리팹에서 읽어야 하므로 직렬화합니다.")]
        [SerializeField] private string defaultTitle;

        [Header("FullScreen Settings")]
        [Space(3f)]
        [Tooltip("전체화면일 때 화면 위쪽 이 범위 안으로 포인터가 들어오면 헤더가 내려옵니다. " +
                 "캔버스 단위가 아니라 실제 화면 픽셀이라, 화면 배율이나 창 크기가 바뀌어도 손에 잡히는 폭은 같습니다.")]
        [SerializeField] private float fullScreenHeaderRevealZonePixels = 32f;

        [Tooltip("전체화면일 때 헤더가 미끄러지는 속도. 클수록 빠릅니다.")]
        [SerializeField] private float fullScreenHeaderRevealSpeed = 14f;

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

                switch (_layoutMode)
                {
                    case UGUIWindowLayoutMode.Maximized:
                        return UGUIWindowMode.Maximized;
                    case UGUIWindowLayoutMode.FullScreen:
                        return UGUIWindowMode.FullScreen;
                    default:
                        return UGUIWindowMode.Windowed;
                }
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

        /// <summary>
        /// 프리팹에 저장된 창 제목(미지정 시 클래스명). 인스턴스가 없어도 읽을 수 있으므로
        /// 작업 표시줄이 아직 실행되지 않은 앱의 이름을 표시할 때 쓴다.
        /// </summary>
        public string DefaultTitle
        {
            get { return string.IsNullOrWhiteSpace(defaultTitle) ? GetType().Name : defaultTitle; }
        }

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

        // 전체화면을 빠져나올 때 돌아갈 레이아웃(확대였는지 창 모드였는지)
        private UGUIWindowLayoutMode _layoutModeBeforeFullScreen = UGUIWindowLayoutMode.Windowed;

        // 전체화면 진입 전 헤더의 세로 위치. 헤더를 화면 밖으로 밀어냈다가 되돌리는 데 쓴다.
        private float _headerAnchoredYBeforeFullScreen;

        // 0 = 헤더가 화면 위로 완전히 숨음, 1 = 헤더가 콘텐츠 위에 완전히 내려옴
        private float _headerRevealProgress;

        // 감지 범위를 실제 픽셀에서 캔버스 단위로 바꿀 때 필요. 매 프레임 탐색하지 않도록 캐싱한다.
        private Canvas _cachedCanvas;

        private float PixelsToCanvasUnits(float pixels)
        {
            if (_cachedCanvas == null)
            {
                _cachedCanvas = GetComponentInParent<Canvas>();
            }

            return UGUIWindowManager.PixelsToCanvasUnits(pixels, _cachedCanvas);
        }

        private RectTransform HeaderRectTransform
        {
            get
            {
                return view != null && view.windowHeader != null
                    ? view.windowHeader.transform as RectTransform
                    : null;
            }
        }
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

            // 프리팹이 전체화면 상태로 저장된 경우: 매니저 점유와 레이아웃을 여기서 맞춰준다.
            // (_windowedRestoreState를 잡은 뒤여야 복원할 창 크기가 남는다.)
            if (_layoutMode == UGUIWindowLayoutMode.FullScreen)
            {
                _headerAnchoredYBeforeFullScreen = HeaderRectTransform != null
                    ? HeaderRectTransform.anchoredPosition.y
                    : 0f;
                _headerRevealProgress = 0f;
                ApplyHeaderReveal();
                ApplyFullScreenLayout();

                if (windowManager != null)
                {
                    windowManager.SetFullScreenWindow(this);
                }
            }
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

        protected virtual void Update()
        {
            if (_isMinimized || _layoutMode != UGUIWindowLayoutMode.FullScreen)
            {
                return;
            }

            UpdateHeaderReveal();
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

            // 전체화면 점유를 반납하지 않고 닫으면 도크가 숨은 채로 남고,
            // 풀에서 다시 꺼냈을 때도 전체화면 상태가 따라온다.
            // 페이드가 끝난 뒤에 해제해야 창이 작게 줄었다가 사라지는 것처럼 보이지 않는다.
            ExitFullScreen();

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
                case UGUIWindowMode.FullScreen:
                    EnterFullScreen();
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

        /// <summary>
        /// macOS의 '확대(zoom)'에 해당한다. 도크 등이 예약한 여백을 뺀 사용 가능 영역만 채우고
        /// 헤더는 그대로 남는다.
        /// </summary>
        public void Maximize()
        {
            if (!isResizable)
            {
                UGUIWindowLog.LogError($"This Window {GetType()} cannot be resized!");
                return;
            }

            LeaveFullScreenChrome();

            _layoutMode = UGUIWindowLayoutMode.Maximized;
            _isMinimized = false;
            SynchronizeWindowMode();

            ApplyMaximizedLayout();

            HasBorder = false;
            isMovable = false;

            Focus();
        }

        /// <summary>
        /// macOS의 '전체화면'에 해당한다. 예약된 여백을 무시하고 화면 전체를 덮으며,
        /// 헤더는 화면 위로 밀려나 포인터를 위쪽 가장자리에 대면 다시 내려온다.
        /// </summary>
        public void EnterFullScreen()
        {
            if (!isResizable)
            {
                UGUIWindowLog.LogError($"This Window {GetType()} cannot be resized!");
                return;
            }

            if (_layoutMode == UGUIWindowLayoutMode.FullScreen)
            {
                return;
            }

            _layoutModeBeforeFullScreen = _layoutMode;
            _layoutMode = UGUIWindowLayoutMode.FullScreen;
            _isMinimized = false;
            SynchronizeWindowMode();

            _headerAnchoredYBeforeFullScreen = HeaderRectTransform != null
                ? HeaderRectTransform.anchoredPosition.y
                : 0f;
            _headerRevealProgress = 0f;
            ApplyHeaderReveal();

            ApplyFullScreenLayout();

            HasBorder = false;
            isMovable = false;

            if (windowManager != null)
            {
                windowManager.SetFullScreenWindow(this);
            }

            Focus();
        }

        /// <summary>
        /// 전체화면을 해제하고 진입 직전의 레이아웃(확대 또는 창 모드)으로 되돌린다.
        /// 전체화면이 아니면 아무것도 하지 않는다.
        /// </summary>
        public void ExitFullScreen()
        {
            if (_layoutMode != UGUIWindowLayoutMode.FullScreen)
            {
                return;
            }

            if (_layoutModeBeforeFullScreen == UGUIWindowLayoutMode.Maximized)
            {
                Maximize();
            }
            else
            {
                RestoreWindow();
            }
        }

        public void RestoreWindow()
        {
            if (!isResizable)
            {
                UGUIWindowLog.LogError($"This Window {GetType()} cannot be resized!");
                return;
            }

            LeaveFullScreenChrome();

            _layoutMode = UGUIWindowLayoutMode.Windowed;
            _isMinimized = false;
            SynchronizeWindowMode();

            view.ApplyRestoredState(_windowedRestoreState);

            HasBorder = _windowedRestoreState.hasBorder;
            isMovable = _windowedRestoreState.isMovable;
        }

        public void Minimize()
        {
            // 전체화면인 채로 최소화하면 도크가 숨은 채 되살릴 수단이 사라지므로,
            // 이전 크기로 되돌린 뒤 최소화한다.
            ExitFullScreen();

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
            switch (_windowMode)
            {
                case UGUIWindowMode.Maximized:
                    _layoutMode = UGUIWindowLayoutMode.Maximized;
                    break;
                case UGUIWindowMode.FullScreen:
                    _layoutMode = UGUIWindowLayoutMode.FullScreen;
                    break;
                default:
                    _layoutMode = UGUIWindowLayoutMode.Windowed;
                    break;
            }

            _isMinimized = _windowMode == UGUIWindowMode.Minimized;
            _layoutModeBeforeFullScreen = UGUIWindowLayoutMode.Windowed;
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

        private void ApplyFullScreenLayout()
        {
            // 확대와 달리 도크가 예약한 여백도, 헤더 자리도 비워두지 않는다.
            view.ApplyFullScreenState();
        }

        /// <summary>
        /// 전체화면에서 벗어날 때 헤더 위치와 매니저 점유를 되돌린다.
        /// 전체화면이 아니면 아무것도 하지 않는다.
        /// </summary>
        private void LeaveFullScreenChrome()
        {
            if (_layoutMode != UGUIWindowLayoutMode.FullScreen)
            {
                return;
            }

            RectTransform header = HeaderRectTransform;
            if (header != null)
            {
                header.anchoredPosition = new Vector2(
                    header.anchoredPosition.x,
                    _headerAnchoredYBeforeFullScreen);
            }

            _headerRevealProgress = 0f;

            if (windowManager != null)
            {
                windowManager.ClearFullScreenWindow(this);
            }
        }

        /// <summary>
        /// _headerRevealProgress를 헤더의 세로 위치로 옮긴다.
        /// 헤더 pivot이 위쪽이므로 y = 헤더 높이면 화면 위로 완전히 벗어나고, y = 0이면 화면 위 가장자리에 붙는다.
        /// </summary>
        private void ApplyHeaderReveal()
        {
            RectTransform header = HeaderRectTransform;
            if (header == null)
            {
                return;
            }

            float hiddenY = header.rect.height;
            float revealedY = 0f;

            header.anchoredPosition = new Vector2(
                header.anchoredPosition.x,
                Mathf.Lerp(hiddenY, revealedY, _headerRevealProgress));
        }

        private void UpdateHeaderReveal()
        {
            RectTransform header = HeaderRectTransform;
            if (header == null || !_hasHeader)
            {
                return;
            }

            float target = IsPointerInHeaderRevealZone(header) ? 1f : 0f;
            if (Mathf.Approximately(_headerRevealProgress, target))
            {
                return;
            }

            float t = 1f - Mathf.Exp(-fullScreenHeaderRevealSpeed * Time.unscaledDeltaTime);
            _headerRevealProgress = Mathf.Lerp(_headerRevealProgress, target, t);

            if (Mathf.Abs(_headerRevealProgress - target) < 0.001f)
            {
                _headerRevealProgress = target;
            }

            ApplyHeaderReveal();
        }

        private bool IsPointerInHeaderRevealZone(RectTransform header)
        {
            if (!UGUIWindowManager.TryGetPointerScreenPosition(out Vector2 screenPosition))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    RectTransform,
                    screenPosition,
                    null,
                    out Vector2 localPointer))
            {
                return false;
            }

            // 전체화면이므로 창 rect의 위쪽 가장자리가 곧 화면의 위쪽 가장자리다.
            // 헤더가 내려와 있는 동안에는 감지 범위를 헤더 높이까지 넓혀,
            // 포인터를 헤더 위에 올려둔 채로 버튼을 누를 수 있게 한다.
            Rect windowRect = RectTransform.rect;
            float zoneHeight = Mathf.Max(
                PixelsToCanvasUnits(fullScreenHeaderRevealZonePixels),
                _headerRevealProgress * header.rect.height);

            return localPointer.y >= windowRect.yMax - zoneHeight
                && localPointer.x >= windowRect.xMin
                && localPointer.x <= windowRect.xMax;
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
