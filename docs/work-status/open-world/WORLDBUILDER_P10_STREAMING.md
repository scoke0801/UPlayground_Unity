# WorldBuilder P10 C 심리스 스트리밍 검증

[P10 통합 기록](WORLDBUILDER_P10.md) · [발행 지침](../../../Assets/docs/guide/WORLDBUILDER_BAKE_AUTHORING_GUIDE.md) · [스트리밍 지침](../../../Assets/docs/guide/WORLDBUILDER_STREAMING_AUTHORING_GUIDE.md)

## 후속 수정과 재검증

- C를 375개에서 377개로 갱신했다. 최초 재생성에 따라 식생이 다시 적용된 것을 확인한 뒤, 16개 TerrainData의 기준본과 원래 저작 씬으로 복원했다. Unity에서 누락 진단의 두 배치만 기존 계층에 추가했다. 기존 375개 배치와 나무 1,260그루를 확인했다. [배치 복원 및 적용 로그](../../../output/WorldBuilder/P10/Streaming/placement-only.log)
- C에 [Navigation Layout](../../../Assets/10.Datas/WorldBuilder/P10Regional/NavigationLayout.asset)을 저장했다. Ecology와 같은 KCC 프리팹, 실제 교량 게이트, 도로 체크포인트를 사용한다. 타일 내부 교량 링크는 공유 surface를 한 번 등록해 양쪽 끝점을 검증한다.
- 첫 Bake에서 WB200과 WB500 사전 검사를 통과했다. 이후 교량 접근로의 `tile_2_2 → tile_1_2` AI 경로가 끊겨 WB700으로 중단됐다. 실제 도로 경계 교차 6회 중 공유 지점 1곳을 합쳐 5곳에 링크를 저작했다. 첫 Bake에서 생성한 16개 NavMesh를 사용한 재현에서 경계와 도로 체크포인트 133구간이 양방향으로 연결됐다. [첫 Bake 검증 결과](../../../output/WorldBuilder/P10/Streaming/first-bake-receipt.json) · [경계 진단](../../../output/WorldBuilder/P10/Streaming/seam-probe.txt)
- 보존한 씬으로 다시 Bake해 Navigation을 통과한 뒤, 저장 씬 의존성 검사에서 Shader Graph의 임포트 전용 스크립트를 감지했다. 실제 C 의존성에서 미등록이던 패키지 경로 4개를 기존 예외 목록에 추가했다. [두 번째 Bake 검증 결과](../../../output/WorldBuilder/P10/Streaming/shadergraph-bake-receipt.json) · [패키지 의존성 목록](../../../output/WorldBuilder/P10/Streaming/editor-import-dependencies.txt)
- **Bake·Editor 발행 통과.** Revision `8f2e7eec43b74997b36fe1906c1a654a`의 512m 청크 16개를 재개방하고 Terrain, Collider, NavMesh, 원경 프록시 및 인스턴스 ID를 검사했다. 명시 링크 6개와 자동 경계 링크 20개, 나무 1,260그루를 기록했다. C 전용 그룹의 catalog·씬 키 17개를 등록하고 Asset Database 모드로 확인했다. [발행 포인터](../../../Assets/WorldBuilder/Worlds/35c2ca802bd64575961ac7cda330ac91/Published/Manifest.json) · [Bake 로그](../../../output/WorldBuilder/P10/Streaming/followup-bake.log) · [청크와 의존성 목록](../../../output/WorldBuilder/P10/Streaming/catalog.txt)
- 청크 씬에서 도로 3개는 `tile_0_0`, 교량은 `tile_1_2`, 강과 호수는 각각 `tile_2_2`, `tile_2_0`이 소유한다. 경계에 걸치는 객체는 catalog 의존성을 통해 소유 청크와 함께 준비된다.

## 발행본 Editor Play

| 검사 | 결과 |
|---|---|
| C 부트스트랩과 청크 로딩 | 통과. Boot → Title → C를 게임 씬 전환 경로로 열었다. 플레이어와 TPS 카메라를 초기화했고 16개 청크가 로드됐다. `tile_1_2` 준비 중 교량 게이트와 Nav 링크가 닫혀 있었고 준비 후 주행에서 둘 다 열렸다. |
| 교량과 경계 양방향 | 통과. 최종 검증 명령에서 실제 플레이어 KCC와 가상 게임패드로 `tile_2_2 → tile_1_2 → tile_2_2`를 주행했다. 순방향 154/154, 역방향 163/163 프레임에서 안정 접지했다. 각 방향에서 교량 지지 Collider와 준비된 링크를 확인했다. |
| 목적지 지연·실패·재시도 | 통과. 진단 중 현재 지지 타일을 유지하고 먼 청크를 해제했다. 목적지 청크에 0.6초 지연과 초기화 실패를 주입한 동안 기존 위치·카메라·접지가 유지됐다. 재시도 후 목적지 Ready와 좌표 이동을 확인했다. |
| Editor 프레임 시간 | 순방향 154표본의 p50/p95가 173.5/188.6ms, 역방향 163표본은 171.4/187.6ms다. 캡처와 계측이 포함된 Editor 표본이다. |
| 성·강가 마을·호수 마을 전 구간과 표현 품질 | 사용자 테스트 대기. 초기 전 구간 자동 주행에는 연결된 DualSense의 스틱 입력이 섞였다. 실물 패드 입력을 격리하고 접근로·교량·경계의 기술 검증을 다시 수행했다. |

[최종 Editor Play 로그](../../../output/WorldBuilder/P10/Streaming/drive.log) · [입력 격리 재현 로그](../../../output/WorldBuilder/P10/Streaming/drive-isolated-session.log) · [순방향 프레임](../../../output/WorldBuilder/P10/Streaming/drive-leg-1.csv) · [역방향 프레임](../../../output/WorldBuilder/P10/Streaming/drive-leg-2.csv) · [교량 접근 화면](../../../output/WorldBuilder/P10/Streaming/drive-leg-1-edge-1.png) · [재시도 전](../../../output/WorldBuilder/P10/Streaming/destination-pending.png) · [재시도 후](../../../output/WorldBuilder/P10/Streaming/destination-ready.png)

Editor 검증을 다시 실행하려면 [C 검증 부트스트랩](../../../Assets/WorldBuilder/Worlds/35c2ca802bd64575961ac7cda330ac91/P10Bootstrap.unity)을 검증 세션 동안 씬 목록에 포함한다. Asset Database 모드에서 Boot를 실행하고 Title 준비 후 `UPlayGround.Editor.WorldBuilderP10PlayProbe.StartStreaming()`을 호출한다. 기본 검증은 교량과 경계 왕복 및 실패 복구를 실행하고 별도 저장 폴더를 사용한다. 전체 도로를 자동 추종할 때는 `fullLoop: true`를 사용한다. 회신에는 `T35 / 통과·실패·미수행 / 구간과 실제 증상 / 캡처 또는 로그`를 남긴다.

최종 P07·P08 회귀 T17·T18은 2/2 통과했다. C 발행본 179개 파일, 원본 TerrainData 16개, P08 발행 관련 파일 410개, P09 상태 문서 및 씬 목록의 전후 지문이 일치했다. 기존 저장 파일 5개의 지문·크기·수정 시각과 실제 패드 활성 상태를 확인했다. 검증용 임시 씬 등록과 이번 작업에서 실패한 두 revision을 정리하고 Editor를 P08Authoring 단독 Edit Mode로 복원했다. Player와 Addressables 콘텐츠 빌드는 미수행이다. [최종 회귀 결과](../../../output/WorldBuilder/P10/Streaming/control-tests-followup.json)

## 복사용 검증 프롬프트

```text
UPlayground의 WorldBuilder P10 C 월드(GUID 35c2ca802bd64575961ac7cda330ac91)를 대상으로 심리스 스트리밍을 검증해줘. AGENTS.md, P10 작업 상태, Bake·스트리밍 지침을 읽고 현재 Editor와 에셋을 대조해줘.

1. 열린 Unity Editor 한 개에서 MCP로 C 저작 씬을 Bake 사전 검증해줘. Navigation Layout의 실제 KCC 프리팹, 잠긴 POI와 필수 도로, 교량 양 끝의 좌표·타일·게이트를 확인해줘. 사전 검사 실패는 오류 코드와 재현 입력을 남겨줘.
2. 사전 검증이 통과하면 C 전용 revision을 chunked Bake하고 Editor에서만 사용할 발행본을 확인해줘. 512m 타일 16개, 청크 종속성, Terrain·Collider·NavMesh·proxy 및 물·도로·교량의 소유권을 대조해줘.
3. 저장된 발행본으로 Play Mode에 진입해 실제 TPS 플레이어와 KCC로 성↔강가 마을↔호수 마을↔성 루프의 타일 경계와 교량을 양방향으로 주행해줘. 핵심 구간에서는 연속 입력을 사용하고 목적지 텔레포트는 지연·실패 복구 검사에만 사용해줘. 청크 요청·Ready·해제, 좌표·접지, Nav 링크, 카메라 추적과 대상 청크 로드 전후 화면을 기록해줘.
4. 목적지 청크 지연·실패·재시도에서 현재 플레이어·카메라 위치와 지지면이 유지되는지, 목적지 준비 후에만 교량·Nav 링크가 열리는지 확인해줘. 멀어진 청크를 해제한 뒤 돌아와 다시 로드할 수 있는지 확인하고 프레임 타임을 기록해줘.
5. C 결과를 통과·실패·미수행으로 나누고 프레임 타임 표본, 로그, 캡처 및 재현 절차를 작업 상태와 월드 위키에 연결해줘. 기존 P08 회귀는 별도 결과로 기록해줘. 경계의 팝인 체감, 식생·그래픽 품질, 실제 조작감은 사용자 테스트 대기로 남겨줘.

기존 C 저작 원본, B 검증 월드, P08/P09 발행본, 기존 게임 월드, 미커밋 변경, 저장 슬롯을 보존해줘. 검증 중 쓰는 저장은 격리하고 종료 후 원래 Editor 상태를 복원해줘. 새 Player 또는 Addressables 빌드가 필요한 단계는 사유를 알려줘. 기존 Editor에서 검증 가능한 범위는 계속 진행해줘.
```

## Unity MCP 검증 결과

2026-11-20 최초 사전 검사 기록이다.

**C 청크 발행 사전 검사 실패.** 열린 6000.3.21f1에서 C 저장 저작 씬을 열고 `WorldBakeValidation.Validate`를 실행했다. MCP 요청은 120초에 시간 초과됐다. 같은 Editor가 작업을 마치며 보고서를 저장했고, 재연결 후 원래 P08Authoring Edit Mode를 확인했다.

| 항목 | 판정 | 근거 |
|---|---|---|
| WB200 적용 배치 일치 | 실패 | 저장 배치 375개, Bake 방식 재평가 377개. 저장본에만 있는 ID는 0개이며, 재평가 결과에만 있는 ID는 2개다. |
| WB500 C Navigation Layout | 실패 | C GUID에 연결할 저장된 Navigation Layout이 없다. C 원본 교량 좌표와 세 POI를 넣은 검사용 Layout을 별도로 저장해 같은 검사 메서드를 실행했다. 교량 양쪽 표면이 같아 거절됐다. |
| 교량 양끝 표면 | 재현 | 시작 XZ (993.107, 1253.469), 도착 XZ (1018.911, 1234.544). 512m 타일 계약에서 양끝 모두 `tile_1_2`다. 필수 도로 두 구간이 이 교량을 사용한다. |
| C 청크 Bake·발행, C Play 스트리밍 | 미수행 | C 사전 검사를 통과해야 진행할 수 있다. C 발행 포인터가 없음을 Editor에서 확인했다. 런타임 횡단·해제·재시도와 프레임 타임은 미계측이다. |
| 기존 P07·P08 회귀 | 통과 2/2 | 동일 Editor의 T17·T18에서 기존 월드의 KCC 교량·동굴 횡단, 청크 지연·실패·재시도 및 링크 복구를 확인했다. [Test Runner 실행 원문](../../../output/WorldBuilder/P10/Streaming/control-tests.json) |
| T35 표현 품질·실조작 | 사용자 테스트 대기 | 식생·그래픽 품질과 스트리밍 팝인 체감은 수동 검수 항목이다. |

[Bake 사전 검사 원문](../../../output/WorldBuilder/P10/Streaming/preflight-report.json) · [배치 ID 차이](../../../output/WorldBuilder/P10/Streaming/placement-id-diagnostic.txt) · [교량 Layout 재현 결과](../../../output/WorldBuilder/P10/Streaming/navigation-layout-diagnostic.json)

Editor 범위에서 다음 단계는 배치 생성·검증 입력의 차이를 해소하고, 같은 타일 안의 교량 게이트를 Bake·런타임 계약에 맞추는 작업이다. 실제 C 청크 횡단은 이 사전 검사와 Editor 발행 후 검증한다. 이번 검사에서 Player와 Addressables 빌드는 실행하지 않았다.

C 저작 파일 4개와 공용 빌드 설정의 SHA-256이 전후 일치했다. B fixture 50개와 P08 발행 파일 410개는 기존 보존 지문과 일치했고, 저장 파일 5개의 SHA-256·크기·수정 시각도 일치했다. P09 문서 지문을 확인했다. 검사용 Layout을 정리하고 원래 P08Authoring Edit Mode를 복원했다.
