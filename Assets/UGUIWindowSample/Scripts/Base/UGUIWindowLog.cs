using System.Security.Cryptography;
using UnityEngine;

namespace UGUIWindow
{
    public class UGUIWindowLog : MonoBehaviour
    {
        private static UGUIWindowLog _instance;
        private static object locker = new object(); // Thread-Safe한 싱글톤 패턴을 구현하기 위한 lock

        public static UGUIWindowLog Instance
        {
            get
            {
                // 인스턴스가 존재하지 않을 경우
                if (_instance == null)
                {
                    // 일단 인스턴스가 존재하는 지 확인
                    _instance = FindAnyObjectByType<UGUIWindowLog>(FindObjectsInactive.Include);

                    // 인스턴스가 존재한다면 활성화
                    if (_instance != null)
                    {
                        _instance.enabled = true;
                    }
                    // 없다면 새로 생성
                    else
                    {
                        // lock은 비싼 연산에 속하므로 Double-checked locking을 사용한다
                        // https://stackoverflow.com/questions/12316406/thread-safe-c-sharp-singleton-pattern
                        lock (locker)
                        {
                            if (_instance == null)
                            {
                                GameObject obj = new GameObject("UGUIWindowLogger");
                                _instance = obj.AddComponent<UGUIWindowLog>();
                            }
                        }
                    }
                }

                // 인스턴스 반환
                return _instance;
            }
        }

        [SerializeField] private UGUIWindowLogLevel releaseLogLevel = UGUIWindowLogLevel.Error;
        [SerializeField] private UGUIWindowLogLevel devLogLevel = UGUIWindowLogLevel.Warning;
        [SerializeField] private UGUIWindowLogLevel editorLogLevel = UGUIWindowLogLevel.Info;

        private UGUIWindowLogLevel logLevel = UGUIWindowLogLevel.None;

        private void Awake()
        {
#if UNITY_EDITOR
            logLevel = editorLogLevel;
#elif DEVELOPMENT_BUILD
            logLevel = devLogLevel;
#else
            logLevel = releaseLogLevel;
#endif
        }

        public static void Log(object message)
        {
            if (Instance.logLevel > UGUIWindowLogLevel.Info)
                return;

            Debug.Log(message);
        }

        public static void Log(object message, Object context)
        {
            if (Instance.logLevel > UGUIWindowLogLevel.Info)
                return;

            Debug.Log(message, context);
        }

        public static void LogWarning(object message)
        {
            if (Instance.logLevel > UGUIWindowLogLevel.Warning)
                return;

            Debug.LogWarning(message);
        }

        public static void LogWarning(object message, Object context)
        {
            if (Instance.logLevel > UGUIWindowLogLevel.Warning)
                return;

            Debug.LogWarning(message, context);
        }

        public static void LogError(object message)
        {
            if (Instance.logLevel > UGUIWindowLogLevel.Error)
                return;

            Debug.LogError(message);
        }

        public static void LogError(object message, Object context)
        {
            if (Instance.logLevel > UGUIWindowLogLevel.Error)
                return;

            Debug.LogError(message, context);
        }
    }
}
