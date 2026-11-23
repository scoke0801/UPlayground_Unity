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

설계의 QHD 60fps, Medium GPU 증가 1.0ms 이내, 카메라 CPU 0.2ms, 준비 후 GC 0B/frame은 목표다. 고정 화면 렌더를 GPU 시간으로 해석하지 않는다.

Windows Editor, D3D11, RTX 3060 Ti, i5-12400F, 640×360, MSAA 1x, Render Scale 1.0, Medium. URP 단일 카메라 요청을 끔·켬으로 교차해 각각 60회 실행한 CPU 경과 시간이다. 8회 준비 렌더를 제외했고 같은 RenderTexture를 사용했다. 전환에 따른 카메라 안개 상태 초기화와 URP 제출·대기가 포함된다.

| 샘플 구역 | 끔 중앙값 / p95 (ms) | 켬 중앙값 / p95 (ms) | 중앙값 증가 (ms) |
| --- | ---: | ---: | ---: |
| 거리 + 높이 | 4.23 / 5.12 | 4.38 / 5.40 | 0.14 |
| 계곡과 국소 부피 | 3.39 / 5.23 | 3.80 / 4.90 | 0.40 |
| Legacy 굴절과 입자 | 4.63 / 6.40 | 5.33 / 6.66 | 0.71 |

GPU 전용 시간, 안정 프레임 GC, QHD 예산과 Player 변형은 미측정이다.

Editor Profiler의 같은 Game View 해상도에서 안개 끔과 켬을 교대로 측정한다. 거리 기준, 계곡, 굴절·입자 구역을 따로 기록한다. VSync·API·MSAA·Render Scale·해상도·GPU·CPU와 선택 슬롯 수를 함께 기록하고 준비 이후 300프레임의 중앙값과 p95를 비교한다. `Toon Fog Camera Setup`, `Toon Fog Composite (depth only)`, `Toon Fog Legacy Refraction Copy`, `Water Scene Resolve`의 시간을 전체 프레임 차이와 구분한다.

[단계별 작업 상태](TaskStatus.md)
