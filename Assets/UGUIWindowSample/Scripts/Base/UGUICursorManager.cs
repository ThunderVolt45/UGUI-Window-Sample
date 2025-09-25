using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace UGUIWindow
{
    public enum UGUICursor
    {
        Default,
        ResizeHorizontal,
        ResizeVetical,
        ResizeDiagonalNeSw,
        ResizeDiagonalNwSe,
    }

    public class UGUICursorManager : MonoBehaviour
    {
        private static UGUICursorManager _instance;
        private static object locker = new object(); // Thread-Safe한 싱글톤 패턴을 구현하기 위한 lock

        public static UGUICursorManager Instance
        {
            get
            {
                // 인스턴스가 존재하지 않을 경우
                if (_instance == null)
                {
                    // 일단 인스턴스가 존재하는 지 확인
                    _instance = FindAnyObjectByType<UGUICursorManager>(FindObjectsInactive.Include);

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
                                _instance = managerGameObject.GetComponent<UGUICursorManager>();
                            }
                        }
                    }
                }

                // 인스턴스 반환
                return _instance;
            }
        }

        [Header("Cursor Textures")]
        [SerializeField] public Texture2D defaultCursor;
        [SerializeField] public Vector2 defaultCursorHotspot;

        [Space(5f)]
        [SerializeField] public Texture2D resizeHorizontalCursor;
        [SerializeField] public Vector2 resizeHorizontalCursorHotspot;

        [Space(5f)]
        [SerializeField] public Texture2D resizeVerticalCursor;
        [SerializeField] public Vector2 resizeVerticalCursorHotspot;

        [Space(5f)]
        [SerializeField] public Texture2D resizeDiagonalNeSwCursor;
        [SerializeField] public Vector2 resizeDiagonalNeSwCursorHotspot;

        [Space(5f)]
        [SerializeField] public Texture2D resizeDiagonalNwSeCursor;
        [SerializeField] public Vector2 resizeDiagonalNwSeCursorHotspot;

        private void Awake()
        {
            SetCursor(UGUICursor.Default);
        }

        /// <summary>
        /// 현재 마우스 커서 위치에 있는 가장 상위의 UI GameObject를 반환합니다.
        /// </summary>
        /// <returns>UI GameObject. 없다면 null.</returns>
        public GameObject GetObjectUnderCursor(Vector2 cursorPosition)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = cursorPosition;

            // 레이캐스트 결과를 저장할 리스트를 생성합니다.
            List<RaycastResult> results = new List<RaycastResult>();

            // UI 레이캐스터를 통해 포인터 위치를 기준으로 레이캐스트를 실행하고, 모든 결과를 results 리스트에 저장합니다.
            EventSystem.current.RaycastAll(pointerData, results);

            // 결과가 하나 이상 있다면, 가장 앞에 있는(가장 위에 그려진) 오브젝트를 반환합니다.
            if (results.Count > 0)
            {
                return results[0].gameObject;
            }

            // 결과가 없다면 null을 반환합니다.
            return null;
        }

        public static void SetCursor(UGUICursor cursor)
        {
            switch (cursor)
            {
                case UGUICursor.ResizeHorizontal:
                    Cursor.SetCursor(Instance.resizeHorizontalCursor, Instance.resizeHorizontalCursorHotspot, CursorMode.Auto);
                    break;
                case UGUICursor.ResizeVetical:
                    Cursor.SetCursor(Instance.resizeVerticalCursor, Instance.resizeVerticalCursorHotspot, CursorMode.Auto);
                    break;
                case UGUICursor.ResizeDiagonalNeSw:
                    Cursor.SetCursor(Instance.resizeDiagonalNeSwCursor, Instance.resizeDiagonalNeSwCursorHotspot, CursorMode.Auto);
                    break;
                case UGUICursor.ResizeDiagonalNwSe:
                    Cursor.SetCursor(Instance.resizeDiagonalNwSeCursor, Instance.resizeDiagonalNwSeCursorHotspot, CursorMode.Auto);
                    break;
                default:
                    Cursor.SetCursor(Instance.defaultCursor, Instance.defaultCursorHotspot, CursorMode.Auto);
                    break;
            }
        }

        #region InputSystem
        private void OnPoint(InputValue input)
        {
            var pointedObject = GetObjectUnderCursor(input.Get<Vector2>());

            if (pointedObject == null)
            {
                SetCursor(UGUICursor.Default);
                return;
            }

            if (pointedObject.TryGetComponent<UGUIWindowBorder>(out var border))
                {
                    switch (border.borderPosition)
                    {
                        case UGUIBorderPosition.East:
                        case UGUIBorderPosition.West:
                            SetCursor(UGUICursor.ResizeHorizontal);
                            return;
                        case UGUIBorderPosition.North:
                        case UGUIBorderPosition.South:
                            SetCursor(UGUICursor.ResizeVetical);
                            return;
                    }
                }

            if (pointedObject.TryGetComponent<UGUIWindowEdge>(out var edge))
            {
                switch (edge.edgePosition)
                {
                    case UGUIEdgePosition.NorthEast:
                    case UGUIEdgePosition.SouthWest:
                        SetCursor(UGUICursor.ResizeDiagonalNwSe);
                        return;
                    case UGUIEdgePosition.NorthWest:
                    case UGUIEdgePosition.SouthEast:
                        SetCursor(UGUICursor.ResizeDiagonalNeSw);
                        return;
                }
            }

            SetCursor(UGUICursor.Default);
        }
        #endregion
    }
}
