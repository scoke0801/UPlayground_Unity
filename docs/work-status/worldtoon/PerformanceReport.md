# WorldToon — 성능 보고

- 상위: [WorldToon 작업 상태](../worldtoon.md) · 기준: [설계서 §17](../../전달용/WorldToon_Handoff/WorldToon_Design.md)

## 결론

**GPU·CPU 프레임 비용은 측정하지 않았다(`NOT_MEASURED`).** 설계서 §17은 최적화된 실제 Player 빌드 기준 A/B를 요구하며, Player 빌드는 별도 지시가 없어 `사용자 지시 대기`다. Editor GPU 수치는 Player 비용을 대표하지 않아 쓰지 않았다. 성능 목적의 도입 판정은 아직 할 수 없다.

## 정적 지표(성능 측정 아님)

같은 키워드 조합(PC 낮: 캐스케이드 그림자·Forward+ 추가 광원·SSAO·DBuffer MRT3·soft shadow·Linear Fog·Light Layers)에서 ForwardLit 패스를 D3D로 컴파일한 바이트코드 크기다. 실행 시간과 비례한다는 보장은 없다.

| 셰이더 | 정점(B) | 프래그먼트(B) | URP/Lit 대비 프래그먼트 |
|---|---|---|---|
| URP/Lit | 1,254 | 16,674 | 기준 |
| URP/Simple Lit | 1,330 | 13,926 | −16% |
| WorldToon Mesh Lite | 1,330 | 9,990 | −40% |
| WorldToon Mesh Standard(추가 광원) | 1,330 | 14,098 | −15% |

표면 재질 샘플: WorldToon Mesh는 BaseMap 1회(URP/Lit은 BaseMap·Normal·Metallic/Occlusion 등 재질 설정에 따라 증가). 그림자맵·SSAO·DBuffer·프로브 샘플은 양쪽 공통이다.

## 측정 계획(빌드 지시 후)

| 항목 | 조건 |
|---|---|
| 장면 | `WorldToonValidation` 구역 A(바위·소품) 동일 객체 집합의 재질 세트만 전환(원본/WorldToon). 동시 배치 금지 |
| 1차 A/B | 같은 해상도(QHD 2560×1440 시작안)·카메라·조명·그림자·밀도·LOD, VSync 끔. 준비 300프레임·측정 1,000프레임·각 3회 이상, median·p95 |
| 지표 | GPU Frame ms·ForwardLit 패스 ms(지원 프로파일러), Batches·SetPass, 이 모듈 GC Alloc(0 B 목표: 런타임 C# 없음) |
| 2차 | 저품질 프리셋(MeshFar)·LOD를 포함한 실사용 비교는 별도 기록 |
| 판정 | 최소 개선 ms와 허용 회귀를 1차 측정 전에 고정. 측정 오차 이내면 교체 보류 |

| 지표 | 값 |
|---|---|
| GPU Frame ms | NOT_MEASURED |
| ForwardLit 패스 ms | NOT_MEASURED |
| Batches/SetPass | NOT_MEASURED |
| 이 모듈 GC Alloc | 해당 런타임 코드 없음(구조상 0 B, 측정은 NOT_MEASURED) |
