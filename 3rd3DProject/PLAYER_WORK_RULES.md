# 플레이어 작업 필수조건

이 프로젝트에서 모든 요청과 작업을 시작하기 전에 이 파일을 읽는다.

1. PlayerTestScene 이외의 다른 씬은 절대 조작하지 않는다.
2. 사용자가 요청한 스크립트 이외의 어떤 스크립트도 수정하거나 조작하지 않는다.
3. 사용자가 요청한 작업 이외의 작업을 이 프로젝트에서 실행하지 않는다.
4. 이번 작업에서 만드는 모든 스크립트는 Assets/02Scripts/01Player에 보관한다.
5. 기존 맵에 플레이어가 서도록 기존 오브젝트에 충돌을 설정하며, 별도의 지면 오브젝트를 추가하지 않는다.
6. 기존 이동·애니메이션·Cinemachine, 중앙 구체 커서, 빈손 물건 들기·놓기, 등록된 인벤토리 UI까지 획득 연출을 보존한다. 2026-10-06 조준·발사 요청은 샷건 유지형 줌·줌 중 걷기 고정, 한 클릭당4산탄 히트스캔, 총구 벽 차단, 손 준비·반동과 장착 중 속 빈 산탄 범위 원이다. 줌하면 산탄과 표시 범위가 함께 좁아진다. 실제 인벤토리 저장·슬롯·UI 제작은 포함하지 않는다.

## 구현·정리 원칙

7. 요청을 구현하기 전에 관련 스크립트, 씬 연결, 입력, 물리, 애니메이션, 카메라까지 실제 구현 경로와 의존성을 확인한다.
8. 최적화와 확장성을 우선하고 SOLID 원칙에 따라 책임을 분리한다. 사용하지 않는 추상화나 불필요한 컴포넌트는 추가하지 않는다.
9. 없어도 되는 기능과 이전 구현의 불필요한 스크립트를 점검하여 정리하고, 최대한 가볍게 유지한다. 사용자의 2026-09-30 요청에 따라 이번 요청 범위의 수정으로 대체되어 불필요해진 파일은 참조가 없음을 확인한 즉시 제거하고 작업 로그에 대상·이유·영향을 기록한다. 이 승인은 원본 모델, 사용자 작성 파일, 관련 없는 파일, 검증 증거·작업 기록의 삭제 또는 기존 변경 복구로 확대하지 않는다. 해당 대상은 개인 지침에 따라 별도 승인을 받는다.
10. 사용자가 이어서 작업하기 쉽도록 이름, 인스펙터 설정, 구현 경로와 책임을 명확히 표시한다. 현재 사용하는 기능과 연결은 보존하며 변경 후 실제 Play 모드에서 검증한다.

## 작업 기록과 설명서 작성 (2026-09-30 추가)

11. 모든 작업에서 요청, 확인한 구현 경로, 실제 변경·삭제 파일, 변경 이유, 실행한 검증과 결과, 오류 및 남은 문제를 `PLAYER_WORK_LOG.md`에 시간순으로 추가한다. 기존 기록은 덮어쓰지 않는다. 기록이 없는 과거 작업을 수행했다고 만들어 쓰지 않는다.
12. 매번 `PLAYER_IMPLEMENTATION_GUIDE.md`를 현재 구현에 맞게 갱신한다. 중학생이 읽어도 따라 할 수 있도록 동작 원리, 조작 방법, Unity에서 찾을 위치, 수정할 항목, 수정 뒤 확인 방법을 쉬운 말로 설명한다. 기술 용어는 필요한 곳에서 뜻을 함께 적는다.
13. 재사용하는 구현은 유지하고 새 구현으로 대체한 파일만 정리한다. 사용되지 않는다는 사실은 코드 참조뿐 아니라 씬·프리팹 연결과 실행 경로까지 확인한다. 삭제 대상이 없으면 '삭제 없음'과 이유를 로그에 남긴다.
14. 빈손의 입력 규칙은 우클릭을 누르는 동안만 물건을 들고, 떼면 내려놓는 것이다. 물건 중심은 중앙 조준선에 맞추며 벽 충돌이 우선한다. 숫자 1은 기존 샷건 장착·해제 토글이다. 물건을 들고 1을 누르면 안전한 내려놓기에 성공한 경우에만 장착한다. 2026-10-06 신규 요청에 따라 샷건 장착 중 우클릭은 유지형 줌이며 줌 중 이동은 Shift와 무관하게 걷기다. 좌클릭 한 번당 한 발을 줌·비줌 모두에서 발사한다. 카메라 중앙으로 목표를 정하고 실제 총구 방향 히트스캔으로 명중을 판정하며, 몸통에서 총구까지의 벽과 총구의 벽 겹침도 검사한다. 손 준비·발사 반동 애니메이션을 추가하되 기존 이동 클립은 유지한다. 소환 동작·효과음·탄약·적 체력 구현은 이 요청에 포함하지 않는다. 정지 카메라는 멈춘 방향에서 좌우60도, 이동 중에는 수평360도이며 상하 범위는 유지한다.

## 공식 기능과 느슨한 결합 (2026-09-30 요청 6·7)

15. 구현 전에 적용하려는 Unity 기능의 공식 문서와 현재 프로젝트 버전의 API를 확인하고, 참고 주소·선택 이유를 작업 로그에 기록한다. 설치된 패키지 코드는 버전별 동작 확인 자료로 사용한다.
16. 마찰·충돌·기본 입력·애니메이션 재생·카메라 투영 등 엔진이 담당해야 할 일은 기본 기능을 사용한다. 같은 기능을 자체 물리 엔진이나 복잡한 프레임워크로 다시 만들지 않는다.
17. Unity 기능을 쓰더라도 게임의 판단 규칙을 Unity 구성에 묶지 않는다. '언제 집고 놓는가', '장착·해제하는가', '걷기·달리기인가', '정지 시 얼마까지 회전하는가'는 Unity 타입이 없는 일반 C# 값과 정책으로 분리한다.
18. Rigidbody·Animator·Cinemachine·입력 장치·UI·렌더 API를 다루는 코드는 연결부(어댑터)에 모은다. Unity 버전이나 엔진을 바꿀 때 판단 규칙은 재사용하고 해당 연결부를 교체할 수 있게 한다. 엔진 없이 검증할 수 있는 규칙은 실제로 독립 컴파일해 확인한다.
19. 클래스 사이도 구체 타입의 서로 참조를 줄이고 필요한 기능만 작은 인터페이스로 주고받는다. 미래에 쓸지 모르는 범용 계층이나 모든 Unity API의 포장 코드는 추가하지 않는다.
20. 기존 구현도 위 기준으로 의존성을 조사해 필요한 곳을 수정한다. 엔진 전용 물리·렌더·UI 코드를 '그대로 타 엔진 이식 가능'이라고 보고하지 말고, 재사용 부분·교체 부분·실제 검증 범위를 설명서에 구분한다. 타 엔진 이식 완료 여부는 실제로 이식·검증하기 전에는 주장하지 않는다.

## 현재 플레이어 구현 경로

모든 아래 파일은 `Assets/02Scripts/01Player/`에 있다. 기존 이동·카메라 파일은 기능별 폴더로 옮기되 파일명과 meta GUID를 유지한다. 새 상호작용 컴포넌트는 뒤의 연결 안내를 따른다.

| 파일 | 책임 | 변경할 때 확인할 곳 |
| --- | --- | --- |
| `Core/CatPlayerMotor.cs` | Unity 실행 순서와 서비스 구성, 인스펙터 설정, 입력 교체·상태 초기화 진입점 | `Cat_Player` 컴포넌트 |
| `Input/CatPlayerInput.cs` | 이동 의도 값, `ICatPlayerInputSource`, 기본 WASD·Shift 키보드 입력 | 입력 장치를 추가할 때 |
| `Movement/CatPlayerLocomotion.cs` | 카메라 기준 이동 방향·가속·회전, Rigidbody 쓰기 | 이동 규칙을 수정할 때 |
| `Animation/CatPlayerAnimation.cs` | 이동 의도를 Idle/Walk/Run 상태로 연결 | Animator 상태와 전환 시간 |
| `Movement/CatStepSettings.cs` | 턱 검사 설정값과 설명 | Motor의 턱 설정 |
| `Movement/CatStepSolver.cs` | `ICatStepSolver` 구현, 바닥·턱·공간 검사와 상승 속도 계산 | 지형 통과 규칙을 수정할 때 |
| `Camera/CatCinemachineCursor.cs` | 시작 잠금, Esc 해제, 클릭 재잠금, 포커스 상실 시 입력 차단 | 메뉴·대화창 입력 연결 |
| `Camera/CatCameraLook.inputactions` | Cinemachine의 마우스 Look 입력 | 카메라 입력 바인딩 |
| `Editor/Validation/CatPlayerValidation.cs` | 수동 검증 메뉴와 실행·중단·결과 기록 | Unity의 Tools/Cat Player 메뉴 |
| `Editor/Validation/CatPlayerValidationCases.cs` | 실제 맵 기반 검증 사례와 판정 기준 | 맵 구조 또는 동작 기준 변경 시 |

### 실행 흐름과 설정

1. `CatPlayerMotor.Update` → 입력 공급자 `Read()` → `CatPlayerInputFrame` → 애니메이션 전환.
2. `CatPlayerMotor.FixedUpdate` → `CatPlayerLocomotion.Apply` → `CatMovementPolicy`의 속도 선택과 `ICatStepSolver` 검사 → Rigidbody 속도·회전 적용.
3. `CatCinemachineCursor` → 커서 잠금과 Cinemachine 입력 활성화. `Cat_CinemachineCamera` → `Cat_CameraTarget` 추적 → Main Camera의 CinemachineBrain 출력.
4. `CatCameraOrbitLimit` → `ICatMotionState`로 이동 여부 확인 → `CatOrbitPolicy`의 정지 좌우60도/이동360도 결과를 Cinemachine에 적용.
5. `CatInteractionInput` → `CatHandPolicy` → `CatInteractionController` → 물리 운반 또는 `ICatEquipmentPort` 장비 표시. 정책 자체는 Unity를 참조하지 않는다.

`Cat_Player > CatPlayerMotor`에서 연결, 이동·회전, 작은 턱 넘기, 애니메이션을 조절한다. 걷기 1.6, 달리기 3.4, 가속 18, 회전 720도/초가 기본이다. 턱 최대 높이 0.28, 전방 추가 검사 거리 0.12, 상승 속도 1.8은 월드 단위이며 최대 높이 0이면 보정이 꺼진다. 애니메이션 상태 이름과 0.12초 전환 시간은 편집 모드에서 설정한 뒤 Play를 시작한다.

### 확장·보존 규칙

- 게임패드·AI 등 새 입력은 `ICatPlayerInputSource`를 구현하고 `CatPlayerMotor.SetInputSource(...)`로 연결한다. `null`을 전달하면 기본 키보드 입력으로 복귀한다. 커스텀 입력의 메뉴·기절 등 차단 정책은 해당 입력 공급자에서 처리한다.
- 지형 처리 교체는 작은 `ICatStepSolver` 계약을 따른다. 실제 Rigidbody 쓰기는 `CatPlayerLocomotion`에 모아 중복 제어를 피한다.
- 재사용·리스폰 시 `ResetRuntimeState()`로 입력, 턱 목표, 애니메이션 캐시를 초기화한다. 비활성화 시에도 자동 호출되며 위치나 수평 속도를 임의로 초기화하지 않는다.
- UI가 커서를 제어할 때는 `CatCinemachineCursor.SetLocked(bool)`를 사용한다. 커서 해제 중에는 기본 키보드 이동과 카메라 회전이 차단된다.
- 기존 컴포넌트의 클래스·파일·meta GUID와 Motor의 `view`, `walkSpeed`, `runSpeed`, `acceleration`, `turnSpeed`, `steps` 필드명은 연결 보존을 위해 유지한다. 카메라 InputAction GUID도 유지한다.
- 매 프레임 새 컬렉션을 만들지 않는다. 물리 검사는 NonAlloc 버퍼를 재사용하고 애니메이션 상태 해시는 초기화 때만 계산한다. 변경 가능성이 없는 부분까지 인터페이스나 MonoBehaviour로 분할하지 않는다.

### 개발 도구 정리와 검증

과거 일회용 `*.cs.txt` 12개, 최초 설치 전용 `Editor/CatCinemachineSetup.cs`, 중복 검증기 `Editor/CatCinemachineValidation.cs`와 `Editor/CatStepValidation.cs`는 통합 도구로 대체했다. 검증 도구는 패키지 설치·씬 자동 저장·카메라 재구성을 하지 않는다. 실행 전 정확한 `PlayerTestScene` 경로를 검사하며 결과는 프로젝트의 `Logs/CatPlayerValidation.txt`에서 확인한다.

통합 Play 검증은 런타임의 플레이어 위치와 입력을 일시적으로 제어하고 종료 시 Play 모드를 끝낸다. 실제 맵 구조가 달라져 전제를 충족하지 못하는 사례는 통과로 보고하지 않는다. 다른 씬·팀원 스크립트·프로젝트 설정은 이 정리 범위에 포함하지 않는다.

검증 메뉴는 `Tools > Cat Player > 연결 검사`, `Play 모드 전체 검증`, `검증 중단`이다. 연결 검사는 편집 모드에서도 가능하며, 전체 검증은 `PlayerTestScene`에서 Play를 시작한 뒤 실행한다. 실행 중에는 직접 키보드·마우스로 캐릭터를 조작하지 않는다. 검증 로그의 `PASS`, `FAIL`, `검증불가`와 마지막 `RESULT`를 함께 확인한다.


## 중앙 커서와 아이템 상호작용

- `Interaction/CatInteractionController.cs`: 카메라 중심 조준, 플레이어 기준 거리, 벽 가림 검사, 손 사용 정책과 입력 연결.
- `Interaction/CatInteractionItem.cs`: 아이템 식별자·종류와 교체 가능한 `Visual`, 루트 충돌 크기.
- `Interaction/CatItemCarrier.cs`: 실린더 추출·중앙 조준선 보유·벽 검사·내려놓기. 우클릭을 떼면 보유를 끝낸다. 바닥이 없으면 빈 공간에서 중력으로 놓고, 해제 위치가 완전히 막혔다면 아이템이 공간 확보 후 물리를 복구한다.
- `Interaction/Presentation/CatCenterCursor.cs`: 화면 중앙의 6픽셀 반투명 흰 구체와 얇은 외곽선. Esc로 잠금을 풀면 숨긴다.
- `Interaction/Presentation/CatItemHighlight.cs`: 조준 중인 모델에만 옅은 노란 면, 진한 노란 외곽선과 약한 후광을 덧그린다. 원본 공유 재질은 변경하지 않는다.
- `Interaction/Presentation/CatInventoryPickupPresenter.cs`: 등록된 UI로 아이템 외형이 날아가는 연출과 완료 이벤트.
- `Interaction/Presentation/CatItemIconCapture.cs`: 실제 메시를 획득할 때 한 번 촬영하고 임시 카메라·메시·RenderTexture를 해제한다.
- 같은 Presentation 폴더의 셰이더 2개는 커서와 강조용이며 기존 URP Renderer 설정은 바꾸지 않는다.
- `Editor/Setup/CatInteractionSetup.cs`: 명시적으로 실행하는 설치·폴더 정리 도구. 기존 설치가 있으면 덮어쓰지 않는다.
- `Editor/Validation/CatInteractionValidation.cs`: Play 모드의 조준·우클릭 유지/해제·장비 토글·벽 가림·UI 및 정지 카메라 검증. 종료 시 Play를 끝내며 임시 테스트 UI는 씬에 저장하지 않는다.

### 씬과 모델 교체

`PlayerTestScene > Cat_Interaction_Items`에 실린더 4개와 납작한 큐브 2개를 배치한다. 프리팹과 머티리얼은 `Assets/03Sprites/Player/Interaction/`에 둔다. 기존 맵 메시나 프리팹을 변경하지 않는다.

아이템 루트의 `CatInteractionItem`, `BoxCollider`, `Rigidbody`는 유지하고 `Visual` 아래의 외형을 교체한다. 루트 컴포넌트 메뉴의 `Visual 크기에 맞춰 충돌 갱신`을 실행하거나 코드에서 `RefreshVisual()`을 호출하면 충돌 크기·강조 대상이 갱신된다. 외형에는 별도 Rigidbody·Collider를 넣지 않고 루트에서 관리한다. 보유 중 교체는 피하고 내려놓은 상태에서 갱신한다.

### 인벤토리 담당자 연결

`Cat_Player > CatInventoryPickupPresenter > Inventory Target`에 실제 인벤토리 UI의 RectTransform을 지정한다. UI가 나중에 생성되면 `RegisterTarget(rectTransform)`으로 등록하고 해제 시 `RegisterTarget(null)`을 호출한다. **미등록·비활성 UI에서는 큐브가 사라지지 않으며 획득 연출도 실행하지 않는다. 임시 화면 목적지는 사용하지 않는다.**

`Collected` 이벤트는 연출 완료 시 아이템 `itemId`를 1회 전달한다. 인벤토리 저장은 담당자의 코드가 이 이벤트를 구독하여 처리한다. 연출 중 목적지 해제·비활성화 시 아이템의 표시와 물리 상태를 복원하며 이벤트를 보내지 않는다. 현재 단계에는 슬롯 수·용량·중복 여부 등 인벤토리 규칙을 구현하지 않았다.

Overlay Canvas는 카메라 없이 좌표를 변환하고, Camera/World Canvas는 등록된 Canvas 카메라를 사용한다. 획득 아이콘은 입력을 가로채지 않는 별도 표시 계층이며 일회성 촬영으로 실제 교체 모델의 외형을 사용한다.


### 2026-09-30 상호작용 검증 기록

- Play 상호작용 16개 PASS: 조준, F 들기·놓기, 선반 추출, 벽 가림, UI 미등록 거부, UI 도착 이벤트 1회, 연결 해제 시 복구, 외형 크기 변경, 커서 잠금 해제 차단.
- 기존 이동·작은 턱·Idle/Walk/Run·Cinemachine 검증 19개 PASS.
- 실제 Game 화면에서 노란 강조와 흰 중앙 구체를 확인. 씬의 Inventory Target은 비워 두며 테스트 UI는 Play 종료와 함께 제거됨.
- 상세 결과: `Logs/CatInteractionValidation.txt`, `Logs/CatPlayerValidation.txt`. 화면: `Logs/interaction_hover_game.png`, `Logs/interaction_pickup.png`.
- 설치 도구는 전체 `SaveAssets()`를 호출하지 않는다. 요청한 Interaction 폴더의 에셋과 PlayerTestScene만 저장한다.
- 이번 최초 설치의 전체 에셋 저장 시 기존 Idle/Walk/Run 클립도 변경된 상태로 저장된 것이 감지되었다. 시작 전 해시와 일치하는 원본 및 현재 파일을 외부 작업 백업에 보관했고, 사용자 복원 승인 전에는 애니메이션을 다시 변경하지 않는다.
