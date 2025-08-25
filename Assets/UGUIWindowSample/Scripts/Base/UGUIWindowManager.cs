using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UGUIWindow
{
    public class UGUIWindowManager : MonoBehaviour
    {
        private static UGUIWindowManager _instance;
        private static object locker = new object(); // Thread-Safe한 싱글톤 패턴을 구현하기 위한 lock

        public static UGUIWindowManager Instance
        {
            get
            {
                // 인스턴스가 존재하지 않을 경우
                if (_instance == null)
                {
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
                                var managerPrefab = Resources.Load<GameObject>("UGUIWindowManager");
                                var managerGameObject = Instantiate(managerPrefab);
                                _instance = managerGameObject.GetComponent<UGUIWindowManager>();
                            }
                        }
                    }
                }

                // 인스턴스 반환
                return _instance;
            }
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

        [Header("Object Pool")]
        [Tooltip("Window가 최소화되었을 때 이동할 오브젝트의 CanvasScaler")]
        [SerializeField] private CanvasScaler minimizedObjectPool;

        [Tooltip("Window가 비활성화 되었을 때 이동할 오브젝트의 CanvasScaler")]
        [SerializeField] private CanvasScaler disabledObjectPool;

        [Header("Event Listener")]
        [Space(5f)]
        [Tooltip("DPI 설정이 변경되었을 때 실행할 Event")]
        public UnityEvent<int, int, float> OnDPIChanged;

        // 현재 열려있는 윈도우의 순서를 저장하는 이중 연결 리스트
        private DoublyLinkedList<UGUIWindow> currentlyOpenedWindows;

        // 생성된 윈도우가 저장된 오브젝트 풀
        private Dictionary<string, UGUIWindow> windowPool;

        public static float CurrentDPI
        {
            get { return Instance._currentDPI; }
        }

        public float ScreenMultiplierWidth
        {
            get { return _screenMultiplierWidth; }
        }

        public float ScreenMultiplierHeight
        {
            get { return _screenMultiplierHeight; }
        }

        private float _currentDPI = 2f;
        private float _screenMultiplierWidth = 1f;
        private float _screenMultiplierHeight = 1f;

        #region Initialize
        private void Awake()
        {
            // 인스턴스 중복이 감지된다면 자폭한다
            if (_instance != null)
            {
                Destroy(gameObject);
            }
            
            // 자료구조 초기화
            currentlyOpenedWindows = new DoublyLinkedList<UGUIWindow>();
            windowPool = new Dictionary<string, UGUIWindow>();

            // 애플리케이션이 더 이상 메모리를 확보할 수 없을 때 호출할 함수 등록
            Application.lowMemory += OnLowMemory;

            // 씬 이동으로 인해 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 캔버스 초기화
            InitializeCanvas();

            // 윈도우 생성
            CreateWindow<UGUIWindow>();
            CreateWindowEx<UGUIWindowMultipleInstanceSample>(null, -200, 0, 250, 250);
            CreateWindowEx<UGUIWindowMultipleInstanceSample>("MultipleInstanceSample", -150, 50, 250, 250);
            CreateWindowEx<UGUIWindowMultipleInstanceSample>("MultipleInstanceSample", -100, 100, 250, 250);
        }

        private void InitializeCanvas()
        {
            var currentResolution = Screen.currentResolution;
            var dpi = PlayerPrefs.GetFloat("DPI Settings", 2f);

            SetDPI(currentResolution.width, currentResolution.height, dpi);
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
            // 화면 해상도와 DPI 값에 맞춰 캔버스의 설정을 변경한다.
            Instance.ChangeCanvasResolution(screenWidth, screenHeight, dpi);

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
        #endregion

        #region Window - Create
        /// <summary>
        /// 윈도우을 생성하는 private 메소드
        /// </summary>
        /// <param name="windowName">윈도우의 이름</param>
        /// <param name="postInstantiationAction">윈도우 GameObject가 생성된 후 실행할 추가 작업</param>
        private UGUIWindow GetOrCreateWindow(Type windowType, string windowName, Action<GameObject> postInstantiationAction = null)
        {
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
            postInstantiationAction?.Invoke(createdObject);

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

            // 윈도우 이벤트 리스너 등록
            createdWindow.OnOpenWindow.AddListener(OnWindowOpened);
            createdWindow.OnCloseWindow.AddListener(OnWindowClosed);
            createdWindow.OnFocusWindow.AddListener(OnWindowFocused);
            createdWindow.OnMinimizeWindow.AddListener(OnWindowMinimized);

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

            return Instance.GetOrCreateWindow(windowType, windowName, null);
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
            Action<GameObject> setupAction = (createdObject) =>
            {
                var windowTransform = createdObject.transform as RectTransform;
                windowTransform.anchoredPosition = new Vector2(x, y);
                windowTransform.sizeDelta = new Vector2(width, height);
            };

            // 공통 메소드 호출
            Type windowType = typeof(T);
            return Instance.GetOrCreateWindow(windowType, windowName, setupAction);
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
            Action<GameObject> setupAction = (createdObject) =>
            {
                var windowTransform = createdObject.transform as RectTransform;
                windowTransform.anchorMin = anchorMin;
                windowTransform.anchorMax = anchorMax;
                windowTransform.anchoredPosition = new Vector2(x, y);
                windowTransform.sizeDelta = new Vector2(width, height);
            };

            // 공통 메소드 호출
            Type windowType = typeof(T);
            return Instance.GetOrCreateWindow(windowType, windowName, setupAction);
        }
        #endregion

        #region Window - Event
        private void OnWindowOpened(UGUIWindow openedWindow)
        {
            // 윈도우를 기본 캔버스로 이동시킨다.
            openedWindow.transform.SetParent(mainCanvasScaler.transform);

            // 오브젝트를 최상단으로 올림
            openedWindow.transform.SetAsLastSibling();

            // 이중 연결 리스트에서 최말단으로 이동
            currentlyOpenedWindows.Remove(openedWindow);
            currentlyOpenedWindows.AddLast(openedWindow);
        }

        private void OnWindowFocused(UGUIWindow focusedWindow)
        {
            // 오브젝트를 최상단으로 올림
            focusedWindow.transform.SetAsLastSibling();

            // 이중 연결 리스트에서 최말단으로 이동
            currentlyOpenedWindows.Remove(focusedWindow);
            currentlyOpenedWindows.AddLast(focusedWindow);
        }

        private void OnWindowMinimized(UGUIWindow minimizedWindow)
        {
            // 최소화된 윈도우는 일단 열린 상태는 아닌 것으로 간주한다.
            currentlyOpenedWindows.Remove(minimizedWindow);

            // 윈도우를 최소화 풀로 이동시킨다.
            minimizedWindow.transform.SetParent(minimizedObjectPool.transform);
        }

        private void OnWindowClosed(UGUIWindow closedWindow)
        {
            currentlyOpenedWindows.Remove(closedWindow);

            // 윈도우가 오브젝트 풀링을 사용하는 경우
            if (closedWindow.useObjectPooling && !closedWindow.allowMultipleInstance)
            {
                // 윈도우를 오브젝트 풀 안으로 이동
                closedWindow.transform.SetParent(disabledObjectPool.transform);
            }
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
            // 만약 열려있는 윈도우가 있다면 최말단 윈도우를 닫는다.
            if (currentlyOpenedWindows.Count > 0)
            {
                currentlyOpenedWindows.Last().Close();
                return;
            }

            // 열려있는 윈도우가 없다면
            CreateWindow(defaultWindowOnEscape.GetType());
        }
        #endregion
    }
}
