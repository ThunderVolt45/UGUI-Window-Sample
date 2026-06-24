# 05. 에디터 도구

> [← 매뉴얼 목차로](../Manual.md)

창 프리팹 제작을 돕는 에디터 확장입니다. 모두 플레이 모드가 아닌 **에디터 타임**에 동작합니다.

**목차**

- [메뉴: GameObject ▸ UGUI Window](#메뉴-gameobject--ugui-window)
- [인스펙터 버튼 (UGUIWindowView)](#인스펙터-버튼-uguiwindowview)
- [내부 헬퍼: UGUIWindowHelper](#내부-헬퍼-uguiwindowhelper)
- [권장 워크플로](#권장-워크플로)

---

## 메뉴: GameObject ▸ UGUI Window

`UGUIEditorMenu`가 두 개의 메뉴 항목을 제공합니다.

### Create Window Manager

```
GameObject ▸ UGUI Window ▸ Create Window Manager
```

`Resources/UGUIWindowManager.prefab`을 씬에 배치합니다. 씬에 매니저가 이미 있으면 경고만 띄우고 생성하지 않습니다. 되돌리기(Undo)에 등록되어 Ctrl+Z로 취소할 수 있습니다.

### Create Window Templete

```
GameObject ▸ UGUI Window ▸ Create Window Templete
```

`UGUIWindowView` + `UGUIWindow` 컴포넌트를 가진 빈 창 골격(`200×200`)을 만듭니다. 선택 중인 오브젝트가 있으면 그 자식으로 배치됩니다. 커스텀 창을 새로 만들 때 출발점으로 사용하세요.

> 메뉴 이름의 "Templete"는 코드상의 철자 그대로입니다(`CreateWindowTemplete`).

## 인스펙터 버튼 (UGUIWindowView)

`UGUIWindowView`를 선택하면 커스텀 인스펙터(`UGUIWindowViewEditor`)가 두 개의 버튼을 추가로 보여줍니다.

### Auto find Base Components

오브젝트의 하위 트랜스폼을 **재귀 순회**해 `UGUIWindowHeader`, `UGUIWindowBorder`, `UGUIWindowEdge`를 찾아 `windowHeader` / `windowBorders` / `windowEdges` 필드에 자동 할당합니다. 이미 헤더/보더/엣지가 붙어 있는 프리팹의 참조를 다시 잡을 때 사용합니다.

### Create & Assignment Base Components

```
Resources/BaseComponents/Header.prefab
Resources/BaseComponents/Borders.prefab
Resources/BaseComponents/Edges.prefab
```

위 기본 컴포넌트 프리팹들을 창 하위에 인스턴스화한 뒤, 곧바로 *Auto find*를 실행해 참조까지 연결합니다. 빈 창 템플릿에 헤더·보더·엣지를 한 번에 갖추는 가장 빠른 방법입니다.

> 프리팹 로드에 실패하면 경로를 포함한 에러 로그가 출력됩니다. 프로젝트의 `Assets/UGUIWindowSample/Resources/BaseComponents/` 경로가 유지되어야 합니다.

## 내부 헬퍼: UGUIWindowHelper

`UGUIWindowViewEditor`가 리스트(배열) 필드를 직렬화해 저장할 때 `UGUIWindowHelper.SetSerializedArray`를 사용합니다. 이는 에디터 스크립트로 프리팹을 수정한 변경분이 제대로 저장되도록 `SerializedProperty`를 통해 배열을 채우는 범용 헬퍼입니다. 일반 사용 시 직접 호출할 일은 없습니다.

## 권장 워크플로

1. `Create Window Templete`로 빈 창 생성
2. 인스펙터에서 `Create & Assignment Base Components`로 헤더·보더·엣지 부착
3. `UGUIWindow`의 옵션([04장](04-window-options.md)) 설정
4. 본문 UI 구성 후 `Assets/Resources/Windows/{클래스명}.prefab`으로 저장([03장](03-creating-windows.md))

## 다음으로

- 창 동작에 코드로 반응하기 → [06. 이벤트와 라이프사이클](06-events-lifecycle.md)
</content>
