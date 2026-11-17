# WorldBuilder Regional Atlas v1.1 — Codex/Claude 전달 순서

1. `AGENT_TASK_PROMPT.txt` — 구현 요구와 금지 사항, P00~P10 단계.
2. `IMPLEMENTATION_SPEC.md` — v1.0 본문 + 27~29장 상세 계약 (에이전트 우선).
3. `atlas/Regional_Atlas_Manifest.json` — 신규 C 지역형 데이터와 좌표/고도 메타데이터.
4. `core/samples/sample_manifest.json` — 기존 B 기술 검증 데이터. C와 혼동하지 않음.
5. `OpenWorld_HeightMap_Design_v1_1.html` — 오프라인용 이미지 내장 설계서.
6. `atlas/*.png` — 우거진 숲, 가을 숲, 메마른 지대, 성 평야, 고산, 강가/호수 마을의 원본 시안.

주의: 그림 시안과 R16 HeightMap은 픽셀 정합된 렌더링 쌍이 아닙니다. C의 원본 RAW는 첫 행 북쪽이며, `atlas/01_Regional_Heightmap_2049_R16LE_SouthFirst.raw`를 정상 Core Import용으로 사용하세요.

B는 기능 검증 fixture로 유지합니다. P00~P09/T01~T26을 P10/T27~T35로 바꾸어 생략하지 마세요.
