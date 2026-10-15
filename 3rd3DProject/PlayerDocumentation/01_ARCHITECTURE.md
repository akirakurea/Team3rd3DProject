# 전체 구조와 읽는 방법

## 이 문서가 설명하는 현재 상태

고양이 플레이어는 카메라 기준 이동, 걷기·달리기·점프, 물건 운반, F 획득·적 포획 연결, 샷건 조준·사격, 5칸 인벤토리와 덫 설치를 함께 사용합니다. 이 문서는 이 대화에서 만들거나 수정·연결한 기능을 중심으로, **현재 파일의 구현과 저장된 씬 연결**을 설명합니다. 팀원이 만든 GameManager·Hotbar·Enemy·덫 원본은 재사용 경계를 설명하며 모두 Codex가 처음 만든 코드라고 표시하지 않습니다.

기준 날짜는 **2026-10-14, 한국 시간**입니다. 기준 Git 커밋은 `6b8bafde9f16129d6c6a9a7c7127aaca5582a63a`입니다. 문서 작성 시작 시 작업 폴더의 추적 파일 변경은 없었습니다. 문서 작성 때문에 Unity 씬을 저장하거나 게임 코드를 바꾸지 않았습니다. Unity 편집기에만 남아 있는 미저장 내용은 이 디스크 기준 문서와 다를 수 있습니다.

| 항목 | 확인한 값 | 근거 |
|---|---|---|
| 프로젝트 | `C:/Users/307/Desktop/unity/Team3rd3DProject/3rd3DProject` | 실제 Git 작업 폴더 |
| 대상 씬 | `Assets/01Scenes/PlaytestScene01.unity` | 현재 저장된 씬 |
| Unity | 6000.5.6f1 | ProjectSettings/ProjectVersion.txt |
| 카메라 | Cinemachine 3.1.7 | Packages/manifest.json |
| 입력 | Input System 1.20.0 | Packages/manifest.json |
| 화면 출력 | URP 17.5.0, uGUI 2.5.0 | Packages/manifest.json |
| 상세 조사 코드 | Player C# 53개 + 직접 연결된 Systems C# 10개 | [스크립트 색인](07_SCRIPT_INDEX.md) |

코드 안의 기본값은 새 컴포넌트를 만들 때의 출발값입니다. 기존 씬이나 프리팹에 이미 저장한 값이 있으면 그 값이 우선합니다. 따라서 문서는 **코드 기본값**, **저장된 씬 값**, **과거 시험 결과**, **현재 미확인 사항**을 나누어 표시합니다.

## 문서 형식을 고른 이유

모든 프로젝트에 하나의 “가장 올바른 양식”이 있는 것은 아닙니다. 이 프로젝트는 개발자가 바뀌어도 코드를 찾아 고칠 수 있어야 하므로 다음 공식 지침의 목적을 조합했습니다. 양식의 원문을 그대로 복제한 것이 아니라 프로젝트에 맞게 구성한 문서입니다.

- **읽는 목적 구분:** 처음 배우기, 특정 작업 수행, 정확한 참고 자료, 원리 이해를 분리합니다. 이 문서의 빠른 시작·수정 절차·스크립트 색인·구조 설명에 대응합니다. [Diátaxis](https://diataxis.fr/)
- **설계 정보 추적:** 목표와 제약, 구성요소, 실행 흐름, 결정 이유, 품질·위험·용어를 남깁니다. 각 기능의 담당 파일과 검증 근거를 연결한 이유입니다. [arc42 개요](https://arc42.org/overview/)
- **초보자에게 필요한 순서:** 범위와 독자를 먼저 정하고 핵심을 앞에 제시한 뒤 설명을 이어 갑니다. 코드 문법보다 실제 행동을 먼저 설명합니다. [Google 기술문서 작성 지침](https://developers.google.com/tech-writing/one/documents)

읽는 분은 Unity에서 오브젝트를 선택하고 Inspector를 볼 수 있는 초급 개발자를 가정합니다. C#을 처음 보더라도 아래 용어표와 짧은 코드 설명을 함께 읽으면 어느 파일을 고칠지 찾을 수 있게 구성했습니다.

## 13개 구현 항목

| ID | 구현 항목 | 설명·코드리뷰 |
|---|---|---|
| F01 | 키보드·마우스 입력과 게임 조작 권한 | [이동과 카메라](02_MOTION_CAMERA.md) |
| F02 | 이동, 작은 턱, 기본 물리와 마찰 | [이동과 카메라](02_MOTION_CAMERA.md) |
| F03 | 3인칭 카메라, 정지 회전 제한, 커서 잠금 | [이동과 카메라](02_MOTION_CAMERA.md) |
| F04 | Animator, 걷기·달리기·대기·점프와 사격 연결 | [이동과 카메라](02_MOTION_CAMERA.md) |
| F05 | 좌클릭 유지로 들고 놓기 | [상호작용과 사격](03_INTERACTION_COMBAT.md) |
| F06 | F 인벤토리 획득과 Enemy 포획 연결 | [상호작용과 사격](03_INTERACTION_COMBAT.md) |
| F07 | 중앙 커서, 노란 강조, 획득 이동 연출 | [상호작용과 사격](03_INTERACTION_COMBAT.md) |
| F08 | 샷건 장착, 줌, 4산탄 히트스캔, 총구 차단 | [상호작용과 사격](03_INTERACTION_COMBAT.md) |
| F09 | 5칸 인벤토리 데이터와 UI 재사용 | [인벤토리와 덫](04_INVENTORY_TRAPS.md) |
| F10 | 보이지 않는 격자 설치, 보상, 설치물 수명 | [인벤토리와 덫](04_INVENTORY_TRAPS.md) |
| F11 | 에디터 연결 도구와 검증기 | [에디터·에셋](05_EDITOR_ASSETS.md) |
| F12 | Git 충돌, GUID·메타 보존, 협업 | [에디터·에셋](05_EDITOR_ASSETS.md) |
| F13 | 모델, 리깅, 모션, 재질, Unity 에셋 전달 | [에디터·에셋](05_EDITOR_ASSETS.md) |

## 전체 호출 구조

```text
사용자 키보드와 마우스
        │
        ▼
GameplayInput ── 게임 상태 / 열린 메뉴 / 포커스 / 커서 잠금 확인
        │
        ├─ PlayerController
        │    ├─ MovementPolicy / JumpPolicy      언제 어떻게 움직일지 판단
        │    ├─ PlayerMovement / PlayerJump     Rigidbody와 실제 위치 연결
        │    └─ PlayerAnimation                 Animator 매개변수 전달
        │
        ├─ CameraCursorLock / CameraOrbitLimit / AimZoom
        │                                        잠금·회전 제한·시야 변화
        │
        ├─ InteractionController ─ ItemCarrier   손 운반
        │                      └─ ItemInteractor
        │                           ├─ ItemPickup             자료·획득 규칙 조회
        │                           ├─ HotbarStore ─ HotbarManager ─ HotbarUI
        │                           └─ ThiefController.GetCaptured   적 포획 요청
        │
        ├─ ShotgunEquipment / ShotgunCombat ─ HitscanQuery
        │                         └─ ShotgunPose / ShotSpreadRing
        │
        └─ TrapPlacement ─ PlacementInput / PlacementHold / PlacementSurface
                    ├─ PlacementFeedback        위치·시간 표시
                    ├─ HotbarManager            재고 소비
                    └─ PlacedTrapLifetime       라운드 종료 정리

GameManager의 라운드 결과 ─ TrapRoundReward ─ HotbarManager ─ HotbarUI
```

이 그림은 실행 순서를 한 줄로 강제한 것이 아니라 누가 누구에게 일을 맡기는지를 나타냅니다. 실제 시간별 실행은 입력을 읽는 `Update`, 물리를 갱신하는 `FixedUpdate`, 본·표시를 마무리하는 담당 코드로 나뉩니다.

### 폴더가 나뉜 이유

| 폴더 | 쉬운 설명 | 대표 파일 |
|---|---|---|
| Core | 필요한 담당자를 조립하는 곳 | PlayerController |
| Domain | Unity 밖에서도 설명할 수 있는 게임 규칙 | MovementPolicy, JumpPolicy, PlacementHold |
| Input | 실제 키보드·마우스를 읽는 곳 | PlayerInput, GameplayInput |
| Movement | 판단 결과를 물리에 연결 | PlayerMovement, StepSolver |
| Camera | 화면의 방향·잠금·줌 | CameraCursorLock, CameraOrbitLimit, AimZoom |
| Animation | 애니메이션 상태와 자세 전달 | PlayerAnimation, ShotgunPose |
| Equipment | 총 장착·발사·명중 | ShotgunEquipment, ShotgunCombat, HitscanQuery |
| Interaction | 집기·내려놓기와 설치 | InteractionController, ItemCarrier, TrapPlacement |
| Presentation | 보이는 피드백 | ItemHighlight, CenterCursor, PlacementFeedback |
| Editor | 개발자가 수동으로 사용하는 연결·검사 도구 | Setup, Validation |
| 04Systems | 기존 게임 시스템과 연결 | HotbarManager, GameManager, ItemInteractor, PauseMenuUI |

일반 C# 클래스는 Unity 오브젝트에 하나씩 붙이지 않아도 `new`로 만들어 사용할 수 있습니다. 따라서 스크립트 파일이 53개라는 것이 Player에 컴포넌트 53개가 붙었다는 뜻은 아닙니다. 인터페이스나 자료 구조 파일도 포함된 수입니다.

## 현재 조작 규칙

| 입력 | 조건 | 동작 |
|---|---|---|
| WASD | 게임 입력 허용 | 카메라 기준 이동·걷기 |
| Shift + WASD | 조준하지 않음 | 달리기 |
| 마우스 이동 | 입력 허용 | 3인칭 회전. 정지 시 멈춘 방향 기준 좌우 60도 제한, 이동 의도 있을 때 수평 제한 해제 |
| Space | 점프 사용 컨트롤러와 입력 허용 | 눌림→상승→낙하→착지. 착지 전 짧은 입력 보관 |
| 숫자 1 | 장비 입력 허용 | 기존 샷건 장착·해제 |
| 좌클릭 유지 | 빈손·운반 대상·설치 입력 미점유 | 들고, 떼면 현재 위치에서 기본 물리로 내려놓기 |
| 좌클릭 새 누름 | 샷건 장착·설치 입력 미점유 | 한 발사에 산탄 4개 |
| 우클릭 유지 | 샷건 장착 | 줌, 조준 중 걷기 제한 |
| F 새 누름 | 유효한 물건/적 대상 | 인벤토리 보관 또는 기존 Enemy 포획 경로 |
| 휠 | 인벤토리 선택 입력 허용 | 5칸 순환 선택 |
| 좌클릭/F 유지 | 선택한 덫·유효 바닥, F 대상 우선권 없음 | 같은 칸에 1초 유지 후 덫 1개 설치 |
| Esc | 게임/설정 UI 상태에 따라 | 설정 열기·닫기. 메뉴에서는 커서를 풀고 게임 복귀 시 잠금 복구 |

핵심은 **입력 허용과 행동 우선순위를 먼저 확인한다**는 점입니다. 샷건을 들었다고 언제나 좌클릭이 발사는 아닙니다. 덫이 선택되어 설치가 입력을 사용하면 설치가 우선합니다. 같은 입력을 여러 담당자가 동시에 실행하지 않게 작은 계약으로 조율합니다.

## 공통 권한과 UI 연결

`InputPermission`은 메뉴별 차단 요청을 집합으로 기억합니다. 설정창 A와 결과창 B가 모두 차단 중일 때 A만 닫아도 B의 차단은 남습니다. 한 개의 전역 bool을 아무 메뉴나 true/false로 바꾸는 방식보다 요청의 주인이 분명합니다.

`GameplayInput.IsEnabled`는 게임 상태뿐 아니라 시간 정지, 메뉴, 창 포커스, 커서 잠금, 해당 프레임 억제를 함께 확인합니다. `CameraCursorLock`이 실행 중 Cursor 값을 쓰는 담당자입니다. 메뉴는 커서를 직접 잠그지 않고 차단을 요청합니다. ESC로 메뉴를 닫는 누름이나 클릭이 동시에 사격·집기로 사용되지 않도록 입력 재개 시점을 제한합니다.

| 게임 상태 | 일반 게임 입력 정책 | 새 덫 설치 |
|---|---|---|
| Ready | 차단 | 차단 |
| Playing | 메뉴·포커스·잠금 등 추가 조건 통과 시 허용 | 조건 통과 시 허용 |
| RoundClear | 결과 전 대기 조작을 허용하는 정책 | 차단 |
| RoundResult / Win / Lose | 차단 | 차단 |
| GameManager 없는 독립 시험 | 다른 입력 조건에 따름 | 설치기의 독립 실행 경로 허용 |

### 설정창 코드의 역할

`PauseMenuUI`는 화면의 열림·닫힘, 슬라이더와 버튼 이벤트를 다룹니다. 실제 음량·해상도 반영은 `IGameSettingsApplier`를 통해 `GameSettingsApplier`에 요청합니다. `GameSettings`는 PlayerPrefs 저장값과 변환 계산을 담당합니다. PlayerPrefs는 이 프로젝트의 일반 게임 설정 저장이며 보관함 재고 저장이 아닙니다.

```csharp
Time.timeScale = 0f;
GameplayInput.SetBlocked(this, true);
panel.SetActive(true);
```

출처: [PauseMenuUI.Open](../Assets/02Scripts/04Systems/PauseMenuUI.cs). 게임 시간을 멈추고, 이 메뉴가 입력을 막고 있음을 등록한 뒤 패널을 켭니다. 닫을 때는 자기 차단 요청을 해제합니다. UI 클릭에는 EventSystem/InputSystemUIInputModule의 정상 연결도 필요합니다. 커서 표시만 고쳐도 UI 입력 모듈이 없으면 클릭이 되지 않는 이유입니다.

`GameSettingsApplier`는 전체 음량을 AudioListener에, 효과 음량을 연결된 AudioMixer 파라미터에 적용합니다. 믹서가 없으면 효과 음량의 실제 믹서 반영은 하지 않습니다. 해상도는 드래그 중 글자만 바꾸고 버튼을 놓을 때 적용합니다. 실제 창 크기 변화는 빌드 환경에서도 확인해야 합니다.

현재 `MouseSensitivity`는 설정 저장·화면 표시 경로만 검색되며 Player 카메라 입력에 곱하는 사용처는 확인되지 않았습니다. **감도 슬라이더 값 저장과 실제 회전 감도 적용을 같은 기능으로 설명하지 않습니다.** [코드 검토 목록](08_REVIEW_AND_VALIDATION.md)에 확장 시 확인할 항목으로 남겼습니다.

## 설계 결정 기록

| 결정 | 이유 | 지금 지켜야 할 조건 |
|---|---|---|
| 실제 움직임은 Rigidbody, 모양 변화는 본 애니메이션 | 점프 높이와 몸의 눌림이 서로 중복 이동하지 않게 함 | 애니메이션 Root Motion과 물리 이동 중복 확인 |
| Animator Controller가 전환, C#은 상태 전달 | Unity 창에서 상태·혼합 시간을 볼 수 있게 함 | 구형 Idle.controller 호환을 함부로 삭제하지 않기 |
| 게임 규칙 일부를 일반 C#으로 분리 | 테스트·이식이 쉬움 | Unity 어댑터와 순수 정책의 경계를 유지 |
| 입력과 커서 잠금에 공통 권한 사용 | 설정창에서 마우스가 사라지는 충돌 감소 | 메뉴마다 같은 owner로 등록·해제 |
| 기존 Hotbar 사용 | 재고가 두 군데에 나뉘는 문제 방지 | 저장 데이터와 UI 이벤트 연결 유지 |
| 설치 시작과 완료 시 조건 재검사 | 1초 동안 칸·장애물·재고가 바뀔 수 있음 | 재고 제거 성공 시에만 실물 유지 |
| 라운드 수명과 재고 분리 | 설치한 덫만 지우고 미사용 물건은 보존 | 프리팹 에셋에 Destroy 호출 금지 |
| 기존 메타·GUID 보존 | 팀원 PC의 Missing 참조 방지 | 파일과 meta를 짝으로 관리, 재발급으로 해결하지 않기 |

## 처음 읽을 때의 순서

1. 이 문서의 조작 규칙에서 실제 행동을 먼저 이해합니다.
2. 고칠 기능의 F번호를 고릅니다. 예를 들어 덫 시간은 F10입니다.
3. 해당 장에서 **담당 파일 → 인스펙터 값 → 코드 흐름 → 확인 사례** 순서로 읽습니다.
4. 이름을 모르면 [스크립트 색인](07_SCRIPT_INDEX.md)에서 파일명이나 클래스명을 찾습니다.
5. 왜 이렇게 바뀌었는지는 [작업 이력](06_HISTORY.md)과 원본 로그를 대조합니다.
6. 수정 전후에 [검토·검증 안내](08_REVIEW_AND_VALIDATION.md)의 해당 사례를 확인합니다.

## 쉬운 용어표

| 용어 | 뜻과 이 프로젝트의 예 |
|---|---|
| 클래스 | 한 가지 역할의 설계도. PlacementHold는 유지 시간 계산 담당 |
| 인스턴스 | 설계도로 실제 만든 물건. 바닥에 설치한 덫 한 개 |
| 컴포넌트 | Unity 오브젝트에 붙이는 기능. Rigidbody, TrapPlacement |
| 인터페이스 | 다른 담당자에게 요청할 수 있는 기능의 약속. IPrimaryAction |
| 이벤트 / 구독 | 일이 일어났다고 알림 / 그 알림을 듣기로 연결. OnSlotChanged |
| 직렬화 | 값을 파일에 저장할 수 있는 형태로 기록. Inspector의 참조와 수치 |
| 프리팹 | 여러 번 생성할 수 있는 오브젝트 원본 |
| 프리팹 변형 | 원본을 이어받고 차이만 저장한 에셋. Trap_Inventory |
| 오버라이드 | 이 씬의 인스턴스에서 원본과 다르게 저장한 값 |
| ScriptableObject | 오브젝트 행동 대신 공유할 자료를 저장하는 에셋. TrapItem |
| GUID | Unity가 에셋을 찾을 때 쓰는 고유 식별자. 파일 이름과 다름 |
| meta | GUID와 가져오기 설정이 들어 있는 짝 파일 |
| Collider | 물체의 충돌 범위. 겉모양 메시와 다를 수 있음 |
| Rigidbody | 질량·중력·속도 등 기본 물리 처리를 연결하는 컴포넌트 |
| Trigger | 통과하면서 감지 알림을 받을 수 있는 충돌 영역 |
| Raycast | 선을 쏘아 무엇을 먼저 만나는지 검사하는 기능 |
| 히트스캔 | 발사 순간 선으로 명중 판정. 실제 날아가는 탄환 오브젝트가 아님 |
| bounds | 외형 전체를 감싸는 상자. 회전한 복잡한 모양을 정확히 따라가는 표면은 아님 |
| 스키닝 | 본이 움직일 때 각 정점이 얼마나 따라갈지 연결하는 것 |
| Animator | 상태와 매개변수에 따라 클립을 재생·혼합하는 Unity 기능 |
| 회귀 검사 | 고친 기능 외에 원래 되던 기능도 여전히 되는지 확인 |
| 해시 | 파일 내용의 지문. 같은지 비교하기 위한 값 |
