# 툰 워터 월드 — 구현·검증 상태

- 설계 입력: [상세 구현 설계 v1.0](../전달용/toon_water_world/toon_water_world_implementation_design.html)
- 현행 통합 계약: [기존 이동·렌더링 통합](../../Assets/docs/design/TOON_WATER_EXISTING_INTEGRATION.md)
- 연결: [월드 위키](../wiki/world-art.md), [3종 공통 계약](../전달용/README.md)
- 현재 판정: **요청한 기존 수영·잠수 재사용과 수면/수중 단일 합성의 구현·Editor 자동 검증 반영 / 실제 입력·미술 품질·실기 성능은 사용자 테스트 대기**.
- 전체 원문에 포함된 GPU 생태계·수중 자원 예약/원자 보상·저장까지 완료했다는 판정은 아니다. 기존 채집 시스템을 새 원자 계약의 완료 증거로 승격하지 않는다.

## 구현과 정규 연결

Unity 6000.3.21f1 / URP 17.3.0 / Direct3D11 / PC_RPAsset 기준이다. 기존 UnderwaterRendererFeature와 LakeUnderwater.shader GUID를 유지하고 내부를 SurfaceInfo→WaterSceneResolve로 교체했다. 이전 전체 화면 수중 패스를 함께 실행하지 않는다. PC 렌더러의 Forward+·Depth Priming 경로에서 폭포 DepthNormalsOnly도 제공한다.

| 분야 | 반영한 코드·에셋 | 실제 동작 |
|---|---|---|
| 이동 재사용 | [수영 컨트롤러](../../Assets/02.Scripts/GameActor/MovementController/PlayerMovementController.Swimming.cs), [수영 상태](../../Assets/02.Scripts/GameActor/State/Player/PlayerSwimState.cs) | 기존 KCC·입수·잠수·부력·탈출 유지. 일반 수역 유속 추가, WaterfallCurrent 낙구 우선 |
| 공통 수역 조회 | [FootstepWaterSurface](../../Assets/02.Scripts/GameActor/Component/Common/FootstepWaterSurface.cs), [UnderwaterRegion](../../Assets/02.Scripts/Gameplay/World/UnderwaterRegion.cs) | 수영·발소리·펫·수중 프로필의 우선순위/높이 선택 공유. 로컬 체적·건조 구역 제외 |
| CPU/GPU 파도 | [프로필](../../Assets/02.Scripts/Data/World/WaterDynamicsProfileSO.cs), [공통 HLSL](../../Assets/06.Shaders/Water/WaterWaves.hlsl) | 월드 XZ·파수·진폭·속도·시계 공유. queryMesh와 시각 메시 분리 |
| 단일 광학 합성 | [Feature](../../Assets/02.Scripts/Gameplay/World/UnderwaterRendererFeature.cs), [SurfaceInfo](../../Assets/06.Shaders/Water/WaterSurfaceInfo.shader), [Resolve](../../Assets/06.Shaders/Water/WaterSceneResolve.shader), [공통 구간 계산](../../Assets/06.Shaders/Water/WaterCommon.hlsl) | 전용 수면 깊이·Point Body ID, 원근/정사영, near clip 경계 보정, 굴절 색/깊이 동시 회귀, 수중 감쇠·코스틱·반사·포말 |
| Fog·중복 방지 | [프레임 실행권](../../Assets/02.Scripts/Gameplay/World/WaterOpticsFrameData.cs) | 카메라당 한 번 합성. Opaque Fog는 Resolve가 공기 길이만 적용. 일반 굴절도 합성 후 별도 Opaque 스냅샷을 읽음. 투명 전 Fog 키워드·투명 후 물 플래그 복구 |
| 접촉과 비 | [고정 접촉 큐](../../Assets/02.Scripts/GameActor/Component/Common/WaterContactStamps.cs), [낙하물](../../Assets/02.Scripts/GameActor/Component/Common/WaterContactEmitter.cs) | 발·입수·수영·낙하물의 GPU 노멀/포말 스탬프. 비는 기존 날씨 강도와 선형 차폐 마스크를 사용. 이벤트/빗방울마다 GO 생성하지 않음 |
| 폭포 | [연결](../../Assets/02.Scripts/Gameplay/World/WaterfallLink.cs), [ribbon 셰이더](../../Assets/06.Shaders/Water/ToonWaterfall.shader), [WaterAware 입자](../../Assets/06.Shaders/Water/WaterAwareParticle.shader) | 상하류 참조·낙구 물살·시각 코어·지속 낙하 파문·미스트를 분리 |
| 저작 | [Lab 생성](../../Assets/02.Scripts/Editor/WaterLabAuthoring.cs), [검증기](../../Assets/02.Scripts/Editor/WaterWorldValidator.cs), [기존 씬 연결](../../Assets/02.Scripts/Editor/WaterSceneIntegration.cs) | 신규 Lab·마스크 생성, 기존 수면 원본 보존, 파도 오차/예산/렌더러 검사. 반복 검증·마스크 베이크만 툴 런처의 월드/물에 노출 |

[WaterLab](../../Assets/01.Scenes/Tests/WaterLab.unity)은 두 높이 수역·건조 동굴·다리·물고기/수초 Proxy·폭포·카메라 북마크를 가진 렌더링 재현 장면이다. 최종 아트나 플레이 가능한 새 채집 루프로 제시하지 않는다. 프로필·시각/쿼리 메시·노출 마스크·재질과 meta는 [ToonWater 데이터](../../Assets/10.Datas/World/ToonWater/)에 있다.

[정규 호수 연결 근거](../../output/ToonWaterWorld/Rendering/scene-integration.json): LakeVillageWorld의 LakeWater에 큰 호수용 48m/28m 파장 프로필, 시각 메시와 기존 96삼각형 쿼리 메시, 강수 노출 마스크를 연결했다. 시각 메시 23,377정점·46,440삼각형, GPU 체적 셀 94개, 파도 보간 오차 상한 0.01802m다. 최초 815,616삼각형 메시보다 약 94.3% 줄였다. 기존 광학 프로필·재질과 원본 메시 GUID는 보존했다.

## 최종 자동 검증

| 확인 주체·방법 | 결과 | 원본 근거 |
|---|---|---|
| Unity EditMode: GPU 계약·공통 수역·기존 수영/발소리 | **90/90 Passed, 실패/건너뜀 0**. 큰 월드 96각형의 구간 합집합, 건조 차집합, 우선순위·높이, 공기/물 길이 분리, CPU/GPU 파도, 메시 재연결 포함 | [MCP 테스트 상세](../../output/ToonWaterWorld/Rendering/editmode-90.json) |
| WaterLab: URP SingleCameraRequest, 색·광학 길이 촬영 | **14건 촬영/단언 성공**. 각 카메라 Resolve 1회, 물 플래그 복구, 용량 거절 0, 자홍색 0 | [메타데이터](../../output/ToonWaterWorld/Rendering/captures.json), [촬영 도구](../../Assets/02.Scripts/Editor/WaterLabValidation.cs) |
| Lab 반잠수·잠수·건조·정사영 | 반잠수에 공기/물 공존, 완전 잠수에 광학 길이 존재, 건조 동굴 물 픽셀 0, 정사영 물 픽셀 존재 | [반잠수](../../output/ToonWaterWorld/Rendering/HalfSubmerged.png), [건조 마스크](../../output/ToonWaterWorld/Rendering/DryCave-OpticalLength.png) |
| 불투명 Fog 소유권 | 강한 공기 Fog를 켜고 물 흡수를 0으로 둔 수중 흰 물체의 선형 중심색 RGB=1.0. 지상 Fog 중복 없음 | [Fog 단언 촬영](../../output/ToonWaterWorld/Rendering/FogOwnership-UnfoggedUnderwater.png)와 captures.json |
| 정규 호수 실제 렌더 | **위/아래·예산 뷰 4건 성공**, 각 Body 1·Cell 94·자홍색 0 | [메타데이터](../../output/ToonWaterWorld/Rendering/world-captures.json), [수면 위](../../output/ToonWaterWorld/Rendering/LakeVillage-Above.png), [수면 아래](../../output/ToonWaterWorld/Rendering/LakeVillage-Under.png) |
| Unity 셰이더 조회 | 7개 지원=true, 컴파일 오류 0. Resolve/WaterAware/Probe에는 FXC loop hint 최적화 경고 2종이 남음 | [셰이더 조회](../../output/ToonWaterWorld/Rendering/shader-audit.json) |
| 최종 Unity Console 조회 | 오류 0건. 과거 실패를 이번 통과로 숨기지 않음 | 해당 대화의 최종 read_console 응답 |

JSON은 실제 MCP 결과이며 NUnit XML을 별도 확보했다고 보고하지 않는다. 이전 단계 75건·중간 84/88건은 역사 근거이고 최종 판정은 위 90건이다.

정규 호수 첫 촬영은 약 65.8초의 콜드 셰이더 컴파일/Editor 대기를 포함한다. 이를 프레임 성능으로 취급하지 않는다. 이후 촬영의 약 7.7~9.4ms도 CPU RenderRequest 경과 시간이며 GPU 전용 시간·실기 FPS가 아니다. GPU 시간·최대 해상도·장시간 메모리/GC·Player/Addressables 검증은 **미수행**이다. 별도 빌드 지시가 없어 빌드는 실행하지 않았다.

## 검증에서 발견해 수정한 문제

- 건조 동굴 카메라가 건조 체적 밖까지 보던 구도를 바로잡고 물 픽셀 0을 재확인했다.
- 폭포에 DepthNormalsOnly가 없어 Depth Priming에서 시각 코어가 사라졌다. 같은 정점 변위/AlphaClip을 사용한 패스를 추가하고 [실제 폭포 출력](../../output/ToonWaterWorld/Rendering/WaterfallRear.png)을 확인했다.
- 큰 호수에서 인접 삼각형의 좌표 오차와 불완전 구간 병합이 32구간 예산을 소모했다. 모든 접촉 구간을 합치고 거리별 최대 2cm 허용차를 적용했다. 건조 차집합은 별도로 유지한다. 대형 좌표 GPU 회귀 검사와 정규 위/아래 촬영으로 재검증했다.
- 기존 굴절 재질이 Fog 처리 전 Cbase를 다시 읽지 않도록 합성 후 별도 읽기 스냅샷을 제공한다. 카메라 색 대상 자체를 동시에 샘플링/쓰기하지 않는다.
- 정규 촬영 초기에는 URP ReflectionProbeManager/ZBinningJob 예외가 관측되었다. 최종 SingleCameraRequest 촬영 4건과 Console 오류 0은 위 기록으로 구분한다. URP 패키지 원본은 수정하지 않았다.

## 지원 제한과 원문 후속

- 체적 미설정은 기존 수직 투영 호환 모드다. 정규 LakeWater는 이 모드를 유지하므로 모든 기존 지하 동굴이 자동 보호되었다고 판정하지 않는다. 신규 높이 수역/동굴에는 volumeBounds와 dryVolumes를 실제 공간대로 저작해야 한다. Lab의 두 높이/건조 검증과 기존 월드 전체 저작 완료는 다르다.
- 수면 뒤 임의 AlphaBlend, 한 광선의 여러 자유수면 반사/굴절, XR·모바일·TAA motion vector, 정확한 수중 SSR, planar reflection은 지원/검증 완료 범위가 아니다. 수중 반사는 원거리 매체 방사휘도 근사다.
- 접촉은 고정 이벤트 목록을 GPU에서 평가하는 프로시저럴 스탬프 경로다. 원문 예시의 지속 RT atlas/PDE solver를 구현한 것은 아니다. 현재 소규모 이벤트에서 RT 재투영·상시 메모리를 피하는 선택이며 큰 이벤트 밀도는 실제 GPU 측정 후 별도 전환한다.
- 광학 Body/Cell/Dry/Stamp 예산과 구간 예산을 넘기면 진단/대체 경로로 드러낸다. 입력 검사 통과를 최악 시점 성능 통과로 해석하지 않는다.
- 원문의 GPU 장식 생물 확장, CPU 자원 예약→재검증→보상/소모/저장 원자 커밋과 스트리밍/리스폰 매트릭스는 별도 미구현/미검증 범위다. 기존 GatheringActor·GroundDrop·WorldState API를 새 계약으로 완료 처리하지 않는다. 이번 작업에서 아이템/보상·저장 파일은 수정하지 않았다.

## 사용자 직접 테스트 — 사용자 테스트 대기

대상은 위 변경본의 Unity Editor다. 원본 저장 슬롯을 덮지 않는 테스트 상태에서 키보드 또는 게임패드를 사용한다. 기존 툴 런처의 `월드/맵 → 호수·여울마을 도보 연결`로 진입하고, 실패 시 항목 ID·시작 위치/저장 상태·입력 장치·실제 증상과 Console 로그를 남긴다.

| ID | 절차 | 기대 결과·실패 기준 |
|---|---|---|
| WU01 | 얕은 물→입수→수면 이동→잠수→상승→물가 탈출 | 기존 조작/충돌/부력 유지. 얕은 물 발소리와 입수/이동 파문 확인. 수면 밖 수영·경계 상태 진동·입력 먹통·예외는 실패 |
| WU02 | 카메라를 수면 위·아래로 천천히 이동하고 near clip에 수면을 겹침 | 화면의 공기/수중 영역 공존. 전체 화면 색 점프·이중 착색·수면 소실·전경 굴절 번짐은 실패 |
| WU03 | WaterLab을 열어 SceneView로 두 높이 수역과 건조 동굴 확인 | 상류/하류 독립, 건조 공간 수중 효과 없음. Lab은 렌더 fixture이며 bookmark Camera는 기본 비활성이다. GameView 검수는 한 Camera만 활성화 |
| WU04 | 기존 폭포에서 상류→낙구→하류 이동 | 낙구 부력 해제·하류 수영 복귀. Lab에서는 ribbon·낙하 파문·미스트와 비/지붕 마스크 확인 |
| WU05 | 같은 수중 공간에서 게임 카메라·SceneView·해상도/화면 크기 변경 후 씬 이탈/재진입 | 카메라 간 상태 누출·남은 물 플래그·자홍색·예외·지속 메모리 증가 없음 |
| WU06 | 실제 플레이 해상도/AA에서 근접·원경·야간 관찰 | 물속 대상·외곽선 식별, 물선 안정성·반사·포말·음영이 자연스러운지 직접 판단. 자동 수치만으로 미술 인수하지 않음 |

회신: `WU ID / 통과·실패·미수행 / 실제 결과 / 입력 장치·시작 상태 / 화면 또는 로그`.

## 인수 파일·보존·정리

- Git 추적 밖 LakeVillageWorld와 meta를 변경 전에 [원본 백업 기록](../../output/ToonWaterWorld/Backup/source.json)에 따라 보존했다. 원본 씬 150,836,134바이트이며 당시 사용자 변경 보호용이다. 현재 정본으로 사용하거나 이후 사용자 변경 위에 무조건 복원하지 않는다.
- [산출물 목록](../../output/ToonWaterWorld/final-paths.json)은 코드·셰이더·씬·SO/메시/재질/마스크와 문서를 포함한다. 57개 명시 경로와 해당 Assets meta 존재를 확인했다. Git 추적 밖 파일도 인수 범위다.
- 최초 과다 세분화 메시 `Assets/10.Datas/World/ToonWater/Visual_8f86c22e4495e294bbe82f739fd30304.asset`(45,727,165바이트) 삭제는 자동 승인 검토가 거절했다. 한 씬 외의 참조/보존 필요성이 확인되지 않았다는 사유다. 우회 삭제하지 않고 asset/meta를 보존했다. 현재 호수의 실제 연결은 위 scene-integration.json의 2,056,889바이트 최적화 메시다.
- 검사 임시 Camera·Mesh·Material·SO·RT는 finally에서 해제했고 외부 패키지·사용자 SO/아트·다른 작업 파일은 원복/삭제하지 않았다. 커밋하지 않았다.- 최종 [인수 준비표와 검증 기록](../../output/harness/20261112-231205-8b96c977/review.md)에 단계별 위키·출처·보존 판단을 연결했다.

## MSAA 4배 사용자 오류 후속

사용자 실제 Game/SceneView에서 `DrawTransparentObjects`의 깊이 4배와 `Water Scene Resolve` 색상 1배가 연결되어 RenderGraph 컴파일이 실패했다. 기존 90건·14건·정규 4건 통과는 단일 샘플 검사였으며 MSAA 통과 근거가 아니다. 뒤따른 ZBinningJob 예외는 선행 그래프 실패 이후 발생했으므로 먼저 첨부물 불일치를 수정하고 재확인한다.

- 수정: 합성 출력의 카메라 색 MSAA 유지. 셰이더에서 샘플링하는 불투명 스냅샷만 단일 샘플로 생성한다. 프로젝트 MSAA 설정은 낮추지 않는다.
- 검증: 기존 Lab 검사에 1/2/4배 출력과 실제 색·깊이 샘플 수 단언을 추가. 각 14건, 총 **42건 통과**. [1배](../../output/ToonWaterWorld/Rendering/MSAA-1/captures.json), [2배](../../output/ToonWaterWorld/Rendering/MSAA-2/captures.json), [4배](../../output/ToonWaterWorld/Rendering/MSAA-4/captures.json). 모든 출력에서 요청/색/깊이 샘플 수 일치, 자홍색 0. 반잠수·정사영·건조 공간·Fog 소유권 검사도 각 배율에서 통과.
- 정규 LakeVillageWorld의 [MSAA 4배 위/아래 2건](../../output/ToonWaterWorld/Rendering/MSAA-4/world-captures.json)도 실제 색4/깊이4, 자홍색0, Body1/Cell94로 통과. 검사 후 프로젝트 MSAA=4 유지, 물 전역 플래그=0 복구. 현재 Unity Console 오류0으로 후속 ZBinningJob 예외도 재발하지 않음. URP 패키지/Job 코드는 수정하지 않았다.
- 이전 90건은 이번 수정 후 재실행한 결과로 표기하지 않는다. 이번 회귀 검증은 변경한 RenderGraph 연결의 실제 URP 출력 44건이다. Player 빌드·장시간 플레이·실기 성능은 미수행.
- 사용자 재확인 WU07: 기존 오류가 난 Game/SceneView에서 MSAA 4배를 유지하고 수면 위↔아래 카메라 이동·씬 재진입. 기대: DrawTransparentObjects MSAA 불일치 및 ZBinningJob 오류 없이 화면 출력. 실제 플레이 확인은 사용자 테스트 대기.


## 수면 경계와 수중 비 사용자 피드백 후속

- 확인: 관리 Resolve 경로가 재질의 Shallow/Deep 색과 SurfaceOpacity를 적용하지 않았고, Weather Particle/EnvironmentEffects에 수중 차폐 연결이 없었다.
- 구현: 수면 위에서만 기존 깊이 색·최소 불투명도 복구. 수면 법선에 따른 해안 포말 변주. 카메라별 잠수 플래그와 수면 깊이로 날씨 조각 차단, 잠수 관측점에서는 강수 방출 중단·잔여 비/눈/먼지 제거 및 재부상 복구.
- 검증 진행: 기존 수면/MSAA 검사에 실제 Weather Particle의 공기 중 표시·잠수 카메라 차단·수면 뒤 차단 및 CPU 입자 정리/재부상 회귀 검사 추가. 자동 검사와 사용자 미술 판정은 구분한다.

### 수면 경계·비 수정 검증 결과

- 관련 EditMode **24/24 통과**: 기존 수역/GPU 광학·보호 구역과 새 잠수 강수 중단→잔여 입자 제거→재부상 복구 검사. [원본 결과](../../output/ToonWaterWorld/BoundaryRainFix/editmode-24.json).
- MSAA 1/2/4배 각 **17건**, 총 **51건 통과**. 기존 14건에 실제 Weather Particle 재질의 공기 중 표시·잠수 카메라 차단·수면 뒤 차단 3건을 더했다. [1배](../../output/ToonWaterWorld/Rendering/MSAA-1/captures.json), [2배](../../output/ToonWaterWorld/Rendering/MSAA-2/captures.json), [4배](../../output/ToonWaterWorld/Rendering/MSAA-4/captures.json). 공기 중 붉은 검사 입자는 중심 R=1, 잠수/수면 뒤에서는 표시되지 않음. 각 카메라 후 물 활성·잠수 플래그 0 복구. 실제 강수 전체 플레이 인수와 구분한다.
- 최종 정규 호수 MSAA 4배 **3구도 통과**: [눈높이 해안](../../output/ToonWaterWorld/BoundaryRainFix/Shore-EyeHeight.png), [높은 시점](../../output/ToonWaterWorld/BoundaryRainFix/Shore-HighAngle.png), [잠수](../../output/ToonWaterWorld/BoundaryRainFix/Lake-Under.png), [카메라·샘플 메타데이터](../../output/ToonWaterWorld/BoundaryRainFix/shore-captures.json). 요청/색/깊이 모두4, 자홍색0. AI가 실제 PNG를 열어 얕은 색→깊은 색·해안 대비와 반사/파문을 확인했다. 사용자 미술 인수로 대체하지 않는다.
- 처음 선택한 메시 외곽 시점은 지형 안쪽/물 없는 공간을 보아 최종 미술 근거에서 제외했다. 전체 조망에서 실제 노출 수역을 찾은 뒤 위 눈높이/높은 시점으로 다시 확인했다. BoundaryRainFix의 그 밖 초기 캡처는 탐색 기록이다.
- 정규 전용 재질 `Assets/10.Datas/World/OpenWorld/LakeVisualV2/LakeWater.mat`의 불투명도·반사·파문·포말과 얕은/깊은 색·최소 조명을 보정했다. [수정 전 재질](../../output/ToonWaterWorld/BoundaryRainFix/LakeWater.before.mat)을 보존했고 GUID/meta·씬·충돌·수영 설정은 변경하지 않았다. 구매 원본은 건드리지 않았다.
- 최종 [셰이더 조회](../../output/ToonWaterWorld/BoundaryRainFix/shader-audit.json): Weather/Resolve 모두 지원, 오류0. Resolve의 기존 loop hint 경고2는 남는다. [Console 오류0](../../output/ToonWaterWorld/BoundaryRainFix/console.json). Player/Addressables 빌드·장시간 플레이·실기 GPU 시간은 미수행.
- 사용자 재확인 **WU08**: 비/폭풍 상태에서 눈높이로 물가 접근→카메라 잠수→수중 이동/위쪽 보기→재부상. 기대: 물가와 수면이 구분되고 잠수 화면에는 비/눈 입자가 안 보이며, 재부상하면 비가 복귀한다. 수면 빗방울 파문은 유지된다. 게임패드/키보드, 위치·시간/날씨와 화면을 함께 남긴다. **사용자 테스트 대기**.
