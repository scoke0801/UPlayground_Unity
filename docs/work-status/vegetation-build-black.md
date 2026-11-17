# 식생 빌드 검정 출력

- BUG-VEGETATION-BUILD-01 / S3 / 2026-11-16 지정 빌드의 SSAO/DBuffer off 변형 누락 확인, 설정·캐시 판정 수정 반영. 아래 「지정 빌드 내부 분석과 수정」이 최신 판단이다. 수정본 Player 화면은 사용자 테스트 대기다.
- 사용자 원문: “빌드에서 풀, 나무 등이 전부 까맣게 나오는 버그가 있음”. 첨부 화면에서 가까운 풀·나무·일부 바위의 검정 출력과 꽃·지형의 색 출력이 함께 관찰된다.
- 재현: Windows Development Player, 빈도·마지막 정상 시점·회귀 여부·저장 슬롯·입력 장치·정확한 캐릭터/무기 정보는 모름. 씬은 화면과 현재 프로젝트로 LakeVillageWorld 추정이며 확정 아님.
- 기대: 식생과 바위의 원래 색·음영 유지. 실제: 검정 실루엣 출력.
- 실제 Player.log: 2026-11-15 21:55, Unity 6000.3.21f1 / D3D11 / RTX 3060 Ti. 셰이더 오류·공유 버퍼 경고는 발견되지 않았다. 기존 NavMesh agent 생성 실패 다수가 있으며 이번 범위 밖이다.
- 조사: 환경 반응 파생 셰이더 12종에 공통 `VegetationReactionState` 미초기화 경고. 본편 Trilight 주변광과 베이크 조명 데이터 없음도 확인했으나 원인으로 확정하지 않았다.
- 기존 셰이더·환경 런타임의 미커밋 변경은 보존한다. Player/Addressables 빌드는 별도 지시가 없어 실행하지 않는다.

## 지정 빌드 내부 분석과 수정 — 2026-11-16

- 사용자 후속: “확정하고 고쳐줘”, “C:\\UsingProject\\UnityProject\\BuildOuptut_BuildManager 빌드는 이거야”. 지정 빌드를 읽기 전용으로 분석했다. 새 Player/Addressables 빌드는 실행하지 않았다.
- **확정된 결함:** `data.unity3d/sharedassets3.assets`의 실제 Shader 객체를 파싱해 식생·지형 Forward Fragment 프로그램15종을 조사했다. **15종 모두 SSAO off가0개**이며, `_DBUFFER_MRT3`를 지원하는 식생·Terrain/Lit도 DBuffer off가0개다. 예: 풀 `2ae88eff...`384개 전부 SSAO on/DBuffer on, 반응 Lit `933532...`384개도 동일, Terrain/Lit128개도 동일하다. [빌드 내부 변형 수](../../Logs/VegetationBuildInvestigation/20261116-player-shader-variants.json). 호수 Renderer는 SSAO/Decal off라 필요한 조합이 존재하지 않는다. 파생 셰이더 컴파일 성공이나 RP2개 수집은 이 누락을 검증하지 못했다.
- **발생 경로:** [Unity6000.3 ShaderKeywordFilter 문서](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ShaderKeywordFilter.FilterAttribute.html)는 네이티브 사전 필터가 QualitySettings의 RP 타입 트리를 사용한다고 명시한다. Quality에는PC RP 하나뿐이고 그 Renderer 전부가 SSAO on이라 `everyRendererHasSSAO=true`→`SelectOnly`가 된다. 씬 RP 수집 옵션은 URP의 후속 스크립트 스트리핑 목록을 늘리지만 먼저 없어진 off 변형을 복구하지 못한다. SSAO off가 없어진 상태에서는 호수용 SSAO off/DBuffer off 조합도 남지 않는다.
- **수정:** [PC_RPAsset](../../Assets/Settings/PC_RPAsset.asset)의 Renderer 목록에 기존 호수 Renderer를 두 번째로 추가하고 SSAO 사전 필터를 Select(1)로 맞췄다. 기존 Renderer0·기본 인덱스0은 유지한다. URP 전처리가 같은 Quality RP 안의 on/off 두 Renderer를 평가하므로 사전 필터와 후속 필터가 모두 필요한 조합을 보존한다. 전역 스트리핑 해제·구매 셰이더 수정·씬 분기는 없다.
- **재사용 결함 보완:** [BuildManagerFingerprint](../../Assets/02.Scripts/Tool/Editor/Build/BuildManagerFingerprint.cs)가 Player와 콘텐츠 양쪽에서 Graphics/Quality 설정 및 RP/글로벌RP 에셋의 의존성 해시를 포함한다. 주소 지정 에셋에 직접 연결되지 않은 렌더 설정 변경도 기존 콘텐츠/Player 지문을 무효화한다. 이미 만들어진 실행 파일이나 번들을 이 코드가 사후 수정하지는 않는다.
- **주변광 후보 정정:** [빌드 RenderSettings](../../Logs/VegetationBuildInvestigation/PlayerData/104-747623.json)의 SH27계수는0이지만, 격리 Editor 씬에서 동일 Trilight 색을 적용하면 `ambientProbe`를0으로 넣어도 getter가 유효한 계수를 반환했다. Custom 모드에서는0, Trilight 전환 뒤에는 비영 값이었다. 따라서 저장된 SH0만으로 주변광 누락을 확정할 수 없으며 이번에는 조명 코드를 변경하지 않았다. Trilight의 `ambientIntensity`가 무효인 별도 낮밤 조정 문제는 [공식 API 설명](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RenderSettings-ambientIntensity.html)에 근거한 범위 밖 후보로 남긴다.
- **회귀 검사 통과:** [검사](../../Assets/Tests/EditMode/BuildManager/BuildManagerRenderingTests.cs)에서 수정 전 `everyRendererHasSSAO=true`로 의도한 실패를 재현했다(job `3d99830452c54f1ba1431dafe5e24117`). 수정 후32/32 통과에 이어 DBuffer off 조합과 Player/콘텐츠 지문을 각각 보강한 **최종 BuildManager EditMode33/33 통과**(job `0d3e43c46393460c9f451203d3f64076`), 정상 컴파일·Console 오류0 확인. 최초 검사에는 reflection public 필드 누락으로 별도 오류1건이 있었고 검사 코드를 바로잡은 뒤 원래 결함을 재현했다. [빌드 SHA256·검사 식별](../../Logs/VegetationBuildInvestigation/20261116-fix-verification.json).
- **검정 출력 GPU 재현 통과:** 동일 `933532...` 식생 셰이더·초록 재질·직접광0·주변광을 유지하고 DBuffer 키워드만 바꿨다. 효과 입력을 생산하지 않는 Renderer를 대조하도록 DBuffer 입력은0이다. off는 중심RGB `(0.03529412,0.196078435,0.007843138)`, 빌드에 남은 on은 **RGB `(0,0,0)`**, 양쪽alpha1이다. DBuffer가 없는 실행 경로에서 on 변형이 선택되면 검정이 되는 경로를 재현했다. [수치](../../Logs/VegetationBuildInvestigation/20261116-dbuffer-gpu-probe.json), [정상](../../Logs/VegetationBuildInvestigation/dbuffer-0.png), [검정](../../Logs/VegetationBuildInvestigation/dbuffer-1.png), [재현 csx](../../Tools/Rendering/vegetation-feature-variant-probe.csx). 이는 Editor의 격리 GPU 비교이며 수정본 Player 화면 확인을 대신하지 않는다. 원본 씬·재질 변경 없이 전역 키워드/벡터/텍스처·비동기 컴파일·RT를 복원하고 임시 Unity 오브젝트를 제거했다.
- **범위 보존:** 기본 Renderer0과 기본 인덱스0을 유지했다. 포함된 Title/Loading/LakeVillageWorld 카메라는 `m_RendererIndex=-1`이고 코드의 `SetRenderer(1)` 사용은 발견되지 않았다. 이전에 무효였던 Renderer1을 카메라가 선택하는 회귀 근거는 없다. 기존 SSAO/그림자 최적화와 호수 씬·구매 원본을 수정하지 않았다.
- **분석 근거:** Unity 공식 UnityDataTools의 UFS API와 설치된6000.3.21f1 네이티브 DLL로 Player 아카이브를 읽었다. 타입 정보가 제거돼6000.3.20f1 공개 구조 데이터로 해석했고 모든 대상 Shader/RenderSettings의 소비 바이트 수가 객체 크기와 정확히 일치함을 검사했다. [실제 풀 Shader의 키워드·프로그램 원본](../../Logs/VegetationBuildInvestigation/20261116-built-grass-keywords.json), [분석 소스 보관](../../Logs/VegetationBuildInvestigation/player-data-inspection-source.cs.txt). 전체 빌드 복제/추출·다운로드 도구·중간 덤프·보조 컴파일 출력을 포함한 임시 `output/VegetationBuildFix/` **5,098,957,222bytes를 제거**했다. 사용자 지정 빌드와 원본 Library는 보존했고, 작은 로그·메타데이터·GPU 캡처9파일/616,702bytes만 보존했다. 준비표는 `output/harness/20261116-232537-a78f3ea4/`다.
- **남은 확인:** 수정 적용에는 콘텐츠와 Player의 **전체 재빌드**가 필요하다. 기존 `BuildOuptut_BuildManager`는 조사 대상 그대로이며 새 빌드 실행·새 실행 파일의 VB01~03 화면 확인은 별도 지시 전 **미수행/사용자 테스트 대기**다. 재빌드 후 위 내부 검사에서 식생·Terrain/Lit의 SSAO off/DBuffer off 프로그램이0보다 큰지, 원래 위치08:58의 근/원거리 식생·지형·건물에 색과 음영이 구별되는지 확인한다. 환경광 보정으로 증상을 덮지 않았다.

## 2026-11-16 재제보 조사 — 수정 전 판단

- 사용자 원문: “빌드에서 [Image #1] 식생이 이렇게 보이는 원인 조사 요청”. 사진에서 가까운 수관·풀·지형·건물은 매우 어둡고, 하늘·HUD·꽃잎은 색을 유지한다. 먼 수목의 밝기는 안개 합성으로도 생길 수 있어 원거리 LOD 정상의 단독 증거로 삼지 않는다. 빈도·회귀·정확한 저장 슬롯·사진의 빌드 식별은 모름. 이전과 같은 BUG ID를 유지한다.
- 로컬 `C:/UsingProject/UnityProject/BuildOuptut_BuildManager/BuildManager.manifest.json`: 콘텐츠 완료 **11월16일20:40:48 KST**, Player 완료 **21:01:08 KST**. Boot/Title/Loading/LakeVillageWorld가 포함된다. 성공 보고는 오류0·경고169다. 이 경로와 사용자 사진의 동일 빌드 여부는 미확정이다.
- 기존 Editor.log에는 `PC_RPAsset`와 `LakeVillage_RPAsset_Ambient` **2개 RP 수집 기록**, 두 RP/Renderer의 Player 포함 기록, 식생 파생 셰이더 컴파일 기록이 있다. [발췌 근거](../../Logs/VegetationBuildInvestigation/20261116-build-excerpt.txt). 이 수집 기록만으로 Addressables 안의 개별 변형이 전부 보존됐다고 판단하지 않는다.
- `933532a4fcc9baf4fa0491de14d08ed7`의 ForwardLit은 스크립트 스트리핑 후 VP48/FP384변형이 남는다. GBuffer VP/FP0은 현재 Renderer의 **Forward+** 경로에서 정상적으로 제거될 수 있으므로 이를 결함으로 해석하지 않는다. 모든 Forward 변형이나 셰이더 자체가 빠졌다는 근거는 없다.
- 현재 기본 RP의 SSAO 사전 필터는2(SelectOnly), 호수 RP는0(Remove)다. 설치된 URP `ShaderBuildPreprocessor.GetSupportedShaderFeaturesFromAssets`는 **RP마다 별도 필터를 계산**하므로 기본 RP의2만으로 호수용 SSAO off 변형 누락을 단정할 수 없다. 앞선 조사보다 이 구분을 보완했다.
- 글로벌 설정 파일 수정 시각은 **20:50:19 KST**로 콘텐츠 완료보다 늦다. 현재 `m_IncludeReferencedInScenes=1`인 것은 확인했지만, 수정 시각만으로20:40 콘텐츠가 같은 설정을 사용했는지는 알 수 없다. 콘텐츠/Player의 설정 불일치와 번들 변형 누락은 남은 후보다. [콘텐츠 지문 계산](../../Assets/02.Scripts/Tool/Editor/Build/BuildManagerFingerprint.cs)은 그룹·프로필·주소 지정 에셋 의존성을 해시하며 글로벌 RP 수집 설정을 명시적으로 추가하지 않는다. 이번에는 수정하지 않았다.
- 최신 Player.log(수정22:55:34 KST)는 Unity6000.3.21f1/D3D11/RTX3060Ti를 사용한다. `shader|render|buffer|exception|error|ambient` 검색에서 식생 셰이더 오류·공유 버퍼 오류는 발견하지 못했다. 오류 로그 부재는 변형 일치나 올바른 SH값의 증거가 아니다. 기존 Fauna 호출 스택·NavMesh 문제 등은 이번 범위 밖이다.

### 우선 후보와 판별 방법

1. **그림자 안의 주변광(SH) 누락/불일치.** [호수 씬](../../Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity)의 Trilight 색은0이 아니지만 `m_LightingDataAsset=0`, baked/realtime lightmap은 비활성이다. [WorldLightingController.ApplyAmbient](../../Assets/02.Scripts/Gameplay/World/WorldLightingController.cs)는 Trilight에서 강도만 변경하고, 런타임 전체 검색에 `ambientProbe=`/`DynamicGI.UpdateEnvironment` 경로는 없다. 호수 RP 그림자 거리는25m다. 주변광이 없으면 가까운 그림자는 검고 그림자 거리 밖/안개 영역은 밝아지는 증상과 부합한다. **런타임 SH를 측정하지 않았으므로 가설이다.** LightingData가 없다는 사실만으로 결함을 확정하지 않는다. Unity6의 환경광 자동 생성 변경은 [공식 업그레이드 지침](https://docs.unity3d.com/6000.0/Documentation/Manual/UpgradeGuideUnity6.html)의 Environment lighting 절에 근거한다.
2. **실제 번들의 SSAO/조명 변형 불일치.** RP 수집 보정과 콘텐츠 생성 시점이 같은지 미확정이다. 검은 draw의 실제 키워드와 셰이더 변형을 확인해야 한다. 현재 파일 설정만 보고 기존 빌드 해결을 선언하지 않는다.
3. **환경 반응 버퍼의 상태/인덱스 이상.** 공유 버퍼는32바이트 stride로 HLSL과 일치하고, 정상 상태는 원본 색을 유지한다. 앞선 일반/인스턴싱 보조 검사는 실제 Player의 GPU Resident Drawer·런타임 상태를 검증한 것이 아니다. 검은 표면의 `_EnvState0`/실제 공유 버퍼 인덱스·surface를 확인해야 제외할 수 있다. 사진의 건물·지형 어둠 때문에 식생 전용 결함으로 범위를 한정하지 않는다.

다음 판별은 같은 빌드·위치·08:58에서 **검은 draw1개와 밝은 꽃1개**를 비교한다. Development Player에 Frame Debugger를 연결해 실제 RP/패스·키워드(`_SCREEN_SPACE_OCCLUSION`, `_MAIN_LIGHT_SHADOWS`, `_CLUSTER_LIGHT_LOOP`, `_ENVIRONMENT_REACTION_BUFFER`)·주변광 SH·그림자/AO 입력·표면 상태를 확인한다. 진단 변경이 필요한 경우 그림자만 해제/명시적 주변광만 공급/반응 버퍼만 해제를 각각 독립적으로 비교하고 원상 복구한다. 동시에 여러 설정을 바꾼 재빌드는 인과관계 근거로 삼지 않는다. 누락 변형 판별에는 [Unity 공식 설명](https://unity.com/blog/engine-platform/shader-variants-optimization-troubleshooting-tips)의 Strict Shader Variant Matching도 사용할 수 있다.

**수행/한계:** 정적 코드·씬·설정·기존 빌드/Player 로그 조사만 수행했다. Editor는 다른 작업의 WorldBuilder Preview 씬/EditMode였으므로 씬 교체·Play·GPU 전역 상태 변경은 하지 않았다. 읽기 전용 RP 수집 MCP 재조회는 연결 종료로 결과 미수신이며 통과가 아니다. Player 실행 중 SH/키워드·Frame Debugger 비교는 **미수행**이다. 런타임 코드·재질·씬·설정 수정, Player/Addressables 빌드, 커밋 없음. 작은 로그 발췌만 보존하며 삭제할 임시 산출물은 없다.

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
