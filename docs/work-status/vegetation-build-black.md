# 식생 빌드 검정 출력

- BUG-VEGETATION-BUILD-01 / S3 / 씬 참조 RP의 빌드 수집 누락 수정 반영, 사용자 테스트 대기. 실제 빌드 화면의 해결은 아직 검증하지 않았다.
- 사용자 원문: “빌드에서 풀, 나무 등이 전부 까맣게 나오는 버그가 있음”. 첨부 화면에서 가까운 풀·나무·일부 바위의 검정 출력과 꽃·지형의 색 출력이 함께 관찰된다.
- 재현: Windows Development Player, 빈도·마지막 정상 시점·회귀 여부·저장 슬롯·입력 장치·정확한 캐릭터/무기 정보는 모름. 씬은 화면과 현재 프로젝트로 LakeVillageWorld 추정이며 확정 아님.
- 기대: 식생과 바위의 원래 색·음영 유지. 실제: 검정 실루엣 출력.
- 실제 Player.log: 2026-11-15 21:55, Unity 6000.3.21f1 / D3D11 / RTX 3060 Ti. 셰이더 오류·공유 버퍼 경고는 발견되지 않았다. 기존 NavMesh agent 생성 실패 다수가 있으며 이번 범위 밖이다.
- 조사: 환경 반응 파생 셰이더 12종에 공통 `VegetationReactionState` 미초기화 경고. 본편 Trilight 주변광과 베이크 조명 데이터 없음도 확인했으나 원인으로 확정하지 않았다.
- 기존 셰이더·환경 런타임의 미커밋 변경은 보존한다. Player/Addressables 빌드는 별도 지시가 없어 실행하지 않는다.

## 거리 의존 제보와 빌드 수집 누락

- 사용자 추가 원문: “멀리있는건 괜찮고 가까이있는게 이상하게보인다”. 추가 화면에는 근거리 나무·풀 외에 지붕과 글라이더도 검게 보인다. 이전 HLSL 경고 수정만으로 전체 증상을 설명할 수 없으며 새 사진의 빌드 갱신 여부는 미확정이다.
- 기본 `PC_RPAsset`은 SSAO/Decal 활성, 실행 시 `SceneEnvironmentRendering`이 선택하는 `LakeVillage_RPAsset_Ambient`는 SSAO/Decal 비활성이다. 런타임 전용 RP는 기본/품질 설정 목록에 없고, 글로벌 `IncludeAdditionalRPAssets.includeReferencedInScenes`와 `includeAssetsByLabel`이 모두 false였다.
- 설치된 Unity Core `Editor/BuildTargetExtensions.cs`는 기본/품질 RP 외의 씬 참조 RP를 위 옵션이 켜진 경우에만 수집한다. URP `ShaderBuildPreprocessor.cs`는 수집된 모든 Renderer의 SSAO가 활성일 때 SSAO off 변형을 제거한다. 따라서 실제 실행 경로에 필요한 변형을 빌드에서 제거할 수 있는 **확정된 설정 결함**이다. 사용자 사진과의 인과관계 최종 확인은 새 빌드가 필요하다.
- [글로벌 설정](../../Assets/Settings/UniversalRenderPipelineGlobalSettings.asset)의 `m_IncludeReferencedInScenes`를 1로 변경했다. LakeVillageWorld는 현재 EditorBuildSettings 및 Development 빌드 프로필의 포함 씬이다. 기존 SSAO off·그림자 25m 최적화·재질·LOD는 유지한다. Addressables 전용으로만 로드하고 Player 씬 의존성에 전혀 없는 RP는 이 옵션의 포함 대상이 아니므로 별도 label/품질 등록이 필요하다.
- 실제 씬 1,608,581행의 `_renderPipeline`이 GUID `60140dc81ad77624695c9f527e515b6f`를 참조하는 것과 해당 RP→Renderer GUID `e490e390172dee24badc651d231f0299` 연결, Development 프로필의 월드 씬 참조를 파일에서 확인했다. 옵션·씬·Renderer 참조 정적 검사 통과, 변경은 글로벌 설정 1줄이다. 글로벌 설정의 `git diff --check` 통과(위키에는 이번 변경 밖의 기존 EOF 공백 경고가 있다).
- **Unity 네이티브 수집 검증 통과:** 글로벌 설정 재임포트 후 `UnityEditor.Rendering.BuildTargetExtensions.TryGetRenderPipelineAssets`가 true를 반환했고, `includeReferencedInScenes=true`, 목록에 `Assets/Settings/PC_RPAsset.asset`와 `Assets/10.Datas/World/AmbientFauna/Rendering/LakeVillage_RPAsset_Ambient.asset` 두 개가 포함됨을 확인했다. 응답이 지연됐지만 최종 결과를 수신했다. 별도 URP 전체 기능 목록 조회는 시간 초과여서 통과로 승계하지 않는다. Player/Addressables 빌드 없음. 이전 경고 제거를 이 새 설정 수정의 검증 근거로 승계하지 않는다.
- 콘텐츠와 Player를 모두 새로 만드는 **전체 빌드**로 VB01~02를 재확인해야 한다. 기존 번들을 재사용하거나 기존 Player에 콘텐츠만 교체한 결과는 이 수정의 전체 반영 근거가 아니다. 이번 세션에서는 실행하지 않았다.

## 반영과 보조 검증

- [공유 상태 HLSL](../../Assets/06.Shaders/Environment/VegetationReaction.hlsl)의 `VegetationReactionState`를 명시 초기화·단일 반환으로 변경했다. 원래 상태 값, 지형 범위 밖의 0, 유효 인덱스의 공유 버퍼 조회를 유지한다. 기존 낙목 소거 변경은 보존했다.
- Unity Editor D3D11: 파생 셰이더 12종의 기존 `use of potentially uninitialized variable (VegetationReactionState)` 경고가 수정 후 0개, 셰이더 오류 0개임을 직접 조회했다. 다른 구매 셰이더 유래 pow/구형 API 경고까지 없앴다는 의미는 아니다.
- [성공한 GPU 출력 원본](vegetation-build-black-probe.json): 본편 실제 메시/재질을 선택한 9종에서 일반 그리기의 공유 버퍼 off/on 픽셀 수·평균 색이 동일하며, 정상 색 출력이 존재했다. SH를 명시한 인스턴싱 9종도 검정 이외의 색을 출력했다. 일반/인스턴싱은 직접광 전달 조건이 달라 서로의 색 일치를 주장하지 않는다.
- 초기 비교는 안개색으로 표면색이 덮여 무효였다. 초기 인스턴싱 검정은 검사 입력의 SH 누락으로 생긴 별도 현상이었고 입력 보완 후 사라졌다. 이를 사용자 버그의 원인으로 해석하지 않는다.
- [재현 도구](../../Tools/ReactiveEnvironment/vegetation-build-black-probe.csx)는 최종적으로 일반/인스턴싱 각각 공유 버퍼 off/on 4모드를 비교하도록 보완했다. 이 최종 확대 실행은 MCP 시간 초과·연결 단절로 결과 미수신이므로 통과로 기록하지 않는다. 열린 Editor 재기동·강제 종료·캐시 삭제·씬 저장은 하지 않았다.
- 런타임 조명 변경·구매 원본 수정은 하지 않았다. Trilight/조명 데이터 관련 조사는 원인 미확정이다. 검정 출력의 다른 원인이 남아 있을 수 있다.

## 사용자 재확인

기존 제보와 같은 저장 슬롯·위치·시간대에서 확인한다. 이 세션에서는 빌드를 실행하지 않았다. 새 셰이더가 들어간 콘텐츠를 사용해야 하므로 기존 콘텐츠를 재사용하는 PlayerOnly 결과만으로 판단하지 않는다.

| ID | 절차 | 기대 / 실패 기준 | 현재 |
|---|---|---|---|
| VB01 | 같은 위치의 가까운 풀·나무·바위를 관찰하고 카메라 회전 | 잎·풀·껍질·바위 색과 그림자가 구별됨. 검정 실루엣 지속 시 실패 | 사용자 테스트 대기 |
| VB02 | 나무에 접근했다가 멀어져 LOD 전환 관찰 | 근거리/원거리 모두 원래 색 유지 | 사용자 테스트 대기 |
| VB03 | 비 또는 기존 환경 반응이 있는 위치 확인 | 젖음/그을림이 해당 대상 상태에만 반영됨 | 사용자 테스트 대기 |

회신: `VB01~03 / 통과·실패·미수행 / 실제 결과`. 실패 시 같은 장소의 Editor Play 비교 결과와 새 Player.log를 대조한다.

## 정리

- 일시 캡처는 인수 근거가 아니므로 삭제했다. 성공한 수치 원본과 재현 csx만 보존한다. 사용자 씬·원본 구매 에셋·기존 미커밋 변경은 원복하지 않는다.
- [월드 위키](../wiki/world-art.md)
