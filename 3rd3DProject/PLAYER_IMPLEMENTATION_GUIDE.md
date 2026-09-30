# 플레이어 구현과 수정 설명서

이 문서는 현재 플레이어 기능을 직접 수정할 사람을 위한 안내입니다. 작업 전에는 `PLAYER_WORK_RULES.md`를 먼저 읽습니다. 작업별 검증·오류·삭제 기록은 `PLAYER_WORK_LOG.md`에 추가합니다.

## 조작 방법

| 입력 | 동작 |
| --- | --- |
| WASD | 걷기 |
| Shift + WASD | 달리기 |
| 이동 키를 누르지 않음 | 대기 |
| 마우스 이동 | 정지: 멈춘 방향에서 좌우60도 / 이동: 수평360도. 상하 범위는 기존과 같음 |
| 빈손에서 마우스 우클릭을 누르고 유지 | 중앙 커서가 가리키는 물건을 화면 중앙에 들기 |
| 우클릭에서 손을 뗌 | 즉시 보유 종료·내려놓기 |
| 숫자 1 | 기존 빨강·파랑 샷건을 즉시 양손으로 잡기 / 다시 누르면 해제 |
| Esc / 왼쪽 클릭 | 마우스 잠금 해제 / 다시 잠금 |

실린더는 우클릭을 새로 누를 때 집고 버튼을 누르는 동안만 듭니다. 떼면 내려놓습니다. 기존 F 보유 토글은 제거했습니다. 장애물이 없으면 물건의 중심이 중앙 포인터와 겹칩니다. 벽에 걸리거나 선반에서 빠져나오는 동안에는 충돌을 피하는 위치가 우선합니다. 바닥을 못 찾으면 빈 공간에서 중력으로 놓습니다. 새 장애물과 완전히 겹쳐 안전한 위치가 없다면 손은 즉시 비우고 물건은 그 자리에 잠시 멈춥니다. 공간이 생긴 뒤 충돌과 중력을 켜므로 플레이어나 벽을 강제로 밀지 않습니다. 큐브는 기존처럼 등록된 인벤토리 UI로 이동하는 획득 연출이며, UI가 없으면 그대로 남습니다.

물건을 든 상태에서 1을 누르면 안전하게 내려놓을 수 있을 때만 내려놓고 샷건을 잡습니다. 바닥이나 공간이 부족하면 기존 물건을 계속 듭니다. 총을 이미 들었을 때 1을 또 누르면 총을 숨기고 원래 손 동작으로 돌아갑니다. 총 장착 중에는 다른 물건 집기를 막습니다. 총 발사·소환 효과·소환 애니메이션은 연결하지 않았습니다. 외부 입력·버튼에서는 `CatInteractionController.TryToggleEquipment()`를 사용합니다. `CatShotgunEquipment`는 장비 표시만 맡으므로 그 안에 입력 검사나 아이템 내려놓기 규칙을 넣지 않습니다.

## 어디에 무엇이 있나요?

씬은 `Assets/01Scenes/PlayerTestScene.unity`만 사용합니다. 코드는 모두 `Assets/02Scripts/01Player` 아래에 있습니다. 폴더는 기능을 나누어 담는 서랍이라고 생각하면 됩니다.

| 폴더·파일 | 맡은 일 |
| --- | --- |
| `Core/CatPlayerMotor.cs` | 입력·이동·동작 재생을 연결하는 중심 |
| `Input/CatPlayerInput.cs` | WASD와 Shift를 이동 의도로 바꿈 |
| `Movement/` | 실제 이동과 작은 턱 넘기 |
| `Animation/CatPlayerAnimation.cs` | 대기·걷기·달리기 선택 |
| `Camera/` | Cinemachine 마우스 입력과 커서 잠금 |
| `Interaction/CatInteractionController.cs` | 중앙 조준 검사, 순수 손 규칙 결과를 들기·장착 기능에 연결 |
| `Interaction/CatItemCarrier.cs` | 선반에서 꺼내기, 들고 이동하기, 안전하게 내려놓기 |
| `Interaction/CatInteractionItem.cs` | 물건 종류와 교체 가능한 외형 지정 |
| `Interaction/Presentation/` | 중앙 커서, 노란 강조, 인벤토리로 날아가는 표시 |
| `Equipment/CatShotgunEquipment.cs` | 총 표시·숨김과 양손 위치 맞추기. 입력을 직접 읽지 않음 |
| `Editor/Setup/CatEquipmentSetup.cs` | 샷건 연결을 수동 설치·재연결. 이미 조절한 자세는 보존 |
| `Editor/Validation/CatInteractionValidation.cs` | 변경된 들기·1번 토글·UI·카메라·마찰을 Play에서 검사 |
| `Domain/` | Unity 없는 이동·손 사용·카메라 제한 규칙 |
| `Input/CatInteractionInput.cs` | 우클릭 유지 상태와 숫자1을 단순한 값으로 변환 |
| `Camera/CatCameraOrbitLimit.cs` | 순수 회전 범위 규칙을 Cinemachine 수평 축에 적용 |

샷건 에셋은 `Assets/03Sprites/Player/Weapons/Shotgun/`에 있습니다. `Shotgun01_RedBlue.fbx`가 이전 제작 원본의 복사본입니다. `Shotgun_Ready.prefab`은 외형과 양손 기준점을 묶은 재사용 묶음입니다. 빨강 총열·파랑 손잡이 등의 `.mat` 파일은 색상을 맡습니다.

## 어떻게 움직이나요?

1. 카메라 정중앙에서 눈에 보이지 않는 검사 선을 쏩니다. 가까운 물건인지, 벽 뒤에 있지는 않은지 확인합니다.
2. 빈손이고 우클릭을 새로 눌렀으면 기존 집기 기능을 호출합니다. `CatHandPolicy`가 집기·놓기·장착·해제 중 어떤 일을 할지 결정합니다. 입력 장치 자체는 `CatInteractionInput`만 읽습니다.
3. 숫자 1은 장착 담당 코드로 들어갑니다. 연결이 올바른지 먼저 확인하고, 들고 있던 물건을 안전하게 내려놓습니다. 실패하면 장착하지 않습니다.
4. 총은 미리 연결한 하나의 모델을 켜서 표시합니다. 매번 새 총을 만들지 않습니다. 예전 소환 동작의 마지막 잡는 자세를 참고했지만 소환 애니메이션 자체는 가져오지 않았습니다.
5. Animator가 몸·꼬리·발의 기존 동작을 계산한 뒤, `LateUpdate`에서 양손 뼈만 `LeftGrip`·`RightGrip` 위치에 맞춥니다. Grip은 손이 놓일 기준점입니다. 다음 프레임에는 이전 손 보정을 풀고 다시 계산하여 위치가 계속 틀어지지 않게 합니다.

## 직접 바꾸는 방법

### 샷건 위치 또는 손 위치

1. Play를 끕니다. Hierarchy에서 `Cat_Player`를 선택합니다.
2. `Cat Shotgun Equipment`의 `Weapon Local Position`으로 총의 위치, `Weapon Local Euler Angles`로 기울기를 바꿉니다. 이 값은 몸통 뼈를 기준으로 합니다. Unity 좌표의 Y가 위·아래입니다.
3. 손만 옮기려면 `Cat_Player > Cat_Shotgun > LeftGrip / RightGrip`의 위치를 조절합니다. 왼손은 앞쪽 파란 손잡이, 오른손은 뒤쪽 파란 손잡이에 놓습니다.
4. Play에서 숫자 1을 누르고 대기·걷기·달리기, 정면·측면을 모두 봅니다. Play 중 수정한 값은 Play 종료 시 돌아가므로 편집 모드에서 적용합니다.

### 샷건 외형 또는 색

- `Shotgun_Ready.prefab`의 `Visual` 외형을 교체하되 `LeftGrip`·`RightGrip` 기준점과 컴포넌트 연결은 유지합니다. 총 모델의 시작 위치는 뒤 손잡이 피벗에 맞춥니다. 모델이 바뀌면 양손 기준점도 다시 조절합니다.
- `Barrel_Red.mat`는 총열, `Grip_Blue.mat`는 파란 손잡이, `Receiver_DeepBlue.mat`는 가운데 몸체, `Muzzle_Dark.mat`는 총구 색입니다. URP Unlit 재질로 캐릭터처럼 평평한 색을 표시합니다.
- 연결을 잃었으면 `Tools > Cat Player > 샷건 연결 확인 및 설치`를 사용합니다. 허용된 씬 편집 모드에서만 실행하며, 이미 연결된 총의 자세는 덮어쓰지 않습니다.

### 집기 거리와 모델 교체

- `Cat_Player > Cat Interaction Controller > Reach`가 플레이어 기준 집기 거리입니다.
- 물건의 `Visual` 아래 외형만 바꾸고 루트의 `CatInteractionItem`, `BoxCollider`, `Rigidbody`는 유지합니다. 컴포넌트 메뉴의 `Visual 크기에 맞춰 충돌 갱신`을 실행합니다.
- 물건이 아닌 일반 모델을 노랗게 강조하려면 `CatHighlightTarget`과 기존 비트리거 Collider를 사용합니다. 강조만 있는 모델은 자동으로 집을 수 있는 물건이 되지 않습니다.

### 인벤토리 담당자 연결

`CatInventoryPickupPresenter > Inventory Target`에 실제 UI의 RectTransform을 등록합니다. 코드에서는 `RegisterTarget(...)`을 사용할 수 있습니다. 도착 시 `Collected` 이벤트로 물건 식별자가 한 번 전달됩니다. 실제 인벤토리 데이터 저장은 별도 담당 코드에서 처리합니다. 등록된 UI가 없거나 도중에 사라지면 물건은 사라지지 않습니다.

## 수정 뒤 확인하기

`PlayerTestScene`에서 Play한 뒤 `Tools > Cat Player`의 해당 검증 메뉴를 실행합니다. 자동 검증은 잠시 입력과 카메라를 제어하므로 실행 중 직접 조작하지 않습니다. 종료 시 Play가 끝나며 검사 결과가 `Logs`에 남습니다. 마지막 `RESULT`와 실패 항목을 확인합니다. 확인되지 않은 결과를 성공으로 기록하지 않습니다.

손으로도 빈손 우클릭 유지·해제, 물건을 든 채 1, 다시1로 해제, 총을 든 채 이동, 정지 좌우60도·이동360도를 확인합니다. 다른 씬·다른 담당자의 스크립트·기존 애니메이션 에셋은 이 수정에 포함하지 않습니다.


## 다른 Unity 버전이나 엔진으로 옮기는 방법

규칙과 실행 도구를 나눴습니다. 예를 들어 “버튼을 떼면 물건을 놓는다”는 게임 규칙이고, 실제 물체를 중력으로 떨어뜨리는 것은 엔진의 역할입니다. 콘센트가 바뀔 때 기기 전체 대신 어댑터를 바꾸는 것과 비슷합니다.

| 부분 | 지금 역할 | 엔진을 바꾸면 |
| --- | --- | --- |
| `CatMovementPolicy`, `CatHandPolicy`, `CatOrbitPolicy` | 이동 상태·손 사용·카메라 각도 판단. 일반 C# 값만 사용 | C# 지원 엔진에서는 규칙 파일 재사용, 다른 언어라면 같은 규칙 번역 |
| `ICatMotionState`, `ICatEquipmentPort` | 다른 기능에 필요한 최소 약속 | 새 연결부가 같은 약속을 지키도록 구현 |
| Input·Motor·Locomotion·Animation | 실제 키 입력, Rigidbody, Animator 연결 | 해당 엔진의 입력·몸체·애니메이션 API로 교체 |
| CatStepSolver·CatItemCarrier | 충돌·바닥·벽 검사와 물체 이동 | 새 엔진의 레이/형상 검사·물리 API로 교체 |
| 카메라 연결부 | 기존 Cinemachine에 순수 각도 제한을 전달 | 새 엔진 카메라에 같은 각도 규칙 연결 |
| 강조·아이콘 촬영·중앙 포인터·UI 연출 | Unity 렌더·Canvas·셰이더 표현 | 해당 엔진의 화면 표시 기능으로 교체 |

이동·손 사용·회전 정책은 Unity DLL 없이 독립 컴파일해 검사합니다. 물리·UI·렌더 코드 전체가 엔진 독립인 것은 아닙니다. 기존 `ICatPlayerInputSource`, `ICatStepSolver`, `ICatHighlightSource`는 Unity 타입을 포함한 연결용 인터페이스입니다. 복잡한 범용 물리·UI 프레임워크를 추가하지 않고 교체할 부분을 분명하게 유지합니다. 실제 다른 엔진에 옮겨 실행한 상태는 아닙니다.

### 값을 바꿀 위치

- 마찰: `Assets/03Sprites/Player/Physics/Player_NoFriction.physicMaterial`. Static/Dynamic Friction=0, Combine=Multiply. 플레이어 충돌체에 연결합니다. 프로젝트 전체 물리 설정은 바꾸지 않았습니다. 이동 키를 뗐을 때 멈추는 감속은 마찰과 다른 이동 제어이므로 유지합니다.
- 보유 거리: `Cat_Player > Cat Interaction Controller > Hold Distance`. 물건 중심은 카메라 중앙 조준선에 두고 이 거리만 조절합니다.
- 정지 회전 폭: `Cat_CinemachineCamera > Cat Camera Orbit Limit > Stationary Half Angle`. 60이면 왼쪽60도+오른쪽60도입니다. 멈출 때마다 기준 방향을 새로 잡습니다.
- 손 사용 규칙: `Domain/CatHandPolicy.cs`. 장비 종류를 바꾸려면 입력 코드를 복제하는 대신 `ICatEquipmentPort` 구현을 연결합니다.
- 이동 속도: 기존 Motor의 Walk Speed/Run Speed. 어떤 상황에 어떤 속도를 선택하는지는 `Domain/CatMovementPolicy.cs`입니다.
- 독립 규칙 검사: `Tools > Cat Player > 순수 규칙 검증`. 결과와 실제 Play 검사를 둘 다 확인합니다.
- 해제 후 물리 복구 대기: `CatInteractionItem.IsReleasePending`으로 읽습니다. 이때는 보유 중이 아니며 새 집기도 막습니다. 공간 검사·물리 복구는 해당 물건이 맡습니다.

### 구현 전에 확인한 공식 자료

[물리 재질](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PhysicsMaterial.html), [마찰 결합 순서](https://docs.unity3d.com/6000.0/Documentation/Manual/collider-surfaces-combine.html), [버튼 유지 입력](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.Controls.ButtonControl.html), [중앙 조준선](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.ViewportPointToRay.html), [Cinemachine 회전 범위](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineOrbitalFollow.html)를 확인했습니다. 다른 엔진의 구현 가능성까지 검증한 것은 아닙니다.

## 현재 검증 결과 (2026-09-30)

순수 규칙27개, 실제 상호작용38개, 기존 이동19개를 통과했습니다. 물건 중심과 포인터의 측정 오차는0.135픽셀이었고, 정지좌우60도와 이동360도도 실제 입력으로 확인했습니다. 자세한 결과는 `PLAYER_WORK_LOG.md`와 `Logs/CatInteractionValidation_20260930_174604_982.txt`, `Logs/CatPlayerValidation.txt`에 있습니다.

자동 검증에서 만드는 UI·가림용물체·강조용물체는 검사 종료 시 직접 제거합니다. 실제 사용자 인벤토리 UI는 추가하지 않았습니다. 씬의 Inventory Target에 담당자가 만든 UI를 연결해야 큐브 획득 연출이 실행됩니다.
