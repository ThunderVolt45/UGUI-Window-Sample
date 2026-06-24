# 01. 시작하기

> [← 매뉴얼 목차로](../Manual.md)

이 장에서는 빈 씬에서 창 시스템을 켜고 첫 창을 띄우기까지를 다룹니다.

**목차**

- [사전 요구사항](#사전-요구사항)
- [1단계 — Window Manager 배치](#1단계--window-manager-배치)
- [2단계 — 첫 창 띄우기](#2단계--첫-창-띄우기)
- [3단계 — 동작 확인](#3단계--동작-확인)

---

## 사전 요구사항

- **Unity 6000.2.6f2** 이상 (Unity 6 권장)
- 패키지: **Input System**, **TextMeshPro** (프로젝트에 이미 포함됨)

가장 빠르게 동작을 확인하려면 샘플 씬을 여세요.

```
Assets/UGUIWindowSample/Scenes/UGUIWindowSampleScene.unity
```

재생하면 데스크톱 + 여러 창이 떠 있는 데모가 실행됩니다. 자세한 구조는 [07. 샘플 살펴보기](07-samples.md)를 참고하세요.

## 1단계 — Window Manager 배치

창 시스템의 모든 동작은 `UGUIWindowManager` 싱글톤을 통합니다. 씬에 매니저를 두는 방법은 두 가지입니다.

**방법 A. 메뉴로 명시적 생성 (권장)**

상단 메뉴에서

```
GameObject ▸ UGUI Window ▸ Create Window Manager
```

를 선택하면 `Resources/UGUIWindowManager.prefab`이 씬에 배치됩니다. 이 프리팹에는 메인 캔버스, 최소화/비활성 오브젝트 풀, DPI 설정 등이 미리 연결되어 있습니다.

> 씬에 매니저가 이미 있으면 경고만 띄우고 중복 생성하지 않습니다.

**방법 B. 자동 생성에 맡기기**

매니저를 직접 두지 않아도, 코드에서 `UGUIWindowManager.Instance`(또는 `CreateWindow`)가 처음 호출될 때 프리팹을 자동으로 로드해 생성합니다. 빠른 프로토타이핑에는 편하지만, 캔버스/풀 참조를 커스터마이즈하려면 방법 A로 직접 배치하는 편이 낫습니다.

## 2단계 — 첫 창 띄우기

아무 `MonoBehaviour`에서나 정적 메서드 한 줄로 창을 생성합니다.

```csharp
using UGUIWindow;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    private void Start()
    {
        // 기본 제공 창(UGUIWindow)을 띄운다.
        UGUIWindowManager.CreateWindow<UGUIWindow>();
    }
}
```

이때 매니저는 `Resources/Windows/UGUIWindow.prefab`을 로드해 메인 캔버스에 인스턴스화하고 최상단에 표시합니다. 프리팹 이름이 **타입 이름과 일치**해야 한다는 점이 핵심 규약입니다(자세히는 [03. 창 만들기](03-creating-windows.md)).

## 3단계 — 동작 확인

재생 후 다음을 확인해 보세요.

- 창의 **헤더를 드래그**하면 이동합니다(이동 가능 설정 시).
- 창의 **모서리/변을 드래그**하면 크기가 바뀝니다(리사이즈 가능 설정 시).
- 헤더 우측 **버튼**으로 닫기/최대화/최소화가 됩니다.
- **ESC** 키를 누르면 최상단 창이 닫힙니다(Input System의 Cancel 액션).

각 동작을 켜고 끄는 설정은 [04. 창 옵션 설정](04-window-options.md)에서 다룹니다.

## 다음으로

- 시스템이 내부적으로 어떻게 구성되는지 → [02. 핵심 개념과 아키텍처](02-concepts.md)
- 나만의 창을 만들고 싶다면 → [03. 창 만들기](03-creating-windows.md)
</content>
