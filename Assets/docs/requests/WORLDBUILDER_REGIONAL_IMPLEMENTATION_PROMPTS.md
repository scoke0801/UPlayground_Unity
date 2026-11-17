# WorldBuilder 지역형 HeightMap 월드 — 구현 요청 프롬프트

현재 지시가 아니다. 사용하기 전에 [설계](../design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md)와 최신 코드를 대조한다. 한 번에 한 단계(또는 P00 같은 조사 단계)만 요청한다.

## 공통 프롬프트 (모든 단계 앞에 붙임)

```
UPlayground에 신규 월드 제작 도구 WorldBuilder v1을 구현한다. 기존 월드(생명의 호수 등)는 편집·교체하지 않는다.

먼저 읽을 것(이 순서):
1. AGENTS.md, Tools/Harness/README.md, docs/wiki/world-art.md
2. Assets/docs/design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md (정본 요약, 0장이 사용자 결정)
3. docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/IMPLEMENTATION_SPEC.md 중 해당 단계 장(텍스트만. OpenWorld_HeightMap_Design_v1_1.html은 base64 이미지가 커서 열지 않는다)
4. .claude/rules/architecture.md, Assets/docs/guide/CODE_AUTHORING_GUIDE.md, Assets/docs/guide/TEST_VALIDATION_GUIDE.md

확정 결정:
- 월드 C: 2,048m×2,048m, 샘플 2049², 간격 1m, 512m Terrain 4×4, 타일 513².
- 높이: originY=0, verticalRange=700m, worldY = sample/65535×700 (WorldProfile 파라미터). 원본 RAW의 2,800m 기록은 보존.
- Unity 입력 RAW는 atlas/01_Regional_Heightmap_2049_R16LE_SouthFirst.raw 하나뿐(첫 행 남쪽). 첫 행이 북쪽인 RAW·PNG, 컨셉 PNG는 참조용이며 높이 입력 금지.
- 좌표: +X 동, +Z 북, heights[z,x]. Height 513²와 Hole 512² 혼동 금지. SetHoles true=표면.
- 기술 fixture B(2km, −80~400m)는 별개이며 불변. P00~P09/T01~T26을 P10/T27~T35로 대체·생략하지 않는다.
- D01~D12(설계 문서 2장)를 계약으로 따른다. 기존 asmdef 경계·서비스 계약을 우선하고 어댑터로 연결한다. GUID·.meta·Addressables 키 보존, 패키지 무단 업그레이드 금지.

작업 규칙:
- 시작 전 git status와 미커밋 변경을 기록하고 보존한다. 커밋·push·reset·clean 금지. Player·Addressables 빌드는 별도 지시 없이 실행하지 않는다.
- Unity Editor는 하나다. MCP·Test Runner로만 다룬다. C# 수정은 Hot Reload 운영 가이드를 따른다.
- 빈 메서드, TODO, 예외 삼키기, 항상 성공 반환으로 완료를 가장하지 않는다. 컴파일·EditMode·PlayMode·빌드는 실제 실행한 것만 성공으로 보고하고 미수행은 사유와 함께 적는다.
- 코드 주석·문서는 한국어. 단계가 끝나면 부산물을 정리하고 docs/wiki/world-art.md와 docs/work-status/ 상태 문서(없으면 docs/work-status/open-world/WORLDBUILDER.md 신설)를 갱신하고 링크를 확인한다. 보고에 갱신한 위키 링크를 넣는다.
- 보고: 변경 파일, 실제 실행한 검사와 결과, 미수행·사용자 테스트 대기 항목, 다음 단계.
```

## P00 · 저장소 조사 (코드 변경 없음)

```
[공통 프롬프트]

단계: P00. 코드는 만들지 않는다.
1. ProjectVersion.txt, Packages/manifest.json, packages-lock.json에서 Unity·URP·Addressables·Splines·AI Navigation·Terrain Tools 실제 버전과 설치 여부를 기록한다.
2. 기존 asmdef, 씬 로더, 세이브, 미니맵, 리스폰, AI/내비 서비스, 카메라 가림 규칙의 연결 지점과 타입·시그니처를 실제 파일로 확인해 정리한다. 추측하지 않는다.
3. 기존 KCC·NavMesh 설정에서 실제 보행 가능 최대 경사·캐릭터 반경·계단 높이를 읽어 설계의 출발값(경사 12°, 경로 폭 4m)과 대조한다.
4. C RAW에 verticalRange=700m를 적용했을 때 경사 분포(중앙값·p90·p99·최대)를 실제로 측정하고 위 최대 경사와 비교한다. 지나치게 가파르면 근거와 함께 대안을 제안하되 값을 임의로 바꾸지 않는다.
5. 산출물: docs/work-status/open-world/WORLDBUILDER.md (INTEGRATION_AUDIT, PLAN: 어셈블리 배치안, 단계별 수정 파일 예상, 위험, 미결정). 설계 문서 9장과 충돌하는 점은 보고한다.
```

## P01 · 수치 기반

```
[공통 프롬프트]

단계: P01 (설계 문서 8장). P00 산출물을 먼저 읽는다.
- HeightField, R16 RAW codec(전달본 core/reference/Raw16HeightmapCodec.cs는 참고만, 복사 후 완료 처리 금지), 좌표 변환, 타일 split/join(공유 경계 바이트 동일), 샘플 importer, 기본 Terrain 미리보기.
- WorldProfile(SO)에 worldSize·sampleCount·originY·verticalRange를 두고 C 값을 기본으로 한다.
- 테스트 T01~T04 + T27/T28(C RAW 방향·해상도·SHA-256, 컨셉 이미지가 Terrain 입력으로 로드되지 않음). orientation_test_33_r16le.raw로 뒤집힘·전치를 먼저 검사한다.
- 4×4 Terrain 생성·저장 후 모서리 방향을 Unity 화면으로 확인한다. 미술 품질 판정은 사용자 확인 대기로 둔다.
```

## P02~P09, P10 (같은 틀)

```
[공통 프롬프트]

단계: P0N / P10-X. 설계 문서 8장 표의 해당 행과 IMPLEMENTATION_SPEC.md의 해당 장(P02=09·10장, P03=10·11장, P04=12장·27~28장, P05=14장, P06=13·15장, P07=16·18·19·21장, P08=17·16장, P09=20·21장, P10=27~29장)을 읽는다.
이전 단계 상태 문서의 남은 항목과 사용자 테스트 대기 항목을 먼저 확인한다. 이 단계의 게이트 테스트만 실행하고 이전 단계 통과를 승계하지 않는다.
P08의 실제 Player 빌드·Addressables 검증은 사용자가 빌드를 지시했을 때만 수행한다. 지시가 없으면 "사용자 테스트 대기"로 보고한다.
P10은 RegionPreset 빈 에셋 생성만으로 완료 처리하지 않는다. 앵커는 후보값이므로 경사·수위·도로·성문·부두 사용 가능성을 검사한 뒤 수동 Lock하고 재생성 보존을 검증한다. T35(실제 TPS 시점 경관·주 경로)는 사용자 승인 항목이다.
```

## 독립 검토 (구현 후)

```
[공통 프롬프트]

구현 맥락 없이 검토한다(qa-reviewer). 파일은 수정하지 않는다. diff, 설계 0~2장 결정, 해당 단계 게이트(T번호)를 대조하고 기존 검사를 실행해 통과·실패·미수행을 근거와 함께 판정한다. 기존 월드·서비스·.meta 변경 여부, B 샘플 불변, RAW 방향·높이 범위 혼용 여부를 반드시 확인한다.
```
