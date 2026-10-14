# 물건 상호작용과 샷건

문서 기준: 2026-10-14에 확인한 현재 작업 폴더의 C# 소스. 이 장은 F05~F08의 구현을 설명합니다. 씬에 실제로 연결된 오브젝트와 재질 값은 별도의 씬 구성표를 함께 확인해야 합니다. 아래의 숫자는 따로 표시하지 않는 한 **코드에 적힌 기본값**이며, Unity Inspector에서 저장한 값이 다를 수 있습니다.

이 장을 작성하며 게임 코드, 씬, 메타 파일은 수정하지 않았습니다. 코드 경로는 확인했지만 이번 문서 작업에서 모든 키를 눌러 새로 플레이 검증한 것은 아닙니다. 따라서 아래의 검증 절차는 앞으로 실행할 체크리스트이며, 통과 기록을 뜻하지 않습니다.

## 1. 처음 읽는 사람을 위한 개념

화면 가운데의 흰 구체는 마우스 장치 자체가 아니라 **조준 위치를 보여 주는 표시**입니다. 실제 대상 찾기는 카메라 가운데에서 보이지 않는 선을 쏘는 `Raycast`가 담당합니다. 그 선에 먼저 닿는 충돌체를 찾고, 플레이어와의 거리 및 벽 가림을 추가로 확인합니다.

- `Collider`: 물체가 차지한 공간을 표현하는 충돌 모양입니다. 이것만 붙인다고 중력이 생기지는 않습니다.
- `Rigidbody`: 중력과 속도 등 물리 운동을 Unity에 맡기는 컴포넌트입니다.
- `interface`: 두 코드 사이의 작은 약속입니다. 예를 들어 “빈칸이 있는가?”와 “저장할 수 있는가?”만 약속하면, 저장소의 내부 구현을 몰라도 아이템을 넣을 수 있습니다.
- `Update`: 이번 화면 프레임의 입력과 상태를 계산하는 단계입니다.
- `LateUpdate`: 다른 계산 뒤에 물건, 손, 총구 등 최종 위치를 맞추는 단계입니다. 이 프로젝트는 이 순서를 명시적으로 사용합니다.
- `event`: 어떤 일이 끝났다고 다른 코드에 알려 주는 알림입니다. 알림을 보내는 쪽과 받는 쪽은 역할이 다릅니다.

상호작용은 서로 다른 세 가지 경로가 있습니다. 이름에 “아이템”이 들어간다고 하나의 기능으로 합치면 기존 동작이 달라질 수 있습니다.

| 경로 | 시작 입력 | 물체에 일어나는 일 | 인벤토리 저장 |
| --- | --- | --- | --- |
| F05 손 운반 | 빈손에서 좌클릭을 누르고 유지 | 물체가 중앙 조준선 가까이 따라옴. 떼면 현재 위치에서 중력으로 낙하 | 하지 않음 |
| F06 F 획득 | 대상을 보며 F를 새로 누름 | 일반 아이템은 월드에서 숨겨짐. 특정 소비형 아이템은 실행 중 인스턴스 제거 | `ItemInteractor`가 저장소에 직접 요청 |
| F07 UI 획득 연출 | `InventoryPickup` 종류의 기존 큐브를 좌클릭 | 외형을 촬영한 아이콘이 등록된 UI로 이동 | 연출 완료 알림만 보냄. 실제 저장은 수신자 책임 |

## 2. 기능과 파일 책임표

링크는 이 문서를 프로젝트의 `PlayerDocumentation` 폴더에 놓았을 때 열리는 상대 경로입니다. 행 번호는 현재 확인본 기준이므로 이후 수정되면 함수 이름도 함께 찾아보세요.

| ID | 파일과 확인 위치 | 이 파일이 담당하는 일 | 담당하지 않는 일 |
| --- | --- | --- | --- |
| F05/F06/F07 | [InteractionController.cs](../Assets/02Scripts/01Player/Interaction/InteractionController.cs), 14~264행 | 중앙 대상 선택, 거리·가림 검사, 손 명령을 운반/연출/장비로 연결 | 아이템 저장 규칙, 피해량 |
| F05 | [ItemCarrier.cs](../Assets/02Scripts/01Player/Interaction/ItemCarrier.cs), 38~226행 | 집기, 선반에서 꺼내기, 벽에 막히는 이동, 손 놓기와 물리 복구 | 키보드/마우스 읽기, 인벤토리 |
| F05/F07 | [CatInteractionItem.cs](../Assets/02Scripts/01Player/Interaction/CatInteractionItem.cs), 12~140행 | 아이템 ID, 종류, `Visual` 외형, 루트 충돌 크기, 사용 가능 상태 | F 저장소와 라운드 보상 |
| F05/F08 | [HandPolicy.cs](../Assets/02Scripts/01Player/Domain/HandPolicy.cs), 18~52행 | 손 상태에 따라 집기/놓기/장착/해제 결정. F의 저장·포획 결정도 포함 | Unity 물리, 모델 표시 |
| F05 | [InteractionInput.cs](../Assets/02Scripts/01Player/Input/InteractionInput.cs), 16~25행 | 좌클릭 새 누름/유지, 숫자 1을 값으로 변환 | 무엇을 집을지 결정 |
| F06 | [ItemInteractor.cs](../Assets/02Scripts/04Systems/ItemInteractor.cs), 53~285행 | F 대상 등록·자동 발견, 저장 요청, Enemy 포획 연결 | 이동·총 발사·인벤토리 UI 그리기 |
| F06 | [ItemPickup.cs](../Assets/02Scripts/04Systems/ItemPickup.cs), 4~18행 | 월드 물건에 아이템 데이터와 획득 조건 연결 | F키 직접 처리 |
| F07 | [CenterCursor.cs](../Assets/02Scripts/01Player/Interaction/Presentation/CenterCursor.cs), 11~23행 | 중앙 구체의 화면상 크기와 위치, 커서 잠금 시 표시 | 대상 찾기·아이템 저장 |
| F07 | [IHighlightSource.cs](../Assets/02Scripts/01Player/Interaction/Presentation/IHighlightSource.cs), 4~10행 | 강조할 모델과 사용 가능 상태를 제공하는 공통 약속 | 실제 노란색 그리기 |
| F07 | [HighlightTarget.cs](../Assets/02Scripts/01Player/Interaction/Presentation/HighlightTarget.cs), 8~32행 | 일반 오브젝트를 강조 대상으로 제공, Renderer 목록 갱신 | 집기·획득 권한 부여 |
| F07 | [ItemHighlight.cs](../Assets/02Scripts/01Player/Interaction/Presentation/ItemHighlight.cs), 25~132행 | 원본 재질 위에 강조용 면·후광·테두리를 추가로 그림 | 원본 공유 재질 변경 |
| F07 | [InventoryPickupEffect.cs](../Assets/02Scripts/01Player/Interaction/Presentation/InventoryPickupEffect.cs), 13~224행 | UI 목적지 확인, 아이콘 이동, 완료 이벤트 | 5칸 저장소 관리 |
| F07 | 위 파일의 `PickupReservation`, 228~349행 | 연출 동안 월드 모델/충돌/물리 상태 예약, 완료·취소 정리 | UI 이동 경로 계산 |
| F07 | [ItemIconCapture.cs](../Assets/02Scripts/01Player/Interaction/Presentation/ItemIconCapture.cs), 11~173행 | 실제 메시를 한 번 촬영하고 임시 카메라/메시/텍스처 정리 | 아이템 수량과 슬롯 |
| F08 | [ShotgunEquipment.cs](../Assets/02Scripts/01Player/Equipment/ShotgunEquipment.cs), 7~159행 | 샷건 표시, 몸통 기준 위치, 양손을 잡는 지점에 맞춤 | 사격 허용 판단·명중 계산 |
| F08 | [ShotgunCombat.cs](../Assets/02Scripts/01Player/Equipment/ShotgunCombat.cs), 8~138행 | 입력·장비·자세·히트스캔을 순서대로 연결, 명중 이벤트 | 적 체력·기절 시간·인벤토리 |
| F08 | [HitscanQuery.cs](../Assets/02Scripts/01Player/Equipment/HitscanQuery.cs), 31~199행 | 조준점, 총구 벽 막힘, 산탄별 충돌 결과 | 난수 분포·반동·입력 |
| F08 | [CombatPolicy.cs](../Assets/02Scripts/01Player/Domain/CombatPolicy.cs), 16~43행 | 단발 발사 간격, 조준 중 달리기 금지 | 카메라 시야각 변경 |
| F08 | [CombatInput.cs](../Assets/02Scripts/01Player/Input/CombatInput.cs), 25~68행 | 우클릭 유지, 좌클릭 새 누름, 입력 복귀 프레임 차단 | 피해 판정 |
| F08 | [ShotSpreadRing.cs](../Assets/02Scripts/01Player/Equipment/Presentation/ShotSpreadRing.cs), 42~81행 | 현재 산탄 각도를 화면 중앙의 속 빈 원으로 표시 | 총알 위치 결정 |
| 공통 | [IPrimaryAction.cs](../Assets/02Scripts/01Player/Domain/IPrimaryAction.cs), 1~17행 | 설치가 좌클릭/F를 쓰는지, F 대상이 있는지 알려 주는 작은 약속 | 덫 설치 자체 |

F08이 읽는 다른 기능의 파일은 [ShotgunPose.cs](../Assets/02Scripts/01Player/Animation/ShotgunPose.cs), [AimZoom.cs](../Assets/02Scripts/01Player/Camera/AimZoom.cs), [PlayerMovement.cs](../Assets/02Scripts/01Player/Movement/PlayerMovement.cs)입니다. 이들은 각각 자세/애니메이션, 카메라 렌즈, 이동을 담당합니다. `HotbarManager`, `ThiefController`, `ThiefShotReceiver`는 기존 팀 기능과의 연결 지점입니다. 연결을 사용한다는 사실을 해당 기능 전체를 이번 Player 작업에서 제작했다는 뜻으로 해석하지 않습니다.

## 3. 현재 입력 우선순위

입력 중복은 “같은 클릭으로 총도 쏘고 물건도 집는 문제”입니다. 이를 막기 위해 한 입력을 먼저 사용하는 기능이 다른 기능에 사용 중임을 알려 줍니다.

| 상황 | 허용하는 동작 | 중복을 막는 코드 |
| --- | --- | --- |
| 메뉴/결과창/포커스 상실 등 공통 입력 차단 | 플레이 입력을 막음 | `GameplayInput.IsEnabled`와 각 입력 공급자 |
| 게임 조작이 다시 켜진 첫 프레임 | 복귀용 클릭을 집기/발사로 사용하지 않음 | `InputActivationGate.AllowsPress`, `CombatInput.cs` 30~41행 |
| 덫 선택 동작이 좌클릭을 사용 중 | 설치가 우선. 집기/사격 새 요청 차단 | `IPrimaryAction.ClaimsPrimaryInput`; `InteractionController` 95~97행, `ShotgunCombat` 68~69행 |
| F 유지 설치가 입력을 사용 중 | F 즉시 획득 중복 차단 | `ItemInteractor` 142~145행의 `IInteractionAction` |
| 기존 대상을 F로 상호작용한 프레임 | 손 집기/장착을 같은 프레임에 추가 실행하지 않음 | `ConsumesHandInput`, `InteractionController` 113~114행 |
| 총을 든 상태 | 좌클릭은 단발, 우클릭 유지는 조준 | `HandPolicy`와 `CombatInput` |
| 물건을 들고 숫자 1 | 현재 위치에 놓고 샷건 장착 요청 | `TryToggleEquipment` 223~226행 |
| 물건을 들고 좌클릭 해제 또는 입력 불가 | 물건 놓기 | `LateUpdate` 105~106행, `HandPolicy` 22·25행 |

`IPrimaryAction`은 “모든 입력을 막아라”가 아닙니다. 사격 쪽은 `FirePressed`만 false로 바꿉니다. 따라서 덫을 선택해도 총 표시를 자동으로 숨기지 않고, 조준 입력 값도 별도로 유지할 수 있습니다. F를 새로 누를 때 대상 획득이 우선인지, 유지 설치가 시작됐는지는 덫 설치 장의 입력 중재 규칙과 함께 읽어야 합니다.

## 4. F05 — 좌클릭 물건 운반

### 4.1 목적과 데이터 흐름

목적은 빈손으로 집을 수 있는 물체를 중앙 포인터에 가까이 들고, 버튼을 떼면 자연스럽게 놓는 것입니다. 인벤토리에 저장하는 기능과 다릅니다.

```text
마우스 좌클릭 → InteractionInput → HandInput
→ InteractionController가 현재 손 상태 구성
→ HandPolicy가 Pickup/Release 결정
→ ItemCarrier가 물리 상태 변경 및 위치 이동
→ 놓을 때 Collider 복구 + Rigidbody 중력 재개
```

현재 손에 총이 있거나, 이미 물건을 들었거나, UI 획득 연출 중이면 `HasFreeHands`가 false입니다. 조준 대상은 카메라 중앙에서 가장 가까운 물체를 찾은 다음 플레이어 몸통의 기준점에서 다시 벽에 가려지는지 확인합니다. 카메라가 벽 너머 물체를 보더라도 플레이어 손이 벽을 뚫고 집지 않도록 두 번 검사하는 것입니다.

근거: `InteractionController.cs` 36~43행, 134~150행.

### 4.2 핵심 코드 읽기: 언제 집을 수 있는가

`HandPolicy.cs` 22~28행 중 발췌:

```csharp
if (!input.Enabled) return state.Carrying ? HandCommand.Release : HandCommand.None;
if (input.ToggleEquipment && state.Equipped) return HandCommand.Unequip;
if (state.Carrying && !input.GrabHeld) return HandCommand.Release;
if (input.GrabPressed && input.GrabHeld && !state.Carrying && !state.Equipped && !state.PickupBusy)
    return HandCommand.Pickup;
```

1. 첫 줄은 조작할 수 없게 되면 손에 든 물건을 놓으라는 뜻입니다. 이미 빈손이면 아무것도 하지 않습니다.
2. 둘째 줄은 총을 든 채 숫자 1을 누르면 총을 해제하라는 뜻입니다.
3. 셋째 줄은 물건을 들고 있더라도 버튼을 계속 누르지 않으면 놓으라는 뜻입니다.
4. 마지막 조건은 “이번 프레임에 새로 눌렀고, 아직 누르고 있고, 손이 비어 있고, 연출 중이 아니다”를 모두 요구합니다. 버튼을 누른 채 새 물체를 지나간다고 자동으로 계속 집지 않습니다.

이 함수는 Unity 오브젝트를 움직이지 않습니다. 명령만 고르므로 작은 단위 검사를 하기 쉽고 다른 입력 장치에서도 재사용할 수 있습니다.

### 4.3 집은 물건이 이동하는 과정

`ItemCarrier.TryGrab`은 먼저 기존 속도, 충돌 상태 및 표시 크기를 확인합니다. 보유 중에는 `isKinematic=true`, `useGravity=false`로 바꾸고 물건의 Collider들을 일시 비활성화합니다. 그래서 물리 엔진이 손의 이동과 서로 경쟁하지 않습니다. 대신 이 운반 코드는 `BoxCastNonAlloc`과 `OverlapBoxNonAlloc`으로 길과 도착 공간을 직접 검사합니다.

선반의 턱에 걸리지 않게 이동은 세 단계입니다.

1. 현재 위치에서 0.08만큼 위로 들어 올립니다.
2. 들어 올린 높이를 유지하며 선반에서 수평으로 꺼냅니다.
3. 중앙 조준선의 보유 지점을 따라갑니다.

중앙점의 위치는 화면 좌표를 임의로 옮기는 방식이 아닙니다. 카메라 중앙의 선 위에서 플레이어 깊이와 보유 거리를 더한 지점을 구합니다. 기본 `holdDistance`는 0.8입니다. 장애물이 있으면 포인터 위치보다 물리 충돌을 우선하여 물건이 더 멀리 가지 못합니다.

근거: `ItemCarrier.cs` 46~79행, 93~109행, 122~127행, 162~188행.

### 4.4 놓을 때의 코드

`ItemCarrier.Restore`, 217~223행:

```csharp
body.isKinematic = false;
body.useGravity = true;
body.collisionDetectionMode = collisionMode;
body.linearVelocity = Vector3.zero;
body.angularVelocity = Vector3.zero;
body.WakeUp();
```

1. 직접 위치를 붙잡던 상태를 끝내고 Unity 물리 계산에 넘깁니다.
2. Unity 중력을 켭니다.
3. 집기 전에 사용하던 충돌 감지 방식을 되돌립니다.
4. 과거 속도를 되살리지 않고 현재 지점에서 정지 상태로 출발하게 합니다.
5. 회전 속도도 정리합니다.
6. 잠들어 있던 물리 계산을 깨웁니다.

이 코드는 바닥 좌표로 순간 이동시키지 않습니다. 실제 떨어지는 시간과 충돌은 Rigidbody가 계산합니다. 파일의 과거 `ReleaseRecovery` 저장 형식은 호환 때문에 남아 있지만, 예전의 공중 대기/바닥 찾기를 현재 운반 규칙으로 설명하면 안 됩니다.

### 4.5 Inspector 설정과 외형 교체

운반 대상에는 `CatInteractionItem`이 필요하고, 이 컴포넌트는 루트의 `BoxCollider`와 `Rigidbody`를 요구합니다. `Kind`를 `CarryOnly`로 설정하고 `Visual Root`에 실제 모델이 들어 있는 자식을 지정합니다. `Visual Root`로 자기 자신이나 다른 계층의 모델을 연결하면 `RefreshVisual`이 유효한 외형으로 취급하지 않습니다.

외형을 교체할 때는 루트의 식별자와 물리 컴포넌트를 유지하고 `Visual` 아래의 모델을 교체합니다. 그 다음 컴포넌트 메뉴의 **Visual 크기에 맞춰 충돌 갱신**을 실행합니다. 이 함수가 렌더러들의 경계 크기를 루트 좌표로 바꿔 BoxCollider의 중심과 크기를 계산합니다. 손에 들고 있는 동안 바꾸지 말고 내려놓은 상태에서 갱신하세요.

`InteractionController`의 `Reach`는 선택 거리, `Hold Distance`는 든 위치입니다. 둘은 목적이 다르므로 “더 멀리 있는 물건을 집고 싶다”면 Reach를, “들었을 때 얼굴과 겹친다”면 Hold Distance를 조정합니다. `Solid Layers`는 벽을 포함한 충돌 검사 범위이므로 특정 아이템 레이어만 넣으면 벽 가림 검사가 약해질 수 있습니다.

### 4.6 수정 후 확인할 일

- 빈손에서 좌클릭 유지/해제가 각각 한 번의 집기/놓기로 이어지는지 확인합니다.
- 선반에서 꺼낼 때 위→수평→보유 순서가 자연스러운지 확인합니다.
- 벽에 가까이 다가가도 물건이 벽을 통과하지 않는지 확인합니다.
- 들고 있던 중 ESC 또는 창 포커스 상실 시 물건이 공중에 고정되지 않는지 확인합니다.
- 놓은 물체가 현재 위치에서 떨어지는지, 예전 위치나 바닥으로 순간 이동하지 않는지 확인합니다.

## 5. F06 — F 아이템 저장과 Enemy 포획

### 5.1 현재 코드는 레이어 이름만으로 등록하지 않습니다

현재 `ItemInteractor.cs`에는 `Item`/`Enemy` 이름을 읽는 `LayerMask`, `NameToLayer`, `itemLayers`, `enemyLayers` 판정 코드가 없습니다. 과거 요청이나 작업 설명에 레이어 사용이 적혀 있더라도, **현재 읽은 소스만 기준으로는 레이어를 붙이기만 하면 F 대상이 되는 구조가 아닙니다.**

현재 대상 발견 경로는 다음 세 가지입니다.

1. `Targets` 목록이나 실행 중 `Register`로 명시적으로 등록한 대상.
2. 충돌체에서 부모 방향으로 찾은 `ItemPickup`에 유효한 아이템 데이터가 있고, `CanPickup=true`인 대상. `Use Item Pickup Registration`이 켜져 있어야 합니다.
3. 충돌체 또는 그 부모에 `ThiefController`가 있는 대상. 발견하면 Enemy 종류로 등록합니다.

코드는 충돌체에서 위쪽으로만 올라갑니다. 부모에 큰 Collider가 있고 아래 자식에만 `ItemPickup`이 있으면 자동 발견 경로가 그 자식으로 내려가지는 않습니다. 또한 `useItemPickupRegistration`이 꺼졌을 때 Enemy의 **비트리거 선택**과 **트리거 허용** 코드 조건이 서로 다릅니다. 비트리거는 `ThiefController`를 찾지만 트리거 허용은 해당 옵션을 확인합니다. 이 차이는 Inspector 옵션을 바꿀 때 검사해야 할 현재 구조의 경계입니다.

근거: `ItemInteractor.cs` 118~138행, 162~201행. 일반 Raycast가 어떤 레이어를 통과하는지는 `InteractionController.solidLayers`에서 별도로 제한됩니다.

### 5.2 일반 아이템의 저장 흐름

```text
F 새 누름 → 입력 권한 및 설치의 F 점유 확인
→ 중앙 조준선/거리/벽 검사
→ 등록 대상 또는 ItemPickup 데이터 발견
→ RegisteredInteractionPolicy가 저장 가능 판단
→ IItemInventory.TryAdd
→ 성공한 새 물체만 숨김/소비형 실행 인스턴스 제거
```

기존 `HotbarManager`는 `HotbarStore`라는 작은 연결부를 통해 `IItemInventory` 계약으로 사용합니다. `ItemInteractor`가 필요한 기능은 빈칸 조회와 추가 요청뿐입니다. UI 선택 강조, 마우스 휠, 전체 슬롯 그림을 직접 관리하지 않습니다. 다른 저장소를 연결하려면 `TrySetInventory`를 사용하되 획득을 처리하는 중에는 교체가 거부됩니다.

### 5.3 핵심 코드 읽기: 손 상태를 바꾸지 않는 F

`ItemInteractor.Collect`, 227~239행 중 발췌:

```csharp
entry.reserved = true;
var data = entry.Data();
if (!store.TryAdd(data, out int index)) { entry.reserved = false; return false; }
var collected = entry.target;
bool consumeInstance = collected.TryGetComponent<ItemPickup>(out var pickup) && pickup.ConsumeWorldInstance;
collected.SetActive(false);
```

1. 처리 중인 물체에 “다른 획득이 끼어들지 말라”는 예약 표시를 합니다.
2. 저장할 아이템 데이터를 얻습니다.
3. 빈칸이 없거나 저장이 실패하면 예약을 취소하고 물체를 그대로 둡니다.
4. 성공한 물체만 별도로 잡아 둡니다.
5. 재설치 때 새로 생성하는 물건인지 확인합니다.
6. 지금 획득한 월드 물체를 숨깁니다.

이 함수에는 총을 집어넣거나 손에 든 다른 물체를 놓는 호출이 없습니다. 따라서 F로 새 아이템을 저장해도 기존 손 상태를 유지합니다. 소비형 아이템일 때는 이어지는 조건에서 실행 중이며 실제 씬 인스턴스인 경우에만 `Destroy`합니다. 프리팹 에셋을 파괴하는 경로로 사용하면 안 됩니다.

등록 항목의 `Item`을 비워 두면 `Data()`가 이름 기반의 실행 중 `HotbarItemData`를 만듭니다. 여기에는 월드 오브젝트를 프리팹으로 억지 연결하지 않습니다. 그러므로 “저장은 되었지만 나중에 설치할 모델 데이터가 없다”는 문제가 생길 수 있습니다. 설치·사용까지 필요한 아이템은 실제 데이터 에셋을 `ItemPickup` 또는 등록 항목에 연결해야 합니다.

### 5.4 Enemy는 현재 기절 조건과 라운드 카운트에 연결됩니다

현재 `Capture`는 `ThiefController.IsCapturable`이 false이면 예약을 풀고 실패합니다. 팀 Enemy 코드에서 `IsCapturable`은 `blackboard.IsStunned`를 반환합니다. 즉 **현재는 기절한 쥐만 F로 포획할 수 있습니다.** 노란 강조만 보고 포획 가능하다고 단정할 수는 없습니다. 강조 가용성에는 기절 조건이 직접 포함되어 있지 않기 때문입니다.

`ItemInteractor.cs` 253~260행:

```csharp
if (thief != null && !thief.IsCapturable)
{
    captured.Remove(target);
    entry.reserved = false;
    return false;
}
if (thief) thief.GetCaptured();
```

1. 쥐 담당 컴포넌트가 존재하지만 잡을 수 없는 상태인지 묻습니다.
2. 실패한 대상을 “이미 잡았다”는 목록에서 빼 줍니다.
3. 다시 시도할 수 있도록 예약을 풉니다.
4. 이번 시도는 실패로 끝납니다.
5. 잡을 수 있다면 팀 Enemy 코드가 가진 포획 절차를 호출합니다.

현재 팀 코드 [ThiefController.cs](../Assets/02Scripts/02Enemy/ThiefController.cs) 84~94행의 `GetCaptured`는 보물 정리 뒤 `GameManager.Instance?.OnRatCaught()`를 호출하고 쥐의 실행 인스턴스를 제거합니다. 따라서 현재 `ItemInteractor`의 주석과 `LastResult`에 남아 있는 “카운트 미구현”은 실제 연결과 맞지 않습니다. 이 장에서는 실행되는 코드가 우선이며, 이 문서 작성 중 해당 문자열을 고치지는 않았습니다.

`ItemInteractor.EnemyCaptured` 이벤트는 그 후에도 발생합니다. 새 수신자에서 다시 `OnRatCaught()`를 부르면 한 마리를 두 번 센 것으로 처리할 위험이 있습니다. 카운트는 한 경로에만 맡겨야 합니다. 등록 항목을 Enemy로 만들었지만 `ThiefController`가 없다면 현재 코드는 성공 메시지/이벤트를 보낼 수 있어도 물체를 자동으로 숨기지는 않습니다. Enemy 확장 시 놓치기 쉬운 경계입니다.

### 5.5 자주 생길 수 있는 오류와 확인 순서

| 증상 | 코드에서 확인할 지점 | 확인 방법 |
| --- | --- | --- |
| F가 먹지 않음 | 입력 차단/설치 점유/거리/대상 등록 | `GameplayInput`, `ClaimsInteractionInput`, `Pickup Range`, Collider의 부모 경로 순으로 확인 |
| Item 레이어인데 못 줍는 물건 | 현재 자동 발견은 `ItemPickup` 중심 | ItemPickup의 데이터 연결 또는 Targets 등록 확인 |
| 노란 쥐를 F로 못 잡음 | `IsCapturable` 기절 조건 | Enemy의 기절 상태 확인. 강조와 포획 권한은 같지 않음 |
| 보관 후 사용할 수 없음 | 임시 데이터 또는 prefab 없는 데이터 | 실행 중 이름만 만든 데이터인지, 정상 `HotbarItemData` 에셋인지 확인 |
| 손에 든 물건/총이 사라짐 | F 외의 경로가 손 상태를 변경하는지 | F 자체는 손 상태 변경을 요청하지 않으므로 추가 이벤트 수신자와 비활성화 경로 점검 |
| 포획 카운트가 두 번 증가 | `EnemyCaptured` 구독자가 다시 카운트하는지 | GetCaptured와 수신자를 함께 검색 |

F 경로 수정 뒤에는 빈손/물건 보유/총 장착의 세 상태에서 각각 일반 아이템과 기절한 Enemy를 검사해야 합니다. 인벤토리 빈칸이 없는 경우에는 아이템이 사라지지 않는지, Enemy 포획에는 저장소 빈칸이 필요하지 않은지 함께 확인합니다.

## 6. F07 — 중앙 커서, 노란 강조, UI 획득 연출

### 6.1 중앙 구체와 산탄 원의 차이

`CenterCursor`의 기본 지름은 6픽셀입니다. 카메라 바로 앞에 구체를 놓고, 시야각과 화면 높이로 실제 크기를 역산합니다. 그래서 화면 해상도나 카메라 거리와 관계없이 화면 가운데의 작은 표시로 보이게 합니다. 커서 잠금이 풀리면 구체도 숨깁니다.

`CatCursorSphere.shader`는 반투명 흰색과 얇은 테두리를 그립니다. 실제 벽의 깊이에 가려지지 않도록 표시용 셰이더로 동작합니다. 이 구체 자체에 아이템 선택 Collider를 붙이는 방식이 아닙니다. 산탄 원은 이와 별도의 `ShotSpreadRing`입니다.

### 6.2 노란 강조는 원본 재질을 바꾸지 않습니다

`ItemHighlight`는 `IHighlightSource`가 제공한 Renderer와 메시를 사용해 면, 후광, 테두리를 추가로 그립니다. `InteractionController`가 만든 이 보조 객체가 담당하며, 모든 물체에 같은 무거운 상호작용 컴포넌트를 붙여야 하는 구조는 아닙니다.

일반 모델에는 `HighlightTarget`만 붙여도 강조 대상으로 사용할 수 있습니다. 단, 조준용 활성 Collider가 필요합니다. 이 컴포넌트는 **강조 권한만** 주므로 물건 집기나 F 저장까지 자동 허용하지 않습니다. 들 수 있는 물건은 `CatInteractionItem`, F 데이터 아이템은 `ItemPickup`이나 등록 목록을 별도로 갖춰야 합니다.

렌더링 과정은 다음과 같습니다.

1. 대상을 바꾸면 그 모델의 메시 목록을 준비합니다.
2. 일반 메시의 읽기가 허용되면 같은 위치 정점의 법선을 모아 테두리가 갈라지는 현상을 줄입니다.
3. 움직이는 스킨 메시라면 `BakeMesh`로 현재 본 자세를 메시 모양에 반영합니다.
4. `Draw`에서 면, 후광, 테두리를 그립니다.
5. 최근 대상 하나는 캐시해서 조준이 잠깐 벗어났다 돌아와도 매번 다시 만들지 않습니다.
6. 모델 교체/비활성화/폐기 때 직접 만든 임시 메시를 정리합니다.

근거: `ItemHighlight.cs` 25~81행, 84~132행. 강조 셰이더 [CatInteractionOverlay.shader](../Assets/02Scripts/01Player/Interaction/Presentation/CatInteractionOverlay.shader)의 `_Color`, `_Extrusion`, `_Cull`로 색과 바깥 확장량을 정합니다. `_Extrusion`은 월드 공간 두께이므로 화면 픽셀 고정 두께와 다릅니다.

스킨 강조의 크기는 특별히 검사해야 합니다. 현재 45·109행은 `BakeMesh(..., true)`를 사용하고 120행은 `localToWorldMatrix`로 그립니다. 베이크 시 스케일을 포함시키는 설정과 그릴 때의 변환 행렬이 함께 적용되므로, 모델/부모 스케일이 1이 아닌 경우의 실제 결과를 비교해야 합니다. 이는 현재 코드에서 발견한 **검증 지점**이며, 이 문서 작업만으로 특정 RAt 크기 문제를 재현·수정했다고 주장하지 않습니다.

### 6.3 UI 획득 연출은 저장소가 아닙니다

`InventoryPickupEffect`는 등록된 `inventoryTarget`이 존재하고 활성 Canvas 위에 보일 때만 시작합니다. 연결이 없으면 임의의 화면 모서리로 보내지 않고 실패합니다. `RegisterTarget(null)`로 해제하거나 연출 중 UI가 사라지면 예약된 월드 물체 상태를 복구합니다.

기본 연출 값은 시간 0.55초, 화면 위로 휘는 높이 100, 마지막 크기 0.12배, 촬영 해상도 256입니다. 실제 Inspector 저장값은 별도입니다. 연출은 `Time.unscaledDeltaTime`을 쓰므로 `timeScale=0`만으로 자동 정지되지 않습니다. UI를 닫거나 연출 담당을 비활성화하면 취소 경로가 실행됩니다.

핵심 순서는 다음과 같습니다.

```text
UI 목적지 유효성 확인
→ 실제 외형을 임시 카메라로 한 번 촬영
→ PickupReservation이 월드 표시·충돌·물리 상태를 저장하고 숨김
→ 아이콘이 UI로 이동
→ 도착하면 예약 완료 + Collected(itemId)
→ 취소하면 원래 표시·충돌·물리 상태 복구
```

`InventoryPickupEffect.cs` 188~197행 중 발췌:

```csharp
PickupReservation completed = reservation;
reservation = null;
activeTarget = null;
bool committed = completed != null && completed.TryCommit(out itemId);
ClearPresentation();
if (committed) collected?.Invoke(itemId);
```

1. 끝낼 예약을 임시 변수로 보관합니다.
2. 현재 예약부터 비웁니다. 완료 이벤트 안에서 다시 호출해도 같은 물체를 두 번 완료하지 않기 위함입니다.
3. 현재 목적지 연결도 비웁니다.
4. 물체가 아직 유효한 경우에만 예약을 완료합니다.
5. 아이콘과 촬영 텍스처를 정리합니다.
6. 성공했을 때만 아이템 ID를 알립니다.

이 발췌는 읽기 쉽게 중간의 `string itemId = null;` 선언을 생략한 것입니다. 그대로 복사하여 독립 함수로 실행할 코드가 아니라 원래 함수의 핵심 순서를 설명한 코드입니다.

완료 알림은 저장 성공을 보장하지 않습니다. `Collected` 수신자가 연결되지 않았으면 아이콘 연출은 끝나도 Hotbar에 저장되지 않을 수 있습니다. F 획득은 별도의 직접 저장 경로이므로 F로 넣는 모든 아이템에 이 비행 연출이 자동 붙는다고 설명하면 안 됩니다.

### 6.4 촬영용 객체와 리소스의 정리

`ItemIconCapture`는 임시 카메라와 Light, 표시용 메시를 월드의 먼 위치에 만들고 전용 레이어 31만 찍습니다. 원본 Renderer나 공유 Material은 바꾸지 않고 필요한 PropertyBlock 값을 복사합니다. URP에서는 지원되는 `RenderPipeline.StandardRequest`로 촬영합니다. 지원하지 않거나 실패하면 false를 반환합니다.

성공/실패 여부와 관계없이 `finally`에서 임시 루트를 먼저 비활성화하고 생성한 메시를 파괴합니다. 촬영한 RenderTexture는 연출이 끝날 때 `Release`합니다. “한번 생성한 아이콘을 영구 에셋으로 저장”하는 기능은 아닙니다.

### 6.5 수정 후 확인할 일

- 화면 비율을 바꿔도 중앙 구체가 가운데에 있고 크기가 과도하게 커지지 않는지 확인합니다.
- 일반 메시와 움직이는 스킨 메시를 각각 강조하고, 오브젝트 스케일을 작게/크게 한 경우도 비교합니다.
- 원본 Material 색이 바뀌지 않는지, 조준을 벗어나면 추가 강조만 없어지는지 확인합니다.
- UI 목적지 미등록/비활성 상태에서 물체가 사라지지 않는지 확인합니다.
- 정상 도착에서 이벤트가 1회인지, 연출 도중 UI 제거 시 이벤트 없이 물체가 복구되는지 확인합니다.
- 여러 번 획득한 뒤 임시 `CatItemIconPreview`나 RenderTexture가 계속 쌓이지 않는지 확인합니다.

## 7. F08 — 샷건 장착·조준·발사

### 7.1 장착과 손 자세

숫자 1의 의미는 기존 총을 보이게 하거나 숨기는 토글입니다. 소환 연출을 재생하는 방식이 아닙니다. `ShotgunEquipment`는 씬에 준비된 `weaponRoot`와 몸통 본 `motionRoot`를 연결받습니다. 총 루트는 손 본 아래가 아니라 플레이어 아래에 두고, 손의 변형 본을 총의 `leftGrip`과 `rightGrip` 위치로 맞춥니다.

매 프레임 이전 손 보정을 먼저 복구하고, Animator가 기본 자세를 계산한 뒤 `LateUpdate`에서 총 위치와 양손 잡는 위치를 다시 적용합니다. 이 순서가 필요한 이유는 손 위치를 계속 더하기만 하면 조금씩 틀어지기 때문입니다.

근거: `ShotgunEquipment.cs` 75~79행, 104~117행. 잘못 연결된 루트가 Player 자신이나 부모인 경우 모델을 끄다 Player 전체가 꺼지지 않도록 `HasValidBindings` 및 `SetWeaponVisible`에 방어 조건이 있습니다.

### 7.2 조준과 걷기 고정

장착이 준비된 상태에서 우클릭을 유지하면 `ShotgunCombat.IsAiming=true`가 됩니다. `AimZoom`은 이 값만 읽고 Cinemachine 렌즈의 시야각을 줄입니다. 기본 목표 시야각은 40도, 전환 시간은 0.16초입니다. Main Camera의 Transform을 사격 코드가 직접 움직이는 방식이 아닙니다.

`CombatPolicy.RestrictMovement`는 조준 중 이동 의도에서 달리기 플래그만 제거합니다. 따라서 우클릭 중 Shift를 눌러도 걷고, 이동키를 누르지 않으면 대기합니다. 조준했다고 자동으로 걷기 애니메이션을 재생하는 것은 아닙니다.

`IAimState`는 조준 여부와 카메라 방향으로 몸을 돌릴 필요만 알립니다. 이동과 카메라가 샷건 내부의 발사 횟수나 총구 계산을 알아야 할 필요가 줄어듭니다.

### 7.3 단발과 산탄의 기본값

| 설정 | 코드 기본값 | 바꾸는 위치와 의미 |
| --- | --- | --- |
| `shotInterval` | 0.55초 | ShotgunCombat. 새 발사 요청 사이 최소 간격 |
| `range` | 75 | ShotgunCombat. 광선 판정 거리 |
| `pelletCount` | 4 | ShotgunCombat. 한 발의 산탄 수. 코드상 1~16으로 제한 |
| `spreadDegrees` | 4도 | ShotgunCombat. 비조준 산탄 원뿔의 반각 |
| `aimedSpreadDegrees` | 1.5도 | ShotgunCombat. 조준 산탄 반각. 실제값은 비조준 각도보다 커지지 않게 제한 |
| `muzzleRadius` | 0.025 | ShotgunCombat. 총구/몸통 벽 겹침 확인 반경 |
| `fireMoment` | 0.12초 | ShotgunPose. 준비 자세에서 실제 명중 판정을 할 시각 |
| `lineWidthPixels` | 1.4픽셀 | ShotSpreadRing. 산탄 범위 원의 선 두께 |
| `outlineWidthPixels` | 0.6픽셀 | ShotSpreadRing. 범위 원 바깥 어두운 선 두께 |

좌클릭 유지 자동 발사는 없습니다. `CombatInput`이 `wasPressedThisFrame`만 전달하고 `CombatPolicy.TryRequest`가 간격을 확인합니다. 너무 빠르게 여러 번 누른 입력을 나중에 모아서 모두 발사하는 발사 버퍼도 없습니다.

### 7.4 클릭부터 명중까지

```text
좌클릭 새 누름
→ 장비/클립/카메라/총구 연결이 준비되었는가?
→ 덫이 좌클릭을 쓰지 않는가?
→ 발사 간격이 지났는가?
→ ShotgunPose.BeginShot
→ Animator가 총 올리기/반동 곡선 계산
→ 장비가 총과 양손 최종 자세 적용
→ 몸이 카메라 쪽으로 돌아왔고 fireMoment가 되었는가?
→ 총구 벽 검사 + 산탄 4개의 히트스캔
→ PelletResolved / IShotReceiver / ShotFired 알림
```

총구 판정은 손의 최종 자세가 적용된 뒤 `PoseApplied` 이벤트에서 실행합니다. 준비 자세를 시작하자마자 이전 프레임 총구 위치로 쏘는 문제를 피하기 위한 순서입니다. 발사 전에 몸 방향이 카메라 수평 방향과 충분히 가까운지 내적 0.94 기준으로 기다립니다. 이후 `AcknowledgeShot`으로 실제 발사를 인정한 뒤 반동을 진행합니다.

근거: `ShotgunCombat.cs` 44~59행, 63~120행. 자세의 `ReadyToFire`, `SampleTime`은 `ShotgunPose.cs` 23행 및 68~69행.

### 7.5 카메라 조준선과 총구 광선은 다릅니다

카메라에서 쏜 선은 **어디를 겨냥할지** 고릅니다. 실제 산탄은 **총구 앞쪽 +Z**에서 출발합니다. 이 두 위치가 다르기 때문에 총구가 벽 안에 있는데 카메라만 벽 너머를 보는 경우를 따로 검사해야 합니다.

`HitscanQuery.Cast`는 다음 순서로 검사합니다.

1. 몸통 기준점이 벽 안에 겹쳤는지 확인합니다.
2. 몸통에서 총구까지 굵기가 있는 선을 쏘아 중간 벽을 찾습니다.
3. 총구 자체가 벽 안에 겹쳤는지 확인합니다.
4. 문제가 없으면 총구의 실제 앞 방향에 산탄 각도를 더해 광선을 쏩니다.
5. 플레이어 자신과 자식 충돌체는 제외하고 가장 가까운 외부 충돌체를 고릅니다.

결과는 `Miss`(빗나감), `Hit`(명중), `Blocked`(총구 쪽이 막힘), `QueryOverflow`(검사 배열이 꽉 차 정확하게 판단할 수 없음)로 나뉩니다. 배열이 꽉 찼을 때 멀리 있는 적을 맞았다고 추측하지 않고 안전하게 막습니다. 기본 검사 배열은 64칸이고 매 프레임 새 배열을 만들지 않고 재사용합니다.

핵심 발췌, `ShotgunCombat.cs` 111~117행:

```csharp
LastHit = query.Cast(guardOrigin.position, muzzle, range, hitLayers.value, spread, muzzleRadius);
PelletResolved?.Invoke(LastHit);
if (LastHit.Collider && LastHit.Kind == ShotHitKind.Hit)
{
    var receiver = LastHit.Collider.GetComponentInParent<IShotReceiver>();
    receiver?.ReceiveShot(LastHit);
}
```

1. 한 산탄의 실제 충돌 결과를 계산합니다.
2. 결과가 빗나감/막힘이어도 관심 있는 코드에 알립니다.
3. 실제 명중한 경우에만 맞은 대상에게 알립니다.
4. 충돌체 또는 부모에서 `IShotReceiver` 약속을 구현한 코드를 찾습니다.
5. 그 코드가 있다면 해당 대상의 처리 방법을 실행합니다.

현재 팀 [ThiefShotReceiver.cs](../Assets/02Scripts/02Enemy/PlaceHolder00/ThiefShotReceiver.cs) 10~12행은 이 알림을 받아 `TakeStun(stunDuration)`을 호출합니다. 기본값은 1.5초입니다. 산탄 하나마다 수신될 수 있으므로 “한 번의 클릭은 정확히 한 번만 대상 효과를 호출한다”고 가정하면 안 됩니다. 새로운 체력·피해량을 추가할 때는 산탄별 피해인지 샷건 한 발당 피해인지 먼저 결정해야 합니다.

### 7.6 산탄 원과 모델 축

`ShotSpreadRing`은 현재 퍼짐 각도와 카메라 시야각을 읽어 화면 반지름을 계산합니다. 원이 산탄 방향을 결정하는 것이 아니라, 전투 코드가 결정한 각도를 따라 표시합니다. 조준 시 시야각도 줄어들므로 원의 화면 크기는 퍼짐 각도와 시야각 두 값의 영향을 받습니다.

현재 산탄은 완전한 난수 방식이 아닙니다. 네 방향을 균등하게 배치하고 `ShotsFired`에 따라 회전각과 반지름이 바뀌는 결정적인 패턴입니다. 따라서 “확률적으로 매번 독립 무작위 산탄”으로 문서화하면 실제 코드와 다릅니다. `ShotgunCombat.cs` 105~110행이 분포를 바꾸는 위치입니다.

현재 샷건 원본 모델의 총열은 로컬 -X 방향이고, 별도 `muzzle` 표시점은 +Z가 발사 방향입니다. `ShotgunPose.Apply` 107~117행은 이 모델 축을 보정합니다. 다른 무기 모델을 넣었는데 총이 옆으로 돌아가면 입력 코드보다 모델 축/총구 표시점/이 회전 보정을 먼저 확인해야 합니다.

### 7.7 추가 기능을 붙일 위치

| 원하는 변경 | 우선 수정할 지점 | 함께 확인할 것 |
| --- | --- | --- |
| 손잡이 위치 변경 | ShotgunEquipment의 Grip Transform, 로컬 자세 값 | 좌우 손 본 스케일은 함부로 변경하지 않음 |
| 반동 세기/총 올리기 모양 | Shotgun_Fire 클립의 기존 바인딩, ShotgunPose | `raiseWeight/recoilDistance/recoilPitch` 필드명을 바꾸면 클립 연결 영향 |
| 줌 배율/속도 변경 | AimZoom Inspector | 실제 Cinemachine 카메라에 연결되어 있는지 |
| 산탄 수/퍼짐 변경 | ShotgunCombat Inspector | 산탄 원, 적 수신 횟수, 벽 가까이 발사 모두 검사 |
| 피격 대상 추가 | 대상 코드에 IShotReceiver 구현 | 플레이어가 상대 체력 구현을 직접 참조하지 않게 유지 |
| 게임패드 입력 | ICombatInputSource 구현 후 SetInputSource | 메뉴 복귀/포커스 상실 때 Reset 계약도 확인 |
| 효과음/탄흔 추가 | ShotFired/PelletResolved 수신자 | 각 이벤트가 샷건 한 발 기준인지 산탄 기준인지 구분 |

지금 코드만으로 탄약, 재장전, 저장 가능한 총 아이템, 데미지 수치, 네트워크 판정까지 구현되어 있다고 할 수 없습니다. 실제 대상 효과는 수신자 코드와 씬 연결에 달려 있습니다.

### 7.8 수정 후 확인할 일

- 숫자 1 두 번으로 장착→해제가 되고 손 자세가 원래 애니메이션으로 돌아오는지 확인합니다.
- 우클릭 유지 중 Shift 이동은 걷기, 해제 후 Shift 이동은 달리기인지 확인합니다.
- 좌클릭 유지로 연속 발사되지 않는지, 새 클릭당 산탄 수가 설정값과 같은지 확인합니다.
- 벽 바로 앞/총구가 벽 안/카메라만 적을 보는 위치에서 관통하지 않는지 확인합니다.
- 샷건 발사 뒤 이동·점프·대기 본이 계속 움직이는지 확인합니다. Animator 상태 이름만 바뀌는 것은 충분한 증거가 아닙니다.
- 덫 선택 중 같은 좌클릭으로 총도 발사되지 않는지 확인합니다.
- 메뉴 진입/복귀 클릭으로 오발하지 않는지 확인합니다.

## 8. SOLID 관점의 코드리뷰

이 표의 “확인된 구조”는 실제 소스에서 확인한 사실입니다. “검토 지점”은 지금 당장 재현한 오류가 아니라 향후 변경 시 주의할 위험입니다. 권장 구조가 있다고 해서 이번 문서 작성에서 자동으로 리팩터링하지 않습니다.

| 원칙 | 확인된 장점 | 현재 한계와 검토 지점 | 필요할 때의 개선 방향 |
| --- | --- | --- | --- |
| S: 한 클래스의 책임을 좁힘 | HandPolicy는 결정, ItemCarrier는 운반, HitscanQuery는 물리 조회, ItemHighlight는 표시, PickupReservation은 예약 복구 | InteractionController는 대상 조회·손 조정·강조 연결을 함께 맡고 ItemInteractor는 등록/발견/저장/Enemy 연결을 함께 담당 | 대상 종류가 크게 늘어날 때만 대상 조회와 실행 연결을 별도 서비스로 나눔. 지금부터 빈 추상화만 늘리지 않음 |
| O: 기존 코드를 덜 고치며 확장 | IHighlightSource, IItemInventory, IPickupPort, IShotReceiver로 다른 구현을 연결 가능 | ShotgunCombat은 구체적인 ShotgunEquipment/ShotgunPose에 의존. ShotgunPose는 현재 모델의 -X 축을 가정 | 다른 무기가 실제로 추가될 때 모델별 자세 어댑터나 작은 장비 계약 검토 |
| L: 약속을 지키면 대체 가능 | 입력 공급자와 저장소를 주입할 수 있고 실패는 bool로 알림 | 사용자 저장소가 성공 반환 전에 저장하지 않거나 예외를 던지면 예약과 월드 상태가 어긋날 수 있음 | `TryAdd=false`면 아무 변화 없음, `true`면 한 번 저장 완료라는 계약을 테스트로 고정 |
| I: 필요한 기능만 요청 | IPrimaryAction/IInteractionAction/IShotSpreadState/IAimState가 서로 작음 | IItemInventory는 HotbarItemData에, IPickupPort는 CatInteractionItem/Camera에 의존 | 이 계약은 이 프로젝트 내부의 분리에는 도움이 되지만 완전 엔진 독립은 아님. 다른 엔진 이식 시 데이터/표시 연결부 교체 |
| D: 규칙이 세부 구현에 덜 의존 | HandPolicy/CombatPolicy는 Unity 타입 없이 판단. 손과 저장은 작은 계약 사용 | ItemInteractor가 ThiefController를 직접 찾고 호출. 표시·물리 클래스는 Unity API 의존 | Enemy 종류가 늘면 포획 가능 여부/실행을 제공하는 작은 공통 계약 검토. 팀 Enemy 코드 협의 전 임의 변경 금지 |

### 8.1 현재 코드에서 문서와 실행이 다른 부분

1. **레이어 기반 F 자동 획득:** 현재 ItemInteractor는 레이어 이름이 아니라 등록/ItemPickup/ThiefController로 대상을 찾습니다. 과거 설명을 그대로 믿고 Layer만 바꾸면 작동하지 않을 수 있습니다.
2. **쥐 카운트 미구현이라는 주석:** 현재 GetCaptured는 GameManager.OnRatCaught를 호출합니다. 실행 결과 설명은 “기절한 쥐 포획이 팀 라운드 카운트로 연결됨”이 맞습니다.
3. **UI 획득과 F 저장의 관계:** UI 비행 연출 자체는 Hotbar 저장소가 아니며, F의 직접 저장과 별개입니다.
4. **미사용으로 보이는 선언:** RegisteredCommand에는 현재 정책이 내보내지 않는 Hold/Drop 값이 남아 있습니다. 코드에서 사용하지 않는 열거값과 파일 전체 미사용은 다른 문제입니다. 지우기 전 직렬화/외부 코드 참조까지 조사해야 하므로 이 문서에서 삭제하지 않습니다.

### 8.2 오류 재현 없이 단정하면 안 되는 부분

- 스킨 강조의 이중 스케일 가능성은 현재 `BakeMesh(true)`와 변환 행렬의 조합에서 확인할 검토 지점입니다. 대상별 크기 비교가 필요합니다.
- `EnemyCaptured` 수신자가 카운트를 다시 늘리는지 확인하지 않고 이중 카운트 버그가 있다고 단정하지 않습니다.
- `TryAdd`나 피격 이벤트 수신자가 예외를 던졌을 때 중간 상태가 남을 수 있지만, 실제 수신자와 Console을 확인하기 전 현재 오류 원인으로 단정하지 않습니다.
- `ItemHighlight`는 최근 대상의 Renderer 목록을 캐시합니다. 외형 교체 시 갱신 계약을 지켜야 합니다. 모든 동적 교체 사례가 새로 검증된 것은 아닙니다.

## 9. 추적 가능한 변경·검증 기록 남기는 방법

수정할 때는 “상호작용 수정” 한 줄보다 아래처럼 기능 ID와 실제 영향을 적어 두면 다음 사람이 빠르게 찾을 수 있습니다.

```text
기능: F06 F 획득
요청: 새로 만든 덫을 F로 저장한다.
원인/근거: ItemPickup.Item 연결 여부와 부모 Collider 경로를 확인했다.
변경: 어떤 코드/씬 연결을 바꿨는지 정확한 경로를 기록한다.
보존: 기존 GUID, 팀 Enemy, 다른 씬 등 실제 보존을 검사한 범위를 적는다.
검증: 빈손/총/물건 보유 + 빈칸 있음/없음 + 벽 가림을 각각 기록한다.
결과: 통과, 실패, 미검증을 나누고 실패 로그 위치를 연결한다.
```

과거 작업 로그의 PASS는 당시 버전과 당시 검사 조건에 대한 기록입니다. 현재 코드가 바뀌었다면 이전 PASS를 현재 통과 결과로 복사하지 않습니다. 특히 이 장의 F06은 과거 단순 비활성화 설명보다 현재 Enemy 포획/라운드 연결을 우선하여 읽어야 합니다.
