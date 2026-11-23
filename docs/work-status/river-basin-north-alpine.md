# RiverBasin4km 북부 설산

[월드와 아트 위키](../wiki/world-art.md) · [공간 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#북부-설산-탐험-동선)

## 1. 이동 구역과 HeightMap

- 채석 요새 접근로 X 2972 / Z 2152에서 북쪽으로 갈라진다. 폭포 동쪽 사면을 따라 X 3310 / Z 3540의 고원 입구에 오른 뒤, X 3280~3385 / Z 3540~3740을 순환한다.
- WorldBuilder Sculpt `6dea362daf01427cb68a56a9b811cbb7`에 저장했다. 6개 Terrain에서 높이 표본 192,820개를 변경했고 최대 변화는 56.10m다. [조형과 검증](../../Tools/RiverBasinWorld/Rebuild/north-alpine-shape.cs.txt)
- 첫 Editor Play에서 급경사 타일 경계 전의 발자국 기록이 비었다. Z 3584 경계에 완경사 구간을 조형해 Sculpt `386a0bc20ada4e0691e6619296ee1fa7`으로 저장했다. [첫 주행 로그](river-basin-north-alpine-evidence/alpine-play-first.txt)
- Unity Editor 저장 높이 기준 동선 길이 1,978.4m, 중심과 좌우 3m를 포함한 6,027개 지점의 최대 경사는 43.6°다. 진입부 최대 경사 38.9°, 지상 장애물과 수면 침범 0개, 전체 112개 타일 경계의 높이 차 0m다.
- 작업 전 씬과 월드 정의, 수정 타일 사본은 `output/RiverBasinWorld/NorthAlpine/before`에 보존했다.

## 2. 적설과 발 디딤

- 고도와 경사, 바람 방향을 반영해 고원 완사면에 눈이 쌓이고 급경사에서 암반이 드러나도록 했다. 기존 설면 알베도에 반복 가능한 512² 노멀을 연결하고 타일 크기를 12.8m로 좁혔다. [설면 노멀 저작](../../Tools/RiverBasinWorld/Rebuild/north-alpine-normal.cs.txt) · [적설 및 수신 지도 저작](../../Tools/RiverBasinWorld/Rebuild/north-alpine-surface.cs.txt)
- 네 Terrain의 눈 가중치 50% 이상 면적은 약 0.144㎢다. 눈과 모래의 R8 마스크를 382×763으로 합쳤다. 기존 사막 마스크 대비 표본 차이는 0개이며, 저장된 설산 알파맵과 수신 마스크 차이도 허용 오차 1단계 이내다.
- 기존 Footsteps - Essentials의 부드러운 눈 음원에서 보행·달리기·착지별 4개를 연동했다. 기존 발 디딤의 먼지 프리팹을 복제해 색과 수명을 조정한 눈가루를 연결했다.

## 3. 검증

사막과 설산에서 걷기·달리기 발자국 누락 제보에 따라 평가된 발 포즈를 추적한다. 설산 Editor Play 걷기 8/8회, 달리기 6/6회가 교대로 찍혔다. [변경과 사용자 테스트](river-basin-sand-footprints.md#걷기달리기의-평가된-발-포즈-추적)

- Unity 정상 컴파일 완료. `FootstepEnvironmentTests`와 `SandFootprintMathTests` EditMode 21/21 통과.
- 텍스처와 사면은 Unity에서 [조감](river-basin-north-alpine-evidence/alpine-overview.png), [오르막](river-basin-north-alpine-evidence/alpine-ascent.png), [고원 근경](river-basin-north-alpine-evidence/alpine-snow-close.png)을 촬영해 확인했다.
- Editor Play 첫 입력 주행에서 라온이 X 3298 / Z 3498부터 고원 Z 3663까지 경유지 4/4를 통과했다. 1,751개 표본에서 안정 지지를 유지했고 지면 프로필은 암반 → 풀 → 눈 순서로 선택됐다. 발자국 요청 5개, 누락 0개, GPU 눌림 2,254·테두리 1,830 표본을 확인했다.
- 쉼터 저장 뒤 동선·수면·높이 경계·적설 마스크 검사를 다시 통과했다. 라온으로 경계 Z 3584 전후를 재주행해 경유지 3/3, 안정 접지 676/676, 경계 전/후 발자국 2/7개, 누락 0개를 확인했다. GPU 눌림 717·테두리 589 표본을 읽었고 실제 발자국을 Unity 카메라로 촬영했다. [재주행 로그](river-basin-north-alpine-evidence/alpine-play.txt) · [Play 화면](river-basin-north-alpine-evidence/alpine-play.png) · [눈 발자국 근경](river-basin-north-alpine-evidence/alpine-footprint.png) · [입력 검증 도구](../../Tools/RiverBasinWorld/Rebuild/north-alpine-probe.cs.txt)
- 착지 확인을 시작하는 사이 Editor가 Play를 종료했다. 착지 입력과 전체 동선 양방향, 시청각 인수는 사용자 테스트 대기다.
- 기존 씬과 데이터 사본 9.17MiB 및 WorldBuilder Undo에 쓰이는 조형 세션 스냅샷 2개(총 160.06MiB)를 보존했다. 조형 패치 두 파일의 SHA-256은 월드 정의와 일치한다.

### 사용자 테스트 대기

| 항목 | 재현 절차와 기대 결과 |
|---|---|
| RB-ALPINE-01 | 4km 씬을 별도 저장 불러오기 없이 연다. 채석 요새 접근로 X 2972 / Z 2152에서 북쪽으로 갈라져 X 3310 / Z 3540까지 오른다. 고원 순환로를 양방향으로 돌아 내려온다. 걸려 멈추는 곳, 미끄러지거나 물에 빠지는 곳, 방향을 놓치는 지점을 기록한다. |
| RB-ALPINE-02 | X 3310 / Z 3584에서 걷기·달리기·점프 착지를 비교한다. 눈밭에서 눈 소리와 눈가루가 나오고 카메라를 돌리면 자국이 보여야 한다. 아래쪽 암반으로 내려갈 때 눈 자국이 번지거나 이전 지면 소리가 반복되면 실패다. |

회신 형식은 `항목 ID / 통과·실패·미수행 / 실제 결과 / 화면 또는 로그`다. Player·Addressables 빌드와 정규 월드 청크 재진입은 미수행이다.
