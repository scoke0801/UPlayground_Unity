# 재질별 안개

| 재질 | 실제 셰이더·깊이·큐 | 연결 |
| --- | --- | --- |
| 라온 몸·얼굴 | lilToon 2.3.4 `Hidden/lilToonOutline`, Queue 2000, ZWrite 1. `FORWARD_OUTLINE` inverted hull, Outline ZWrite 1 | 화면 합성. 실제 배경 변경에서 hull 경계는 사용자 테스트 대기 |
| 라온 머리·얼굴 부속 | `Hidden/lilToonTransparentOutline` Queue 2001/2450, `Hidden/lilToonTransparent` Queue 2460, ZWrite 1 | 이름은 Transparent 계열이며 실제 큐는 URP 불투명 범위다. 깊이와 함께 합성 |
| 라온 의상·귀걸이 | `Hidden/lilToonCutoutOutline`, AlphaTest 2450, ZWrite 및 Outline ZWrite 1 | 컷아웃·hull 깊이로 화면 합성 |
| 라온 Fake Shadow | `_lil/[Optional] lilToonFakeShadow`, Queue 2505, ZWrite 1, Multiply | 기존 그림자 경로. 안개를 포함한 배경을 곱하므로 전용 식과 원거리 확인이 남음 |
| WorldToon·반응형 식생·지형 | 프로젝트 소유 셰이더와 Depth·DepthNormals 패스 | 불투명 깊이 및 컷아웃 기준. 식생 LOD와 바람의 플레이 확인 대기 |
| 관리 수역 | `WaterSceneResolve`, 기존 불투명 색을 굴절 UV로 읽고 수중 광학을 적용 | 공기 구간만 공통 수학으로 계산. 굴절 앞·뒤 공기 구간이 불투명도 상한을 공유 |
| Legacy LakeWater | Queue 3000, ZWrite Off, Straight Alpha, URP Scene Color를 읽는 수면 | 필요 시 안개 합성 후 배경 복사. 굴절 배경과 수면색·반사를 별도 처리 |
| WorldBuilder Water·Foam | Straight Alpha, ZWrite Off | 프로젝트 공통 표면 함수. 관리 수면은 기존 Resolve가 소유 |
| WaterAwareParticle | Premultiplied Alpha, ZWrite Off | 수면 뒤 가림과 수중 광학 유지. 공기 쪽에 표면 안개를 적용한 뒤 alpha 곱 |
| 샘플 판 | 불투명, 컷아웃, Straight, Premultiplied, Additive | 같은 거리에서 비교. Additive는 투과율만 적용 |
| Overlay UI | 카메라 안개 합성 후 렌더링 | HUD 색상 보존 |

외곽선의 위 세 그룹은 설치된 lilToon의 기존 Forward 출력을 사용한다. 피부 뒤 투명 곱 레이어의 목표는 배경 원색에 그림자를 곱한 후 안개를 입히는 것이다. 이미 안개가 깔린 배경에 곱하면 공기 산란량까지 어두워진다. 해당 특수 재질은 일반 투명 어댑터와 별도로 확인한다.

[화면 결과](Validation.md) · [작업 상태](TaskStatus.md)
