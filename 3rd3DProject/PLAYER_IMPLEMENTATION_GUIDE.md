# 플레이어 구현과 수정 설명서

이 문서는 현재 플레이어 기능을 직접 수정할 사람을 위한 안내입니다. 작업 전에는 `PLAYER_WORK_RULES.md`를 먼저 읽습니다. 작업별 검증·오류·삭제 기록은 `PLAYER_WORK_LOG.md`에 추가합니다.

## 2026-10-08 현재 작업 저장과 병합

Player 작업은 `197532a` 커밋에 저장돼 있습니다. 그 뒤 받아온 작업을 합치면서 생긴 카메라 충돌은 사용자가 선택한 현재 Player 구도(두 카메라 높이 Y=1.3789881)로 정리했습니다. 두 카메라의 충돌한 위치·회전 외에는 자동 병합된 내용을 유지합니다.

Unity에 `The open scene(s) have been modified externally`가 떠 있으면 `Reload`는 디스크에 저장된 병합 결과를 다시 읽는 버튼입니다. 이번 디스크 파일의 충돌 표시는 제거됐지만 Unity 재불러오기·Play 검사는 아직 완료하지 않았습니다. 예전 내용을 다시 저장해 병합 결과를 덮지 않도록 먼저 새 씬을 불러온 뒤 확인합니다.

저장은 파일을 디스크에 기록하는 것, 커밋은 현재 파일 상태를 내 컴퓨터의 Git 기록으로 남기는 것, 푸시는 그 기록을 GitHub로 보내는 것입니다. 이번 요청은 로컬 커밋까지이며 푸시는 포함하지 않습니다.

## 점프로 들어가는 화살표와 부드러운 연결 (2026-10-08 후속 적용)

`Any State`는 “지금 어떤 동작을 하고 있든”이라는 공통 출발점입니다. `Any State → Jump` 하나가 대기·걷기·달리기에서 점프로 들어가는 연결을 함께 담당합니다. 각 상태에서 Jump로 향하는 화살표가 따로 없어도 연결돼 있습니다. 돌아올 때에는 `Movement` 값에 맞춰 Jump에서 Idle·Walk·Run 중 하나로 갑니다.

현재 진입은 0.08초, 복귀는 0.15초 동안 두 자세를 섞습니다. 이전에는 모두 0초여서 즉시 바뀌었습니다. `PlayerAnimation`은 점프를 마친 직후에도 마지막 점프 재생 위치를 유지합니다. 그래야 복귀 중인 자세가 점프 준비 자세로 되감기지 않습니다.

이동 중에는 착지 충격을 보여 준 뒤 약 0.18초부터 보행으로 돌아옵니다. 제자리 착지는 기존 0.69초 회복을 유지합니다. 일반 점프 준비 0.40초, 높이, 원본 클립은 그대로입니다. 움직일 때 착지 뒤 발이 멈춘 채 미끄러지는 구간을 줄이는 변경입니다.

수정할 곳은 `PlayerJump.controller`의 `Any State → Jump`와 `Jump → Idle/Walk/Run` 화살표입니다. `Fixed Duration`을 켠 상태에서 `Transition Duration`을 초 단위로 조절합니다. 착지 회복 기준은 `Domain/JumpPolicy.cs`의 `MovingLandingTime`입니다. 0.13초 착지 눌림보다 짧게 만들면 충격 표현과 버퍼 재도약을 잘라낼 수 있습니다. 조절 뒤 이동 중 점프, 착지 직후 Space 재입력, 점프 중 사격을 함께 확인합니다.

## 사격 후 멈춤 교정과 점프 버퍼링 (2026-10-08)

샷건을 쏜 뒤 몸이 멈췄던 이유는 사격 레이어의 `Ready`가 빈 상태였기 때문입니다. 상태 이름은 Walk나 Run으로 바뀌어도 실제 본은 마지막 자세에 멈췄습니다. 이제 `Ready`에도 기존 `Shotgun_Fire` 클립을 연결하고 `Speed=0`, 재생 위치=0으로 두어 사격용 세 값을 쉬는 값으로 돌려줍니다. `Ready`의 Motion을 비우지 않습니다. 모델이나 사격 원본 클립은 고치지 않았습니다.

점프 버퍼링은 **착지 조금 전에 누른 Space를 잠깐 기억해 두는 기능**입니다. 현재 기본값은 0.15초입니다. 그 안에 착지하면 입력을 한 번 소비하고, 몸이 눌렸다가 다시 뛰도록 이어집니다. 실제 이륙은 눌림 0.13초와 짧은 준비 0.08초를 거친 뒤입니다. 공중에서 즉시 한 번 더 뛰는 기능은 아닙니다.

재도약은 착지 때 눌린 자세를 거꾸로 펴서 이륙으로 연결합니다. 별도의 준비 자세로 순간 이동시키지 않아 귀·손·꼬리가 같은 곡선을 따라 이어집니다. 일반 점프의 원본 클립과 준비 시간은 그대로이며, 재도약 중 입력을 취소해도 현재 자세에서 착지 회복으로 이어집니다.

- 설정 위치: Player의 `PlayerController > 점프 (Space) > Jump Settings > Jump Buffer Seconds`.
- 값을 늘리면 조금 더 일찍 누른 입력도 기억합니다. 0이면 착지 전 입력을 보관하지 않습니다.
- 키를 계속 누르는 것만으로 반복 점프하지 않습니다. 다시 누른 순간만 보관합니다.
- Esc로 입력을 막거나, 창의 포커스를 잃거나, Player를 끄거나, 입력 공급자를 교체하면 예약이 지워집니다.
- 제자리 착지에서 이미 몸이 펴지는 중에 누른 입력은 0.15초 안에 회복이 끝나야 다음 점프가 됩니다. 이동 착지의 짧은 회복 안에서 누른 입력은 현재 착지 자세부터 역으로 이어서 재도약하므로 준비 자세로 순간 이동하지 않습니다. 제자리에서 이미 몸이 펴진 뒤 이동과 Space를 함께 누르면 짧은 역재생을 하지 않고 일반 회복을 기다립니다.
- 수정 뒤에는 착지 직전/너무 이른 Space, 키 유지, 사격 후 걷기·달리기·점프, Esc 직후 착지를 확인합니다. Animator의 상태 이름과 함께 실제 발·몸통 움직임도 봅니다.

일반 점프 연결 개선은 후속 요청으로 적용됐습니다. 현재 값과 조절 방법은 위의 최신 연결 안내를 따릅니다.

## Animator에서 애니메이션 연결 수정하기 (2026-10-08)

현재 `PlaytestScene01`의 애니메이션은 **Animator가 전환하고 Animation 창에서 클립을 편집**합니다. 스크립트는 이동·점프·사격의 상태값만 전달합니다. 같은 본을 두 번 제어하지 않도록 구형 `Animation` 컴포넌트를 따로 추가하지 않습니다.

1. Project에서 `Assets/04Prefabs/Player/Animations/PlayerJump.controller`를 더블클릭합니다. 또는 씬의 Player를 선택하고 `Window > Animation > Animator`를 엽니다.
2. `Base Layer`에는 `Idle`, `Walk`, `Run`, `Jump`가 있습니다. 상태 사이 화살표를 선택하면 `Conditions`에서 전환 조건, `Transition Duration`에서 부드럽게 바뀌는 시간을 수정할 수 있습니다. 현재 이동 전환은 0.12초, 점프 진입은 0.08초, 복귀는 0.15초입니다. 위의 최신 점프 연결 설명을 함께 확인합니다.
3. `Shotgun` 레이어에는 `Ready`와 `Fire`가 있습니다. 기존 사격 클립의 총 들기·반동 값 세 가지만 재생하므로 이동·점프와 함께 작동합니다. 두 상태 사이 전환 시간은 0초로 두어 실제 발사 준비 시각과 어긋나지 않게 합니다.
4. 실제 자세를 고치려면 씬의 Player를 선택한 채 `Window > Animation > Animation`을 열고 클립을 선택합니다. 상태의 `Motion` 칸이 재생할 클립의 연결입니다. 모델·본 이름과 기존 클립의 연결을 바꾸지 않습니다.

| Animator 매개변수 | 뜻 |
| --- | --- |
| `Movement` | 0=대기, 1=걷기, 2=달리기 |
| `JumpActive` | 준비·공중·착지 동작 중인지 |
| `JumpTime` | 점프 클립에서 현재 보여 줄 위치(0~1) |
| `ShotActive` | 사격 준비·반동 동작 중인지 |
| `ShotTime` | 사격 클립에서 현재 보여 줄 위치(0~1) |

점프와 사격의 `Motion Time`은 위 매개변수에 연결합니다. 점프는 실제 바닥에 닿을 때 착지해야 하고, 사격은 총구가 앞으로 향할 때까지 준비 자세를 기다려야 하기 때문입니다. `Has Exit Time`을 켜거나 클립이 시간만으로 자동 종료되도록 바꾸면 이 동작이 달라집니다. 본 모션과 무관한 중력·접지·조준점 계산은 계속 물리/게임 코드가 맡습니다.

전환의 `Ordered Interruption`은 켜 둡니다. 전환 도중 같은 조건으로 계속 전환을 다시 시작하면 이전 자세에 멈출 수 있어, 현재 구성에서는 전환 순서를 지키도록 설정했습니다.

`Animation/PlayerAnimation.cs`는 이동·점프 값을 전달하는 연결부, `Animation/ShotgunPose.cs`는 사격 값을 전달하고 조준점으로 총을 맞추는 연결부입니다. 현재 컨트롤러에서 직접 `Play`, `CrossFade`, `SampleAnimation`으로 상태를 바꾸거나 클립을 강제 재생하지 않습니다. 다른 씬이 공유하는 옛 `Idle.controller`는 그대로 보존했으므로 그 컨트롤러에서만 최소한의 예전 재생 경로를 유지합니다. 관련 파일이나 메타를 삭제하면 공유 연결에 영향을 줄 수 있어 파일을 없애지 않고 내부 역할을 정리했습니다.

사격 클립을 교체할 때는 `Shotgun > Ready > Motion`, `Fire > Motion`과 Player의 `ShotgunPose > Fire Motion`을 같은 클립으로 맞춥니다. Ready는 속도0과 위치0 고정을 유지하고, Fire는 ShotTime으로 재생합니다. 컴포넌트의 클립 필드는 길이와 실제 발사 준비 시각 검사에 사용합니다. `raiseWeight`, `recoilDistance`, `recoilPitch` 세 곡선의 이름과 컴포넌트 바인딩을 유지하며 0초에는 모두0으로 둡니다.

## Space 점프 사용과 수정 (2026-10-08 적용)

`PlaytestScene01`에서 재생한 뒤 Game 화면을 클릭하고 **Space를 한 번** 누릅니다. 약 0.4초 몸을 낮춘 뒤 뛰고, 실제 바닥에 닿으면 착지 자세를 거쳐 대기·걷기·달리기로 돌아옵니다. Space를 계속 눌러도 자동으로 다시 뛰지 않습니다. 공중 재점프는 지원하지 않습니다. Esc로 커서를 풀면 이동·점프 입력을 받지 않습니다.

몸통은 준비·착지 때 세로 2/3까지 눌리고, 도약 때 최대 1.22배 늘어나는 승인된 클립을 사용합니다. 손·귀·꼬리의 지연 동작도 이 클립에 있습니다. 꼬리에는 별도 보정이 있지만 기존 본 구조에서 전체 부피가 완전히 보존된다는 뜻은 아닙니다. 총을 든 동안에는 기존 양손 손잡이 고정이 손의 아래쪽 지연 동작보다 우선합니다.

### 높이와 연결을 바꾸는 곳

1. Hierarchy의 `Cat_Player` 본체에서 `PlayerController > 점프 (Space) > Jump Settings`를 펼칩니다.
2. `Enabled`는 이 씬에서 켜져 있습니다. `Height`는 목표 높이이며 현재 0.45입니다. 실제 높이는 중력과 물리 갱신 간격에 따라 조금 작아집니다. `Max Ground Angle`은 바닥으로 인정할 최대 기울기(현재 55도), `Ground Layers`는 검사할 충돌 레이어입니다.
3. Animator의 Controller는 `Assets/04Prefabs/Player/Animations/PlayerJump.controller`입니다. 기존 Idle/Walk/Run에 Jump 상태를 더한 사본입니다. 기존 원본 컨트롤러는 그대로 보존했습니다.
4. 자세는 같은 폴더 `Jump.anim`에서 수정합니다. 이 클립은 몸체를 월드 위로 이동시키지 않습니다. 실제 높이를 클립에도 넣으면 Rigidbody 상승과 겹치므로 넣지 않습니다.
5. 수정 뒤 제자리·이동 중 점프, 턱 위/아래 착지, 총·물건 보유, Space 유지/재입력, Esc 후 재진입을 확인합니다. 다른 씬이나 프리팹 원본으로 적용 범위를 넓히지는 않습니다.

### 코드가 나누어 맡은 일

파일은 모두 `Assets/02Scripts/01Player/` 안에 있습니다. 새 컴포넌트를 Player에 붙일 필요는 없습니다.

| 파일 | 담당 |
| --- | --- |
| `Input/PlayerInput.cs` | Space를 새로 누른 순간을 읽고 기존 WASD·Shift와 함께 전달 |
| `Domain/JumpPolicy.cs` | 준비·공중·착지 순서, 중복 점프 방지. Unity가 없어도 검사할 수 있는 판단 규칙 |
| `Movement/PlayerJump.cs` | 현재 바닥과 수직 속도를 Unity 물리에서 읽음. 기존 Y방향 CapsuleCollider 구조 사용 |
| `Movement/PlayerMovement.cs` | 이륙 때 한 번 상승 속도를 적용. 이후 중력은 Rigidbody에 맡김 |
| `Animation/PlayerAnimation.cs` | 이동 상태·점프 활성 여부·클립 위치를 Animator에 전달. 상태 전환은 Controller에서 처리 |
| `Core/PlayerController.cs` | 위 기능들을 기존 Update/FixedUpdate 실행 순서에 연결 |

공중 자세는 고정된 시간만 기다려 착지하지 않고 실제 바닥 검사 결과를 기다립니다. 기존 작은 턱 넘기는 점프 중 잠시 중단하고 일반 이동 때 그대로 사용합니다. 준비/착지 시간을 바꾸려면 `JumpPolicy.cs`의 시점 상수와 `Jump.anim`의 해당 자세 시점을 함께 맞춰야 합니다. Game 화면의 색은 기존 모델·재질·팔레트를 사용합니다.

## F키로 물건 보관하기, 쥐 포획하기

이 부분은 현재 코드의 사용 방법입니다. 실제 실행 검증 결과는 작업 로그에서 따로 확인합니다.

화면 가운데 포인터를 **등록된 대상**에 맞춘 뒤 F를 한 번 누릅니다. 기본 상호작용 거리는 2.2입니다. 벽에 가린 대상이나 너무 멀리 있는 대상은 선택하지 않습니다. F는 누른 채 유지하는 방식이 아니라 한 번 누를 때 한 번 작동합니다.

| 현재 상황 | F를 누르면 |
| --- | --- |
| 손이 비어 있고 일반 물건을 보고 있음 | 왼쪽부터 비어 있는 인벤토리 칸에 보관합니다. 손은 계속 비어 있습니다. |
| 총을 들고 일반 물건을 보고 있음 | 새 물건만 빈 칸에 보관하고 총 장착은 유지합니다. |
| 좌클릭으로 물건을 들고 다른 일반 물건을 보고 있음 | 기존 물건은 계속 들고 새 물건만 빈 칸에 보관합니다. |
| 등록된 대상을 보고 있지 않음 | 아무 동작도 하지 않습니다. 손에 든 물건을 놓거나 보관품을 꺼내지 않습니다. |
| 일반 물건을 보고 있지만 5칸이 모두 찼음 | 획득하지 않습니다. 대상 물건·기존 인벤토리·손 상태를 유지합니다. |
| 등록된 쥐를 보고 있음 | 포획합니다. 일반 물건용 인벤토리 빈칸과는 관계없습니다. |

저장이 성공하면 새 물건의 월드 원본만 비활성화합니다. 저장에 실패하면 원본은 남습니다. F는 손 상태를 바꾸지 않으며, 같은 물건이 월드와 인벤토리에 동시에 남지 않도록 이미 획득한 대상을 다시 획득하지 못하게 합니다.

인벤토리 UI와 보관품을 꺼내는 입력은 아직 정하지 않아 추가하지 않았습니다. 숫자 1은 기존 샷건 장착·해제에만 사용합니다.

### 기존 조작과 함께 쓰기

- 기존 **좌클릭 유지로 들기 → 좌클릭을 떼면 놓기**는 유지합니다. 놓는 순간의 위치·회전은 바꾸지 않고 기존 Rigidbody의 중력으로 떨어뜨립니다.
- 샷건의 **좌클릭 발사·우클릭 조준·숫자 1 장착/해제**도 유지합니다.
- 좌클릭으로 들던 물건이 있을 때 숫자 1을 누르면 현재 위치에서 놓고 샷건을 장착합니다. F로 저장한 물건은 손에 들지 않았으므로 인벤토리에 그대로 남습니다.
- 같은 순간에 F와 기존 손 조작 키를 누르면 F를 먼저 처리해 한 입력으로 두 동작이 겹치지 않게 합니다.
- `HotbarManager > Read Selection Input`은 **꺼 둡니다**. 켜면 핫바의 숫자 1 선택과 샷건의 숫자 1 입력이 함께 읽힙니다. 나중에 UI 버튼이나 `Select(칸 번호)`로 칸을 선택하는 것은 이 설정이 꺼져 있어도 가능합니다.

### 대상 등록과 설정 위치

`PlaytestScene01`에서 플레이어 **본체**의 `InteractionController`, `ItemInteractor`, `HotbarManager`를 확인합니다. 플레이어 아래 빈 자식 오브젝트로 모델과 스크립트를 분리하는 방식은 사용하지 않습니다. 기존 `Assets/02Scripts/04Systems/ItemInteractor.cs`와 `HotbarManager.cs`를 재사용하며, 운반과 판단 규칙은 `Assets/02Scripts/01Player`의 기존 파일에 있습니다.

1. `ItemInteractor > Interaction`에는 같은 플레이어의 `InteractionController`를 연결합니다. `Inventory`에는 사용할 `HotbarManager`를 연결합니다.
2. `InteractionController > Registered Interaction`에는 위 `ItemInteractor`를 연결합니다.
3. `ItemInteractor > Targets`에 항목을 추가하고 `Target`에 월드 물건 또는 쥐의 루트를 넣습니다. 루트란 함께 보관하거나 포획할 물체의 가장 위쪽 오브젝트입니다. 플레이어 본체나 플레이어가 포함된 부모는 대상이 될 수 없습니다.
4. `Kind`는 물건이면 `Item`, 쥐면 `Enemy`로 선택합니다. `Item`에는 기존 `HotbarItemData`가 있으면 연결하고, 없으면 `Display Name`을 적습니다. 데이터가 비어 있을 때 만드는 이름 데이터는 현재 실행 중에만 사용합니다.
5. 포인터가 맞을 수 있도록 대상 또는 그 자식에 **활성화된 Collider**가 있어야 합니다. 등록한 물체의 Trigger도 선택하지만, 관련 없는 Trigger는 선택이나 벽 판정에 사용하지 않습니다. 기존 콜라이더 모양을 바꾸거나 등록만을 위해 새 물리 컴포넌트를 자동 추가하지 않습니다.

기존 `ItemPickup`에 아이템 데이터가 연결된 물체는 `Use Item Pickup Registration`이 켜져 있으면 자동으로 등록 대상으로 인식합니다. 실행 중 생성한 물건은 `Register(new InteractionRegistration { ... })`로 등록하고 `Unregister(대상)`으로 해제할 수 있습니다. 이미 획득하여 예약된 대상의 등록 해제는 거부합니다. 인스펙터의 `Targets`를 실행 중 바꿨다면 `RebuildRegistrations()`를 호출해야 목록이 다시 반영됩니다.

`Pickup Range`는 F 상호작용 거리입니다. `InteractionController > Hold Distance`는 좌클릭으로 드는 거리이며 F 보관에는 영향을 주지 않습니다. 좌클릭 운반은 외형 전체를 감싼 경계로 검사하므로 큰 물건이 벽·선반에 보수적으로 막힐 수 있습니다.

현재 씬에는 원통 4개·큐브 2개·덫 1개와 쥐 2마리를 등록했습니다.

### UI 담당자와 연결하기

새 F 기능의 5칸 데이터 저장은 **UI가 없어도** 동작합니다. 기존 좌클릭 큐브의 '등록된 UI로 날아가는 연출'과는 별도입니다. 그 기존 연출은 UI 목적지가 등록되어야 실행되며, F 기능이 임의의 UI 목적지를 만들지는 않습니다.

아래는 UI 담당자가 나중에 사용할 수 있는 기존 연결 지점입니다. 이번 작업에는 인벤토리 UI나 새 꺼내기 입력을 추가하지 않았습니다. `HotbarUI`는 `HotbarManager.Instance`와 아래 이벤트를 사용하며, 아이템 데이터에 `Icon`이 없어도 저장 칸은 사용됩니다.

| 연결 지점 | 담당자가 할 일 |
| --- | --- |
| `GetSlot(0~4)` | 각 칸의 현재 아이템을 읽습니다. 화면의 1번 칸은 코드에서 0번입니다. |
| `OnSlotChanged` | 알려 준 칸의 아이콘·이름을 다시 그립니다. |
| `OnSelectedChanged` | 선택 테두리를 갱신합니다. |
| `Select(0~4)` | UI 버튼으로 선택 칸을 바꿉니다. 현재 구현에서 선택만으로 물건을 다시 꺼내 들지는 않습니다. |
| `TrySetInventory(IItemInventory)` | 다른 담당자의 저장 시스템으로 바꿉니다. 획득 처리 중에는 교체를 거부합니다. `null`이면 연결된 기본 HotbarManager로 돌아갑니다. |

다른 저장 시스템을 연결하려면 `IItemInventory`의 `HasSpace`, `GetSlot`, `TryAdd`, `TryRemove`를 구현합니다. `TryAdd`가 실패하면 칸을 바꾸지 않아야 하며, 성공했을 때만 월드 원본을 숨깁니다. 기존 계약인 `TryRemove(칸 번호, 예상 아이템)`는 그 칸에 예상한 아이템이 있을 때만 제거해야 합니다. 현재 F 획득은 저장만 수행하며, 이 제거 함수를 새 꺼내기 입력에 연결하지 않습니다.

쥐 포획은 기존 `ThiefController.GetCaptured()`를 사용합니다. 기존 쥐가 들고 있던 보물을 내려놓는 처리를 거친 뒤 쥐가 제거되고, 등록된 대상은 비활성화됩니다. 현재 포획 경로는 기절 여부로 제한하지 않으며 **포획 수·점수·라운드 판정은 추가하지 않았습니다**. 다른 담당자는 `EnemyCaptured` 이벤트를 받아 나중에 필요한 표시를 연결할 수 있습니다.

현재 인벤토리는 실행 중의 상태만 보관합니다. Play 종료·게임 종료 후의 영구 저장, 인벤토리 UI, 보관된 물건을 선택해 다시 꺼내기, 사용 효과, 최대 보유량을 넘는 추가 보관은 구현하지 않았습니다. 기능을 바꾼 뒤에는 빈손·총 장착·좌클릭 보유 각각에서 F 저장 후 손 상태가 같은지, 5칸 가득 참과 빈 공간 F에서 상태가 유지되는지 확인합니다. 좌클릭 해제의 자연 낙하, 쥐 포획, 숫자 1 장착도 따로 확인합니다.


## 현재 열어야 할 프로젝트와 파일

현재 Unity 프로젝트는 `C:/Users/307/Desktop/unity/Team3rd3DProject/3rd3DProject`입니다. 작업 씬은 `Assets/01Scenes/PlaytestScene01.unity` 하나입니다. 아래에 남긴 날짜별 검증·이전 기록은 당시 기록이며, 현재 경로는 이 안내를 우선합니다.

| 대상 | 현재 위치 |
| --- | --- |
| Player C# 코드 | `Assets/02Scripts/01Player/` |
| F 진입점·5칸 저장소 | `Assets/02Scripts/04Systems/ItemInteractor.cs`, `HotbarManager.cs` |
| 캐릭터 모델 | `Assets/04Prefabs/Player/Models/Cat_Player00.fbx` |
| 플레이어 프리팹 | `Assets/04Prefabs/Player/Prefabs/Cat_Player (1).prefab` |
| 기존 Idle·Walk·Run 클립 | `Assets/04Prefabs/Player/Animations/` |
| 들기·획득 아이템과 강조 재질 | `Assets/04Prefabs/Player/Interaction/` |
| 샷건 모델·프리팹·색상 재질 | `Assets/04Prefabs/Player/Weapons/Shotgun/` |
| 플레이어 물리 재질 | `Assets/04Prefabs/Player/Physics/Player_NoFriction.physicMaterial` |
| 발사 클립 | `Assets/03Sprites/Player/Animations/Shotgun_Fire.anim` |
| 산탄 원 재질 | `Assets/03Sprites/Player/Weapons/Shotgun/Shotgun_SpreadRing.mat` |

현재 작업은 기존 ItemInteractor에 F 획득을 확장하고 승인된 5칸 저장소를 연결하는 범위입니다. 아래 이름 변경 설명은 앞선 작업 기록입니다. 사용자가 요청한 C# 40개의 이름을 간단하게 바꾸되 폴더 구조를 유지합니다. 짝이 되는 `.meta`는 경로만 함께 바꾸고 내용과 GUID는 그대로 보존합니다. 다른 파일의 정리·이동·삭제·이름 변경은 포함하지 않습니다. Rat·Enemy·Mouse 코드, 다른 씬과 프로젝트 설정도 수정하지 않습니다. 과거 삭제 승인이나 다른 프로젝트 사본을 자동 복원 근거로 사용하지 않습니다.

## 조작 방법

| 입력 | 동작 |
| --- | --- |
| WASD | 걷기 |
| Shift + WASD | 달리기. 샷건 줌 중에는 걷기 |
| 이동 키를 누르지 않음 | 대기 |
| 마우스 이동 | 정지: 멈춘 방향에서 좌우60도 / 이동: 수평360도. 상하 범위는 기존과 같음 |
| 빈손에서 마우스 좌클릭을 누르고 유지 | 중앙 커서가 가리키는 물건을 화면 중앙에 들기 |
| 빈손 집기에 사용한 좌클릭에서 손을 뗌 | 즉시 보유 종료·내려놓기 |
| 등록 물건에 포인터를 맞추고 F | 새 물건만 인벤토리에 보관. 기존 손 상태 유지 |
| 숫자 1 | 기존 빨강·파랑 샷건을 즉시 양손으로 잡기 / 다시 누르면 해제 |
| 샷건 장착 중 우클릭 유지 | 줌·조준, 이동은 걷기로 고정. 떼면 기본 시야로 복귀 |
| 샷건 장착 중 좌클릭 | 한 번 누를 때 4발 산탄 발사. 누르고 있어도 연속 발사하지 않음 |
| Esc / 왼쪽 클릭 | 마우스 잠금 해제 / 다시 잠금. 재잠금 클릭으로는 집기·발사하지 않음 |

빈손 우클릭은 물건을 집지 않습니다. Esc로 마우스를 푼 뒤 화면을 다시 잠그는 좌클릭도 집기에 쓰지 않습니다. 다시 잠근 다음 새로 클릭해야 집습니다.

실린더는 좌클릭을 새로 누를 때 집고 버튼을 누르는 동안만 듭니다. 장애물이 없으면 물건의 중심이 중앙 포인터와 겹칩니다. 벽에 걸리거나 선반에서 빠져나오는 동안에는 충돌을 피하는 위치가 우선합니다. 버튼을 떼면 그 순간의 위치·회전을 유지한 채 손을 비우고 중력으로 놓습니다. 바닥으로 순간 이동시키거나 공중에 멈춘 채 공간이 생기기를 기다리지 않습니다. 기존 좌클릭 큐브는 등록된 UI로 날아가는 연출이므로 UI가 없으면 남습니다. 별도의 F 입력은 UI 없이 해당 물건을 5칸 저장소에 보관합니다.

Collider는 물건의 **충돌 모양**이고, Rigidbody는 **중력과 물리 움직임을 계산하는 몸체**입니다. Collider만으로 중력이 생기지는 않습니다. 내려놓을 때 기존 Rigidbody의 `Is Kinematic`을 끄고 `Use Gravity`를 켜며, 속도를 0으로 만든 뒤 Unity의 물리 계산에 맡깁니다. Collider의 기존 활성 상태는 복원하고 새 Rigidbody나 자체 중력 스크립트를 추가하지 않습니다.

좌클릭으로 물건을 든 상태에서 1을 누르면 현재 위치에서 놓고 샷건을 잡습니다. 바닥 탐색이나 착지 완료를 기다리지 않습니다. 총을 이미 들었을 때 1을 또 누르면 총을 숨기고 원래 손 동작으로 돌아갑니다. 총 장착 중에는 좌클릭 물건 집기를 막지만 F 인벤토리 저장은 가능합니다. 소환 효과·소환 애니메이션은 사용하지 않습니다. 발사는 아래 조준·발사 기능에서 처리합니다. 외부 입력·버튼에서는 `InteractionController.TryToggleEquipment()`를 사용합니다. `ShotgunEquipment`는 장비와 손 자세만 맡으므로 그 안에 입력 검사나 아이템 내려놓기 규칙을 넣지 않습니다.

## 어디에 무엇이 있나요?

씬은 `Assets/01Scenes/PlaytestScene01.unity`만 사용합니다. 플레이어 코드는 `Assets/02Scripts/01Player` 아래에 있으며 F 획득과 저장소는 앞서 적은 기존 `04Systems/ItemInteractor.cs`, `HotbarManager.cs`를 재사용합니다. 폴더는 기능을 나누어 담는 서랍이라고 생각하면 됩니다.

| 폴더·파일 | 맡은 일 |
| --- | --- |
| `Core/PlayerController.cs` | 입력·이동·동작 재생을 연결하는 중심 |
| `Input/PlayerInput.cs` | WASD와 Shift를 이동 의도로 바꿈 |
| `Movement/` | 실제 이동과 작은 턱 넘기 |
| `Animation/PlayerAnimation.cs` | 대기·걷기·달리기 선택 |
| `Camera/` | Cinemachine 마우스 입력과 커서 잠금 |
| `Interaction/InteractionController.cs` | 중앙 조준 검사, 순수 손 규칙 결과를 들기·장착 기능에 연결 |
| `Interaction/ItemCarrier.cs` | 선반에서 꺼내기, 벽을 검사하며 들고 이동하기, 현재 위치에서 중력으로 놓기 |
| `Interaction/CatInteractionItem.cs` | 물건 종류와 교체 가능한 외형 지정 |
| `Interaction/Presentation/` | 중앙 커서, 노란 강조, 인벤토리로 날아가는 표시 |
| `Equipment/ShotgunEquipment.cs` | 총 표시·숨김과 양손 위치 맞추기. 입력을 직접 읽지 않음 |
| `Editor/Setup/EquipmentSetup.cs` | 기존 샷건 에셋을 연결. 이미 조절한 자세는 보존 |
| `Editor/Validation/InteractionValidation.cs` | 변경된 들기·1번 토글·UI·카메라·마찰을 Play에서 검사 |
| `Domain/` | Unity 없는 이동·손 사용·카메라 제한 규칙 |
| `Input/InteractionInput.cs` | 좌클릭 유지 상태와 숫자1을 단순한 값으로 변환 |
| `Camera/CameraOrbitLimit.cs` | 순수 회전 범위 규칙을 Cinemachine 수평 축에 적용 |

샷건 모델과 프리팹은 `Assets/04Prefabs/Player/Weapons/Shotgun/`에 있습니다. `Shotgun01_RedBlue.fbx`가 모델이고, `Shotgun_Ready.prefab`은 외형과 양손 기준점을 묶은 재사용 묶음입니다. 같은 폴더의 빨강 총열·파랑 손잡이 등의 `.mat` 파일은 색상을 맡습니다. 산탄 범위 원 재질만 `Assets/03Sprites/Player/Weapons/Shotgun/Shotgun_SpreadRing.mat`에 있습니다.

Editor의 Setup 도구는 현재 연결을 재사용하고, 연결이 비었을 때 기존 GUID로 에셋을 찾습니다. GUID는 파일의 식별 번호이므로 폴더 이름에 기대어 새 복사본을 만들 필요가 없습니다. 도구는 새 에셋·메타를 만들거나 파일을 옮기지 않고, 전체 `SaveAssets()`도 호출하지 않습니다. 수동 연결 메뉴는 `PlaytestScene01`의 연결을 변경하고 해당 씬을 저장하므로, 연결 복구가 필요한 경우에만 사용합니다.

## 어떻게 움직이나요?

1. 카메라 정중앙에서 눈에 보이지 않는 검사 선을 쏩니다. 가까운 물건인지, 벽 뒤에 있지는 않은지 확인합니다.
2. 빈손이고 좌클릭을 새로 눌렀으면 기존 집기 기능을 호출합니다. `HandPolicy`가 집기·놓기·장착·해제 중 어떤 일을 할지 결정합니다. 입력 장치 자체는 `InteractionInput`만 읽습니다.
3. 숫자 1은 장착 담당 코드로 들어갑니다. 연결이 올바른지 먼저 확인하고, 좌클릭으로 들고 있던 물건을 현재 위치에서 놓은 뒤 장착합니다. F 저장품은 손에 없으므로 그대로 보관합니다.
4. 총은 미리 연결한 하나의 모델을 켜서 표시합니다. 매번 새 총을 만들지 않습니다. 예전 소환 동작의 마지막 잡는 자세를 참고했지만 소환 애니메이션 자체는 가져오지 않았습니다.
5. Animator가 몸·꼬리·발의 기존 동작을 계산한 뒤, `LateUpdate`에서 양손 뼈만 `LeftGrip`·`RightGrip` 위치에 맞춥니다. Grip은 손이 놓일 기준점입니다. 다음 프레임에는 이전 손 보정을 풀고 다시 계산하여 위치가 계속 틀어지지 않게 합니다.

강조할 모델은 `IHighlightSource`라는 공통 약속으로 찾습니다. 기존 아이템의 예약·사용 불가 상태를 먼저 확인하므로 강조용 컴포넌트로 집기 제한을 우회하지 않습니다. `ItemHighlight`는 최근 모델의 강조용 메시를 보관해 조준을 잠깐 벗어났다가 돌아와도 재사용합니다. 대상 교체·외형 변경·대상 비활성화·파괴 때 임시 메시를 정리하며, 상호작용 컨트롤러가 비활성화될 때도 모두 해제합니다. 원본 모델 메시와 재질은 수정하지 않습니다.

## 직접 바꾸는 방법

아래는 해당 항목의 변경을 요청받았을 때 참고하는 설명입니다. 이번 재구현에서는 기존 모델·재질·메타 파일을 보존합니다.

### 샷건 위치 또는 손 위치

1. Play를 끕니다. Hierarchy에서 `Cat_Player`를 선택합니다.
2. `Shotgun Equipment`의 `Weapon Local Position`으로 총의 위치, `Weapon Local Euler Angles`로 기울기를 바꿉니다. 이 값은 몸통 뼈를 기준으로 합니다. Unity 좌표의 Y가 위·아래입니다.
3. 손만 옮기려면 `Cat_Player > Cat_Shotgun > LeftGrip / RightGrip`의 위치를 조절합니다. 왼손은 앞쪽 파란 손잡이, 오른손은 뒤쪽 파란 손잡이에 놓습니다.
4. Play에서 숫자 1을 누르고 대기·걷기·달리기, 정면·측면을 모두 봅니다. Play 중 수정한 값은 Play 종료 시 돌아가므로 편집 모드에서 적용합니다.

### 샷건 외형 또는 색

- `Shotgun_Ready.prefab`의 `Visual` 외형을 교체하되 `LeftGrip`·`RightGrip` 기준점과 컴포넌트 연결은 유지합니다. 총 모델의 시작 위치는 뒤 손잡이 피벗에 맞춥니다. 모델이 바뀌면 양손 기준점도 다시 조절합니다.
- `Barrel_Red.mat`는 총열, `Grip_Blue.mat`는 파란 손잡이, `Receiver_DeepBlue.mat`는 가운데 몸체, `Muzzle_Dark.mat`는 총구 색입니다. URP Unlit 재질로 캐릭터처럼 평평한 색을 표시합니다.
- 연결을 잃었으면 `Tools > Cat Player > 샷건 연결 확인 및 설치`를 사용합니다. 허용된 씬 편집 모드에서만 실행하며, 이미 연결된 총의 자세는 덮어쓰지 않습니다.

### 집기 거리와 모델 교체

- `Cat_Player > Interaction Controller > Reach`가 플레이어 기준 집기 거리입니다.
- 물건의 `Visual` 아래 외형만 바꾸고 루트의 `CatInteractionItem`, `BoxCollider`, `Rigidbody`는 유지합니다. 컴포넌트 메뉴의 `Visual 크기에 맞춰 충돌 갱신`을 실행합니다.
- 물건이 아닌 일반 모델을 노랗게 강조하려면 `HighlightTarget`과 기존 비트리거 Collider를 사용합니다. 강조만 있는 모델은 자동으로 집을 수 있는 물건이 되지 않습니다.

### 인벤토리 담당자 연결

기존 좌클릭 큐브 연출은 `InventoryPickupEffect > Inventory Target`에 실제 UI의 RectTransform을 등록합니다. 코드에서는 `RegisterTarget(...)`을 사용할 수 있습니다. 도착 시 `Collected` 이벤트로 물건 식별자가 한 번 전달되고 저장은 별도 담당 코드가 처리합니다. 등록된 UI가 없거나 도중에 사라지면 연출이 물건을 없애지 않습니다. F 획득은 이 UI 연출과 별도로 기존 5칸 저장소를 사용합니다.

## 수정 뒤 확인하기

`PlaytestScene01`만 열고 Play한 뒤 `Tools > Cat Player`의 해당 검증 메뉴를 실행합니다. 자동 검증은 잠시 입력과 카메라를 제어하므로 실행 중 직접 조작하지 않습니다. 이동·상호작용·전투·실제 입력 검증은 한 번에 하나씩 실행합니다. 종료 시 Play가 끝나며 결과는 프로젝트 밖 `%TEMP%/PlayerValidation`에 남습니다. 파일 탐색기 주소창에 이 경로를 넣으면 열 수 있습니다. 마지막 `RESULT`와 실패 항목을 확인합니다. 확인되지 않은 결과를 성공으로 기록하지 않습니다.

손으로도 빈손 좌클릭 유지·해제, 물건을 든 채 1, 다시1로 해제, 총을 든 채 이동, 정지 좌우60도·이동360도를 확인합니다. 다른 씬·다른 담당자의 스크립트·기존 애니메이션 에셋은 이 수정에 포함하지 않습니다.


## 다른 Unity 버전이나 엔진으로 옮기는 방법

규칙과 실행 도구를 나눴습니다. 예를 들어 “버튼을 떼면 물건을 놓는다”는 게임 규칙이고, 실제 물체를 중력으로 떨어뜨리는 것은 엔진의 역할입니다. 콘센트가 바뀔 때 기기 전체 대신 어댑터를 바꾸는 것과 비슷합니다.

| 부분 | 지금 역할 | 엔진을 바꾸면 |
| --- | --- | --- |
| `MovementPolicy`, `HandPolicy`, `OrbitPolicy` | 이동 상태·손 사용·카메라 각도 판단. 일반 C# 값만 사용 | C# 지원 엔진에서는 규칙 파일 재사용, 다른 언어라면 같은 규칙 번역 |
| `IMotionState`, `IEquipmentPort` | 다른 기능에 필요한 최소 약속 | 새 연결부가 같은 약속을 지키도록 구현 |
| Input·PlayerController·PlayerMovement·PlayerAnimation | 실제 키 입력, Rigidbody, Animator 연결 | 해당 엔진의 입력·몸체·애니메이션 API로 교체 |
| StepSolver·ItemCarrier | 충돌·바닥·벽 검사와 물체 이동 | 새 엔진의 레이/형상 검사·물리 API로 교체 |
| 카메라 연결부 | 기존 Cinemachine에 순수 각도 제한을 전달 | 새 엔진 카메라에 같은 각도 규칙 연결 |
| 강조·아이콘 촬영·중앙 포인터·UI 연출 | Unity 렌더·Canvas·셰이더 표현 | 해당 엔진의 화면 표시 기능으로 교체 |

이동·손 사용·회전 정책은 Unity DLL 없이 독립 컴파일해 검사합니다. 물리·UI·렌더 코드 전체가 엔진 독립인 것은 아닙니다. 기존 `IPlayerInputSource`, `IStepSolver`, `IHighlightSource`와 `IPickupPort`는 Unity 타입을 포함한 연결용 인터페이스입니다. 복잡한 범용 물리·UI 프레임워크를 추가하지 않고 교체할 부분을 분명하게 유지합니다. 실제 다른 엔진에 옮겨 실행한 상태는 아닙니다.

### 값을 바꿀 위치

- 마찰: `Assets/04Prefabs/Player/Physics/Player_NoFriction.physicMaterial`. Static/Dynamic Friction=0, Combine=Multiply. 플레이어 충돌체에 연결합니다. 프로젝트 전체 물리 설정은 바꾸지 않습니다. 이동 키를 뗐을 때 멈추는 감속은 마찰과 다른 이동 제어이므로 유지합니다.
- 보유 거리: `Cat_Player > Interaction Controller > Hold Distance`. 물건 중심은 카메라 중앙 조준선에 두고 이 거리만 조절합니다.
- 정지 회전 폭: `Cat_CinemachineCamera > Camera Orbit Limit > Stationary Half Angle`. 60이면 왼쪽60도+오른쪽60도입니다. 멈출 때마다 기준 방향을 새로 잡습니다.
- 손 사용 규칙: `Domain/HandPolicy.cs`. 장비 종류를 바꾸려면 입력 코드를 복제하는 대신 `IEquipmentPort` 구현을 연결합니다.
- 이동 속도: PlayerController의 Walk Speed/Run Speed. 어떤 상황에 어떤 속도를 선택하는지는 `Domain/MovementPolicy.cs`입니다.
- 독립 규칙 검사: `Tools > Cat Player > 순수 규칙 검증`. 결과와 실제 Play 검사를 둘 다 확인합니다.
- 해제 후 자연 낙하: `ItemCarrier`가 현재 위치에서 Collider와 기존 Rigidbody를 해제합니다. 공중 복구 대기는 사용하지 않습니다. `CatInteractionItem.IsReleasePending`과 예전 저장 형식은 연결 보존을 위해 남겼으며, 예전 잔여 상태가 있으면 즉시 물리를 복구합니다.

### 구현 전에 확인한 공식 자료

[물리 재질](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PhysicsMaterial.html), [마찰 결합 순서](https://docs.unity3d.com/6000.0/Documentation/Manual/collider-surfaces-combine.html), [버튼 유지 입력](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.Controls.ButtonControl.html), [중앙 조준선](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.ViewportPointToRay.html), [Cinemachine 회전 범위](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineOrbitalFollow.html)를 확인했습니다. 다른 엔진의 구현 가능성까지 검증한 것은 아닙니다.

자연 낙하는 Unity의 [Rigidbody 물리 제어](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody-isKinematic.html), [Use Gravity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody-useGravity.html), [WakeUp](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody.WakeUp.html)을 사용합니다. 놓는 순간 기존 몸체를 물리 시뮬레이션에 다시 참여시키며, 별도 낙하 위치나 중력 계산을 만들지 않습니다.

## 현재 검증 결과 (2026-09-30)

순수 규칙27개, 실제 상호작용38개, 기존 이동19개를 통과했습니다. 물건 중심과 포인터의 측정 오차는0.135픽셀이었고, 정지좌우60도와 이동360도도 실제 입력으로 확인했습니다. 자세한 결과는 `PLAYER_WORK_LOG.md`와 `Logs/CatInteractionValidation_20260930_174604_982.txt`, `Logs/CatPlayerValidation.txt`에 있습니다.

자동 검증에서 만드는 UI·가림용물체·강조용물체는 검사 종료 시 직접 제거합니다. 실제 사용자 인벤토리 UI는 추가하지 않았습니다. 씬의 Inventory Target에 담당자가 만든 UI를 연결해야 큐브 획득 연출이 실행됩니다.

## Package Manager의 Duplicate key 오류 (2026-10-06)

`Packages/manifest.json`은 프로젝트가 사용하는 패키지 목록입니다. 이번에는 `dependencies` 안에 `"com.unity.behavior": "1.0.16"`이 두 줄 있어 같은 줄 하나만 남겼습니다. 패키지 버전은 바꾸지 않았습니다.

다시 같은 오류가 나면 오류 메시지에 나온 패키지 이름을 이 파일에서 검색합니다. 같은 목록에 이름이 두 번 있고 버전도 같다면 중복된 줄만 정리합니다. 버전이 서로 다르면 임의로 선택하지 말고 필요한 버전을 먼저 확인합니다. 수정 후에는 JSON 문법과 중복 키를 함께 검사합니다. 최초 검사 이후 아래 후속 작업에서 Unity 내부 패키지 처리와 Play 실행까지 확인했습니다.

### 후속 정상화와 확인 방법 (2026-10-06)

패키지는 프로젝트가 사용하는 기능 부품입니다. `manifest.json`은 필요한 부품 목록이고, `packages-lock.json`은 Unity가 계산한 정확한 연결 기록입니다. 이번에는 연결 기록에도 `depth`가 두 번 쓰여 있었고, 카메라에 필요한 Cinemachine은 목록에서 빠져 있었습니다.

중복된 depth는 실제 연결 단계인1로 정리하고 나머지 연결 기록은 Unity가 다시 계산하도록 했습니다. 카메라 코드를 없애지 않고 기존에 사용한 Cinemachine3.1.7을 다시 연결했습니다. Splines2.9.0은 Cinemachine에 필요한 부품이라 함께 기록됩니다. 잠금 파일 전체를 삭제하거나 패키지를 모두 최신 버전으로 바꾸지 않았습니다.

수정 뒤 확인할 순서는 다음과 같습니다.

1. Unity의 Window > Package Manager에서 Cinemachine3.1.7과 Behavior1.0.16이 로드되는지 확인합니다.
2. 컴파일이 끝난 뒤 새 빨간 오류가 발생하지 않는지 확인합니다. 이전 오류 기록이 남아 있는 것과 다시 발생한 오류를 구분합니다.
3. `PlayerTestScene`에서 Play를 누릅니다. 캐릭터와 카메라가 실행되는지 확인한 뒤 Play를 끕니다. 이 검사 때문에 다른 씬을 열거나 현재 씬을 저장할 필요는 없습니다.

이번 결과는 패키지67개 오류0, JSON/연결 검사 통과, 컴파일 실패 없음, PlayerTestScene 약10.7초 실행 검사 통과입니다. 애니메이터와 Idle/Walk/Run 상태, 활성 Cinemachine 카메라와 Rigidbody를 확인했습니다. 모든 조작과 모든 맵의 전 기능 검사를 다시 한 것은 아닙니다. 상세 결과는 `Logs/PackageRepair20261006/`에 있습니다.

일반적인 패키지 추가·변경은 Package Manager에서 하고 `packages-lock.json`의 depth를 임의로 편집하지 않습니다. 이번 depth 편집은 JSON 오류 때문에 Unity가 파일을 읽지 못하는 상태를 벗어나기 위한 최소 수정이었습니다.

## 샷건 조준·4발 산탄 (2026-10-06)

샷건을 들면 중앙 점 주위에 속이 빈 원이 나옵니다. 이 원은 산탄이 퍼질 수 있는 각도 범위입니다. 우클릭을 누르면 화면이 확대되고 원도 작아져 산탄이 더 모입니다. 이때 Shift를 눌러도 걷습니다. 가만히 있으면 대기 모션을 유지합니다.

좌클릭을 새로 누르면 손과 총을 앞으로 드는 준비 동작 뒤에 산탄4개를 판정하고 반동을 재생합니다. 한 번 누른 채 계속 유지해도 자동으로 발사하지 않습니다. 기본 발사 간격은0.55초이며 그 안에 누른 입력은 예약하지 않습니다. Esc 뒤 화면을 다시 잡는 클릭은 발사하지 않고, 그 다음 새 클릭부터 발사합니다.

원리: 먼저 화면 중앙에 무엇이 있는지 확인합니다. 총을 그 방향으로 들고 실제 총구에서 광선4개를 보냅니다. 이것이 히트스캔입니다. 날아가는 총알 오브젝트를 매번 만들지 않습니다. 몸통과 총구 사이에 벽이 있거나 총구가 벽 안에 있으면 벽을 뚫고 쏘지 않습니다. 너무 가까운 벽은 총을 뒤집어 조준하지 않고 앞을 향한 자세를 유지한 채 벽에서 판정을 끝냅니다.

원은 카메라 시야각과 현재 퍼짐 각도로 계산한 각도 안내입니다. 카메라와 총구의 위치가 다르므로 아주 가까운 물체의 실제 탄착점은 원 안의 픽셀과 정확히 일치하지 않을 수 있습니다. 가려진 벽과 실제 총구 판정이 우선합니다.

### 바꿀 위치

1. `PlaytestScene01 > Cat_Player > Shotgun Combat`: `Pellet Count`=4, `Spread Degrees`=4°, `Aimed Spread Degrees`=1.5°, `Shot Interval`=0.55초, `Range`=75. 퍼짐 값은 원의 중심부터 가장자리까지의 각도입니다. 줌 퍼짐을 비줌보다 크게 설정해도 비줌 값을 넘지 않습니다.
2. `Cat_CinemachineCamera > Aim Zoom`: `Aimed Field Of View`=40, `Transition Seconds`=0.16. 시야각을 작게 하면 더 확대됩니다. 기본 시야각60은 줌을 놓으면 복귀합니다.
3. `Cat_Player > Shotgun Pose`: `Aimed Local Position`은 몸통 본 기준 조준 위치입니다. `Raise Seconds`는 우클릭으로 총을 드는 시간, `Fire Moment`는 발사 클립에서 실제 판정하는 시각입니다.
4. `Assets/03Sprites/Player/Animations/Shotgun_Fire.anim`: 준비와 반동 곡선입니다. `raiseWeight`는 총을 드는 양, `recoilDistance`는 뒤로 밀리는 양, `recoilPitch`는 위로 들리는 각도입니다. 발사 시각은 총을 다 드는 순간과 맞춥니다. 기존 Idle/Walk/Run 클립은 수정하지 않았습니다.
5. `Cat_Player > Shot Spread Ring`: `Line Width Pixels`와 `Outline Width Pixels`로 원의 선과 얇은 검은 테두리를 조절합니다. 기존 중앙 점의 위치·크기는 유지합니다.
6. 총 모델을 교체하면 `Cat_Shotgun > Muzzle`을 실제 총열 끝에 맞추고 파란 Z축이 발사 방향을 가리키게 합니다. `LeftGrip`과 `RightGrip`은 양손 위치입니다. `ShotGuardOrigin`은 몸통 안의 검사 시작점으로 유지합니다.

### 코드 담당 구분

모든 코드는 `Assets/02Scripts/01Player/` 아래에 있습니다.

| 파일 | 역할 |
| --- | --- |
| `Domain/CombatPolicy.cs` | 발사 간격·줌 중 걷기 규칙, 조준/퍼짐 상태 약속. Unity 타입 없음 |
| `Input/CombatInput.cs` | 실제 마우스 입력·포커스·재잠금 클릭 구분, 필요한 입력만 초기화하는 약속 |
| `Equipment/ShotgunCombat.cs` | 입력·장비·자세·명중 결과 연결 |
| `Equipment/HitscanQuery.cs` | 카메라 조준점, 실제 총구4광선에 쓰는 충돌·벽 검사 |
| `Animation/ShotgunPose.cs` | 새 발사 클립을 총과 양손 자세로 표현 |
| `Camera/AimZoom.cs` | 조준 상태를 기존 Cinemachine 렌즈에 전달 |
| `Equipment/Presentation/ShotSpreadRing.cs`와 기존 `CatShotSpreadRing.shader` | 현재 퍼짐 각도와 카메라 투영으로 속 빈 원 표시 |
| `Editor/Setup/CombatSetup.cs` | 허용된 씬에서 기존 발사 클립·재질을 GUID로 찾아 연결 |
| `Editor/Validation/HitscanValidation.cs` | 실제 충돌·벽 검사 |
| `Editor/Validation/CombatValidation.cs` | 이동·줌·손 자세·산탄·링·밀착 벽 통합 검사 |
| `Editor/Validation/CombatInputValidation.cs` | 가상 입력 장치로 실제 기본 키·마우스 처리 검사 |

적에게 맞았을 때의 결과는 `IShotReceiver.ReceiveShot(ShotHit)`에 전달합니다. 적 담당자가 이 약속을 구현해 체력을 줄이면 됩니다. 이 작업에는 적 체력·대미지 수치·탄약·재장전·효과음을 추가하지 않았습니다. `PelletResolved`는 산탄마다, `ShotFired`는 한 번 발사할 때마다 알려주는 이벤트입니다.

발사 규칙은 일반 C#으로 재사용할 수 있습니다. 엔진을 바꾸면 입력·물리·클립 재생·Cinemachine·화면 표시 연결부를 교체해야 합니다. 다른 엔진에서 실행 검증한 상태는 아닙니다.

전투 입력을 교체할 때는 `ICombatInputSource`를 구현합니다. 버튼의 이전 상태를 기억하는 입력만 추가로 `IResettableInputSource`를 구현하면, 창의 포커스를 잃거나 전투가 비활성화될 때 `Reset()`이 호출됩니다. 초기화가 필요 없는 입력에는 이 함수를 억지로 넣지 않아도 됩니다. 총을 내린 채 대기할 때는 불필요한 조준 검사를 건너뛰고, 조준·발사·총을 내리는 전환 중에는 기존 검사를 유지합니다.

### 확인 방법

`PlaytestScene01`만 열고 Play한 뒤 `Tools > Cat Player > Play 모드 조준 발사 검증` 또는 `Play 모드 실제 조준 입력 검증`을 실행합니다. 각각 새 Play에서 실행하고 도중에 직접 조작하지 않습니다. 검증은 종료 시 Play를 끝내고 `%TEMP%/PlayerValidation`에 결과를 씁니다. `순수 규칙 검증`은 편집 모드에서도 가능하며 결과는 Unity Console에 표시됩니다. 물리 검사 코드는 Play 전용이며 테스트에 사용한 임시 물체는 씬에 저장하지 않습니다.

공식 자료: [클립의 지정 시각 재생](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AnimationClip.SampleAnimation.html), [Cinemachine 렌즈](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineCamera.html), [광선 검사와 시작점 내부 제한](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html), [구형 경로 검사](https://docs.unity3d.com/ScriptReference/Physics.SphereCastNonAlloc.html), [겹침 검사](https://docs.unity3d.com/ScriptReference/Physics.OverlapSphereNonAlloc.html). 좁은 벽 검사는 광선 하나에만 맡기지 않고 경로와 양 끝 겹침을 함께 검사합니다.

### 샷건 구현 검증 결과 (2026-10-06)

독립 규칙35개, 실제 Unity 충돌18개, 전투 통합32개, 기본 장치 입력 경로15개를 통과했습니다. Game 화면의 원 표시와 줌 축소, 사선에서 총을 앞으로 내민 양손 자세도 확인했습니다. 최초 준비 시간 오류를 수정한 기록과 최종 증거 경로는 `PLAYER_WORK_LOG.md`에 있습니다. 입력 검사는 가상 장치를 통한 기본 처리 경로 검사이며, 전체 프로젝트 빌드 시험은 아닙니다.

## GitHub Desktop에서 붉은 줄이 보일 때

오른쪽 비교 화면의 붉은 줄은 바뀌기 전 내용, 초록 줄은 바뀐 뒤 내용입니다. 한 줄을 수정해도 이전 줄을 빼고 새 줄을 넣은 것처럼 표시합니다. 이 색 자체를 오류로 보고 Discard Changes(수정 취소)를 누르지 않습니다.

노란 LF→CRLF 안내는 줄바꿈 방식의 자동 변환 예정입니다. 코드가 고장났다는 뜻은 아닙니다. 현재 저장소는 하위 .gitattributes의 매크로 선언10~12행이 허용되지 않는 별도 설정 문제가 확인됐습니다. 파일을 이동하지 않고 약식 규칙을 실제 속성으로 풀어 쓰면 정리할 수 있지만, Git/LFS 적용 범위가 달라지므로 이 진단에서는 변경하지 않았습니다.

커밋은 현재 상태를 로컬 이력에 기록하는 일입니다. 변경 파일과 .meta를 함께 확인하고 왼쪽 아래 Summary에 `샷건 조준·4발 산탄·발사 애니메이션 구현 및 패키지 오류 수정`처럼 내용을 적은 뒤 `Commit … files to Player`를 누릅니다. Summary는 필수이고 Description은 선택입니다. 성공하면 History에서 커밋을 확인합니다. Push origin은 그 이후 GitHub로 전송하는 별도 동작입니다.

## OneDrive 밖에서 프로젝트 사용하기

이 절의 복사·등록 설명과 아래 날짜별 이전 결과는 과거 작업 기록입니다. 현재는 문서 맨 위의 `C:/Users/307/Desktop/unity/Team3rd3DProject/3rd3DProject`를 사용하며, 이번 재구현에서 프로젝트를 다시 복사하거나 옮기지 않습니다.

작업 폴더를 C:/UnityProjects/Team3rd3DProject처럼 OneDrive 밖에 둡니다. 현재는 Git 저장소의 바깥 폴더Team3rd3DProject와 Unity 프로젝트의 안쪽 폴더3rd3DProject가 다르므로 등록할 위치를 구분합니다.

1. Unity에서 작업을 저장하고 종료합니다. GitHub Desktop의 작업도 마치고 종료합니다. 온라인 전용 파일이 있다면 OneDrive의 '이 장치에 항상 유지'로 다운로드가 완료된 뒤 복사합니다.
2. 기존 Team3rd3DProject 폴더 전체를 C:/UnityProjects에 복사합니다. .git 숨김 폴더, 아직 커밋하지 않은 파일과 .meta를 함께 보존합니다. 새 복사본 검증 전 원본은 삭제하지 않습니다.
3. GitHub Desktop의 File > Add local repository에서 C:/UnityProjects/Team3rd3DProject를 선택합니다. 새 저장소 생성이나 원격 재다운로드가 아닙니다.
4. Unity Hub의 Projects > Add > Add project from disk에서 C:/UnityProjects/Team3rd3DProject/3rd3DProject를 선택하고 기존6000.5.6f1로 엽니다.
5. GitHub Desktop Repository > Show in Explorer에서 새 경로를 확인하고, Player 브랜치/History/아직 커밋하지 않은 변경 목록이 보존됐는지 확인합니다. Unity에서는 PlayerTestScene의 컴파일과 실행을 확인합니다. 이후 새 복사본을 작업 대상으로 사용합니다.

OneDrive 밖에서 작업하는 것과 .gitattributes의 잘못된 매크로 선언을 수정하는 것은 서로 다른 일입니다. 경로를 옮겼다고 기존 Git 설정 경고까지 해결된 것으로 판단하지 않습니다. 2026-10-06 사용자 승인 후 실제 복사와 앱 등록을 완료했습니다. 아래의 새 경로를 사용합니다.


### 실제 이전 결과와 앞으로 열 위치 — 2026-10-06

이제부터 사용할 Git 저장소는 C:/UnityProjects/Team3rd3DProject이고, Unity 프로젝트는 그 안의 3rd3DProject 폴더입니다. 바깥 폴더에는 Git 변경 기록인 .git이 있고, 안쪽 폴더에는 Unity의 Assets, Packages, ProjectSettings가 있습니다.

- GitHub Desktop: 새 경로 등록 완료. Player 브랜치와 미커밋 변경 42개 항목 보존. Repository > Show in Explorer에서 C:/UnityProjects/Team3rd3DProject가 열리는지 확인합니다.
- Unity Hub: C:/UnityProjects/Team3rd3DProject/3rd3DProject 등록 완료. 기존6000.5.6f1로 열어 PlayerTestScene만 실행 검증했습니다.
- 같은 이름의 이전 프로젝트도 목록에 남을 수 있습니다. 이름보다 경로를 보고 C:/UnityProjects 쪽을 엽니다. OneDrive 원본은 보관용으로 남겼으므로 두 사본을 번갈아 편집하지 않습니다.
- 복사 직후 80,531개 파일의 크기와 SHA-256이 모두 일치했습니다. SHA-256은 파일 내용이 같은지 확인하는 지문입니다. 아직 커밋하지 않은 파일과 .meta도 보존했습니다.
- 컴파일 오류0, 누락 스크립트0, 이동·카메라 연결 검사 통과. 기존 실제 입력 검증15개도 모두 통과했습니다. Play는 종료했고 씬을 저장하거나 수정하지 않았습니다.
- 작업 기록은 PLAYER_WORK_LOG.md, 복사 검증 도구와 결과는 저장소 밖 C:/UnityProjects/MigrationRecords/20261006-OneDrive에 있습니다.
- 원본 삭제·커밋·푸시는 하지 않았습니다. 기존 하위 .gitattributes 매크로 경고는 경로 이전과 별개로 남아 있습니다.

앱 등록에는 공식 명령줄 기능을 사용했습니다. 참고: [GitHub Desktop 명령줄](https://docs.github.com/en/desktop/overview/launching-github-desktop-from-the-command-line?platform=windows), [Unity CLI](https://docs.unity.com/en-us/unity-cli/unity-cli-reference).

## 현재 스크립트 이름과 수정 지점 (2026-10-07)

앞부분의 기능 안내는 아래 새 이름을 사용합니다. 날짜가 적힌 과거 검사·이전 기록의 파일명은 당시 이름을 보존했습니다. 이번 변경의 검사 결과는 `PLAYER_WORK_LOG.md`에서 따로 확인합니다.

### 이름을 바꿔도 연결을 지키는 방법

GUID는 Unity가 파일을 구분하는 이름표입니다. C# 파일명을 바꾸면서 이 이름표를 새로 만들면 기존 연결이 끊길 수 있습니다. 이번 40개 스크립트는 기존 `.meta`를 짝으로 유지하고 파일명만 함께 바꾸는 대상입니다. 모델·프리팹·클립·재질·입력 액션·셰이더의 이름은 바꾸지 않습니다.

`Interaction/CatInteractionItem.cs`는 예외입니다. 예전 물리 복구 상태를 읽는 저장 형식의 이름이 달라지지 않도록 파일명과 클래스명을 유지합니다. 현재는 남은 복구 상태를 즉시 해제하며 공중 대기를 시작하지 않습니다. 저장되는 필드 이름, 컴포넌트 연결과 발사 애니메이션의 세 필드도 유지합니다.

변경 전후 `.meta` 내용과 GUID가 같은지, 새 파일명으로 스크립트가 로드되는지, 모델·프리팹·애니메이션 연결에 Missing이 없는지를 함께 확인합니다. 다른 씬을 수정하거나 기존 에셋을 새로 만드는 방식으로 연결을 고치지 않습니다. Unity의 [에셋 이동 API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.MoveAsset.html)와 [에셋 메타데이터 안내](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html)를 기준으로 확인합니다.

### 입력과 획득 연출을 바꾸려면

- 기본 집기 버튼은 `Input/InteractionInput.cs`에서 읽습니다. 빈손은 좌클릭, 샷건은 `Input/CombatInput.cs`의 좌클릭 발사·우클릭 조준으로 나뉩니다. 손에 총이 있으면 `HandPolicy`가 물건 집기를 막습니다.
- 두 입력이 함께 사용하는 `InputActivationGate`는 `CombatInput.cs`에 있습니다. “입력이 다시 켜진 첫 프레임에는 새 클릭을 전달하지 않는다”는 규칙만 담당합니다. 장치와 Unity 상태를 직접 읽지 않아 같은 규칙을 별도로 검사할 수 있습니다.
- 입력을 바꾸려면 `IInteractionInputSource` 또는 `ICombatInputSource`를 구현해 해당 컴포넌트의 `SetInputSource(...)`로 연결합니다. 이전 버튼 상태를 기억하는 입력만 `IResettableInputSource.Reset()`도 구현하면 됩니다. `null`을 넘기면 기본 입력으로 돌아갑니다.
- 인벤토리 도착 UI는 `InventoryPickupEffect.RegisterTarget(rectTransform)`으로 등록합니다. UI가 등록되어 활성화된 뒤에만 획득 연출을 시작합니다. 실제 인벤토리 저장은 기존처럼 `Collected` 이벤트를 받은 담당 코드가 처리합니다.
- 다른 획득 연출을 연결하려면 `IPickupPort`의 `IsBusy`, `TryCollect(...)`를 구현하고 `InteractionController.TrySetPickupSource(source)`를 호출합니다. 이 함수가 `false`이면 현재 연출 중이므로 교체되지 않은 것입니다. `null`이면 인스펙터의 기존 `pickup`으로 돌아갑니다. 실제 인벤토리 저장 규칙을 이 인터페이스에 추가하지 않습니다.

수정 후에는 새 Play에서 빈손 좌클릭 집기·유지·해제, 빈손 우클릭으로 집히지 않음, Esc 후 재잠금 클릭으로 집히지 않음, 숫자1 토글, 샷건 좌클릭 발사·우클릭 조준을 확인합니다. UI 미등록 시 아이템이 남고, 등록 후에는 완료 이벤트가 한 번만 오는지도 확인합니다. 검증을 끝내기 전에는 통과했다고 기록하지 않습니다.

### 이름 대응표

모든 경로는 `Assets/02Scripts/01Player/` 아래입니다. 표의 각 C# 파일과 기존 `.meta`가 한 쌍으로 이름을 바꿉니다.

| 변경 전 | 현재 이름 |
| --- | --- |
| `Animation/CatPlayerAnimation.cs` | `Animation/PlayerAnimation.cs` |
| `Animation/CatShotgunPose.cs` | `Animation/ShotgunPose.cs` |
| `Camera/CatAimZoom.cs` | `Camera/AimZoom.cs` |
| `Camera/CatCameraOrbitLimit.cs` | `Camera/CameraOrbitLimit.cs` |
| `Camera/CatCinemachineCursor.cs` | `Camera/CameraCursorLock.cs` |
| `Core/CatPlayerMotor.cs` | `Core/PlayerController.cs` |
| `Domain/CatCombatPolicy.cs` | `Domain/CombatPolicy.cs` |
| `Domain/CatHandPolicy.cs` | `Domain/HandPolicy.cs` |
| `Domain/CatMovementPolicy.cs` | `Domain/MovementPolicy.cs` |
| `Domain/CatOrbitPolicy.cs` | `Domain/OrbitPolicy.cs` |
| `Domain/ICatMotionState.cs` | `Domain/IMotionState.cs` |
| `Editor/Setup/CatCombatSetup.cs` | `Editor/Setup/CombatSetup.cs` |
| `Editor/Setup/CatEquipmentSetup.cs` | `Editor/Setup/EquipmentSetup.cs` |
| `Editor/Setup/CatInteractionSetup.cs` | `Editor/Setup/InteractionSetup.cs` |
| `Editor/Setup/CatPlayerInteractionSettings.cs` | `Editor/Setup/InteractionSettings.cs` |
| `Editor/Validation/CatCombatInputValidation.cs` | `Editor/Validation/CombatInputValidation.cs` |
| `Editor/Validation/CatCombatValidation.cs` | `Editor/Validation/CombatValidation.cs` |
| `Editor/Validation/CatHitscanValidation.cs` | `Editor/Validation/HitscanValidation.cs` |
| `Editor/Validation/CatInteractionValidation.cs` | `Editor/Validation/InteractionValidation.cs` |
| `Editor/Validation/CatPlayerValidation.cs` | `Editor/Validation/PlayerValidation.cs` |
| `Editor/Validation/CatPlayerValidationCases.cs` | `Editor/Validation/PlayerValidationCases.cs` |
| `Editor/Validation/CatPolicyValidation.cs` | `Editor/Validation/PolicyValidation.cs` |
| `Equipment/CatHitscanQuery.cs` | `Equipment/HitscanQuery.cs` |
| `Equipment/CatShotgunCombat.cs` | `Equipment/ShotgunCombat.cs` |
| `Equipment/CatShotgunEquipment.cs` | `Equipment/ShotgunEquipment.cs` |
| `Equipment/Presentation/CatShotSpreadRing.cs` | `Equipment/Presentation/ShotSpreadRing.cs` |
| `Input/CatCombatInput.cs` | `Input/CombatInput.cs` |
| `Input/CatInteractionInput.cs` | `Input/InteractionInput.cs` |
| `Input/CatPlayerInput.cs` | `Input/PlayerInput.cs` |
| `Interaction/CatInteractionController.cs` | `Interaction/InteractionController.cs` |
| `Interaction/CatItemCarrier.cs` | `Interaction/ItemCarrier.cs` |
| `Interaction/Presentation/CatCenterCursor.cs` | `Interaction/Presentation/CenterCursor.cs` |
| `Interaction/Presentation/CatHighlightTarget.cs` | `Interaction/Presentation/HighlightTarget.cs` |
| `Interaction/Presentation/CatInventoryPickupPresenter.cs` | `Interaction/Presentation/InventoryPickupEffect.cs` |
| `Interaction/Presentation/CatItemHighlight.cs` | `Interaction/Presentation/ItemHighlight.cs` |
| `Interaction/Presentation/CatItemIconCapture.cs` | `Interaction/Presentation/ItemIconCapture.cs` |
| `Interaction/Presentation/ICatHighlightSource.cs` | `Interaction/Presentation/IHighlightSource.cs` |
| `Movement/CatPlayerLocomotion.cs` | `Movement/PlayerMovement.cs` |
| `Movement/CatStepSettings.cs` | `Movement/StepSettings.cs` |
| `Movement/CatStepSolver.cs` | `Movement/StepSolver.cs` |

### 이번 변경의 확인 결과 (2026-10-07)

컴파일과 총 165개 검사를 통과했습니다. 순수 규칙 35개, 상호작용 41개, 실제 전투 입력 15개, 전투 통합 32개, 히트스캔 18개, 강조 캐시 7개, 교체 가능한 입력·연출 10개, 현재 맵 이동 7개입니다. 이름이 바뀐 스크립트 40개의 GUID 연결과 프리팹 4개, 발사 클립의 연결도 확인했습니다.

전체 메타 1,046개의 내용은 동일합니다. Play를 종료한 편집기에서 Missing Script·누락 메시·재질 오류는 0이었습니다. 기존 사용자 씬/폰트 변경, RAt 파일과 프로젝트 설정은 보존했습니다. 다른 씬 실행·팀원 PC·전체 게임 빌드를 검사한 결과는 아닙니다.

검사 방법과 오류 처리 기록은 `PLAYER_WORK_LOG.md`, 원본 대비 결과와 백업은 `C:/Users/307/Documents/Codex/PlayerRefactor20261007`에 있습니다. Git으로 공유할 때에는 이름이 바뀐 `.cs`와 같은 이름의 `.meta`를 함께 포함해야 기존 식별 번호와 연결이 유지됩니다.


### 검은자위가 측면에서 사라질 때 확인할 설정 (2026-10-07)

이번 수정은 모델링을 바꾸지 않고 Unity 재질의 그리는 순서만 고친 것입니다. Git 이력상 10월 2일 커밋 `5d7d582`에서는 양쪽 검은자위가 3100이었으나, 10월 6일 `ba840e8`에서 자동값 -1로 바뀌었습니다. 확인된 기존값 3100으로 복구했습니다. 흰자위와 검은자위가 모두 같은 순서 3000을 사용하면 카메라 각도에 따라 흰자위가 나중에 그려져 검은자위를 덮었습니다.

- `Assets/04Prefabs/Player/Materials/Eye_L_Low22.mat`, `Eye_R_Low22.mat`: 기존 기본 순서 3000 유지.
- 같은 폴더의 `Pupil_L_Low22.mat`, `Pupil_R_Low22.mat`: Render Queue를 3100로 지정. 파일의 `m_CustomRenderQueue`는 자동값 -1에서 3100로 바뀝니다. 검은자위를 흰자위 다음에 그리라는 뜻입니다.
- 색·팔레트·텍스처·셰이더·메시·본·애니메이션·프리팹·씬·메타·GUID는 작업 전 상태를 유지합니다. 각도에 따라 눈을 끄거나 모델을 부풀리는 설정은 사용하지 않습니다.

나중에 수정할 때에는 위 두 Pupil 재질을 선택해 Render Queue를 확인하세요. 흰자위 재질까지 같은 값으로 바꾸지 않습니다. `PlaytestScene01`에서 정면, 좌우 사선, 양 측면으로 돌려 검은자위를 확인하고 뒤통수에서 머리를 뚫고 보이지 않는지도 확인합니다.

이번에는 수정 전후 같은 조건의 14방향 렌더와 Play 상태의 14방향 렌더를 확인했습니다. 정면·좌우45/60도·좌우135도·후면의 수정 전후 이미지는 픽셀 단위로 같았고, 좌우75/90/105도에서 가려지던 검은자위가 복구됐습니다. Play에서도 눈 재질 네 개의 순서·표시·셰이더 상태 검사 4/4가 통과했습니다. 전체 게임 빌드나 팀원 PC 검증까지 수행한 것은 아닙니다.

비교 이미지와 지문 검사 결과: `C:/Users/307/Documents/Codex/PlayerEyeFix20261007`.


### 손·발 외곽선과 점프 시안 (2026-10-08)

손·발 외곽선은 PlaytestScene01의 Cat_Player 아래 Hand_L_Low22, Hand_R_Low22, Foot_L_Low22, Foot_R_Low22에 추가했습니다. 각 Skinned Mesh Renderer의 Materials에서 첫 칸은 기존 색, 두 번째 칸은 기존 Cat_Outline입니다. 몸통에 쓰던 검은 외곽선을 손·발에도 그리는 방식입니다. 모델의 점이나 본은 바꾸지 않았습니다. 프리팹 원본에는 적용하지 않아 이 씬에서만 보이는 설정입니다.

외곽선 굵기를 조절하는 기존 재질 값은 Width=0.01입니다. 이 재질은 몸통도 함께 사용하므로 바꾸면 몸통 선도 변합니다. 이번에는 재질 값 자체를 바꾸지 않았습니다. 각 손·발의 외곽선에 같은 메시를 한 번 더 그리므로 표시 비용은 추가됩니다. 실제 기기 성능 측정은 하지 않았습니다.

점프 시안은 C:/Users/307/Documents/Codex/PlayerJumpPreview20261008/jump_preview_color_corrected.gif에서 볼 수 있습니다. 정면·사선·측면이 함께 나옵니다. 최초 시안 단계에는 게임에 연결하지 않았고, 이후 승인받아 문서 맨 위의 Space 점프로 적용했습니다.

- 준비와 착지: 몸통 본의 세로 크기 2/3. 도약: 최대 1.22배. 꼬리는 별도로 보정해 몸통과 함께 납작해지지 않도록 했습니다.
- 손은 도약 때 아래로 늦게 따라오고, 귀는 뒤로 최대 60도 접힙니다. 착지 때도 50도 접힌 뒤 돌아옵니다. 꼬리 5개 본은 끝으로 갈수록 조금 늦게 움직입니다.
- Cat_Jump_Pose_Draft.anim은 제자리 자세용, Cat_Jump_Preview_Draft.anim은 화면 확인용 상승·하강까지 포함합니다. 둘 다 위 외부 폴더에 있는 2초/60fps 초안입니다. 실제 게임에서는 자세용과 이동 처리를 연결해야 하며, 검토용 상승을 게임 이동과 겹쳐 적용하면 안 됩니다.
- 수정할 때에는 같은 폴더의 JumpPoseDraft.cs.txt에서 HeightScale(눌림/늘어남), EarFold(귀 접힘), HandDrag(손 지연), TailDrag(꼬리 지연)를 조절한 뒤 클립을 다시 생성합니다. 이 파일은 외부 제작 도구이며 게임에 붙이는 스크립트가 아닙니다.

저장한 두 클립을 다시 읽어 121개 시점의 32개 본을 검사했습니다. 위치 차이는 최대 약 0.00000006, 회전·크기 차이는 0이었습니다. 꼬리 본 사이 길이는 유지하지만 기존 본 계층의 비균일 크기 때문에 꼬리 부피까지 완전히 동일하다는 뜻은 아닙니다. 손·발 외곽선은 Idle/Walk/Run 미리보기에서 확인했습니다. 이후 실제 게임 적용 검증은 작업 로그의 점프 적용 항목에서 확인합니다.


### 점프 미리보기 색상 정정 (2026-10-08)

첫 점프 영상은 화면에 표시하기 위한 색상 변환이 빠져 주황색이 더 붉고 진하게 보였습니다. 모델의 색을 다시 칠한 것이 아닙니다. 원본 재질·팔레트·모델은 그대로이며, 외부 미리보기 촬영 도구에서 표시용 색 변환(sRGB)을 켜서 고쳤습니다.

수정 영상: C:/Users/307/Documents/Codex/PlayerJumpPreview20261008/jump_preview_color_corrected.gif. PNG 기준 주황 RGB(255,144,40), 크림 RGB(250,240,230)을 원본과 대조했습니다. 앞으로 촬영 결과가 진해지면 프로젝트의 팔레트를 바꾸기 전에 미리보기 RenderTexture가 Linear 데이터를 그대로 저장하는지 확인합니다. GIF는 색 수 제한 때문에 작은 색 차이가 남을 수 있습니다. 색상 교정에서는 점프 동작을 바꾸지 않았고, 게임 연결은 이후 별도 승인에 따라 진행했습니다.
