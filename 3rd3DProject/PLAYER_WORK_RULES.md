# 플레이어 작업 필수조건

이 프로젝트에서 모든 요청과 작업을 시작하기 전에 이 파일을 읽는다.

현재 작업 프로젝트는 `C:/Users/307/Desktop/unity/Team3rd3DProject/3rd3DProject`이다. 아래 과거 기록보다 최신 사용자 요청과 이 문서의 현재 범위가 우선한다.

- 씬 작업과 검증은 `Assets/01Scenes/PlaytestScene01.unity` 하나에 한정한다.
- 코드 변경은 요청된 `Assets/02Scripts/01Player`의 기존 C#과 최신 F 합의에서 승인한 `04Systems/ItemInteractor.cs`, `HotbarManager.cs`만 대상으로 한다. Rat·Enemy·Mouse 스크립트, 다른 씬과 프로젝트 설정은 수정하지 않는다.
- 작업 시작 시 존재하는 전체 `.meta`의 내용과 각 GUID를 보존한다. 이름을 바꾸는 C# 파일의 `.meta`는 짝을 이루어 경로만 함께 바꾸고 내용은 바꾸지 않는다. GUID는 Unity가 파일을 연결할 때 사용하는 식별 번호이며, 새 번호를 발급하거나 메타를 다시 만들지 않는다.
- 2026-10-07 사용자가 Player 스크립트의 간단한 이름 변경을 명시적으로 요청했다. 설명서의 대응표에 있는 기존 C# 40개와 짝이 되는 `.meta`의 이름 변경만 허용한다. 폴더·모델·프리팹·재질·애니메이션 등 다른 파일의 이름 변경·이동·삭제는 포함하지 않는다. 과거 삭제 승인이나 이전 사본을 복원·삭제 권한으로 재사용하지 않는다.

1. `Assets/01Scenes/PlaytestScene01.unity` 이외의 다른 씬은 절대 조작하지 않는다.
2. 사용자가 요청한 스크립트 이외의 어떤 스크립트도 수정하거나 조작하지 않는다.
3. 사용자가 요청한 작업 이외의 작업을 이 프로젝트에서 실행하지 않는다.
4. 코드 수정은 위 최신 허용 범위에 한정한다. 과거 승인된 이름 변경은 `Assets/02Scripts/01Player`의 기존 C#만 대상으로 했다. 폴더 구조를 유지하고 새 스크립트나 메타로 대체하지 않는다. `CatInteractionItem.cs`는 내부 복구 상태의 저장 형식을 보존하기 위해 파일명·클래스명을 유지한다.
5. 기존 맵의 충돌 연결을 보존하며, 별도의 지면 오브젝트를 추가하지 않는다.
6. 기존 이동·애니메이션·Cinemachine, 중앙 구체 커서, 빈손 물건 들기·놓기, 등록된 인벤토리 UI까지 획득 연출을 보존한다. 2026-10-06 조준·발사 요청은 샷건 유지형 줌·줌 중 걷기 고정, 한 클릭당4산탄 히트스캔, 총구 벽 차단, 손 준비·반동과 장착 중 속 빈 산탄 범위 원이다. 줌하면 산탄과 표시 범위가 함께 좁아진다. 최신 F 합의에서는 기존 5칸 저장소를 재사용하며 UI 제작은 포함하지 않는다.

## 2026-10-07 최신 F 상호작용 합의

- 이번 변경은 기존 `Assets/02Scripts/04Systems/ItemInteractor.cs`의 연장이다. 승인된 A안에 따라 같은 폴더의 기존 `HotbarManager.cs` 5칸 저장소를 재사용한다. 해당 두 파일과 관련 기존 `01Player` 코드, `PlaytestScene01`, 기존 작업 문서만 변경한다. 아래의 과거 `01Player` 한정·인벤토리 미구현 설명보다 이 합의가 우선한다.
- F는 중앙 포인터의 등록 대상에 2.2 거리 안에서 한 번 작동한다. 일반 물건은 항상 빈 인벤토리 칸에 저장하고 월드 원본만 비활성화한다. 빈손·총 장착·좌클릭 물건 보유 상태는 그대로 유지하며 F로 새 물건을 손에 들거나 총을 해제하지 않는다. 빈칸이 없으면 대상·저장소·손 상태를 모두 유지한다.
- 등록된 대상을 보고 있지 않을 때 F는 아무 동작도 하지 않는다. F로 보관품을 꺼내거나 내려놓는 입력과 인벤토리 UI는 아직 합의되지 않았으므로 추가하지 않는다.
- 좌클릭을 떼면 물건의 현재 위치·회전을 유지한 채 보유를 끝낸다. 기존 Collider 활성 상태를 복원하고 기존 Rigidbody의 `Is Kinematic`을 끄고 `Use Gravity`를 켜 정지 상태에서 자연 낙하하게 한다. 바닥으로 순간 이동, 대체 착지점 탐색, 공중 정지·복구 대기, 자체 중력 계산은 사용하지 않는다. Collider는 충돌 모양이며 중력은 Rigidbody가 담당한다.
- 등록 Enemy는 빈손·물건·총 상태 모두 F로 포획하며 손 상태를 유지한다. 쥐 카운트·점수는 추후 구현한다. 등록된 Trigger도 대상이 될 수 있지만 벽 뒤의 대상은 거부한다. 기존 덫·쥐의 파일이나 Collider 설정은 변경하지 않는다.
- 빈 자식 오브젝트를 만들지 않는다. 같은 Player 본체의 기존 상호작용과 연결한다. UI는 만들지 않고 저장 조회·변경 이벤트를 제공한다. Hotbar 숫자키 입력은 이 Player에서 꺼서 기존 1번 샷건 입력을 보존한다.
- 모든 기존 `.meta` 내용과 GUID를 보존한다. 이번에는 이름 변경·파일 삭제·패키지 설치·Git 쓰기 작업이 없다. 승인된 범위의 코드·씬과 문서 외 수정이 없는지 최종 지문 검사를 한다.

## 구현·정리 원칙

7. 요청을 구현하기 전에 관련 스크립트, 씬 연결, 입력, 물리, 애니메이션, 카메라까지 실제 구현 경로와 의존성을 확인한다.
8. 최적화와 확장성을 우선하고 SOLID 원칙에 따라 책임을 분리한다. 사용하지 않는 추상화나 불필요한 컴포넌트는 추가하지 않는다.
9. 중복과 불필요한 처리는 요청된 기존 Player C# 파일 안에서만 줄인다. 파일·폴더 정리와 삭제는 하지 않는다. 이름 변경은 설명서의 40개 C# 대응표에만 한정한다. 사용하지 않는 파일을 발견해도 보고만 하며, 2026-09-30의 과거 삭제 승인을 다시 적용하지 않는다. 기존 변경을 되돌리거나 다른 사본의 파일을 자동 복원하지 않는다.
10. 사용자가 이어서 작업하기 쉽도록 이름, 인스펙터 설정, 구현 경로와 책임을 명확히 표시한다. 현재 사용하는 기능과 연결은 보존하며 변경 후 실제 Play 모드에서 검증한다.

## 작업 기록과 설명서 작성 (2026-09-30 추가)

11. 모든 작업에서 요청, 확인한 구현 경로, 실제 변경·삭제 파일, 변경 이유, 실행한 검증과 결과, 오류 및 남은 문제를 `PLAYER_WORK_LOG.md`에 시간순으로 추가한다. 기존 기록은 덮어쓰지 않는다. 기록이 없는 과거 작업을 수행했다고 만들어 쓰지 않는다.
12. 매번 `PLAYER_IMPLEMENTATION_GUIDE.md`를 현재 구현에 맞게 갱신한다. 중학생이 읽어도 따라 할 수 있도록 동작 원리, 조작 방법, Unity에서 찾을 위치, 수정할 항목, 수정 뒤 확인 방법을 쉬운 말로 설명한다. 기술 용어는 필요한 곳에서 뜻을 함께 적는다.
13. 재사용하는 구현은 유지한다. 사용 여부는 코드 참조뿐 아니라 씬·프리팹 연결과 실행 경로까지 확인한다. 이번 범위는 파일 삭제를 포함하지 않으므로 작업 로그에 '삭제 없음'과 보존 범위를 남긴다.
14. 빈손의 입력 규칙은 좌클릭을 누르는 동안만 물건을 들고, 떼면 현재 위치에서 자연 낙하시키는 것이다. 들고 있는 물건 중심은 중앙 조준선에 맞추며 벽 충돌이 우선한다. 숫자 1은 기존 샷건 장착·해제 토글이다. 좌클릭으로 물건을 들고 1을 누르면 현재 위치에서 놓고 샷건을 장착한다. 바닥 탐색이나 공간 확보를 기다리지 않는다. 2026-10-06 신규 요청에 따라 샷건 장착 중 우클릭은 유지형 줌이며 줌 중 이동은 Shift와 무관하게 걷기다. 좌클릭 한 번당 한 발을 줌·비줌 모두에서 발사한다. 카메라 중앙으로 목표를 정하고 실제 총구 방향 히트스캔으로 명중을 판정하며, 몸통에서 총구까지의 벽과 총구의 벽 겹침도 검사한다. 손 준비·발사 반동 애니메이션을 추가하되 기존 이동 클립은 유지한다. 소환 동작·효과음·탄약·적 체력 구현은 이 요청에 포함하지 않는다. 정지 카메라는 멈춘 방향에서 좌우60도, 이동 중에는 수평360도이며 상하 범위는 유지한다.

## 공식 기능과 느슨한 결합 (2026-09-30 요청 6·7)

15. 구현 전에 적용하려는 Unity 기능의 공식 문서와 현재 프로젝트 버전의 API를 확인하고, 참고 주소·선택 이유를 작업 로그에 기록한다. 설치된 패키지 코드는 버전별 동작 확인 자료로 사용한다.
16. 마찰·충돌·기본 입력·애니메이션 재생·카메라 투영 등 엔진이 담당해야 할 일은 기본 기능을 사용한다. 같은 기능을 자체 물리 엔진이나 복잡한 프레임워크로 다시 만들지 않는다.
17. Unity 기능을 쓰더라도 게임의 판단 규칙을 Unity 구성에 묶지 않는다. '언제 집고 놓는가', '장착·해제하는가', '걷기·달리기인가', '정지 시 얼마까지 회전하는가'는 Unity 타입이 없는 일반 C# 값과 정책으로 분리한다.
18. Rigidbody·Animator·Cinemachine·입력 장치·UI·렌더 API를 다루는 코드는 연결부(어댑터)에 모은다. Unity 버전이나 엔진을 바꿀 때 판단 규칙은 재사용하고 해당 연결부를 교체할 수 있게 한다. 엔진 없이 검증할 수 있는 규칙은 실제로 독립 컴파일해 확인한다.
19. 클래스 사이도 구체 타입의 서로 참조를 줄이고 필요한 기능만 작은 인터페이스로 주고받는다. 미래에 쓸지 모르는 범용 계층이나 모든 Unity API의 포장 코드는 추가하지 않는다.
20. 기존 구현도 위 기준으로 의존성을 조사해 필요한 곳을 수정한다. 엔진 전용 물리·렌더·UI 코드를 '그대로 타 엔진 이식 가능'이라고 보고하지 말고, 재사용 부분·교체 부분·실제 검증 범위를 설명서에 구분한다. 타 엔진 이식 완료 여부는 실제로 이식·검증하기 전에는 주장하지 않는다.

## 현재 플레이어 구현 경로

모든 아래 파일은 `Assets/02Scripts/01Player/`에 있다. 현재 폴더 구조와 메타 내용을 유지한다. 2026-10-07 변경 전·후 파일명은 `PLAYER_IMPLEMENTATION_GUIDE.md`의 40개 대응표에서 확인한다. 상호작용 컴포넌트는 뒤의 연결 안내를 따른다.

| 파일 | 책임 | 변경할 때 확인할 곳 |
| --- | --- | --- |
| `Core/PlayerController.cs` | Unity 실행 순서와 서비스 구성, 인스펙터 설정, 입력 교체·상태 초기화 진입점 | `Cat_Player` 컴포넌트 |
| `Input/PlayerInput.cs` | 이동 의도 값, `IPlayerInputSource`, 기본 WASD·Shift 키보드 입력 | 입력 장치를 추가할 때 |
| `Movement/PlayerMovement.cs` | 카메라 기준 이동 방향·가속·회전, Rigidbody 쓰기 | 이동 규칙을 수정할 때 |
| `Animation/PlayerAnimation.cs` | 이동 의도를 Idle/Walk/Run 상태로 연결 | Animator 상태와 전환 시간 |
| `Movement/StepSettings.cs` | 턱 검사 설정값과 설명 | PlayerController의 턱 설정 |
| `Movement/StepSolver.cs` | `IStepSolver` 구현, 바닥·턱·공간 검사와 상승 속도 계산 | 지형 통과 규칙을 수정할 때 |
| `Camera/CameraCursorLock.cs` | 시작 잠금, Esc 해제, 클릭 재잠금, 포커스 상실 시 입력 차단 | 메뉴·대화창 입력 연결 |
| `Camera/CatCameraLook.inputactions` | Cinemachine의 마우스 Look 입력 | 카메라 입력 바인딩 |
| `Editor/Validation/PlayerValidation.cs` | 수동 검증 메뉴와 실행·중단·결과 기록 | Unity의 Tools/Cat Player 메뉴 |
| `Editor/Validation/PlayerValidationCases.cs` | 실제 맵 기반 검증 사례와 판정 기준 | 맵 구조 또는 동작 기준 변경 시 |

### 실행 흐름과 설정

1. `PlayerController.Update` → 입력 공급자 `Read()` → `PlayerInputFrame` → 애니메이션 전환.
2. `PlayerController.FixedUpdate` → `PlayerMovement.Apply` → `MovementPolicy`의 속도 선택과 `IStepSolver` 검사 → Rigidbody 속도·회전 적용.
3. `CameraCursorLock` → 커서 잠금과 Cinemachine 입력 활성화. `Cat_CinemachineCamera` → `Cat_CameraTarget` 추적 → Main Camera의 CinemachineBrain 출력.
4. `CameraOrbitLimit` → `IMotionState`로 이동 여부 확인 → `OrbitPolicy`의 정지 좌우60도/이동360도 결과를 Cinemachine에 적용.
5. `InteractionInput` → `HandPolicy` → `InteractionController` → 물리 운반 또는 `IEquipmentPort` 장비 표시. 정책 자체는 Unity를 참조하지 않는다.

`Cat_Player > PlayerController`에서 연결, 이동·회전, 작은 턱 넘기, 애니메이션을 조절한다. 걷기 1.6, 달리기 3.4, 가속 18, 회전 720도/초가 기본이다. 턱 최대 높이 0.28, 전방 추가 검사 거리 0.12, 상승 속도 1.8은 월드 단위이며 최대 높이 0이면 보정이 꺼진다. 애니메이션 상태 이름과 0.12초 전환 시간은 편집 모드에서 설정한 뒤 Play를 시작한다.

### 확장·보존 규칙

- 게임패드·AI 등 새 입력은 `IPlayerInputSource`를 구현하고 `PlayerController.SetInputSource(...)`로 연결한다. `null`을 전달하면 기본 키보드 입력으로 복귀한다. 커스텀 입력의 메뉴·기절 등 차단 정책은 해당 입력 공급자에서 처리한다.
- 지형 처리 교체는 작은 `IStepSolver` 계약을 따른다. 실제 Rigidbody 쓰기는 `PlayerMovement`에 모아 중복 제어를 피한다.
- 재사용·리스폰 시 `ResetRuntimeState()`로 입력, 턱 목표, 애니메이션 캐시를 초기화한다. 비활성화 시에도 자동 호출되며 위치나 수평 속도를 임의로 초기화하지 않는다.
- UI가 커서를 제어할 때는 `CameraCursorLock.SetLocked(bool)`를 사용한다. 커서 해제 중에는 기본 키보드 이동과 카메라 회전이 차단된다.
- 승인된 클래스·파일명 변경에도 meta의 내용과 GUID, PlayerController의 `view`, `walkSpeed`, `runSpeed`, `acceleration`, `turnSpeed`, `steps` 필드명은 연결 보존을 위해 유지한다. 카메라 InputAction GUID도 유지한다.
- 매 프레임 새 컬렉션을 만들지 않는다. 물리 검사는 NonAlloc 버퍼를 재사용하고 애니메이션 상태 해시는 초기화 때만 계산한다. 변경 가능성이 없는 부분까지 인터페이스나 MonoBehaviour로 분할하지 않는다.
- 상태 초기화가 필요한 상호작용·전투 입력만 `IResettableInputSource`를 구현한다. 포커스 상실·비활성화 때 이 약속으로 초기화하며, 모든 입력에 불필요한 초기화 함수를 강요하지 않는다. 두 기본 입력은 `InputActivationGate`를 공유해 입력이 다시 켜진 첫 프레임의 클릭을 집기·발사로 쓰지 않는다.
- 강조 대상은 기존 아이템의 예약 상태를 먼저 확인한 뒤 `IHighlightSource`로 찾는다. 최근 대상의 강조 메시를 재사용하고 대상 교체·외형 변경·대상 비활성화·파괴·컨트롤러 비활성화 때 임시 메시를 정리한다.

### 개발 도구 정리와 검증

과거 일회용 `*.cs.txt` 12개, 최초 설치 전용 `Editor/CatCinemachineSetup.cs`, 중복 검증기 `Editor/CatCinemachineValidation.cs`와 `Editor/CatStepValidation.cs`는 통합 도구로 대체했다. 검증 도구는 패키지 설치·씬 자동 저장·카메라 재구성을 하지 않는다. 실행 전 정확한 `Assets/01Scenes/PlaytestScene01.unity` 경로를 검사하며 현재 결과는 프로젝트 밖 `%TEMP%/PlayerValidation`에서 확인한다.

통합 Play 검증은 런타임의 플레이어 위치와 입력을 일시적으로 제어하고 종료 시 Play 모드를 끝낸다. 실제 맵 구조가 달라져 전제를 충족하지 못하는 사례는 통과로 보고하지 않는다. 다른 씬·팀원 스크립트·프로젝트 설정은 이 정리 범위에 포함하지 않는다.

검증 메뉴는 `Tools > Cat Player > 연결 검사`, `협업 에셋 연결 검사`, `Play 모드 이동·턱·카메라 검증`, `검증 중단`이다. 연결 검사는 편집 모드에서도 가능하며, Play 검증은 `PlaytestScene01`에서 Play를 시작한 뒤 실행한다. 이동·상호작용·전투·실제 입력 검증은 동시에 실행할 수 없다. 실행 중에는 직접 키보드·마우스로 캐릭터를 조작하지 않는다. 검증 로그의 `PASS`, `FAIL`, `검증불가`와 마지막 `RESULT`를 함께 확인한다.


## 중앙 커서와 아이템 상호작용

- `Interaction/InteractionController.cs`: 카메라 중심 조준, 플레이어 기준 거리, 벽 가림 검사, 손 사용 정책과 입력 연결.
- `Interaction/CatInteractionItem.cs`: 아이템 식별자·종류와 교체 가능한 `Visual`, 루트 충돌 크기.
- `Interaction/ItemCarrier.cs`: 실린더 추출·중앙 조준선 보유·벽 검사·내려놓기. 좌클릭을 떼면 현재 위치·회전에서 보유를 끝내고 기존 Rigidbody의 중력으로 놓는다. 바닥 스냅과 공중 복구 대기는 없다. 이전 `CatInteractionItem.ReleaseRecovery` 저장 형식은 보존하되 남은 상태는 즉시 물리를 복구한다.
- `Interaction/Presentation/CenterCursor.cs`: 화면 중앙의 6픽셀 반투명 흰 구체와 얇은 외곽선. Esc로 잠금을 풀면 숨긴다.
- `Interaction/Presentation/ItemHighlight.cs`: 조준 중인 모델에만 옅은 노란 면, 진한 노란 외곽선과 약한 후광을 덧그린다. 원본 공유 재질은 변경하지 않는다.
- `Interaction/Presentation/InventoryPickupEffect.cs`: 등록된 UI로 아이템 외형이 날아가는 연출과 완료 이벤트.
- `Interaction/Presentation/ItemIconCapture.cs`: 실제 메시를 획득할 때 한 번 촬영하고 임시 카메라·메시·RenderTexture를 해제한다.
- 같은 Presentation 폴더의 셰이더 2개는 커서와 강조용이며 기존 URP Renderer 설정은 바꾸지 않는다.
- `Editor/Setup/InteractionSetup.cs`: 명시적으로 실행하는 기존 상호작용 연결 도구. 연결이 없으면 기존 GUID로 에셋을 찾는다. 새 에셋·메타 생성, 파일 이동과 전체 `SaveAssets()` 호출은 하지 않는다.
- `Editor/Validation/InteractionValidation.cs`: Play 모드의 조준·좌클릭 유지/해제·장비 토글·벽 가림·UI 및 정지 카메라 검증. 종료 시 Play를 끝내며 임시 테스트 UI는 씬에 저장하지 않는다.

### 씬과 모델 교체

`PlaytestScene01`의 기존 상호작용 아이템을 사용한다. 프리팹과 머티리얼은 `Assets/04Prefabs/Player/Interaction/`의 현재 연결을 재사용한다. 플레이어 모델·프리팹·무기·물리 재질은 `Assets/04Prefabs/Player/`, 발사 클립과 산탄 원 재질은 `Assets/03Sprites/Player/`에 있다. 파일을 옮기거나 기존 맵 메시·프리팹 에셋을 변경하지 않는다.

아이템 루트의 `CatInteractionItem`, `BoxCollider`, `Rigidbody`는 유지하고 `Visual` 아래의 외형을 교체한다. 루트 컴포넌트 메뉴의 `Visual 크기에 맞춰 충돌 갱신`을 실행하거나 코드에서 `RefreshVisual()`을 호출하면 충돌 크기·강조 대상이 갱신된다. 외형에는 별도 Rigidbody·Collider를 넣지 않고 루트에서 관리한다. 보유 중 교체는 피하고 내려놓은 상태에서 갱신한다.

### 인벤토리 담당자 연결

`Cat_Player > InventoryPickupEffect > Inventory Target`에 실제 인벤토리 UI의 RectTransform을 지정한다. UI가 나중에 생성되면 `RegisterTarget(rectTransform)`으로 등록하고 해제 시 `RegisterTarget(null)`을 호출한다. **미등록·비활성 UI에서는 큐브가 사라지지 않으며 획득 연출도 실행하지 않는다. 임시 화면 목적지는 사용하지 않는다.**

위 UI 안내는 기존 좌클릭 큐브 연출에 해당한다. `Collected` 이벤트는 연출 완료 시 아이템 `itemId`를 1회 전달하고 담당자의 코드가 저장을 처리한다. 연출 중 목적지 해제·비활성화 시 아이템의 표시와 물리 상태를 복원하며 이벤트를 보내지 않는다. 이 연출 자체에는 저장 칸 규칙이 없다. F 획득은 별도로 기존 5칸 `HotbarManager`에 직접 저장하며 UI 없이 동작한다. 인벤토리 UI와 보관품 꺼내기 입력은 추가하지 않는다.

Overlay Canvas는 카메라 없이 좌표를 변환하고, Camera/World Canvas는 등록된 Canvas 카메라를 사용한다. 획득 아이콘은 입력을 가로채지 않는 별도 표시 계층이며 일회성 촬영으로 실제 교체 모델의 외형을 사용한다.

### 2026-10-07 입력과 연결 보존 원칙

- 빈손의 좌클릭은 새로 누를 때 집고, 유지하는 동안 들며, 떼면 내려놓는다. 빈손 우클릭으로는 집지 않는다. 샷건 장착 중 좌클릭 발사·우클릭 줌은 기존대로 유지한다.
- Esc 후 화면을 다시 잠그는 좌클릭으로 물건을 집거나 총을 쏘지 않는다. 창의 포커스를 잃으면 보유를 종료하고 입력 상태를 비운다.
- `InteractionController`의 획득 판단은 `IPickupPort`를 통해 전달한다. 다른 획득 연출은 `TrySetPickupSource(source)`로 연결하고, `null`이면 인스펙터의 `pickup` 연결로 돌아간다. 현재 획득 연출이 진행 중이면 교체 요청을 거부한다.
- 이름 변경은 Unity의 `AssetDatabase.MoveAsset`으로 C#과 메타를 함께 처리하며, 작업 전후 메타의 내용·GUID와 스크립트 연결을 대조한다. 이름이 바뀐 MonoBehaviour에는 예전 클래스명을 알리는 `MovedFrom`을 둔다. 직렬화 필드명과 발사 클립의 `raiseWeight`, `recoilDistance`, `recoilPitch`는 유지한다.
- 현재 결과는 새 검증이 끝난 뒤 `PLAYER_WORK_LOG.md`에 기록한다. 아래 날짜별 검증 기록을 이번 변경의 통과 결과로 재사용하지 않는다.

### 2026-09-30 상호작용 검증 기록

- Play 상호작용 16개 PASS: 조준, F 들기·놓기, 선반 추출, 벽 가림, UI 미등록 거부, UI 도착 이벤트 1회, 연결 해제 시 복구, 외형 크기 변경, 커서 잠금 해제 차단.
- 기존 이동·작은 턱·Idle/Walk/Run·Cinemachine 검증 19개 PASS.
- 실제 Game 화면에서 노란 강조와 흰 중앙 구체를 확인. 씬의 Inventory Target은 비워 두며 테스트 UI는 Play 종료와 함께 제거됨.
- 상세 결과: `Logs/CatInteractionValidation.txt`, `Logs/CatPlayerValidation.txt`. 화면: `Logs/interaction_hover_game.png`, `Logs/interaction_pickup.png`.
- 설치 도구는 전체 `SaveAssets()`를 호출하지 않는다. 요청한 Interaction 폴더의 에셋과 PlayerTestScene만 저장한다.
- 이번 최초 설치의 전체 에셋 저장 시 기존 Idle/Walk/Run 클립도 변경된 상태로 저장된 것이 감지되었다. 시작 전 해시와 일치하는 원본 및 현재 파일을 외부 작업 백업에 보관했고, 사용자 복원 승인 전에는 애니메이션을 다시 변경하지 않는다.
