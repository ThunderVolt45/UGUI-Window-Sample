# 07. 샘플 살펴보기

> [← 매뉴얼 목차로](../Manual.md)

샘플 씬은 창 시스템으로 만든 **데스크톱 OS 메타포** 데모입니다.

```
Assets/UGUIWindowSample/Scenes/UGUIWindowSampleScene.unity
```

구성 클래스의 상세 구조는 [클래스 다이어그램: Sample](../class-diagram/Sample.md)을 참고하세요. 여기서는 "무엇을 보여주는 데모인지"에 집중합니다.

**목차**

- [등장 요소](#등장-요소)
- [데스크톱과 아이콘](#데스크톱과-아이콘)
- [메뉴 창 (UGUIMenu)](#메뉴-창-uguimenu)
- [설정 창 (UGUIApplicationSetting)](#설정-창-uguiapplicationsetting)
- [창 전환 오버레이](#창-전환-오버레이)
- [데모로 확인할 수 있는 것](#데모로-확인할-수-있는-것)

---

## 등장 요소

| 클래스 | 역할 |
| --- | --- |
| `UGUIDesktop` | 바탕화면. 시작 시 데모 창을 띄우고 아이콘을 관리 |
| `UGUIIcon` | 바탕화면 아이콘. 대상 창 아이콘을 표시하고 더블클릭으로 창 생성 |
| `UGUIMenu` | 시작/메뉴 창. 설정 열기·종료 버튼 |
| `UGUIApplicationSetting` | 설정 창. 해상도·프레임레이트·창 모드·DPI 변경 |
| `UGUIWindowMultipleInstanceSample` | 다중 인스턴스/풀링 시연용 빈 창 |
| `UGUIWindowSwitcher` | 활성/최소화 창을 최근 포커스 순서로 보여주는 창 전환 오버레이 |

## 데스크톱과 아이콘

`UGUIDesktop`은 `Start`에서 하위 트랜스폼을 재귀 순회해 모든 `UGUIIcon`을 수집하고, 데모 창 몇 개를 생성합니다.

```csharp
// UGUIDesktop.Start() 발췌
UGUIWindowManager.CreateWindow<UGUIWindow>();
UGUIWindowManager.CreateWindowEx<UGUIWindowMultipleInstanceSample>(null, -200, 0, 250, 250);
UGUIWindowManager.CreateWindowEx<UGUIWindowMultipleInstanceSample>("MultipleInstanceSample", -150, 50, 250, 250);
UGUIWindowManager.CreateWindowEx<UGUIWindowMultipleInstanceSample>("MultipleInstanceSample", -100, 100, 250, 250);
```

- 같은 타입(`UGUIWindowMultipleInstanceSample`)을 위치만 바꿔 **세 번** 생성합니다 → 다중 인스턴스 동작 확인.
- 바탕화면의 빈 곳을 클릭하면 모든 아이콘의 선택(포커스)이 해제됩니다.

`UGUIIcon`은 `targetClassName`에 맞는 `Resources/Windows/{ClassName}` 프리팹에서 `WindowIcon`을 읽어 바탕화면 아이콘에 표시합니다. 또한 **더블클릭**을 직접 판정합니다(`doubleClickThreshold` 내 연속 클릭). 더블클릭하면 `targetClassName` 문자열로 타입을 찾아 창을 엽니다.

```csharp
// 문자열 → 타입 → 창 생성
Type targetWindowType = Type.GetType($"UGUIWindow.{targetClassName}", true);
UGUIWindowManager.CreateWindow(targetWindowType);
```

> 이 패턴 덕분에 아이콘마다 코드를 따로 작성하지 않고, 인스펙터에서 `targetClassName`만 지정하면 원하는 창을 띄울 수 있습니다.

## 메뉴 창 (UGUIMenu)

`UGUIWindow`를 상속한 간단한 창입니다.

- **설정 버튼** → `UGUIApplicationSetting` 창을 열고 자신은 닫습니다.
- **종료 버튼** → 에디터/웹/스탠드얼론 각각에 맞게 애플리케이션을 종료합니다.

## 설정 창 (UGUIApplicationSetting)

가장 실전적인 커스텀 창 예시로, `Awake`/`OnEnable`을 `override`하며 `base`를 호출하는 패턴([03장](03-creating-windows.md))을 보여줍니다.

- **드롭다운 구성** — 화면 모드(전체화면/테두리 없는 창/창모드), 지원 해상도, 프레임레이트, DPI 목록을 동적으로 채웁니다. 최소 해상도·최소 FPS·세로 화면비 같은 조건으로 옵션을 걸러냅니다.
- **현재 설정 감지** — 열릴 때마다 `OnEnable`에서 현재 해상도/모드/DPI를 읽어 드롭다운을 선택 상태로 맞춥니다.
- **적용** — `Screen.SetResolution`과 `UGUIWindowManager.SetDPI`를 호출해 해상도와 DPI를 동시에 반영합니다.

## 창 전환 오버레이

`UGUIWindowSwitcher`는 `UGUIDesktop` 시작 시 런타임 UI로 생성됩니다. 브라우저·운영체제 환경에서 Alt+Tab 입력을 보장하기 어렵기 때문에 기본 입력은 **Ctrl + Backquote**입니다.

- `Ctrl + Backquote` → 전환 시작/다음 창 선택
- `Ctrl + Shift + Backquote` → 반대 방향 선택
- Ctrl 해제 → 선택된 창으로 전환
- `Esc` → 전환 취소

오버레이는 전용 `Canvas`의 최상단 정렬로 다른 창보다 앞에 표시되며, 반투명 배경 위에 창 아이콘들을 `HorizontalLayoutGroup`으로 배치하고, 현재 선택된 창의 제목을 표시합니다. 후보는 `UGUIWindowManager.GetSwitchableWindows()`에서 받아오므로 닫힌 창은 제외되고, 최소화된 창은 포함되며, 최근 포커스된 순서가 유지됩니다.

## 데모로 확인할 수 있는 것

- 창 생성/풀링(같은 창을 닫았다 다시 열기)
- 헤더 드래그 이동, 보더/엣지 드래그 리사이즈
- 기본 `UGUIWindow`의 본문 마스킹과 세로 스크롤(내용 최소 크기보다 작게 리사이즈)
- 최대화/복원(버튼·더블클릭), 최소화
- 포커스에 따른 z-순서 변화(클릭한 창이 최상단)
- 창 전환 오버레이를 통한 활성/최소화 창 순환
- DPI 변경 시 전체 UI 스케일 일괄 조정
- ESC로 최상단 창 닫기

## 다음으로

- 코드에서 바로 찾아볼 수 있는 API 요약 → [08. API 레퍼런스](08-api-reference.md)
</content>
