# Claude / Codex 구현 요청문

아래 내용을 새 작업의 첫 요청으로 전달한다. `WorldToon_Design.md`를 같은 작업 컨텍스트에 첨부한다.

---

현재 Unity 프로젝트에 **WorldToon 경량 월드 툰 셰이더군**을 구현해줘.
첨부한 `WorldToon_Design.md`를 요구사항과 검증 기준으로 사용해.

## 목표

플레이어와 명시적으로 보호한 주요 캐릭터의 lilToon은 그대로 유지한다.
일반 NPC·몬스터·건물·바위·소품에는 공통 Mesh Toon을 적용할 수 있게 하고,
잔디·덤불·나뭇잎은 Foliage Toon, Unity Terrain은 Terrain4 전용 경로로 분리한다.
서로 다른 셰이더가 동일한 툰 명암과 색감 규칙을 공유하도록 구현한다.

최적화의 목적은 필요한 표현만 남겨 비용을 통제하는 것이며,
모든 기존 셰이더를 무조건 교체하는 것이 아니다.

## 작업 순서

**G0부터 G7까지 순서대로 진행해.** 조사 보고서만 작성하고 끝내지 말고,
현재 환경에서 가능한 구현·테스트·빌드 검증까지 수행해.
단, 단계별 실패를 숨긴 채 전체 월드를 강제 교체하지 마.

1. `ProjectVersion.txt`, 패키지 manifest/lock, Graphics/Quality의 URP Asset,
   Renderer Data, 기존 재질·Terrain·식생·NPC·LOD·Addressables를 조사한다.
   정확한 Unity/URP 버전, Forward/Forward+/Deferred, 타깃 API,
   필수 GI·SSAO·모션벡터·데칼 기능을 `ProjectAudit.md`에 기록한다.
2. 기존 프로젝트 규칙에 맞는 ShaderLab/HLSL 기반 Mesh Toon 최소본을 만든다.
   BaseMap·2단 명암·주광 그림자·현재 프로젝트의 GI·Fog와 필요한 패스를 연결한다.
3. 원본/최적화된 원본/lilToonLite(외형 허용 시)/커스텀의 최소 비교 씬을 만든다.
4. Foliage의 Cutout·정점 바람·양면·실제 배치 경로를 연결하고 패스 정합을 검증한다.
5. NPC는 기존 스키닝/Animancer를 보존하고 재질 슬롯별로 이관 가능하게 만든다.
   밤·실내의 추가 광원과 코드/애니메이션에 의한 재질 제어를 확인한다.
6. Terrain4는 설치 URP의 Terrain 경로를 기준으로 구현한다.
   타일당 최대 4개 레이어, Basemap/생성, Holes, Terrain Instancing을 검증한다.
7. 분석→복제→미리보기→검증→선택 적용→복구 도구와 Variant Manifest를 만든다.
   실제 Player, 품질 전환, LOD, Addressables, 원거리 Terrain까지 확인한다.
8. 기능·성능을 통과한 대상만 적용 후보로 확정하고 보류·원본 유지 이유를 보고한다.

## 반드시 지킬 제한

- 플레이어/보호 캐릭터, 원본 lilToon, 원본 머티리얼·텍스처·TerrainData를 보호한다.
- URP 패키지나 `Library/PackageCache`를 직접 수정하지 않는다.
- 프로젝트 엔진 업그레이드, 전역 SRP Batcher 해제, 전체 Renderer 교체를 하지 않는다.
- Outline·MatCap·Rim·Fur·Refraction·범용 Transparent를 v1에 추가하지 않는다.
- 새로운 Compute 식생 렌더러·GPU 스키닝·월드 스트리밍을 만들지 않는다.
- NPC와 Props는 같은 Mesh 계열을 사용하며 불필요한 코드 복제를 피한다.
- Terrain에 일반 Mesh 셰이더를 대입하지 않는다. 5개 이상 레이어는 원본을 유지한다.
- Terrain Detail의 프로브/라이트맵 제약과 실제 인스턴싱 여부를 확인한다.
- SRP Batcher와 GPU Instancing, 일반 Renderer와 명시적 인스턴싱의 MPB 사용을 구분한다.
- Cutout·바람·LOD를 컬러/그림자/깊이/노멀/모션벡터에서 일관되게 처리한다.
- Forward+ 광원 순회, APV, Mixed Lighting, Decal은 설치 패키지의 실제 API를 확인한다.
- `Always Included Shaders` 전체 등록이나 모든 variant 유지로 문제를 숨기지 않는다.
- `.material`을 통한 런타임 재질 복제나 Renderer별 매 프레임 파라미터 갱신을 만들지 않는다.
- 실제 실행하지 않은 컴파일·Player·그래픽·GPU 테스트를 성공했다고 기록하지 않는다.

## 구현 방식

기존 폴더/네임스페이스/asmdef/테스트 도구를 우선한다.
신규 필수 외부 라이브러리나 유료 에셋을 도입하지 않는다.
기능을 넓히기보다 작은 동작 단위를 구현하고 검증한다.
설계에서 환경 의존으로 남긴 항목은 저장소와 실제 프로젝트 설정을 읽어 결정한다.
지원할 수 없는 기능이 발견되면 해당 대상의 적용을 막고 원본을 유지한다.

GUI/Unity 실행 환경이 없으면 정적 분석·실제 구현 코드·테스트 코드·재현 절차를 작성하되,
실행할 수 없는 항목은 `NOT_RUN`과 사유를 남긴다.
측정값은 `NOT_MEASURED`로 두고 임의의 성능 수치를 쓰지 않는다.
막히지 않은 다른 단계는 계속 진행한다.

## 검증과 보고

설계서 T01~T28에 대해 `PASS / FAIL / NOT_RUN / NOT_APPLICABLE`를 기록한다.
성능 비교는 같은 해상도·카메라·조명·메시·밀도·LOD·그림자를 사용한 1차 A/B와,
실제 저품질 프리셋을 적용한 2차 비교를 구분한다.
최적화된 lilToon 대비 이득이 없으면 그 사실을 보고하고 해당 대상의 교체를 보류한다.

각 단계 종료 시 아래 순서로 보고해.

- 변경 파일과 구현 범위
- 실행한 테스트/명령과 실제 결과
- 미실행·실패·제약 사항
- 설계와 달라진 결정 및 이유
- 다음 단계

최종적으로 `ProjectAudit.md`, `Decisions.md`, `ConversionReport.json`,
`ValidationReport.md`, `PerformanceReport.md`, 출처 고지와 적용/보류 목록을 남겨줘.
완료 상태는 “코드 작성”, “컴파일 확인”, “시각 검증”, “성능 검증”, “도입 승인”을 구분해.
