# 커밋 메시지 규약

이 프로젝트의 커밋 메시지는 읽기 쉽고 짧게 유지합니다. 기본 형식은 아래처럼 씁니다.

```text
type(scope): 한글 요약
```

- `type`은 변경의 성격을 나타냅니다.
- `scope`는 선택 사항이며 변경 대상이 분명할 때만 씁니다.
- `한글 요약`은 한 줄로, 무엇이 달라졌는지 현재형으로 씁니다.

커밋 메시지 제목과 본문은 한글로 작성합니다. 단, `type`, `scope`, 코드 식별자, 클래스명, 파일명, API 이름은 영어 원문을 그대로 써도 됩니다.

## 타입

| Type | 의미 |
| --- | --- |
| `feat` | 사용자에게 보이는 기능 추가 |
| `fix` | 버그 수정 |
| `docs` | 문서 변경 |
| `refactor` | 동작 변화 없는 코드 구조 개선 |
| `asset` | 프리팹, 씬, 스프라이트 등 Unity 에셋 변경 |
| `sample` | 데모 씬이나 Sample 폴더 전용 변경 |
| `chore` | 설정, 정리, 빌드에 직접 영향이 작은 관리 작업 |
| `ci` | GitHub Actions 등 CI/CD 변경 |

## Scope 예시

자주 쓰는 scope는 다음 정도로 제한합니다.

```text
window
manager
view
editor
desktop
dpi
docs
ci
```

scope가 애매하면 생략합니다.

## 예시

```text
feat(window): 최소화 복원 이벤트 추가
fix(manager): 포커스한 창을 최상단에 유지
asset(desktop): 샘플 작업 표시줄 프리팹 갱신
sample(desktop): 기본 데모 창 생성 흐름 추가
docs(api): CreateWindow 프리팹 이름 규약 설명 보강
ci: 풀 리퀘스트에서 Windows 플레이어 빌드
chore: Unity 프로젝트 설정 갱신
```

## 작은 규칙

- 한 커밋은 하나의 목적만 담습니다.
- 제목은 가능하면 72자 안팎으로 짧게 씁니다.
- Unity 에셋을 추가하거나 옮길 때는 `.meta` 파일을 함께 커밋합니다.
- public API나 프리팹 이름 규약을 깨는 변경은 `!`를 붙입니다.

```text
feat(manager)!: 창 프리팹 로드 경로 변경
```

필요한 설명이 길어지면 제목 아래에 빈 줄을 두고 본문을 추가합니다.
