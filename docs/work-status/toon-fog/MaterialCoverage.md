# 재질별 안개

| 재질 | 초기 조사 | 연결 |
| --- | --- | --- |
| 캐릭터 | lilToon 2.3.4 | 불투명과 깊이를 쓰는 hull은 화면 합성 |
| WorldToon·반응형 식생·지형 | 프로젝트 소유 셰이더와 Depth 패스 | 불투명 깊이 및 컷아웃 기준 |
| WaterSceneResolve | 기존 불투명 색을 굴절 UV로 읽고 수중 광학을 적용 | 공기 구간만 공통 수학으로 교체 |
| LakeWater | URP Scene Color를 읽는 legacy 수면 | 굴절 배경과 수면 성분 별도 처리 |
| WaterAwareParticle | premultiplied | 수역 광학과 공기 안개 연결 |
| Overlay UI | 카메라 안개 합성 후 렌더링 | HUD 색상 보존 |

화면 검증은 [작업 상태](TaskStatus.md)에 기록한다.
