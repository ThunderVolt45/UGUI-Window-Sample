# 08. API 레퍼런스

> [← 매뉴얼 목차로](../Manual.md)

자주 쓰는 public API 요약입니다. 전체 멤버와 관계는 [클래스 다이어그램](../ClassDiagram.md)을 참고하세요.

**목차**

- [UGUIWindowManager (정적 진입점)](#uguiwindowmanager-정적-진입점)
- [UGUIWindow (개별 창)](#uguiwindow-개별-창)
- [UGUIWindowView (뷰)](#uguiwindowview-뷰)
- [UGUIWindowContent (본문 스크롤)](#uguiwindowcontent-본문-스크롤)
- [열거형](#열거형)
- [로깅 (UGUIWindowLog)](#로깅-uguiwindowlog)

---

## UGUIWindowManager (정적 진입점)

### 창 생성

```csharp
static UGUIWindow CreateWindow<T>(string windowName = null) where T : UGUIWindow
static UGUIWindow CreateWindow(Type windowType, string windowName = null)
static UGUIWindow CreateWindowEx<T>(string windowName, int x, int y, int width, int height)
static UGUIWindow CreateWindowEx<T>(string windowName, int x, int y, int width, int height,
                                    Vector2 anchorMin, Vector2 anchorMax)
```

- 프리팹은 `Resources/Windows/{타입명}.prefab`에서 로드됩니다(이름 일치 필수).
- `CreateWindow(Type)`는 `UGUIWindow` 비상속 타입에 `ArgumentException`을 던집니다.

### DPI / 기타

```csharp
static void  SetDPI(int screenWidth, int screenHeight, float dpi) // 캔버스 스케일 조정 + PlayerPrefs 저장
static float CurrentDPI { get; }                                   // 현재 DPI
IReadOnlyList<UGUIWindow> GetOpenWindows()                         // MainCanvas 하위 활성 창
IReadOnlyList<UGUIWindow> GetVisibleWindows()                       // MainCanvas + 최소화 풀의 활성 창
IReadOnlyList<UGUIWindow> GetSwitchableWindows()                    // 최근 포커스 순서의 전환 후보
UGUIWindow   GetFocusedWindow()                                     // MainCanvas의 마지막 활성 Window
void         FocusWindow(UGUIWindow window)                         // 최소화 복원 포함 포커스
void         TrimWindow()                                          // 비활성 풀의 창을 파괴
Vector2      GetPointerDeltaInRect(PointerEventData eventData,
                                   RectTransform relativeTo)        // 포인터 델타를 RectTransform 로컬 좌표로 변환
float        ScreenMultiplierWidth { get; }                        // 드래그 DPI 보정 계수
float        ScreenMultiplierHeight { get; }
UnityEvent<int,int,float> OnDPIChanged                             // DPI 변경 알림
```

## UGUIWindow (개별 창)

### 동작 메서드

```csharp
void Open()                                  // 열기 (+ OnOpenWindow)
async void Close()                           // Fade 후 닫기 (+ OnCloseWindow)
void Focus()                                 // 포커스/최상단 (+ OnFocusWindow)
void Minimize()                              // 최소화 (+ OnMinimizeWindow)
void Maximize()                              // 최대화 (isResizable 필요)
void RestoreWindow()                         // 복원 (isResizable 필요)
void ChangeWindowMode(UGUIWindowMode mode)   // 모드 전환 분기
void RestoreFromMinimized()                  // 최소화 해제 및 기존 레이아웃 모드 복원
void Move(int x, int y)                       // 위치 설정 (+ 상태 기억)
void Resize(int width, int height)            // 크기 설정 (+ 상태 기억)
void SetAnchor(Vector2 anchorMin, Vector2 anchorMax) // 앵커 설정 (+ 상태 기억)
void SetWindowTitle(string title)            // 타이틀 변경
void MemorizeLastWindowState()               // 현재 상태 스냅샷 저장
```

### 프로퍼티 / 필드

```csharp
UGUIWindowMode WindowMode { get; set; }      // 레이아웃 모드와 최소화 상태를 합친 현재 모드
bool HasHeader { get; set; }
bool HasBorder { get; set; }
bool HasExitButton { get; set; }
bool HasMaximizeButton { get; set; }
RectTransform RectTransform { get; }
string WindowTitle { get; }                  // SetWindowTitle로 지정된 제목

bool allowMultipleInstance                   // 중복 생성 허용(풀링 비활성)
bool useObjectPooling                         // 풀링 사용
bool isMovable                                // 헤더 드래그 이동
bool isResizable                              // 보더/엣지 리사이즈
Vector2 minimumWindowSize                     // 최소 크기
```

### 이벤트

```csharp
UnityEvent<UGUIWindow> OnOpenWindow
UnityEvent<UGUIWindow> OnCloseWindow
UnityEvent<UGUIWindow> OnFocusWindow
UnityEvent<UGUIWindow> OnMinimizeWindow
```

### 상속 시 주의

`Awake`/`OnEnable`을 재정의하면 **반드시 `base`를 먼저 호출**하세요(View 연결·상태 초기화·Fade 인트로 담당).

```csharp
protected override void Awake()  { base.Awake();  /* ... */ }
protected override void OnEnable(){ base.OnEnable(); /* ... */ }
```

## UGUIWindowView (뷰)

```csharp
void SetTitle(string title)
void SetHeaderActive(bool value)
void SetBorderActive(bool value)
void SetExitButtonActive(bool value)
void SetMaximizeButtonActive(bool value)
void SetActive(bool value)
Awaitable Fade(float startAlpha, float targetAlpha, float startScale, float targetScale, float dur = 0.15f)
void ApplyMaximizedState(float headerHeight)
void ApplyRestoredState(UGUIWindowState state)
RectTransform RectTransform { get; }
```

대부분 `UGUIWindow`가 내부적으로 호출하므로 직접 쓸 일은 드뭅니다.

## UGUIWindowContent (본문 스크롤)

`Content` 오브젝트에 붙어 창 본문의 마스킹과 스크롤 상태를 관리합니다. 실제 본문 UI는 `Content/Viewport/ScrollContent` 아래에 배치합니다.

```csharp
bool enableHorizontalScroll   // 기본 false
bool enableVerticalScroll     // 기본 true
Vector2 minimumContentSize    // LayoutUtility min 값보다 큰 수동 최소 크기

RectTransform viewport
RectTransform scrollContent
Scrollbar horizontalScrollbar
Scrollbar verticalScrollbar

void SetDirty()               // 다음 LateUpdate에서 스크롤 상태 재계산
```

스크롤바는 `ScrollContent`의 최소 요구 크기가 `Viewport`보다 커지는 축에서만 표시됩니다. 최소 요구 크기는 `LayoutElement`, LayoutGroup 계열, 또는 `minimumContentSize`로 지정합니다.

## 열거형

```csharp
enum UGUIWindowMode    { Windowed, Maximized, Minimized }
enum UGUIBorderPosition{ North, South, East, West }
enum UGUIEdgePosition  { NorthEast, NorthWest, SouthEast, SouthWest }
enum UGUICursor        { Default, ResizeHorizontal, ResizeVetical, ResizeDiagonalNeSw, ResizeDiagonalNwSe }
enum UGUIWindowLogLevel{ Info, Warning, Error, None }
```

## 로깅 (UGUIWindowLog)

```csharp
static void Log(object message)
static void LogWarning(object message)
static void LogError(object message)
// (각각 Object context 오버로드 존재)
```

빌드 종류별 로그 레벨(에디터=Info, 개발=Warning, 릴리즈=Error)에 따라 출력 여부가 결정됩니다.

## 관련 문서

- [클래스 다이어그램 인덱스](../ClassDiagram.md)
- [매뉴얼 목차](../Manual.md)
</content>
