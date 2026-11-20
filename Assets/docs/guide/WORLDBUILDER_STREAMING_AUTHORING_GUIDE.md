# WorldBuilder P08 스트리밍 저작 지침

[지역형 HeightMap 설계](../design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md) · [P07 검증·발행 지침](WORLDBUILDER_BAKE_AUTHORING_GUIDE.md) · [P08 작업 상태](../../../docs/work-status/open-world/WORLDBUILDER.md#p08-streaming)

## 검증 월드 진입

1. 전용 [P08 Authoring 씬](../../WorldBuilder/Worlds/a808a808a808a808a808a808a808a808/P08Authoring.unity)과 [Definition](../../10.Datas/WorldBuilder/P08Validation/Definition.asset)을 사용한다. 변경한 입력은 저장한 뒤 발행한다.
2. [부트스트랩 씬](../../WorldBuilder/Worlds/a808a808a808a808a808a808a808a808/P08Bootstrap.unity)을 하나만 열고 Play한다. Editor에서 Addressables의 **Use Asset Database** 재생 모드로 연다. [Published manifest](../../WorldBuilder/Worlds/a808a808a808a808a808a808a808a808/Published/Manifest.json)가 선택한 revision의 catalog와 청크를 읽는다.
3. 시작 지점과 교량·동굴 동선은 [Navigation Layout](../../10.Datas/WorldBuilder/P08Validation/NavigationLayout.asset)을 따른다. 청크 준비 중에는 안전 위치에서 기다린다. 지도와 현장 포털에서 이동에 실패하면 현재 위치에서 같은 목적지를 다시 선택한다.

## 발행과 재현

- P08 입력을 연 상태에서 `WorldBakePipeline.Bake(definition, layout, checkpoint, chunked: true)`를 호출한다. P07 Validate, 저장 씬 경로 검사와 지문 검사를 통과한 revision에 512m 타일 씬과 `Chunks.asset`이 생성된다. `Publish`가 P08 소유 Addressables 그룹에 catalog와 씬 키를 등록하고 발행 포인터를 교체한다. `Rollback`은 이전 발행본의 catalog GUID 및 지문을 다시 확인한다.
- `WorldBuilderP08Validation.CreateBootstrap()`은 P08의 지형 proxy 재질, 지도 범위, 시작 위치와 기존 파티 설정의 플레이어 Actor를 연결한다. 플레이어는 목적지가 준비된 뒤 ActorSpawnManager로 스폰한다. 재발행 시 부트스트랩은 발행 포인터를 읽어 새 catalog를 찾는다. 부트스트랩 씬은 저장 위치에서 다시 진입할 수 있도록 빌드 씬 목록에 별도 항목으로 등록한다.
- 지상 타일 4×4와 동굴 surface의 소유권은 catalog에 기록한다. 타일 경계에 걸친 물·도로·교량은 단일 소유자를 사용한다. 수면은 저작 시 잘라 둔 메시와 UV를 유지하고 실제 물리 수심은 Ready 상태인 Terrain에서 얻는다. 동굴 건조 체적은 발행 revision 안의 데이터에서 읽는다.
- 원경 proxy는 타일당 65² 정점으로 생성한다. 기동 기본값은 1-ring, 2-ring 예비 로드, hysteresis 64m, 최소 유지 5초, 병렬 로드 2개와 unload 1개다. 이동 선행 거리는 관측한 로드 시간과 이동량에 따라 갱신한다. 설정 변경은 다음 발행 revision에 반영한다.
- 발행 전에 SO와 RAW 의존성 및 Missing Script를 검사한다. Editor Play 성공과 standalone Player 콘텐츠로드 판정은 각각 기록한다.

## 확인할 동선

| 확인 | 동작 | 기대 |
| --- | --- | --- |
| 경계 왕복 | 교량·동굴을 양방향으로 통과하고 경계 앞에서 반대 방향으로 달린다 | 지지면이 유지되고 양끝이 준비된 링크만 열린다 |
| 이동 실패 | 지연·실패가 설정된 목적지에 이동을 요청하고 다시 시도한다 | 준비되기 전 위치·카메라가 바뀌지 않는다 |
| 저장과 복원 | 소모 상태를 만든 청크에서 벗어난 뒤 돌아와 저장한다. 같은 슬롯을 다시 로드한다 | 안정 ID의 상태가 다시 적용되고 남은 저장 데이터는 보존된다 |
| 수계 | 강 경계와 같은 XZ의 지상·동굴 바닥을 각각 확인한다 | 지상 수영 조회가 이어지고 동굴 내부에서 물이 감지되지 않는다 |

## P09 통합 재검증

[수용 기준과 실행 원본](../../../docs/work-status/open-world/WORLDBUILDER_P09.md)

- 시작 전에 기존 월드와 Published 포인터·revision, Addressables 설정·그룹, NavMesh·Terrain·SO·RAW의 본체와 meta를 지문으로 남긴다. Player와 로컬 콘텐츠는 승인 범위에 해당할 때 고유 출력 디렉터리에 생성한다.
- 열린 Windows Unity Editor의 MCP에서 `UPlayGround.Editor.WorldBuilderP09Validation.Generate(4)`, `Generate(8)`을 순서대로 실행한다. 기존 P09 출력 경로에는 생성 명령을 다시 쓰지 않는다. 두 월드는 1m 간격과 512m 타일당 513² Height/512² Hole을 유지한다. 같은 32×32 높이 패치에서 Dirty/Full을 각각 1회 워밍업하고 3회 기록하며 해시를 비교한다.
- P08 기존 경로 재현은 `CreateP08EditorProfile()`이 생성하는 P09 전용 씬을 단독으로 열고 Editor Play로 수행한다. 두 끝 청크 중앙을 잇는 경로에서 5회 워밍업 뒤 50회 왕복한다. 최소 유지 시간은 비교를 위해 P08과 동일하게 진단값 0초를 사용한다. P08 발행본은 읽기 전용 입력이다.
- 승인된 Windows Player 검증은 `Build("고유-실행-ID")`로 로컬 Addressables와 Player를 만들고 `-screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile <출력경로>\Player.log`로 실행한다. 두 규모의 프레임 원본, 청크 상태, 활성화 이벤트와 메모리·소유 핸들을 기록한다. 실행 중 Editor 테스트와 빌드 작업을 겹치지 않는다.
- T26의 카메라 캡처는 정해진 지점을 렌더링한다. 실제 키보드·패드로 교량 양방향 접근, 동굴 입구와 내부, 원경과 근경 전환을 확인한다. 경계의 벌어짐, 이동 중 지지면 상실, 물·식생이 순간적으로 사라지는 경우의 위치와 입력을 기록한다.


T20 발행 복구는 `VerifyPublishRecovery()`가 생성하는 별도 64m 씬에서 재현한다. 완료된 fixture의 재실행에는 별도의 검증 목적지를 사용한다. 발행 포인터의 current·previous, meta GUID와 revision receipt를 함께 대조한다. CPU/GPU 프레임과 청크 상태는 Windows 원본 CSV와 함께 보관하고, GPU 유효 표본이 없으면 p95/p99 판정을 남겨 둔다.
