# 연속 월드 구현의 외부 근거

2026-10-25 조사. 아래 판단은 설치 버전과 실제 저장소 제약을 조합한 설계 판단이다. 다른 엔진의 구현을 Unity 기능으로 취급하지 않는다. 패키지를 업그레이드하지 않았다.

| 자료 / URL | 적용 버전 | 핵심 제약 | 프로젝트 적용 판단 |
|---|---|---|---|
| [씬 로딩](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/LoadingScenes.html) | Addressables2.9.1 | additive 씬의 활성화 대기는 다른 비동기 로드도 막을 수 있음. 씬 밖으로 옮긴 객체가 의존 자산의 소유권을 자동 이전하지 않음 | activateOnLoad=true, 콘텐츠는 비활성 root에서 별도 준비. 파티/카메라 참조는 상주 자산 또는 명시적 lease |
| [메모리 관리](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/MemoryManagement.html), [자산 수명](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/memory-assets.html) | 2.9.1 | refcount 감소가 즉시 물리 메모리 반환과 같지 않음. bundle 수명 영향 | 씬·자산 handle 회계와 메모리 profiler를 별도로 검증. 경계마다 UnloadUnusedAssets 호출 금지 |
| [작업 핸들](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/AddressableAssetsAsyncOperationHandle.html) | 2.9.1 | 사용 중 handle 유지, 실패 handle도 해제, 해제 후 재사용 불가 | 지역당 소유자 하나, 실패/늦은 성공/unload 실패의 정리 경로를 시험 |
| [의존성과 중복](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/AssetDependencies.html) | 2.9.1 | 여러 bundle이 같은 비 Addressable 자산을 참조하면 중복 포함 가능. 일부 자산 사용도 bundle 의존을 불러옴 | 공용 지형 재질·수목·캐릭터 의존 검사 후 명시적 공유 그룹 검토. Build Layout 검증은 빌드 승인 이후 |
| [SetActiveScene](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html) | Unity6000.3 | 새 루트 생성 대상과 조명 설정 기준. 렌더링할 씬을 고르는 기능이 아님 | 논리 mapId와 분리. 생성 scene/parent를 명시하며 Save가 Active Scene만 읽지 않게 함 |
| [allowSceneActivation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html) | 6000.3 | false일 때 진행0.9·isDone=false, 뒤의 비동기 작업도 정체 | 스트리밍 queue를 이 방식으로 봉쇄하지 않음. 요청 취소는 결과 폐기이며 engine cancel API로 간주하지 않음 |
| [backgroundLoadingPriority](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-backgroundLoadingPriority.html) | 6000.3 | 백그라운드 읽기 외 메인 스레드 통합 비용 존재. 우선순위 예산은 Player에 적용, Editor에는 효과 없음 | Editor 로드 측정은 준비 오류 찾기에 사용. Player 프레임 예산 달성을 주장하지 않음 |
| [Terrain.SetNeighbors](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Terrain.SetNeighbors.html) | 6000.3 | 인접 Terrain의 LOD 연결, 모든 이웃 관계 설정 필요 | 높이·틈·Collider·물 높이를 자동 봉합한다고 가정하지 않음. 현재 작은 마을 중첩은 별도 표면 저작 필요 |
| [NavMeshLink](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshLink.html) | 웹2.0.16 / 설치2.0.14 | 같은 agent type의 Surface 연결, 다른 씬 연결·unload 수명 관리 필요 | 설치된 Documentation~/NavMeshLink.md 및 런타임 소스를 기준으로 사용. Link 비활성 후 경로 재요청, KCC 통과 별도 검증 |
| [Navigation 변경 이력](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/changelog/CHANGELOG.html) | 2.0.14→2.0.16 | 2.0.15의 먼 원점 bounds 수정, 2.0.16의 Terrain tree 수집 변경은 베이크 결과에 영향 | 최신 웹 결과를 설치 환경 보장으로 사용하지 않음. 이번 조사만으로 버전 변경하지 않음 |
| [Light Probe와 씬 로딩](https://docs.unity3d.com/6000.3/Documentation/Manual/light-probes-and-scene-loading.html) | 6000.3 | additive 로드/unload 후 tetrahedralization 자동 보장 없음, 재계산 비용 존재 | 여러 지역 변경을 묶어서 비동기 재계산. 상태/완료 시점 계측 |
| [다중 씬 베이크](https://docs.unity3d.com/6000.3/Documentation/Manual/bakemultiplescenes.html) | 6000.3 | 씬 간 그림자·간접광을 함께 계산하지만 Lightmap은 씬별 로드/해제. 함께 베이크한 Light Probe 데이터는 함께 로드 | 지역 분리만으로 Probe 메모리도 분리된다고 가정하지 않음. 재배치 후 옛 베이크를 재사용하지 않음 |
| [RemoveNavMeshData](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AI.NavMesh.RemoveNavMeshData.html) | 6000.3 | 제거된 데이터는 agent와 경로 query에서 사용할 수 없음 | Link 차단과 진행 중 경로 재평가를 unload 절차에 포함. KCC 경로 소비자를 별도로 검사 |
| [URP Volumes](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/Volumes.html) | Unity6000.3 / 설치URP17.3 | 위치와 Volume 설정을 통한 환경 혼합 | global baseline 하나와 지역 local Volume. 지역별 global Volume 경쟁을 피함 |
| [URP Reflection Probes](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/lighting/reflection-probes-introduction.html) | 6000.3 / URP17.3 기준 대조 | volume/blend distance/importance·렌더 경로에 따른 반사 선택 | 경계가 box 면에서 보간되도록 중첩. 모든 probe를 지역 전역 설정으로 취급하지 않음 |
| [다중 씬 Occlusion](https://docs.unity3d.com/6000.3/Documentation/Manual/occlusion-culling-scene-loading.html) | 6000.3 | 함께 로드될 씬은 공동 데이터로 베이크. 독립 베이크의 자동 합성 아님 | 현재 씬의 서로 다른 occlusion 자산을 그대로 믿지 않음. 검증된 공동 bake 전 occlusion 없는 기준으로 측정 |
| [Guerrilla: Elijah Houck](https://www.guerrilla-games.com/read/guerrilla-spotlight-elijah-houck) | 2022, Horizon/Decima 제작 사례 | 길의 굽이·조망·관심 지점·생활 사건을 통한 탐험 설계 | Unity 기능이 아닌 공간 설계 원칙. 호수–마을 시야 열림과 귀환 인지를 기존 지형에 적용 |
| [Guerrilla: 퀘스트의 액션/RPG 균형](https://www.guerrilla-games.com/read/balancing-action-and-rpg-in-horizon-zero-dawn-quests) | GDC2018, Decima | 서사 퀘스트와 열린 세계의 시스템적 활동을 함께 설계한 발표 | 접근 경로의 자유와 사건 선행조건을 분리. 이 페이지는 발표 소개 근거이며 링크된 전체 발표를 읽었다고 주장하지 않음 |

선행 로드 거리는 문서의 고정 권장값을 복사하지 않는다. 실제 최대 접근속도, 경계 시야, 해당 기기의 로드/준비 지연 분포로 산정한다. 현재 실측값이 없으므로 거리·FPS·메모리 목표는 미확정이다.

## 대표 구간 구현 시 추가 확인

- Addressables 2.9.1 설치 소스의 `SceneProvider` 및 [2.9 씬 로딩 문서](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/LoadingScenes.html)를 대조했다. 지역 씬은 `activateOnLoad:true`로 로드하고, 비활성 콘텐츠 루트에서 공개를 분리한다. 언로드 연산이 로드 핸들을 해제하므로 호출자가 같은 로드 핸들을 다시 Release하지 않는다. 실패한 로드 핸들만 별도 Release한다.
- [Unity 6000.3 TerrainData.SetHeights](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TerrainData.SetHeights.html)를 기준으로 편집 시 복제 TerrainData에 높이를 적용한다. 원본 TerrainData를 변경하지 않으며 실행 중 지형 갱신 방식으로 사용하지 않는다.
- [SceneManager.CreateScene](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.CreateScene.html)의 설치본 실행 확인 결과 EditMode 테스트에서는 사용할 수 없었다. `EditorSceneManager.NewPreviewScene`으로 Test Runner의 미저장 씬을 건드리지 않고 격리하되 Preview Scene은 Active Scene으로 지정할 수 없다. 이 제약 때문에 테스트 구성을 수정했다.
