using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UGUIWindow
{
    public class UGUIWindowManager : MonoBehaviour
    {
        private static UGUIWindowManager _instance;
        private static object locker = new object(); // Thread-Safe한 싱글톤 패턴을 구현하기 위한 lock
        private static bool isShuttingDown;

        public static UGUIWindowManager Instance
        {
            get
            {
                // 인스턴스가 존재하지 않을 경우
                if (_instance == null)
                {
                    if (isShuttingDown)
                    {
                        return null;
                    }

                    // 일단 인스턴스가 존재하는 지 확인
                    _instance = FindAnyObjectByType<UGUIWindowManager>(FindObjectsInactive.Include);

                    // 인스턴스가 존재한다면 활성화
                    if (_instance != null)
                    {
                        _instance.gameObject.SetActive(true);
                        _instance.enabled = true;
                    }
                    // 없다면 프리팹을 불러와 새로 생성한다
                    else
                    {
                        // lock은 비싼 연산에 속하므로 Double-checked locking을 사용한다
                        // https://stackoverflow.com/questions/12316406/thread-safe-c-sharp-singleton-pattern
                        lock (locker)
                        {
                            if (_instance == null)
                            {
                                if (isShuttingDown)
                                {
                                    return null;
                                }

                                var managerPrefab = Resources.Load<GameObject>("UGUIWindowManager");
                                var managerGameObject = Instantiate(managerPrefab);
                                _instance = managerGameObject.GetComponent<UGUIWindowManager>();
                            }
                        }
                    }
                }

                _instance?.EnsureRuntimeState();

                // 인스턴스 반환
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            isShuttingDown = false;
        }

        [Header("Settings")]
        [Tooltip("Window prefab을 가져올 기본 경로")]
        [SerializeField] private string defaultWindowPath = "Windows/";

        [Header("Canvas")]
        [Tooltip("Window가 표시될 Canvas")]
        [SerializeField] private CanvasScaler mainCanvasScaler;

        [Header("Default Window")]
        [Tooltip("활성화된 Window가 없는 상태에서 Escape를 받았을 때 생성할 Window")]
        [SerializeField] private UGUIWindow defaultWindowOnEscape;

        [Header("Window Switcher")]
        [Tooltip("Window 전환 오버레이를 생성할지 여부")]
        [SerializeField] private bool enableWindowSwitcher = true;

        [Header("Maximized Window Area")]
        [Tooltip("최대화된 Window가 부모 Rect의 왼쪽/아래쪽에서 띄울 여백")]
        [SerializeField] private Vector2 maximizedWindowOffsetMin = Vector2.zero;

        [Tooltip("최대화된 Window가 부모 Rect의 오른쪽/위쪽에서 띄울 여백")]
        [SerializeField] private Vector2 maximizedWindowOffsetMax = Vector2.zero;

        [Header("Object Pool")]
        [Tooltip("Window가 최소화되었을 때 이동할 오브젝트의 CanvasScaler")]
        [SerializeField] private CanvasScaler minimizedObjectPool;

        [Tooltip("Window가 비활성화 되었을 때 이동할 오브젝트의 CanvasScaler")]
        [SerializeField] private CanvasScaler disabledObjectPool;

        [Header("Event Listener")]
        [Space(5f)]
        [Tooltip("DPI 설정이 변경되었을 때 실행할 Event")]
        public UnityEvent<int, int, float> OnDPIChanged;

        [Tooltip("관리 중인 Window가 Open 상태가 되었을 때 실행할 Event")]
        public UnityEvent<UGUIWindow> OnManagedWindowOpened;

        [Tooltip("관리 중인 Window가 Close 되었을 때 실행할 Event")]
        public UnityEvent<UGUIWindow> OnManagedWindowClosed;

        [Tooltip("관리 중인 Window가 Focus 되었을 때 실행할 Event")]
        public UnityEvent<UGUIWindow> OnManagedWindowFocused;

        [Tooltip("관리 중인 Window가 Minimize 되었을 때 실행할 Event")]
        public UnityEvent<UGUIWindow> OnManagedWindowMinimized;

        // 현재 열려있는 윈도우의 순서를 저장하는 이중 연결 리스트
        private DoublyLinkedList<UGUIWindow> currentlyOpenedWindows;

        // 작업 표시줄 등 외부 UI에 표시될 수 있는 Open/Minimize 상태의 윈도우 목록
        private HashSet<UGUIWindow> managedVisibleWindows;

        // MainCanvas와 MinimizedWindow에 걸친 최근 포커스 순서
        private LinkedList<UGUIWindow> recentlyFocusedWindows;

        // 생성된 윈도우가 저장된 오브젝트 풀
        private Dictionary<string, UGUIWindow> windowPool;

        private UGUIWindowSwitcher windowSwitcher;

        // 전체화면은 한 번에 하나의 Window만 점유한다.
        private UGUIWindow fullScreenWindow;

        public static float CurrentDPI
        {
            get
            {
                var manager = Instance;
                return manager != null ? manager._currentDPI : 2f;
            }
        }

        public float ScreenMultiplierWidth
        {
            get { return _screenMultiplierWidth; }
        }

        public float ScreenMultiplierHeight
        {
            get { return _screenMultiplierHeight; }
        }

        public Vector2 GetPointerDeltaInRect(PointerEventData eventData, RectTransform relativeTo)
        {
            if (eventData == null || relativeTo == null)
            {
                return Vector2.zero;
            }

            Vector2 currentPosition;
            Vector2 previousPosition;
            Camera eventCamera = eventData.pressEventCamera != null
                ? eventData.pressEventCamera
                : eventData.enterEventCamera;
            Vector2 previousScreenPosition = eventData.position - eventData.delta;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    relativeTo,
                    eventData.position,
                    eventCamera,
                    out currentPosition)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    relativeTo,
                    previousScreenPosition,
                    eventCamera,
                    out previousPosition))
            {
                return currentPosition - previousPosition;
            }

            return new Vector2(
                eventData.delta.x * ScreenMultiplierWidth,
                eventData.delta.y * ScreenMultiplierHeight
            );
        }

        /// <summary>
        /// 현재 전체화면을 점유 중인 Window. 없으면 null.
        /// </summary>
        public UGUIWindow FullScreenWindow
        {
            get { return fullScreenWindow; }
        }

        public bool HasFullScreenWindow
        {
            get { return fullScreenWindow != null; }
        }

        /// <summary>
        /// 실제 화면 픽셀을 해당 Canvas의 좌표 단위로 바꾼다.
        /// 가장자리 감지 범위처럼 '손의 정확도'에 맞춰야 하는 값은 화면 배율이나 창 크기를 따라
        /// 늘었다 줄었다 하면 안 되므로, 캔버스 단위로 직접 쓰지 말고 이 변환을 거칠 것.
        /// </summary>
        public static float PixelsToCanvasUnits(float pixels, Canvas canvas)
        {
            if (canvas == null)
            {
                return pixels;
            }

            float scaleFactor = canvas.rootCanvas.scaleFactor;
            return scaleFactor > 0f ? pixels / scaleFactor : pixels;
        }

        /// <summary>
        /// 포인터의 화면 좌표를 얻는다. 마우스가 없는 환경이면 false.
        /// </summary>
        public static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            return true;
        }

        public Vector2 MaximizedWindowOffsetMin
        {
            get { return maximizedWindowOffsetMin; }
        }

        public Vector2 MaximizedWindowOffsetMax
        {
            get { return maximizedWindowOffsetMax; }
        }

        public IEnumerable<UGUIWindow> ManagedVisibleWindows
        {
            get
            {
                return GetVisibleWindows();
            }
        }

        private float _currentDPI = 2f;
        private float _screenMultiplierWidth = 1f;
        private float _screenMultiplierHeight = 1f;

        #region Initialize
        private void Awake()
        {
            // 인스턴스 중복이 감지된다면 자폭한다
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            isShuttingDown = false;
            
            EnsureRuntimeState();

            // 애플리케이션이 더 이상 메모리를 확보할 수 없을 때 호출할 함수 등록
            Application.lowMemory += OnLowMemory;

            // 씬 이동으로 인해 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;

            if (_instance == this)
            {
                isShuttingDown = true;
                _instance = null;
            }
        }

        private void EnsureRuntimeState()
        {
            currentlyOpenedWindows ??= new DoublyLinkedList<UGUIWindow>();
            managedVisibleWindows ??= new HashSet<UGUIWindow>();
            recentlyFocusedWindows ??= new LinkedList<UGUIWindow>();
            windowPool ??= new Dictionary<string, UGUIWindow>();

            OnManagedWindowOpened ??= new UnityEvent<UGUIWindow>();
            OnManagedWindowClosed ??= new UnityEvent<UGUIWindow>();
            OnManagedWindowFocused ??= new UnityEvent<UGUIWindow>();
            OnManagedWindowMinimized ??= new UnityEvent<UGUIWindow>();
        }

        private void Start()
        {
            // 캔버스 초기화
            InitializeCanvas();
            InitializeWindowSwitcher();
        }

        private void InitializeCanvas()
        {
            var currentResolution = Screen.currentResolution;
            var dpi = PlayerPrefs.GetFloat("DPI Settings", 2f);

            SetDPI(currentResolution.width, currentResolution.height, dpi);
        }

        private void InitializeWindowSwitcher()
        {
            if (!enableWindowSwitcher || mainCanvasScaler == null)
            {
                return;
            }

            windowSwitcher = UGUIWindowSwitcher.Instance;
            if (windowSwitcher == null)
            {
                return;
            }

            windowSwitcher.AttachTo(mainCanvasScaler.transform);
        }
        #endregion

        #region Window - Query
        public IReadOnlyList<UGUIWindow> GetOpenWindows()
        {
            EnsureRuntimeState();

            var windows = new List<UGUIWindow>();
            CollectActiveChildWindows(mainCanvasScaler != null ? mainCanvasScaler.transform : null, windows);
            return windows;
        }

        public IReadOnlyList<UGUIWindow> GetVisibleWindows()
        {
            EnsureRuntimeState();

            var windows = new List<UGUIWindow>();
            CollectActiveChildWindows(mainCanvasScaler != null ? mainCanvasScaler.transform : null, windows);
            CollectActiveChildWindows(minimizedObjectPool != null ? minimizedObjectPool.transform : null, windows);
            return windows;
        }

        public IReadOnlyList<UGUIWindow> GetSwitchableWindows()
        {
            EnsureRuntimeState();

            var switchableWindows = GetVisibleWindows();
            var remainingWindows = new HashSet<UGUIWindow>(switchableWindows);
            var orderedWindows = new List<UGUIWindow>();

            var node = recentlyFocusedWindows.First;
            while (node != null)
            {
                var next = node.Next;
                var window = node.Value;

                if (window == null || !remainingWindows.Contains(window))
                {
                    recentlyFocusedWindows.Remove(node);
                }
                else
                {
                    orderedWindows.Add(window);
                    remainingWindows.Remove(window);
                }

                node = next;
            }

            foreach (var window in switchableWindows)
            {
                if (!remainingWindows.Remove(window))
                {
                    continue;
                }

                orderedWindows.Add(window);
                recentlyFocusedWindows.AddLast(window);
            }

            return orderedWindows;
        }

        public UGUIWindow GetFocusedWindow()
        {
            EnsureRuntimeState();

            if (mainCanvasScaler == null)
            {
                return null;
            }

            var mainCanvasTransform = mainCanvasScaler.transform;
            for (int i = mainCanvasTransform.childCount - 1; i >= 0; i--)
            {
                var child = mainCanvasTransform.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child.TryGetComponent(out UGUIWindow window) && window.isActiveAndEnabled)
                {
                    return window;
                }
            }

            return null;
        }

        public void FocusWindow(UGUIWindow window)
        {
            EnsureRuntimeState();

            if (!IsSwitchableWindow(window))
            {
                return;
            }

            if (window.WindowMode == UGUIWindowMode.Minimized)
            {
                window.RestoreFromMinimized();
                return;
            }

            window.Focus();
        }

        private void CollectActiveChildWindows(Transform parent, List<UGUIWindow> results)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child.TryGetComponent(out UGUIWindow window) && window.isActiveAndEnabled)
                {
                    results.Add(window);
                }
            }
        }

        private bool IsSwitchableWindow(UGUIWindow window)
        {
            if (window == null || !window.isActiveAndEnabled)
            {
                return false;
            }

            var parent = window.transform.parent;
            return (mainCanvasScaler != null && parent == mainCanvasScaler.transform)
                || (minimizedObjectPool != null && parent == minimizedObjectPool.transform);
        }

        private void PromoteRecentlyFocusedWindow(UGUIWindow window)
        {
            if (window == null)
            {
                return;
            }

            recentlyFocusedWindows.Remove(window);
            recentlyFocusedWindows.AddFirst(window);
        }

        private void EnsureRecentlyFocusedWindow(UGUIWindow window)
        {
            if (window == null || recentlyFocusedWindows.Contains(window))
            {
                return;
            }

            recentlyFocusedWindows.AddLast(window);
        }
        #endregion

        #region Canvas
        /// <summary>
        /// Canvas의 Reference Resolution 값을 DPI 설정에 맞춰 바꾸는 메소드
        /// </summary>
        /// <param name="screenWidth">현재 화면의 너비</param>
        /// <param name="screenHeight">현재 화면의 높이</param>
        /// <param name="dpi">목표 DPI % (1f = 100%)</param>
        public static void SetDPI(int screenWidth, int screenHeight, float dpi)
        {
            var manager = Instance;
            if (manager == null)
            {
                return;
            }

            // 화면 해상도와 DPI 값에 맞춰 캔버스의 설정을 변경한다.
            manager.ChangeCanvasResolution(screenWidth, screenHeight, dpi);

            // PlayerPrefs에 DPI 값을 기록한다.
            PlayerPrefs.SetFloat("DPI Settings", dpi);
        }

        private void ChangeCanvasResolution(int screenWidth, int screenHeight, float dpi)
        {
            _currentDPI = dpi;

            float referecnceWidth = screenWidth / dpi;
            float referecnceHeight = screenHeight / dpi;

            // 각 Canvas Scaler의 해상도 변경
            mainCanvasScaler.referenceResolution =
                new Vector2(referecnceWidth, referecnceHeight);
            disabledObjectPool.referenceResolution =
                new Vector2(referecnceWidth, referecnceHeight);
            minimizedObjectPool.referenceResolution =
                new Vector2(referecnceWidth, referecnceHeight);

            _screenMultiplierWidth = referecnceWidth / screenWidth;
            _screenMultiplierHeight = referecnceHeight / screenHeight;

            // DPI 설정이 바뀌었음을 알림
            OnDPIChanged?.Invoke(screenWidth, screenHeight, dpi);
        }

        public void SetMaximizedWindowOffsets(Vector2 offsetMin, Vector2 offsetMax)
        {
            maximizedWindowOffsetMin = offsetMin;
            maximizedWindowOffsetMax = offsetMax;

            foreach (var window in ManagedVisibleWindows)
            {
                window.RefreshMaximizedLayout();
            }
        }

        public void ClearMaximizedWindowOffsets()
        {
            SetMaximizedWindowOffsets(Vector2.zero, Vector2.zero);
        }
        #endregion

        #region Window - FullScreen
        /// <summary>
        /// Window가 전체화면에 진입했음을 등록한다. 이미 다른 Window가 전체화면이면 그 Window를 먼저 내보낸다.
        /// UGUIWindow.EnterFullScreen이 호출하므로 직접 부를 일은 없다.
        /// </summary>
        public void SetFullScreenWindow(UGUIWindow window)
        {
            if (window == null || fullScreenWindow == window)
            {
                return;
            }

            var previousWindow = fullScreenWindow;

            // previousWindow를 내보내기 전에 먼저 갱신해야, 그쪽의 ClearFullScreenWindow가
            // 소유자 검사에 걸려 방금 등록한 window를 지우지 않는다.
            fullScreenWindow = window;

            if (previousWindow != null)
            {
                previousWindow.ExitFullScreen();
            }
        }

        /// <summary>
        /// Window가 전체화면에서 벗어났음을 등록 해제한다. 현재 점유자가 아니면 무시한다.
        /// </summary>
        public void ClearFullScreenWindow(UGUIWindow window)
        {
            if (fullScreenWindow != window)
            {
                return;
            }

            fullScreenWindow = null;
        }
        #endregion

        #region Window - Create
        /// <summary>
        /// 윈도우을 생성하는 private 메소드
        /// </summary>
        /// <param name="windowName">윈도우의 이름</param>
        /// <param name="postInstantiationAction">윈도우 GameObject가 생성된 후 실행할 추가 작업</param>
        private UGUIWindow GetOrCreateWindow(Type windowType, string windowName, Action<UGUIWindow> postInstantiationAction = null)
        {
            EnsureRuntimeState();

            // 타입 검사
            // if (!typeof(UGUIWindow).IsAssignableFrom(windowType))
            // {
            //     throw new ArgumentException($"Passing {windowType.Name} as a parameter is not allowed");
            // }

            string key = windowType.Name;

            // 윈도우 풀 확인
            if (windowPool.TryGetValue(key, out var pooledWindow))
            {
                if (pooledWindow != null)
                {
                    // 풀링된 윈도우를 메인 캔버스로 이동시키고 최상단에 표시
                    pooledWindow.transform.SetParent(mainCanvasScaler.transform);
                    pooledWindow.transform.SetAsLastSibling();

                    // 윈도우를 연다.
                    pooledWindow.Open();

                    return pooledWindow;
                }

                // 모종의 이유로 풀링된 오브젝트가 파괴된 상태라면 딕셔너리에서 제거하고 생성 단계로 넘어간다.
                UGUIWindowLog.LogError($"Found a window {key} in the object pool, but it was already destroyed!");
                windowPool.Remove(key);
            }

            // 프리팹 로드 및 생성
            var windowPrefab = Resources.Load(defaultWindowPath + key);
            GameObject createdObject = Instantiate(windowPrefab, mainCanvasScaler.transform) as GameObject;
            UGUIWindow createdWindow = createdObject.GetComponent<UGUIWindow>();

            // 메소드별 특화 로직 실행
            postInstantiationAction?.Invoke(createdWindow);

            // 윈도우 기본 설정
            string windowTitle = string.IsNullOrEmpty(windowName) ? key : windowName;
            createdObject.name = windowTitle; // 일관성을 위해 GameObject의 이름도 설정합니다.
            createdWindow.SetWindowTitle(windowTitle);

            // (오브젝트 풀링 기능 사용시) 윈도우 풀에 등록
            if (!createdWindow.allowMultipleInstance && createdWindow.useObjectPooling)
            {
                windowPool.Add(key, createdWindow);
            }

            // 현재 열려있는 윈도우 리스트에 등록
            currentlyOpenedWindows.AddLast(createdWindow);
            PromoteRecentlyFocusedWindow(createdWindow);

            // 윈도우 이벤트 리스너 등록
            createdWindow.OnOpenWindow.AddListener(OnWindowOpened);
            createdWindow.OnCloseWindow.AddListener(OnWindowClosed);
            createdWindow.OnFocusWindow.AddListener(OnWindowFocused);
            createdWindow.OnMinimizeWindow.AddListener(OnWindowMinimized);

            managedVisibleWindows.Add(createdWindow);
            OnManagedWindowOpened?.Invoke(createdWindow);

            return createdWindow;
        }

        /// <summary>
        /// 윈도우를 생성하는 함수입니다.
        /// </summary>
        /// <typeparam name="T">생성할 윈도우</typeparam>
        /// <param name="windowName">생성할 윈도우의 이름</param>
        /// <returns>UGUIWindow</returns>
        public static UGUIWindow CreateWindow<T>(string windowName = null) where T : UGUIWindow
        {
            Type windowType = typeof(T);
            return CreateWindow(windowType, windowName);
        }

        /// <summary>
        /// 윈도우를 생성하는 Non-Generic 함수입니다.
        /// </summary>
        /// <param name="windowType">생성할 윈도우의 Type</param>
        /// <param name="windowName">생성할 윈도우의 이름</param>
        /// <returns>UGUIWindow</returns>
        public static UGUIWindow CreateWindow(Type windowType, string windowName = null)
        {
            // 타입 검사
            if (!typeof(UGUIWindow).IsAssignableFrom(windowType))
            {
                throw new ArgumentException($"Passing {windowType.Name} as a parameter is not allowed");
            }

            var manager = Instance;
            return manager != null ? manager.GetOrCreateWindow(windowType, windowName, null) : null;
        }

        /// <summary>
        /// 위치, 크기 등 추가 옵션을 지정하여 윈도우를 생성합니다.
        /// </summary>
        /// <typeparam name="T">생성할 윈도우의 Type</typeparam>
        /// <param name="windowName">생성할 윈도우의 이름</param>
        /// <param name="x">생성할 윈도우의 x 좌표 값</param>
        /// <param name="y">생성할 윈도우의 y 좌표 값</param>
        /// <param name="width">생성할 윈도우의 너비 값</param>
        /// <param name="height">생성할 윈도우의 높이 값</param>
        /// <returns>UGUIWindow</returns>
        public static UGUIWindow CreateWindowEx<T>(string windowName, int x, int y, int width, int height) where T : UGUIWindow
        {
            // RectTransform 설정
            Action<UGUIWindow> setupAction = (createdWindow) =>
            {
                createdWindow.Move(x, y);
                createdWindow.Resize(width, height);
            };

            // 공통 메소드 호출
            Type windowType = typeof(T);
            var manager = Instance;
            return manager != null ? manager.GetOrCreateWindow(windowType, windowName, setupAction) : null;
        }

        /// <summary>
        /// 위치, 크기 등 추가 옵션을 지정하여 윈도우를 생성합니다.
        /// </summary>
        /// <typeparam name="T">생성할 윈도우의 Type</typeparam>
        /// <param name="windowName">생성할 윈도우의 이름</param>
        /// <param name="x">생성할 윈도우의 x 좌표 값</param>
        /// <param name="y">생성할 윈도우의 y 좌표 값</param>
        /// <param name="width">생성할 윈도우의 너비 값</param>
        /// <param name="height">생성할 윈도우의 높이 값</param>
        /// <param name="anchorMin">생성할 윈도우의 anchorMin</param>
        /// <param name="anchorMax">생성할 윈도우의 anchorMax</param>
        /// <returns>UGUIWindow</returns>
        public static UGUIWindow CreateWindowEx<T>(string windowName, int x, int y, int width, int height, Vector2 anchorMin, Vector2 anchorMax) where T : UGUIWindow
        {
            // RectTransform 설정
            Action<UGUIWindow> setupAction = (createdWindow) =>
            {
                createdWindow.SetAnchor(anchorMin, anchorMax);
                createdWindow.Move(x, y);
                createdWindow.Resize(width, height);
            };

            // 공통 메소드 호출
            Type windowType = typeof(T);
            var manager = Instance;
            return manager != null ? manager.GetOrCreateWindow(windowType, windowName, setupAction) : null;
        }
        #endregion

        #region Window - Event
        private void OnWindowOpened(UGUIWindow openedWindow)
        {
            EnsureRuntimeState();

            // 윈도우를 기본 캔버스로 이동시킨다.
            openedWindow.transform.SetParent(mainCanvasScaler.transform);

            // 오브젝트를 최상단으로 올림
            openedWindow.transform.SetAsLastSibling();

            // 이중 연결 리스트에서 최말단으로 이동
            currentlyOpenedWindows.Remove(openedWindow);
            currentlyOpenedWindows.AddLast(openedWindow);
            PromoteRecentlyFocusedWindow(openedWindow);

            managedVisibleWindows.Add(openedWindow);
            OnManagedWindowOpened?.Invoke(openedWindow);
        }

        private void OnWindowFocused(UGUIWindow focusedWindow)
        {
            EnsureRuntimeState();

            // 오브젝트를 최상단으로 올림
            focusedWindow.transform.SetAsLastSibling();

            // 이중 연결 리스트에서 최말단으로 이동
            currentlyOpenedWindows.Remove(focusedWindow);
            currentlyOpenedWindows.AddLast(focusedWindow);
            PromoteRecentlyFocusedWindow(focusedWindow);

            OnManagedWindowFocused?.Invoke(focusedWindow);
        }

        private void OnWindowMinimized(UGUIWindow minimizedWindow)
        {
            EnsureRuntimeState();

            // 최소화된 윈도우는 일단 열린 상태는 아닌 것으로 간주한다.
            currentlyOpenedWindows.Remove(minimizedWindow);

            // 윈도우를 최소화 풀로 이동시킨다.
            minimizedWindow.transform.SetParent(minimizedObjectPool.transform);

            managedVisibleWindows.Add(minimizedWindow);
            EnsureRecentlyFocusedWindow(minimizedWindow);
            OnManagedWindowMinimized?.Invoke(minimizedWindow);
        }

        private void OnWindowClosed(UGUIWindow closedWindow)
        {
            EnsureRuntimeState();

            currentlyOpenedWindows.Remove(closedWindow);
            managedVisibleWindows.Remove(closedWindow);
            recentlyFocusedWindows.Remove(closedWindow);

            // 윈도우가 오브젝트 풀링을 사용하는 경우
            if (closedWindow.useObjectPooling && !closedWindow.allowMultipleInstance)
            {
                // 윈도우를 오브젝트 풀 안으로 이동
                closedWindow.transform.SetParent(disabledObjectPool.transform);
            }

            OnManagedWindowClosed?.Invoke(closedWindow);
        }

        private void OnWindowDestroyed(UGUIWindow destroyedWindow)
        {

        }
        #endregion

        #region Window - Trim
        // 애플리케이션이 더 이상 메모리를 확보할 수 없을 때 호출 되는 함수
        private void OnLowMemory()
        {
            UGUIWindowLog.LogWarning("Low Memory Warning! Destroy unused window to reduce memory usage.");

            TrimWindow();
        }

        // 현재 사용하고 있지 않은 윈도우를 오브젝트 풀에서 제거
        public void TrimWindow()
        {
            EnsureRuntimeState();

            // 임시 리스트 생성
            List<string> keysToRemove = new List<string>();

            // 딕셔너리를 순회하며 제거할 대상을 찾음
            foreach (var window in windowPool)
            {
                if (!window.Value.isActiveAndEnabled)
                {
                    keysToRemove.Add(window.Key);
                }
            }

            // 수집한 키 리스트를 기반으로 딕셔너리에서 오브젝트 제거
            foreach (var targetKey in keysToRemove)
            {
                Destroy(windowPool[targetKey].gameObject);
                windowPool.Remove(targetKey);
            }
        }
        #endregion

        #region InputSystem
        private void OnCancel()
        {
            EnsureRuntimeState();

            // 전체화면 중이라면 창을 닫지 않고 전체화면부터 해제한다.
            if (fullScreenWindow != null)
            {
                fullScreenWindow.ExitFullScreen();
                return;
            }

            // 만약 열려있는 윈도우가 있다면 최말단 윈도우를 닫는다.
            if (currentlyOpenedWindows.Count > 0)
            {
                currentlyOpenedWindows.Last().Close();
                return;
            }

            // 열려있는 윈도우가 없다면
            if (defaultWindowOnEscape == null)
            {
                UGUIWindowLog.LogWarning(
                    $"{nameof(defaultWindowOnEscape)}가 지정되지 않아 Escape로 열 기본 Window가 없습니다. " +
                    $"{nameof(UGUIWindowManager)} 프리팹에서 지정하세요.", this);
                return;
            }

            CreateWindow(defaultWindowOnEscape.GetType());
        }
        #endregion
    }
}
