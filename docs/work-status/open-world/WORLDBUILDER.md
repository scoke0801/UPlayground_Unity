# WorldBuilder 작업 상태

- 정본: [지역형 HeightMap 설계](../../../Assets/docs/design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md), [요청](../../../Assets/docs/requests/WORLDBUILDER_REGIONAL_IMPLEMENTATION_PROMPTS.md)
- 범위: P00 → P03 구현·자동 검사 통과. 신규 월드만 생성하며 기존 월드·Addressables·서비스는 변경하지 않는다. 사용자 확인 항목은 단계별로 별도 유지한다.

## P00 · INTEGRATION_AUDIT (2026-11-16)

저장소 파일 및 열린 Editor를 AI가 읽기 조사했다. Editor PID 29756, Edit Mode, 컴파일 중 아님. 기존 `LakeVillageWorld.unity` 하나가 저장된 상태로 열려 있다([조회 원본](../../../output/WorldBuilder/baseline-editor.json)).

### 기존 변경 보존

시작 시 변경: `.claude/settings.local.json`, 지도 UI 2파일, 개발 BuildProfile, URP 전역 설정, 설계·요청 README·월드 위키. 미추적: InitTestScene과 meta, 요청·설계 meta, 전달 폴더 전체, Blender 원본 4개, scratchpad, texture-scale-pass. 모두 보존한다. 이번 작업은 기존 씬과 지도 UI를 저장하지 않는다.

### 패키지

`ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `packages-lock.json` 대조: Unity **6000.3.21f1**, URP **17.3.0**, Addressables **2.9.1**, AI Navigation **2.0.14**. Splines·Terrain Tools는 manifest/lock에 없으며 설치하지 않는다. P01은 Terrain 기본 API만 사용한다.

### 실제 연결 지점

| 분야 | 실제 파일·타입·시그니처 | 후속 연결 판단 |
|---|---|---|
| 어셈블리 | `UPlayGround.Core/Data/Contracts/Actor/UI/EditorTools`, Manager는 기본 Assembly-CSharp | Core 숫자 계산은 무의존. 신규 Data/Editor 분리. Manager를 하위 모듈에서 직접 참조하지 않는다 |
| 씬 전환 | `Manager/Scene/SceneManager.Load.cs`: `bool TryLoadScene(string sceneName,string arrivalId)`, `void LoadScene(string,Vector3)`, `OnSceneContextReady`, `OnLoadComplete` | 접수와 준비 완료 구분. P08에서 기존 Manager 쪽 어댑터 필요 |
| 청크 | `Manager/World/ContinuousWorldRegion.cs`: `void Request(bool required)`, `ToWorld(Vector3)`, `ToSource(Vector3)`; Addressables Additive/activateOnLoad:true, handle 소유·Unload | 기존 로더를 새로 복제하지 않고 어댑터 계약을 먼저 확정 |
| 진입 준비 | `ContinuousWorldController`: `IEnumerator PrepareEntry(Vector3,string)`, `UniTask<string> TravelAsync(string,string,Vector3?,CancellationToken)` | Ready 전 이동 차단을 기존 진입 절차에 연결 |
| 씬 컨텍스트 | `Manager/Scene/SceneContext.cs`: `MapID`, `WorldMapConfig`; `NotifySceneContextReady(this)` | P01 preview에는 배치하지 않음(게임 초기화 방지) |
| 세이브 | `Manager/Save/ISaveable.cs`: `ExportSaveData(GameSaveData)`, `ImportSaveData(GameSaveData)`, `ResetForNewGame()`; `SaveManager.RegisterSaveable(ISaveable)` | WorldRuntimeDelta는 기존 저장 어댑터로 전달. Core/Data에서 Manager 참조 금지 |
| 자산 | `Contracts/GameServices.cs`: `IAssetService.LoadSceneAsync<T>(string key,string owner,CancellationToken)`, `AcquireGlobalAsync<T>`; `IAssetLease<T>.Asset/Dispose` | 이 계약은 에셋 임대이며 씬 handle 계약으로 오인하지 않는다 |
| 미니맵 | `Data/UI/MapConfigDatabaseSO.GetConfig(string mapId)` → `MinimapIconConfigSO.backgroundSprite/captureCenter/captureWorldSizeXY`; `UI/HUD/Minimap/MinimapMarkerRegistry.Register/Unregister(MinimapMarkerRegistrar)` | 신규 mapId만 별도 등록. 현재 UI 미커밋 변경 보존 |
| 리스폰 | `InteractionRespawnManager.RegisterRegion(Scene)`, `UnregisterRegion(Scene)`, `RegisterRuntimePlacement(GatheringActor,string)`; `MonsterRespawnManager.RegisterRegion(Scene)`, `RecordDeath(MonsterActor,string)` | 청크 준비/해제에 기존 지역 등록 연결, 영속 ID 보존 |
| AI/내비 | `EnemyMovementController.CombatMovement.cs`: `NavMesh.SamplePosition/CalculatePath`; `NpcBrain.cs`, `PetFollowController.cs`도 NavMesh 직접 사용 | 별도 공용 Nav 서비스는 조사 범위에서 없음. P07 Editor Nav 어댑터 사용 |
| 카메라 | `Camera/Integration/CameraRuntimeServices.cs`의 `ICameraRuntimeAdapter`; `Assets/10.Datas/Camera/CameraOccluderNameRules.asset` | 프리팹·씬에 CameraOccluder 삽입 금지. P01은 Terrain만 생성 |

코드 경로는 `Assets/02.Scripts/` 기준. 신규 모듈에서 기존 Manager 싱글톤을 참조하지 않는다.

### 보행 제약과 경사 실측

- `Assets/03.Prefabs/PlayerActor_.prefab` 및 `Actor/Player_Raon.prefab`: KCC 반경 **0.35m**, 안정 경사 **60°**, step **0.5m**. 구 Player_Actor/Player_Model 및 MonsterActor_는 반경 **0.5m**, 경사60°, step0.5m. 프리팹 구성값이며 실제 입력 보행을 측정한 결과는 아니다.
- `ProjectSettings/NavMeshAreas.asset` Humanoid: 반경 **0.5m**, 높이2m, 경사 **45°**, climb **0.75m**, cellSize0.16666667m. KCC step보다 Nav climb이 크므로 후속 경로는 두 제한의 작은 값으로 검증한다.
- C RAW를 700m/간격1m로 전수 계산(내부 중앙차분, 외곽 단측차분): 고도 **1.2497~640.8469m**, 경사 중앙값 **30.3095°**, p90 **60.0411°**, p99 **73.9404°**, 최대 **83.2810°**. 12° 초과86.99%, 45° 초과25.03%, 60° 초과10.03%.
- [재현 도구](../../../Tools/WorldBuilder/audit.cjs), [측정 원본·B 24파일 SHA](../../../output/WorldBuilder/p00-audit.json). C SHA는 manifest의 `59c7d2f4…cacb85181`과 일치.
- 12°·폭4m는 설계 출발값이고 엔진 상한이 아니다. 폭4m는 플레이어 지름0.7m/Nav 지름1m보다 넓지만 실제 장애물·전투 여유는 미검증. **700m는 유지**, P04에서 필수 루트/거점에 국소 RoadGrade/Flatten 적용 및 실제 KCC/Nav 연결 검증. 전체 RAW를 보행 가능으로 간주하지 않는다.

## PLAN

| 단계 | 수정 예상 | 경계 |
|---|---|---|
| P01 | `Assets/02.Scripts/WorldBuilder/{Core,Data,Editor}/`, `Assets/Tests/WorldBuilder/EditMode/`, 별도 profile·source·preview 씬/TerrainData | Core는 Unity 참조 없음. SO는 `Assets/10.Datas/WorldBuilder/`. 파생물은 `Assets/WorldBuilder/Worlds/` |
| P02~03 | Core 연산/Dirty/ID·Editor 저작 세션, Data 원본·override | P02/P03 구현·검증 반영, 다음 P04 |
| P04~06 | 계획/예약/수계/동굴/바이옴 데이터와 Editor 생성 | 실제 동선 경사 문제 해소 필요 |
| P07~09 | Nav/Bake/검증·Runtime·Integration 및 Manager 쪽 어댑터 | 기존 씬·세이브 계약 보존, 빌드는 사용자 지시 때만 |
| P10 | 지역 계획/Lock/지역 검증 | B fixture와 T01~26 대체 금지 |

설계9장과 구조 충돌 없음. 설계0장/5장의 과거 미확정·2,800m 문구는 사용자 결정700m에 종속된다. 원본 manifest2,800m는 수정하지 않는다. Core/Data/Editor만 지금 생성하고 Runtime/Integration 빈 껍데기는 만들지 않는다.

## P01 · 구현 반영·자동 검사 통과 / 사용자 테스트 대기

### 코드와 저작 자산

- [Core](../../../Assets/02.Scripts/WorldBuilder/Core/): 불변 R16 `HeightField`, little-endian codec(남/북 행 명시), 유한값·범위 검사, 좌표 변환, 공유 경계를 포함한 tile 추출/병합. 서로 다른 경계는 무음 봉합 없이 거절한다. `WorldBuilder.Core`는 `noEngineReferences=true`다.
- [WorldProfile](../../../Assets/02.Scripts/WorldBuilder/Data/WorldProfile.cs): 기본 C2,048m/2049²/originY0/700m, tile512. B는 별도 프로필−80/480m다.
- [Editor importer](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldBuilderSampleImporter.cs): 방향 fixture 선검사, C의 경로·manifest·SHA 고정, PNG/북쪽 RAW 입력 거절. 원본 `.bytes`와 metadata JSON을 저장하고 신규 GUID 폴더에 Terrain16개·URP 중립 표면·방향 표식·미리보기 씬을 만든다. 기존 Published/월드는 건드리지 않는다. 새 타입/asmdef은 Hot Reload 지원 밖이므로 정상 컴파일했다.
- 사람이 쓰는 기본 자산 생성 진입점은 툴 런처에 올리지 않았다. MCP `execute_code`에서 `UPlayGround.EditorTools.UPlaygroundToolCatalog.TryExecute("WorldBuilder.ImportTechnicalSample", out error)` 또는 `WorldBuilder.ImportRegionalSample`로 실행한다. 매번 새 GUID로 생성하며 기존 월드를 덮어쓰지 않는다. 겹쳐 열린 미리보기는 검사 후 닫고, 저장된 씬을 별도로 열어 확인한다.

| 샘플 | 프로필 | 저장된 4×4 미리보기 |
|---|---|---|
| B 불변 기술 fixture | [−80/480m 프로필](../../../Assets/10.Datas/WorldBuilder/32742014596749a481700c6e6c9b0307.asset) | [B 씬](../../../Assets/WorldBuilder/Worlds/32742014596749a481700c6e6c9b0307/Preview/WorldPreview.unity) |
| C 지역 원본 | [0/700m 프로필](../../../Assets/10.Datas/WorldBuilder/35c2ca802bd64575961ac7cda330ac91.asset) | [C 씬](../../../Assets/WorldBuilder/Worlds/35c2ca802bd64575961ac7cda330ac91/Preview/WorldPreview.unity) |

각 월드 `Source/BaseHeight.json`에 적용 범위·원본 범위·checksum·프로필 경로를 기록했다. C 원본2,800m 기록과 원본 RAW는 보존한다. `Preview`는 파생 확인용이며 Published Bake가 아니다.

### 검증 결과

확인 주체는 AI의 Unity6000.3.21f1 API·Editor Test Runner·카메라 렌더다. 실제 입력·Player 실행으로 대체하지 않는다.

| 항목 | 결과·근거 |
|---|---|
| Unity 컴파일·리로드 | 정상 컴파일 성공, 신규 Core/Data/Editor/Test4개 어셈블리 로드 확인. [타입 조회](../../../output/WorldBuilder/compiled-types-final.json), [컴파일 요약](../../../output/WorldBuilder/compile-summary.txt) |
| 관련 EditMode | **11/11 통과, 실패0·skip0**. [NUnit 원본](../../../output/WorldBuilder/EditMode-final.xml), [MCP 결과](../../../output/WorldBuilder/test-results-complete.json), [테스트 코드](../../../Assets/Tests/WorldBuilder/EditMode/HeightmapTests.cs) |
| T01/T02 | 33² fixture 네 모서리, 전체 ushort65536값 왕복, B RAW 전체 checksum 왕복·16타일 fixture 바이트 일치·split/join 통과 |
| T03 | Unity GetHeights 최대 오차 B **0.00733454m**, C **0.01069620m**. RAW 재출력 B0.00732433m/C0.01068132m. 허용0.03m 이내 |
| T04 수치·저장 | 공유 경계 바이트 일치, Unity 경계 차이 **0m**. B/C각16개 저장·닫기·다시 열기 후 이웃·충돌체 원본·513height/512holes·네 모서리 검사 통과. [저장/재로드 원본](../../../output/WorldBuilder/saved-reload-results-final.json) |
| T27/T28 | PNG와 북쪽 RAW를 Terrain 입력에서 거절. C2049²·SHA·북쪽 참조를 한 번 뒤집은 바이트 동일성 통과. 이미지7종을 지역 Profile 참조로 연결하는 작업은 P10에 남김 |
| 방향 화면 | Unity 카메라로 [B](../../../output/WorldBuilder/B-overview.png)·[C](../../../output/WorldBuilder/C-overview.png) 촬영·AI 확인. +Z가 위, +X가 오른쪽이며 C 북부 산지 방향과 숫자 모서리 검사를 대조. 중립 표면의 기술 미리보기이고 미술 인수 아님 |
| 보존 | B 원본24파일 SHA 재대조 차이0. 기존 LakeVillage `dirty=false` 확인. 기존 씬·프리팹·패키지·Addressables 키 변경 없음 |
| 독립 QA | 별도 qa_reviewer가 코드·asmdef·원본 SHA·재로드 근거를 검토, 확정 결함 없음. Unity 슬롯이 없어 직접 실행하지 않았으며 실행 결과는 리드가 별도로 확인 |

마지막 중립 텍스처 영구 저장 보완은 수치 코어를 바꾸지 않았으므로11개 수치 검사를 다시 반복하지 않았다. 해당 변경은 정상 컴파일·실제 저장/재로드 후32개 Terrain의 Material/Shader/TerrainLayer/영구 Texture 참조 검사·Unity 렌더로 확인했다. 최신 재로드 근거와 최종 캡처가 이 보완을 포함한다.

### 작업 중 발견·수정한 실패

1. Test Runner의 제목 없는 임시 씬에서 `NewScene(Additive)` 거절(8/10 통과), runtime `CreateScene`은 EditMode 미지원(8/11 통과). [첫 XML](../../../output/WorldBuilder/EditMode.xml), [두 번째 XML](../../../output/WorldBuilder/EditMode-attempt2.xml). 전용 PreviewScene과 저장 제외 임시 객체 생성으로 수정 후11/11 통과. 실패 입력에서 기존 활성 씬/dirty·생성객체0 보존도 검사한다.
2. 첫 저장/재로드에서 `SetNeighbors`가 유지되지 않는 것을 발견했다. Profile GUID 기반 고유 groupingID와 `allowAutoConnect=true`로 수정. 새 설정을 저장한 뒤 추가 쓰기 없이 재로드해 검사했다. 이전 실패를 통과로 승계하지 않았다.
3. 최초 기본 Terrain이 URP에서 magenta로 표시됐고 캡처에 기존 씬도 섞였다. 전용 URP Terrain/Lit 표면·실제 저장 텍스처와 촬영 전용 레이어 분리로 수정했다. 원본 씬의 조명/설정은 바꾸지 않았다. 최초 실패 캡처는 `output/WorldBuilder/*-overview-before-surface.png`에 남겼다.
4. B/C 동시 임포트 호출은 MCP 응답 시간이 초과했으나 실행은 완료됐다. 재임포트하지 않고 생성된 씬2개·Terrain32개·원본 metadata를 조사하고 저장/재로드 검사로 후속 확인했다.

### 남은 사용자 확인·다음 단계

| ID | 대상·조작 | 기대 결과·실패 기준 |
|---|---|---|
| WB-P01-01 | B/C 씬을 **각각 단독으로** 열고 Scene View에서 네 모서리와 타일 경계를 근/원거리로 이동 | 방향 표식이 남서/남동/북서/북동과 일치. 카메라 거리·LOD 변화 시 벌어짐/튀는 경계가 없어야 함. 현재 고정 캡처와 수치만 확인했으므로 **사용자 테스트 대기** |
| WB-P01-02 | 후속 플레이어 진입 준비가 끝난 뒤, 키보드·게임패드로 타일 경계 횡단 및 급경사 접근 | 충돌 틈/낙하/걸림이 없어야 함. P01은 캐릭터·씬 컨텍스트·Nav를 연결하지 않으므로 지금 실행한 것으로 보고하지 않음 |
| WB-P01-03 | C 전체 높이 규모와 산지 실루엣 검토 | 미술 품질·조망·700m 체감은 **사용자 확인 대기**. 원본 경사를 보행 가능 판정으로 쓰지 않음 |

P02 진행은 아래 기록을 따른다. P03 이후는 미구현. Player/Addressables 빌드·커밋·push는 지시가 없어 수행하지 않았다. PlayMode, 동적 LOD/실제 KCC 경계 통과, import 중간 엔진 실패 주입은 미수행이다. 입력 오류 롤백 검사와 실제 중간 실패 복구 검증을 구분한다.

### 부산물

보존: 두 월드의 원본 사본·TerrainData/표면·씬·meta(다음 단계 입력), 프로필2개, `output/WorldBuilder/`의 소형 XML/JSON/로그/캡처(검증 근거), [준비표](../../../output/harness/20261116-214158-58d364b7/review.md). 원본 전달 폴더·구매/Blender 파일은 그대로다. 재생성 임시 프로젝트·보조 DLL·Player 빌드는 만들지 않았다. 불필요한 초기 로그 전문은 정리하고 컴파일 요약을 보존한다. 마지막 측정 용량과 씬 복원 결과는 아래 최종 확인에 기록한다.

## P01 최종 확인

- [최종 Editor 상태](../../../output/WorldBuilder/final-editor.json): Play/컴파일 중 아님. 시작 때의 LakeVillageWorld 하나만 열림, `dirty=false`. 새 미리보기2개는 임시 촬영 상태를 저장하지 않고 닫았다.
- 월드 파생 자산·Source/meta **34,650,367바이트(약33.05MiB)**, 검증 근거 폴더 **5,876,390바이트(약5.60MiB)** 보존. 삭제는 이번 작업의 불필요한 초기 로그 전문1개뿐이다.
- 로컬 문서 링크65개 존재 확인, B24파일 SHA 재확인 통과. 기존 tracked 변경 목록에 새로 섞인 씬·프리팹·패키지 변경 없음. 사용자 기존 변경은 그대로다.
- [최종 산출물 준비표](../../../output/harness/20261116-220737-eac23e19/review.md). P00/P01 결과를 [월드 위키](../../wiki/world-art.md)에 연결했다.

## P02 · 비파괴 저작 레이어 · 구현·자동 검사 통과 / 사용자 테스트 대기 (2026-11-16)

### 범위·연결

- [저작 가이드](../../../Assets/docs/guide/WORLDBUILDER_TERRAIN_AUTHORING_GUIDE.md). 툴 런처 **월드 / 맵 → WorldBuilder 지형 저작** (`WorldBuilder.Authoring`).
- Core: 불변 TerrainOperation, Float32 m 높이·별도 Hole 셀 결과, AddDelta/Absolute/Flatten/Smooth/HoleCut/ProtectRegion, stage/priority/sequence/GUID 정렬, 부분 재평가·halo, Dirty DAG 폐쇄, WBTS/WBTP v1 바이너리.
- Data: `WorldDefinition`이 Base SHA·프로필 의미의 sourceRevision·명령/패치 참조·편집 세션·적용 내용 hash·Dirty 영역/항목을 보관한다. 큰 패치 배열은 SO에 직접 넣지 않는다.
- Editor: 명령·Sculpt·RAW 세션과 실제 Terrain 어댑터, Unity Undo 후 명령 재평가 및 물리 스냅샷 복원, 입력 오류/저장 오류 시 타일별 복원 경로. 원본 Base는 교체하지 않는다.
- 시작 Editor는 C 미리보기 하나, `dirty=false`였다([조회](../../../output/WorldBuilder/P02/editor-before.json)). 기존 P01·사용자 미커밋 변경을 보존한다. 작업 도중 별도 `BuildManagerWindow.cs`, `vegetation-build-black.md` 변경이 관측됐으나 이번 범위에서 수정하지 않았다.

### 발견·수정 및 검증 진행

1. 독립 QA에서 **높은 priority/stage의 기존 명령이 새 캡처를 덮는 결함**을 확인했다. 새 캡처는 기존 높이 스택의 최대 stage/priority 뒤 sequence로 넣고 Sculpt/RAW Absolute 재현 회귀를 추가했다.
2. **다른 월드·이전 세션 RAW 수용**을 막기 위해 `.raw.json`의 월드/원본/스택/세션/export hash/수치/행 규약을 적용 전에 대조한다. 불일치 때 명령과 지형이 바뀌지 않는 회귀를 추가했다. 수정 후 독립 재검토에서 추가 확정 결함 없음(실행 검증은 리드 별도).
3. 첫 통합 실행 **14/35 통과, 21실패**. 내부 n² 작업 버퍼의 Hole을 (n−1)² 결과로 변환하지 않은 Core 결함이었다. Build 경계에서 명시적으로 셀 그리드로 복사하도록 수정했다. [실패 XML](../../../output/WorldBuilder/P02/EditMode-attempt1.xml), [실행 원본](../../../output/WorldBuilder/P02/test-results-attempt1.json).
4. 다음 실행 **24/35 통과, 11실패**([XML](../../../output/WorldBuilder/P02/EditMode-attempt2.xml)). Unity 인라인 직렬화가 비활성 세션 null을 빈 객체로 만드는 문제를 `SerializeReference`로 수정했다. TerrainData는 재임포트 시 C# wrapper가 바뀌므로 참조 검증·실패 복원은 자산 GUID와 현재 Collider 연결을 사용한다. 이후 **35/35 통과**([XML](../../../output/WorldBuilder/P02/EditMode-35-passed.xml)).
5. 전역 Undo 이벤트가 다른 객체·네이티브 브러시에도 현재 월드를 재생성하는 결함을 추가 수정했다. 이 월드 명령 스택이 바뀌지 않았으면 아무 것도 적용하지 않는다. 비교 기준은 생성자·명령 성공·Undo 성공에서 갱신하며 실패 시 이전 값을 유지한다. 다른 객체 Undo 2건과 Sculpt 중 native Undo/Redo 회귀를 추가했다.
6. 긴 실행 중 MCP 결과 조회가 끊긴 경우 실행을 재요청하지 않고 NUnit 원본과 같은 job ID를 확인했다. [최종 복구 결과](../../../output/WorldBuilder/P02/test-results-final-recovered.json)가 XML과 일치한다. 앞선 실패·통신 단절을 성공으로 바꾸지 않고 원본을 보존했다.

### 최종 코드·연결 자산

- [수치 명령·평가·Dirty·바이너리](../../../Assets/02.Scripts/WorldBuilder/Core/), [WorldDefinition](../../../Assets/02.Scripts/WorldBuilder/Data/WorldDefinition.cs), [편집 세션](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldTerrainAuthoringSession.cs), [Terrain 어댑터](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldTerrainStateAdapter.cs), [저작 창](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldBuilderAuthoringWindow.cs).
- [C 저작 정의](../../../Assets/10.Datas/WorldBuilder/35c2ca802bd64575961ac7cda330ac91_Authoring.asset)를 [기존 C 미리보기](../../../Assets/WorldBuilder/Worlds/35c2ca802bd64575961ac7cda330ac91/Preview/WorldPreview.unity)에 연결했다. 기본 명령0개·세션null. 실제 지형에 시연용 명령을 남기지 않았다.
- 툴 런처 ID 실행과 **WorldBuilder** 창 열기·정의 연결 확인([실행 원본](../../../output/WorldBuilder/P02/setup-results.json)). 씬은 `dirty=false`이며 기존 C 씬 하나를 유지한다. GUI의 수동 브러시 조작성 인수를 대신하는 증거는 아니다.

### 자동 검사·보존 결과

| 항목 | 실제 결과·근거 |
|---|---|
| 정상 컴파일·리로드 | 신규 타입/직렬화 변경으로 Hot Reload 지원 밖. 정상 컴파일 후 동작 확인, [Console 오류0](../../../output/WorldBuilder/P02/console-final.json) |
| 최종 EditMode | **38/38 통과·실패0·skip0**, 약204.62초. P01회귀11 + P02수치12 + Editor통합15. [NUnit 원본](../../../output/WorldBuilder/P02/EditMode-final.xml), [같은 job의 최종 결과](../../../output/WorldBuilder/P02/test-results-final-recovered.json) |
| T05 | 실제 Unity 네 타일 공유 모서리 수정·캡처·재생성 후 높이 일치, 전역 마스크 투영 일치 |
| T06/T07 | 같은 입력3회·4×4 정/역/병렬 타일 지역 평가 hash 동일. 추가·삭제·이동·마스크·Smooth halo 포함 Dirty/Full 결과 bit 동일 |
| T08 | Sculpt와 RAW Delta/Absolute가 캡처 기준 Final에 한 번만 적용되고 같은 Base 재생성3회에서 보존. 높은 stage/priority 뒤 캡처·세션/sourceRevision 충돌·metadata 불일치 거절·자산 rebind 검사 포함 |
| T09 | 미캡처 지형을 Regenerate/Bake 입력에서 거절. Capture 또는 명시적 Revert 후 해소 |
| T10의 P02 범위 | 높이와 Hole **각20회 Undo/Redo** 후 수치 hash·TerrainData GUID·Collider 참조 유지. 타 객체 Undo와 native 브러시 Undo가 현재 명령 스택을 재생성하지 않음 |
| 기타 | HoleCut 합집합/삭제·보호 합집합·불변 원본/배열·잘못된 수치/격자/중복 ID/손상 바이너리/고도 범위 초과 거절. 잘못된 명령이 SO·네이티브 지형을 변경하지 않는지 확인 |
| P01·사용자 보존 | C 도구 연결 전후 P01 자산·Source·meta **94파일 SHA 변경0**([결과](../../../output/WorldBuilder/P02/p01-preservation.json)). B 전달 원본24파일도 P00 해시와 동일. 현재 C 씬 저장 상태 유지, 기존 게임 월드·씬 로더·세이브·Addressables·패키지 변경 없음 |
| 독립 QA | 구현과 분리된 qa_reviewer가 코드 및 최신38건 XML·setup·94파일 보존 근거 확인. 미해결 확정 결함 없음. QA는 Unity를 직접 실행하지 않았으며 리드의 실제 원본을 확인함 |

### 사용자 테스트 대기·미수행

| ID | 조작 | 기대 결과·실패 기준 |
|---|---|---|
| WB-P02-01 | C 씬에서 연결된 WorldBuilder 창을 열고 Sculpt 시작 → `(1024,1024)` 주변 공유 모서리를 기본 Terrain 브러시로 수정 → 캡처 → 재생성 → Undo/Redo | 수정 한 번만 보존, 경계 단차·명령 소실·다른 객체 Undo 간섭 없음. 실제 브러시/Scene View 조작성은 사용자 확인 대기 |
| WB-P02-02 | 새 RAW/JSON export를 외부 편집기에서 R16 LE/SouthFirst로 수정 후 같은 세션에 가져오기. Delta와 Absolute 각각 확인 | Base 불변, 변경 부분만 캡처. JSON 누락·다른 세션은 거절. 실제 외부 편집기 UX 확인 대기 |
| WB-P02-03 | 미캡처 변경 상태에서 재생성/입력 검사 → 차단 확인 → Capture 또는 명시적 Revert. 세션 저장 후 실제 도메인 리로드도 별도 확인 | 자동 덮어쓰기 금지, 재연결 후 기준·패치 참조 유지. 자산 rebind는 자동 검사했지만 실제 도메인 리로드 실험은 미수행 |

중간 엔진·디스크 실패 주입은 미수행이다. 입력 오류의 무변경 검사와 실제 중간 실패 복구 검증을 혼동하지 않는다. P01의 실제 KCC·LOD·미술 인수도 여전히 사용자 대기다. PlayMode/Player/Addressables 빌드·커밋·push는 수행하지 않았다.

### 정리·다음 단계

- 테스트가 만든 UUID 월드/자산·RAW/JSON은 각 TearDown에서 정리했다. `Assets/WorldBuilder/Worlds/`에는 기존 B/C 두 폴더만 남는다. C의 원본·프로필·TerrainData·씬을 덮어쓰지 않고 별도 저작 SO1개만 추가했다.
- `output/WorldBuilder/P02/`의 실패/성공 XML·JSON·소형 로그는 재검증 근거로 보존한다. 임시 프로젝트·보조 DLL·설치 캐시·Player 빌드는 만들지 않았다.
- `Library/WorldBuilder/`의74파일/22,521,504바이트는 현재 C의 정상 적용 복원 캐시(약20MiB)와 작은 검사 캐시다. 기존 Library 보존 규칙에 따라 임의 삭제하지 않았다. 이 캐시는 저작 Base나 Published가 아니다.
- [P02 준비표·최종 결과](../../../output/harness/20261116-232400-d08f82d1/review.md)에 위키 연결·검증 범위·정리/보존 내역을 기록했다. 원본 실행 로그는 `output/WorldBuilder/P02/`에 유지한다.
- **다음은 P03** 후보 격자·안정 ID·공간 해시·필터·Override다. 실제 식생 재생성 보존(T08 확장)과 건물/물 Undo(T10 확장)는 해당 기능 구현 단계에 이어 검증한다.

## 사용자 제보 · 미리보기 지형 광택 수정 (2026-11-16)

- 제보: C 지형이 금속/젖은 표면처럼 번들거림(S3, Scene View). 원인은 임시 RGBA 텍스처의 알파1을 `DiffuseAlphaChannel` smoothness로 읽은 것이다. `layer.smoothness=0`만으로는 알파 경로를 막지 못했다.
- 수정: B/C 전용 TerrainLayer를 `TerrainLayerSmoothnessSource.Constant`, `smoothness=0`으로 저장. `WorldBuilderSampleImporter.ApplyPreviewSurface`에도 같은 설정을 넣어 새 생성/재적용 시 유지한다. 공용 URP 셰이더·조명·지형 수치는 변경하지 않았다.
- 검증: 정상 컴파일/리로드, Unity 실제 API 저장([설정 결과](../../../output/WorldBuilder/Matte/apply-result.json)), 같은 Scene View의 [수정 전](../../../output/WorldBuilder/Matte/before-matte-1.png)·[수정 후](../../../output/WorldBuilder/Matte/after-matte.png)를 AI가 확인해 흰 정반사 반짝임 제거 확인. TerrainData32개+Base2개 SHA 변경0([보존 결과](../../../output/WorldBuilder/Matte/geometry-preservation.json)), C 씬dirty=false.
- 매끄러움 설정 보정이므로38개 수치 테스트·Play/빌드는 반복하지 않았다. 미술 최종 인수는 사용자 확인 대기. 작은 캡처/JSON/컴파일 요약은 근거로 보존하며 임시 프로젝트·새 캐시 없음.

T10의 높이·Hole 각20회는 P02 범위다. 건물 이동/삭제·물 경로 Undo는 P04/P05 이후, T08의 실제 식생 재생성은 해당 후속 구현 이후에 검증한다. 중간 엔진/디스크 실패 주입·실제 도메인 리로드 중 세션 복원은 별도 미수행 항목으로 남긴다. 이번 단계에 빈 런타임/Bake/식생 구현을 추가하지 않는다.

## P03 · 공통 배치 기반 (2026-11-17)

**판정: P03 구현 반영·자동 검사 통과 / 조작·미술 인수는 사용자 테스트 대기. P04 이후는 미구현이다.**

### 코드·데이터 계약

- [Core 배치 엔진](../../../Assets/02.Scripts/WorldBuilder/Core/PlacementEngine.cs), [공통 레코드·ID](../../../Assets/02.Scripts/WorldBuilder/Core/PlacementTypes.cs), [공간 해시·지지면](../../../Assets/02.Scripts/WorldBuilder/Core/PlacementSpatialHash.cs): 전역 후보 셀·slot, canonical key 버전1, SHA-256 앞128bit ID, Base64 sourceIdentity 원문, seed/salt 별도 난수 키. 타일 순서·병렬·규칙/Dictionary 순서에 배치 결과가 의존하지 않는다. 중복 ID·충돌은 오류다.
- [Data](../../../Assets/02.Scripts/WorldBuilder/Data/): `PlacementCatalog`, `PlacementRuleAsset`, `PlacementRecord`와 `WorldDefinition`의 배치 seed/epoch·Override·Reservations·파생 결과. 실제 프리팹 GUID와 참조를 대조한다. 수치·리소스는 SO에서 관리한다. asmdef 참조·경계는 변경하지 않았다.
- [Editor 트랜잭션](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldPlacementAuthoring.cs): Manual/Pin/Transform/Delete/Exclude/Replace와 소유 마커가 있는 별도 프리팹 계층을 Undo 그룹으로 갱신한다. 실패하면 SO·장면을 복원한다. 생성 컨테이너에 추가한 객체·프리팹 내부/마커 수정·미캡처 루트 변환은 재생성 전에 차단한다. 가까운 새 대상에 자동 연결하지 않는다.
- [P02 연결](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldTerrainAuthoringSession.cs): 지형 명령의 Dirty 전파 뒤 같은 트랜잭션에서 배치를 재평가·저장한다. `sourceRevision`은 Base·좌표 의미 그대로이며 배치 seed/epoch를 포함시키지 않았다. Inspector 직접 seed 편집도 적용 당시 epoch와 대조한다. 기존 데이터·Sculpt/RAW 세션을 초기화하지 않는다.
- 최종 고도·경사·Hole·사각 예약/태그 제외·회전 OBB 점유·최소 간격·footprint 지지면을 실제 평가한다. Terrain 이외 surfaceId는 거절한다. 수계·바이옴·중요 구조물 Physics/출입구 검사는 이번 단계에 없으며 통과를 가장하는 빈 검사도 없다.
- **Dirty는 보수적으로 전역 후보를 다시 평가한다.** 간격 충돌 연쇄와 모든 이웃 halo를 포함해 Full과 같게 만든다. 부분 갱신 성능 최적화는 미구현이다. 수직/yaw 배치이며 비균일 배율은 Catalog 허용 때만 가능하다. 규칙당 후보 한도1,000,000과 창 상세256건 제한은 [저작 지침](../../../Assets/docs/guide/WORLDBUILDER_PLACEMENT_AUTHORING_GUIDE.md)을 따른다.

### 검증 씬·여는 방법

| 대상 | 경로·확인 |
|---|---|
| 별도 합성 월드 | [P03Validation.unity](../../../Assets/WorldBuilder/Worlds/d303d303d303d303d303d303d303d303/P03Validation.unity). **2,048m·2049²·originY0·700m·Terrain16개**, 첫 범위 X256~768/Z0~512의512m 구역이 X512 타일 경계를 가로지른다 |
| 저작 SO | [Definition](../../../Assets/10.Datas/WorldBuilder/P03Validation/Definition.asset), 같은 폴더의 Catalog·Rule·Profile. 프리팹·합성 RAW·파생 TerrainData는 별도 월드 폴더에 저장 |
| 실행 | 툴 런처 **월드 / 맵 → WorldBuilder 지형 저작**, 위 Definition 연결 → **P03 · 공통 배치**. 최종 Editor에 이 씬과 연결된 창을 열어두었다. P01용 저작 정의 생성 버튼 대신 준비된 Definition을 연결한다 |
| 실제 배치 | 기본 Cube 소품250개, Pin1·수동 배치1·Delete tombstone1. X512 경계 양쪽123/127개, 모든 쌍의 footprint/spacing 검사 통과. Delete 대상 없음. 재생성 전후 hash 동일, Pin XYZ/ID 유지, 오류0. [API 원문](../../../output/WorldBuilder/P03/fixture-result.json), [재생성](../../../output/WorldBuilder/P03/native-regenerate.json) |
| 화면 | [전체512m 구역](../../../output/WorldBuilder/P03/overview.png)·[경계/고정 footprint](../../../output/WorldBuilder/P03/boundary.png)·[Pin 근접](../../../output/WorldBuilder/P03/pin.png)을 AI가 실제 Unity 카메라 캡처로 확인. 미술 인수를 대체하지 않는다. 무광 `Constant/0` 유지 |

### 실제 실행한 검증

| 검사 | 결과·원본 근거 |
|---|---|
| 정상 컴파일·리로드 | 같은 `UPlayground@fddd0702`, Unity6000.3.21f1 Edit Mode. 새 타입/직렬화/Editor 코드는 Hot Reload 지원 밖이므로 정상 컴파일. 컴파일 완료 후 Console 오류0과 후속 조회 확인 |
| **최종 관련 EditMode** | **60/60 통과, 실패0·건너뜀0, 234.80초**. 기존 P01/P02 회귀38 + 배치 Core16 + 실제 씬 통합6. [최종 실행 원문](../../../output/WorldBuilder/P03/editmode-final.json), job `a189184942a64b04b9746e704394bee2`. 현재 코드34파일 SHA 불일치0([입력](../../../output/WorldBuilder/P03/code-hashes.json), [최종 대조](../../../output/WorldBuilder/P03/code-preservation.json)) |
| T06 | 같은 입력3회·타일 정/역/병렬·규칙 순서 변경의 canonical 배치 해시 일치 |
| T07 | 국소 높이 변경 뒤 P02 실제 Dirty→배치 갱신과 Full 일치. Definition 디스크 본문/강제 재임포트·sourceRevision 보존 확인. 2km 씬 입력에 별도 HoleCut 수치 평가도 수행: Hole 거절1, Dirty/Full 해시 일치, 저작 데이터·씬 미변경([원문](../../../output/WorldBuilder/P03/hole-dirty-full.json)) |
| T11·Undo | Delete 같은 epoch 재등장 방지·Pin 보존·seed/epoch와 rule 소실 orphan 보고. **Delete와 Move/Rotate/Scale 각20회** 실제 객체/SO Undo/Redo 확인. 실패한 프리팹 교체·seed 변경은 전체 복원 |
| T12·필터 | 고정 회전 footprint 침범 없음·경계 양쪽 spacing 일치. 독립 좌표 yaw 반례, 높이/경사/Hole/예약/지지/표면 거절, Exclude 태그, mandatory 오류/장식 경고, 중복 ID·손상 입력 검사 |
| P02 결합 실패 복구 | 지형 적용·Definition 저장 후 Pin 지지 충돌을 일으켜 명령·native 지형·배치 전체 복원 확인. 생성 컨테이너에 추가한 소품·프리팹 자식 편집이 재생성에서 유실되지 않고 차단됨 |
| 보존 | 시작 [git status](../../../output/WorldBuilder/P03/baseline-status.txt)·[145파일 SHA](../../../output/WorldBuilder/P03/baseline-hashes.json) 기록. B/C·전달본 **변경/누락0**([최종 대조](../../../output/WorldBuilder/P03/preservation.json)). 기존 C Definition/프로필·B/C 씬·원본·구매 에셋·패키지·Addressables 키를 수정하지 않음. 작업 중 관찰된 추가 NPC 변경도 다른 작업으로 보존 |
| 독립 QA | 구현 맥락과 분리한 읽기 전용 qa_reviewer가 diff·계약·통합6건 원문·34코드/145원본 SHA를 대조. 발견한 회전·저장·Redo·수동 자식 보존 문제 수정 후 근거 확인. [검토 기록](../../../output/WorldBuilder/P03/qa-review.md). QA가 Unity를 직접 실행한 것으로 보고하지 않는다 |

실패를 통과 기록에서 숨기지 않았다. [첫60건 실행](../../../output/WorldBuilder/P03/full-regression-first.json)은57/60으로, 첫 Redo에서 old PlacementHash가 남는2건과 잘못된 검증 이동 좌표1건이 실패했다. SO 최종 상태/Flush를 객체 생성 전에 기록하고 생성 계층·marker·instance 수정 기록을 추가해 해결했다. 이후 [통합6/6](../../../output/WorldBuilder/P03/integration-final.json), 위 최종60/60을 재실행했다. 그 전 fixture 준비 실패(미저장 Test Runner 씬·scene 전환 중 SO 참조 해제·기본 Prefab Override 오인) 원문도 같은 P03 폴더에 보관한다. 같은 Editor의 긴 동기 작업 중 MCP 통신 제한 시간이 초과됐지만 실제 완료/파일/후속 조회를 확인했으며 생성 명령 중복 실행·Editor 재시작으로 해결하지 않았다.

### 사용자 테스트 대기·위험·정리

- [WB-P03-01~06 조작 명세](../../../Assets/docs/guide/WORLDBUILDER_PLACEMENT_AUTHORING_GUIDE.md#사용자-테스트-명세): 창 조작성·실제 Scene 선택/Undo·삭제/Pin/변환/제외/교체·경계 시각 확인. 자동 API·수치·캡처 확인과 사람 인수를 구분한다.
- 기존 P02 브러시/외부 RAW 편집기 UX·실제 도메인 리로드 중 활성 세션 복원·중간 디스크 실패는 미수행/사용자 테스트 대기 그대로다. 실제 KCC·동적 LOD·미술 인수도 승계 완료하지 않는다. PlayMode·Player·Addressables 빌드·커밋·push는 수행하지 않았다.
- 부분 배치 재생성 성능 최적화·수계/바이옴·비 Terrain 지지면·중요 구조물 물리 검사는 후속 범위다. 자동 배치가 적거나0인 경우도 거절 개수/Warning/Error를 확인해야 한다.
- 테스트 UUID 월드·SO·RAW는 TearDown/실패 정리에서 제거했다. 최종 `Assets/WorldBuilder/Worlds/`는 기존 B/C + 별도 P03 한 폴더다. 합성 검증 씬·SO·프리팹·원본/meta는 재현용으로 보존한다. 새 임시 프로젝트·보조 DLL·패키지 설치 캐시·빌드는 만들지 않았다.
- 기존 `Library/WorldBuilder/`는 최종193파일/45,988,368바이트로 Undo 적용 복원 캐시이며 삭제하지 않았다([실측](../../../output/WorldBuilder/P03/library-cache-size.json)). 원본·Published가 아니다. P03 결과 JSON·소형 캡처·QA 기록은 인용 근거로 보존한다.
- **다음 P04 범위:** Region/POI/Reservation 계획, Plot/RoadGrade, 건물 metadata, 도로·교량 연결과 T13 접근/필수 동선 검증. 이번 요청에서는 착수하지 않는다. 실제 식생 아트·수계 제작도 수행하지 않았다.
- [P03 최종 준비표·검증/정리 인계](../../../output/harness/20261117-222549-a8744117/review.md). 최종 산출물/meta SHA와 링크를 대조했고, 준비표 확인 시 입력 변경0을 확인했다. 이후 상태 문서에는 이 인계 링크만 추가했다.
- 최종 P03 관련 로컬 링크·앵커 **44개/실패0**, `git diff --check` 통과, 마지막 Console 오류0을 확인했다. 결과는 `output/WorldBuilder/P03/link-check.json`에 있다. 검증 자산 두 폴더 내부55파일/18,599,785바이트(상위 폴더 meta 별도)와 작은 실행 근거 약330KB를 보존했다.

<a id="p04-planning"></a>
## P04 · 계획·구조물 (2026-11-17~18, 최종 검증 중)

사용자 `다음 진행`에 따라 P04만 진행한다. P05 수계 생성과 P10 지역 완성은 이번 단계에 포함하지 않는다. P03의 과거60/60을 현재 변경본으로 승계하지 않고 관련 회귀를 다시 실행했다.

### 변경과 보존 계약

- [계획 엔진](../../../Assets/02.Scripts/WorldBuilder/Core/WorldPlanningEngine.cs), [다각형·전역 A*](../../../Assets/02.Scripts/WorldBuilder/Core/PlanningGeometry.cs), [도로 메시](../../../Assets/02.Scripts/WorldBuilder/Core/RoadGeometry.cs): Region/POI·예약 다각형, 부지 내부/명시 지지점·평균/최대 경사, falloff 포함 cut/fill, 출입 소켓, 계획 수위/여유고, 교대 폭 전체의 Hole/지지·접근 경사를 평가한다. 도로 면적 전체와 고정 footprint·보호 다각형·Hole 셀을 대조한다. 타일 절단점은 전역 정점·노멀·접선·누적 UV를 복제한다.
- [WorldPlan](../../../Assets/02.Scripts/WorldBuilder/Data/WorldPlan.cs)과 Structure/Road/BridgePlanningPreset SO가 저작 원본이다. 고도는 제안 후 명시 확정한다. 실제 Splines2.9.0의 열린 SplineContainer를 사용하며 사용자 캡처 경로는 Lock한다. banking은 명시 거절한다. 필수 교량 ID를 지정한 도로는 양 교대 사이 연속 구간이 실제 상판 폭/고도 안에 있어야 한다. 양 교대에 닿기만 하고 육지로 우회하는 고정 경로도 거절한다.
- 교량 Catalog의 halfZ×2/span, halfX×2/폭, pivotHeight0를 고정1mm 오차로 확인한다. 로컬 +Z 길이·중앙 XZ pivot·상판 Y0·단위 root 규격을 지원한다. 소켓 spanTolerance로 이 검사를 우회할 수 없다. 실제 프리팹/Collider와 메타데이터 일치는 별도 저작 인수 대상이다.
- [계획 트랜잭션](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldPlanningAuthoring.cs)은 계획 SO·소유 지형 패치/예약·P03 재평가·실제 구조물/도로 계층을 함께 적용한다. [창 없는 Undo 복원기](../../../Assets/02.Scripts/WorldBuilder/Editor/WorldPlanningUndoRestore.cs)가 native Terrain 스냅샷을 복원한다. 실패하면 해당 작업 전체를 되돌리며 사용자 추가 자식도 보존한다. 파생 패치/메시는 content hash 경로에 남겨 Undo 참조를 유지한다.
- 기존 WorldDefinition은 계획 참조·연산/예약 소유권만 확장했다. sourceRevision의 Base·좌표·Profile 의미, 기존 Sculpt/RAW/Override·원본 참조·GUID를 유지한다. 다른 정상 세션이 적용한 계획 패치는 열린 P02 세션의 논리 캐시에도 반영한다.
- [교량 진입 게이트](../../../Assets/02.Scripts/WorldBuilder/Runtime/WorldBridgeAccessGate.cs)는 양쪽 준비 신호 전, 참조 누락, 컴포넌트 비활성 상태에서 진입을 허용하지 않는다. 실제 청크 로더와의 연결은 P08 전이며 아직 수행하지 않았다. 검증 NavMesh는 전용 씬 기준 데이터이며 계획 편집 후 정식 Navigation Bake를 완료한 것으로 취급하지 않는다.
- 실제 수계·바이옴을 빈 검사로 통과시키지 않는다. P04는 명시한 계획 수위·다각형만 사용한다. 기존 B/C·게임 월드·구매 원본·패키지·Addressables 키를 변경하지 않는다. CameraOccluder 컴포넌트는 추가하지 않았다.

### 별도 검증 씬과 열기

1. [P04Validation.unity](../../../Assets/WorldBuilder/Worlds/d404d404d404d404d404d404d404d404/P04Validation.unity)를 연다. B/C/P03과 다른 합성 월드다.
2. 툴 런처 **월드 / 맵 → WorldBuilder 지형 저작**에서 [Definition.asset](../../../Assets/10.Datas/WorldBuilder/P04Validation/Definition.asset)을 연결하고 **P04 · 계획·건물·도로·교량**을 펼친다. [Plan.asset](../../../Assets/10.Datas/WorldBuilder/P04Validation/Plan.asset), [건물](../../../Assets/10.Datas/WorldBuilder/P04Validation/Structure.asset)·[도로](../../../Assets/10.Datas/WorldBuilder/P04Validation/Road.asset)·[교량 프리셋](../../../Assets/10.Datas/WorldBuilder/P04Validation/Bridge.asset)을 Inspector에서 연다.
3. 전체는 **2,048m×2,048m·2049²·originY0·verticalRange700m·16타일**이다. 첫 사용 영역은 x256~768/z0~512의512m 구역이다. 건물2·교량1·필수 연결 도로1·기본 소품248(Pin1)을 배치했다. 물 표면 아트는 없으며 계곡은 계획 수계다. TerrainLayer smoothness0과 무광 설정을 유지한다.
4. [저작·사용자 테스트 명세](../../../Assets/docs/guide/WORLDBUILDER_PLANNING_AUTHORING_GUIDE.md)의 WB-P04-01~05를 따른다. 창에는 POI 고도 제안(미적용)/이동·확정, 선택 Spline 캡처/Lock, 계획 구조물·도로 전체 삭제가 있다.

[최종 구역 화면](../../../output/WorldBuilder/P04/sector-final.png), [교량 접속](../../../output/WorldBuilder/P04/bridge-final.png), [출입구](../../../output/WorldBuilder/P04/entrance-final.png)를 AI가 열어 확인했다. 첫 화면에서 A*가 교량을 우회한 것을 발견해 `requiredBridgeGuid` 연결을 추가했다. [적용 전](../../../output/WorldBuilder/P04/sector-overview.png)도 실패/수정 근거로 보존한다. [계획/sourceRevision 보존](../../../output/WorldBuilder/P04/required-bridge-apply.json), [NavMesh GUID 보존](../../../output/WorldBuilder/P04/navigation-guid-preservation.json), [기존48개 meta 보존](../../../output/WorldBuilder/P04/required-meta-preservation.json)을 확인했다. 초기 [씬 생성](../../../output/WorldBuilder/P04/scene-create.json)과 [필수 연결 전 상태](../../../output/WorldBuilder/P04/scene-before-required.json)는 최종 상태와 구분한다.

### 실제 실행과 최종 판정

- 같은 `UPlayground@fddd0702` Editor에서 새 타입/직렬화/Editor 변경을 정상 컴파일했다. Splines2.9.0은 Editor PackageInfo에서 간접 Registry 의존으로 확인했으며 패키지를 설치/업그레이드하지 않았다.
- 최종91건 재실행 중: 기존 P01/P02 38 + P03 22 + P04 Core13/Editor7/메타데이터1/실제 Play 진입 Agent2 + Camera 이름 규칙8. job `09925d968cf9463a84a292db17ef24ea`. 현재 코드/asmdef56파일과 씬 자산 지문을 먼저 기록했다. 최종 원문과 지문 대조 후 자동 게이트를 판정한다.
- [앞선77/77](../../../output/WorldBuilder/P04/regression-77.json), [면적·cutfill·하위 단계 실패 복원15/15](../../../output/WorldBuilder/P04/strip-cutfill-rollback-pass.json), [필수 교량 Core13/13](../../../output/WorldBuilder/P04/required-bridge-core-pass.json), [교량 메타데이터·Agent2/2](../../../output/WorldBuilder/P04/preset-traversal-pass.json)를 실제 실행했다. 이전 단계의 통과를 이번 수정본으로 승계하지 않는다.
- [실패80건 실행](../../../output/WorldBuilder/P04/regression-80-first.json)은79/80이었다. Play 진입 후 검증 교량 누락을 확인해 실행 씬을 일시 명시하고 EnterPlayMode의 도메인 리로드 기대값을 현재 Editor 설정과 맞췄다. 실행 씬 설정과 다른 열린 Build Manager 창은 teardown에서 복원한다. 첫 Play의 외부 Localization 오류를 포함해 [실패/QA 기록](../../../output/WorldBuilder/P04/qa-review.md)을 보존하며 전체 오류 로그를 무시하지 않았다. 실제 활성 Sculpt 세션의 도메인 리로드 복원은 별도 미검증이다.
- [카메라 이름 규칙 보존](../../../output/WorldBuilder/P04/camera-rules-preservation.json): 기존72개 값/순서를 보존하고 검증용 House Auto/Interior·Bridge Solid·Road Solid 세 prefix만 추가했다. 기존 원본 GUID를 유지했다.
- 독립 QA는 파일 수정·Unity 슬롯 없이 구현/계약/실행 원문을 대조했다. 창 없는 Undo·최초 적용 Undo·첫 Redo·고정 footprint·좁은 Pin/Hole·교대 Hole·접근 경사·교량 메타데이터·필수 교량 우회 지적을 수정했다. 마지막 연속 상판 검사도 재검토했으며 추가 확정 차단 결함은 없었다. 최종91건·보존 지문 확인은 대기 중이다.

### 남은 확인과 정리

- WB-P04-01~05의 실제 창/Scene 조작성·시각 인수, 실제 KCC/TPS 캐릭터 조작과 카메라 가림은 **사용자 테스트 대기**다. Agent 자동 통과와 사람 인수를 구분한다.
- 기존 P02 실제 브러시/외부 RAW UX·활성 세션 실제 도메인 리로드 복원·중간 디스크 실패, 동적 LOD·미술 인수는 그대로 미수행/사용자 테스트 대기다. 실제 게임 로더·P08 스트리밍/Nav Bake·Player/Addressables 빌드·커밋·push는 수행하지 않았다.
- 검증 NavMesh는 기준 씬용이다. 임의 POI/도로 편집 뒤에도 자동 갱신된다고 가정하지 않는다. 도로/교량 위 비 Terrain surface anchor, 교량의 다른 축/pivot·경사 상판/도로 banking은 현재 지원 범위 밖이다. 기존 C의 가파른 실제 지역 주동선은 이번 합성 씬 통과로 완료 처리하지 않는다.
- 테스트 UUID 월드/데이터는 각 TearDown에서 제거한다. 파생 불변 자산과 Library/WorldBuilder 적용 스냅샷은 Undo 근거로 보존하며 Library/Temp를 지우지 않는다. 시작 미커밋 NPC/아트/P03 변경을 보존한다. 최종 용량·210개 원본 보존 대조·문서 링크·준비표 검사는 실행 후 아래에 기록한다.
- 다음 P05는 수계 계획 데이터·물 표면/흐름/수변 마스크와 관련 게이트다. P04의 계획 수위/교량 연결·Sculpt/Override/GUID를 보존하며 실제 수계 생성은 다음 요청에서 진행한다.

- 11월18일 재개 시 이전90건 job은 `Unknown job_id`, Editor 실행 중 검사도 없었다. [중단 기록](../../../output/WorldBuilder/P04/interrupted-final-job.json)을 남기고 완료 통과로 승계하지 않는다. [재개 지문](../../../output/WorldBuilder/P04/resume-preservation.json)에서 코드56/기존 원본210, [별도 검증 자산98개](../../../output/WorldBuilder/P04/scene-preservation-resume.json) 모두 변경0을 확인했다.
- 마지막 SaveMesh 가드는 GUID→path→Mesh 타입/내용을 확인하며 다른 SO/미식별 기존 파일을 덮어쓰지 않는다. [다른 타입 자산의 객체·GUID·파일 SHA 보존1/1](../../../output/WorldBuilder/P04/asset-type-preservation-pass.json)을 실행했다. 현재 source56개 지문을 다시 기록해 최종91건을 시작했다.
- 중단된 테스트 UUID `38c3d5874e2044eba520980be59946ee`는 정의의 WorldGuid·128m 규격·미사용 여부·절대 경로를 확인하고 전용 Asset API로만 정리했다(61파일/382,841바이트, [정리 원문](../../../output/WorldBuilder/P04/interrupted-fixture-cleanup.json)). 기존 B/C/P03/P04 검증 씬은 정리 대상이 아니다.
