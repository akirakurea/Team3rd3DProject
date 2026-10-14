# 인벤토리와 덫

## F09 인벤토리 데이터와 화면

인벤토리는 **아이템을 기억하는 보관함**, 핫바 UI는 **그 보관함을 그리는 화면**입니다. 화면에 그림이 없다고 곧바로 데이터가 사라진 것은 아닙니다. 반대로 덫 미리보기가 보인다고 실제 덫이 설치된 것도 아닙니다. 이 세 가지를 나누어 확인해야 합니다.

### 현재 연결

| 구성 | 실제 담당 | 하는 일 |
|---|---|---|
| 아이템 설명서 | [HotbarItemData.cs](../Assets/02Scripts/04Systems/HotbarItemData.cs) | 이름, 아이콘, 설치할 프리팹을 보관하는 ScriptableObject |
| 5칸 보관함 | [HotbarManager.cs](../Assets/02Scripts/04Systems/HotbarManager.cs) | 각 칸의 데이터와 선택 칸 관리, 빈칸 추가·사용, 휠 입력 |
| 보관함 화면 | [HotbarUI.cs](../Assets/02Scripts/04Systems/HotbarUI.cs) | 아이콘·이름·선택 테두리를 갱신 |
| 월드에서 줍기 | [ItemInteractor.cs](../Assets/02Scripts/04Systems/ItemInteractor.cs), [ItemPickup.cs](../Assets/02Scripts/04Systems/ItemPickup.cs) | F 대상과 거리 등을 검사하고 저장 성공 후 월드 물건 처리 |
| 덫 아이템 자료 | [TrapItem.asset](../Assets/04Prefabs/Player/Inventory/TrapItem.asset) | `itemId=jaw_trap`, 표시 이름 덫, TrapIcon, Trap_Inventory 연결 |
| 설치할 실물 | [Trap_Inventory.prefab](../Assets/04Prefabs/Player/Inventory/Trap_Inventory.prefab) | 기존 덫의 변형 프리팹. ItemPickup과 TrapPickup을 추가한 구성 |

`HotbarManager`, `HotbarUI`, `HotbarItemData`는 GameUI 쪽 기존 구현을 재사용·연결한 경로입니다. 플레이어 기능을 위해 만든 별도 두 번째 인벤토리가 아닙니다. 팀원의 보물 HUD와도 별개입니다. 보물 5개와 인벤토리 5칸은 숫자만 같고 서로 다른 자료입니다.

저장된 `PlaytestScene01.unity`의 Player 인스턴스는 `readSelectionInput=1`, `readNumberKeys=0`으로 덮어씁니다. 첫 슬롯의 데이터는 TrapItem입니다. 즉 **첫 칸에 덫 1개를 들고 시작하고, 휠로 칸을 고르며, 숫자 1은 샷건에 남겨 둡니다.** 코드 기본값 `readNumberKeys=true`만 보고 현재 씬도 숫자키로 선택한다고 설명하면 틀립니다. `HotBar`의 화면에는 아이콘·테두리·이름 연결 5묶음이 있고 `buttons` 배열은 비어 있습니다. 코드에는 버튼 선택 지원이 있어도 현재 저장된 화면에 클릭 버튼이 연결됐다고 볼 수는 없습니다.

### 보관함 코드 읽기

[HotbarManager.TryAdd](../Assets/02Scripts/04Systems/HotbarManager.cs)는 다음 순서로 동작합니다. 아래는 메서드 본문에서 가져온 실제 코드입니다.

```csharp
slotIndex = -1;
if (item == null) return false;
slotIndex = FirstEmptySlot;
if (slotIndex < 0) return false;

slots[slotIndex] = item;
OnSlotChanged?.Invoke(slotIndex);
return true;
```

1. `-1`은 아직 물건을 넣은 칸이 없다는 표시입니다.
2. 넣을 데이터가 없거나 빈칸이 없으면 실패합니다. 기존 물건을 지우지 않습니다.
3. 가장 왼쪽 빈칸에 같은 아이템 자료를 저장합니다. 같은 종류라도 한 칸에 여러 개 쌓는 구조는 아닙니다.
4. `OnSlotChanged`는 “이 칸이 바뀌었어요”라는 알림입니다. UI가 이를 듣고 아이콘을 다시 그립니다. `?.`는 듣는 쪽이 없어도 오류를 내지 않고 지나간다는 뜻입니다.

`TryRemove(slotIndex, expected)`는 칸 번호뿐 아니라 예상 아이템도 검사합니다. 설치를 누르는 동안 칸이 바뀌었는데 엉뚱한 물건을 소모하는 일을 막는 장치입니다. `ConsumeSelected()`는 선택 칸을 사용 처리하는 편의 함수입니다. 설치 쪽은 더 엄격한 `TryRemove` 경로를 사용합니다.

UI는 `Start`에서 `HotbarManager.Instance`를 찾아 알림을 구독합니다. `RefreshSlot`은 슬롯 데이터가 있으면 이름을 표시하고, 아이콘도 있으면 이미지를 켭니다. 테두리는 `SelectedIndex`에 맞춰 한 칸만 켭니다. 매 프레임 모든 아이콘을 다시 그리는 방식이 아닙니다.

### F 획득과 손 상태

F 저장은 손에 총·다른 물건을 들고 있어도 **그 손 상태를 유지**합니다. 새 물건을 빈 인벤토리 칸에 넣는 행동입니다. 좌클릭 유지로 눈앞에 들고 다니는 기능과 다릅니다. 일반 대상의 발견·거리·벽 가림과 Enemy 조건은 [상호작용 문서](03_INTERACTION_COMBAT.md)의 F06을 참고합니다.

덫에는 `TrapPickup`이 `IItemPickupRule`과 `IItemPickupConsumption`을 구현합니다. 인터페이스는 “이 물건에게 물어볼 수 있는 질문 목록”입니다.

```csharp
public bool CanPickup => trap && !trap.IsUsed;
public bool ConsumeWorldInstance => true;
```

첫 줄은 덫 컴포넌트가 있고 아직 사용되지 않은 덫만 주울 수 있다는 뜻입니다. 두 번째 줄은 저장 성공 후 월드의 복제본을 없애야 한다는 뜻입니다. 사용된 덫을 다시 주워 무한 재사용하는 것을 막습니다. 프리팹 에셋 원본을 지우는 뜻은 아닙니다.

### 인벤토리를 수정할 때

- 슬롯 수를 바꾸려면 HotbarManager의 배열뿐 아니라 HotbarUI의 슬롯 연결 수도 함께 맞춥니다. 현재 UI는 두 배열 길이가 같다는 전제로 값을 읽습니다.
- 첫 아이템을 바꾸려면 해당 씬 Player 인스턴스의 HotbarManager `slots[0]` 연결을 바꿉니다. 원본 프리팹과 씬 덮어쓰기 중 어느 쪽을 편집하는지 먼저 확인합니다.
- 아이콘이나 이름은 TrapItem 같은 HotbarItemData에서 수정합니다. 모델 자체를 바꾸는 곳은 `placePrefab`입니다.
- UI만 교체할 때는 보관 데이터와 저장 메서드를 다시 만들지 말고 `OnSlotChanged`, `OnSelectedChanged` 알림을 새 UI가 듣게 연결합니다.
- 이 구조에는 파일 저장·불러오기 기능이 없습니다. 게임을 껐다 켜도 재고를 기억하는 기능으로 설명하면 안 됩니다.

## F10 덫 설치

### 한 번의 설치가 처리되는 순서

```text
휠로 TrapItem 선택
  → 화면 중앙에서 가상의 광선 발사
  → 실제 바닥을 찾고 월드 격자 중심으로 맞춤
  → 바닥 지지·거리·벽·빈 공간·기존 덫 검사
  → 초록/빨강 외형과 진행률 표시
  → 좌클릭 또는 F를 같은 칸에서 1초 유지
  → 완료 직전 같은 검사 재실행
  → 덫 복제본 생성 + 예상 슬롯 1개 소비
  → 라운드가 끝날 때 지울 설치물 목록에 등록
```

바닥에 격자 선을 그리거나 새 지면을 만드는 기능은 없습니다. “보이지 않는 모눈종이에 자리를 맞춘다”고 이해하면 됩니다. 게임 축은 Unity의 월드 X/Z 평면, 높이는 Y를 사용합니다. 과거 모델 작업에서 사용자가 정한 축 이름과 구별합니다.

### 나누어 맡은 스크립트

| 파일 | 책임 | 직접 맡지 않는 일 |
|---|---|---|
| [PlacementInput.cs](../Assets/02Scripts/01Player/Domain/PlacementInput.cs) | 좌클릭/F 중 어느 누름을 설치에 쓰는지 결정 | 키보드 장치 읽기·콜라이더 검사 |
| [PlacementHold.cs](../Assets/02Scripts/01Player/Domain/PlacementHold.cs) | 같은 칸·슬롯에서 시간 누적, 취소, 버튼 해제 대기 | 프리팹 생성·UI 표시 |
| [PlacementSurface.cs](../Assets/02Scripts/01Player/Interaction/Placement/PlacementSurface.cs) | 설치 위치·지지면·공간 검사 | 재고 소비·진행률 그리기 |
| [PlacementFeedback.cs](../Assets/02Scripts/01Player/Interaction/Placement/PlacementFeedback.cs) | 외형 미리보기·색·문구·진행률 | 실제 덫 기능 실행 |
| [TrapPlacement.cs](../Assets/02Scripts/01Player/Interaction/Placement/TrapPlacement.cs) | 위 담당자와 인벤토리를 연결하고 설치 확정 | 보상 조건 계산 |
| 같은 파일의 `PlacedTrapLifetime` | 이 설치기가 만든 복제본만 라운드 종료 때 정리 | 보관함·원본 프리팹 삭제 |
| [TrapPickup.cs](../Assets/02Scripts/01Player/Interaction/Placement/TrapPickup.cs) | 덫 회수 가능 여부, 발동 시 기존 쥐 기절 API 연결 | 적 행동트리·점수 시스템 구현 |
| [TrapRoundReward.cs](../Assets/02Scripts/01Player/Interaction/Placement/TrapRoundReward.cs) | 완벽 방어 라운드의 덫 1회 지급 | 치즈 아이콘 애니메이션 |

`TrapPlacement`가 `IPrimaryAction`과 `IInteractionAction`을 구현하므로 기존 발사·운반·F 상호작용이 설치 입력의 사용 여부를 물을 수 있습니다. 큰 새 플레이어 매니저를 하나 더 만드는 방식이 아닙니다.

### 입력 우선순위

| 상황 | 처리 |
|---|---|
| 덫 선택 + 손에 운반 중 물건 없음 | 좌클릭을 설치가 사용하며 총 발사·빈손 집기와 중복 실행하지 않음 |
| 샷건을 들고 덫 선택 | 총을 자동 해제하지 않고 덫 미리보기·설치 사용 |
| 물건을 좌클릭으로 운반 중 | 설치하지 않음 |
| F를 새로 누르는 순간 상호작용 대상으로 조회되는 대상 존재 | 기존 F 상호작용 우선. 최종 획득·포획 성공 여부와 관계없이 같은 누름을 설치에 재사용하지 않음 |
| 대상 없이 덫 선택 후 F 유지 | 설치 시작 가능 |
| 좌클릭과 F를 동시에 누름 | F 판단 우선 |
| 좌클릭 설치 중 F를 새로 누름 | 설치 취소, 새 F 상호작용에 입력 양보 |
| 설치 성공 후 계속 누름 | 두 버튼을 모두 뗄 때까지 다음 설치 시작 안 함 |
| 칸·슬롯 변경, 장애물, Esc, 메뉴·포커스 차단 | 진행 취소. 누른 채 복귀해도 즉시 설치하지 않음 |

입력 경계 처리는 `PlacementInput.Read`의 `Source` 상태에 있습니다. `None`은 시작 전, `Mouse/F`는 설치 중, `Collection`은 수집에 넘긴 누름, `Blocked`는 해제를 기다리는 상태입니다. 한 번의 클릭으로 집기·발사·설치가 동시에 실행되지 않도록 “이번 누름의 담당자”를 기억합니다. `HasInteractionTarget()`은 대상 존재를 확인하며 빈 인벤토리와 Enemy 포획 가능 여부는 실제 상호작용에서 나중에 검사합니다. 따라서 꽉 찬 보관함 앞의 물건이나 포획할 수 없는 쥐도 대상 조회에 잡히면 그 F 누름은 설치로 전환하지 않습니다.

### 1초 유지 코드 읽기

[PlacementHold.Tick](../Assets/02Scripts/01Player/Domain/PlacementHold.cs)의 핵심입니다.

```csharp
if (!valid || x != cellX || z != cellZ || slot != selectedSlot)
{ Cancel(true); return false; }
Elapsed = Math.Min(Math.Max(.01f, duration), Elapsed + Math.Max(0f, deltaTime));
if (Elapsed < Math.Max(.01f, duration)) return false;
IsHolding = false;
return true;
```

첫 줄은 유효하지 않은 위치, 다른 칸, 다른 슬롯으로 바뀌면 시간을 취소합니다. 다음 줄은 프레임 사이에 지난 시간을 누적하되 목표 시간보다 커지지 않게 합니다. 마지막은 시간이 찼을 때 딱 한 번 성공을 알립니다. 새 누름을 받은 프레임에는 시작 위치를 기록하며, 그 다음 유지 프레임부터 누적합니다. 그래서 표시상 1초를 기준으로 동작하되 프레임 단위의 작은 차이는 생깁니다.

### 격자와 장애물 판정

`PlacementSurface.Failed`는 다음 계산으로 위치를 고정합니다. 함수 이름은 실패 후보를 먼저 만드는 단계라는 뜻이며, 이후 검사를 통과하면 유효 후보가 됩니다.

```csharp
var cell = new Vector2Int(Mathf.RoundToInt(point.x / cellSize),
                         Mathf.RoundToInt(point.z / cellSize));
```

격자 크기가 1이면 X=2.2, Z=4.7인 바닥 점은 대략 X=2, Z=5 칸 중심으로 맞춰집니다. 격자의 원점은 월드 (0,0,0)이고 설치 회전은 `Quaternion.identity`입니다. 맵의 회전이나 사용자 회전 입력에 따라 격자를 돌리는 기능은 없습니다.

검사는 아래 순서로 겹쳐 적용됩니다.

1. **첫 충돌 확인:** 카메라 광선에서 플레이어 자신과 일반 Trigger를 제외하고 가까운 충돌을 찾습니다. 벽을 맞았으면 그 뒤의 바닥을 유효 위치로 통과시키지 않습니다.
2. **허용 바닥 확인:** `floors`에 등록한 실제 Collider만 바닥으로 인정합니다. Item/Enemy 레이어를 붙이는 것과는 다른 목록입니다.
3. **지지면 확인:** 중앙과 네 모서리 아래에 바닥이 있어야 합니다. 최대 경사 약 15도, 다섯 지점 높이 차이는 0.08 이하입니다. 가장자리에서 덫 절반만 허공에 놓이지 않게 합니다.
4. **거리·도달 경로:** 플레이어 위치에서 Y 방향 0.65 올린 점을 기준으로 거리와 가림을 확인합니다. 화면에 보여도 멀거나 벽 뒤면 안 됩니다.
5. **공간 확인:** `OverlapBoxNonAlloc`으로 덫 폭과 높이의 공간을 확인합니다. 벽·기둥·기름통·매대처럼 Collider가 있는 장애물은 설치를 막습니다.
6. **중복 덫 확인:** Trigger뿐 아니라 기존 덫 Renderer의 bounds도 검사합니다. 발동 후 Trigger가 꺼졌더라도 보이는 덫에 겹쳐 설치하지 않도록 합니다.

`NonAlloc`은 검사 결과를 매번 새 배열로 만들지 않고 준비한 배열에 담는다는 뜻입니다. 현재 배열 용량은 128입니다. 꽉 찬 경우 “어쩌면 안전할 것”이라고 진행하지 않고 설치 불가로 처리합니다. Collider가 없는 장애물, Ignore Raycast 레이어로 제외한 물체까지 자동으로 감지하는 기능은 아닙니다.

### 현재 씬 설정과 수정 위치

아래는 **코드 기본값이 아니라 저장된 PlaytestScene01의 TrapPlacement 값**입니다.

| 인스펙터 필드 | 저장값 | 바꾸면 달라지는 것 |
|---|---:|---|
| `floors` | Collider 참조 12개 | 설치를 허용하는 실제 바닥 |
| `cellSize` | 1 | 보이지 않는 격자 간격 |
| `reach` | 2.2 | 플레이어에서 설치 지점까지 허용 거리 |
| `holdSeconds` | 1 | 설치 완료에 필요한 유지 시간 |
| `footprintHalfSize` | X 0.45475003, Y 0.26500002 | 덫의 월드 X/Z 방향 반폭. 전체 폭은 두 배 |
| `modelGroundOffset` | (0.087250024, -0.008000001, 0) | 모델 원점과 실제 바닥 면적 중심의 차이 보정 |
| `clearanceHeight` | 1.4 | 덫 위에 확보할 빈 공간 높이 |
| `previewMaterial` | TrapPreview.mat | 임시 외형 표현용 원본 재질 |
| `progressFill`, `statusText` | 각각 Image, TMP_Text 연결 | 진행률과 설치 불가 사유 표시 |

모델을 교체할 때는 격자만 키우지 말고 `footprintHalfSize`와 `modelGroundOffset`을 함께 확인해야 합니다. 전자는 충돌 여유 공간이고 후자는 위치 맞춤 값입니다. `PlacementSurface`는 생성 시 설정을 보관합니다. 실행 중 인스펙터 값을 바꾼다고 이미 만들어진 검사기가 모두 즉시 갱신되는 구조는 아닙니다. 수정 후 재생을 다시 시작해 확인하는 편이 정확합니다.

### 미리보기와 실제 생성

`PlacementFeedback`은 원본 프리팹 전체를 생성하지 않습니다. Transform·MeshFilter·MeshRenderer를 복사해 외형만 표시합니다. Collider·덫 발동 스크립트가 없는 임시 오브젝트이므로 미리보기가 쥐를 잡지 않습니다. 임시 재질을 사용해 공유 재질의 색을 바꾸지 않으며 종료 때 해제합니다.

설치 가능은 반투명 초록, 불가능은 반투명 빨강입니다. 바닥 자체를 찾지 못하면 외형 대신 안내 문구만 남을 수 있습니다. 진행률은 유효한 바닥 후보에서 표시합니다. 현재 안내 문구의 “1초”는 문자열에 직접 쓰여 있으므로 `holdSeconds`를 바꾸면 안내도 함께 조정해야 합니다. 이것은 설정 확장 시 주의할 점이지 이번 문서 작업에서 수정한 내용은 아닙니다.

`CompletePlacement`는 시간이 찼다고 바로 재고를 지우지 않습니다. 카메라 위치, 같은 격자, 같은 슬롯, 같은 아이템을 다시 확인합니다. 복제본을 만든 뒤 `TryRemove`가 실패하면 방금 만든 복제본을 비활성화하고 제거합니다. 슬롯 변경 경쟁으로 물건만 잃거나 무료 덫이 남는 일을 줄입니다.

## 덫 발동과 라운드 수명

설치 프리팹은 기존 `ConsumableJawTrap`을 재사용합니다. `TrapPickup.HandleTriggered`는 전달받은 actor에 `ThiefController`가 있으면 `TakeStun(Mathf.Max(.01f, trap.removeDelay))`를 호출합니다. 덫을 밟았다고 이 연결부가 곧바로 쥐를 포획하거나 점수를 올리는 것은 아닙니다. 기절 뒤 F 포획은 기존 Enemy 경로가 처리합니다.

`PlacedTrapLifetime`은 이 `TrapPlacement`가 등록한 월드 복제본만 기억합니다. `RoundClear`, `RoundResult`, `Win`, `Lose` 및 다음 라운드 시작 시 남아 있는 설치물을 정리합니다. 시간 정지 상태에서도 표시와 충돌을 먼저 끄고 지연 없는 `Destroy`를 요청합니다. 시작부터 맵에 있던 덫, 인벤토리의 미사용 덫, Project 창의 프리팹 에셋을 지우는 목록이 아닙니다.

일반 게임 입력은 결과창 전 `RoundClear` 동안 일부 허용하지만 **새 덫 설치는 Playing에서만 허용**합니다. 설치물을 지운 뒤 결과 대기 중 다시 깔아서 남겨 버리는 일을 막습니다. GameManager 없는 독립 검사 환경에서는 설치를 허용하는 별도 경로가 있습니다.

## 3치즈 보상

### 정확한 조건

3치즈는 치즈 아이템 세 개를 인벤토리에 넣었다는 뜻이 아니라 **라운드 방어 성적**입니다. [RoundResultUI.GetCheeseCount](../Assets/02Scripts/04Systems/RoundResultUI.cs)는 모든 보물을 지켰으면 3, 현재 기본 기준으로 3개 이상 남겼으면 2, 그보다 적으면 1을 표시합니다. 현재 씬은 보물 총 5개입니다.

| 라운드 종료 결과 | 치즈 표시 | 덫 보상 |
|---|---:|---|
| 5개 중 5개 지킴 + 빈 인벤토리 있음 | 3 | 1개 |
| 5개 중 5개 지킴 + 5칸 모두 참 | 3 | 지급하지 않음. 나중에 재지급하지 않음 |
| 5개 중 3~4개 지킴 | 2 | 없음 |
| 5개 중 1~2개 지키고 클리어 | 1 | 없음 |
| 보물이 모두 사라져 패배 | 패배 경로 | 없음 |

보상 코드의 실제 조건은 `result.treasuresTotal > 0 && result.IsPerfect`이고, `IsPerfect`는 `treasuresLost == 0`입니다. 라운드 총 보물이 바뀌어도 “모두 지켰는가”가 기준입니다.

### 지급 시점과 중복 방지 코드

```csharp
if (round <= lastProcessedRound) return;
lastProcessedRound = round;
if (perfect && inventory && trapItem)
    inventory.TryAdd(trapItem);
```

출처: [TrapRoundReward.GrantOnce](../Assets/02Scripts/01Player/Interaction/Placement/TrapRoundReward.cs).

첫 줄은 같은 라운드 알림을 여러 번 받아도 중복 보상을 막습니다. 두 번째 줄은 이번 라운드를 처리했다고 표시합니다. 그 뒤 조건과 연결이 유효하면 가장 왼쪽 빈칸에 덫을 넣습니다. 처리 표시를 먼저 하기 때문에 가득 찬 경우, 연결이 없는 경우 모두 해당 라운드 보상을 나중에 자동 재시도하지 않습니다.

일반 라운드는 마지막 쥐 포획 → RoundClear → 현재 설정 1.5초 대기 → RoundResult/결과 알림 순서입니다. 보상은 그 결과 알림을 듣고 들어갑니다. 치즈 그림이 하나씩 나타나는 연출과는 별도로 진행합니다. 마지막 라운드는 GameManager가 곧바로 Win으로 가므로 `TrapRoundReward`가 Win 알림에서 결과 자료를 만들어 같은 조건으로 판단합니다.

### 보상이 안 보일 때 확인 순서

1. 결과의 `treasuresLost`가 정말 0인지 봅니다. 화면의 치즈 연출 완료와 데이터 지급 시점은 다릅니다.
2. HotbarManager의 5칸을 전부 확인합니다. 덫은 선택 칸이 아니라 **가장 왼쪽 빈칸**에 들어가며 선택은 자동으로 바꾸지 않습니다.
3. Player의 TrapRoundReward가 활성 상태이고 `inventory`, `trapItem` 참조가 실제 사용 중인 보관함·TrapItem인지 확인합니다.
4. 데이터는 추가됐는데 그림이 안 보이면 HotbarUI의 `icon`, `label`, `border`와 TrapItem.icon을 봅니다.
5. GameManager/HotbarManager를 실행 중 교체하는 기능을 추가했다면 이벤트 연결을 확인합니다. 보상 연결은 OnEnable/Start에서 확인하며 매 Update 재연결하는 구조는 아닙니다. HotbarUI도 Start에서 한 번 연결합니다.

2026-10-14의 기존 재현 검사 기록에서는 첫 칸 덫 1개로 시작해 쥐 3마리를 공개 API로 포획한 뒤, 보물 5/5와 RoundResult에서 두 번째 칸에 덫이 추가되고 두 아이콘 및 치즈 3개가 표시됐습니다. **실제 키보드 F 입력을 사용한 그 증상 재현 검사는 아니며, 사용자가 겪은 미지급 문제의 원인이 확정되거나 수정됐다는 뜻도 아닙니다.** 원본 검사 기록은 [근거 목록](08_REVIEW_AND_VALIDATION.md)에 연결합니다.

## 설계 검토와 확장 방법

SRP 관점에서 입력 해석, 유지 시간, 공간 검사, 표시, 재고 연결, 라운드 수명을 나눈 점이 좋습니다. `PlacementHold`와 `PlacementInput`은 Unity 타입 없이 일반 값만 받으므로 별도로 검사하거나 다른 엔진에 옮기기 쉽습니다. 반면 `TrapPlacement`는 HotbarManager와 ConsumableJawTrap 구체 구현에, `TrapRoundReward`는 GameManager 단일 인스턴스에 직접 의존합니다. 모든 엔진 의존성이 사라진 구조는 아닙니다.

여러 종류의 설치물을 추가할 때는 우선 새 아이템마다 외형 크기·회전·설치 조건이 다른지 확인합니다. 현재 `HasSelectedTrap`은 같은 `trapItem` 자료인지 직접 비교합니다. 단순히 `itemId`를 똑같이 쓰거나 레이어만 붙인다고 모든 아이템이 설치되지 않습니다. 종류가 실제로 늘어날 때 아이템별 설치 설정과 보상 규칙의 작은 계약을 분리하는 것이 확장 방향입니다. 사용하지 않는 거대한 범용 프레임워크를 미리 만들 필요는 없습니다.

## 수정 후 확인할 사례

| 검사 | 기대 결과 |
|---|---|
| 덫 선택, 빈 바닥, 좌클릭/F 각각 1초 | 재고 1개만 줄고 위치 예고와 같은 곳에 1개 설치 |
| 0.5초에 키 해제 또는 다른 격자로 이동 | 설치 0개, 재고 그대로 |
| 3초 이상 계속 누름 | 처음 1개만 설치 |
| F 획득 대상과 덫 설치가 겹침 | 획득 우선, 같은 누름으로 설치되지 않음 |
| 샷건 장착 후 덫 선택 | 총 유지, 설치와 사격 중복 없음 |
| 벽·선반·기름통·기둥·기존 덫·바닥 끝 | 설치 불가 표시와 재고 보존 |
| 설치 중 Esc 또는 포커스 상실 | 취소, 복귀 후 새 누름부터 시작 |
| RoundClear/Win/Lose | 설치한 복제본 제거, 인벤토리 재고 보존 |
| 완벽 방어·빈칸 있음/없음 | 한 개 지급/지급 안 함 |
| 결과 이벤트 중복 | 같은 라운드 보상 중복 없음 |

이 표는 앞으로 수정한 뒤 실행할 체크리스트입니다. 이번 문서 작성에서 게임을 새로 실행해 모두 통과했다는 기록이 아닙니다.
