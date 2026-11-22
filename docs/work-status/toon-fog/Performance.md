# 성능

| 경로 | 최대 국소 슬롯 | 경계 및 노이즈 샘플 | Fog용 컬러 복사 |
| --- | ---: | ---: | ---: |
| Low | 2 | 2, 노이즈 없음 | 0 |
| Medium | 4 | 4 | 0 |
| High | 8 | 8 | 0 |
| Legacy 굴절을 사용하는 월드 | 선택 품질과 동일 | 선택 품질과 동일 | 조건부 1회 |
| 기존 Water Scene Resolve 카메라 | 선택 품질과 동일 | 공기 교차 구간마다 적용 | 기존 수역 경로 사용 |

불투명 합성은 전체 해상도이며 균일 부피는 교차 길이를 해석적으로 계산한다. PC RP는 기존 효과도 Depth/Opaque Texture를 사용한다. Frame Debugger에서 기존 URP 복사와 안개 기능 요청으로 추가된 작업을 구분한다.

## 실측

측정 대기. 설계의 QHD 60fps, Medio GPU 증가 1.0ms 이내, 카메라 CPU 0.2ms, 준비 후 GC 0B/frame은 목표다. 고정 화면 렌더를 GPU 시간으로 해석하지 않는다.

Editor Profiler의 같은 Game View 해상도에서 안개 끔과 켬을 교대로 측정한다. 거리 기준, 계곡, 굴절·입자 구역을 따로 기록한다. VSync·API·MSAA·Render Scale·해상도·GPU·CPU와 선택 슬롯 수를 함께 기록하고 준비 이후 300프레임의 중앙값과 p95를 비교한다. `Toon Fog Camera Setup`, `Toon Fog Composite (depth only)`, `Toon Fog Legacy Refraction Copy`, `Water Scene Resolve`의 시간을 전체 프레임 차이와 구분한다.

[단계별 작업 상태](TaskStatus.md)
