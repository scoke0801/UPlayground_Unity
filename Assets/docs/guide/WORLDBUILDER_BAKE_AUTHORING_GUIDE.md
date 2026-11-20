# WorldBuilder 검증·Bake·발행

월드의 저작 씬과 Definition을 저장한 뒤 도구 런처의 **월드 / 맵 → WorldBuilder 지형 저작**에서 같은 Definition을 선택한다. **P07 · 검증·Bake·발행** 영역에서 월드 ID, 저작 source revision, 변경 범위, staged/Published revision과 마지막 결과를 확인한다.

1. 월드의 `NavigationLayout` SO에 실제 플레이어 Prefab과 기존 AI Agent Type을 지정한다. 지상 청크 ID는 `tile_x_z`, 동굴 Navigation 영역 ID는 `cave_<caveGuid>` 형식이다. 교량과 동굴 링크에는 실제 `bridgeGuid` 또는 `portalId`를 기록한다. 필수 경로는 시작과 도착 surface 및 좌표를 기록한다. 해당 경로가 반드시 통과할 링크가 있으면 `requiredCrossingId`를 지정한다.
2. **Validate**로 저작 상태를 점검한다. 문제의 GUID나 위치를 사용해 원인을 찾는다. 활성 Sculpt/RAW 세션은 Capture 또는 Revert로 마무리하고 씬과 SO를 저장한다. 실제 AI 경로 검사 결과는 Bake 보고서에서 확인한다.
3. **Bake**로 새 BuildId를 만든다. 진행 창에서 **Cancel**하면 다음 처리 단계에서 중단된다. Native TerrainData, 로컬 런타임 리소스, NavMeshData, 씬, catalog와 `Build.json`은 월드 폴더의 `BakeRevisions/<BuildId>/`에 저장된다. 열린 저작 씬에서 입력이 변경되면 새로 Bake한다.
4. validated revision을 선택하고 **Publish**한다. Warning에는 승인자와 조치 사유를 남긴다. `Published/Manifest.json`이 현재 발행 BuildId와 직전 BuildId를 가리킨다. **Rollback**은 무결성을 검사한 직전 BuildId로 되돌린다. 저작 Definition과 source revision은 별도로 유지된다.
5. 발행 결과를 확인하려면 Manifest의 BuildId에 해당하는 `World.unity`를 단독으로 연다. **Play**에서 `WorldBakedNavigation`이 씬에 포함된 NavMesh와 링크를 등록한다. 링크 상태를 검사할 때는 지상 청크, 교량 및 포털의 양쪽 준비 신호를 각각 확인한다. AI 경로와 실제 KCC 통과 결과는 각각 기록한다.
6. 작업자가 남겨야 할 revision은 **보존 지정 전환**으로 표시한다. **과거 revision 정리**는 선택한 validated revision의 파일 지문, 발행 상태와 프로젝트 참조를 확인한다. 현재·직전 Published, 보존 지정, 열린 씬 및 다른 에셋에서 사용 중인 revision은 정리할 수 없다. 중단 기록은 BuildId의 보고서에서 확인한다.

교량 양 끝이 같은 지상 타일에 있으면 `fromSurface`와 `toSurface`에 같은 ID를 지정한다. 두 끝점은 서로 다른 위치에 두고 실제 교량 게이트를 연결한다. Bake는 해당 surface를 한 번 등록하고 끝점과 링크 등록을 검사한다. 런타임은 두 끝점이 공유하는 surface와 교량 Collider가 준비되면 게이트를 연다.

도로가 타일 경계를 지나는 곳에서 두 NavMesh 영역이 끊기면 도로 위 경계 양쪽에 명시 링크를 둔다. 양 끝점은 각각 해당 타일의 NavMesh에 붙어야 한다. 교량이나 동굴 포털이 없는 도로 경계의 링크에는 `bridgeId`와 `portalId`를 비워 둔다.

검증 씬은 기능 재현용 월드다. 자연스러운 환경 저작과 본편 입력·카메라 확인은 별도 절차를 따른다. 검사 증거와 남은 항목은 [월드 작업 상태](../../../docs/work-status/open-world/WORLDBUILDER.md#p07-bake)에 있다. Player·Addressables와 런타임 로딩은 후속 단계에서 다룬다.

