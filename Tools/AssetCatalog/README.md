# 환경 에셋 카탈로그 재생성

환경 프리팹을 캡처해 카테고리별 HTML로 만드는 도구다. 결과물과 사용법은 [docs/asset-catalog/index.html](../../docs/asset-catalog/index.html), 상태는 [작업 상태](../../docs/work-status/asset-catalog.md)에 있다.

## 절차

1. Unity Editor를 열어 두고 Unity MCP로 [render_catalog.cs.txt](render_catalog.cs.txt)를 `execute_code`(action=execute) 본문으로 실행한다. 후보 수집부터 캡처·`manifest.json`·`skipped.txt` 기록까지 에디터 업데이트 루프에서 순차 처리한다(약 3,200개, 수 분).
2. `docs/asset-catalog/progress.txt`가 `DONE`으로 끝나면 [render_relight.cs.txt](render_relight.cs.txt)를 같은 방식으로 실행해 인게임 낮 조명으로 다시 촬영한다. 1번의 캡처는 에디터 기본 조명이라 어둡다. 이 스크립트는 동기 실행이므로 3,000장 이상이면 시간 제한에 걸린다. 파일 목록을 나누거나 1번처럼 `EditorApplication.update` 루프로 바꿔 쓴다.
3. `python -I Tools/AssetCatalog/build_catalog.py docs/asset-catalog`로 HTML 6종과 `index.html`을 만든다.
4. 용량이 2.6KB 미만인 `img/*.jpg`는 빈 캡처다. relight 스크립트가 한쪽 면만 그려지는 벽을 반대 방향(+Z)에서 다시 찍어 대부분 복구하고, 남은 것은 `docs/work-status/asset-catalog.md`에 기록한다.
5. 끝나면 `progress.txt`는 지운다.

## 인게임 조명 기준

월드 조명은 런타임에 `WorldLightingController`가 시각에 따라 만든다. 카탈로그는 `time01=0.38`(늦은 오전) 키프레임의 태양, `LakeVillageWorld` 씬의 Skybox 앰비언트, 그 씬이 쓰는 `LakeAtmosphere` 볼륨(Bloom·ColorAdjustments)을 임시 씬에 복제해 찍는다. 정오는 태양이 머리 위라 옆면이 어두워 쓰지 않았다. 기준을 바꾸려면 `render_relight.cs.txt` 상단 주석의 값을 고친다.

## 주의

- 후보 필터·카테고리 분류는 스크립트 안의 정규식이다. 새 팩을 추가했다면 필터와 `build_catalog.py`의 `pack()`을 함께 점검한다.
- 비동기 셰이더 컴파일을 끄지 않으면 첫 렌더가 빈 이미지로 저장된다.
- 루트가 비활성인 프리팹은 `SetActive(true)`를 해야 보인다.
- 인스턴스를 만드므로 `ExecuteAlways` 스크립트가 있는 프리팹은 씬 상태에 영향을 줄 수 있다. 실행 후 씬이 더러워지지 않았는지 확인한다.
