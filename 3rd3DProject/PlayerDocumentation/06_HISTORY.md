# 구현 이력과 검증 근거 추적표

작성 기준: 2026-10-14. 이 문서는 기존 [작업 로그](../PLAYER_WORK_LOG.md)를 대체하거나 과거 기록을 고치지 않습니다. 요구사항이 어떻게 바뀌었는지와 현재 코드를 연결하는 안내판입니다. 문서 작성 중 게임 코드·씬·메타를 변경하거나 Play 시험을 새로 수행하지 않았습니다.

## 기록을 읽는 기준

- **현재 파일 확인**: 이번 문서 검토에서 실제 소스·에셋의 존재와 내용을 읽었습니다. 실행 성공과 같지 않습니다.
- **당시 검증 기록**: 과거 로그가 검사 방법·결과·증거 위치를 남겼습니다. 오늘 전체 재시험했다는 뜻이 아닙니다.
- **대화 요청 이력**: 사용자 요구는 확인되지만 현재 원본 파일이나 실행 증거를 연결하지 못한 부분입니다. 완료로 표시하지 않습니다.
- **미완료/미재현**: 이전에 시험이 통과했더라도 새로 제보된 문제가 재현되지 않았거나 검증이 끝나지 않은 상태입니다.

다른 날짜의 PASS 숫자를 더해 “모든 기능 완전 검증” 숫자로 만들지 않습니다. 같은 검사에 반복 항목이 있고, 이전에 놓친 문제가 나중에 확인된 사례도 있기 때문입니다. 로컬 절대경로 증거는 이 PC의 기록입니다. GitHub에 자동 업로드된 자료이거나 팀원 PC에도 존재하는 파일이라는 뜻은 아닙니다.

## 기능 번호와 현재 담당 위치

| ID | 기능 | 현재 대표 코드·에셋 |
|---|---|---|
| F01 | 입력과 조작 권한 | `Input/GameplayInput.cs`, `Domain/InputPermission.cs`, `Input/PlayerInput.cs`, `Input/InteractionInput.cs`, `Input/CombatInput.cs` |
| F02 | 이동·턱·물리 | `Core/PlayerController.cs`, `Movement/PlayerMovement.cs`, `StepSolver.cs`, `StepSettings.cs`, `Player_NoFriction.physicMaterial` |
| F03 | 카메라·커서 잠금 | `Camera/CameraCursorLock.cs`, `CameraOrbitLimit.cs`, `AimZoom.cs`, `Domain/OrbitPolicy.cs` |
| F04 | 애니메이션·점프 | `Animation/PlayerAnimation.cs`, `ShotgunPose.cs`, `Domain/JumpPolicy.cs`, `Movement/PlayerJump.cs`, `PlayerJump.controller`, `Jump.anim` |
| F05 | 좌클릭 운반 | `Interaction/InteractionController.cs`, `ItemCarrier.cs`, `CatInteractionItem.cs`, `Domain/HandPolicy.cs` |
| F06 | F 획득·적 상호작용 | `Assets/02Scripts/04Systems/ItemInteractor.cs`와 대상 탐색/입력 권한 연결 |
| F07 | 커서·강조·획득 연출 | `Interaction/Presentation/CenterCursor.cs`, `ItemHighlight.cs`, `HighlightTarget.cs`, `InventoryPickupEffect.cs` |
| F08 | 샷건·조준·발사 | `Equipment/ShotgunEquipment.cs`, `ShotgunCombat.cs`, `HitscanQuery.cs`, `Equipment/Presentation/ShotSpreadRing.cs` |
| F09 | 인벤토리·UI | `Assets/02Scripts/04Systems/HotbarManager.cs`, `HotbarUI.cs`, `HotbarItemData.cs`, 설정/결과 UI와의 연결 |
| F10 | 덫 설치·보상·수명 | `Interaction/Placement/TrapPlacement.cs`, `TrapRoundReward.cs`와 같은 폴더의 설치/획득 연결, `Inventory/TrapItem.asset` |
| F11 | 에디터 구성·검증 | `Editor/Setup` 4파일, `Editor/Validation` 7파일 |
| F12 | Git·GUID·협업 | `.meta` 보존, 기존 참조 검사, 승인된 Git 작업·범위 지문 기록 |
| F13 | 모델·리깅·재질·에셋 | `Assets/04Prefabs/Player`, `Assets/03Sprites/Player`의 현재 모델·팔레트·클립·재질 |

위에서 `Assets/...`로 시작하지 않는 C# 경로는 `Assets/02Scripts/01Player/` 아래입니다. 현재 파일 전체 역할은 각 기능 문서와 [에디터·에셋 문서](05_EDITOR_ASSETS.md)를 함께 봅니다. 과거 Cat 접두사 파일명은 2026-10-07 이름 대응표를 통해 현재 이름으로 찾습니다.

## 초기 모델 제작 대화: 요청과 확인 가능 사실을 분리

다음은 날짜가 확정되지 않은 초기 대화 이력입니다. 기존 작업 로그는 2026-09-30부터 시작하므로, 그보다 앞선 모든 Blender 작업을 현재 로그의 완료 작업으로 소급 작성하지 않습니다.

| 대화에서 요청한 작업 | 이후 결정·변경 | 현재 연결되는 자료 | 상태 |
|---|---|---|---|
| 앞치마 고양이 4방향 정사영·50% 겹침, 비율 실측, 고밀도 원형 후 저밀도화 | 이후 기존 캐릭터 폐기와 새 참조 캐릭터 제작 요청 | 현재 `Cat_Player00.fbx`의 존재만 확인 | 초기 캐릭터 완료 여부/비교 이미지 전수검증 불명 |
| 매끈한 원형, 모든 면 쿼드, 단일 메시, 폴리곤 축소 | 900폴리곤, 이후 1,800쿼드 허용, 특정 부위를 제외한 900 이하로 여러 차례 범위 조정 | 현재 프리팹 SkinnedMeshRenderer 10개 | 모든 최종 예산·단일 메시 달성 확인 안 됨 |
| 눈구멍·볼 연결, 측면 눈·코·턱선, 흰 털 경계 수정 | 각도별 눈 숨김 취소, 눈 아래 움푹한 부위를 채우기 | Eye/Pupil/Body 메시·재질, 이후 Unity 렌더 순서 교정 로그 | 당시 Blender 버전별 수정 결과는 현재 문서에서 인증하지 않음 |
| 귀 앞면 유지·뒷면 곡선, 분홍 귀 테두리 제거·깊이 | 형태 22를 기준으로 리토폴로지 요청 | Body_Ears_Tail, 이름에 Low22가 남은 부위 | 이름만으로 원형22 완전 일치 증명 불가 |
| 그림자 제거 후 필요한 부위에 단색 직접 그림자 | 손 아래·몸통에 번진 그림자 제거, 턱 아래 위치 축소 | 부위별 PNG 팔레트, URP Unlit 계열 재질 | 모든 팔레트 수정 단계를 역추적할 원본 미확인 |
| 외곽선 제거 후 다시 눈과 본체 굵기 조절·손발 추가 | Unity 손·발 외곽선 적용 및 왼손 참조 복구 | CatOutline.shader, Cat_Outline, 10-08/10-14 로그 | Unity 단계는 기록 존재, 초기 모든 렌더 방식 적용 여부 불명 |
| 꼬리 위치·길이·S자·접합 곡선 수정, 다이내믹 | 몇 차례 길이 변경 뒤 기존 동작에 적용 | 꼬리 5본, Idle/Walk/Run 클립 | 현행 본/클립은 존재. 실시간 물리 꼬리 솔버까지 적용됐다는 근거는 없음 |
| 대기·걷기·달리기 제작, 달리기 압축·귀 지연, 정면 발 사선 제거 | 기존 달리기를 걷기로 지정하고 새 달리기 요청 | 현행 3개 이동 클립 및 본 변환 곡선 | Unity 클립 존재·후속 실행 기록 확인. 각 Blender 작업본의 일치 검사는 안 함 |
| 총 꺼내기/넣기/발사/기절/놓기/집기 고려 리그 | 손 모으기+펑 소환 요청 후 Unity 장착에서는 소환 제외 | 샷건 FBX·손 기준점·Shotgun_Fire | 장착/발사는 후속 기록. 기절 등 모든 모션 제작 완료로 보지 않음 |
| Unity 전달 파일 하나에 캐릭터·팔레트·대기/걷기/달리기 포함 | 실제 Unity에 모델/재질/팔레트/프리팹/클립으로 분리 배치 | 현재 Player 에셋 목록 | 전달용 단일 아카이브의 현재 위치·무결성 미확인 |
| PUBG식 WASD/Shift, 마우스 3인칭, 맵 충돌, Cinemachine | 정지 회전 ±60도, 이동360도, 턱 통과, SOLID 분리 | F01~F04 현재 코드 및 9-30 이후 회귀 로그 | 현재 구현 및 과거 검증 기록 존재 |
| 초기 큐브·실린더 운반/F 토글·획득 UI | 우클릭→좌클릭 유지 운반, F는 손 상태 유지 저장으로 변경 | F05~F10 | 최신 규칙은 뒤의 날짜별 결정이 우선 |

삭제·복원·원형 일치 같은 표현은 대화의 **요청**과 실제 **수행 증거**가 모두 있는지 확인해야 합니다. 이번 문서는 원본 Blender 파일을 새로 열거나 재렌더하지 않았습니다.

## 날짜별 변경 추적

표의 줄 번호는 현재 `PLAYER_WORK_LOG.md` 기준입니다. 로그에 적힌 검증 결과는 당시 범위에 한정됩니다. 각 행의 기능 번호를 통해 현재 담당 코드와 연결할 수 있습니다.

| 로그 시작행·날짜 | 변경 이유와 결정 | 기능 | 현재 담당 파일/연결 | 검증 근거와 현재 해석 |
|---|---|---|---|---|
| 5 · 09-30 | 빈손 우클릭 집기, 숫자1 기존 샷건 장착. 소환 제외 | F05/F08/F11/F13 | InteractionController, ShotgunEquipment, EquipmentSetup | `New_Cat_OverlayFit/interaction72_work`, 기존 Logs 장비33/상호작용22 검사. 우클릭 규칙은 10-07 변경으로 대체 |
| 59 · 09-30 | 누르는 동안 보유, 무마찰, 중앙 보유, 1번 토글, 정지 ±60도, 판단 규칙 분리 | F01/F02/F03/F05/F08 | HandPolicy, MovementPolicy, OrbitPolicy, ItemCarrier, InteractionSettings | `interaction73_work`, 당시 정책27+상호작용38+이동19. 해제 대기/안전 위치 탐색은 10-07 제거 결정으로 대체 |
| 108 · 10-06 | manifest의 behavior 중복 키 제거 | F12 | Packages/manifest.json의 당시 복구 | `Logs/PackageRepair20261006` 이전 단계. JSON 검사만 했던 단계이며 후속 실제 Unity 확인은 다음 행 |
| 118 · 10-06 | lock의 Mathematics depth 중복, 누락 Cinemachine 복구 | F03/F12 | 당시 manifest/lock, 기존 Cinemachine 연결 | 패키지67개 오류0·컴파일·짧은 PlaySmoke 기록. 전체 게임 회귀/클린 빌드 아님 |
| 135 · 10-06 | 유지형 줌·줌 걷기·좌클릭4산탄·총구 벽 차단·준비/반동 | F01/F03/F04/F08 | CombatPolicy, ShotgunCombat, HitscanQuery, ShotgunPose, AimZoom, ShotSpreadRing | `Logs/Shotgun20261006`: 물리18·통합32·장치15·정책35 기록. 이후 Animator 연결 방식은 10-08 변경 |
| 160 · 10-06 | GitHub Desktop 빨강/초록과 LF/CRLF 경고 설명 | F12 | 문서 안내; 게임 코드 변경 없음 | 읽기 전용 diff/속성 검사. 하위 .gitattributes 경고는 미수정 |
| 170 · 10-06 | OneDrive 밖 이전 방법 안내 | F12 | 문서 안내 | 원인 확정 없이 복사·검증 절차 제시한 단계. 아직 실제 이전 아님 |
| 181 · 10-06 | 승인 후 원본 보존 복사와 연결 | F12 | 당시 C:/UnityProjects 사본 | Robocopy/파일 대조/Unity 검증 기록. 현재 프로젝트는 이후 Desktop/unity 복구본이므로 이 옛 경로를 수정 대상으로 쓰지 않음 |
| 202 · 10-06 | 사용자가 복구한 현재 프로젝트에 빠진 Player 연결 재적용 | F08/F11/F12 | 기존 15개 C#, PlaytestScene01 Player 참조 | `PlayerRestore20261006`: 기존meta1046 보존, 전투/상호작용 등 검사. 옛맵19개 검증불가, 현재맵 별도7개 통과 |
| 226 · 10-07 | 좌클릭 집기와 40개 C#의 간단한 이름, 책임 정리 | F01/F05/F07/F11/F12 | InteractionInput, InputActivationGate, IPickupPort, 현행 이름 | `PlayerRefactor20261007`: 40개 cs/meta쌍 GUID 보존, 대체 입력/연출 시험. CatInteractionItem은 직렬화 호환 위해 이름 유지 |
| 252 · 10-07 | 모델 그대로 두고 측면 검은자위 소실 수정 | F13 | Eye/Pupil 재질 Render Queue | 같은 조건14방향 렌더·측면 복구 기록. 모델 변경/각도별 눈 숨김으로 해결한 것이 아님 |
| 268 · 10-07 | 기존 ItemInteractor를 F 획득·5칸 보관으로 확장 | F06/F09 | ItemInteractor, HotbarManager, 등록 대상/저장 계약 | 해당 절의 F·재고 검사 기록. 별도 InteractionController 빈 오브젝트 만들기 취소 |
| 291 · 10-07 | F는 손 상태 유지 저장, 놓을 때 현재 위치에서 기본 중력 | F05/F06/F09 | ItemInteractor, ItemCarrier, CatInteractionItem | 바닥 스냅·공중 대기 제거, 동적 Rigidbody/중력 복구 기록. Collider 자체가 중력을 만드는 것은 아님 |
| 314 · 10-08 | 손발 외곽선 저장, 점프는 승인용 시안만 제작 | F04/F13 | Cat_Outline 슬롯, 외부 점프 초안 | `PlayerJumpPreview20261008`: 32본/샘플121 비교·렌더. 이 단계는 Space 적용 전 |
| 329 · 10-08 | 미리보기의 색이 붉어진 원인 교정 | F13 | 외부 PreviewCore의 SRGB RenderTexture | 원본68파일 동일, PNG 색 대조. 게임 팔레트가 변경된 문제로 처리하지 않음 |
| 338 · 10-08 | 승인받은 점프를 Space와 실제 물리에 연결 | F01/F02/F04 | JumpPolicy, PlayerJump, PlayerMovement, PlayerAnimation, Jump.anim/controller | `PlayerJumpApply20261008`: 정책61·입력16·환경23 등. 천장1개는 전제 부족 미실행 |
| 359 · 10-08 | 재생 연결을 Animator 상태/전환으로 이동 | F04/F08 | PlayerAnimation, ShotgunPose, PlayerController, PlayerJump.controller | `PlayerAnimatorNative20261008`. 구 Idle.controller 호환은 유지. 당시 상태 중심 검사가 뒤의 사격 후 정지를 놓쳤음 |
| 379 · 10-08 | 사격 뒤 본 정지 교정, 착지 직전 점프 0.15초 보관 | F01/F04/F08 | Shotgun Ready에 기존 Fire 0초 자세, JumpPolicy 버퍼 | `PlayerAnimationFix20261008`: 실제 본 필수판정 추가, 사격76+버퍼28. 일반 점프 혼합 개선은 다음 승인 단계 |
| 401 · 10-08 | 점프 진입/복귀 혼합과 이동 중 긴 착지 회복 교정 | F04 | PlayerJump.controller 진입0.08/복귀0.15, JumpPolicy 이동회복 | `PlayerJumpTransitions20261008`: 정책132, Play170 기록(공통 항목 반복 포함). 후기 재도약 경계도 별도 확인 |
| 421 · 10-08 | 저장/커밋 요청 중 카메라 충돌 두 곳 정리 | F03/F12 | PlaytestScene01 Main/Cinemachine 카메라 | `PlayerCommit20261008`: Player 구도 높이1.3789881 선택. Reload 팝업으로 당시 병합 후 Unity 검증 미완료 명시 |
| 431 · 10-12 | 설정창에서 커서 숨김, 권한/설정 적용 책임 분리 | F01/F03/F06/F09 | GameplayInput, InputPermission, CameraCursorLock, GameSettingsApplier, 메뉴 | 격리126조건·Play21 기록. 슬라이더 콜백 검사라 실제 UI 입력 누락을 놓쳤다는 한계가 다음 행에서 확인됨 |
| 449 · 10-12 | ESC UI 실제 조작 불가와 복귀 잠금 후속 교정 | F01/F03/F09 | 씬 EventSystem/InputSystemUIInputModule, CameraCursorLock | `PlayerMenuInputFix20261012`: 실제 UI Raycast·가상 마우스 드래그19조건. 설정값 복구; 하드웨어 수동검사와는 구분 |
| 466 · 10-12 | RAt(1) 이름은 맞지만 실제 F 대상이 잘못 연결 | F06/F12 | ItemInteractor의 씬 target 참조 | `PlayerRatInteractionFix20261012`: 실제 쥐 루트로 연결, Enemy 파일 미수정. 최종12조건; 숫자1 준비 실패 원인은 별도 미확정 |
| 480 · 10-14 | 왼손 재질 슬롯에 GameObject가 들어가 외곽선 누락 | F13/F12 | 씬 Hand_L_Low22 Materials[1] → 기존 Cat_Outline | `LeftHandOutline20261014`: 한 참조 교정, 렌더 확인. 실제 게임 Play 검사는 안 함 |
| 494 · 10-14 | 로컬 작업 커밋 없이 PR47의 스포너/포획 변경만 반영 | F06/F12 | PR 원본 ThiefSpawner/RatBase, ItemInteractor, Player prefab·씬 | `PR47Import20261014`: 8파일 범위, 기존GUID 유지, 생성 쥐 기절 전 거부/후 포획 검사. 당시 커밋/푸시 안 함 |
| 509 · 10-14 | GameUI의 휠 인벤토리 재사용, 1초 덫 설치·미리보기 | F09/F10 | HotbarManager/UI, TrapPlacement와 설치 규칙/표시 | `TrapInventory20261014`: 코드 단계. Unity 컴파일 응답 대기로 실제 연결 미완료였던 기록 |
| 531 · 10-14 | MCP 재시도, 회수한 덫을 다시 놓는 데이터 보완 | F06/F10 | TrapWorldItem와 연결 경로 | 같은 외부 폴더. 응답 문제가 이어져 재시작 요청; 성공한 단계로 덮어쓰지 않음 |
| 540 · 10-14 | Unity 재시작 뒤 UI·덫 실제 연결/검증 | F09/F10 | PlaytestScene01 Hotbar/Trap 연결, Player Inventory 에셋 | 동일 로그의 실제 입력/설치·장애물 검증 결과. GameUI 원본 씬을 수정한 것이 아님 |
| 559 · 10-14 | F 유지1초 설치, 첫칸1개 시작, 3치즈 보상 | F06/F09/F10 | PlacementInput, TrapRoundReward, 씬 슬롯0 | `TrapInventoryF20261014`: 순수81·실입력37·보상27. 만석 보상 생략 승인, 마지막Win 처리. URP 줄바꿈만 승인 복원 |
| 578 · 10-14 | 미사용 코드·겹치는 책임 정리 | F05/F06/F07/F10/F11 | 기존11개 C#, PickupReservation, 검증 슬롯선택 범위 | `PlayerSolidCleanup20261014`: 파일 삭제0/meta1074 보존. 입력40/18, 덫37, 보상27, 연출109 등. 미저장 씬 보존 |
| 611 · 10-14 | 라운드 뒤 설치 덫 잔존 제거, 3치즈 기준 명확화 | F10 | TrapPlacement 내부 PlacedTrapLifetime | `TrapRoundCleanup20261014`: 설치101·보상27·연결34. 자기 설치 인스턴스만 종료 시 제거. 미사용 재고/맵 원본 덫은 유지 |

## 요구사항 변경으로 헷갈리기 쉬운 부분

### 손으로 들기와 인벤토리에 저장하기

초기에는 F 토글이나 우클릭 집기를 요청했지만 현재 합의는 역할이 나뉩니다. **좌클릭 유지**는 손 운반이고, **F**는 손 상태를 유지한 채 인벤토리로 새 물건을 보냅니다. 빈손·총·기존 물건 보유 중 어느 상태인지가 F 저장 뒤에도 유지됩니다. “총을 넣고 집기”는 이전 결정이며 최신 F 규칙으로 대체됐습니다. 덫이 선택된 상태의 설치 입력은 별도로 우선순위를 확인합니다.

놓을 때는 바닥으로 순간 이동하거나 바닥 위치를 직접 찾아 고정하지 않습니다. 현재 위치에서 운반을 끝내고 기존 Rigidbody의 중력과 충돌을 복구합니다. 예전 로그의 안전 위치 탐색·공중 복구 대기는 현재 구현 설명으로 그대로 복사하면 안 됩니다.

### 적 포획과 +1 카운트

초기에는 Enemy가 사라지는 것까지만 요청했고 카운트는 보류했습니다. 이후 프로젝트의 기존 `ThiefController.GetCaptured()`가 `GameManager.OnRatCaught()`로 연결되어 있는 것이 확인됐고 PR47의 기절 조건도 포함됐습니다. 현재 ItemInteractor의 오래된 “카운트 미구현” 주석보다 실제 호출 경로가 우선입니다. 다만 팀원 Enemy 전체 동작을 이번 문서에서 새로 테스트한 것은 아닙니다.

### 애니메이션 연결과 본 움직임

Animator 상태 이름이 Walk로 보인다고 몸과 발이 실제로 움직이는 것은 아닙니다. 10-08 사격 후 정지는 상태 이름 중심 검사가 놓친 사례입니다. 그래서 이후에는 몸통 스케일·발과 꼬리 회전의 변화까지 검사했습니다. 점프에서 각 이동 상태로 나가는 화살표는 있어도 들어오는 화살표가 각각 없는 이유는 `Any State → Jump`가 공통 입구이기 때문입니다.

### 메뉴 입력과 카메라 잠금

정지 중 카메라 ±60도 제한은 메뉴를 열었을 때 마우스를 숨기는 규칙이 아닙니다. 메뉴가 열리면 커서를 풀고 게임 입력을 막고, ESC로 닫고 키를 놓은 뒤 잠금을 복귀합니다. `Time.timeScale=0`만으로 마우스 입력이 사라지는 것은 아니므로 공통 권한 처리가 필요합니다. UI에 EventSystem이 빠지면 슬라이더 코드가 맞아도 화면 클릭이 전달되지 않습니다.

### 덫 재고, 설치된 덫, 보상을 구분하기

덫 아이콘은 인벤토리의 보관품이고, 바닥의 설치 덫은 월드 인스턴스입니다. 라운드가 끝날 때 제거하는 것은 **현재 설치기가 성공적으로 놓은 월드 덫**입니다. 시작 재고·미사용 재고·보상 재고·맵에 미리 있던 덫을 일괄 지우는 기능이 아닙니다. 재고 지급과 월드 수명 관리를 다른 책임으로 나눈 이유입니다.

## 최신 미해결 보고: 3치즈인데 덫이 안 들어옴

사용자의 후속 제보는 이전 27개 보상 시험 통과만으로 닫지 않았습니다. 별도 [capture_path.txt](C:/Users/307/Documents/Codex/TrapRewardAudit20261014/capture_path.txt)를 읽으면 다음 실행이 기록되어 있습니다.

1. 시작: 라운드1, 보물5/5, 쥐3, 슬롯0 덫1개·나머지 빈칸, 보상 컴포넌트 활성.
2. 실제 생성된 쥐의 포획 API 경로를 세 번 실행하여 쥐가 3→2→1→0으로 감소했습니다.
3. `RoundClear`에서는 슬롯0 한 개가 유지됐고, 약1.5초 후 `RoundResult`에 들어갔습니다.
4. 결과 이벤트에서 슬롯1이 `TrapItem`으로 바뀌었고 실제 UI의 슬롯0·1 모두 `TrapIcon`이 활성 상태였습니다. 치즈 아이콘3개도 활성 상태로 기록됐습니다.

이 시험에서는 정상 지급이 확인됐지만 **사용자가 겪은 미지급 상황은 재현하지 못했습니다.** 자동 AI가 물건을 훔치는 자연 게임 전체를 완료한 시험이나 사용자의 실제 F 키 입력 재현은 아닙니다. 원인이 만석, 다른 인벤토리 연결, 비활성 보상 컴포넌트, UI 표시 지연 중 무엇이라고 확정하지 않습니다. 원인 수정 완료로 기록하지도 않습니다.

현재 코드의 보상 규칙은 `RoundResult.IsPerfect`, 즉 **보물 손실0**입니다. 검사 당시 라운드 시작 보물이5개이므로 5개 모두 지키고 클리어해야3치즈입니다. 치즈 물체3개 획득이나3라운드 진입이 아닙니다. 일반 라운드는 결과 표시 시점, 마지막 라운드는 `Win` 전환 경로에서 한 번만 빈칸에1개를 추가합니다. 가득 차면 지급하지 않고, 나중에 빈칸을 만들어도 소급 지급하지 않는 것은 사용자 승인 규칙입니다.

## 최신 Git 작업: PR #49

PR47을 작업 파일로 가져왔던 이력과 PR49 충돌 해결은 서로 다른 작업입니다. PR49의 로컬 병합 커밋은 `6b8bafde9f16129d6c6a9a7c7127aaca5582a63a`입니다. 기존 Player 파일2,049개/meta1,074개는 같은 내용을 보존했고, main의 새 덫·맵 프리팹과 각 meta 총4파일만 추가했습니다. 기존 씬과 런타임 코드가 Player 원본과 동일한지 Git blob으로 비교했습니다.

근거는 [충돌 해결 검토안](C:/Users/307/Documents/Codex/PR49Conflict20261014/CONFLICT_RESOLUTION_REVIEW.md)과 [validation_final.json](C:/Users/307/Documents/Codex/PR49Conflict20261014/validation_final.json)입니다. 후보 검사36개 통과는 문법·참조·보존 범위 검사이며 새 프리팹 실제 게임 배치의 완료 판정이 아닙니다. 원격 푸시는 **403으로 실패하여 미완료**입니다. 로컬에 커밋이 있다는 이유로 GitHub PR 충돌도 해결됐다고 말하면 안 됩니다.

## 후속 담당자가 근거를 찾는 방법

1. 기능 번호로 현재 파일을 찾고 실제 Inspector 연결과 소스를 함께 확인합니다. 이름이 비슷한 사본만 보고 수정 대상을 정하지 않습니다.
2. 아래 로그 헤딩 색인의 줄 번호로 요청·수정·실패·재검증의 원문을 읽습니다. 과거 기록은 지우지 않습니다.
3. 시험 기록의 날짜, 파일 해시 기준, 입력 방식, 검사 대상 씬, 실제 검사한 수치를 확인합니다.
4. 고친 기능과 관련된 실패 사례를 다시 실행합니다. 과거 PASS를 새로운 코드의 PASS로 재사용하지 않습니다.
5. 종료 후 변경 파일과 `.meta`/GUID를 기준 사본과 비교합니다. 새 코드·에셋은 새 meta가 필요할 수 있지만 기존 에셋 GUID 재발급과 구분합니다.

### 시험 결과를 과장하지 않기 위한 실제 사례

| 기록 | 확인한 것 | 확인하지 못한 것 |
|---|---|---|
| 10-12 최초 UI 21조건 | 메뉴 상태·커서·콜백 경로 | 화면 클릭을 전달할 EventSystem 부재. 후속 실제 입력19조건으로 보완 |
| 10-08 초기 Animator 검사 | 상태/사격 필드/손 연결 | 사격 후 실제 본 정지. 후속 본변화 판정으로 보완 |
| 옛맵 이동19개 | 검사기가 요구하는 특정 이름·높이의 지형 | 현재 맵에 다른 높이가 있으면 실행 불가. 별도7개 검사로 현재턱만 확인 |
| 덫 보상27개 | 공개 GameManager 이벤트 경로의 지급 규칙 | 자연 AI 플레이 모든 흐름과 사용자 미지급 상황 |
| PR49 후보36개 | YAML·참조·파일보존·컴파일 | 새 덫·맵 프리팹의 실제 게임 사용, 원격 푸시 성공 |

## 작업 로그 모든 제목의 색인

아래 색인은 기존 로그의 `#`, `##`, `###` 제목을 모두 읽어 생성합니다. 제목을 정리하면서 실패 중간 기록이나 하위 검증 기록이 빠지는 것을 막기 위한 표입니다. 줄 번호는 이 문서 작성 시점 기준이며, 이후 원문 앞부분이 추가되면 달라질 수 있습니다. 같은 이름의 “검증” 제목도 소속 작업과 함께 구분합니다.

| 원문 행 | 원문 제목 | 소속 기능 |
|---|---|---|
| [작업 로그](../PLAYER_WORK_LOG.md) 1행 | 플레이어 작업 로그 | 전체 |
| [작업 로그](../PLAYER_WORK_LOG.md) 5행 | 2026-09-30 — 우클릭 집기와 기존 샷건 장착 (완료) | F05/F08/F11/F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 15행 | 구현 및 연결 | F05/F08/F11/F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 25행 | 첫 검증과 수정할 검사 조건 | F05/F08/F11/F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 34행 | 재검증 준비와 외형 확인 | F05/F08/F11/F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 42행 | 최종 기능 검증 | F05/F08/F11/F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 50행 | 최종 파일 범위와 정리 | F05/F08/F11/F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 59행 | 2026-09-30 — 무마찰·누르는 동안 들기·카메라 제한·의존성 분리 (완료) | F01/F02/F03/F05/F08 |
| [작업 로그](../PLAYER_WORK_LOG.md) 72행 | 중간 구현 및 검증 | F01/F02/F03/F05/F08 |
| [작업 로그](../PLAYER_WORK_LOG.md) 89행 | 최종 상호작용 검증 | F01/F02/F03/F05/F08 |
| [작업 로그](../PLAYER_WORK_LOG.md) 96행 | 최종 이동·범위 검사 및 인수인계 | F01/F02/F03/F05/F08 |
| [작업 로그](../PLAYER_WORK_LOG.md) 108행 | 2026-10-06 — manifest.json 중복 키 오류 수정 | F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 118행 | 2026-10-06 — 패키지 오류 정상화와 Unity 실행 검증 | F03/F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 135행 | 2026-10-06 — 샷건 줌·걷기 제한·4발 산탄·발사 자세 | F01/F03/F04/F08 |
| [작업 로그](../PLAYER_WORK_LOG.md) 160행 | 2026-10-06 — GitHub Desktop 표시와 커밋 전 경고 확인 | F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 170행 | 2026-10-06 — OneDrive 밖 작업 경로로 옮기는 방법 안내 | F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 181행 | 2026-10-06 — 승인된 OneDrive 밖 복사·연결·검증 완료 | F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 202행 | 2026-10-06 — 사용자 복구본 위 Player 기능 재연결, GUID 보존 | F08/F11/F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 226행 | 2026-10-07 — 좌클릭 집기와 Player 스크립트 이름·책임 정리 | F01/F05/F07/F11/F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 252행 | 2026-10-07 — 모델 보존, 측면 검은자위 렌더 순서 수정 | F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 268행 | 2026-10-07 — 기존 ItemInteractor의 F 획득·5칸 보관 확장 | F06/F09 |
| [작업 로그](../PLAYER_WORK_LOG.md) 291행 | 2026-10-07 — F 저장 시 손 상태 유지, 해제 위치에서 기본 중력 낙하 | F05/F06/F09 |
| [작업 로그](../PLAYER_WORK_LOG.md) 314행 | 2026-10-08 — 손·발 외곽선 저장, 점프 승인용 초안 | F04/F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 329행 | 2026-10-08 — 점프 미리보기 색상 오류 교정 | F13 |
| [작업 로그](../PLAYER_WORK_LOG.md) 338행 | 2026-10-08 — 승인된 점프를 Space와 실제 물리에 적용 | F01/F02/F04 |
| [작업 로그](../PLAYER_WORK_LOG.md) 359행 | 2026-10-08 — Animator/Animation 기본 기능으로 재생 연결 정리 | F04/F08 |
| [작업 로그](../PLAYER_WORK_LOG.md) 379행 | 2026-10-08 — 샷건 후 본 정지 교정, 점프 연결 진단, 점프 버퍼링 | F01/F04/F08 |
| [작업 로그](../PLAYER_WORK_LOG.md) 401행 | 2026-10-08 점프 진입·복귀 연결 개선 | F04 |
| [작업 로그](../PLAYER_WORK_LOG.md) 421행 | 2026-10-08 저장·커밋과 카메라 병합 충돌 정리 | F03/F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 431행 | 2026-10-12 설정창 커서 충돌 수정과 승인된 SOLID 정리 | F01/F03/F06/F09 |
| [작업 로그](../PLAYER_WORK_LOG.md) 449행 | 2026-10-12 ESC 설정창 조작 불가·복귀 잠금 후속 교정 | F01/F03/F09 |
| [작업 로그](../PLAYER_WORK_LOG.md) 466행 | 2026-10-12 RAt (1) F 상호작용 불가 수정 | F06/F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 480행 | 2026-10-14 — 왼손 외곽선 연결 복구 | F13/F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 494행 | 2026-10-14 — PR #47 변경만 미커밋 상태로 반영 | F06/F12 |
| [작업 로그](../PLAYER_WORK_LOG.md) 509행 | 2026-10-14 인벤토리·덫 설치 — 코드 단계 / Unity 연결 대기 | F09/F10 |
| [작업 로그](../PLAYER_WORK_LOG.md) 531행 | 2026-10-14 MCP 확인 후 재시도와 덫 반복 획득 보완 | F06/F10 |
| [작업 로그](../PLAYER_WORK_LOG.md) 540행 | 2026-10-14 Unity 재시작 후 인벤토리·덫 적용과 검증 완료 | F09/F10 |
| [작업 로그](../PLAYER_WORK_LOG.md) 559행 | 2026-10-14 F 유지 설치·시작 덫·3치즈 보상 적용 | F06/F09/F10 |
| [작업 로그](../PLAYER_WORK_LOG.md) 578행 | 2026-10-14 미사용 코드·중복 책임 정리와 실제 입력 재검증 | F05/F06/F07/F10/F11 |
| [작업 로그](../PLAYER_WORK_LOG.md) 611행 | 2026-10-14 라운드 종료 시 설치 덫 제거 및 3치즈 조건 설명 | F10 |

색인 검증: 원문 제목 40개와 표 항목 40개가 일치합니다. 상세 날짜별 변경표는 `##` 작업 31개를 모두 포함합니다.


## 이번 기술문서 작성 작업

위 31개 작업·40개 제목은 문서를 만들기 직전 로그의 고정된 색인입니다. 이번 문서 작성은 그 뒤의 별도 작업으로 [작업 로그](../PLAYER_WORK_LOG.md)의 “2026-10-14 구현 기술문서 작성”에 추가했습니다.

- 대상: F01~F13, Player C# 53개와 직접 연계 Systems C# 10개.
- 결과: 전체 구조, 기능별 코드 풀이·수정 방법, Editor 도구, 에셋/GUID 주의사항, 이력·소스 색인, SOLID 검토, 검증 근거와 검색 가능한 HTML.
- 검증: [문서 검사 보고서](evidence/document_validation.json)에서 링크·소스 해시·이력 포함·기존 파일 보존 결과를 확인합니다. 게임 Play를 새로 실행한 작업은 아닙니다.
- 변경 범위: 새 PlayerDocumentation 폴더와 기존 작업 로그·구현 설명서의 끝에 추가한 안내뿐입니다. 게임 코드·씬·에셋·메타는 보존했습니다.
