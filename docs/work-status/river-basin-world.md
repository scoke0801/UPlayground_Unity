# RiverBasinWorld — 제작·검증 상태

- 설계 정본: [정규 월드 제작 설계](../../Assets/docs/design/world-art/open-world/REGULAR_WORLD_PRODUCTION_DESIGN.html) (좌표·치수·예산은 블록아웃에서 조정할 시작값이며 구현 완료 수치가 아니다)
- 저작 지침: [오픈월드 맵](../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md), [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)
- 저작 원본·기록 스크립트: [Tools/RiverBasinWorld/README.html](../../Tools/RiverBasinWorld/README.html)
- 분야 위키: [월드와 아트](../wiki/world-art.md) (표의 「강 유역 월드」 행)
- 조사 기준: 2026-11-14 문서·파일 조사. 이번에 게임·Unity를 실행하지 않았다.
- 현재 판정: **독립 씬 1차 제작본 저장됨 / 미술 NO-SHIP·사용자 인수 대기 / P5 정규 등록 미제작**.

## 반영된 에셋

| 구분 | 위치 |
|---|---|
| 씬 | `Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity` (MapID `RiverBasinWorld`) |
| 전용 데이터 | `Assets/10.Datas/World/RiverBasinWorld/` (VisualR1: `RiverBasinPipeline.asset`, `RiverBasinSky.mat`) |
| 하늘·날씨 | `Assets/10.Datas/World/Sky/Sky_RiverBasin.asset`, `Assets/10.Datas/World/LivingWorld/Environment/RiverBasin_Weather_v1.json` |
| 납품 자산 폴더 | `Assets/03.Prefabs/Environment/RiverBasinWorld/`, `Assets/08.Models/World/RiverBasinWorld/`, `Assets/04.Images/World/RiverBasinWorld/` |
| 저작 입력 | `Tools/RiverBasinWorld/` (`world-plan.json`, `production-plan.json`, `asset-sources.json`, `Generated/*.f32`, `Source/`, `VisualR1/`, `VisualR3/`) |

최종 입력 기준 경로 길이는 A=2.315km, B=1.598km, C=2.472km, 대표 구간 평면 354.8m다. 길이는 XZ 평면 합계이며 이동 시간으로 환산하지 않는다.

## 단계 기록

| 단계 | 내용 | 상태 |
|---|---|---|
| 설계 | 지역·경로·VP·POI·검증 계획 | 문서 완료. 실제 조망·초견 검증은 미수행 |
| 1차 제작 | 전용 Terrain 9장, 교량·사당·수목 군락, 지도 계약·재질·수면 굽이, 검병 1명·발견 상자 1개 | 씬 저장됨. 기록 스크립트는 재실행 금지 |
| VisualR1 | 파이프라인·하늘 재질 | 데이터 존재. 개별 판정 기록 없음 |
| VisualR3 (B1, 2026-11-09) | 지형·물 비주얼 개선, 폭포 립 연결 | 교훈은 [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)에 정리. 사용자 지적 후 재수정 |
| P5 | 정규 등록, 지도 배경, 정상 맵 전환 | **미제작** |

## 검증 결과

| 확인 | 결과 | 근거 |
|---|---|---|
| 정적 지지면·통행 중심선·양방향 NavMesh | 통과 | Tools README (원본 보고서는 삭제됨) |
| 카메라 가림 이름 규칙 | 8/8 | 위와 동일 |
| 단독 Play 초기화·실제 카메라 렌더 | Ready | 위와 동일. 입력 기반 검증 아님 |
| 경고 | 미등록 서비스 2건, 무게 프로필 폴백 1건, Localization 오류 | 위키 기록 |
| 미술 | NO-SHIP | 위키 기록. 사용자 인수 대기 |

## 사용자 테스트 대기·미수행

실제 입력 이동, 전투, 카메라, 물가 복귀, 밤, 저장/재진입, 성능, 미술 품질. 위 표의 한 항목만으로 전체 통과를 판단하지 않는다.

## 차단·주의

- **근거 소실:** `output/RiverBasinWorld/`(`RESULT.html`, `TEST_SPEC.html`, `audit.json`, 캡처)는 2026-11-13 output 정리로 삭제됐다. Tools README의 해당 링크는 끊겨 있다. 위 검증 수치는 그 보고서를 현재 다시 확인한 값이 아니다.
- **재실행 금지:** `build-world.csx`, `revise-stage.csx`, `finish-*.csx`, `place-encounter.csx`는 기록용이다. 수동 최종 배치가 반영돼 있어 재실행해도 같은 씬이 나온다고 보장하지 않는다. 재생성이 필요하면 현재 씬·전용 데이터·meta·ManualGameplay ID부터 보호한다.
- **git ignore:** 환경 에셋은 git ignore 대상이라 저장소 diff만으로 변경·복구 여부를 판단하지 않는다.
- 기존 OpenWorld 생성기와 과거 일회성 스크립트는 실행하지 않는다.

## 다음 행동

1. 사용자가 미술 방향을 확정하고 사용자 테스트 명세를 새로 작성한다(삭제된 `TEST_SPEC.html` 대체).
2. `audit-world.csx`·`capture-world.csx`로 현재 씬을 다시 검사해 이 문서의 검증 표를 갱신한다.
3. 위 경고 3종과 Localization 오류를 원인 분류한다.
4. P5 정규 등록은 사용자 지시 후 착수한다.
