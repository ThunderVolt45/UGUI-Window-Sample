# 06. 이벤트와 라이프사이클

> [← 매뉴얼 목차로](../Manual.md)

이 장은 창의 생애주기와, 그 분기마다 발생하는 이벤트를 다룹니다. 사용법과 함께 매니저가 내부적으로 어떻게 반응하는지도 설명합니다.

**목차**

- [창 이벤트 (UnityEvent)](#창-이벤트-unityevent)
- [라이프사이클 흐름](#라이프사이클-흐름)
- [DPI 변경 이벤트](#dpi-변경-이벤트)
- [입력 (Input System)](#입력-input-system)

---

## 창 이벤트 (UnityEvent)

`UGUIWindow`는 네 개의 `UnityEvent<UGUIWindow>`를 노출합니다. 인스펙터에서 연결하거나 코드로 구독할 수 있습니다.

| 이벤트 | 발생 시점 |
| --- | --- |
| `OnOpenWindow` | `Open()` 호출 시 |
| `OnCloseWindow` | `Close()` 시작 시(Fade 아웃 직전) |
| `OnFocusWindow` | `Focus()` 또는 창/헤더/보더/본문 클릭 시 |
| `OnMinimizeWindow` | `Minimize()` 시 |

```csharp
window.OnOpenWindow.AddListener(w => Debug.Log($"{w.name} 열림"));
window.OnCloseWindow.AddListener(w => SaveLayout(w));
```

> 매니저는 창을 생성할 때 이 네 이벤트를 **자동 구독**해 z-순서와 풀 이동을 처리합니다(아래 참고). 사용자 리스너는 그 위에 추가됩니다.

## 라이프사이클 흐름

```mermaid
sequenceDiagram
    participant App as 호출 코드
    participant Mgr as UGUIWindowManager
    participant Win as UGUIWindow
    participant View as UGUIWindowView

    App->>Mgr: CreateWindow&lt;T&gt;()
    Mgr->>Mgr: 풀 확인 / 프리팹 인스턴스화
    Mgr->>Win: 이벤트 구독 + Open()
    Win->>View: SetActive(true)
    Win-->>Mgr: OnOpenWindow
    Mgr->>Mgr: 최상단 이동, z-순서 말단 등록
    Win->>View: OnEnable → Fade 인트로

    Note over App,View: 사용자 상호작용(이동/리사이즈/포커스)

    App->>Win: Close()
    Win-->>Mgr: OnCloseWindow
    Win->>View: Fade 아웃(await)
    View->>View: SetActive(false)
    alt 풀링 사용
        Mgr->>Mgr: 비활성 풀로 이동(보관)
    else 풀링 미사용 / 다중 인스턴스
        Win->>Win: Destroy(gameObject)
    end
```

### 열기 (Open)

`Open()`은 창을 활성화하고 `OnOpenWindow`를 발생시킵니다. 매니저는 이를 받아 창을 메인 캔버스로 옮기고 최상단(`SetAsLastSibling`)에 두며, z-순서 리스트 말단으로 이동시킵니다. 활성화 직후 `OnEnable`에서 **Fade 인트로**(알파 0→1, 스케일 0.9→1)가 재생됩니다.

### 포커스 (Focus)

창 본체·헤더·보더·엣지·본문 중 어디를 클릭해도 `Focus()`가 호출됩니다(각 컴포넌트가 `IPointerDownHandler`로 구현). 매니저는 해당 창을 최상단으로 올리고 z-순서 말단으로 옮깁니다.

### 최소화 (Minimize)

`Minimize()`는 모드를 `Minimized`로 바꾸고 `OnMinimizeWindow`를 발생시킵니다. 매니저는 창을 열린 목록에서 빼고 **최소화 풀**로 이동시킵니다(파괴하지 않음).

### 닫기 (Close)

`Close()`는 비동기 메서드입니다.

1. `OnCloseWindow` 발생
2. **Fade 아웃**(알파 1→0, 스케일 1→0.9)을 `await`
3. 비활성화(`SetActive(false)`)
4. 풀링을 쓰지 않거나 다중 인스턴스 창이면 `Destroy`, 아니면 비활성 풀로 이동해 보관

> **왜 닫아도 파괴하지 않나?** [02장 오브젝트 풀링](02-concepts.md)에서 설명했듯, 재사용으로 생성 비용과 GC를 줄이기 위함입니다. 실제 파괴는 `Application.lowMemory` 시 `TrimWindow()`가 담당합니다.

## DPI 변경 이벤트

매니저의 `OnDPIChanged(width, height, dpi)`는 DPI/해상도가 바뀔 때 발생합니다. 데스크톱 캔버스처럼 매니저 밖의 UI가 스케일을 맞춰야 할 때 구독합니다. 샘플의 `UGUIDesktop.OnDPIChange`가 이 이벤트에 연결되어 자신의 `CanvasScaler`를 갱신합니다.

## 입력 (Input System)

- **Cancel(ESC)** → 매니저의 `OnCancel`: 열린 창이 있으면 최상단 창을 닫고, 없으면 `defaultWindowOnEscape` 창을 생성합니다.
- **Point(마우스 이동)** → `UGUICursorManager.OnPoint`: 커서 아래가 보더/엣지면 방향에 맞는 리사이즈 커서로 바꿉니다.

이들은 `PlayerInput`의 메시지(`OnCancel`/`OnPoint`)로 호출됩니다.

## 다음으로

- 이 모든 것이 실제로 엮인 데모 → [07. 샘플 살펴보기](07-samples.md)
</content>
