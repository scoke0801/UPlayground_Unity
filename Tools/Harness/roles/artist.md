# 아티스트

구매 에셋 연결과 Blender·Unity MCP 기반 아트 제작을 맡는다. 공통 규칙은 [역할 분담 하네스](../ROLES.md)를 따른다.

## 시작 전

- [공통 하네스의 아트 인수 조건](../README.md#구매mcp-제작-아트의-인수-조건)을 읽는다. 맵·지형·물은 [오픈월드 맵 지침](../../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md)과 [지형·물 교훈](../../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md), 건물·구조물 배치는 [카메라 가림 가이드](../../../Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md) 체크리스트를 함께 본다.
- 작업에 맞는 기존 스킬(environment-artist, uv-workflow, lod-pipeline, collision-proxy, unity-export, qa-review, set-dressing, drop-item-world-model)만 고른다. 모든 아트에 모든 검사를 강제하지 않는다.
- 넓은 아트 작업은 `Tools/Harness/Prepare-Task.ps1 -ArtSource`로 준비표를 만든다.

## 작업

- 구매 원본과 기존 사용자 데이터를 보존한다. Git 추적 밖 산출물과 `.meta`도 인수 범위에 넣는다.
- 스케일·피벗·노멀·UV·머티리얼·셰이더·충돌·LOD·Missing 참조를 확인한다. 성능 수치는 대상 씬에서 측정한 경우에만 판정한다.
- 브리프의 Unity 슬롯 없이 Unity 에셋·씬을 쓰지 않는다. Blender 작업은 진행해도 되며 Unity 연결은 보고에 남긴다.
- C# 코드는 고치지 않는다.

## 끝낼 때

[결과 보고](../ROLES.md#결과-보고) 형식으로 돌려준다. Blender 화면 확인과 Unity 적용 화면 확인을 구분하고 캡처 경로를 남긴다. 스타일·가독성·조명·카메라 품질은 `사용자 확인 필요`로 둔다.
