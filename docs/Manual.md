# UGUI Window Sample — 매뉴얼

`UGUI-Window-Sample`은 Unity의 UGUI만으로 데스크톱 OS 스타일의 **창(Window) 기반 UI 시스템**을 구현한 프로젝트입니다. 이 매뉴얼은 두 가지 목적을 함께 다룹니다.

- **사용 가이드** — 이 시스템을 직접 다루거나 자신의 프로젝트에 가져다 쓰는 방법
- **설계 설명** — 각 기능이 *왜* 그렇게 동작하는지, 내부 구조와 의도

> 클래스 단위의 구조 레퍼런스는 [클래스 다이어그램 문서](ClassDiagram.md)를 참고하세요. 이 매뉴얼은 "어떻게 쓰고 왜 그런가"에 집중합니다.

## 목차

| 챕터 | 내용 |
| --- | --- |
| [01. 시작하기](manual/01-getting-started.md) | 환경, Window Manager 배치, 첫 창 띄우기 |
| [02. 핵심 개념과 아키텍처](manual/02-concepts.md) | Window/View/Manager 3분할, 풀링, z-순서, DPI, 싱글톤 |
| [03. 창 만들기](manual/03-creating-windows.md) | `CreateWindow` API, 커스텀 창 정의, 리소스 규약 |
| [04. 창 옵션 설정](manual/04-window-options.md) | 헤더·보더·버튼, 이동/리사이즈, 최소 크기, 창 모드 |
| [05. 에디터 도구](manual/05-editor-tools.md) | 메뉴 항목, 인스펙터 자동 할당/생성 버튼 |
| [06. 이벤트와 라이프사이클](manual/06-events-lifecycle.md) | 열기/닫기/포커스/최소화 이벤트, Fade, 풀링 흐름 |
| [07. 샘플 살펴보기](manual/07-samples.md) | 데모 씬(데스크톱/아이콘/설정 창) 구조 |
| [08. API 레퍼런스](manual/08-api-reference.md) | 주요 public API 요약 |

## 한눈에 보기

```csharp
// 가장 단순한 사용: 타입으로 창을 생성하면 끝.
UGUIWindowManager.CreateWindow<UGUIMenu>();

// 위치·크기를 지정한 생성
UGUIWindowManager.CreateWindowEx<UGUIMenu>("내 메뉴", x: 0, y: 0, width: 300, height: 400);
```

매니저는 씬에 없으면 자동으로 생성되고, 창 프리팹은 이름 규약(`Resources/Windows/{클래스명}.prefab`)으로 자동 로드됩니다. 자세한 내용은 [03. 창 만들기](manual/03-creating-windows.md)를 참고하세요.

## 프로젝트 환경

- **Unity 6000.2.6f2** (Unity 6)
- **Input System** 패키지 사용 (ESC로 최상단 창 닫기, 마우스 위치 기반 리사이즈 커서)
- **TextMeshPro** 사용 (창 타이틀, 설정 드롭다운 등)
</content>
