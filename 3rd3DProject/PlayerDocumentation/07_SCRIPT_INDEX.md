# 스크립트와 구현 위치 색인

기준 커밋 `6b8bafde`, 2026-10-14. Player C# 53개와 직접 연결된 Systems C# 10개를 모두 포함합니다. 파일 이름을 누르면 프로젝트의 실제 소스로 이동합니다. 클래스 이름은 한 파일에 여러 개가 있을 수 있습니다. 행 번호는 문서 작성 시점이며 편집 후 바뀔 수 있습니다.

표의 **씬 직접참조**는 저장된 PlaytestScene01 YAML에 해당 스크립트가 직접 나타난 수입니다. 프리팹 상속·일반 C# 생성·수동 메뉴는 이 숫자에 완전히 반영되지 않습니다. 사용 여부나 삭제 가능성을 이 숫자로 판정하지 않습니다.

| 파일 | 책임 | 타입과 선언 행 | 씬 직접참조 | 상세 설명 |
|---|---|---|---:|---|
| [01Player/Animation/PlayerAnimation.cs](../Assets/02Scripts/01Player/Animation/PlayerAnimation.cs) | 이동·점프 상태를 Animator 매개변수에 전달하고 구형 컨트롤러 호환 처리 | `PlayerAnimation` 5행, `Settings` 8행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Animation/ShotgunPose.cs](../Assets/02Scripts/01Player/Animation/ShotgunPose.cs) | 발사 클립 시각과 조준·반동 자세를 계산 | `ShotgunPose` 6행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Camera/AimZoom.cs](../Assets/02Scripts/01Player/Camera/AimZoom.cs) | 조준 상태에 따라 Cinemachine 시야각을 보간 | `AimZoom` 9행 | 1 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Camera/CameraCursorLock.cs](../Assets/02Scripts/01Player/Camera/CameraCursorLock.cs) | 메뉴·게임 상태에 맞춰 커서 잠금과 표시를 한 곳에서 관리 | `CameraCursorLock` 9행 | 1 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Camera/CameraOrbitLimit.cs](../Assets/02Scripts/01Player/Camera/CameraOrbitLimit.cs) | 이동 의도에 따라 수평 카메라 회전 허용 범위를 제한 | `CameraOrbitLimit` 9행 | 1 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Core/PlayerController.cs](../Assets/02Scripts/01Player/Core/PlayerController.cs) | 입력·물리·점프·애니메이션 담당자를 조립하고 Unity 실행 순서 연결 | `PlayerController` 6행 | 1 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Domain/CombatPolicy.cs](../Assets/02Scripts/01Player/Domain/CombatPolicy.cs) | 조준 중 걷기와 사격 입력·시간 규칙 등 장비 정책 | `IAimState` 2행, `IShotSpreadState` 9행, `CombatPolicy` 16행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Domain/HandPolicy.cs](../Assets/02Scripts/01Player/Domain/HandPolicy.cs) | 집기·놓기·장착 행동 판단 | `HandInput` 2행, `HandState` 9행, `HandCommand` 16행, `HandPolicy` 18행, `IEquipmentPort` 33행, `RegisteredCommand` 41행, `RegisteredInteractionPolicy` 44행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Domain/IMotionState.cs](../Assets/02Scripts/01Player/Domain/IMotionState.cs) | 카메라가 필요한 이동 의도 조회 계약 | `IMotionState` 2행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Domain/InputPermission.cs](../Assets/02Scripts/01Player/Domain/InputPermission.cs) | 메뉴별 차단 요청을 합산하는 일반 C# 규칙 | `InputPermission` 4행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Domain/IPrimaryAction.cs](../Assets/02Scripts/01Player/Domain/IPrimaryAction.cs) | 좌클릭·F 점유와 대상 존재 조회의 작은 계약 모음 | `IPrimaryAction` 2행, `IInteractionAction` 8행, `IInteractionTargetQuery` 14행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Domain/JumpPolicy.cs](../Assets/02Scripts/01Player/Domain/JumpPolicy.cs) | 점프 단계·자세 시간·입력 보관 판단 | `JumpPhase` 4행, `JumpFrame` 13행, `JumpPolicy` 32행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Domain/MovementPolicy.cs](../Assets/02Scripts/01Player/Domain/MovementPolicy.cs) | 이동 입력 의도와 걷기·달리기 규칙 | `MovementIntent` 2행, `MovementSettings` 19행, `MovementState` 36행, `MovementPolicy` 44행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Domain/OrbitPolicy.cs](../Assets/02Scripts/01Player/Domain/OrbitPolicy.cs) | 정지 기준 방향과 허용 회전 범위 계산 | `OrbitPolicy` 4행, `Constraint` 10행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Domain/PlacementHold.cs](../Assets/02Scripts/01Player/Domain/PlacementHold.cs) | 같은 칸·슬롯의 유지 시간과 해제 대기 판단 | `PlacementHold` 4행 | 0 | [F10](04_INVENTORY_TRAPS.md) |
| [01Player/Domain/PlacementInput.cs](../Assets/02Scripts/01Player/Domain/PlacementInput.cs) | F와 좌클릭의 설치 입력 소유·취소 정책 | `PlacementInputFrame` 2행, `PlacementInput` 13행, `Source` 15행 | 0 | [F10](04_INVENTORY_TRAPS.md) |
| [01Player/Editor/Setup/CombatSetup.cs](../Assets/02Scripts/01Player/Editor/Setup/CombatSetup.cs) | 기존 무기·자세·조준·총구·산탄 표시 연결 도구 | `CombatSetup` 9행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Setup/EquipmentSetup.cs](../Assets/02Scripts/01Player/Editor/Setup/EquipmentSetup.cs) | 기존 샷건과 양손 그립 연결 도구 | `EquipmentSetup` 8행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Setup/InteractionSettings.cs](../Assets/02Scripts/01Player/Editor/Setup/InteractionSettings.cs) | 기존 무마찰·카메라 제한 설정 연결 도구 | `InteractionSettings` 9행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Setup/InteractionSetup.cs](../Assets/02Scripts/01Player/Editor/Setup/InteractionSetup.cs) | 상호작용·샘플 물체·표시 기능 연결 도구 | `InteractionSetup` 8행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Validation/CombatInputValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/CombatInputValidation.cs) | 장착·발사·조준 입력 회귀 검사 | `CombatInputValidation` 13행, `Pause` 41행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Validation/CombatValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/CombatValidation.cs) | 샷건 자세·애니메이션·설정 검사 | `CombatValidation` 17행, `Pause` 56행, `ScriptedCombatInput` 64행, `MotionInput` 79행, `NeutralHandInput` 86행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Validation/HitscanValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/HitscanValidation.cs) | 명중·산탄·총구 가림 등 검사 | `HitscanValidation` 8행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Validation/InteractionValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/InteractionValidation.cs) | 집기·놓기·UI 연출·메뉴 상호작용 검사 | `InteractionValidation` 15행, `Pause` 55행, `MotionInput` 66행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Validation/PlayerValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/PlayerValidation.cs) | 플레이어 연결·이동 검사와 공통 에디터 범위 제어 | `PlayerValidation` 13행, `PlayerEditorScope` 392행, `HotbarSelection` 447행, `HotbarSelectionScope` 470행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Validation/PlayerValidationCases.cs](../Assets/02Scripts/01Player/Editor/Validation/PlayerValidationCases.cs) | 이동·턱·카메라 등 검사 시나리오 | `PlayerValidationKind` 4행, `PlayerValidationCase` 6행, `PlayerValidationSample` 22행, `PlayerValidationCases` 55행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Editor/Validation/PolicyValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/PolicyValidation.cs) | Unity와 분리한 판단 규칙 검사 | `PolicyValidation` 5행 | 0 | [F11](05_EDITOR_ASSETS.md) |
| [01Player/Equipment/HitscanQuery.cs](../Assets/02Scripts/01Player/Equipment/HitscanQuery.cs) | 카메라 목표·총구 차단·실제 명중 물리 질의 | `ShotHitKind` 3행, `ShotHit` 6행, `HitscanQuery` 31행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Equipment/Presentation/ShotSpreadRing.cs](../Assets/02Scripts/01Player/Equipment/Presentation/ShotSpreadRing.cs) | 산탄 범위의 화면 원 표시 | `ShotSpreadRing` 8행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Equipment/ShotgunCombat.cs](../Assets/02Scripts/01Player/Equipment/ShotgunCombat.cs) | 장착 상태·발사 입력·자세·히트스캔 연결 | `ShotgunCombat` 8행, `IShotReceiver` 138행 | 1 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Equipment/ShotgunEquipment.cs](../Assets/02Scripts/01Player/Equipment/ShotgunEquipment.cs) | 장착/해제와 총·양손 그립 위치 보정 | `ShotgunEquipment` 7행, `HandPose` 45행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Input/CombatInput.cs](../Assets/02Scripts/01Player/Input/CombatInput.cs) | 실제 장치에서 사격·조준 입력 읽기 | `CombatInputFrame` 5행, `ICombatInputSource` 13행, `IResettableInputSource` 19행, `InputActivationGate` 25행, `CombatInput` 52행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Input/GameplayInput.cs](../Assets/02Scripts/01Player/Input/GameplayInput.cs) | 게임 상태·메뉴·포커스·커서의 공통 입력 권한 진입점 | `GameplayInput` 5행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Input/InteractionInput.cs](../Assets/02Scripts/01Player/Input/InteractionInput.cs) | 운반·장비 관련 입력 프레임과 확장 계약 | `IInteractionInputSource` 4행, `InteractionInput` 10행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Input/PlayerInput.cs](../Assets/02Scripts/01Player/Input/PlayerInput.cs) | 이동·점프 장치 입력과 활성화 게이트 | `PlayerInputFrame` 5행, `IPlayerInputSource` 25행, `KeyboardInput` 31행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Interaction/CatInteractionItem.cs](../Assets/02Scripts/01Player/Interaction/CatInteractionItem.cs) | 운반 물건의 식별·외형·물리 상태와 호환 상태 관리 | `ItemKind` 4행, `CatInteractionItem` 12행, `ReleaseRecovery` 26행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/InteractionController.cs](../Assets/02Scripts/01Player/Interaction/InteractionController.cs) | 운반·등록 상호작용·획득 연출·강조의 연결 | `IRegisteredInteraction` 4행, `InteractionController` 14행 | 1 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/ItemCarrier.cs](../Assets/02Scripts/01Player/Interaction/ItemCarrier.cs) | 물건 운반 위치·충돌·해제와 월드 예약 상태 처리 | `ItemCarrier` 5행, `PickupStage` 26행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/Placement/PlacementFeedback.cs](../Assets/02Scripts/01Player/Interaction/Placement/PlacementFeedback.cs) | 실제 기능 없는 외형 미리보기와 진행률 표시 | `PlacementFeedback` 10행 | 0 | [F10](04_INVENTORY_TRAPS.md) |
| [01Player/Interaction/Placement/PlacementSurface.cs](../Assets/02Scripts/01Player/Interaction/Placement/PlacementSurface.cs) | 등록 바닥·격자·지지면·장애물·기존 덫 검사 | `PlacementFailure` 5행, `PlacementCandidate` 7행, `PlacementSurface` 18행 | 0 | [F10](04_INVENTORY_TRAPS.md) |
| [01Player/Interaction/Placement/TrapPickup.cs](../Assets/02Scripts/01Player/Interaction/Placement/TrapPickup.cs) | 사용 전 덫 회수와 발동 시 기존 Enemy 기절 API 연결 | `TrapPickup` 6행 | 0 | [F10](04_INVENTORY_TRAPS.md) |
| [01Player/Interaction/Placement/TrapPlacement.cs](../Assets/02Scripts/01Player/Interaction/Placement/TrapPlacement.cs) | 설치 규칙을 장치·카메라·재고·실물 생성에 연결하고 설치물 수명 관리 | `TrapPlacement` 8행, `PlacedTrapLifetime` 150행 | 1 | [F10](04_INVENTORY_TRAPS.md) |
| [01Player/Interaction/Placement/TrapRoundReward.cs](../Assets/02Scripts/01Player/Interaction/Placement/TrapRoundReward.cs) | 라운드 완벽 방어 결과를 덫 1회 지급에 연결 | `TrapRoundReward` 5행 | 1 | [F10](04_INVENTORY_TRAPS.md) |
| [01Player/Interaction/Presentation/CenterCursor.cs](../Assets/02Scripts/01Player/Interaction/Presentation/CenterCursor.cs) | 화면 중앙의 작은 반투명 조준 구체 표시 | `CenterCursor` 6행 | 1 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/Presentation/HighlightTarget.cs](../Assets/02Scripts/01Player/Interaction/Presentation/HighlightTarget.cs) | 다른 물체에도 제공할 수 있는 강조 대상 컴포넌트 | `HighlightTarget` 8행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/Presentation/IHighlightSource.cs](../Assets/02Scripts/01Player/Interaction/Presentation/IHighlightSource.cs) | 강조에 필요한 렌더러와 가용성 조회 계약 | `IHighlightSource` 4행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/Presentation/InventoryPickupEffect.cs](../Assets/02Scripts/01Player/Interaction/Presentation/InventoryPickupEffect.cs) | 등록된 UI 목적지까지 획득 외형을 이동시키는 연출 | `IPickupPort` 5행, `InventoryPickupEffect` 13행, `PickupReservation` 228행, `RendererState` 238행, `ColliderState` 244행, `BodyState` 250행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/Presentation/ItemHighlight.cs](../Assets/02Scripts/01Player/Interaction/Presentation/ItemHighlight.cs) | 현재 대상의 옅은 면·노란 외곽선 렌더 | `ItemHighlight` 6행, `Part` 13행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Interaction/Presentation/ItemIconCapture.cs](../Assets/02Scripts/01Player/Interaction/Presentation/ItemIconCapture.cs) | 아이템 외형을 일회 촬영해 획득 연출용 이미지 생성 | `ItemIconCapture` 6행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [01Player/Movement/PlayerJump.cs](../Assets/02Scripts/01Player/Movement/PlayerJump.cs) | 접지·중력과 점프 정책을 연결 | `PlayerJump` 5행, `Settings` 8행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Movement/PlayerMovement.cs](../Assets/02Scripts/01Player/Movement/PlayerMovement.cs) | 이동 의도를 Rigidbody 속도·회전에 적용 | `PlayerMovement` 4행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Movement/StepSettings.cs](../Assets/02Scripts/01Player/Movement/StepSettings.cs) | 작은 턱 넘기 설정 자료 | `StepSettings` 5행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [01Player/Movement/StepSolver.cs](../Assets/02Scripts/01Player/Movement/StepSolver.cs) | 턱·천장·지지면을 검사해 넘을 위치 계산 | `IStepSolver` 3행, `StepSolver` 11행 | 0 | [F01~F04](02_MOTION_CAMERA.md) |
| [04Systems/GameManager.cs](../Assets/02Scripts/04Systems/GameManager.cs) | 기존 팀 시스템의 게임 상태·라운드·승패·결과 이벤트 | `RoundResult` 7행, `GameManager` 26행, `GameState` 31행 | 1 | [F09/F10 연계](04_INVENTORY_TRAPS.md) |
| [04Systems/GameSettings.cs](../Assets/02Scripts/04Systems/GameSettings.cs) | 기존 일반 설정 저장값·범위·음량 변환 | `GameSettings` 10행 | 0 | [F01/F03/F09 연계](01_ARCHITECTURE.md) |
| [04Systems/GameSettingsApplier.cs](../Assets/02Scripts/04Systems/GameSettingsApplier.cs) | 설정 UI 요청을 Unity 소리·해상도 API에 연결 | `IGameSettingsApplier` 6행, `GameResolution` 16행, `GameSettingsApplier` 32행 | 0 | [F01/F03/F09 연계](01_ARCHITECTURE.md) |
| [04Systems/HotbarItemData.cs](../Assets/02Scripts/04Systems/HotbarItemData.cs) | 인벤토리 한 종류의 이름·아이콘·설치 프리팹 자료 | `HotbarItemData` 9행 | 0 | [F09](04_INVENTORY_TRAPS.md) |
| [04Systems/HotbarManager.cs](../Assets/02Scripts/04Systems/HotbarManager.cs) | 5칸 재고·선택·휠·변경 이벤트 | `HotbarManager` 19행 | 1 | [F09](04_INVENTORY_TRAPS.md) |
| [04Systems/HotbarUI.cs](../Assets/02Scripts/04Systems/HotbarUI.cs) | 보관함 이벤트에 따른 아이콘·이름·테두리 갱신 | `HotbarUI` 20행, `SlotView` 23행 | 1 | [F09](04_INVENTORY_TRAPS.md) |
| [04Systems/ItemInteractor.cs](../Assets/02Scripts/04Systems/ItemInteractor.cs) | F 대상 발견·등록·인벤토리 저장·기존 Enemy 호출 | `IItemInventory` 9행, `RegisteredTargetKind` 15행, `InteractionRegistration` 19행, `ItemInteractor` 53행, `HotbarStore` 73행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [04Systems/ItemPickup.cs](../Assets/02Scripts/04Systems/ItemPickup.cs) | 월드 아이템 자료와 획득 규칙·소비 정책 조회 | `ItemPickup` 4행, `IItemPickupRule` 15행, `IItemPickupConsumption` 18행 | 0 | [F05~F08](03_INTERACTION_COMBAT.md) |
| [04Systems/PauseMenuUI.cs](../Assets/02Scripts/04Systems/PauseMenuUI.cs) | 설정창 열고 닫기·입력 차단 요청·슬라이더 연결 | `PauseMenuUI` 28행 | 1 | [F01/F03/F09 연계](01_ARCHITECTURE.md) |
| [04Systems/RoundResultUI.cs](../Assets/02Scripts/04Systems/RoundResultUI.cs) | 기존 결과창·치즈 등급·다음 라운드 UI와 차단 요청 | `RoundResultUI` 19행 | 1 | [F01/F03/F09 연계](01_ARCHITECTURE.md) |

## 스크립트 외 연결 자료

| 자료 | 확인할 것 |
|---|---|
| [PlayerJump.controller](../Assets/04Prefabs/Player/Animations/PlayerJump.controller) | Base Layer와 Shotgun 레이어의 조건·시간·Write Defaults |
| [Jump.anim](../Assets/04Prefabs/Player/Animations/Jump.anim) | 실제 클립 길이1.2초와 본 곡선. 정책2초 기준과 구분 |
| [Shotgun_Fire.anim](../Assets/03Sprites/Player/Animations/Shotgun_Fire.anim) | ShotgunPose의 숫자3종류 바인딩 |
| [CatCameraLook.inputactions](../Assets/02Scripts/01Player/Camera/CatCameraLook.inputactions) | Cinemachine 마우스 시점 입력 |
| [CatCursorSphere.shader](../Assets/02Scripts/01Player/Interaction/Presentation/CatCursorSphere.shader) | 중앙 구체 표시 |
| [CatInteractionOverlay.shader](../Assets/02Scripts/01Player/Interaction/Presentation/CatInteractionOverlay.shader) | 노란 면·외곽선 표시 |
| [CatShotSpreadRing.shader](../Assets/02Scripts/01Player/Equipment/Presentation/CatShotSpreadRing.shader) | 산탄 범위 원 표시 |
| [TrapItem.asset](../Assets/04Prefabs/Player/Inventory/TrapItem.asset) | 인벤토리 덫 데이터 |
| [Trap_Inventory.prefab](../Assets/04Prefabs/Player/Inventory/Trap_Inventory.prefab) | 원본 덫 변형과 ItemPickup/TrapPickup |
| [PlaytestScene01.unity](../Assets/01Scenes/PlaytestScene01.unity) | 실제 컴포넌트·프리팹 오버라이드 |

## 근거 파일

[소스 스냅샷](evidence/source_snapshot.json)은 각 파일의 SHA-256, 줄 수, 타입과 직접 참조를 저장합니다. [기준 상태](evidence/baseline_context.json)는 버전·패키지·설치 씬 값과 주요 연결의 출처를 남깁니다. 실행 중 생성되는 대상은 이 정적 자료만으로 모두 확인할 수 없습니다.
