# V — 생활 확장 통합 검증

2026-10-23 / 대상 **LW-I1-V1** / **검사 결과·실행 명세 기록, 통합 승인 보류**.

기준은 [테스트 지침](../../guide/TEST_VALIDATION_GUIDE.md), [기반 기획의 LW-T01~10](../LAKE_OF_LIFE_LIVING_WORLD_PLAN.md), [V 작업](../LAKE_OF_LIFE_LIVING_WORLD_TASKS.md), [I 통합 기록](I_INTEGRATION.md)이다. 이 문서의 사용자 실행 항목은 결과를 받기 전까지 `사용자 테스트 대기`다. 코드 계약·상태 주입·과거 단일 씬 테스트의 성공은 실제 새 맵 완주를 증명하지 않는다.

## 1. 통합 버전과 변경 범위 고정

- 저장소 HEAD: `19e38ad2492d97371be22c6e6eaa1213b9b13fec`.
- HEAD만으로 재현할 수 없다. 시작 시 `LakeOfLifeScenario.unity`, `RunnerSceneCondition.cs`가 수정 상태이며 `I_INTEGRATION.md`와 `.meta`는 미추적이었다. [시작 상태](../../../../output/LivingWorld/V/InitialStatus.txt), [시작 diff 요약](../../../../output/LivingWorld/V/InitialDiffStat.txt)을 함께 사용한다.
- Unity `6000.3.21f1 (c02631ffc030)`, URP. 연결 Editor의 `Application.dataPath`가 이 저장소임을 확인했다. 시작 시 호수 씬 하나, `isDirty=false`, Play Mode/컴파일/임포트 진행 없음.
- [통합 파일 SHA-256 명세](../../../../output/LivingWorld/V/IntegrationManifest.csv): Scripts, Tests, Scenario 씬, Data, LivingWorld 프리팹/재질, 셰이더, Addressables, ProjectSettings, Packages, 인계 문서를 포함한다. [저장소 추적 파일 명세](../../../../output/LivingWorld/V/BaselineManifest.csv)는 보조 근거다. 최초 Git 경로 인용 때문에 빠진 한글 경로는 `core.quotepath=false`로 보충했다.
- V의 소유 범위는 이 문서와 `.meta`, `output/LivingWorld/V/`의 검사 근거다. 런타임·본편 씬·저장 파일을 수정하거나 커밋하지 않는다. 결함 수정은 아래 소유자 인계 후 새 해시를 붙여 검증한다.

| 핵심 파일 | V 시작 SHA-256 | I1과 비교 |
|---|---|---|
| `Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity` | `002733D4A265D7B91D9C23B4B764FF9F764927F254DEABCC7B4F3E9F82D15E21` | 일치 |
| `Assets/01.Scenes/Scenario/DroughtVillage.unity` | `E49099CA1A39EB5CC5676A75547A99AC538C8C87AEB8D7B5CF41C1854635BD6B` | 일치 |
| `Assets/10.Datas/Map/MapConfigDatabase.asset` | `EE0D4126BE8973462C34B0491C0E2ED2FB3E4866CB861435822A19AA757A6C34` | 일치 |
| `Assets/10.Datas/World/LivingWorld/Maps/DroughtVillage/NavMesh.asset` | `C7E328B9FA719001233EE66995A86304D83CECA19C8DD5B8ABA8F6975C4F65CD` | 일치 |

검증 범위는 마지막 미커밋 2개 파일보다 넓다. A/B/C/D/E/I를 합친 두 맵 분리, Portal/SceneManager/SaveManager, R04~R09 Flow·대화·표시, 주민 일과, 환경·조명·셰이더, 지도·DB·Addressables·빌드 씬까지 포함한다. 직접 소비자인 Actor/Contracts/FlowGraph/UI/Assembly-CSharp와 관련 테스트도 대상이다. 새 씬·로딩·직렬화 조건·Player 셰이더 의존성 및 주요 통합이므로 **Windows Player 검증은 필수**이며 문서만 수정했다는 이유로 면제하지 않는다.

### 현재 구현 한계

I1의 주민 6명은 몸과 설정만 연결됐고 `_isPlacementReady=false`다. 작업점 18개 중 작업 모션 연결은 헤이즐 약 포장 1개, 휴식점의 보호 판정은 미완성이다. 환경음 clip도 미연결이다. 따라서 일과/피신/음향 완성 조건은 현재 충족하지 못한다. 비활성 상태에서 오류가 없다는 이유로 LW-T05/T06/T07/T09를 통과 처리하지 않는다.

### 검증 중 발견한 동시 작업과 버전 경계

22:25~22:27 KST에 공유 Editor의 `execute_code` 이력을 확인하니 V가 실행하지 않은 `LivingWorldMotionPreview` 프리뷰와 `Assets/07.Animations/LivingWorld/NPC`, `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/LivingWorld` 생성 호출이 있었다. [공유 실행 이력](../../../../output/LivingWorld/V/SharedEditorHistory.json)과 PlayMode 실패 스택을 보존했다. 실행자의 신원·전체 변경 의도는 확인되지 않았다.

[비교 시점의 파일 결과](../../../../output/LivingWorld/V/FinalSnapshotComparison.json)는 기준 파일 변경 0, 명세 범위 내 추가 31개다. 이 중 V 문서/메타 2개 외 29개는 새 MotionSet 14개와 메타·폴더 메타다. 이 비교 범위 밖 `07.Animations`의 추가물까지 전부 셌다는 뜻은 아니다. [회귀 직후 기존 파일 비교](../../../../output/LivingWorld/V/PostTestManifestDiff.json)도 변경 0이었다. 후속 파일은 보존했으며 V의 I1 결과에 포함하지 않았다. 이후에도 외부 작업이 계속될 수 있으므로 이 문서는 최신 작업 트리 전체의 승인서가 아니다.

문서 최종 확인 중에는 `.gitignore`와 주민 일과 프리팹 **6개도 수정 상태**로 바뀌었다. [최종 상태 목록](../../../../output/LivingWorld/V/FinalStatus.txt), [후속 변경 해시](../../../../output/LivingWorld/V/LaterConcurrentChanges.json)를 보관했다. 이는 위 변경 0 비교 **이후**의 상태이며, 새 모션/클립·프리팹 연결을 I1 통과에 포함하지 않는다. C/E/I의 후속 인계와 Editor 독점이 확보되면 이 변경 범위의 참조·일과·저장 중단·성능을 재검증해야 한다.

전체 직렬화 참조 추가 감사는 시작 전에 공유 Editor 충돌을 확인했다. V가 예약했던 delayCall **1개만 취소**했으며 다른 작업의 콜백·에셋은 건드리지 않았다. 분리 PlayMode 재실행과 추가 Editor/Player 조작도 중지했다. 독점 실행이 가능한 시점에 후속 통합 해시를 고정하고 재개해야 한다. 사용자에게 I1 결과 유지/후속 통합 재검증 선택을 요청했으며, 결과를 기다리는 동안에는 최초 요청대로 I1 기록을 유지한다.

## 2. 판정과 증거 규칙

| 상태 | 의미 |
|---|---|
| 통과 | 지정 버전·범위에서 실제 수행했고 기대 결과를 만족 |
| 실패 | 실제 검사에서 조건 위반을 확인. 검사 도구 실패와 제품 결함을 구별 |
| 미수행 | 환경·자료·선행 구현 부족 등으로 실행하지 못함. 이유와 재개 조건 필수 |
| 사용자 테스트 대기 | 아래 플레이 절차의 사용자 결과가 아직 없음. 알려진 결함이 함께 있을 수 있음 |

진행 성공, 오류/경고, 시각·입력 품질을 독립 판정한다. 테스트 0개는 통과가 아니다. 전체 회귀에서 발견한 범위 밖 실패를 제외하여 전체 통과로 바꾸지 않는다. 완료 플래그 주입, 가상 서비스, API 직접 호출, 테스트용 시각 고정은 해당 방식으로 표시한다.

### 이번 실행 결과

과거 I의 75/75 EditMode, 11/11 PlayMode는 참고 이력이며 아래 V 실행 횟수에 합산하지 않는다. Test Framework 1.6.0, Addressables 2.9.1, URP 17.3.0을 사용했다.

| 검사 | 판정·실행 개수 | 근거와 범위 |
|---|---|---|
| 전체 EditMode | **실패: 1,141개 중 통과 1,140 / 실패 1 / 건너뜀 0**, 395.91초 | job `5f9eff6bda38437a9abcd44ef5672c9b`, [전체 결과](../../../../output/LivingWorld/V/EditModeResults.json). Sera 얼굴 profile 누락 |
| 생활 확장 관련 EditMode | **통과 75/75**. 전체 실행에 포함된 부분집합 | [필터 결과](../../../../output/LivingWorld/V/RelatedEditModeResults.json). 환경 시간표 8, 셰이더 상태 1, 맵 저장 이관 6, 일과 계약 14, 씬 로드 소유 3, 통합 참조 3, R04~R09 콘텐츠 40 |
| 전체 Editor Play Mode | **실패: 81개 중 통과 80 / 실패 1 / 건너뜀 0**, 21.13초 | job `b9e954d7964a4ea98defc00d856cd822`, [전체 결과](../../../../output/LivingWorld/V/PlayModeResults.json). DurationEffect 테스트 중 MCP 동적 코드의 컴포넌트 제거 Error가 들어옴. 제품 결함으로 단정하지 않고 분리 확인 |
| 생활 상호작용 Play Mode | **통과 11/11**. 전체 실행에 포함된 부분집합 | 같은 결과 파일의 `NpcLifeActionTests`. 실제 Unity 수명주기의 작업점 경쟁·비활성화·낙하·인계 취소·늦은 재개 포함. 시험 오브젝트·일부 상태/소품 주입이며 본편 6명 보행 아님 |
| Unity 컴파일 상태 | **통과: 컴파일 요청 뒤 `isCompiling=false`, `scriptCompilationFailed=false`** | [확인 방식](../../../../output/LivingWorld/V/CompilationStatus.json). Unity 외부 CLI/Player 컴파일 아님. 전체 회귀는 실제 로드된 테스트 어셈블리로 실행했으며 새 후속 모션 통합 이후 상태는 포함하지 않음 |
| 두 맵 등록·직렬화 참조 | **통과 3/3**, 관련 75개에 포함 | `LivingWorldIntegrationTests`가 두 저장 씬을 PreviewScene으로 로드. Missing Script·깨진 ObjectReference·다른 씬 MonoBehaviour 참조·러너/주민 중복·지역 조건 확인. 테스트 범위 밖의 의도적 null/모든 프로젝트 managed reference가 정상이라는 뜻은 아님 |
| 프로젝트 데이터 검사 | **실패: 오류 판정 260 / 경고 70**, 4개 검증기, 7.29초 | [ProjectValidation.json](../../../../output/LivingWorld/V/ProjectValidation.json), [분류 요약](../../../../output/LivingWorld/V/ProjectValidationSummary.json). 기존 `EditorValidationRegistry.Run(Project())` 실행. 실제 결함/검증기 규칙 불일치를 담당자가 판정해야 함 |
| 프로젝트 전체 Missing Script/managed reference/VFX 직렬화 확대 감사 | **미수행** | [감사 호출 원문](../../../../output/LivingWorld/V/ProjectReferenceAudit.cs.txt)은 예약 뒤 실행 전 취소. 공유 Editor에서 다른 작업이 에셋을 생성해 고정 버전 검사가 불가능. 결과 JSON 없음; 데이터 검증기·두 맵 참조 통과로 대체하지 않음 |
| 씬/주소/Depth 설정 보조 검사 | **통과: 텍스트상 두 씬 enabled, DB 주소 2개, PC Depth Texture 켜짐** | [BuildConfiguration.json](../../../../output/LivingWorld/V/BuildConfiguration.json). 실제 콘텐츠 로드/ShaderHasError/Player stripping 검사는 아님. I의 이전 셰이더 오류 0은 이번 V 결과로 재사용하지 않음 |
| Windows 콘텐츠/Player 빌드 | **미수행, 필수 검증 잔여** | 전체 회귀·데이터 검사 실패, 일과/피신/음향 미완성, 공유 Editor 동시 작업. WP-02~06은 독점 실행 가능한 최종 수정 상태에서 수행. 정책상 생략 가능한 변경으로 분류하지 않음 |
| 실제 저장·연속 플레이·패드·시각/음향·성능 | **사용자 테스트 대기** | 사용자 결과 0건. 아래 명세로 개별 회신 필요 |

전체 EditMode에는 `LakeNarrativePlaybackTests`의 과거 `LakeOfLife` 씬 재생 3개도 들어 있다. 해당 검사는 일부 Flow를 비활성화하고 진입/상태를 주입한다. 통과했어도 새 `LakeOfLifeScenario ↔ DroughtVillage` R04~R09 완주로 세지 않는다. 맵 이관 6개는 합성 저장/임시 폴더의 암호화 사본 경계를 포함하며 실제 사용자 저장 검증과 다르다.

전체 회귀 출력에는 DOTween destroyed target/startup 경고와 `[Services] 등록되지 않은 서비스 계약 요청`도 있다. 일부는 고립된 테스트의 기대 로그일 수 있으므로 테스트 이름·원문을 함께 보존했고, 이를 실제 맵의 경고 0 근거로 사용하지 않았다. 특히 `SkillTreeVisualTests.실제_성장화면의_곡선은_일시정지중에도_흐른다` 출력에는 DOTween safe mode 18 startup errors가 있어 별도 인계한다.

검증 호출 중 응답 연결 종료/WinError 10053이 있었지만 이후 데이터 검사 결과 JSON이 생성된 것을 확인했다. 프로젝트 데이터 검사는 동일한 읽기 전용 호출이 한 번 재시도되었으며 중복 횟수를 합산하지 않았다. 보조 참조 감사 코드의 Newtonsoft 어셈블리 중복 컴파일 오류는 프로젝트 C# 컴파일 오류가 아니다. 감사 호출을 수정해 예약했지만 동시 작업 확인 후 실제 감사 전에 취소했다.

데이터 오류 판정은 Actor 14, Combat 239, Dialogue 7이며 경고는 Actor 18, Combat 28, DataPath 9, Dialogue 11, Item 4다. 검사 명세가 오래됐을 가능성도 조사 대상이다. 예를 들어 기존 `CombatDataValidator.ValidatePlayerMotionSetMatching`은 `ActorAnimator.MotionSet` 하나에 대해 키를 해석하므로 실제 현재 무기별 매핑/폴백과 일치하는지 확인해야 한다. 260건 모두를 신규 제품 결함으로 확정하거나, 반대로 검증기 문제라며 삭제하지 않았다.

## 3. 사용자 테스트 공통 준비

1. 결과에 `LW-I1-V1`, Editor 또는 Player 실행 파일/빌드 ID, 시작 슬롯·사건 단계, 키보드/마우스 또는 실제 패드 모델, 해상도·품질·VSync·프레임 제한을 적는다. 두 게임플레이 씬을 동시에 열어 Play하지 않고 **Boot → 타이틀 → 새 테스트 게임/복사 슬롯 로드**로 시작한다.
2. 게임을 종료한 상태에서 `Application.persistentDataPath/saves/`의 원본 `save_slot_N.sav`와 존재하는 `.sav.bak`·`.sav.tmp`를 별도 백업한다. 원본 파일명·크기·SHA-256을 기록한다. 검사 종료 후 원본 해시가 같은지 확인한다. 원본 슬롯을 활성 슬롯으로 플레이하거나 자동 저장 대상으로 사용하지 않는다.
3. 새 게임도 기존 슬롯을 덮어쓰지 않는 빈 시험 슬롯을 쓴다. 현재 UI가 시험 슬롯을 선택하지 못하면 담당 B가 빈 슬롯의 로드 경로를 준비할 때까지 멈춘다. 파일명만 바꾼 구 저장은 맵 분리 이관 검증 사본이 아니다.
4. 구 저장은 B/I가 위치와 진행을 검토한 뒤 `SaveManager.TryCreateLivingWorldCopy(sourceSlot, destinationSlot, plan, out error)`로 빈 목적 슬롯에 만든다. 현재 제공 기능은 API이며 전용 UI가 있다는 뜻이 아니다. `.sav/.sav.bak/.sav.tmp` 중 하나라도 목적 슬롯에 있으면 덮어쓰지 않는다. 이관 준비가 안 된 슬롯은 테스트 대기로 남긴다.
5. 준비 저장: **S0** 신규 게임, **S1** R04 수락 전, **S2** R04 잔류 선택 후 식수 미인계, **S3** R04 중간, **S4** R06 시험 전, **S5** R09 시작 전, **S6** R09 완료 뒤. 실제 준비 파일은 아직 제공되지 않았다. 정상 플레이로 만든 저장의 사건·시각·주민 선택을 기록하며 플래그 일괄 주입으로 대체하지 않는다.
6. 환경 시각은 호수 10:30 비/13:30 건조 중/22:00 밤, 가뭄 08:00 흐림/14:00 건조 바람/19:00 맑음/22:00 밤을 기준으로 삼는다. 정상 시간 경과 또는 검증된 해당 시각 저장을 사용한다. 개발용 시간 설정을 썼다면 `시각 주입`으로 별도 기록하고 정상 왕복/로드 보존 검사를 생략하지 않는다.
7. 각 항목에서 Console 또는 Player.log의 해당 시간 구간을 보관한다. 오류가 나오면 진행 성공과 별도로 실패를 기록하고 저장/씬 덮어쓰기를 중단한다. 재현 단계·입력 장치·시작 저장·실제 현상·화면/영상·로그를 전달한다.

좌표는 개발자용 길 찾기 보조다. 호수 출구 `Exit_ToDrought` 약 `(984,62.81,449)`, 가뭄 출구 `Exit_ToLake` 약 `(985,62.97,451)`이며 **현장 상호작용 입력**으로 이동한다. 정상 경로 확인에서 좌표 순간이동으로 접근을 대체하지 않는다.

## 4. LW-T01~10 실행 명세

### LW-T01 — 두 맵 왕복과 실패 복귀

**상태: 사용자 테스트 대기.** S1 또는 정상 도달 가능한 S0, 키보드/마우스와 패드를 각각 사용한다.

1. 호수 외곽 출구까지 걸어가 안내와 입력을 확인한다. 입력 전에는 이동하지 않아야 한다. 상호작용 후 로딩 중 같은 입력을 여러 번 누른다.
2. `DroughtVillage / livingworld.drought.from_lake` 도착을 확인한다. 기준 위치 `(978,63.071426,451)`, 회전 quaternion `(0,-0.525731,0,0.850651)`. 몸이 지면에 안전하게 서고 마을/귀환길이 읽혀야 한다. 5초 정지 후 이동·카메라·상호작용을 해 본다.
3. 약 7m 떨어진 귀환 출구로 걸어가 입력한다. `LakeOfLifeScenario / livingworld.lake.from_drought`, `(991,62.74581,449)`, 회전 `(0,0.707107,0,0.707107)`에 도착해야 한다. 총 10회 왕복하며 도착/재입력/발견 시점을 기록한다.
4. 첫 도착 이전과 이후 지도에서 발견 키 `livingworld.drought.entrance`, `livingworld.lake.outskirts`의 사용 가능 상태를 비교한다. 로딩 수락만으로 방문 완료가 기록되면 실패다.
5. **B 담당 실패 재현 준비 후 별도 실행:** 시험 복사본에서 목적 씬을 로드할 수 없는 요청을 기존 로딩 API로 한 번 발생시키고 실패 결과를 기록한다. 본편 BuildSettings·카탈로그를 훼손하지 않는다. 사용자는 실패 뒤 이동/카메라/Cancel/재시도를 확인한다. 준비되지 않으면 이 소항목은 미수행이다.

**기대:** 올바른 방향·접지, 활성 플레이어 1명, 즉시 자동 재전환/중복 로딩/입력 잠금 잔류 없음. 실패 요청은 새 맵 발견·사건 완료를 생산하지 않고 원래 플레이로 복귀한다. 정적 Capsule/NavMesh 검사는 이 항목을 대체하지 않는다.

### LW-T02 — 역순 조사와 수락 후 목표 복원

**상태: 사용자 테스트 대기.** S1의 독립 사본 2개를 준비한다.

1. 사본 A에서 R04 수락 전에 가뭄 마을에 가서 `R04_well` 우물을 조사하고 대화를 끝낸다. 밭·집도 접근 가능한 것부터 조사한다. 아직 열리지 않은 입력은 거부 상태를 기록한다.
2. 호수로 귀환해 기존 퀘스트를 정상 수락하고 저장/로드한다. 다시 가뭄 마을에 들어가 목표·우물 표시·재조사 여부를 확인한다.
3. 사본 B에서는 수락 → 우물 → 집/밭 순서로 진행하고 A와 같은 도달 단계의 퀘스트·주민 선택·물자·보상을 비교한다.

**기대:** 정상 종료한 조사만 `lake.red_tree.well_examined`에 누적되고 기존 발견을 다시 요구해 막히지 않는다. 조기 조사만으로 주민 의사·식수 인계·진실/결말이 완료되지 않는다. 다른 맵에서는 출구를 안내하고 도착 후 실제 대상 마커로 갱신된다. 획득/보상은 1회다.

### LW-T03 — 식수·주민 이동 중단, 저장/로드, 중복 방지

**상태: 사용자 테스트 대기.** S2 사본을 분리해 각 단계가 다른 실험에 영향을 주지 않게 한다.

1. 잔류 주민 의사를 먼저 확인하고 `R04_water` 식수 인계를 시작한다. 시작 직후, 전달 도중, 완료 직전 각각 Cancel/이탈로 중단한다. 재접근하여 완료까지 다시 수행한다.
2. 다른 사본에서 진행 중 저장 입력을 시도한다. 현재 계약이 저장을 거부하면 거부 표시와 조작 복귀를 확인한다. 허용되면 그 사본을 로드해 미완료 단계에서 재시도한다. 저장 성공 여부를 따로 기록한다.
3. 주민 짐 준비/서사 보행 중 대화, 취소, 왕복 요청을 각각 시도한다. 대화 중 포털 요청은 거절되어야 하고, 수락된 이동의 생활 인계는 취소 정리되어야 한다. 임의 완료로 바뀌면 실패다.
4. 정상 완료 후 저장/로드 → 왕복 → 같은 입력 3회 → 재로드한다. 호수 출발 물자와 가뭄 인계 물자를 함께 관찰한다. R03 식사/돌봄도 같은 중단 경계를 1회씩 확인한다.

**기대:** 미완료는 재시도 가능, 실제 완료만 저장, `resident_stays`와 `water_handed_over`가 독립적으로 보존된다. 가방·보상·몸체·손 소품·물통이 복제되지 않고 예약/입력 잠금이 남지 않는다. 주민 이동 도중 로드해도 순간 완료/무한 보행이 없어야 한다. 일과 보행 검사는 T05 활성화 뒤 별도로 수행한다.

### LW-T04 — 기존 저장 복사 이관

**상태: 사용자 테스트 대기, 실제 구 저장과 슬롯별 이관 계획 준비 필요.** 합성 JSON 테스트는 보조 검사다.

1. **OLD-IN** 구 단일 씬 피해 마을 안, **OLD-OUT** 호수 지역 밖/다른 지역, **OLD-R09** 결말 재방문 완료의 실제 3.4 저장을 각각 원본 보관한다. 주민 잔류/떠남, 파티, 퀘스트, 물자·재고, 발견, 시각, killed/consumed/respawn 요약을 기록한다.
2. B/I는 원본 위치를 사건과 현장에 대조해 `IsPlayerLocationReviewed`를 결정한다. 안/밖을 임의 사각형으로 추정하지 않는다. 이동할 9 GUID는 I 기록 3절의 목록을 사용한다. respawn이 있는 GUID는 실제 새 위치/회전을 확인한 뒤에만 `HasVerifiedPose=true`를 지정한다.
3. `SourceMapId/SourceSceneName=LakeOfLifeScenario`, `TargetMapId/TargetSceneName=DroughtVillage`. 안쪽 저장은 검증한 가뭄 도착, 밖 저장은 검토한 현재 위치 유지 또는 호수 안전 도착으로 사본을 만든다. API false/error는 그대로 실패/준비 미완료로 기록하고 확인 필드를 억지로 true로 바꾸지 않는다.
4. 목적 사본만 Boot에서 로드한다. 지도·접지·주민 선택·파티·가방·조사/인계·R09 결과를 원본 요약과 비교하고 저장/왕복/재로드한다. 완료 키 `lake.living_world.map_split.v1`를 확인한다.
5. 이미 이관된 사본을 다시 빈 슬롯으로 복사해 동일성을 확인한다. 원본=목적, 목적 `.bak`만 존재, 목적 `.tmp`만 존재, 위치 미검토, 재스폰 키 충돌은 별도 시험 데이터에서 거부되어야 한다. 사용자 실제 원본을 손상시키며 실패를 만들지 않는다.

**기대:** 원본 해시 불변, 사본만 생성, 부분 파일 잔류/덮어쓰기 없음. 이동 GUID의 맵 소유만 변경되고 퀘스트·주민 선택·가방·파티·시간·기존 사실은 보존된다. R09 완료가 R04 초기 상태로 돌아가지 않는다. 다른 저장 버전은 3.4 성공으로 일반화하지 않는다.

### LW-T05 — 6명 생활, 밤, 대화 중단과 작업점 경쟁

**상태: 사용자 테스트 대기. 현재 I1의 일과 완성 조건은 실패(비활성); 실제 보행 검사는 미수행.** C/E/I가 모션·소품·보호 자리·경유점·중복 이동 컨트롤러를 정리한 버전에서 실행한다. 검사를 위해 현재 안전 게이트만 강제로 켜지 않는다.

1. 호수 `NPC_Joy/Npc_Hazel/NPC_Morgan/Npc_Yeonmok`, 가뭄 `RedTree_RemainingResident/RedTree_SettlingResident`를 09~18시 3~5분 관찰한다. 작업→이동→휴식과 사람별 시작 시차를 기록한다.
2. 각 주민의 이동/작업 중 대화 시작 → 10초 유지 → 종료한다. 대화 동안 위치와 손 소품이 고정되고 종료 뒤 유효한 자리로 다시 예약·이동해야 한다. 이후 슬롯 저장/로드, 씬 이탈/재입장도 수행한다.
3. 플레이어로 통로를 막아 이동 정체를 만든다. 현 설정은 정체 2초, 전체 이동 20초, 재시도 5초이므로 실제 설정값과 비교한다. 막힘 뒤 작업 취소/휴식/대기와 재접근을 관찰한다. 순간이동·벽 관통·무한 보행은 실패다.
4. **C 준비 시험 배치:** 본편을 저장하지 않는 복사 배치에서 두 주민에게 같은 `NpcWorkPoint`를 지정한다. 한 명이 점유한 동안 다른 한 명은 대기/대체 자리, 점유자 대화/취소/비활성화 후 다른 한 명은 획득해야 한다. 이전 실행의 늦은 해제가 새 소유권을 지우면 실패다. 이 배치는 제작 필요이며 현재 본편에 경쟁 상황이 있다고 가정하지 않는다.
5. 21:59→22:00, 05:59→06:00 전후를 관찰하고 밤에 필수 주민에게 대화/식수/퀘스트를 시도한다. 호수 비 시간에도 피신 후 접근 가능한 자리에 있어야 한다. 떠나기로 선택한 주민의 서사 이동을 반복 일과가 되돌리지 않아야 한다.

**기대:** 정원 1, 취소·로드 후 점유/소품 해제, 필수 통행·상호작용 가능. 반복 관찰 전후 인벤토리·일회성 완료가 늘지 않는다. 작업면/손 접점·발 접지·모션 가독성은 실제 화면으로 판단한다.

### LW-T06 — 환경 시간표, 실내 억제와 회복 독립

**상태: 사용자 테스트 대기. 피신 보행은 T05 선행 완료 필요.** S3와 회복 이후 S6를 사용한다.

1. 호수 10:30에 처마 밖→문턱→안→밖을 걸으며 카메라를 급회전한다. 13:30에 다시 확인한다. 비/먼지가 지붕 아래와 얼굴 앞을 관통하거나 야외 비가 통째로 사라지면 실패다.
2. 가뭄 08:00/14:00/19:00과 밤을 방문한다. v1은 강수·날씨 젖음 모두 0이다. 회복 이후에도 갑자기 비로 바뀌는 것을 정상으로 간주하지 않는다.
3. 호수 10:30 및 13:30 저장을 각각 로드하고 왕복한다. 정상 이동 동안 흐른 시각을 기록한다. 동일 day/minute/profile version을 비교하는 보조 검사에서는 같은 상태여야 한다. 정상 왕복에서 경과 시간에 따른 변화는 허용하며 재추첨/매번 초기화는 실패다.
4. 회복 전/후 같은 시각에서 수위·작은 물길·건조 피해 흔적을 비교한다. 날씨만으로 우물 조사, `absorption.stopped`, `return.water_checked`, `_Recession` 등이 바뀌면 실패다. 전체 밭이 즉시 푸르게 되는 것도 기대 결과가 아니다.
5. 비 호수→가뭄→타이틀 또는 다른 씬 전환 뒤 바람/젖음/입자/Volume/조명 잔류와 방향광 중복을 확인한다. 새 생산자의 상태를 이전 생산자 해제가 지우지 않아야 한다.

**기대:** 호수 10:00~11:30 강수 0.4, 13:30 강수 0·젖음 0.4~0.8, 18:00 건조. 환경 상태와 날씨 표현이 일치하며 서사 물길과 독립이다. 지붕 보호 볼륨은 NPC 보행 가능한 피신처를 자동 보증하지 않는다.

### LW-T07 — 전투·조사·대화 가독성

**상태: 사용자 테스트 대기. 환경음 완성 조건은 현재 실패(clip 미연결).** R04 우물/개울·기존 조우와 호수 주민 대화를 같은 카메라/날씨에서 비교한다.

1. 맑음/비/먼지·낮/밤에서 우물/개울 조사 카메라를 끝까지 실행한다. 증거의 형태, 상호작용 프롬프트와 출구가 읽히는지 기록한다.
2. 주민 대화 시작/끝/취소 중 얼굴·표정·자막·파트너와 카메라 앵커를 관찰한다. 집중 감쇠가 시작되고 마지막 대화/조사 종료 후 정상 강도로 돌아와야 한다.
3. 기존 전투에서 Lock-on, 공격 전조, 피격, 회피/가드, KillCam을 관찰한다. 화면의 입자·투명 수면·노출 때문에 공격 방향/타이밍을 놓치거나 카메라가 단서를 가리면 실패다. 게임 조작 없이 수치만으로 판정하지 않는다.
4. D/E의 실제 루프 clip/Mixer 연결 후 환경음 on/off로 같은 대사·전투 신호를 청취한다. 실내·집중 감쇠와 설정 음량/음소거도 확인한다. 현재 무음은 청취 품질 통과가 아니다.

전투 자동 실행을 수행하게 되면 AI가 기존 Player 경로를 준비하고 계측·로그를 수집한다. 이 문서의 사람 항목은 화면/소리/감각 판단이며, 사용자의 공격 재현을 자동 검사 시작의 선행 조건으로 요구하지 않는다. 현재 새 두 맵을 대상으로 한 전투 Player 계측은 미수행이다.

### LW-T08 — 실제 패드와 지도·취소

**상태: 사용자 테스트 대기.** 실제 패드로만 한 번, 키보드/마우스로 한 번 수행한다.

1. R03 식사 `(1030.5,62.051,409)`, 돌봄 `(1006,62.631,410)`, 호수 출구, 가뭄 우물, 식수, 귀환 출구 6곳을 정상 진행 단계에서 접근/실행/Cancel한다. 미완료 입력은 재시도하고 완료 입력은 중복 지급하지 않아야 한다.
2. 지도 열기 → 지역/거점 이동 → 목적지 선택 → 취소 → 재열기 → 이동 확정을 수행한다. 초기 포커스, 방향 이동, Cancel 탈출, 탭의 숄더/트리거 조작을 기록한다.
3. 가뭄 미방문 상태에서는 발견 이동을 사용할 수 없어야 한다. 첫 현장 도착 뒤 양쪽 지도 귀환점을 사용한다. `RegionInfo`가 자기 씬의 도착점을 가리켜 반대 맵으로 튀지 않아야 한다.
4. 다른 맵 목표 여러 개를 추적한다. 출구 별칭 113개가 겹치는 구도에서도 어느 길로 가야 하는지 읽혀야 하고 도착 후 실제 현장 마커로 바뀌어야 한다. 일시정지 상태에서 화면 전환/닫기도 확인한다.

**기대:** 포커스 유실·Cancel 불가·선택 없는 강제 이동·중복 입력 없음. UI 트윈은 일시정지 중 정상 종료하며 화면 닫기 후 플레이 입력이 복귀한다. 알려진 `clue_red_cloth` 중복은 별도 결함이다.

### LW-T09 — 프레임 비용과 누적 자원

**상태: 사용자 테스트 대기 / AI 계측 미수행.** 목표 하드웨어·확정 예산·동일 조건의 이전 Player 기준선이 아직 없다. FPS 달성으로 판정하지 않는다.

1. Windows Player의 CPU/GPU/RAM/VRAM·해상도·품질·VSync·프레임 제한·빌드 종류를 기록한다. 같은 카메라 위치/방향, day/minute, 주민 수, 저장에서 비교한다. Editor와 Player 수치를 같은 기준으로 섞지 않는다.
2. 호수 식사/돌봄, 호수 수면, 가뭄 우물/밭 4구도에서 워밍업 후 각 60초씩 낮/밤/비(호수)/먼지(가뭄)를 3회 측정한다. 환경 off/on과 일과 off/6명 활성 비교는 담당자가 준비한 검증용 설정으로 수행하며 본편을 저장하지 않는다. I1은 일과 off만 측정 가능하다.
3. CPU Main/Render·GPU의 median/p95 ms, GC Alloc/frame, draw/SetPass, 입자 수, 투명 오버드로·Particle Collision, 메모리/VRAM peak, 씬 로드시간을 기록한다. GPU 계측 불가면 0으로 쓰지 않고 미측정으로 적는다.
4. 3~5분 유지 및 T01 10회 왕복 전후 런타임 Material/Volume/AudioSource/ParticleSystem/보조광 수와 메모리를 비교한다. 첫 로딩 캐시 이후 계속 증가하는 자원은 원인과 증가량을 기록한다. 저품질에서도 단서·필수 NPC·날씨 구분을 확인한다.

**판정:** 기획의 16.7ms 및 생활/환경 추가 CPU·GPU 각각 2ms는 첫 실험 예산 후보다. 목표 기기와 기준선에서 승인한 예산을 먼저 기록한 뒤 그 예산으로 판정한다. 현재의 비활성 일과 비용을 생활 6명 완성 비용으로 보고하지 않는다.

### LW-T10 — R04~R09 실제 두 맵 연속 진행과 재방문

**상태: 사용자 테스트 대기.** S1에서 시작해 실제 입력으로 아래 경계를 통과한다. 개발용 완료 플래그/노드 진입 변경으로 단계를 넘긴 실행은 보조 검사로만 남긴다.

| 순서 | 실행 위치와 행동 | 저장/로드·기대 결과 |
|---|---|---|
| R04 | 가뭄 우물/집/주민 선택/식수·짐/밭·개울/조우 → 호수 보고·지원·물자 | 주민 잔류/떠남을 사본별로 검사. 호수에 있을 때 전역 사실 갱신으로 가뭄 조우가 생성되지 않음 |
| R05 | 호수 기록대→차단 수레/우회→구조→쉼터/합류 | 구조 직전/직후 저장·재로드, 동료·보상·대화 중복 없음 |
| R06 | 호수 목록/포장/재고 → 가뭄 설치·순서·시험/협조 주민·묘묘/구조·단절·관찰 → 호수 식량/외부 식수·보고 | 양쪽 목표와 파트너/카메라 정상. 설치·시험 중단 후 재시도 가능, 수위 전후 표시 보존 |
| R07 | 가뭄 잔류 거처·식량 인계 ↔ 호수 외곽 인계·돌봄·미아 대피·재고/준비 | 실제 인계 완료 뒤 출발 물자와 수령 표현이 중복 소유하지 않음 |
| R08 | 호수 중앙 제단·결단·수위/흡수 중단 관찰 | 전투 승리만으로 관찰 완료를 대체하지 않음. 관찰 후에만 다음 준비가 열림 |
| R09 | 호수 재고→포장→수레/적재→출발 → 가뭄 의사·수령→외부 물길·수로/길→피해 관찰 → 호수 식사·비용·안내 | 출발 전/후, 수령 전/후 각각 저장·왕복·로드. 같은 물자가 두 맵에 남거나 2회 지급되지 않음 |
| 결말 재방문 | S6에서 가뭄 밭/물길/주민 → 호수 생활 자리 → 다시 가뭄, 한 번 종료 후 재로드 | 작은 회복과 피해 흔적, 주민 선택, 노동/물자 결과 유지. 초기 퀘스트/대역/수레로 되돌아가거나 진행이 소진되지 않음 |

각 경계에서 활성 목표·지도 마커·주민/동료 몸·상호작용·대화/조사 카메라·서비스 경고/예외를 따로 기록한다. 가능한 조사와 인계 순서를 사본에서 바꿔도 다음 단계로 진입 가능해야 한다. 실행하지 않은 분기와 결말은 통과에 포함하지 않는다.

## 5. Windows Player 검증

**필수, 빌드와 실행을 별도 판정한다.** 개발 빌드 성공이나 Boot만 열린 결과는 콘텐츠 통과가 아니다. 기존 단일 맵 빌드와 과거 Phase 6/7 기록은 LW-I1-V1의 Player 근거가 아니다.

| ID | 작업·완료 근거 | 상태 |
|---|---|---|
| WP-01 | Editor 컴파일/관련 검사, 전체 회귀·참조 결과 정리, 최종 통합 해시 고정 | 전체 회귀·데이터 검사 실패, 공유 Editor 간섭, 전체 직렬화 감사 잔여 |
| WP-02 | 실제 Addressables 프로필·그룹/키·build/load path 기록. MapConfigDatabase/QuestDatabase, 새 지도/환경 의존성의 콘텐츠 빌드·카탈로그 갱신. 오류 0과 결과 파일 보관 | 미수행: WP-01 실패 판정·수정 후 최종 상태에서 실행 |
| WP-03 | Global 또는 실제 활성 BuildProfile에 Boot/Title/Loading/호수/DroughtVillage 포함. Windows64 백엔드·스트리핑·URP 품질·Depth Texture 기록 후 Development Player 빌드. BuildReport의 errors/warnings/size/time·실행 파일 해시 보관 | 미수행: WP-01/02 및 미완성 통합 후 실행 |
| WP-04 | Boot→사본 로드→실제 두 맵 왕복·지도 귀환·우물/식수·R06/R09·저장 재로드. 주소/씬 로드 실패 복귀 포함. Player.log 검토 | 사용자 테스트 대기 |
| WP-05 | 두 맵의 낮/밤/비/먼지·실내·집중 감쇠. 핑크/누락 variant/깊이 텍스처/수면·흙·입자/전역값 해제 확인 | 사용자 테스트 대기 |
| WP-06 | 실제 기기 T09 계측, 최종 배포 승인 시 Release 설정의 핵심 로딩·저장·날씨 경로 재검사 | 사용자 테스트 대기 |

빌드 경로는 실행일에 `Builds/LivingWorld_I1_<YYYYMMDD>/`로 정한다. 아직 이 경로에 검증 빌드가 있다고 가정하지 않는다. `ScenarioMapAuthoring.BuildScenarioPlayer`는 기존 시나리오 검증을 함께 실행하고 자체 경로에 출력하므로 새 두 맵 검증이 끝났다는 근거로 무조건 호출하지 않는다. 실제 enabled 씬 목록과 최종 콘텐츠를 사용한다. 셰이더 오류를 숨기려고 Always Included 전체 보존을 늘리지 않는다.

V의 텍스트 확인에서는 Global 목록의 enabled 씬이 9개이며 Boot/Title/Loading/기존 LakeOfLife/ForgottenWatchpost/HarvestOfPlain/LakeOfLifeScenario/CombatTestMap/DroughtVillage다. 기존 씬을 임의로 빼지 않는다. 주소는 Default Local Group의 `MapConfigDatabase`·`QuestDatabase`, 활성 프로필 ID는 `1d1330edac561884da86544281970ca4`, `m_BuildAddressablesWithPlayerBuild=0`이다. 마지막 값은 패키지 enum/환경설정에 따른 유효 정책을 WP-02에서 확인해야 하며 콘텐츠가 이미 갱신됐다는 뜻이 아니다. Standalone backend/stripping은 YAML에 명시 override가 없어 기본값을 추정하지 않고 실제 Editor 조회를 남겨 두었다.

## 6. 결함 인계와 재검증

담당은 작업 지시의 책임 구분이다. 별도 메시지 발송이나 담당자 수정 완료를 뜻하지 않는다. 코드/에셋을 임의 수정하지 않고 이 문서를 인계물로 사용한다.

| ID / 상태 | 담당·파일 | 재현과 실제 결과 | 기대 결과·수정 후 검증 |
|---|---|---|---|
| V-D01 / 실패: 생활 미완성 | C/E → I. `Assets/03.Prefabs/LivingWorld/`의 주민 설정, 두 Scenario 씬의 `LivingWorld` | I1 주민 6개의 `_isPlacementReady=false`; 낮 시간에 반복 일과/피신 완성을 검증할 수 없음 | 모션·소품·안전 작업/피신 경로 확보 뒤 활성화. T03/05/06/09, 변경 씬 통합 참조와 NpcRoutine/NpcLife만 우선 재검증 |
| V-D02 / 실패: 환경음 미연결 | D/E → I. 환경 프리팹의 RainAudio/WindAudio | clip 없이 비/바람 재생, 청취 비교 불가 | 실제 루프와 Mixer·음량 연결, T06/07/09 재검증 |
| V-D03 / 알려진 결함, I 근거 | I/C. 호수 씬 `NPC_Joy`, `Npc_Lucia` | I 감사에서 NpcMovementController 중복 | 몸별 소유 컨트롤러 1개, 대화·KCC·일과 충돌 없음. 수정된 주민 T03/05/07 |
| V-D04 / 알려진 결함, I 근거 | I/C. 가뭄 `RedTree_RemainingResident`, 헤이즐 작업점 | 잔류 주민 시작점 NavMesh 1m 샘플 실패; 헤이즐 보정 약 0.877m | 실제 KCC 접지·경유점·작업면 검증. 단순 NavMesh 투영으로 실제 보행 통과 처리 금지 |
| V-D05 / 알려진 결함, I 근거 | I/기존 지도 콘텐츠 담당. 호수 `clue_red_cloth` registrar | 동일 마커 2개. I 시작 사본에도 존재 | 소유 판정 뒤 중복 제거; 해당 단서와 T08만 우선 재검증 |
| V-D06 / 실패: 전체 회귀에서 발견 | 캐릭터/대화 얼굴 담당. `Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Sera.prefab` | `DialogueFaceBindingTests.PlayerModelHasValidIdleBlinkBinding("Sera")`: `dialogueFace.profile == null` | 실제 세라 눈꺼풀 mesh/shape에 맞는 프로필 연결. 해당 테스트와 세라 대화 화면 재검증. 통합 기인 여부 미확정 |
| V-D07 / 실패 판정 14건, 규칙 적합성 검토 필요 | Actor 데이터 담당/I. `Npc_Yeonmok_ActorDef.asset`, R05 `Actor_*.asset`, R06 `Actor_Myomyo/Sera/YeonHoa.asset` 등, 전체 경로는 ProjectValidation.json | `EditorValidationRegistry.Run(Project())`에서 `attributeProfile` 비어 있음 | 서사 대역에도 필수인지 실제 소비 계약을 확인해 데이터 또는 검사 규칙을 수정. 해당 Actor 검사와 R05/06·T10 재검증 |
| V-D08 / 실패 판정 239건, 개별 원인 미확정 | GAS/Motion 담당과 기존 CombatDataValidator 담당. `PlayerModel_*.prefab`, Dryad/Training_Dummy/Griffin/Story_LakeRoad_Beast, Default AbilitySet. 정확한 필드/키는 ProjectValidation.json | 같은 전체 검사에서 MotionKey 해석 실패·HitPhase 범위 초과·충돌 이벤트/HitPhase 없음 | 실제 무기별 Motion 해석과 검증기 경로가 같은지 먼저 대조. 검증기 오류를 콘텐츠 누락으로 단정하지 않음. 확인된 Motion 누락은 임의 매핑 금지. 수정된 액터/능력의 데이터 검사와 전투 Player 경로 재검증 |
| V-D09 / 실패 판정 7건 | I/대화 데이터 담당. `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_*.asset` 7개 | 전체 Dialogue 검사에서 graphId 중복: intro_mia/intro_guide/honoka_rescue/jun_rescue/lian_rescue/siuha_confrontation/siuha_joined | 원본/시나리오 그래프 공유가 의도인지 저장·복원 범위와 함께 판정. 무조건 ID 재발급 금지. 수정된 대화와 저장 재진입 검증 |
| V-D10 / 경고 확인, 원인 미확정 | UI/테스트 담당. `Assets/Tests/EditMode/UI/SkillTreeVisualTests.cs`, 관련 화면 tween 수명주기 | 위 일시정지 테스트는 통과하지만 출력에 destroyed RectTransform/CanvasGroup와 DOTween safe mode 18 startup errors | 화면 종료/파괴 시 tween 정리 또는 테스트 격리 확인. 해당 화면 열기→일시정지→닫기·재열기 및 테스트만 재검증 |
| V-D11 / 실패: 검사 간섭 의심 | 검증 환경/MCP 실행 담당. `Assets/Tests/PlayMode/Ability/GameplayAbilityVerticalSlicePlayModeTests.cs` | DurationEffect 실행 중 `Can't remove NpcMovementController ... NpcActor depends on it`, `MotionEventExecutor ... ActorAnimator depends on it`. 스택의 시작은 `MCPDynamicCode.Execute/DestroyImmediate`; V는 이 삭제 코드를 호출하지 않음 | Editor 독점 상태에서 해당 테스트만 재실행, 동적 실행의 출처/시점을 조사. 최초 전체 실패 기록은 유지하고 분리 통과와 구별 |

이번 V에서 제품 코드/데이터 수정은 하지 않았다. 따라서 수정 후 통과로 바꾼 항목도 없다. V-D11 분리 재실행 역시 동시 작업 때문에 **미수행**으로 남겼다. 담당자가 파일·새 해시·변경 이유를 인계하면 해당 테스트/소비자만 먼저 재검증한다. 새 실패나 공용 계약 영향이 없는 한 전체 검사를 반복하지 않는다. 다만 아직 끝나지 않은 주요 통합 필수 검사와 최종 Player 검증은 계속 남는다.

이번에 보관한 것은 이 문서가 인용하는 텍스트/JSON/CSV 로그뿐이다. 별도 프로젝트 사본·Player 빌드·CLI 출력 DLL은 생성하지 않았다. Test Runner의 임시 `InitTestScene`은 실행 종료 후 제거됨을 확인했다. 원래 I의 사용자 변경 백업과 외부 작업의 신규 모션은 삭제하지 않았다.

문서의 LW-T01~10 제목, 상대 링크, `.meta` GUID와 diff 서식을 확인했다. [문서 검사 기록](../../../../output/LivingWorld/V/DocumentCheck.json). V 근거 폴더는 약 5.3MB이며 재현·인계 근거로 보관한다.

## 7. 사용자 회신과 남은 위험

각 소항목별로 다음 형식을 사용한다. 여러 실험을 한 줄의 통과로 합치지 않는다.

```text
LW-T03-2 / 통과·실패·미수행
버전: LW-I1-V1 / Editor 또는 실행 파일·빌드 ID
시작: 사본 슬롯·R 단계·주민 선택·day/time
장치/설정: 입력 장치·해상도·품질
진행 결과: 기대와 실제 차이, 실패한 조작 단계
오류/경고: 없음 또는 메시지/발생 시각
근거: 화면·영상·Editor.log/Player.log·Profiler 파일
원본 저장: 검사 전후 해시 일치 여부
```

실제 저장 이관·두 맵 연속 완주·R09 재방문·패드·환경 화면/소리·성능에는 사용자 결과가 없다. 일과/피신/음향의 구현 결손과 필수 Player 검증이 남아 있으므로 **현재 생활 확장 완료 또는 출시 가능으로 판정하지 않는다.** 로컬 `output/`은 Git ignore 대상이므로 다른 작업 환경에 인계할 때 이 문서가 인용한 소형 근거도 함께 전달한다.
