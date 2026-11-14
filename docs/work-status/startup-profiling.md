# Boot → 타이틀 → Loading → 메인 월드 프로파일링

- 기준: [런타임 성능 가이드](../../Assets/docs/guide/RUNTIME_LOG_PERFORMANCE_GUIDE.md), [테스트 지침](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md), [개발 위키](../wiki/development.md)
- 일자: 2026-11-13. Unity 6000.3.21f1의 열린 Editor 1개, MCP 실행. Player·Addressables 빌드 없음.
- 현재 단계: 계측 복구·외곽선 재질 공유·고정 NPC 외형9종 정리·월드 정적 배칭 플래그 개선·Boot 실패 취소·종료 해제를 반영하고 관련 자동 검증/실제 경로를 확인했다. **전체 병목 해결 완료는 아니다.** 60초 Boot 실패는 재현되지 않아 과거 원인은 미확정이다. Impostor 대안과 디더 템플릿은 화면 차이 또는 전체 이득 근거 부족으로 정규 반영하지 않았다. 실제 입력·시각적 품질은 사용자 테스트 대기다.

## 측정 조건과 첫 실행

Boot에서 시작하고 타이틀의 새 게임 처리와 캐릭터 선택 Confirm을 MCP C# 호출로 실행했다. 기본 선택 Raon으로 Loading을 거쳐 LakeVillageWorld의 SceneLoadState.Completed와 활성 캐릭터 Raon을 확인했다. 실물 입력·화면 품질 확인은 아니다. 기존 저장 슬롯을 로드·저장하지 않았다.

CPU·Rendering·Memory·Physics 영역, Deep Profile 및 할당 호출 스택 꺼짐. MCP manage_profiler 시작은 엔진 Profiler.enabled만 켜고 Editor ProfilerDriver.enabled는 꺼진 상태여서 최초 Boot CPU 프레임은 확보하지 못했다. 이후 ProfilerDriver를 직접 켜 실제 RawFrameDataView 프레임을 확인했다. 패키지 캐시는 수정하지 않았다.

- 첫 Boot 매니저 비동기 경과 시간: PartyManager 38,471 ms, SoundManager 36,759 ms, MonsterCodexManager 35,738 ms, ActorSpawnManager 33,446 ms. 병렬 경과 시간이므로 합산하거나 해당 매니저 자체 CPU 비용으로 해석하면 안 된다.
- 타이틀 2,000프레임: 중앙 16.725 ms, p95 19.463 ms. PlayerLoop 합계/프레임 약 5.049 ms(업데이트·렌더링 포함). EditorLoop와 대기 포함 전체 프레임을 Player 성능으로 해석하지 않는다.
- 첫 전환: NPC Awake 합계 6,910 ms, 그 아래 디더 Awake 52회 합계 4,931 ms, 재질 drawer 호출 합계 6,186 ms. 중첩 샘플은 합산하지 않는다.
- 첫 전환 수집에서 GC.Alloc 샘플 1,041만 개를 문자열로 내보내는 방식이 EditorLoop를 크게 오염시켰다. 총 전환 약 83초는 성능 개선 기준으로 사용하지 않는다. 다음 수집은 짧은 샘플의 이름 조회·내보내기를 생략한다.
- 진입 시 URP layerCullSpherical 경고, 카메라 준비 지연 경고, Graphics Ring Buffer 공간 경고, Raon 무게 프로필 폴백 경고가 관찰됐다. 오류와 로드 실패는 없었다. 범위 밖 설정·연출 문제를 임의 변경하지 않는다.

## 계측 반영

[ActorCameraProximityDither.cs](../../Assets/02.Scripts/GameActor/Component/Common/ActorCameraProximityDither.cs)에 RefreshRenderers / PrepareMaterials / CreateMaterial / Tick 마커를 추가했다. 이름 접두사는 `UPlayGround.CameraDither.`다. static ProfilerMarker.Auto 범위로 동기 CPU만 측정하며 프레임을 넘겨 열어 두지 않는다.

새 필드·허용 목록 밖 메서드이므로 Hot Reload 미지원에 해당한다. Edit Mode에서 정상 컴파일 시작·도메인 리로드 및 MCP 후속 호출 성공, Console 컴파일 오류 0을 확인했다.

## 남은 검증

- 반복 실행의 Boot DB 로드 시간 초과 원인 확정과 DB 참조·주소별 의존성 조사.
- NPC 디더 및 외곽선 재질 생성·Editor drawer 비용 개선. 단순 지연 초기화는 첫 근접 정지를 만들 수 있어 적용하지 않았다. 공유 재질 수명과 액터별 페이드 격리 검토가 먼저 필요하다.
- 실제 입력으로 새 게임·카메라 근접·NPC 외곽선·파티 교체의 시각적 품질은 사용자 테스트 대기.
- Editor 측정이며 Player CPU/GPU·프레임 예산 및 콜드 디스크 로딩 성능은 미검증.

## 재측정·작은 개선

- 마커 재측정: PrepareMaterials 56개 유효 샘플 합계 3,292.56 ms, CreateMaterial 1,448개 합계 3,193.97 ms, RefreshRenderers 52개 합계 3,158.21 ms. 0.05 ms 미만은 내보내지 않아 횟수·합계는 하한이며 중첩 합산하지 않는다. 주요 비용은 여전히 많은 재질 생성과 Editor 재질 drawer 경로다. 캐시·계측 조건이 달라 첫 실행 대비 절감률을 계산하지 않는다.
- `SetKeywordIfExists`의 keywordSpace.keywords 전체 순회를 FindKeyword와 isValid 확인으로 바꿨다. 없는 키워드 무시, 해당 셰이더의 유효 키워드만 변경하는 기존 계약을 보존한다. 머티리얼 생성·페이드·셰이더 선택·수명은 변경하지 않았다.
- 동일 Editor에서 Hidden/ltspass_cutout, 43개 키워드, ETC1_EXTERNAL_ALPHA(이 셰이더에서는 없는 키워드) 조회 2,000회: 29.6814 → 1.6395 ms. 단일 미세 벤치이며 전체 로딩 개선으로 해석하지 않는다. GC.GetAllocatedBytesForCurrentThread는 두 경우 모두 0을 반환해 할당 개선 근거로 쓰지 않는다.
- 첫 추가 테스트는 일반 셰이더에도 Multi 디더 키워드가 있다는 잘못된 테스트 전제로 2건 실패했다. 각 셰이더의 실제 유효 키워드로 수정 후 기존 외곽선·재질 복원 검사 포함 17/17 통과, 실패·건너뜀 0. MCP job `fb7b41f21c5349bf8ad9fed9a0c76529`.
- 최종 프로파일 반복 실행은 ActorDatabase 60초 제한 초과로 BootState.Failed. SoundDatabase·MonsterCodexDatabase도 제한 초과. 임의로 제한을 늘리거나 오류를 숨기지 않았다. 디더는 Boot의 해당 DB 로드에서 실행되지 않으므로 이번 키워드 변경이 원인이라고 판단할 근거는 없다. 프로파일을 끈 별도 재실행에서는 BootState.Ready, PartyManager 15,163 ms·ActorSpawnManager 14,944 ms로 타이틀 진입했다. 캐시 등 조건 차이가 있어 계측이 유일한 원인이라고 확정하지 않는다.

## 수정 후 최종 경로

- 프로파일 기록을 끄고 Boot → 타이틀, 이후 CPU 영역만 켜고 새 게임·Raon 기본 선택 → Loading → LakeVillageWorld Completed·활성 Raon을 확인했다. 마지막 실행 Console 오류 0, 앞서 기록한 경고 4종은 남았다.
- 시작 요청 관측부터 완료 관측까지 약 48.38초. 0.5초 간격 Editor update 관측이므로 이벤트의 정확한 시각이 아니며 캐시·계측 영역 차이 때문에 개선 전후 지표로 사용하지 않는다.
- 완료 후 초기 5프레임을 제외한 268프레임: 전체 Editor 프레임 중앙 34.07 ms, p95 52.27 ms. PlayerLoop 합계 평균 30.67 ms/프레임, UpdateScene 14.55 ms, 게임 카메라 렌더링 16.12 ms. 포함 관계가 있는 값은 중복 합산하지 않는다. RenderLoop.ScheduleDraw는 평균 13.70 ms/프레임으로 렌더 제출 경로도 다음 조사 대상이다. GPU 시간·목표 하드웨어 성능은 측정하지 않았다.
- 최종 CPU 기록에서 디더 Awake 합계 4,310.88 ms·최대 1,017.98 ms, Editor 재질 drawer 합계 5,459.53 ms로 큰 비용은 남아 있다. 키워드 조회의 작은 개선으로 해결됐다고 주장하지 않는다. 이 기록에는 네 사용자 마커 샘플이 나타나지 않아 마커 기준 전후 비교는 미수행으로 남긴다(직전 마커 기록은 확보). Test Runner 후 사용자 마커 활성 상태는 후속 점검 대상이다.
- 종료 후 Play·Profiler를 끄고 초기 Profiler 영역 설정과 원래 LakeVillageWorld 씬을 복원했다. 씬 dirty=false. 검증 전후 git 상태 차이는 이번 코드·테스트·개발 위키·상태 문서 4개뿐이며 기존 사용자 변경은 보존했다.

## 후속 조사: 2026-11-13

### 계측과 현재 판정

- 다른 MCP 작업 세션의 중단 상태와 Editor 유휴·테스트 종료를 확인한 뒤 같은 Editor를 사용했다. 런타임 C# 변경은 이번 후속 조사에서 없다. `ActorCameraProximityDither`는 Hot Reload allowlist 밖이며 Edit Mode 수정도 정상 컴파일 대상이다. 적용할 런타임 변경이 없어 새 컴파일은 요청하지 않았다.
- `manage_profiler` 설치본 `SessionOps.Start`는 엔진 `Profiler.enabled`만 변경하며 Driver/프레임 생성 여부를 확인하지 않는다. Edit Mode에서는 `profileEditor=false`인 동안 Driver를 켜도 새 프레임이 기록되지 않았다. Editor 기록을 켠 뒤 실제 `RefreshRenderers` 호출을 실행해 frame 1182의 마커를 확인했다. 합성 `execute_code`의 마커 호출만 믿지 않고 프로젝트 메서드를 호출했다.
- CPU와 Scripts 활성, Driver와 엔진 활성, Deep Profile 꺼짐을 확인하고 Boot → 타이틀 → 새 게임 → 기본 index 0 → Confirm → LakeVillageWorld `Completed`, 활성 Raon을 재확인했다. [재실행 원본](../../Logs/StartupProfiling/resume-before-transition.json.gz), [마커 집계](../../Logs/StartupProfiling/resume-before-markers.json). 시작 관측부터 완료 48.45초에는 타이틀 대기·수집 비용이 포함되어 로딩 시간으로 쓰지 않는다.
- Refresh 52개 3,365.44ms / Prepare 56개 2,501.78ms / Create 1,448개 2,420.81ms / Tick 44개 1,683.24ms(0.05ms 미만 제외). drawer 3,040개 5,295.70ms. 중첩 합산 및 과거 실행 대비 개선율 계산 금지. Tick의 큰 초기 비용에는 외곽선 생성 경로도 포함될 수 있으며 단순 거리 연산 비용으로 해석하지 않는다.
- 관련 [EditMode 재실행](../../Logs/StartupProfiling/resume-editmode-results.json) 17/17 통과, 실패·건너뜀 0(`8ee26dd5225f4057abc7d3a6ba494efa`). Test Runner 뒤 Driver/엔진/CPU/Scripts가 꺼진 상태를 관측했다. 이것만으로 과거 마커 누락 원인을 확정하지 않는다.
- Test Runner 뒤 명시적으로 기록을 다시 준비한 [동일 경로 원본](../../Logs/StartupProfiling/resume-after-tests-transition.json.gz)에서는 1,597개 유효 프레임(11870~13466), 네 마커 전부 수집, 단계별 Driver/엔진/Scripts=true, `Completed`·활성 Raon·Console 오류 0을 확인했다. Create 1,448개 2,401.92ms, Refresh 54개 2,772.99ms, Prepare 58개 2,469.97ms, Tick 13,364개 1,713.73ms. 앞 실행과 샘플 임계값이 다르므로 count/total 전후 개선율을 만들지 않는다.
- GC.Alloc 10,039,486개를 ID로 제외했어도 반복 샘플 순회는 합계 1,511.06ms·최대 한 회 687.06ms였다. [검증 수집 본문](../../Logs/StartupProfiling/capture-verified-transition.cs.txt)을 추가하고 상세 순회를 **기록을 멈춘 뒤**로 옮겼다. 실행 중에는 0.5초마다 단계/프레임 번호만 관측한다. 시작 조건 검사, 5초간 새 프레임 미생성 실패, 단계별 활성 상태, 사용자 마커의 짧은 샘플 보존, GC.Alloc ID 제외, 보관 프레임 유실 검사 및 기존 원본 덮어쓰기 거부를 포함한다. 게임 코드·패키지 캐시는 바꾸지 않는다.
- 지연 내보내기 첫 시도는 타이틀 대기까지 2,184프레임을 기록해 2,000프레임 보관 범위를 넘었다. [실패 원본](../../Logs/StartupProfiling/resume-deferred-transition.json.gz)에 `droppedFrames=184`와 실패 이유가 남았으며 전체 경로 비교에서 제외한다. 타이틀 입력을 수집 직후 실행하고 완료 뒤 기록을 3초로 줄인 재검증은 [최종 원본](../../Logs/StartupProfiling/resume-deferred-verified-transition.json.gz)에서 `failure=null`, `droppedFrames=0`, 169개 유효 프레임(17462~17630), 네 마커를 확인했다. GC.Alloc 7,474,334개는 문자열 없이 제외했고 상세 순회 882.13ms는 기록 종료 후 발생했다. 보관 범위 유실 검사가 실제 실패 기록을 거절한 것까지 확인했다.
- 최종 재검증 Create 1,448개 2,427.07ms / Refresh 54개 2,403.14ms / Prepare 58개 2,490.95ms / Tick 6,065개 1,715.01ms. Refresh 밖 OnEnable의 Prepare도 있으므로 서로 합산하지 않는다. CPU와 Rendering 영역의 Editor 기록이며 앞 수집과 내보내기·관찰 구간이 달라 게임 성능 전후 비교가 아니다. 이번에 확인한 개선은 상세 순회 비용을 측정 구간 밖으로 옮긴 것이다. [경로 완료 스냅샷](../../Logs/StartupProfiling/resume-path-completed.json)에서 `Completed`·활성 Raon, 최종 Console 조회 오류 0을 확인했다. 새 게임·Confirm은 MCP 메서드 호출이며 사용자 실제 입력 확인은 아니다.

### 재질 병목: 적용하지 않은 후보와 다음 행동

- 액터별 준비 함수는 이미 원본 Material을 키로 중복 제거한다. `ActorOutlineLod`도 인스턴스 내부에서 원본별 재질을 공유한다. `PlayerEquipment`는 같은 프레임 장비 갱신을 다음 프레임 한 번으로 합친다. 단순한 중복 제거가 빠져 있다는 가정은 맞지 않는다.
- [소유 관계 원본](../../Logs/StartupProfiling/material-ownership.json): 월드 안정화 후 활성 디더 43개(NPC 38개), 추적 슬롯 4,633개 중 비활성 하위 계층 4,091개, 액터별 고유 원본 합계 1,138개·전체 고유 원본 214개. 씬 진입 때 생성된 전체 52개 액터와 같은 집합이 아니며 슬롯 수를 재질 생성 수로 쓰지 않는다. 비활성 복장·장비·모델의 전환 가능성을 분류한 뒤 사전 준비 대상을 줄이는 것이 다음 후보다.
- 같은 Editor·CPU 계측·각 100회 3반복으로 생성자를 비교했다. `new Material(shader)`를 `new Material(template)` 또는 `Object.Instantiate(template)`로 바꾸는 것만으로 일관된 이득이 없었다. 속성 수동 복사도 빨라지지 않았다. [생성자](../../Logs/StartupProfiling/material-constructor-benchmark.json), [복사 방법](../../Logs/StartupProfiling/material-copy-benchmark.json), [Instantiate](../../Logs/StartupProfiling/material-instantiate-benchmark.json). 이 측정은 임시 Material 생성·해제를 포함하는 분리 검사이며 게임 전후 측정이 아니다.
- `MaterialEditor.Begin/EndNoApplyMaterialPropertyDrawers`는 현재 Unity에서 비공개 API다. 전역 drawer 억제를 반사로 적용하지 않았다. 액터 간 런타임 재질 공유도 현재 페이드가 Material 자체의 알파를 변경하므로 격리를 깨뜨린다. 공유하려면 슬롯별 PropertyBlock과 다른 색상/디졸브 소유자의 계약 검증이 먼저 필요하다. 첫 카메라 접근 시 생성하는 지연 초기화는 적용하지 않았다.
- 다음 행동: NPC 비활성 모델의 활성화 호출자·복장 정책을 분류하고, 실제 전환 대상만 로딩 중 준비하는 변경을 별도 동일 조건 측정으로 검증한다. 재질 수명·외부 교체·모델 교체·대화 억제·복구와 해제의 기존 회귀 검사를 함께 유지한다. 이번 후속 조사에서 런타임 성능 개선을 입증한 변경은 없다.

### Boot DB와 렌더 제출

- [DB 의존성·소유 원본](../../Logs/StartupProfiling/boot-db-dependencies.json): 이번 CPU 기록 Boot는 Ready. ActorSpawnManager 15,201.86ms, MonsterCodexManager 15,306.28ms, SoundManager 15,533.60ms, PartyManager 15,547.57ms(병렬 경과 시간, 합산 금지). 각 DB 전역 핸들은 성공 상태·단일 소유자였으며 타입+키로 공유한다.
- `AssetDatabase.GetDependencies` 재귀 경로 수는 ActorDatabase 6,014 / UIPrefabDatabase 3,063 / SoundDatabase 185 / MonsterCodexDatabase 224다. ActorDefinitionSO.prefab과 UIPrefabEntry.prefab은 직접 Unity 참조다. 에셋 경로 수는 Addressables 실제 로드 개수·디스크 I/O·독립 CPU 시간이 아니다. 성공 시점의 의존성 조사이므로 과거 시간 초과의 단독 원인으로 확정하지 않는다. DB별 참조를 끊는 변경은 GUID·스폰·UI 로딩 계약 변경을 동반하여 이번에 임의 적용하지 않았다.
- `LoadAsync` 신규 경로는 linked token의 실시간 CancelAfter(60초)와 UniTask await를 사용한다. GameManager는 핵심 매니저를 WhenAll로 시작한다. 메인 스레드 에셋 통합 정지가 길면 여러 DB의 기한이 함께 지날 수 있으나 이번 실행에서는 시간 초과가 재현되지 않았다. 캐시된 대기자의 호출자 취소·실패 시 owner 회수와 신규 요청의 복수 owner 취소 경로에는 별도 경계 검사가 더 필요하다. 시간 제한·오류 정책은 변경하지 않았다.
- [실제 로케이터](../../Logs/StartupProfiling/boot-db-locations.json)에서 세 DB 모두 `AssetDatabaseProvider`, 소스 `.asset` 경로, 위치의 dependencies=0을 확인했다. 번들 로딩 측정이 아니다. 이 0은 SO가 참조하는 Unity 에셋이 없다는 뜻이 아니며, 위 6,014개 `AssetDatabase.GetDependencies` 결과와 다른 계층이다. 다음 재현은 같은 PlayMode provider를 유지하고 DelayedActionManager의 예약 대기와 메인 스레드 `AssetDatabase` 역직렬화 구간을 분리해야 한다.
- [렌더 카운터](../../Logs/StartupProfiling/render-counters.json)는 유효한 39샘플. draw calls 약 7,666 / SetPass 421 / batches 약 7,660 / triangles 약 8.205M. 0 다음 약 2배 값이 포함되어 Editor 프레임 경계/집계 영향이 있다. 단일 stats 조회는 0을 반환했으므로 이를 draw가 없다는 근거로 쓰지 않는다. GPU·Player 성능은 미측정이다.
- [Frame Debugger 816개 이벤트](../../Logs/StartupProfiling/frame-debugger-events.json)에서 MainCamera의 Opaque·DepthNormal prepass·MainLightShadow와 GPU skinning을 확인했다. SRP batch 이벤트는 여러 draw를 묶으므로 이벤트 수와 7,666 draw calls를 일대일 비교하지 않는다. MCP 상세 필드 이름이 Unity 6의 `m_` 필드와 달라 비어 있었고, 직접 조회에서도 선택된 마지막 이벤트 한 개만 상세가 유효했다([상세 조회 결과](../../Logs/StartupProfiling/frame-debugger-details.json)). 전체 shader/pass/배치 중단 원인은 아직 미검증이다. 다음 행동은 대표 Opaque/DepthNormal/Shadow 이벤트를 선택해 개별 확인하고 동일 카메라의 CPU 제출·draw를 대응시키는 것이다. 품질·LOD·그림자 설정은 변경하지 않았다.

### 후속 조사 종료와 보존

- Play·Profiler·Frame Debugger 기록을 끄고 원래 LakeVillageWorld 씬과 초기 Profiler 영역 설정을 복원했다. [종료 스냅샷](../../Logs/StartupProfiling/resume-restored.json): Play/일시정지/컴파일/Driver/엔진/Deep/ProfileEditor=false, 씬 dirty=false, 초기 영역 설정과 일치. 최종 Play Console 오류 0. Player·Addressables 빌드·커밋은 하지 않았다.
- 이번 후속 조사의 런타임 C#·테스트·SO·프리팹 변경은 없다. 기존 키워드 개선과 사용자 미커밋 변경은 유지한다. 새 수집 본문은 `Logs/StartupProfiling`의 MCP 실행 입력이므로 게임 어셈블리에 새 도구를 추가하지 않았다.
- 네 전환 원본(최초 후속/테스트 후/보관 범위 초과 실패/지연 내보내기 재검증)은 gzip으로 보존한다. `compress.cjs`의 원본·압축 해제 SHA-256 일치를 확인한 뒤 이번 실행의 비압축 중복만 정리한다. 생성자 비교의 부정 결과·DB 의존성·렌더 이벤트·17건 테스트 결과·수집 본문은 재개 근거로 보존한다. 사용자 아트·구매 원본·미추적 파일은 정리하지 않는다.

## 지속 목표 1단계: 불변 외곽선 재질 공유

- 구현: [ActorOutlineLod](../../Assets/02.Scripts/GameActor/Component/Common/ActorOutlineLod.cs)의 생성 후 수정하지 않는 경량 재질을 `(원본 Material, 대상 Shader, 원본 ComputeCRC)`별로 공유한다. 각 LOD 소유자는 원본별 한 번만 참조를 얻고 마지막 소유자의 Dispose에서만 파괴한다. 반복 Dispose·동일 슬롯 중복으로 이중 해제하지 않는다. 기존 소유 슬롯 복구를 먼저 수행하며 외부 교체는 보존한다. 알파를 수정하는 디더 재질은 공유하지 않는다.
- Hot Reload: 추가 필드·allowlist 밖 변경이라 미지원. Edit Mode·HotReloadBootstrap.Current 비활성을 확인하고 정상 컴파일했다. 시작 1794561305497 / 완료 1794561321216 / 도메인 리로드 1794561332851 이후 같은 Editor 후속 호출 성공, Console 컴파일 오류 0을 확인했다.
- [EditMode 원본](../../Logs/StartupProfiling/goal-outline-editmode-results.json): 20/20 통과, 실패·건너뜀 0. 새 3건은 공유/마지막 해제/중복 Dispose/외부 슬롯 보존, 원본 변경 격리, 대상 셰이더 변경 격리다. 기존 디더·대화 억제·비활성화·그림자/표면 보존 검사도 포함한다.
- 40개 LOD가 같은 원본을 쓰는 분리 측정: [이전](../../Logs/StartupProfiling/goal-outline-before-benchmark.json)은 서로 다른 재질 40개, 반복 비용 51.25~115.74ms. [수정 후 유휴 상태 측정](../../Logs/StartupProfiling/goal-outline-after-idle-benchmark.json)은 재질 1개, 첫 반복 2.46ms·후속 1.74~1.85ms. 컴파일 후 첫 측정은 Test Runner 실행과 같은 시기에 요청되어 별도 원본으로만 보존하고 주 근거로 쓰지 않는다. 게임 전체 시간의 개선율이 아니다.
- 동일 열린 Editor·CPU/Rendering·Deep 꺼짐·종료 후 내보내기·완료 뒤 3초의 경로를 [변경 전](../../Logs/StartupProfiling/goal-outline-before-transition.json.gz) / [변경 후](../../Logs/StartupProfiling/goal-outline-after-transition.json.gz)에 보존했다. 두 실행 모두 Boot Ready → 새 게임 기본 Raon → 월드 Completed. 수정 후 Console 오류 0, Play 종료 뒤 공유 캐시 0개를 확인했다. [수정 후 수집 본문](../../Logs/StartupProfiling/goal-outline-after-capture.cs.txt)에 외곽선 생성 마커도 포함했다.
- 실제 경로 Tick 합계는 1,636.96ms(3,386샘플) → 337.52ms(5,689샘플), 수정 후 외곽선 생성 마커 151개 230.55ms였다. 디더 Create는 1,448개로 같으며 2,334.47 → 2,399.24ms로 여전히 남았다. 런타임 NPC 수명/프레임 수가 달라 Tick 합계의 비율을 전체 로딩 절감률로 쓰지 않는다.
- 안정화 후 [변경 전 소유](../../Logs/StartupProfiling/goal-outline-before-ownership.json)는 1,098참조/1,098재질, [변경 후 소유](../../Logs/StartupProfiling/goal-outline-after-ownership.json)는 988참조/141재질이었다. 살아 있는 NPC 집합은 같지 않지만 변경 후 동일 재질 참조가 공유됨을 확인했다. 아직 고유 디더 생성, Boot 타임아웃, 렌더 제출 병목의 해결 완료는 아니다.
- 다음 단계: 기존 `NpcAppearancePrototype`의 비활성 메시 잎·애니메이션 바인딩·참조 보존 검사를 재사용할 수 있는지 조사한다. 현재 프로토타입은 staging 전용이고 과거 output 보고서는 위키 정본에서 제외되어 있으므로 재검증 없이 정규 프리팹에 적용하지 않는다. 구매 원본·정규 얼굴·체형·골격·GUID·유지 local fileID를 보존하며 동적 외형 교체 대상은 제외한다. 사용자 외형 확인은 계속 P02/P03 대기다.

## 지속 목표 2단계: 로드 취소·실패 소유권 복구

- [변경 전 재현](../../Logs/StartupProfiling/goal-asset-cancel-before.json): 완료된 핸들의 캐시를 이미 취소된 토큰으로 조회하면 OperationCanceledException이 전파되어도 `취소된 사용자`가 Owners에 남았다. 캐시 결과가 null인 경우도 추가 소유권 회수 경로 밖에 있었다. 과거 60초 시간 초과의 원인으로 확정한 것은 아니다.
- [AssetManager](../../Assets/02.Scripts/Manager/AssetManager.cs)는 취소된 요청을 등록 전에 거부하고 캐시/신규 로드의 취소·실패에서 이번 요청의 소유권을 회수한다. 같은 owner의 다른 요청이 대기할 수 있어 PendingRequests를 별도로 추적하며, 성공한 요청은 소유권을 확정하고 소유자·대기자 모두 없을 때만 핸들을 해제한다. 기록별 IsReleased로 Dispose와 늦은 완료 사이 중복 해제를 막고, 오래된 요청이 같은 키의 새 레코드를 삭제하지 않게 한다. 60초 제한·실패 전파는 유지한다.
- 필드·async 변경은 Hot Reload 미지원이어서 Edit Mode 정상 컴파일했다. 테스트에서 ResourceManager.Update를 public으로 가정한 컴파일 오류를 수정해 검증 전용 반사 호출로 바꿨다. 생산 코드에는 비공개 API를 추가하지 않았다. 최종 컴파일/리로드 1794562117727 → 1794562120652 / 1794562129103, 후속 MCP 조회·Console 컴파일 오류 0을 확인했다.
- [새 수명 테스트](../../Assets/02.Scripts/Editor/AssetManagerLifetimeTests.cs)는 7건이다. 이미 취소된 호출, 로드 대기 중 취소, 같은 owner의 두 요청 중 하나만 취소, 실패 결과의 owner 회수/기존 owner 유지, 마지막 owner 해제, 성공 중복 조회·반복 Dispose를 검증한다. 테스트가 만든 수동 ResourceManager 작업만 사용하며 저장 데이터/Addressables 설정을 바꾸지 않는다.
- 첫 [7건 결과](../../Logs/StartupProfiling/goal-asset-lifetime-first-results.json)는 6통과/1실패였다. 실패 작업은 ResourceManager의 지연 완료 콜백도 참조를 갖는데 이를 처리하기 전 Handle.IsValid=false를 요구한 테스트 전제 오류였다. 콜백을 처리한 뒤 최종 해제를 검사하도록 수정했다. [최종 관련 검사](../../Logs/StartupProfiling/goal-asset-lifetime-results.json)는 수명 7건+디더/외곽선 20건, 27/27 통과·실패/건너뜀 0이다.
- 수정 후 Boot Ready → 새 게임 기본 Raon → LakeVillageWorld Completed·활성 Raon을 확인했다. [경로 원본](../../Logs/StartupProfiling/goal-asset-after-transition.json.gz)은 187개 유효 프레임·누락 0·failure=null, GC.Alloc 7,499,543개는 이름 없이 제외, 상세 순회 879.85ms는 기록 종료 뒤 실행했다. Boot 중간 관측에서 ActorSpawnManager 30,534.65ms / MonsterCodexManager 30,655.62ms / SoundManager 30,937.93ms로 긴 로드는 여전히 관찰됐다. 이번 수명 수정이 Boot 시간 초과·로딩 성능을 해결했다고 주장하지 않는다. 저장/Player/Addressables 빌드는 수행하지 않았다.

### 조건부 Profiler API 추가 확인

`execute_code` 기본 CodeDom 컴파일에는 ENABLE_PROFILER가 없었다. `Profiler.SetAreaEnabled`, `SetCategoryEnabled`, `GetAllCategories`는 Conditional(ENABLE_PROFILER)이므로 직접 호출이 컴파일에서 제거될 수 있다. 실제 GetAllCategories 결과 배열이 기본값 Render(0)만 남는 현상을 반사 호출과 비교해 확인했다. [동일 상태 재현](../../Logs/StartupProfiling/goal-conditional-profiler-proof.json)은 CPU=false에서 조건부 API 호출 뒤도 false, ProfilerDriver 호출 뒤 true·Scripts=true였다. 진단/설정은 조건부 호출을 피하고 ProfilerDriver 또는 확인된 메서드의 반사 호출을 사용해야 한다. 이전 마커 누락에 가능한 구체 경로를 찾았지만 과거 실행의 정확한 호출 순서는 보존되지 않아 단독 원인으로 확정하지 않는다. GC.Alloc은 category 23(Memory)이며 CPU 기록 중 Memory category가 이미 false여도 샘플이 수집되어 단순 category 비활성화는 해결책으로 채택하지 않았다.

### 렌더 제출의 선택 이벤트 상세 재조회

[선택 이벤트 원본](../../Logs/StartupProfiling/goal-frame-debugger-selected.json): 열린 Editor의 월드에서 일시정지 후 Frame Debugger 807개 이벤트를 확보하고 `limit=index+1`로 각 이벤트를 선택한 다음 상세를 다시 조회했다. 4개 모두 valid=true로, 앞서 마지막 이벤트만 읽히던 진단 한계를 해소했다. GPU 시간·전체 이벤트의 병목 순위 검사는 아니다.

| 선택 경로 | 실제 Shader / Pass | 관측 |
|---|---|---|
| Opaque SRP batch 575 | Hidden/ltspass_transparent / FORWARD_OUTLINE | draw 1, 정점 1,891, 인덱스 7,584, 중단 38=`SRP: Node use multi-pass shader` |
| DepthNormals SRP batch 303 | Hidden/ltspass_transparent / DEPTHNORMALS | draw 1, 정점 10,731, 인덱스 43,653, 중단 37=`SRP: Node have different shaders` |
| MainLightShadow SRP batch 140 | URP/Lit / ShadowCaster | draw 1, instance 1, 중단 10=그림자 셰이더/프로퍼티/키워드 차이 |
| QuadTreeBatchNode 457 | URP Terrain/Lit Base Pass / ForwardLit | draw 1, instance 35. 이 표본은 식생이 아니라 지형 인스턴싱 경로였다. |

이 표본으로 전체 CPU 제출 비용을 어느 한 셰이더 탓으로 확정하지 않는다. 외곽선·DepthNormals·그림자를 끄거나 품질을 낮추는 변경은 적용하지 않았다. 다중 패스/셰이더 전환과 지형 인스턴싱을 구분했으며, 다음은 같은 카메라에서 전체 패스별 draw 수/재질 키워드 분포를 수집해 병합 가능한 동일 표면을 확인하는 것이다. 조회 뒤 Frame Debugger/Play/Profiler를 끄고 초기 영역 및 LakeVillageWorld를 복원했다.

### 다음 단계 준비: 고정 외형의 비활성 부품

과거 보고서를 승계하지 않고 현재 정규/기존 staging 자산 두 쌍을 새로 읽었다. [현재 자산 대조](../../Logs/StartupProfiling/goal-static-npc-existing-prototypes-audit.json): 남성 127→58, 여성 120→37 renderer, 활성 외형 fingerprint·골격 fingerprint·MotionSet 참조는 각각 같았다. [디더 변환 원본 수](../../Logs/StartupProfiling/goal-static-npc-prototype-materials.json)는 남성 40→29, 여성 39→24다. 시각 렌더/클립별 검수나 정규 적용 완료를 뜻하지 않는다. 기존 staging 24종과 원본은 변경하지 않았다.

다음 실행은 `NpcAppearancePrototype`의 참조·애니메이션 바인딩 보호 알고리즘을 토대로 새 staging에서 효과를 검증하는 단계다. 기존 source/staging을 덮어쓰지 않고, 실제 runtime 외형 교체가 있는 프리팹은 제외한다. 정규 반영 전에 유지 local fileID·골격·행동/상호작용 참조·원문/meta 백업, 렌더 전후 비교와 중간 실패 롤백 검사를 수행한다. 정규 prefab을 재생성하거나 구매 원본을 편집하지 않는다. 불필요한 초기 재질 준비를 줄이는 것이 목적이며 카메라 근접 시점으로 생성을 옮기는 방식은 쓰지 않는다.

## 지속 목표 3단계: 고정 외형 staging 재검증

- [설계](../../Assets/docs/design/dev-tools/STARTUP_NPC_COMPACTION_DESIGN.md)에 원본 보존·새 staging·비교 렌더·canonical GUID/local fileID·외부 참조·백업/실패 롤백 게이트를 분리했다. 구매 원본을 수정하거나 카메라 근접 순간으로 초기화를 미루지 않는다.
- `NpcAppearancePrototype.BuildProfilingCopy`는 기존 시제품/근거 경로를 덮어쓰지 않고 별도 경로로 생성한다. 기존 BuildPair/대표 확장 경로도 같은 구현을 사용한다. 분류를 `FindUnusedMeshLeaves`로 분리하고 부모 GameObject 활성화 곡선, 런타임 CharacterEquipmentVisuals/AvatarView, 비활성 루트를 추가로 보호했다.
- Editor/새 메서드 변경은 Hot Reload 미지원이라 정상 컴파일했다. 시작/완료 1794563361837/1794563380383, 도메인 리로드 1794563393936, 후속 MCP 성공·Console 오류 0. 분류 회귀 5/5 통과(`e8918a61580e4493b15cc38bb58ec9ce`): 비활성 순수 잎, 활성 메시, 본 참조, 자신/부모 활성화 곡선, 런타임 외형·루트 비활성 보호.
- 새 사본은 `Assets/03.Prefabs/Characters/StartupProfilingStaging/R1/`의 남녀 두 종이다. 원본 `.prefab/.meta` 해시는 [검사 결과](../../Logs/StartupProfiling/NpcCompaction/R1/source-preservation.json)에서 모두 불변이다. 기존 staging 24종은 보존했다.
- idle/walk/talk의 정면/측면, 총 [12쌍 PNG 비교](../../Logs/StartupProfiling/NpcCompaction/R1/render-comparison.json)에서 SHA-256이 전부 같았다. 정면 idle 원본도 실제 이미지로 확인했다. 이 결과는 정규 적용·장시간 동작·사용자 미술 승인 완료를 뜻하지 않는다.
- 새 사본 renderer는 남성 127→58, 여성 120→37, 활성 renderer는 각각 6개로 동일하다. 같은 Editor의 warm Instantiate 5회는 남성 원본 21.19~26.59ms/사본 20.52~26.40ms, 여성 원본 15.83~18.10ms/사본 13.34~15.24ms였다. 겹치는 분포와 Editor/비재생 조건을 구분하며 전체 로딩 개선율을 만들지 않는다. 원본/사본의 재로드 참조·외형·골격 검사는 통과했다.
- 정규 입력의 남성 69/여성 83개 순수 비활성 잎과 제거 대상 GUID:fileID 207/249개를 문자열로 고정했다. `NpcAppearanceCompactionTransaction`은 원문/meta ZIP 백업, 유지 fileID·직렬화 필드 검사, 실패 시 Undo와 정확한 원문 복구를 수행한다. 적용 전 프로젝트 외부 참조 검사와 실제 중간 저장 실패 주입이 필요하다. 아직 canonical prefab은 수정하지 않았다.
- 새 도구의 `CompressionLevel` 이름 충돌을 완전 수식 이름으로 수정하고 정상 컴파일·후속 MCP 실행을 확인했다. 후속 Console 오류 0. [확장 테스트 8/8](../../Logs/StartupProfiling/goal-compaction-tests.json)(`a820147444204fa4986a37d8af5e3263`)은 기존 분류 5건과 잘못된 입력에서 계획/백업을 생성하지 않는 3건이다. 실제 파일 복구 검증과 별개다.
- [외부 참조 검사](../../Logs/StartupProfiling/NpcCompaction/CanonicalPairR1/external-references.json)는 텍스트 72,210개/10,581,162,753bytes에서 hit 0·링크 누락 0, 이진 211개 중 미수입 Packages `Samples~` 33개를 명시적으로 분리하고 나머지는 AssetDatabase 의존성 검사로 통과했다. 미수입 샘플은 실제 GUID·등록 경로·상위 폴더 미등록을 모두 확인했다.
- 이 보완의 첫 컴파일은 [Jobs ILPP 메모리 부족](../../Logs/StartupProfiling/goal-compaction-ilpp-memory-failure.json)으로 실패해 이전 어셈블리가 유지됐다. Play 중단 상태에서 미사용 자산 해제·GC 후 정상 컴파일을 재요청하고 새 필드 로드·Console 오류 0을 확인했다. 재시작/Library 삭제/두 번째 Editor는 실행하지 않았다.
- 첫 정규 저장 직후 실패 주입은 [원문/meta 해시 복구](../../Logs/StartupProfiling/NpcCompaction/CanonicalPairR1/injected-rollback-hashes.json)를 남겼다. 이후 남녀 두 종 적용은 유지 GUID/local fileID·직렬화 필드·활성 외형·골격·참조의 저장 후 검사를 통과했다. 정확한 원본은 [ZIP 백업](../../Logs/StartupProfiling/NpcCompaction/CanonicalPairR1/originals.zip)에 보존한다.
- 정규 자산 재렌더의 [12쌍 비교](../../Logs/StartupProfiling/NpcCompaction/CanonicalPairR1/render-comparison.json)도 변경 전과 모두 SHA-256 동일하다. Renderer는 남성 58/여성 37, 활성 각 6개다. 실제 시작 경로/재질 비용은 다음 단계에서 검증하며 두 종의 분리 결과를 전체 로딩 개선으로 환산하지 않는다.
- 정규 적용 후 실제 Boot→타이틀→새 게임→기본 Raon→Loading→LakeVillageWorld 경로는 Completed/활성 Raon, Console 오류 0이었다. [수집 검증](../../Logs/StartupProfiling/goal-canonical-pair-verification.json)은 손실 0·유효 182프레임·failure=null, GC.Alloc 9,407,319개를 숫자 ID로 건너뛰고 기록 중지 후 987.16ms에 내보냈다. [원본](../../Logs/StartupProfiling/goal-canonical-pair-transition.json.gz)은 압축 해제 해시 일치를 확인하고 비압축 중복만 제거했다.
- 사용자 마커는 Refresh 54/3,674.32ms, Prepare 58/2,642.73ms, CreateMaterial 1,437/2,565.86ms, Tick 5,360/341.39ms, Outline Create 151/224.26ms였다. 포함 관계를 합산하지 않는다. [실제 NPC 목록](../../Logs/StartupProfiling/goal-canonical-pair-runtime.json)에서 정리한 남성 한 종이 renderer 58개로 생성됐고 여성은 이 실행에서 생성되지 않았다. Create 수는 앞선 1,448보다 11개 적어 그 남성의 변환 원본 수 차이와 일치한다. 캐시·액터 구성 차이가 있으므로 시간 개선이나 전체 로딩 개선율을 주장하지 않는다.
- Play·Profiler 종료 후 원래 영역과 LakeVillageWorld(비 dirty)를 복원했다. 다음은 실제 생성된 나머지 고정 외형의 새 staging 및 같은 조건의 재질 준비 전후 측정이다. Boot 시간 초과의 재발 조건과 전체 패스별 렌더 제출 분포 조사도 계속 남아 있다.

## 지속 목표 4단계: 실제 생성 고정 외형의 재질 준비 대조

- 앞선 실제 실행에서 생성된 나머지 GeneratedNpcAppearances 7종을 골라 별도 `StartupProfilingStaging/RuntimeR1/` 사본을 생성했다. [원문/meta 검사](../../Logs/StartupProfiling/NpcCompaction/RuntimeR1/source-preservation.json)는 모두 불변, [렌더 42쌍](../../Logs/StartupProfiling/NpcCompaction/RuntimeR1/render-comparison.json)은 idle/walk/talk 정면·측면 모두 SHA-256 동일했다. 기존 staging은 덮어쓰지 않았다.
- [분리 측정 원본](../../Logs/StartupProfiling/NpcCompaction/RuntimeR1/dither-paired-benchmark.json), [요약](../../Logs/StartupProfiling/NpcCompaction/RuntimeR1/dither-paired-summary.json), [수집 코드](../../Logs/StartupProfiling/dither-paired-benchmark.cs.txt): 동일 빈 씬 Editor Play/Profiler off에서 비활성 복제본의 RefreshRenderers를 수동 호출했다. 소스마다 warmup 1회 후 원본/사본 순서를 교대해 각 5회(70표본) 측정했다. 복제·복구·해제는 타이머 밖이고 NPC Awake·대화·카메라 동작은 이 측정의 범위가 아니다.
- 여성 4종의 재질 수는 39→24/24/24/23, 준비 중앙값은 50.77~53.48→30.62~32.16ms였다. 남성 3종은 40→30, 중앙값 51.81~55.37→38.54~40.92ms였다. 긴 표본(사본 최대 112.76ms 포함)도 원본에 보존했다. 동일 조건 분리 작업의 감소이며 전체 로딩 개선율로 환산하지 않는다. Console 오류 0, Play 종료 후 LakeVillageWorld를 복원했다.
- 7종의 [외부 참조 검사](../../Logs/StartupProfiling/NpcCompaction/CanonicalRuntimeR1/external-references.json)는 72,225개/10,591,974,377bytes·hit 0·링크 누락 0이었다. 이진 211개도 같은 수입 여부/의존성 게이트를 통과했다. 세 번째 저장 뒤 실패 주입에서 [7종 원문/meta 복구](../../Logs/StartupProfiling/NpcCompaction/CanonicalRuntimeR1/injected-rollback-hashes.json)를 확인한 뒤 정규 적용했다. [원본 ZIP](../../Logs/StartupProfiling/NpcCompaction/CanonicalRuntimeR1/originals.zip), 유지 ID/직렬화 필드/참조 검사와 [정규 재렌더 42쌍](../../Logs/StartupProfiling/NpcCompaction/CanonicalRuntimeR1/render-comparison.json)을 보존했다. 전후 PNG는 모두 동일했다.
- [관련 회귀 35/35](../../Logs/StartupProfiling/goal-compaction-related-results.json)(`47bb059a5a32498eb2cc93c90010ddd2`)는 분류 8+디더/외곽선 20+AssetManager 수명 7건이다. 앞 실행에서 잘못된 테스트 namespace를 지정해 8건만 수행된 것을 확인하고 올바른 full name으로 다시 실행했다. 수행 건수를 35건으로 오인하지 않았다.
- Test Runner 뒤 명시적으로 Profiler를 준비해 실제 Boot→Title→새 게임→기본 Raon→Loading→월드를 실행했다. [검증](../../Logs/StartupProfiling/goal-runtime-seven-verification.json)은 Completed/활성 Raon, 유효 190프레임·손실 0·failure=null, Console 오류 0이다. GC.Alloc 6,587,128개를 문자열로 내보내지 않고 기록 종료 뒤 839.66ms에 스캔했다. [원본](../../Logs/StartupProfiling/goal-runtime-seven-transition.json.gz)은 압축 해제 SHA-256 일치를 확인했다.
- CreateMaterial은 1,346개/2,090.33ms, Refresh 54개/3,000.53ms, Prepare 58개/2,147.59ms, Tick 5,830개/304.07ms, Outline Create 151개/218.67ms다. 중첩 합산 금지. [실제 NPC](../../Logs/StartupProfiling/goal-runtime-seven-runtime.json)는 앞 실행과 같은 생성 8종 모두 새 renderer 수(37~59개)였다. Create 1,437→1,346의 91개 감소는 분리 측정의 7종 감소 합계와 일치한다. Completed 관측은 27.46초였지만 앞 실행의 36.33초와 캐시 차이를 통제하지 못했으므로 전체 시간 개선율로 쓰지 않는다.
- [기존 GameManager 타이머](../../Logs/StartupProfiling/goal-runtime-seven-boot-timings.json)는 ActorSpawnManager 29.06초, MonsterCodex 30.12초, Sound 30.53초, Party 31.75초였다. 서로 병렬 실행된 대기 포함 시간으로 합산하거나 독립 CPU 비용으로 해석하지 않는다. 이 실행에서 60초 초과는 재현되지 않았고 시간 제한은 그대로다. DB 의존성/경합의 재발 조건 조사와 렌더 제출 비용 조사도 남아 있다.

## 지속 목표 5단계: 전체 Frame Debugger 드로우 분포

- 4단계 실제 경로 완료 후 Editor MainCamera를 일시정지하고 이벤트 839개의 상세를 하나씩 선택해 수집했다. [상세 원본](../../Logs/StartupProfiling/goal-frame-event-details.json), [요약](../../Logs/StartupProfiling/goal-frame-event-summary.json), [수집 코드](../../Logs/StartupProfiling/capture-frame-event-details.cs.txt), [분석 코드](../../Logs/StartupProfiling/analyze-frame-events.cjs). 69.02초 동안 기대/수집 839개·invalid 0·failure=null, 반환 FrameEventIndex도 선택 index와 모두 같았다. Profiler 기록을 끈 상태의 진단이며 이 시간은 플레이 성능 측정이 아니다.
- `m_DrawCallCount`는 Compute/Clear/Resolve 이벤트에서도 1 또는 이전 셰이더 정보를 반환하므로 그래픽 draw 유형(InstancedMesh/HybridBatch/SRPBatch/Mesh/DrawProcedural/DynamicGeometry)만 합산했다. 그래픽 draw 총 7,692회 중 DepthNormals 3,654, 불투명 3,384, 주광원 그림자 551, UI 37회였다. 이벤트 수나 인스턴스 합계를 draw 수로 대체하지 않는다.
- Shader/Pass별 실제 draw는 URP Lit/DepthNormals 3,121, URP Lit/ForwardLit 1,581, Polyart/Utilities/Impostor/ForwardLit 1,259회였다. 단일 프레임 제출 구성으로, 각각의 CPU/GPU 시간 순위를 증명하지는 않는다. NPC 다중 패스 외곽선만을 전체 제출 병목으로 보는 근거는 없다.
- 다음 확인 대상은 환경 Lit의 HybridBatch(DepthNormals 44개 이벤트 포함)와 Impostor의 SRP batch 9개/1,259 draw다. Impostor shader에는 `multi_compile_instancing`이 있고 World 폴더의 관련 재질 15개도 enableInstancing=true였다. 단순 instancing 플래그 누락으로 확정하지 않는다. DOTS/GRD 변형과 실제 사용 재질·메시·배칭 제한을 이어서 확인해야 한다. 구매 shader·품질·그림자·DepthNormals·LOD 설정은 수정하지 않았다.
- 상세 수집 종료 후 전체 이벤트 limit 복원, Frame Debugger/Play/Profiler 종료, 원래 Profiler 영역 및 LakeVillageWorld 복원을 수행했다. 실제 입력·카메라 근접·NPC 외곽선·파티 교체·대화 품질은 계속 사용자 테스트 대기다.
- 후속 실험용 [셰이더 텍스트](../../Logs/StartupProfiling/impostor-dots-prototype.shader.txt)는 구매 원본을 별도 이름으로 복사해 URP 17.3.0의 `DOTS.hlsl` pragma와 fragment의 `UNITY_SETUP_INSTANCE_ID`만 추가했다. 정규 Shader/Material/씬에는 저장하지 않고 ShaderUtil.CreateShaderAsset의 메모리 객체로만 검사했다. [일반/Instancing/DOTS SetPass](../../Logs/StartupProfiling/impostor-dots-variant-probe.json)는 모두 true·오류 0이고 vector truncation 경고 2개가 남는다. 실제 GRD 호환·draw 감소·동일 카메라 이미지·CPU 전후는 미검증이다. 구매 원본을 수정하거나 정규 재질에 연결하지 않았다. 다음은 고정된 월드 카메라에서 원본/실험 shader의 메모리 재질로 렌더 대조와 GRD 배칭 수를 확인하는 것이다.

## 지속 목표 6단계: 렌더 대조 준비와 Editor 연결 단절

- 다른 Codex 작업은 notLoaded, 현재 작업만 active임을 확인하고 기존 LakeVillageWorld 비 dirty·Play/Profiler off에서 시작했다. Boot→타이틀→새 게임→기본 Raon까지 다시 실행했다. 프로파일러를 켜지 않은 렌더 대조 준비였으며 전환 시간 비교용 실행이 아니다.
- [동일 화면 대조 코드](../../Logs/StartupProfiling/compare-impostor-dots.cs.txt)를 준비했다. 월드 일시정지/Profiler off를 요구하고 원본 반복·메모리 재질 교체·복구 화면을 같은 프레임/해상도/MSAA로 캡처하며 finally에서 renderer 배열·target·임시 shader/material을 복구한다. 아직 실행하지 못했으므로 화면 동등성·GRD 호환·배칭 감소·성능은 미검증이다. 구매/정규 shader·material을 쓰지 않았다.
- 월드 준비 상태를 읽던 MCP 호출이 대기하다 `Could not connect to Unity`로 종료됐다. [연결 단절 근거](../../Logs/StartupProfiling/goal-phase6-connection-loss.json): MCP 인스턴스 0개, 기존 Editor PID 사라짐, 새 이름 없는 Unity 프로세스(25208/약 70MB)만 관찰됐다. 종료/충돌/메모리 부족 원인은 미확인이다. 재시작·다른 Editor 실행·잠금/Library/Temp 삭제는 하지 않았다. 사용자에게 대상 Editor 종료/재연결 여부를 물었고 답변 대기다. 이번 재실행의 Play 종료/영역 복원을 확인한 것으로 보고하지 않는다. 마지막 확정 복원은 [5단계](../../Logs/StartupProfiling/goal-phase5-restored.json)다.
- 연결 대기 중 [Boot provider 소스 대조](../../Logs/StartupProfiling/goal-boot-provider-source-analysis.json)를 수행했다. 설치 Addressables의 AssetDatabaseProvider는 delay>=0.01이면 DelayedActionManager에 작업을 예약하고, 해당 큐는 메인 스레드에서 동기 LoadAssetAtPath를 순서대로 실행한다. GameManager의 핵심 DB는 WhenAll 요청이지만 이 provider의 실제 동기 작업이 병렬화되는 것은 아니다. 요청별 60초 deadline에 앞선 큐 작업 시간이 들어갈 수 있다.
- 설치 UniTask generic HandleCompleted는 토큰 취소를 작업 성공보다 먼저 검사한다. deadline 후 성공한 provider도 호출자에게는 취소로 반환될 수 있다. Actor/Sound/Codex의 동시 시간 초과가 각 DB 독립 60초 비용을 뜻하지 않는 구체 경로를 확인했지만 과거 실패의 단독 원인으로 확정하지 않는다. 시간 제한이나 취소 결과 정책은 변경하지 않았다.
- 재연결 뒤 실제 provider delay·DB별 생성/제공/완료 시간과 취소 후 owner/handle 정리를 분리 관찰해야 한다. 현재 GameManager는 실패를 Failed로 기록하지만 공유 초기화 CTS.Cancel은 OnDestroy에만 있어, 실패 시 다른 요청의 조기 취소 경계도 추가 검증 대상이다. 기존 BootInitializationTests 5건은 로더 완료/실패/종료/시간 배율/후처리를 다루며 이 경계는 아직 덮지 않는다. 새 런타임 변경은 하지 않았다.
- 연결 단절이 세 번의 연속 목표 턴에서 동일하게 확인돼 목표를 `blocked`로 기록한다. 세 번째 조회도 MCP 인스턴스 0개·PID 25208 CPU 0.4375초·비교 결과 없음이었다. Editor 없이 가능한 provider/취소 소스 대조는 마쳤고, 남은 실제 화면·GRD 배칭·CPU 전후·Boot 재현/핸들 검증은 수행할 수 없다. 대상 프로젝트의 단일 Editor가 MCP에 재연결되면 현재 상태/dirty/Play/Profiler/Hot Reload부터 재확인하고 이어간다. 완료 판정은 하지 않는다.

## 지속 목표 7단계: 재연결 후 조사 재개

- 사용자 재개 요청 뒤 MCP에서 `UPlayground@fddd0702` 하나가 running이고 대상 프로젝트 경로가 일치함을 확인해 고정했다. Boot 비 dirty·Play/Profiler/Deep off·원래 영역·Console 오류 0, Hot Reload Current 없음이었다. 이전 미커밋 목록과 달라진 항목은 없었다. 임의 Editor 실행 없이 재연결된 인스턴스를 사용한다.
- 프로파일러를 끈 Boot 재실행은 Ready였다. [실제 provider/매니저 시간](../../Logs/StartupProfiling/resumed-boot-provider.json)에서 AssetDatabaseProvider delay=0.1초, Actor 35.41초·Codex 35.49초·Sound 35.67초·Party 37.24초였다. 병렬 요청의 대기 포함 시간으로 합산하거나 timeout 재현으로 해석하지 않는다. 기본 Raon 새 게임으로 월드 진입을 진행했다.
- 메모리 셰이더 대조는 같은 프레임·원본 반복/실험/복구·해상도/MSAA를 확인하는 단계다. 비교 코드에 월드 Completed와 실제 MSAA 확인, null shader 방어를 보완했다. 새 정규 C# 타입/메서드 변경이나 Hot Reload 적용은 없고, 구매/정규 shader/material은 변경하지 않는다.
- 실제 경로는 Completed/활성 Raon·Console 오류 0이었다. 일시정지된 같은 카메라 2560×1440/MSAA4에서 대상 renderer 4,579개/구매 prefab 내장 재질 6개의 메모리 사본만 교체했다. 첫 [R1 이미지 대조](../../Logs/StartupProfiling/ImpostorDotsR1/comparison.json)는 자체 반복 변동 안이었지만 일시정지 때문에 GRD의 재질 분류가 갱신되지 않은 약한 증거였다. 이를 최종 동등성 판정으로 쓰지 않는다.
- 설치 URP의 GRD는 PostLateUpdate에서 재질/renderer 변경을 반영한다. [R2 진단 대조](../../Logs/StartupProfiling/ImpostorDotsR2/comparison.json)는 이 갱신을 진단용 반사 호출로 수행한 뒤 같은 프레임을 렌더했다. 원본 반복은 26픽셀/최대 채널 차이1, 실험은 112,060픽셀/최대37·차이1 초과2,438픽셀, 복구는 70픽셀/최대1이었다. 원본·실험 이미지를 실제 확인했지만 미세 차이가 자체 변동을 넘어 완전 동일로 판정하지 않는다. 실제 움직임·각도·조명·미술 품질은 사용자 테스트 대기다. 진단용 private API를 런타임 제품 코드에 추가하지 않았다.
- [원본 Frame Debugger](../../Logs/StartupProfiling/ImpostorDotsR1/baseline-event-summary.json)는 844개 상세 모두 valid, 전체 그래픽 draw 7,726·Impostor 1,259였다. [실험](../../Logs/StartupProfiling/ImpostorDotsR2/prototype-event-summary.json)은 828개 모두 valid, 전체 6,473·Impostor 6이었다. 전체 차이 1,253은 대상 pass 감소와 정확히 같았다. 단일 고정 카메라 Editor의 구성 변화이며 전체 맵/Player 성능으로 일반화하지 않는다.
- [CPU A/B/A 원본](../../Logs/StartupProfiling/ImpostorDotsR2/cpu-aba.json), [요약](../../Logs/StartupProfiling/ImpostorDotsR2/cpu-aba-summary.json), [수집 코드](../../Logs/StartupProfiling/capture-impostor-cpu-aba.cs.txt): 같은 세션·카메라 pose 고정·TimeScale0/Editor unpaused에서 CPU+Rendering/Deep off/profileEditor=false, 단계별 15프레임 예열 후 각90프레임을 기록했다. GRD 갱신은 이 측정에서는 정상 PlayerLoop로 수행했다. RawFrameData는 기록 종료 뒤143.11ms에 읽었고 GC.Alloc10,058개는 문자열 내보내기 없이 건너뛰었다. failure=null·invalid0.
- ScheduleDraw 중앙값은 원본10.264→실험9.138→복구10.079ms, MainCamera marker는6.397→5.698→6.262ms였다. Inl marker4.514→4.269→4.536ms는 별도 표본으로 합산하지 않는다. 전체 Editor 프레임 중앙값26.910→26.839→26.657ms로 비슷해 FPS 개선을 주장하지 않는다. draw counter 중앙값7,501→6,245→7,489로 복구 전후에도12회 차이가 있어 동적 렌더 구성의 작은 변동을 인정한다.
- renderer4,579개 복구/missing0, [원본 재질 CRC와 렌더러 현황](../../Logs/StartupProfiling/ImpostorDotsR2/restored-materials-and-renderers.json)에서 6개 구매 재질의 CRC가 모두 실험 전과 같았다. 구매 shader 원문 해시와 git 상태 목록도 변하지 않았다. 임시 교체0·SessionState 바인딩0·TimeScale1·Play/Profiler/FrameDebugger off, LakeVillageWorld 비 dirty 및 원래 영역을 [복원 확인](../../Logs/StartupProfiling/resumed-phase7-restored.json)했다. 정규 shader/material에는 아직 반영하지 않았다.
- 다음은 구매 prefab 내장 재질을 수정하지 않는 프로젝트 소유 반영 경로와 미세 이미지 차이를 확인하는 것이다. 별도로 활성 URP Lit renderer13,030개 중8,797개가 static batch이며, 기존 전환 기록에 StaticBatching.Combine712.74ms(중첩 Finish687.15ms 포함)가 있었다. GRD와의 실제 배칭 분할 영향을 가설로 조사하되 플래그/품질 설정을 임의 변경하지 않는다.

## 지속 목표 8단계: 반복 측정 종료 수명 오류

- 7단계 실제 진입/측정 중 Console 오류는 없었지만 Play 종료 뒤 [PetManager NRE](../../Logs/StartupProfiling/pet-shutdown-before.json)를 확인했다. Despawn의 AgentTickManager.Instance.Unregister가 null을 역참조해 GameManager.OnDestroy의 나머지 매니저 정리를 중단시켰다. 이 때문에 종료 후 전체 핸들 정리가 통과했다고 판단하지 않는다. 기존 프로파일러/씬 복원 확인과 서비스 정리 성공은 별개다.
- BaseManager는 OnApplicationQuit에서 공유 종료 플래그를 설정하고 이후 Instance 접근자는 null을 반환한다. PetManager.Despawn은 해당 매니저가 유효할 때만 Unregister하도록 최소 변경했다. 오류를 catch로 숨기거나 시간 제한/리소스 수명 정책을 바꾸지 않는다. 해당 메서드는 allowlist 밖이고 새 PlayMode 테스트를 추가하므로 Hot Reload 대신 정상 컴파일 중이다.
- 정상 컴파일 시작/완료1794567920229/1794567934319·리로드1794567944350, Console 컴파일 오류0이었다. [새 PlayMode 검사 1/1](../../Logs/StartupProfiling/pet-shutdown-playmode-results.json)(`b290a74067a747da9bcba1c6aa2c2f31`)은 종료 접근자 경계, 펫 파괴 예약, 반복 Dispose, 틱 싱글톤 부활 방지를 통과했다.
- PlayMode Test Runner 후 Profiler를 다시 준비해 Boot→타이틀→새 게임→기본 Raon→Loading→월드를 재실행했다. [180개 유효 프레임/손실0](../../Logs/StartupProfiling/pet-shutdown-transition-verification.json)에서 마커5종 모두 수집, Completed/Raon·기록 중지 후 내보내기802.49ms·GC.Alloc6,811,611개 문자열 제외를 확인했다. Create1,346/2,072.95ms·Prepare58/2,135.10ms·Refresh54/2,442.51ms·Tick5,313/266.34ms·OutlineCreate151/210.94ms다. 중첩 합산/다른 캐시 실행과의 시간 개선율 비교는 하지 않는다. [압축 원본](../../Logs/StartupProfiling/pet-shutdown-transition.json.gz)은 압축 해제 해시 일치 후 비압축 중복만 제거했다.
- Play 종료 전후 Console 오류0이었다. [종료 관찰](../../Logs/StartupProfiling/pet-shutdown-cleanup.json)에서 전역32/임대4, 총36개 record가 모두 IsReleased=true·Handle.IsValid=false, global/scene/lease 테이블 및 등록 매니저0·초기화 CTS null이었다. 강한 managed 참조로 종료 뒤 필드만 관찰했고 새 owner/refcount를 추가하지 않았다. [관찰 코드](../../Logs/StartupProfiling/observe-shutdown-cleanup.cs.txt). Profiler/영역·LakeVillageWorld도 복원했다.
- 이후 [기존 EditMode 회귀 첫 결과](../../Logs/StartupProfiling/pet-shutdown-related-editmode.json)는12건 중9통과/3실패였다. 실패3건은 수명 테스트의 의도된 예외가 부팅 후 Addressables 전역 ExceptionHandler를 통해 추가 로그로 기록돼 실패한 것이다. fixture를 해당 Exception 인스턴스만 격리하고 다른 예외는 기존 핸들러로 전달하며 finally에서 핸들러를 복구하도록 고쳤다. 실제 게임 예외 정책은 그대로다. 재컴파일/재검증 중이며 아직 이 12건을 전부 통과로 보고하지 않는다.
- fixture 수정 정상 컴파일 시작/완료1794568440858/1794568446389·리로드1794568453534·컴파일 오류0이었다. [최종 기존 회귀12/12](../../Logs/StartupProfiling/pet-shutdown-related-editmode-final.json)(`ac53239200044a0e9480f7c6fb7e7fcd`)는 이전 실패와 같은 오류 로거를 임시로 연결한 조건에서 통과했다. 검증 뒤 원래 전역 ExceptionHandler와 참조가 같은지 확인하고 임시 AppDomain 데이터도 비웠다. 실패 원본은 보존한다. PetManager 종료 오류 수정 검증은 완료했으며 Boot60초 초과의 원인 해결이나 렌더 셰이더 정규 반영 완료를 뜻하지 않는다.

## 지속 목표 9단계: static batch 원본 메시 대조

- Edit Mode에서 URP Lit/BatchingStatic renderer9,283개·원본 메시262개의 영구 ID를 [읽기 전용 스냅샷](../../Logs/StartupProfiling/StaticBatchR1/source-summary.json)으로 보존했다. Play에서는 Prefab ID 표현이 달라 직접 GlobalObjectId 조회3건이 null이었다. Editor targetObjectId XOR targetPrefabId와 runtime targetObjectId를 대조해 중복0·실제 static renderer8,797개 모두 매칭됨을 [확인](../../Logs/StartupProfiling/StaticBatchR1/id-match.json)했다. 64비트 ID는 C# ulong/문자열로 유지했다.
- [메모리 대조](../../Logs/StartupProfiling/StaticBatchR1/comparison.json)는 합쳐진 메시58개를 원본235개로 일시 교체했다. static submesh 정보·root·bounds·lightmap ST를 보존하고 finally에서 복구했다. 같은 프레임/카메라/2560×1440/MSAA4에서 원본 반복82픽셀/최대6, 합치기 해제39픽셀/최대1, 복구102픽셀/최대1이었다. 복구8,797개 모두 확인했다. 씬/구매 자산/플래그 저장은 없었다.
- [CPU A/B/A](../../Logs/StartupProfiling/StaticBatchR1/cpu-aba-summary.json), [원본](../../Logs/StartupProfiling/StaticBatchR1/cpu-aba.json), [수집 코드](../../Logs/StartupProfiling/capture-static-cpu-aba.cs.txt): 같은 카메라 pose·TimeScale0/Editor unpaused, 단계별15프레임 예열 후90프레임. failure=null·invalid0, 기록 종료 뒤143.60ms에 숫자 ID로 분석하고 GC.Alloc10,151개 문자열을 제외했다. 첫 임시 스크립트는 지역 변수명 충돌로 실행 전 컴파일 실패했고 이름 수정 후 정상 실행했다.
- ScheduleDraw 중앙값9.979→9.306→9.941ms, MainCamera6.452→5.923→6.410ms, 전체 Editor 프레임30.058→27.179→29.009ms였다. draw counter8,641→3,894→8,872로 복구 전후231회 변동이 있어 정밀 FPS/전체 개선율로 쓰지 않는다. 드로우 감소에 비해 CPU 감소는 작았고 p95 변동도 남는다. Inl/포함 관계 수치는 합산하지 않는다.
- 현재 PC 품질 한 단계와 기본 PC_RPAsset 모두 GRD InstancedDrawing이며 현재 API는 D3D11이었다. 설정은 바꾸지 않았다. [설계](../../Assets/docs/design/dev-tools/STARTUP_STATIC_BATCHING_DESIGN.md)에 원본/meta·나머지 static 비트·전체 YAML/ID 보존, 별도 사본, 중간 실패 복구 및 정규 적용 게이트를 정리했다. 아직 정규 씬 플래그는 수정하지 않았다.
- 마지막 EditMode 회귀 뒤에도 실제 경로 Completed/Raon, [197프레임/마커5종/손실0](../../Logs/StartupProfiling/StaticBatchR1/transition-verification.json)을 확인했다. [수집 원본](../../Logs/StartupProfiling/StaticBatchR1/transition.json.gz)은 압축 해제 해시 일치를 확인했다. [종료 관찰](../../Logs/StartupProfiling/StaticBatchR1/shutdown-cleanup.json)에서36개 전부 해제/무효·테이블/매니저0·CTS null, 종료 후 오류0이었다. Play/Profiler off·원래 영역·LakeVillageWorld로 복원했다.

## 지속 목표 10단계: 검증한 정적 배칭 플래그 반영

- 사본 저장·재로드에서 전체 GameObject121,437개 중 대상9,283개가 모두 계획과 일치했다. [YAML 엄격 검사](../../Logs/StartupProfiling/StaticBatchCanonicalR1/scene-validation.json)는 문서106,484개의 ID를 유지하고104,556개를 byte 동일로 확인했다. 직접 GameObject161개·PrefabInstance1,767개 문서의 허용 플래그/override 외 차이는0이다. 나머지 static 비트·메시·재질·조명·물리·배치·참조는 보존했다. [검사기](../../Tools/StartupProfiling/validate-static-scene.cjs)는64비트ID·잘못된 플래그 등 자체 검사3건도 통과했다.
- Git 비추적 원본 씬150,984,854bytes와 meta는 [ZIP](../../Logs/StartupProfiling/StaticBatchCanonicalR1/original-scene.zip)에 보존하고 각 entry SHA-256을 재확인했다. [쓰기 직후 실패 주입](../../Logs/StartupProfiling/StaticBatchCanonicalR1/rollback-proof.json)에서 원문/meta 해시와 비 dirty 재로드를 확인했다.
- 검증한 사본 바이트를 정규 경로에 [적용](../../Logs/StartupProfiling/StaticBatchCanonicalR1/apply.json)하고 [재로드 플래그](../../Logs/StartupProfiling/StaticBatchCanonicalR1/canonical-flags.json)9,283/9,283·wrong0·전체121,437개·원래GUID/meta를 확인했다. Console 오류0. [적용 코드](../../Logs/StartupProfiling/apply-static-canonical.cs.txt), [검사 코드](../../Logs/StartupProfiling/verify-static-canonical.cs.txt)를 보존했다. 사본은 [압축 원문](../../Logs/StartupProfiling/StaticBatchCanonicalR1/validated-staging.unity.gz)으로 보관했다. 실제 경로·마커·렌더 제출·종료 해제 검증은 진행 중이며 반영 성공을 성능 개선 완료로 대체하지 않는다.
- 정규 적용 뒤 실제 경로는 Completed/활성Raon·Console 오류0이었다. [201개 유효 프레임/손실0](../../Logs/StartupProfiling/StaticBatchCanonicalR1/post-transition-verification.json), [압축 원본](../../Logs/StartupProfiling/StaticBatchCanonicalR1/post-transition.json.gz)에서 사용자 마커5종을 확인했다. Create1,346/2,321.96ms·Prepare58/2,387.30ms·Refresh54/3,281.55ms이며 중첩 합산하지 않는다. 기록 종료 뒤943.81ms에 분석했고 GC.Alloc9,216,316개 문자열을 제외했다. 캐시/실행 조건 차이로 전체 로딩 절감률은 계산하지 않는다.
- [런타임 메시 검사](../../Logs/StartupProfiling/StaticBatchCanonicalR1/runtime-meshes.json)는 대상9,283개 모두 원본 메시 일치·wrongMesh0·batched0이었다. 고정 카메라15프레임 예열 후 [90프레임](../../Logs/StartupProfiling/StaticBatchCanonicalR1/cpu-final-summary.json)에서 draw 중앙3,768·ScheduleDraw9.559ms·Editor 프레임25.080ms였다. 앞 A/B/A와 카메라pose는 같지만 별도 Play/캐시·액터 구성이므로 직접 시간 개선율을 만들지 않는다. [수집 코드](../../Logs/StartupProfiling/capture-static-final.cs.txt).
- [Frame Debugger827개 상세](../../Logs/StartupProfiling/StaticBatchCanonicalR1/frame-event-details.json)는 전부valid·failure=null, [그래픽 draw 요약](../../Logs/StartupProfiling/StaticBatchCanonicalR1/frame-event-summary.json)은3,767회였다. URP Lit/DepthNormals519·Forward280, Impostor/Forward1,259회다. 정규 반영으로 URP Lit 분할 감소를 확인했으며 아직 바꾸지 않은 Impostor 비용은 남아 있다. 이벤트·중첩 패스·인스턴스를 합산해 CPU 시간이나 FPS로 환산하지 않는다.
- [종료 관찰](../../Logs/StartupProfiling/StaticBatchCanonicalR1/shutdown-cleanup.json)은36개 전부 해제/무효·테이블/매니저0·CTS null, 종료 Console 오류0이었다. Play/Profiler/Deep off·원래 영역을 복원했다. 자체 staging씬만 압축 검증 뒤 삭제했다. 최초 삭제 요청은 MCP의 기본 패턴 검사에 차단되어 실행되지 않았고, 정확한 사본GUID·현재 비활성·압축 보관을 재확인한 명시적 호출로 정리했다. 원본/구매 자산은 삭제하지 않았다. 실제 입력·카메라 이동·맵 전체 미술 품질은 사용자 테스트 대기다.

## 지속 목표 11단계: DB 큐 대기 분리와 실패 취소

- [가벼운 관찰 코드](../../Logs/StartupProfiling/capture-boot-queue.cs.txt)는 현재 DisableDomainReload 조건에서 기존 InternalIdTransformFunc를 감싸고 발견된 전역 핸들의 완료를 관찰했다. 실제 provider의 로드 직전 transform과 완료 관찰을 기록하며 기존 변환 함수·핸들 참조 횟수·timeout·provider는 바꾸지 않는다. 완료 관찰에는 앞 구독자의 비용이 포함될 수 있어 순수 provider CPU가 아니다. 관찰 전에 완료된/transform을 못 찾은 요청은 별도 표시한다. 최초 임시 코드의 Object 모호성 컴파일 실패는 실행 전에 중단됐고 object.ReferenceEquals로 수정했다.
- Profiler off의 [R1 원본](../../Logs/StartupProfiling/BootQueueR1/trace.json)/[요약](../../Logs/StartupProfiling/BootQueueR1/summary.json)은 Ready·오류0·관찰자153.13ms, [R2 원본](../../Logs/StartupProfiling/BootQueueR2/trace.json)/[요약](../../Logs/StartupProfiling/BootQueueR2/summary.json)은 Ready·오류0·관찰자134.06ms였다. 두 실행 모두 원래 hook 복구·임시 AppDomain 데이터 해제를 확인했다. [분석 코드](../../Logs/StartupProfiling/analyze-boot-queue.cjs).
- ActorDatabase의 시작→완료 관찰은9,235/9,174ms, 발견→시작 대기 하한은2,081/2,081ms였다. Sound는 대기11,363/11,304ms 뒤94/100ms, Codex는 대기11,316/11,255ms 뒤40/43ms였다. 병렬 DB 경과 시간이 큐 대기를 포함함을 확인했지만 과거60초 초과의 단독 원인이나 재발 해결을 확정하지 않았다. 두 실행의 캐시 차이로 개선율을 계산하지 않는다.
- [정적 의존성](../../Logs/StartupProfiling/BootQueueR1/dependencies.json)은 ActorDB6,014개·정의478개/고유 prefab468종, UI DB3,063개·Sound185개·Codex224개였다. Actor와 Sound 교집합0·Codex1개로 단순 공통 DB 중복 로드로 판단할 근거가 없다. 이 숫자는 실제 로드 객체/바이트/CPU와 다르다. 직접 prefab 참조를 Addressable로 바꾸는 변경은478개 데이터와 스폰 계약을 함께 다루므로 여기서 임의 전환하지 않았다.
- GameManager.HandleInitializationException은 Failed/오류를 기록하고도 형제 요청의 공통 CTS를 취소하지 않았다. 오류를 유지한 뒤 CTS.Cancel하는 최소 변경을 반영했다. 해당 메서드는 NoInlining/allowlist 적격이 아니고 새 테스트 변경도 포함하므로 정상 컴파일했다(시작1794571984587/완료1794571988733·리로드1794571994219, 컴파일 오류0).
- [기존+추가 회귀13/13](../../Logs/StartupProfiling/BootQueueR1/related-tests-final.json)(a30f44caa4374868b7d3c751d1e73968)은 Boot6+AssetManager 수명7건이다. 추가 검사는 Failed/최초 오류 유지·공통 토큰 취소·형제 callback을 확인한다. 첫 실행은 수명 테스트 namespace를 잘못 지정해 [Boot6건만](../../Logs/StartupProfiling/BootQueueR1/boot-tests-first.json) 수행됐으므로13건으로 오인하지 않고 올바른 full name으로 재실행했다. 예상 오류 로그는 결과에 보존한 뒤 실제 경로 검증 전에 Console을 비웠다.
- Test Runner/정상 컴파일 뒤 실제 경로 Completed/Raon·Console 오류0, [160프레임/손실0·마커5종](../../Logs/StartupProfiling/BootQueueR2/transition-verification.json)을 확인했다. Create1,346/2,242.72ms·Prepare58/2,307.73ms·Refresh54/3,085.12ms이며 중첩 합산하지 않는다. GC.Alloc9,213,205개 문자열 제외·기록 중지 뒤825.17ms 분석이었다. 실제60초 실패는 재현되지 않았고 제한값은 그대로다. 로드 중 native 동기 작업 자체를 즉시 중단시키는 변경도 아니다.

## 지속 목표 12단계: Impostor 일반 인스턴싱 대안 기각

- DOTS 변형의 화면 차이를 피할 대안으로 메모리 전용 [일반 인스턴싱 shader](../../Logs/StartupProfiling/impostor-instancing-prototype.shader.txt)를 비교했다. DOTS pragma를 제외하고 Hue 선언만 CBUFFER 밖으로 옮겨 SRP Batch 호환을 분리했다. 실제 ShaderUtil은 code10·Hue의 다른 CBUFFER를 이유로 반환했다. 구매 원본·정규 shader/material/씬 참조는 바꾸지 않았다.
- [동일 프레임 비교](../../Logs/StartupProfiling/ImpostorInstancingR1/comparison.json)는2560×1440/MSAA4·같은 카메라·4,579renderer/6material, 바인딩 변경마다 GRD 진단 갱신을 수행했다. 원본 반복96픽셀/최대1, 실험2,345픽셀/1초과1,362/최대67, 복구66픽셀/최대2였다. [원본 PNG](../../Logs/StartupProfiling/ImpostorInstancingR1/baseline-b.png)와 [실험 PNG](../../Logs/StartupProfiling/ImpostorInstancingR1/prototype-b.png)를 직접 열어 확인했지만 수치 차이가 남아 완전 동일/미술 승인으로 판정하지 않는다. [비교 코드](../../Logs/StartupProfiling/compare-impostor-instancing.cs.txt).
- [Frame Debugger843개 상세](../../Logs/StartupProfiling/ImpostorInstancingR1/frame-event-details.json)는 전부valid, [요약](../../Logs/StartupProfiling/ImpostorInstancingR1/frame-event-summary.json)상 전체draw2,551·실험 Impostor23회/1,259instances였다. 앞 정규 캡처의 Impostor1,259회 대비 draw 감소만 확인했고 이를 CPU 개선으로 대체하지 않았다.
- [A/B/A 각90프레임](../../Logs/StartupProfiling/ImpostorInstancingR1/cpu-aba-summary.json)의 ScheduleDraw 중앙값9.287→9.504→9.347ms, MainCamera5.706→5.764→5.714ms·Editor 프레임24.641→24.314→24.090ms였다. draw counter3,488→2,252→3,487이지만 제출 CPU 개선은 입증되지 않았다. failure=null·invalid0·GC.Alloc9,470개 문자열 제외·기록 중지 뒤148.00ms 분석. [수집 코드](../../Logs/StartupProfiling/capture-impostor-instancing-cpu.cs.txt). 품질 차이와 CPU 이득 부재 때문에 정규 반영하지 않았다.
- [복구 검사](../../Logs/StartupProfiling/ImpostorInstancingR1/restoration.json)는 실험 바인딩0·원본6개CRC 동일·SessionState/AppDomain hook 해제·TimeScale1·기록off였다. 중간 runtime override는4,579개 복구/누락0, 최종 CPU sampler도 복구했다. [종료 핸들36개](../../Logs/StartupProfiling/BootQueueR2/shutdown-cleanup.json)는 전부해제/무효·테이블/매니저0·CTS null이었다. Play/Profiler/Deep off·원래 영역·정규 LakeVillageWorld를 복원했다. 다음 렌더 개선은 프로젝트 소유 DOTS shader/material 경로와 다각도 화면·조명 차이의 원인 검증이 먼저다.

## 지속 목표 13단계: 디더 변환 템플릿 분리 측정

- [분리 측정 원본](../../Logs/StartupProfiling/dither-template-benchmark.json)/[요약](../../Logs/StartupProfiling/dither-template-benchmark-summary.json)/[코드](../../Logs/StartupProfiling/benchmark-dither-template.cs.txt)는 실제 정규 NPC의 재질24종을 사용했다. 단일 Editor EditMode/Profiler off·템플릿 준비/예열1회는 타이머 밖·순서 교대6회였다. 매번 전체 변환과 변환 완료 재질 복제를 각144표본 비교했다. full factory에는 reflection 호출과 필드 조회 비용이 포함되므로 순수 변환 비용 차이로 해석하지 않는다.
- 재질당 중앙값1.1916→1.0374ms·평균1.2269→1.0665ms·p951.3675→1.2108ms, 결과CRC mismatch0·sourceCRC 전부 불변이었다. 이득은 새 템플릿24개 생성·보관·수명/무효화 비용을 제외한 수치다. 단순 평균 산술에서도 동일 템플릿을 약8회 이상 재사용해야 최초 변환+추가 복제 비용을 회수한다. 실제 경로의 키별 재사용/CRC 변경/모델 갱신을 포함한 대조는 미수행이므로 캐시 코드를 추가하지 않았다. 첫 근접 순간으로 생성 비용을 옮기는 변경도 없다. 임시 재질은 finally에서 전부 해제했다.

## 남은 병목의 판단과 다음 행동

- **Boot60초:** 현재 두 관찰 실행은 성공했고 큐 대기/로드 직전/완료 관찰을 분리했다. 과거 실패의 캐시·계측 조건은 복원하지 못해 단독 원인을 확정하지 않는다. 다음은 동일 provider/영역/콜드 조건을 명시한 재발 기록과 ActorDB의468종 직접 prefab 의존성 분리 설계다. 시간 제한 연장·취소를 성공 처리하는 우회는 적용하지 않았다. 실패 시 공유 CTS 취소와 마지막 요청/소유자 종료의 핸들 해제는 별도 검증했다.
- **NPC재질:** 고정 외형 정리의 같은 조건 분리 이득과 실제 생성 수 감소는 확인했으나 아직1,346회 생성 비용이 남는다. 페이드 격리를 위해 액터별 가변 재질은 유지한다. 다음은 실제 원본/CRC별 재사용 횟수와 템플릿 최초 비용까지 포함한 A/B/A다. 작은 조회/복제 개선을 전체 로딩 개선으로 보고하지 않는다.
- **렌더제출:** URP Lit 정적 결합 분할은 보존 검사 뒤 개선했다. Impostor는DOTS 변형의 화면 차이와 일반 인스턴싱의 CPU 이득 부재를 기록했다. 다음은 프로젝트 소유 shader/material 사본에서 DOTS의 다각도/광원별 차이 원인을 분리하고 같은 세션 CPU를 대조하는 것이다. 구매 원본·품질 설정은 보존했다. Editor 비용을 Player 성능으로 판정하지 않는다.
- **사람 확인:** P01~P03 및 아래 P04는 사용자 테스트 대기다. 미술/조작 승인과 자동 검증을 합쳐 완료 처리하지 않는다. Player/Addressables 빌드·커밋은 수행하지 않았다.
- 최종 [Editor 상태](../../Logs/StartupProfiling/phase13-final-editor-state.json)는 단일 원래 LakeVillageWorld·비 dirty·Play/Profiler/Deep off·원래 영역·관찰 hook 해제, Console 오류0이었다. [이번 준비표/검증 기록](../../output/harness/20261113-211253-f4424d82/review.md)은 보조 기록이며 현재 판단의 정본은 이 상태 문서다. ZIP 원문을 자기 임시 경로로 꺼내 정규 씬과 엄격 YAML 검사를 다시 실행해 같은106,484문서/9,283대상/허용 밖 차이0을 확인한 뒤 임시150,984,854bytes 입력만 제거했다. 검사기는 별도 원문/정규 경로를 인수로 받을 수 있어 사본 씬 삭제 후에도 재검사가 가능하다. 압축 해제 해시 검증 뒤 R2 전환의 비압축 중복도 제거했다.

## 사용자 확인 명세

키보드·마우스 또는 평소 쓰는 패드로 Editor에서 실행한다. 기존 저장 슬롯은 덮어쓰지 않는다. 아래 항목은 아직 사용자 테스트 대기다.

1. P01: Boot 실행 → 새 게임 → Raon 선택. 로딩 화면이 유지된 뒤 플레이어 기준 카메라로 월드가 열려야 한다. Boot 실패·무한 로딩·잘못된 첫 카메라면 실패다.
2. P02: 플레이어 및 가까운 NPC 쪽으로 카메라를 접근했다가 멀리 이동한다. 페이드·복구 중 분홍 재질, 사라진 외곽선, 계속 숨은 모델이 없어야 한다.
3. P03: 파티 교체 및 NPC 대화 진입·종료 후 모델·장비·외곽선이 원래 상태로 복구돼야 한다.
4. 결과: `P번호 / 통과·실패·미수행 / 실제 증상 / 실행 횟수·입력 장치`. 실패 시 Console의 첫 오류와 어느 화면에서 멈췄는지 전달한다.
5. P04: 같은 월드에서 가까운 건물·바위·나무 주변을 이동하고 카메라를 회전한다. 사라짐/깜박임·그림자/조명/LOD 경계 이상·충돌 또는 이동 경로 변화가 없어야 한다. 정적 배칭 비트만 변경한 자동 보존 검사는 맵 전체의 사람 품질 확인을 대신하지 않는다.

## 근거와 정리

- [마커 전환 요약](../../Logs/StartupProfiling/marked-transition.summary.json), [최초 타이틀 요약](../../Logs/StartupProfiling/baseline-title.summary.json), [EditMode 결과](../../Logs/StartupProfiling/editmode-results.json), [개선 전](../../Logs/StartupProfiling/before-keyword.json) / [개선 후](../../Logs/StartupProfiling/after-keyword.json).
- [수정 후 전환 요약](../../Logs/StartupProfiling/final-transition.summary.json), [완료 후 프레임](../../Logs/StartupProfiling/final-transition.phases.json), [Boot CPU 요약](../../Logs/StartupProfiling/marked-boot-title.summary.json). Boot에서는 ResourceManager.DelayedActionManager 9,057 ms·Loading.ReadObject 8,283 ms 등 에셋 역직렬화 경로를 확인했다. 중첩 합산 금지.
- 프레임 원본은 Logs/StartupProfiling/*.json.gz에 보존했다. 압축 해제 SHA-256과 원본 일치를 확인한 뒤 비압축 중복 약 588 MB를 삭제했다. 수집·분석 스크립트와 실패 기록을 포함해 보존한다. 기존 사용자 파일·구매 에셋·미추적 자료는 정리하지 않았다.
