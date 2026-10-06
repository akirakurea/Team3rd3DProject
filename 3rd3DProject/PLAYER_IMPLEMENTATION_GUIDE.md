# 플레이어 구현과 수정 설명서

이 문서는 현재 플레이어 기능을 직접 수정할 사람을 위한 안내입니다. 작업 전에는 `PLAYER_WORK_RULES.md`를 먼저 읽습니다. 작업별 검증·오류·삭제 기록은 `PLAYER_WORK_LOG.md`에 추가합니다.

## 조작 방법

| 입력 | 동작 |
| --- | --- |
| WASD | 걷기 |
| Shift + WASD | 달리기. 샷건 줌 중에는 걷기 |
| 이동 키를 누르지 않음 | 대기 |
| 마우스 이동 | 정지: 멈춘 방향에서 좌우60도 / 이동: 수평360도. 상하 범위는 기존과 같음 |
| 빈손에서 마우스 우클릭을 누르고 유지 | 중앙 커서가 가리키는 물건을 화면 중앙에 들기 |
| 우클릭에서 손을 뗌 | 즉시 보유 종료·내려놓기 |
| 숫자 1 | 기존 빨강·파랑 샷건을 즉시 양손으로 잡기 / 다시 누르면 해제 |
| 샷건 장착 중 우클릭 유지 | 줌·조준, 이동은 걷기로 고정. 떼면 기본 시야로 복귀 |
| 샷건 장착 중 좌클릭 | 한 번 누를 때 4발 산탄 발사. 누르고 있어도 연속 발사하지 않음 |
| Esc / 왼쪽 클릭 | 마우스 잠금 해제 / 다시 잠금 |

실린더는 우클릭을 새로 누를 때 집고 버튼을 누르는 동안만 듭니다. 떼면 내려놓습니다. 기존 F 보유 토글은 제거했습니다. 장애물이 없으면 물건의 중심이 중앙 포인터와 겹칩니다. 벽에 걸리거나 선반에서 빠져나오는 동안에는 충돌을 피하는 위치가 우선합니다. 바닥을 못 찾으면 빈 공간에서 중력으로 놓습니다. 새 장애물과 완전히 겹쳐 안전한 위치가 없다면 손은 즉시 비우고 물건은 그 자리에 잠시 멈춥니다. 공간이 생긴 뒤 충돌과 중력을 켜므로 플레이어나 벽을 강제로 밀지 않습니다. 큐브는 기존처럼 등록된 인벤토리 UI로 이동하는 획득 연출이며, UI가 없으면 그대로 남습니다.

물건을 든 상태에서 1을 누르면 안전하게 내려놓을 수 있을 때만 내려놓고 샷건을 잡습니다. 바닥이나 공간이 부족하면 기존 물건을 계속 듭니다. 총을 이미 들었을 때 1을 또 누르면 총을 숨기고 원래 손 동작으로 돌아갑니다. 총 장착 중에는 다른 물건 집기를 막습니다. 소환 효과·소환 애니메이션은 사용하지 않습니다. 발사는 아래 조준·발사 기능에서 처리합니다. 외부 입력·버튼에서는 `CatInteractionController.TryToggleEquipment()`를 사용합니다. `CatShotgunEquipment`는 장비와 손 자세만 맡으므로 그 안에 입력 검사나 아이템 내려놓기 규칙을 넣지 않습니다.

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

1. `PlayerTestScene > Cat_Player > Cat Shotgun Combat`: `Pellet Count`=4, `Spread Degrees`=4°, `Aimed Spread Degrees`=1.5°, `Shot Interval`=0.55초, `Range`=75. 퍼짐 값은 원의 중심부터 가장자리까지의 각도입니다. 줌 퍼짐을 비줌보다 크게 설정해도 비줌 값을 넘지 않습니다.
2. `Cat_CinemachineCamera > Cat Aim Zoom`: `Aimed Field Of View`=40, `Transition Seconds`=0.16. 시야각을 작게 하면 더 확대됩니다. 기본 시야각60은 줌을 놓으면 복귀합니다.
3. `Cat_Player > Cat Shotgun Pose`: `Aimed Local Position`은 몸통 본 기준 조준 위치입니다. `Raise Seconds`는 우클릭으로 총을 드는 시간, `Fire Moment`는 발사 클립에서 실제 판정하는 시각입니다.
4. `Assets/03Sprites/Player/Animations/Shotgun_Fire.anim`: 준비와 반동 곡선입니다. `raiseWeight`는 총을 드는 양, `recoilDistance`는 뒤로 밀리는 양, `recoilPitch`는 위로 들리는 각도입니다. 발사 시각은 총을 다 드는 순간과 맞춥니다. 기존 Idle/Walk/Run 클립은 수정하지 않았습니다.
5. `Cat_Player > Cat Shot Spread Ring`: `Line Width Pixels`와 `Outline Width Pixels`로 원의 선과 얇은 검은 테두리를 조절합니다. 기존 중앙 점의 위치·크기는 유지합니다.
6. 총 모델을 교체하면 `Cat_Shotgun > Muzzle`을 실제 총열 끝에 맞추고 파란 Z축이 발사 방향을 가리키게 합니다. `LeftGrip`과 `RightGrip`은 양손 위치입니다. `ShotGuardOrigin`은 몸통 안의 검사 시작점으로 유지합니다.

### 코드 담당 구분

모든 코드는 `Assets/02Scripts/01Player/` 아래에 있습니다.

| 파일 | 역할 |
| --- | --- |
| `Domain/CatCombatPolicy.cs` | 발사 간격·줌 중 걷기 규칙, 조준/퍼짐 상태 약속. Unity 타입 없음 |
| `Input/CatCombatInput.cs` | 실제 마우스 입력·포커스·재잠금 클릭 구분 |
| `Equipment/CatShotgunCombat.cs` | 입력·장비·자세·명중 결과 연결 |
| `Equipment/CatHitscanQuery.cs` | 카메라 조준점, 실제 총구4광선에 쓰는 충돌·벽 검사 |
| `Animation/CatShotgunPose.cs` | 새 발사 클립을 총과 양손 자세로 표현 |
| `Camera/CatAimZoom.cs` | 조준 상태를 기존 Cinemachine 렌즈에 전달 |
| `Equipment/Presentation/CatShotSpreadRing.cs`와 `.shader` | 현재 퍼짐 각도와 카메라 투영으로 속 빈 원 표시 |
| `Editor/Setup/CatCombatSetup.cs` | 허용된 씬에서 연결과 새 클립·재질 생성 |
| `Editor/Validation/CatHitscanValidation.cs` | 실제 충돌·벽 검사 |
| `Editor/Validation/CatCombatValidation.cs` | 이동·줌·손 자세·산탄·링·밀착 벽 통합 검사 |
| `Editor/Validation/CatCombatInputValidation.cs` | 가상 입력 장치로 실제 기본 키·마우스 처리 검사 |

적에게 맞았을 때의 결과는 `ICatShotReceiver.ReceiveShot(CatShotHit)`에 전달합니다. 적 담당자가 이 약속을 구현해 체력을 줄이면 됩니다. 이 작업에는 적 체력·대미지 수치·탄약·재장전·효과음을 추가하지 않았습니다. `PelletResolved`는 산탄마다, `ShotFired`는 한 번 발사할 때마다 알려주는 이벤트입니다.

발사 규칙은 일반 C#으로 재사용할 수 있습니다. 엔진을 바꾸면 입력·물리·클립 재생·Cinemachine·화면 표시 연결부를 교체해야 합니다. 다른 엔진에서 실행 검증한 상태는 아닙니다.

### 확인 방법

`PlayerTestScene`만 열고 Play한 뒤 `Tools > Cat Player > Play 모드 조준 발사 검증` 또는 `Play 모드 실제 조준 입력 검증`을 실행합니다. 각각 새 Play에서 실행하고 도중에 직접 조작하지 않습니다. 검증은 종료 시 Play를 끝내고 `Logs`에 결과를 씁니다. `순수 규칙 검증`은 편집 모드에서도 가능합니다. 물리 검사 코드는 Play 전용이며 테스트에 사용한 임시 물체는 씬에 저장하지 않습니다.

공식 자료: [클립의 지정 시각 재생](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AnimationClip.SampleAnimation.html), [Cinemachine 렌즈](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineCamera.html), [광선 검사와 시작점 내부 제한](https://docs.unity3d.com/ScriptReference/Physics.Raycast.html), [구형 경로 검사](https://docs.unity3d.com/ScriptReference/Physics.SphereCastNonAlloc.html), [겹침 검사](https://docs.unity3d.com/ScriptReference/Physics.OverlapSphereNonAlloc.html). 좁은 벽 검사는 광선 하나에만 맡기지 않고 경로와 양 끝 겹침을 함께 검사합니다.

### 샷건 구현 검증 결과 (2026-10-06)

독립 규칙35개, 실제 Unity 충돌18개, 전투 통합32개, 기본 장치 입력 경로15개를 통과했습니다. Game 화면의 원 표시와 줌 축소, 사선에서 총을 앞으로 내민 양손 자세도 확인했습니다. 최초 준비 시간 오류를 수정한 기록과 최종 증거 경로는 `PLAYER_WORK_LOG.md`에 있습니다. 입력 검사는 가상 장치를 통한 기본 처리 경로 검사이며, 전체 프로젝트 빌드 시험은 아닙니다.

## GitHub Desktop에서 붉은 줄이 보일 때

오른쪽 비교 화면의 붉은 줄은 바뀌기 전 내용, 초록 줄은 바뀐 뒤 내용입니다. 한 줄을 수정해도 이전 줄을 빼고 새 줄을 넣은 것처럼 표시합니다. 이 색 자체를 오류로 보고 Discard Changes(수정 취소)를 누르지 않습니다.

노란 LF→CRLF 안내는 줄바꿈 방식의 자동 변환 예정입니다. 코드가 고장났다는 뜻은 아닙니다. 현재 저장소는 하위 .gitattributes의 매크로 선언10~12행이 허용되지 않는 별도 설정 문제가 확인됐습니다. 파일을 이동하지 않고 약식 규칙을 실제 속성으로 풀어 쓰면 정리할 수 있지만, Git/LFS 적용 범위가 달라지므로 이 진단에서는 변경하지 않았습니다.

커밋은 현재 상태를 로컬 이력에 기록하는 일입니다. 변경 파일과 .meta를 함께 확인하고 왼쪽 아래 Summary에 `샷건 조준·4발 산탄·발사 애니메이션 구현 및 패키지 오류 수정`처럼 내용을 적은 뒤 `Commit … files to Player`를 누릅니다. Summary는 필수이고 Description은 선택입니다. 성공하면 History에서 커밋을 확인합니다. Push origin은 그 이후 GitHub로 전송하는 별도 동작입니다.

## OneDrive 밖에서 프로젝트 사용하기

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
