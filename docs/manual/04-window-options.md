# 04. 창 옵션 설정

> [← 매뉴얼 목차로](../Manual.md)

`UGUIWindow` 컴포넌트의 인스펙터 필드로 창의 외형과 동작을 켜고 끕니다. 대부분 런타임에 프로퍼티로도 바꿀 수 있습니다.

**목차**

- [인스펙터 필드 한눈에 보기](#인스펙터-필드-한눈에-보기)
- [헤더 / 보더 / 버튼](#헤더--보더--버튼)
- [이동 / 리사이즈](#이동--리사이즈)
- [창 모드: Windowed / Maximized / Minimized](#창-모드-windowed--maximized--minimized)

---

## 인스펙터 필드 한눈에 보기

| 필드 | 타입 | 의미 |
| --- | --- | --- |
| **Window Mode** | `UGUIWindowMode` | 시작 모드: `Windowed` / `Maximized` / `Minimized` |
| **Allow Multiple Instance** | `bool` | 중복 생성 허용(켜면 오브젝트 풀링 비활성) |
| **Use Object Pooling** | `bool` | 닫은 창을 재사용(중복 생성 허용 시 무시됨) |
| **Has Header** | `bool` | 헤더(타이틀 바) 보유 |
| **Has Border** | `bool` | 경계(리사이즈용 보더/엣지) 보유 |
| **Has Exit Button** | `bool` | 닫기 버튼 — *헤더가 있을 때만* 의미 있음 |
| **Has Maximize Button** | `bool` | 최대화/복원 버튼 — *헤더가 있고 리사이즈 가능할 때만* |
| **Is Movable** | `bool` | 헤더 드래그로 이동 가능 |
| **Is Resizable** | `bool` | 보더/엣지 드래그로 크기 조절 가능 |
| **Minimum Window Size** | `Vector2` | 리사이즈 시 최소 크기 |
| **Window Events** | `UnityEvent` | 열기/닫기/포커스/최소화 콜백 ([06장](06-events-lifecycle.md)) |

> **에디터에서 즉시 반영** — `Has Header`/`Has Border`/버튼 토글은 `OnValidate`를 통해 플레이 중이 아니어도 인스펙터에서 바로 보입니다. 내부적으로 이전 값과 비교해 변경분만 View에 적용합니다.

## 헤더 / 보더 / 버튼

- **헤더**(`HasHeader`)는 타이틀과 닫기·최대화·최소화 버튼을 담는 바입니다. 헤더가 없으면 종료/최대화 버튼 설정은 효과가 없습니다.
- **보더**(`HasBorder`)는 4개의 변(Border)과 4개의 모서리(Edge)를 통칭합니다. 이들이 리사이즈 입력을 받습니다.
- 헤더와 보더가 겹치는 영역(상단 변, 상단 모서리)은 `UGUIWindowView.ResolveBorderEdgeOverlap()`이 자동으로 정리해, 헤더가 있으면 겹치는 북쪽 보더/모서리를 끕니다.

런타임 토글 예시:

```csharp
window.HasHeader = true;       // 헤더 표시
window.HasBorder = false;      // 경계 숨김
window.HasMaximizeButton = true;
```

## 이동 / 리사이즈

- **이동**(`isMovable`) — 헤더를 드래그하면 창이 따라옵니다. 헤더가 없으면 이동 수단이 없습니다.
- **리사이즈**(`isResizable`) — 보더(한 축)/엣지(두 축)를 드래그해 크기를 조절합니다. `minimumWindowSize` 미만으로는 줄어들지 않습니다.

드래그 이동/리사이즈는 DPI 보정(`ScreenMultiplier`)이 적용되어 해상도와 무관하게 일관되게 움직입니다([02장](02-concepts.md) 참고).

코드로 직접 옮기거나 크기를 바꿀 수도 있습니다.

```csharp
window.Move(100, -50);              // anchoredPosition 설정
window.Resize(400, 300);           // sizeDelta 설정
window.SetAnchor(Vector2.zero, Vector2.one); // 앵커 설정
```

이 세 메서드는 호출 후 자동으로 `MemorizeLastWindowState()`를 불러 "마지막 상태"를 갱신합니다.

## 창 모드: Windowed / Maximized / Minimized

`WindowMode` 프로퍼티로 모드를 바꾸면 적절한 동작으로 분기됩니다.

```csharp
window.WindowMode = UGUIWindowMode.Maximized; // 최대화
window.WindowMode = UGUIWindowMode.Windowed;  // 복원
window.WindowMode = UGUIWindowMode.Minimized; // 최소화
```

| 모드 | 동작 |
| --- | --- |
| **Maximized** | 화면을 가득 채움. 최대화 직전 상태를 저장하고, 이동/보더를 일시 비활성화 |
| **Windowed** | 저장해 둔 상태(`UGUIWindowState`)로 위치·크기·옵션 복원 |
| **Minimized** | 최소화 풀로 이동하고 `OnMinimizeWindow` 발생 |

> **주의** — `Maximize`/`RestoreWindow`는 `isResizable`이 꺼진 창에서는 동작하지 않고 에러 로그를 남깁니다(크기를 못 바꾸는 창은 최대화 개념이 없으므로).

헤더의 **최대화 버튼**과 **헤더 더블클릭** 모두 최대화↔복원을 토글합니다.

## 다음으로

- 프리팹에 헤더/보더를 손쉽게 붙이기 → [05. 에디터 도구](05-editor-tools.md)
- 이벤트로 창 동작에 반응하기 → [06. 이벤트와 라이프사이클](06-events-lifecycle.md)
</content>
