# 에디터 도구, 에셋, 협업 연결

검토 기준: 2026-10-14의 실제 프로젝트 파일과 작업 로그입니다. 이 문서는 코드를 읽고 정리한 기술문서이며, 문서를 작성하면서 설치 메뉴나 Play 검증을 새로 실행하지 않았습니다. 이전 시험 결과와 현재 파일에서 확인한 사실을 구분합니다.

## F11. 에디터 구성·검증 도구

### 왜 게임에서 안 쓰는 스크립트가 있나요?

`Editor` 폴더의 코드는 개발자가 Unity에서 연결을 확인하고 시험할 때 사용하는 공구입니다. 예를 들어 드라이버를 게임 캐릭터가 사용하는 것이 아니라 개발자가 조립할 때 쓰는 것과 같습니다. 게임 중 붙어 있는 컴포넌트가 없다고 미사용 파일로 판단하면 안 됩니다. 아래 파일들은 수동 메뉴, 다른 검증기, 외부 검사 명령에서 사용됩니다.

현재 `Assets/02Scripts/01Player/Editor`에는 Setup 4개와 Validation 7개, 합계 **C# 파일 11개**가 있습니다. 이 폴더 밖 팀원 Editor 코드는 여기의 플레이어 공구와 구분합니다.

### 설치 도구 4개: 누르면 씬이 바뀝니다

공통 메뉴 시작점은 `Tools > Cat Player`입니다. 이 표의 메뉴는 단순 보기 버튼이 아닙니다. 기존 연결을 검사한 뒤 컴포넌트·참조를 연결하고 `PlaytestScene01`을 저장합니다. 현재 씬에 다른 미저장 편집이 있으면 그것도 함께 저장될 수 있습니다. 따라서 작업 중인 씬에 무작정 실행하지 않습니다.

| 파일·진입점 | 메뉴와 책임 | 실제로 바뀔 수 있는 것 | 보존·거부 조건 |
|---|---|---|---|
| [InteractionSetup.cs](../Assets/02Scripts/01Player/Editor/Setup/InteractionSetup.cs), `Configure()` | 상호작용 연결 확인 및 설치 | 상호작용/획득 연출/중앙 커서 연결. 기존 샘플 루트가 없으면 실린더 4개·큐브 2개 배치. 커서가 없으면 구체 생성 | 기존 GUID로 에셋 조회. 잘못된 연결을 새 에셋으로 대신하지 않음 |
| [EquipmentSetup.cs](../Assets/02Scripts/01Player/Editor/Setup/EquipmentSetup.cs), `Configure()` | 샷건 연결 확인 및 설치 | 기존 총과 `LeftGrip`/`RightGrip`, 손 본, 몸통 추적 본 연결. 총이 없으면 기존 `Shotgun_Ready` 인스턴스 생성 | 총 후보가 여러 개이면 중단. 이미 조절한 위치·회전·크기는 보존 |
| [InteractionSettings.cs](../Assets/02Scripts/01Player/Editor/Setup/InteractionSettings.cs), `Apply()` | 무마찰·정지 카메라 설정 연결 | 플레이어 Rigidbody 감쇠 0, 해당 Rigidbody의 충돌체 재질 연결, 정지 회전 제한의 이동 상태 연결 | 기존 무마찰 재질이 0/Multiply인지 검사. 기존 회전 각도와 장비 설정은 덮지 않음 |
| [CombatSetup.cs](../Assets/02Scripts/01Player/Editor/Setup/CombatSetup.cs), `Configure()` | 조준과 샷건 발사 연결 | 샷건 자세·발사·줌·산탄 원 연결. 없을 때 `Muzzle`/`ShotGuardOrigin` 기준점 생성 | 총과 양손 연결을 먼저 요구. 기존 클립·재질·산탄/자세 설정을 사용 |

`InteractionSetup.Inspect()`는 루트·매대·Canvas·Camera 정보를 Console에 출력합니다. 이 함수에는 메뉴 특성이 없으므로 같은 이름의 메뉴가 자동 생성되지는 않습니다. `Configure()`와 달리 설치·씬 저장을 하지 않습니다.

#### 코드리뷰: 연결 검사부터 하는 이유

`EquipmentSetup.Configure()`의 일부입니다.

```csharp
var candidates = bones.Where(bone => bone != player.transform &&
    bone.Find("LeftGrip") != null && bone.Find("RightGrip") != null).ToArray();
if (candidates.Length > 1)
    throw new InvalidOperationException("기존 샷건 후보가 여러 개입니다. 자동 선택하거나 새 총을 추가하지 않습니다.");
```

손잡이 두 개를 가진 물체를 찾고 후보가 하나일 때만 사용합니다. 총이 이미 있는데 참조 하나가 끊어졌다고 새 총을 또 만들지 않으려는 검사입니다. 다만 `DEF_Hand.L`, `DEF_Hand.R`, `CTRL_Squash` 같은 **현재 리그의 이름**에 의존합니다. 모델을 교체하면 본 이름과 연결을 확인해야 합니다. 후보 0개나 이름 변경은 정상적인 자동 수리 범위를 벗어날 수 있습니다.

`CombatSetup`은 총구 기준점이 없을 때 기존 메시의 정점 중 로컬 X가 가장 작은 끝을 찾습니다. 이는 **현재 샷건의 총열 축이 -X인 구조**를 전제로 합니다. 다른 방향으로 만든 총에 그대로 적용하면 총구가 반대쪽에 생길 수 있으므로, 새 모델에서는 `Muzzle`을 먼저 올바른 위치에 준비하는 편이 명확합니다.

설치 도구가 공통으로 수행하는 저장 부분입니다.

```csharp
EditorSceneManager.MarkSceneDirty(scene);
if (!EditorSceneManager.SaveScene(scene))
    throw new InvalidOperationException("PlaytestScene01 저장에 실패했습니다. 변경 내용은 Undo로 확인할 수 있습니다.");
```

`Dirty`는 “아직 저장되지 않은 변경이 있음”이라는 뜻입니다. 저장 대상 씬을 제한하지만, 한 씬의 변경 중 특정 컴포넌트만 골라 저장하는 것은 아닙니다. `Undo` 기록은 실수를 되돌리는 보조 장치이며 사전 백업과 저장 범위 검사를 대신하지 않습니다.

### 검증 도구 7개: 어떤 시험인지 구분하기

| 파일 | 주요 함수·역할 | 시험 전제와 산출물 |
|---|---|---|
| [PlayerValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/PlayerValidation.cs) | `Inspect()` 필수 연결 확인, `InspectSharedAssets()` 협업 참조 검사, `Start()`/`Tick()`/`Finish()` 이동 회귀 시험. 같은 파일의 `PlayerEditorScope`는 범위·동시 실행 통제 | 허용 씬 하나. 연결 검사는 저장하지 않음. Play 시험은 입력·위치 등을 잠시 바꾸고 종료 시 Play도 끝냄 |
| [PlayerValidationCases.cs](../Assets/02Scripts/01Player/Editor/Validation/PlayerValidationCases.cs) | `All` 시험 목록 19개, `HasExpectedMap()` 지형 전제, `Observe()` 관측, `Evaluate()` 판정 | 옛 Texas 맵의 이름·높이·좌표에 의존. 전제가 다르면 검증불가 |
| [InteractionValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/InteractionValidation.cs) | `Start()`/`Cases()`로 집기·해제·장비·UI 연출·강조·회전 검사. `ObserveEquipmentPose()`는 실제 렌더 시점 손 위치 관측 | 샘플 실린더/큐브 등 연결 필요. 시험 중 해당 물건을 이동·비활성화하고 임시 UI/벽을 만듦. 종료 정리 후 Play 종료 |
| [CombatValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/CombatValidation.cs) | 교체 가능한 입력 공급자로 발사 준비·4산탄·줌·벽 차단·양손·표시 원 검사. `OnPellet()`/`OnShot()`/`OnPose()`로 이벤트 관측 | 실제 Unity 실행 루프와 물리 사용. 실제 키보드 입력 시험과는 구분. 위치·카메라·입력을 잠시 바꾸고 종료 복구 |
| [CombatInputValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/CombatInputValidation.cs) | `QueueDevices()` → 기본 입력 공급자 → 장착·사격·줌·ESC 검사 | InputSystem 가상 Keyboard/Mouse 이벤트를 사용. Game 창 포커스와 진행 상태 필요. 사람이 하드웨어로 누른 시험은 아님 |
| [HitscanValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/HitscanValidation.cs) | `Run()`에서 실제 PhysX 질의 18개 검사. `Box()`/`NewObject()` 시험물체 생성 | Play에서 먼 임시 공간을 사용. 메뉴 특성 없음. 결과 문자열 반환. 자기 임시 루트만 `finally`에서 제거 |
| [PolicyValidation.cs](../Assets/02Scripts/01Player/Editor/Validation/PolicyValidation.cs) | `RunMovement()`/`RunHands()`/`RunOrbit()`/`RunCombat()`의 일반 C# 규칙 검사 | `순수 규칙 검증` 메뉴. Unity 없는 실행 파일로도 빌드할 수 있음. 캐릭터 화면·물리·UI의 정상 여부까지 증명하지 않음 |

최근 작업 로그의 “35개 순수 규칙 통과”는 `PolicyValidation`에서 이동 6·손 13·회전 8·전투 8 항목을 합한 기록입니다. 점프·입력 권한·덫 보상 전체가 이 35개에 들어 있다는 뜻은 아닙니다. 다른 기능의 별도 시험 기록을 함께 봐야 합니다.

#### 코드리뷰: 잘못된 씬에서 실행을 막기

`PlayerValidation.cs` 안의 `PlayerEditorScope.RequireScene()`입니다.

```csharp
if (SceneManager.sceneCount != 1 || !scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
    throw new InvalidOperationException("PlaytestScene01 하나만 열려 있어야 합니다. 다른 씬은 자동으로 열거나 변경하지 않습니다.");
```

`ScenePath`는 `Assets/01Scenes/PlaytestScene01.unity`입니다. “현재 화면에 플레이어가 보인다”만으로 작업 대상을 정하지 않고, 열린 씬 수와 정확한 경로를 검사합니다. `FindSingle<T>()`도 대상이 정확히 하나가 아니면 중단합니다. 여러 플레이어가 있는 다른 씬에 맞추려면 이 도구의 범위 규칙부터 별도로 설계해야 합니다.

`BeginValidation()`은 검사 중인 도구 이름을 기록하고, 다른 검증기가 동시에 키보드·카메라·씬을 조작하지 못하게 막습니다. `EndValidation()`은 시작한 도구와 같은 이름일 때만 점유를 풉니다. 게임플레이의 멀티플레이 권한 기능이 아니라 **개발 도구끼리 충돌하지 않게 하는 잠금**입니다.

#### 코드리뷰: 덫 때문에 사격 검사가 실패하는 것을 구분하기

`PlayerEditorScope.UseNonTrapSelection()`은 사격/운반 시험 동안 덫이 아닌 슬롯을 잠깐 선택합니다. 덫이 선택되어 있으면 좌클릭이 설치 입력으로 먼저 사용되므로, 사격 코드가 정상이어도 시험이 실패할 수 있기 때문입니다.

```csharp
if (index < 0)
    throw new InvalidOperationException("검증불가: " + inventory.name + "의 모든 슬롯이 덫입니다. " +
        "사격·운반 검증에는 빈 칸 또는 덫이 아닌 칸이 필요합니다. 아이템은 삭제하지 않았습니다.");
```

이 도구는 아이템을 지워 시험 환경을 만들지 않습니다. 슬롯 **내용**은 유지하고, 선택 번호와 휠 선택 입력 사용 여부만 빌려 씁니다. `IDisposable.Dispose()`에서 원래 선택을 돌려줍니다. 쉽게 말하면 “책갈피를 잠깐 다른 페이지에 옮겼다가 돌려놓는 것”입니다.

#### 코드리뷰: 실제 버튼 경로를 시험하기

`CombatInputValidation.QueueDevices()` 일부입니다.

```csharp
InputSystem.QueueStateEvent(keyboard, keys);
InputSystem.QueueStateEvent(mouse, new MouseState
{ position = mousePosition, buttons = (ushort)((left ? 1 : 0) | (right ? 2 : 0)) });
```

발사 함수를 바로 호출하지 않고, 가상 장치에서 키가 눌렸다는 이벤트를 넣습니다. 그래서 “버튼 읽기 → 입력 차단 → 장비 상태 → 발사” 연결도 검사합니다. 하지만 창 포커스를 잃으면 게임이 정상적으로 입력을 막으므로 시험의 전제가 깨집니다. `runInBackground=true`가 게임 권한 차단을 무효화하지는 않습니다.

설정창 시험의 `PassiveMenuPoint()`는 화면 네 모서리를 Raycast해 버튼·슬라이더를 실제로 누르지 않는 위치를 찾습니다. 여기서 통과하는 것은 **설정창 클릭이 총 발사나 커서 잠금으로 새지 않음**입니다. 슬라이더 값 변경 시험은 2026-10-12 외부 UI 검증 기록으로 별도 구분합니다.

#### 코드리뷰: 시험 뒤 정리와 남은 한계

Play 검증기는 이벤트 구독을 해제하고 임시 물체·장치를 제거하며 선택·입력·커서를 복구합니다. `Finish()`에는 Play를 종료하는 코드도 있습니다. 따라서 **진행 중인 라운드에서 검사 메뉴를 누르면 그 Play 세션이 끝날 수 있습니다.** 에디터에서 변경된 재고를 파일로 저장하는 도구가 아닙니다.

`InteractionValidation`은 재컴파일 상황을 재현하기 위해 테스트 안에서 `PlayerController`의 비공개 필드 `inputSource`, `locomotion`, `animationPresenter`를 Reflection으로 비우는 부분이 있습니다. 실제 게임 입력 코드의 일반 처리 방식은 아닙니다. 이 필드 이름을 바꾸면 이 시험도 함께 갱신해야 합니다.

현재 이동 시험에는 `ClickRelocks`처럼 클릭으로 재잠금을 기대하는 옛 사례가 남아 있습니다. 설정창이 붙은 씬의 최신 규칙은 “ESC로 닫은 뒤 잠금 복귀”이므로, 이 사례를 어떤 상황에 실행하는지 구분해야 합니다. 옛 고정 맵 19개 사례도 복구된 맵에서는 전제 불일치로 검증불가 기록이 있습니다. **검사 파일이 존재함과 현재 씬에서 전부 통과함은 다릅니다.** 이번 문서 작업에서 게임 코드나 시험 기대값을 바꾸지 않았습니다.

### 검증 결과를 읽는 순서

1. 편집 상태에서 `협업 에셋 연결 검사`로 Missing Script·메시·재질·본·클립 경로를 확인합니다. `None`이 모두 오류인 것은 아닙니다. 저장된 식별자는 있는데 객체가 없을 때 Missing으로 구분합니다.
2. `순수 규칙 검증`은 판단 규칙만 빠르게 확인합니다.
3. 별도의 새 Play 세션에서 필요한 통합 시험 하나만 실행합니다. 설치 메뉴와 시험 메뉴를 동시에 실행하지 않습니다.
4. 결과의 `failures`, `unavailable`, `interrupted`를 모두 읽습니다. “일부 PASS 출력”만 보고 전체 통과로 판단하지 않습니다.
5. 끝난 뒤 가상 입력장치·임시 물체 잔존, 원래 씬 미저장 상태, 기존 메타·GUID 보존을 확인합니다.

현재 공통 결과 폴더는 `%TEMP%/PlayerValidation`입니다. `SharedAssets.txt`와 `PlayerValidation.txt`는 같은 이름으로 기록되고, 여러 상호작용·전투 기록은 시간 이름을 사용합니다. 과거 `Logs/` 기록과 외부 검증 폴더는 당시 기록 위치이며 현재 메뉴 출력 위치와 혼동하지 않습니다.

### SOLID 관점에서 본 도구 구조

| 원칙 | 잘 나뉜 부분 | 현재 결합과 수정할 때 볼 부분 |
|---|---|---|
| 단일 책임 | 설치, 이동 사례, 입력 시험, 물리 질의 시험을 분리 | `PlayerValidation.cs`는 실행기와 공통 범위 도우미를 함께 포함. 같은 파일이라고 곧바로 위반은 아니지만 읽기 범위가 큼 |
| 개방·폐쇄 | 입력 공급자를 대체해 게임 코드를 고치지 않고 시험 | 고정 맵 사례는 맵 변경 시 데이터/전제 수정 필요 |
| 치환 | 실제 입력과 시험 입력이 같은 입력 계약을 사용 | 입력 공급자만 통과해도 실제 UI/장치 연결까지 통과했다고 판단하면 안 됨 |
| 인터페이스 분리 | 시험에 필요한 이동·손·전투 입력 계약을 별도로 사용 | 설치 도구는 실제 컴포넌트와 에디터 API를 직접 알아야 하므로 엔진 독립 코드는 아님 |
| 의존 역전 | 순수 규칙은 Unity 없이 검사 가능 | 본 이름, 씬 경로, 기존 GUID는 프로젝트 구성에 묶임. 다른 모델/엔진에서는 연결 도구를 교체해야 함 |

## F12. GUID와 협업 에셋 보존

GUID는 파일의 “주민번호”에 가깝습니다. `Cat_Player00.fbx`라는 이름 자체보다 `.meta` 안의 GUID를 통해 씬·프리팹이 모델을 찾습니다. 파일만 복사하고 `.meta`를 빼거나, 삭제 후 새 파일로 만들면 같은 이름이어도 다른 에셋으로 인식될 수 있습니다. 이는 Unity의 [에셋 메타데이터 설명](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html)에 따른 구조입니다.

`ExistingPlayerAsset<T>()`는 파일 위치를 추측하지 않고 `AssetDatabase.GUIDToAssetPath(guid)`로 기존 에셋을 찾습니다. 결과가 허용된 Player 폴더가 아니거나 필요한 타입이 아니면 중단합니다. GUID가 맞아도 다른 타입의 객체를 Material 칸에 연결하면 정상 연결이 아닙니다. 2026-10-14 왼손 외곽선 오류는 재질 칸이 `Cylinder_1` GameObject를 가리킨 사례였습니다.

협업에서 보존할 묶음은 **에셋 파일 + 기존 .meta + 참조하는 씬/프리팹**입니다. 코드명 변경도 `.cs`와 `.cs.meta`를 함께 이동하고 기존 GUID를 유지해야 합니다. 2026-10-07의 이름 변경은 승인된 40쌍에 한해 수행된 이력이며, 앞으로 모든 이름 변경을 허용한다는 뜻은 아닙니다.

### PR #49의 확인된 결과

로컬 병합 커밋은 `6b8bafde9f16129d6c6a9a7c7127aaca5582a63a`입니다. 부모는 기존 Player `a5cb901988583f948c9aefeb7798afc67efab6a7`과 main `2992b05a5fa9e2c95c7cf906a544072d8ffdbbdb`입니다. 기존 Player 추적 파일 2,049개가 모두 같은 Git blob이며, 기존 메타 1,074개를 보존했습니다. 승인된 다음 4개 파일만 main 원본과 같은 내용으로 추가했습니다.

| 추가된 에셋 쌍 | 보존한 GUID |
|---|---|
| `Assets/04Prefabs/Object/Trap/Prefabs/JawTrap_Consumable 1.prefab` 및 `.meta` | `0abe8137d1c0aa94ea75d70628763a78` |
| `Assets/04Prefabs/Station/Prefab/Texas_Station_Unity.prefab` 및 `.meta` | `a2be60dfb0236ee439771e7fb1c18283` |

기존 `Trap_Inventory.prefab`은 원래 덫 GUID `ff9d9f1ec73eea3a0ddef1892fb9d29a`를 사용하므로 원본 덫과 메타도 남겼습니다. 새 프리팹이 추가되었다고 현재 씬의 덫·맵이 자동 교체된 것은 아닙니다. **원격 푸시는 인증 403으로 완료되지 않았습니다.** 로컬 병합 완료와 GitHub 반영 완료를 구분해야 합니다. 이 문서 작업에서는 인증정보를 조회하거나 로그인을 시도하지 않습니다.

근거: [충돌 해결 검토안](C:/Users/307/Documents/Codex/PR49Conflict20261014/CONFLICT_RESOLUTION_REVIEW.md), [후보 검증](C:/Users/307/Documents/Codex/PR49Conflict20261014/validation_final.json). 독립 Git 트리 대조에서 같은 결과를 확인했습니다. 새 프리팹의 실제 Play 사용성 검증은 별개입니다.

## F13. 모델·리깅·재질·Unity 전달

### 과거 제작 요청과 현재 확인 사실

대화에서는 4방향 레퍼런스 일치, 매끈한 원형, 쿼드 리토폴로지, 귀·눈구멍·볼·주둥이 교정, 꼬리 형태·모션, 색 경계와 직접 그린 그림자, 단일 메시 등을 여러 차례 요청했습니다. 폴리곤 한도도 초기 2,000삼각형 → 900폴리곤 → 1,800쿼드 허용 → 눈·손·발·꼬리를 제외한 900 이하처럼 범위가 달라졌습니다. **서로 다른 시점의 요구를 하나의 최종 달성 수치로 합칠 수 없습니다.**

현재 확인한 Unity 자료는 다음과 같습니다.

| 분류 | 실제 파일·구조 | 확인할 수 있는 내용과 한계 |
|---|---|---|
| 모델 | [Cat_Player00.fbx](../Assets/04Prefabs/Player/Models/Cat_Player00.fbx) | FBX와 기존 importer 메타 존재. Blender 고밀도 원형이나 모든 제작 버전의 완료를 증명하지 않음 |
| 플레이어 프리팹 | [Cat_Player (1).prefab](../Assets/04Prefabs/Player/Prefabs/Cat_Player%20%281%29.prefab) | 저장 텍스트에 SkinnedMeshRenderer 10개. 현재 Unity 구조를 단일 메시라고 설명하면 부정확함 |
| 리그 | 프리팹의 `Cat51_Rig`, `CTRL_Root`, `CTRL_Squash`, `DEF_Hips/Spine/Chest/Neck/Head`, 양손·발·귀·눈, 꼬리 `DEF_Tail_01`~`05` | 본과 스킨 연결이 저장됨. 본 회전·이동·크기를 클립이 바꾸는 구조. 비어 있는 BlendShapeWeights만으로 FBX 내부 모든 형상 정보를 단정하지 않음 |
| 색 | `Assets/04Prefabs/Player/Palette`의 부위별 PNG 10개 | 몸·귀·꼬리, 양눈·검은자위·손·발·코의 팔레트. 빛에 따라 색이 자동 음영 처리되는 재질과 직접 칠한 음영을 구분 |
| 외곽선 | [CatOutline.shader](../Assets/04Prefabs/Player/Materials/CatOutline.shader), `Cat_Outline.mat`, `Cat_EyeOutline.mat` | 법선 방향으로 넓힌 뒤 앞면을 숨겨 테두리를 그리는 패스. 본체 메시와 별도 그리기 비용이 있음 |
| 눈 | [CatEye.shader](../Assets/04Prefabs/Player/Materials/CatEye.shader), Eye/Pupil 재질 | URP Unlit 텍스처 표시. Transparent, ZWrite Off, ZTest LEqual. 렌더 순서가 겹침 표현에 영향 |
| 샷건 | [Shotgun01_RedBlue.fbx](../Assets/04Prefabs/Player/Weapons/Shotgun/Shotgun01_RedBlue.fbx), `Shotgun_Ready.prefab` | 총열 빨강·손잡이 파랑 등 재질 4개, 손 기준점. 소환 모션 대신 장착 기능에 연결 |
| 물리 | [Player_NoFriction.physicMaterial](../Assets/04Prefabs/Player/Physics/Player_NoFriction.physicMaterial) | 렌더용 팔레트와 다른 물리 재질. 마찰 조합용 |
| 상호작용/덫 | `Interaction/Prefabs` 및 `Inventory` | CarryCylinder, InventoryCube, TrapItem, Trap_Inventory, 미리보기 재질·아이콘 |

### 애니메이션 파일을 읽는 법

| 현재 파일 | 저장된 길이·반복 | 역할 |
|---|---|---|
| [Idle.anim](../Assets/04Prefabs/Player/Animations/Idle.anim) | 2초, 반복 | 대기 본 움직임 |
| [Walk.anim](../Assets/04Prefabs/Player/Animations/Walk.anim) | 약 0.6857143초, 반복 | 걷기 본 움직임 |
| [Run.anim](../Assets/04Prefabs/Player/Animations/Run.anim) | 0.4초, 반복 | 달리기 본 움직임 |
| [Jump.anim](../Assets/04Prefabs/Player/Animations/Jump.anim) | 1.2초, 비반복 | 눌림·늘어남·귀/손/꼬리 반응의 제자리 자세 |
| [Shotgun_Fire.anim](../Assets/03Sprites/Player/Animations/Shotgun_Fire.anim) | 0.5초, 비반복 | `ShotgunPose`의 준비·반동 값 연결 |

이 길이는 파일의 `m_StopTime` 값입니다. Animator 상태 속도, 코드의 점프 진행도·정지 유지에 따라 실제 화면 시간이 달라질 수 있으므로 “점프 물리 시간이 항상 1.2초”라는 뜻은 아닙니다. 실제 높이 이동은 Rigidbody가 담당하고 모양의 눌림은 본 애니메이션이 담당합니다.

`Animations`에는 `Idle/Walk/Run/PlayerJump.controller` 4개가 있습니다. `PlayerJump`는 최신 점프·사격 레이어 연결용이고 구형 `Idle.controller` 호환 경로도 남아 있습니다. `Models/Idle.anim`, `naim/idle.anim` 같은 별도 파일은 이름이 비슷하다는 이유만으로 중복 삭제하지 않습니다. 참조·내용·재생 속도가 같다는 확인이 필요합니다.

현재 파일 인벤토리에서 `Assets/04Prefabs/Player`는 메타를 제외한 57파일(FBX 2, 프리팹 5, 재질 25, PNG 11, 애니메이션 6, 컨트롤러 4, 셰이더 2, 물리재질 1, 아이템 데이터 1), `Assets/03Sprites/Player`는 2파일(발사 클립과 산탄 원 재질)입니다. 파일 개수는 사용 중인 Renderer 개수나 폴리곤 수가 아닙니다.

### 색과 외곽선을 수정할 때

눈 셰이더는 `SAMPLE_TEXTURE2D(_BaseMap, ...)`의 색을 직접 반환합니다. 스크립트로 눈을 각도별로 숨기는 기능이라고 해석하면 안 됩니다. 2026-10-07의 수정 기록은 모델을 깎은 작업이 아니라 눈과 검은자위의 재질 렌더 순서를 분리한 것입니다. 사선에서 안 보이면 활성 상태 → 메시/재질 참조 → 렌더 순서 → 깊이 검사 순으로 확인합니다.

외곽선 셰이더는 `Cull Front`로 앞면을 숨기고, 정점 법선 방향으로 `_Width`만큼 넓힙니다. 값이 커지면 윤곽이 굵어지지만 귀·눈 근처가 겹쳐 보일 수 있습니다. 현재 재질을 여러 부위가 공유하므로 재질의 값을 바꾸면 연결된 여러 부위가 함께 바뀝니다. 연결만 고치는 것과 공유 재질의 값을 바꾸는 것을 구분합니다.

**현재 문서 검토에서는 FBX의 최종 삼각형 수, 쿼드 비율, 전 관절 가중치, 4방향 50% 겹침, Blender 원형 22의 정확한 보존 여부를 새로 검사하지 않았습니다.** 초기 요청의 숫자나 “원형 22” 이름을 현재 완료 인증으로 쓰지 않습니다. URP에서 실시간 SSAO·SDF 툰 셰이딩이 모두 적용되었다고도 주장하지 않습니다. 현재 확인된 눈/외곽선 셰이더 구조와 과거 렌더 검증만 기록합니다.

후속 모델 교체의 확인 순서는 기존 FBX·메타 보존 → 새 모델의 본/메시/팔레트 구조 확인 → Animator 곡선 경로 확인 → 양손 기준점·총구축 확인 → 사선 눈·손발 외곽선 확인 → 동작 검증입니다. 이 순서는 수정 안내이며 이번 문서 작성 중 모델 교체를 수행한 것은 아닙니다.
