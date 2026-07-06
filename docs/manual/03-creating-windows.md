# 03. 창 만들기

> [← 매뉴얼 목차로](../Manual.md)

이 장은 기존 창을 생성하는 API와, **나만의 커스텀 창**을 정의하는 방법을 다룹니다.

**목차**

- [창 생성 API](#창-생성-api)
- [리소스 규약 (중요)](#리소스-규약-중요)
- [커스텀 창 만들기 — 단계별](#커스텀-창-만들기--단계별)
- [본문 UI 배치와 스크롤](#본문-ui-배치와-스크롤)
- [같은 창을 여러 개 띄우기](#같은-창을-여러-개-띄우기)

---

## 창 생성 API

모든 생성은 `UGUIWindowManager`의 정적 메서드로 이루어집니다.

```csharp
// 1) 타입으로 생성 (가장 일반적)
UGUIWindowManager.CreateWindow<UGUIMenu>();

// 2) 이름을 지정해 생성 (타이틀 + GameObject 이름에 반영)
UGUIWindowManager.CreateWindow<UGUIMenu>("파일 메뉴");

// 3) Type 객체로 생성 (제네릭을 쓸 수 없는 상황용)
UGUIWindowManager.CreateWindow(typeof(UGUIMenu));

// 4) 위치·크기를 지정해 생성
UGUIWindowManager.CreateWindowEx<UGUIMenu>("파일 메뉴", x: 0, y: 0, width: 300, height: 400);

// 5) 앵커까지 지정해 생성
UGUIWindowManager.CreateWindowEx<UGUIMenu>(
    "파일 메뉴", x: 0, y: 0, width: 300, height: 400,
    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f));
```

| 메서드 | 용도 |
| --- | --- |
| `CreateWindow<T>(name = null)` | 타입 `T`의 창 생성 |
| `CreateWindow(Type, name = null)` | 런타임 `Type`으로 생성 (예: 문자열 → 타입) |
| `CreateWindowEx<T>(name, x, y, w, h)` | 위치·크기 지정 생성 |
| `CreateWindowEx<T>(name, x, y, w, h, anchorMin, anchorMax)` | 앵커까지 지정 생성 |

- `name`을 생략하면 타입 이름이 타이틀로 쓰입니다.
- `CreateWindow(Type)`는 전달 타입이 `UGUIWindow`를 상속하지 않으면 `ArgumentException`을 던집니다.

> **문자열로 창 열기** — 샘플의 `UGUIIcon`은 `targetClassName` 문자열로 `Type.GetType($"UGUIWindow.{name}")`을 만들어 `CreateWindow(Type)`에 넘깁니다. 데이터 기반으로 창을 띄울 때 유용한 패턴입니다.

## 리소스 규약 (중요)

매니저는 창 프리팹을 **이름 규약**으로 자동 로드합니다.

```
Assets/Resources/Windows/{클래스명}.prefab
```

내부적으로 `Resources.Load("Windows/" + windowType.Name)`을 호출하므로:

- 프리팹 **파일 이름**이 창 클래스 이름과 **정확히 일치**해야 합니다. (`UGUIMenu` 클래스 → `UGUIMenu.prefab`)
- 풀링 키도 같은 타입 이름이라, 한 타입당 하나의 프리팹이 대응됩니다.

현재 프로젝트에 등록된 창 프리팹:

```
Assets/Resources/Windows/UGUIWindow.prefab
Assets/Resources/Windows/UGUIMenu.prefab
Assets/Resources/Windows/UGUIApplicationSetting.prefab
Assets/Resources/Windows/UGUIWindowMultipleInstanceSample.prefab
```

## 커스텀 창 만들기 — 단계별

### 1단계. 클래스 정의

`UGUIWindow`를 상속합니다. 가장 단순하게는 비어 있어도 됩니다.

```csharp
using UGUIWindow;

public class MyWindow : UGUIWindow
{
    // 창 고유 로직 추가
}
```

### 2단계. 초기화 시 base 호출 (override 시 주의)

`Awake`/`OnEnable`을 재정의한다면 **반드시 base를 먼저 호출**하세요. 부모가 View 연결과 상태 초기화를 수행하기 때문입니다.

```csharp
public class MyWindow : UGUIWindow
{
    protected override void Awake()
    {
        base.Awake();   // ← 필수
        // 내 초기화
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // ← Fade 인트로 애니메이션 수행
        // 매번 열릴 때 갱신할 로직
    }
}
```

> 샘플의 `UGUIApplicationSetting`이 이 패턴을 따릅니다: `base.Awake()` 후 드롭다운/버튼을 초기화하고, `base.OnEnable()` 후 현재 설정을 읽어 옵니다.

### 3단계. 프리팹 제작

1. 빈 창 골격이 필요하면 메뉴 `GameObject ▸ UGUI Window ▸ Create Window Templete`로 `UGUIWindowView` + `UGUIWindow`를 가진 템플릿을 만듭니다.
2. 템플릿에 헤더·보더·엣지를 붙이려면 인스펙터의 **Create & Assignment Base Components** 버튼을 사용합니다([05. 에디터 도구](05-editor-tools.md)).
3. 컴포넌트를 `MyWindow`로 교체하고 본문 UI를 `Content/Viewport/ScrollContent` 아래에 구성합니다.
4. 프리팹을 **`Assets/Resources/Windows/MyWindow.prefab`**으로 저장합니다(이름 일치 필수).

### 4단계. 생성

```csharp
UGUIWindowManager.CreateWindow<MyWindow>();
```

## 본문 UI 배치와 스크롤

스크롤 가능한 창 본문은 다음 구조를 사용합니다.

```text
Content
├─ Viewport
│  └─ ScrollContent
├─ VerticalScrollbar
└─ HorizontalScrollbar
```

- 실제 버튼, 텍스트, 패널 등 창의 본문 UI는 `ScrollContent` 아래에 배치합니다.
- `Content`에는 `UGUIWindowContent`와 `ScrollRect`가 붙어 있어야 합니다.
- `Viewport`에는 `RectMask2D`가 붙어 있어, 내용이 창 바깥으로 그려지지 않습니다.
- `enableVerticalScroll`은 기본으로 켜져 있고, `enableHorizontalScroll`은 기본으로 꺼져 있습니다.
- 내용이 줄어들 수 있는 동안에는 스크롤바를 만들지 않고, `ScrollContent`의 최소 요구 크기보다 `Viewport`가 작아질 때만 해당 방향 스크롤바가 나타납니다.
- 최소 요구 크기는 `LayoutElement.minWidth/minHeight`, LayoutGroup 계열의 min 값, 또는 `UGUIWindowContent.minimumContentSize`로 표현합니다.

## 같은 창을 여러 개 띄우기

기본은 타입당 하나(풀링)지만, `allowMultipleInstance = true`로 설정하면 호출할 때마다 새 인스턴스가 생성됩니다. 이 경우 오브젝트 풀링은 자동으로 비활성화됩니다.

샘플의 `UGUIWindowMultipleInstanceSample`이 이 용도의 예시입니다. `UGUIDesktop`이 시작 시 같은 타입을 위치만 바꿔 여러 번 생성합니다.

## 다음으로

- 헤더·보더·이동·리사이즈 등 창 옵션 → [04. 창 옵션 설정](04-window-options.md)
- 프리팹 골격을 쉽게 만드는 에디터 도구 → [05. 에디터 도구](05-editor-tools.md)
</content>
