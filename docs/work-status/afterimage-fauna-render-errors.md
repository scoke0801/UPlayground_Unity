# 잔상 텍스처 경고·환경 생물 MSAA 오류 수정

## 원인과 반영 — 2026-11-15

- 잔상: `ResolveMainTexture`가 `_BaseMap`·`_MainTex` 없는 VFX 머티리얼에서도 `Material.mainTexture`를 읽어 Unity 오류를 발생시켰다. 기존 두 속성 우선순위와 `[MainTexture]` 지원을 유지하면서, 대응 속성이 없으면 null을 반환한다.
- 환경 생물: `AmbientFaunaRendererFeature`가 MSAA4 활성 깊이와 단일 샘플 `_CameraNormalsTexture`를 같은 래스터 패스에 연결했다. 최종 선택된 깊이와 노멀의 샘플 수가 같을 때만 DepthNormals 패스를 쓰며, 다르면 기존 DepthOnly 패스로 깊이 출력을 유지한다. 색상 패스·전역 MSAA·품질 SO는 변경하지 않았다.
- URP 설치 소스 `UniversalRendererRenderGraph.CreateCameraNormalsTexture`에서 노멀의 단일 샘플 정책을 확인했다. MSAA 깊이 경로에서는 Fauna 노멀을 기록하지 않는 제한이 있으며, 일치하는 단일 샘플 깊이 경로는 기존 노멀 출력을 유지한다.

## 정본과 검사

- [잔상 코드](../../Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_Afterimage.cs), [Fauna 렌더 패스](../../Assets/02.Scripts/Gameplay/World/LivingWorld/AmbientFaunaRendererFeature.cs)
- [잔상 회귀 검사](../../Assets/Tests/EditMode/Combat/AfterimageTextureTests.cs): null·무텍스처·URP BaseMap·기존 MainTex.
- [GPU 렌더 검사](../../Assets/Tests/PlayMode/AmbientFauna/AmbientFaunaGpuTests.cs): 기존 색상 픽셀 검사에 MSAA4 실행을 추가했다.
- 운영 지침: [테스트](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md), [Hot Reload](../../Assets/docs/guide/HOT_RELOAD_USAGE_GUIDE.md). 수정 메서드는 allowlist 밖이므로 정상 컴파일을 사용한다.
- **Unity 정상 컴파일 완료·Console Error 0·EditMode 22/22·PlayMode 2/2 통과.** 실제 공격 연출·본편 화면 인수는 사용자 테스트 대기다.
- 기존 미추적 씬·Blender 원본·scratchpad 등을 보존했다. Player·Addressables 빌드와 커밋은 실행하지 않는다.

## 검증 결과

- 같은 Editor `UPlayground@fddd0702`에서 정상 컴파일 시작 `1794707498478`, 종료 `1794707513053`, 리로드 후 `1794707523533`을 확인했고 후속 MCP 조회가 성공했다. 처음 Play 일시정지 상태에서는 변경 메서드가 Hot Reload allowlist 밖이라 컴파일하지 않았으며, 편집 모드 전환을 관측한 뒤 요청했다.
- EditMode `c20b35a61e064f6b90946ed9742d55fa`: 잔상 신규 4건과 Fauna 데이터 기존 18건, 총 **22/22 Passed**, 실패·건너뜀 0.
- PlayMode `eaa927ff24f941149e14b385cfd05775`: 단일 샘플·MSAA4 렌더 검사 **2/2 Passed**, 실패·건너뜀 0. 격리 Fauna 씬의 색상 픽셀 출력과 오류 부재를 확인했으며 본편 전투 플레이 검증은 아니다.
- [MCP 검사 원본](afterimage-fauna-render-errors.results.json). 기존 GPU 검사가 갱신한 [픽셀 결과](../../output/AmbientFauna/Visibility/render-check.json)·[진단](../../output/AmbientFauna/Visibility/render-diagnostics.txt)·[출력 전](../../output/AmbientFauna/Visibility/butterfly-off.png)·[출력 후](../../output/AmbientFauna/Visibility/butterfly-on.png)는 보존했다. 공용 파일은 테스트마다 덮어쓰므로 마지막 단일 샘플 결과(144픽셀)이며 두 조건의 성공 근거는 MCP 결과다.
- 검사 뒤 `LakeVillageWorld` 편집 모드로 복귀, Console Error 0, `Assets/10.Datas/`·`Assets/03.Prefabs/` 추가 변경 없음. `git diff --check` 통과. 새 임시 씬·보조 빌드 산출물은 없으며 기존 미추적 씬은 작업 전부터 있어 보존했다.

## 사용자 테스트 대기

대상은 오류가 발생한 `LakeVillageWorld`와 같은 잔상 공격이다. 기존 저장 데이터와 MSAA4 설정을 유지한다.

| ID | 절차 | 기대 결과 |
|---|---|---|
| RF01 | Play에서 무기 궤적과 잔상을 쓰는 공격을 반복한다. | `_MainTex` 속성 오류 없이 몸·무기 잔상이 정상적으로 나타난다. |
| RF02 | MSAA4에서 나비 등 환경 생물 군집을 향해 이동하고 카메라를 돌린다. | `Ambient Fauna Depth` 샘플 불일치·Render Graph Execution 오류 없이 군집과 배경이 출력된다. |

회신: `ID / 통과·실패·미수행 / 실제 결과 / 실패 시 공격·위치·Console 로그`. 자동 검사와 실제 연출·노멀 관련 화면 품질 인수를 구분한다.

연결: [전투 위키](../wiki/combat.md), [월드 위키](../wiki/world-art.md), [환경 생물 기존 상태](ambient-fauna.md).
