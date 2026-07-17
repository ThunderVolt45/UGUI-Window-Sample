using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// 작업 표시줄 아이콘.
    ///
    /// 아이콘의 정체성은 창 인스턴스가 아니라 <see cref="AppClassName"/>(앱 클래스명)이다.
    /// 덕분에 창이 없는 상태(=핀 고정된 런처)로도 존재할 수 있고, 클릭하면 창을 새로 띄운다.
    /// 창이 열리면 <see cref="UGUITaskBar"/>가 <see cref="Bind"/>로 붙이고, 닫히면 <see cref="Unbind"/>한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class UGUITaskIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;

        private readonly Color normalColor = new Color(0.18f, 0.2f, 0.24f, 0.96f);
        private readonly Color focusedColor = new Color(0.27f, 0.48f, 0.84f, 1f);
        private readonly Color minimizedColor = new Color(0.12f, 0.13f, 0.16f, 0.82f);
        // 핀 고정됐지만 실행되지 않은 앱. 실행 중인 아이콘보다 흐려서 한눈에 구분된다.
        private readonly Color pinnedColor = new Color(0.18f, 0.2f, 0.24f, 0.4f);

        private UGUIWindow targetWindow;
        private string appClassName;
        private bool isPinned;
        private bool isHovered;

        // 창 프리팹에서 읽어온 런처용 아이콘·이름(Resources.Load 반복 방지용 캐시).
        private Sprite pinnedSprite;
        private string pinnedTitle;
        private bool prefabResolved;

        public UGUIWindow TargetWindow
        {
            get { return targetWindow; }
        }

        /// <summary>이 아이콘이 대표하는 앱의 클래스명. 창이 없어도 유지되는 정체성.</summary>
        public string AppClassName
        {
            get { return appClassName; }
        }

        /// <summary>핀 고정 아이콘이면 true. 창을 닫아도 작업 표시줄에 남아 런처로 동작한다.</summary>
        public bool IsPinned
        {
            get { return isPinned; }
        }

        /// <summary>창이 열려 있으면 true(최소화 포함).</summary>
        public bool IsRunning
        {
            get { return targetWindow != null; }
        }

        /// <summary>포인터가 이 아이콘 위에 있으면 true. 작업 표시줄이 이름 툴팁을 띄울 때 참조한다.</summary>
        public bool IsHovered
        {
            get { return isHovered; }
        }

        /// <summary>
        /// 툴팁에 표시할 앱 이름. 실행 중이면 창의 실제 제목을, 아니면 프리팹에 저장된 제목을 쓴다.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (targetWindow != null && !string.IsNullOrWhiteSpace(targetWindow.WindowTitle))
                {
                    return targetWindow.WindowTitle;
                }

                return ResolvePinnedTitle();
            }
        }

        public RectTransform RectTransform
        {
            get { return (RectTransform)transform; }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void SetReferences(Image background, Image icon)
        {
            backgroundImage = background;
            iconImage = icon;
            ResolveReferences();
        }

        /// <summary>열린 창에 대응하는 아이콘으로 초기화한다(핀 고정 아님 → 창이 닫히면 사라진다).</summary>
        public void Initialize(UGUIWindow window)
        {
            ResolveReferences();

            targetWindow = window;
            appClassName = window != null ? window.GetType().Name : null;
            isPinned = false;
            isHovered = false;
            Refresh(false);
        }

        /// <summary>창 없이 앱만 대표하는 핀 고정 런처로 초기화한다.</summary>
        public void InitializePinned(string className)
        {
            ResolveReferences();

            targetWindow = null;
            appClassName = className;
            isPinned = true;
            isHovered = false;
            pinnedSprite = null;
            pinnedTitle = null;
            prefabResolved = false;
            Refresh(false);
        }

        /// <summary>핀 아이콘에 실행된 창을 연결한다.</summary>
        public void Bind(UGUIWindow window)
        {
            targetWindow = window;
            Refresh(false);
        }

        /// <summary>창이 닫혔을 때 연결을 끊고 런처 상태로 되돌린다.</summary>
        public void Unbind()
        {
            targetWindow = null;
            Refresh(false);
        }

        public void Refresh(bool focused)
        {
            bool running = targetWindow != null;
            bool minimized = running && targetWindow.WindowMode == UGUIWindowMode.Minimized;

            if (backgroundImage != null)
            {
                backgroundImage.color = !running ? pinnedColor
                    : minimized ? minimizedColor
                    : focused ? focusedColor
                    : normalColor;
            }

            if (iconImage != null)
            {
                // 실행 중이면 창 인스턴스가, 아니면(핀 런처) 프리팹이 아이콘의 출처다.
                Sprite sprite = running ? targetWindow.WindowIcon : ResolvePinnedSprite();

                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
                iconImage.preserveAspect = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // 데스크톱 아이콘과 달리 작업 표시줄은 한 번 클릭으로 실행한다.
            if (targetWindow == null)
            {
                Launch();
            }
            else if (targetWindow.WindowMode == UGUIWindowMode.Minimized)
            {
                targetWindow.RestoreFromMinimized();
            }
            else
            {
                targetWindow.Focus();
            }
        }

        private void Launch()
        {
            if (string.IsNullOrWhiteSpace(appClassName))
            {
                UGUIWindowLog.LogError("Task icon has no app class name to launch.");
                return;
            }

            Type windowType = ResolveWindowType();
            if (windowType == null)
            {
                UGUIWindowLog.LogError($"Cannot launch unknown window class: {appClassName}");
                return;
            }

            // 단일 인스턴스 창은 매니저가 풀에서 기존 창을 되살리므로 중복 생성되지 않는다.
            // 생성 결과는 여기서 붙이지 않는다 — 매니저의 OnManagedWindowOpened를 받은
            // UGUITaskBar가 이 아이콘에 Bind해, 실행 경로가 어디든 동일하게 흐른다.
            UGUIWindowManager.CreateWindow(windowType);
        }

        // UGUIIcon과 동일한 해석 방식.
        private Type ResolveWindowType()
        {
            Type windowType = Type.GetType($"UGUIWindow.{appClassName}", false);
            return windowType != null && typeof(UGUIWindow).IsAssignableFrom(windowType)
                ? windowType
                : null;
        }

        // 핀 런처는 창 인스턴스가 없으므로 아이콘·이름을 창 프리팹에서 읽는다.
        private void ResolvePrefab()
        {
            if (prefabResolved)
            {
                return;
            }

            prefabResolved = true;

            if (string.IsNullOrWhiteSpace(appClassName))
            {
                return;
            }

            var windowPrefab = Resources.Load<GameObject>($"Windows/{appClassName}");
            if (windowPrefab != null && windowPrefab.TryGetComponent(out UGUIWindow prefabWindow))
            {
                pinnedSprite = prefabWindow.WindowIcon;
                pinnedTitle = prefabWindow.DefaultTitle;
            }
        }

        private Sprite ResolvePinnedSprite()
        {
            ResolvePrefab();
            return pinnedSprite;
        }

        private string ResolvePinnedTitle()
        {
            ResolvePrefab();
            return string.IsNullOrWhiteSpace(pinnedTitle) ? appClassName : pinnedTitle;
        }

        private void ResolveReferences()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
        }
    }
}
