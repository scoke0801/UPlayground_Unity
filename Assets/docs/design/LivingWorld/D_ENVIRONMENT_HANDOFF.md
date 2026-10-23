# D 환경 구현 인계

> 2026-10-22 / D 독립 구현·에셋 생성·EditMode 검사 수행. I 통합 및 사용자 Play Mode 확인 대기.
> 기준: [A 6.4/8절](A_BASELINE.md). 본편 씬·공용 Data/Contracts·등록·외부 원본은 수정하지 않았다.

## 1. 완료 범위와 남은 범위

맵별 고정 시간표, 같은 저장 시각의 날씨·건조 중간값 재구성, TGV/수면/입자 공통 바람, 지역 먼지, 비와 젖음에 따른 먼지 감쇠, 처마 보호 볼륨 조회·입자 제거, 지붕 충돌, 대화 자동 감쇠와 조사/전투 집중 입력, 조명 단일 작성자, 씬 종료 전역값 복구를 구현했다. 기존 셰이더 두 개를 보완하고 Built-in 먼지 재질을 대체할 작은 URP 파티클 HLSL만 추가했다. ShaderGraph는 추가하지 않았다.

**남음:** 실제 본편 배치·지붕/바닥 충돌층·관측 카메라·조사/전투 집중 연결, NPC 공용 환경 계약 게시/등록, 환경음 클립 및 Mixer 연결, 본편 가독성·실내 관통·프레임 측정. 환경음은 두 전용 AudioSource와 강수/풍속/실내/집중 감쇠 경로까지 만들었으나 바람·빗소리 루프를 확보하지 못해 clip이 비어 있다. 재생 완료로 보고하지 않는다.

공용 Data 타입은 A에서 I 소유이고 아직 게시되지 않았다. D는 `EnvironmentSchedule`을 **Assembly-CSharp의 독립 평가기**로 두고 D 소유 JSON TextAsset을 입력받는다. 공용 SO/서비스를 대신 등록하거나 Actor가 이 타입을 참조하게 하지 않았다. 프로필을 SO로 표준화할 때는 I가 Data 타입과 변환을 먼저 게시해야 한다.

## 2. 변경 파일과 인계 자산

기존 수정: `Assets/02.Scripts/Gameplay/World/WorldLightingController.cs`, `WorldLightingSceneBinding.cs`, `Assets/02.Scripts/Manager/World/WorldLightingManager.cs`, `Assets/06.Shaders/LakeWater.shader`, `CreekGround.shader`.

신규 코드: `Assets/02.Scripts/Gameplay/World/LivingWorld/`의 `EnvironmentSchedule.cs`, `EnvironmentShaderState.cs`, `EnvironmentController.cs`, `EnvironmentEffects.cs` 및 meta. 신규 셰이더: `Assets/06.Shaders/LivingWorld/WeatherParticle.shader` 및 meta.

| 자산 경로 | GUID |
|---|---|
| `Assets/10.Datas/World/LivingWorld/Environment/DroughtVillage_Weather_v1.json` | `e08b9fa10b19e2a458c867edf2127313` |
| `Assets/10.Datas/World/LivingWorld/Environment/LakeOfLife_Weather_v1.json` | `3338348dea1120b479d78ad90342fca5` |
| `Assets/03.Prefabs/LivingWorld/Environment/DroughtVillageEnvironment.prefab` | `3c599809197c5d6498e4148618968fec` |
| `Assets/03.Prefabs/LivingWorld/Environment/LakeOfLifeEnvironment.prefab` | `f5085c769525219469127f324eae57f8` |
| `Assets/03.Prefabs/LivingWorld/Environment/EnvironmentEffects.prefab` | `a6ba0112d3d31bf4597cd3c07c2af160` |
| `Assets/11.Material/LivingWorld/Environment/CreekGround_Outdoor.mat` | `7466a3ddc91f2a14b98f8e3761d031ed` |
| `Assets/11.Material/LivingWorld/Environment/CreekGround_Sheltered.mat` | `6f293fc28278b4745bd9e4bc604cf5f4` |
| `Assets/11.Material/LivingWorld/Environment/LakeWater_Weather.mat` | `0a31ddb04a74e8b4c92e2106bf14e5e2` |
| `Assets/11.Material/LivingWorld/Environment/Rain.mat` | `eb4403052e4480941b3215897795ce9d` |
| `Assets/11.Material/LivingWorld/Environment/DroughtDust.mat` | `8e2a7dbf7a471514196e1945f530d264` |

신규 Map ID, Addressables 주소, 저장 필드는 추가하지 않았다. 프리팹은 씬 직렬화 참조로 통합하면 된다. **기존 `.gitignore:556`이 새 `Assets/03.Prefabs/*`를 제외한다.** 파일은 디스크에 있으며 커밋/스테이징은 하지 않았다. I가 전달/커밋할 때 위 프리팹 3개와 해당 meta/폴더 meta를 명시적으로 포함해야 한다. `.gitignore`는 D 소유가 아니므로 수정하지 않았다. 폴더 생성/임포트로 생긴 공용 `LivingWorld` 부모 meta는 I가 한 번 인수하고 재발급하지 않는다.

## 3. 공개 입력과 상태·저장 경계

실제 공개 API는 D의 `EnvironmentController`에 있다. `BindObserver(Transform)`, `SetFocus(bool)`, `EvaluateAt(int day, float minuteOfDay)`, `IsSheltered(Vector3)`; 읽기 값은 `MapId`, `ProfileVersion`, `IsReady`, `Revision`, `Current`, `OnStateChanged(Action<EnvironmentController>)`다. `Current`는 `PresetId/PhaseMinute/Wind(Vector2 XZ)/Rain/Wetness/Cloud`를 포함한다. 준비·해제와 게임 분 변경에 revision/통지를 갱신하고 매 프레임 보간된 Current를 읽을 수 있다. 미준비이면 NPC는 새 피신을 시작하지 않는다.

시간 입력은 실제 `IGameTimeService.CurrentDay/MinuteOfDay`를 사용한다. `day*1440 + minute`의 modulo를 double로 계산한다. 시각 서비스가 없으면 미준비·효과 중단이며 새 게임 시각이나 난수를 임의 생성하지 않는다. 독립 `EvaluateAt` 검사는 실제 서비스 연결 Play Mode와 구분한다. 24시간 반복 v1에는 seed·별도 저장·GlobalFlag 쓰기가 없다. `version`을 올려 기존 v1을 덮어쓰는 것이 저장 호환을 보장하지는 않는다. 출시 후에는 v1을 동결하고 I의 프로필 선택/이관 계약을 먼저 만든다.

가뭄 v1은 00:00 맑음→08:00 흐림→14:00 건조한 바람→19:00 맑음이고 rain/wetness는 전체 0이다. 호수 v1은 09:00~10:00 비로 보간, 10:00~11:30 강수 0.4, 12:00 강수 0/젖음 0.8, 15:00 젖음 0.4, 18:00 완전 건조다. 키 사이를 SmoothStep으로 보간한다. 프리셋 이름보다 연속 강수량이 피신 판단의 입력이다.

`_Recession`, 수면 높이, 물길 활성화, vertex R/A, `absorption.stopped`, `return.water_checked`를 작성하지 않는다. 회복 후에도 가뭄 v1을 유지하는 것이 기본이다. 회복했다고 자동으로 비 프로필로 바꾸지 않는다. 실제 비와 공급/흡수 중단 사실은 독립이다.

**I에게 요청할 최소 공용 변경안 (미구현):** `Data/World/LivingWorld/EnvironmentState`에 mapId/isReady/revision/profileVersion/presetId/phaseMinute/wind/rain/wetness/cloud를 두고, `Contracts/IEnvironmentService : IGameService`에 `Current`, `event Action<EnvironmentState> OnStateChanged`, `bool IsSheltered(Vector3 position)`를 게시한다. Manager 계층 어댑터가 D 값을 DTO로 복사·등록하고 씬 종료 시 미준비 통지 후 해제한다. C는 Actor→Assembly-CSharp 참조를 추가하지 말고 이 계약을 소비한다. mapId와 생산자 인스턴스가 바뀐 뒤 늦은 이벤트는 무시한다. 어댑터의 중복 등록/씬 전환/로드와 C의 피신 접근성 검사가 필요하다.

## 4. I의 프리팹·필드 연결

1. 각 맵에 대응 Environment 프리팹 **하나만** 배치한다. `_mapId/_profile/_effects/_rainAnchor/_windNoise`는 연결돼 있다. 호수 프리팹의 DroughtDust는 비활성이다. 피해 마을의 Dust Transform은 우물/밭 바닥에 맞추고 단서·얼굴·생활 통로에서 떨어뜨린다. 원점에 그대로 두지 않는다. 비만 관측점을 따라가며 먼지는 지역에 고정된다.
2. `WorldLightingSceneBinding.environment`에 그 생산자를 연결한다. 기존 sun/moon/volume 바인딩을 유지한다. 해·달·노출을 쓰는 새 Update를 추가하지 않는다. 흐림은 최종 태양/달 강도 최대 25% 감쇠, 노출 -0.1이며 얼굴 보조광·주변광은 유지한다. 중복 컨트롤러는 오류로 거부하고 기존 컨트롤러를 먼저 Disable하여 복원한 뒤 다음 것을 만든다.
3. 씬의 `SICS.WindSettings`를 비활성화한다. 활성 상태이면 D가 오류를 내고 준비되지 않는다. D가 TGV의 Direction/Strength/Speed/Scale/TreeJitter와 Noise를 작성하고 종료 시 이전 값을 복원한다. `_LW_Wind/_LW_Wetness`는 종료 시 0. 지연 해제는 새 소유자 값을 건드리지 않는다. 외부 원본 WindManager.cs는 수정하지 않았다.
4. `BindObserver`에 실제 카메라/AudioListener Transform을 연결하고 카메라 교체 시 다시 호출한다. 미연결이면 비/환경음은 꺼진다. 강수 Anchor 높이 기본 6m, 방출 Box 10×0.2×10m다. 먼지는 Box 5×0.1×5m. 플레이어/카메라 주변 단서와 충돌층에 맞춰 I가 조정한다.
5. `EnvironmentEffects.BindShelters(BoxCollider[], LayerMask)` 또는 직렬화 필드를 연결한다. `_shelters`는 회전·스케일을 포함한 처마/실내 보호 박스이며 Trigger 이벤트 없이 좌표로 검사한다. 통행을 막지 않도록 trigger 또는 비활성 collider 표시용으로 사용한다. `_roofLayers`는 **지붕과 바닥의 실제 충돌 레이어**다. 0이면 비 방출을 막는다. 큰 집은 처마 안쪽 공간과 문턱까지 박스를 나누고, 비 방출 Box가 지붕 위/아래에 걸치는 경우도 검사한다. 비는 world collision lifetimeLoss=1, 보호 볼륨 안 입자는 LateUpdate 제거. 관측점이 실내면 근방 비를 즉시 Clear하므로 창밖 비의 연속 표현은 아직 제한된다.
6. 조사 카메라/전투 시작·종료에 `SetFocus(true/false)`를 연결한다. 기존 `IDialogueService.IsDialogueActive`는 자동 소비한다. 중첩된 조사·전투는 I 어댑터가 집계해 마지막 종료 때 false로 보낸다. 집중 시 입자 25%, 환경음 25%; 실내 환경음 15%. 인물 얼굴 가독성을 수치만으로 통과 처리하지 않는다.
7. `RainAudio/WindAudio`에 별도 루프와 기존 환경음 Mixer 그룹을 연결한다. clip은 현재 비어 있다. sound 설정 음량/음소거가 전달되도록 Mixer를 통해 연결한다. 최대 환경음 볼륨 0.15, 페이드 기본 0.4초. 외부 팩의 Ambient/Forest라는 이름의 **음악**을 날씨 루프로 대체하지 않았다.
8. 기존 RT 재질 교체는 I가 판단한다. 실외 개울은 Outdoor, 실내는 Sheltered, 수면은 LakeWater_Weather 변형을 연결한다. 기존 재질의 새 프로퍼티 기본값은 0이라 미연결 외형은 유지된다. MPB를 쓰거나 지우지 않아 서사 `_Recession` 등 기존 MPB가 보존된다. 한 메시가 실내·외에 걸치면 메시/재질 슬롯을 나눈다. 공통 global 젖음이 모든 실내를 자동 판정하지는 않는다.
9. URP Depth Texture가 필요하다. 파티클 근접 1~3m 페이드와 교차 0.5m 페이드가 대화 카메라의 입자 가림을 줄인다. 빗방울 물튀김 프리팹은 아직 채택하지 않았다. 땅 높이·실내에서 잘못 재생되는 별도 스플래시를 추가하지 않는다.

프리팹 초기 상한은 rain 256 + dust 64 입자다. 풀을 매 프레임 생성하지 않고 기존 ParticleSystem을 재생/정지하며, 필터 버퍼는 초기화 때만 할당한다. maxParticles를 런타임에 늘리면 버퍼도 다시 준비해야 하므로 현재는 프리팹 저작 값으로 고정한다.

## 5. 조사·검사와 재현 절차

조사: LakeWater의 월드 XZ 노멀·WaveVelocity와 Recession, CreekGround의 vertex R/A, WorldLighting의 runtime Volume 사본/그림자 소유를 확인했다. `Idyllic Fantasy Nature/.../WindControl.prefab`에는 Transform만 있으며 독립 풍향 제어기로 채택하지 않았다. TGV WindSettings의 여섯 전역 입력을 재사용했다. CFXR Rain Falling의 외부 동작 스크립트(자체 소멸/밀도 제어)는 D 복제본에서 제거했다. Dust ground는 Built-in `Particles/Standard Unlit`이어서 텍스처만 재사용했다. 외부 원본은 저장하지 않았다.

| 검사 | 결과·한계 |
|---|---|
| Unity C# 컴파일 | D 타입 로드 확인. 최초 새 파일 임포트 전 타입 누락은 강제 Refresh 후 해소. 전 프로젝트의 기존 경고/다른 작업 오류까지 0이라고 주장하지 않음 |
| EditMode | `EnvironmentScheduleTests` 8개 + `EnvironmentShaderStateTests` 1개, **9/9 통과**, 실패/제외 0. 건조 기본표 2일 전 구간·강수 뒤 젖음·재입장/되감기·큰 시각·잘못된 입력·중복/지연 해제 검사 |
| 셰이더 임포트 | LakeWater / CreekGround / WeatherParticle 각각 ShaderHasError=false, 메시지 0. Player variant와 실화면 검증 아님 |
| 프리팹 검사 | 실제 AssetDatabase로 프리팹 3개 Missing Script 0, 프로필/재질/외부 텍스처 참조 확인 |
| 독립 PreviewScene API 검사 | **6묶음 통과**: 회전/스케일 보호 볼륨 안팎, 지붕층 미연결 방출 차단·실외 방출·실내 Clear, 강수 먼지 억제·실내 입자 제거·종료, 빌린 Volume/태양/주변광 복원, 소유 Volume 사본과 컴포넌트 파괴/원본 보존, 실행 중 교체된 타 소유 Volume 보존. 실제 지붕 충돌 시뮬레이션·화면·오디오 청취는 이 검사에 포함되지 않음 |
| Play Mode / 본편 가독성·실내 관통 | **사용자 테스트 대기**. 본편 씬을 열어 저장하거나 Play Mode 진입하지 않음 |
| Windows Player | **미수행**. I/V가 두 맵·Depth Texture·셰이더 포함/스트리핑 최종 연결 후 검사 |
| 성능 | **미측정**. CPU/GPU ms, GC/frame, draw/SetPass, 투명 오버드로, 메모리/VRAM, 오디오 비용, 로드 시간 모두 대기 |

검사 파일은 `Assets/Tests/EditMode/LivingWorld/Environment/`에 있다. 기존 Core.Tests에서 reflection으로 Assembly-CSharp 구현을 호출하며 공용 asmdef를 변경하지 않는다. 실행 필터는 `UPlayGround.Core.Tests.EnvironmentScheduleTests`, `UPlayGround.Core.Tests.EnvironmentShaderStateTests`. Unity 6000.3.21f1에서 수행했다. 결과 요약은 `output/LivingWorldD/validation.json`, 독립 API 검사 재현 코드는 `output/LivingWorldD/preview_checks.cs.txt`, 프리팹 생성 기록은 `output/LivingWorldD/create_assets.cs.txt` (로컬 보조 산출물, git ignored)다. PreviewScene은 검사 후 닫았으며 새 에디터 메뉴/런처 도구를 추가하지 않았다.

### LW-T06 — 재입장·처마·서사 물길

준비: I 연결 버전·프로필 v1 GUID·Unity 버전·장치/품질을 기록한다. 기존 저장을 복사해 R04 회복 전/회복 후 및 호수 강수 중/건조 중 슬롯을 준비한다. 실제 진행 저장과 시각 주입 실험을 구별한다.

1. 가뭄 마을 08:00/14:00/19:00을 관찰한다. 맑음·흐림·건조 바람만 있고 비·날씨 젖음은 0이어야 한다. 시간을 멈춘 독립 검증에서 같은 시각으로 10회 왕복/로드하면 동일한 Current가 복원돼야 한다. 정상 게임에서는 이동 동안 경과한 시각만큼만 변해야 한다.
2. 호수 10:30 비, 13:30 건조 중으로 각각 저장·재입장한다. 13:30에는 rain=0, wetness는 0.4~0.8 사이여야 한다. 입장 때 비가 재추첨되거나 즉시 마르면 실패다.
3. 비/강풍에서 처마 밖→문턱→실내→밖을 걷고 카메라를 급회전한다. 지붕 아래·카메라 앞·실내 바닥의 비와 먼지, 보호 볼륨 경계 누락을 확인한다. 지붕 없는 곳의 비가 모두 사라지는 잘못된 mask도 실패다. 이동·카메라를 가리지 않는지 함께 본다.
4. 같은 날씨에서 회복 전후 `_Recession`, vertex R/A, 수면 높이, `absorption.stopped/return.water_checked`를 비교한다. 날씨만으로 사건/수위가 바뀌면 실패다. 기존 MPB의 `_Recession`을 넣은 상태에서도 날씨 갱신으로 지워지면 실패다.
5. 맵을 떠나 Loading/메뉴로 이동하고 프로필 객체를 해제한다. `_LW_Wind/_LW_Wetness`=0, TGV 원값 복원, 효과·소리 중단, Volume 사본·보조광 중복 없음, 환경 미준비 통지가 기대값이다. 다음 맵을 연 직후 옛 객체의 지연 해제가 새 값을 지우면 실패다.

### LW-T07 — 얼굴·단서·전투 신호

준비: 같은 카메라/진행 상태에서 환경 off와 on을 비교한다. 실제 게임패드 또는 키보드/마우스를 기록한다.

1. 비/건조 바람 최고 구간에 NPC 정면·측면 대화, 우물/뿌리 증거 조사 카메라, 개울 전투를 각각 재생한다. 얼굴·손·조사 대상·공격 예고·피격 피드백을 가리는 파티클/반짝임이 없어야 한다.
2. 대화 자동 감쇠, 조사/전투 SetFocus, 중첩 종료 뒤 복귀를 확인한다. 대화 종료로 전투의 집중 감쇠까지 풀리면 I 집계 실패다. 밤에도 태양/달 중 그림자 작성자는 하나이며 얼굴 보조광이 유지돼야 한다.
3. 루프/Mixer 연결 후 대사·공격 경고·발소리와 비교 청취한다. 환경음이 핵심 신호를 덮거나 음소거 설정을 우회하면 실패다. 현 clip 공란 상태는 청취 통과가 아니다.

### LW-T09 — 비용과 누적

준비: 목표 기기·해상도·URP 품질/Depth Texture, 현재 버전, 두 맵 저장/카메라/시각을 고정한다. 실험 상한 후보 CPU/GPU 추가 각각 2ms, 총 16.7ms는 **승인된 예산이나 달성값이 아니다**.

1. 식사/돌봄/운송, 피해 우물/개울, 신사 구도에서 환경 off / 건조 바람 / 비를 각각 낮·밤, 일과 0/6명 조합으로 비교한다. 로딩·셰이더 워밍업을 제외하고 안정화 후 60초씩 측정한다.
2. CPU/GPU median·p95, GC Alloc/frame, 최대 메모리/VRAM, draw/SetPass, 입자 수/오버드로와 Particle Collision 비용을 기록한다. 보호 박스 수에 따른 필터 순회 비용도 비교한다. 폴링/버퍼 할당이 없다는 코드 검사는 GC 측정의 대체가 아니다.
3. 3~5분 유지 및 두 맵 10회 왕복 후 runtime Material/Volume/AudioSource/ParticleSystem/보조광 개수가 누적 증가하지 않아야 한다. 낮은 품질은 장식 밀도를 먼저 줄이고 증거·NPC 접근·날씨 상태 구분을 유지한다.

회신 형식: `LW-T06/T07/T09-단계 / 통과·실패·미수행 / 버전·시작 슬롯·시각·입력 장치 / 실제 결과 / 화면·로그·Profiler`. 결과 전에는 위 세 항목을 통과 처리하지 않는다.

## 6. 다른 담당 요청과 사용자 변경 보존

I: 위 공용 DTO/서비스·씬 바인딩·집중 집계·Mixer/Depth Texture·ignore된 프리팹 포함을 처리한다. C: 공용 계약 게시 후 같은 환경 상태와 보호 구역을 소비하며 별도 날씨를 만들지 않는다. E: 아래 텍스처 규격은 선전달됐으며 신규 제작은 현재 필수 아님. 추가 자산 담당에게 바람/빗소리 루프(음악·말소리 없음, 루프 경계 청취 확인)를 요청한다.

현재 작업 중 B/C의 다른 파일 변경이 함께 나타났다. D는 해당 파일을 수정·원복하지 않았다. 본편 씬 디스크 SHA-256은 A와 같은 `23F089199D37FB8FCD136574D0BAEE2A2A58CAD8660D8F0B222E4799B2D6FFD5`로 확인했다. 원본 저장·씬·외부 팩·공용 등록을 저장하지 않았고 커밋하지 않았다. AssetDatabase.SaveAssets 전체 저장은 사용하지 않고 D의 새 에셋만 생성했다.

## 7. E에 먼저 전달한 텍스처 계약

현재 신규 텍스처 제작은 필수가 아니다. 기존 Soil Albedo / Wave Normal / FX 텍스처를 우선 재사용한다. 실제 근접 화면에서 부족을 확인한 경우에만 아래 후보를 제작한다.

| 후보 경로 (E 소유) | 용도·UV | 채널·색공간·임포트 | D 재질 프로퍼티 |
|---|---|---|---|
| `Assets/04.Images/LivingWorld/Environment/DroughtSoil_BaseColor.png` | 마른 개울 바닥, 1024² 반복 무늬. 메시 UV0 × 기존 `_BaseMap_ST`. 사진에 조명·그림자·물웅덩이를 굽지 않음 | RGB 색상, sRGB ON, A=1, Repeat, mipmaps ON | Creek Ground `_BaseMap`. 젖음은 텍스처 R이 아니라 **메시 vertex R**; vertex A는 둑 가장자리 |
| `Assets/04.Images/LivingWorld/Environment/LakeWave_Normal.png` | 기존 노멀로 충분하지 않을 때만 512² 타일. 월드 XZ × `_WaveScale`, UV seam 없음 | tangent normal RGB, Normal Map import, sRGB OFF, Repeat, mipmaps ON. 실제 Unity 방향 검수 | Lake Water `_NormalMap`; `_WaveVelocity`, `_WaveStrength` 유지 |

날씨 젖음은 `_LW_Wetness` 전역값 × 재질 `_WeatherExposure`로 기존 vertex R과 합성한다. 실내 재질은 `_WeatherExposure=0`; 실내·외가 한 메시라면 I가 메시/재질 영역을 나눈다. 기존 R/A를 새 마스크로 재해석하지 않는다. 외부 팩 재질을 직접 수정하지 않으며 D의 `Assets/11.Material/LivingWorld/Environment/` 변형에 연결한다. 생성 이미지를 검증된 PBR 세트로 취급하지 않는다.
