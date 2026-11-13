# Non Humanoid lilToon 전환

기준일: 2026-11-13. 판정: **일반28개 + Stone Black/Normal2개 반영. Dryad 유지. Inferno는 표현 보존 예외로 원본 Graph 유지. 자동/API 검사와 정지 화면 확인 완료, 실제 플레이·최종 미술은 사용자 테스트 대기.**

[분야 위키](../wiki/world-art.md) · [최초 검토와 32개 목록](non-humanoid-toon-shader-review.md) · [검증 지침](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md)

## 1단계 — 현재 상태 대조

열린 Unity 6000.3.21f1의 AssetDatabase로 검토 목록 32개를 다시 로드했다. 연결이 일시적으로 끊겼으나 재연결 후 조회했다. 기존 LakeVillageWorld는 Play·일시정지 상태였으며 조사 중 씬 저장·전환은 하지 않았다.

- MeshRenderer/SkinnedMeshRenderer의 몸체·장비 919슬롯, 고유 재질 34개: URP/Lit 30, lilToonMulti 1, 전용 Graph 3. 일반 28개·Dryad 1개·Stone Golem 3개라는 분류가 현재 에셋과 일치했다. 비활성·LOD·파괴 조각을 포함한 참조 수이며 동시 드로우 수가 아니다.
- 원본 프로퍼티를 재조회했다. 일반군은 모두 Surface=0, AlphaClip=0, Cull=2. 식물도 Opaque 유지. Ent V1의 queue=1970도 보존 대상이다.
- URP metallic 맵 R과 smoothness A를 lilToon metallic R·smoothness R로 분리해야 한다. 맵이 있으면 URP의 scalar metallic=0을 그대로 곱하지 않는다.
- AO는 lilToon의 그림자 경계 마스크로 이관하여 공동·틈의 어두움을 유지한다. URP 간접광 AO와 동일한 광학 결과는 아니다.
- 기존 계층 변환기는 공유 원본을 바꾸며, P09ToonLookProfileSO.ValidateBindings는 같은 셰이더를 요구한다. 둘 다 이번 일괄 전환에 사용하지 않는다.
- 플레이어 Raon과 NPC Jun의 실제 재질에서 그림자 경계·명도 하한·외곽선 설정을 조회했다. 피부용 그림자색은 몬스터에 복사하지 않는다.

근거: [실제 슬롯 목록](../../output/non-humanoid-toon/inventory-before.tsv), [프로퍼티 조회](../../output/non-humanoid-toon/materials-before.tsv), [Graph 연결 추적](../../output/non-humanoid-toon/stone-graph.txt).

Stone Graph는 단순 스크롤이 아니라 Time×0.05가 용암 보조 텍스처의 UV **타일링**에 연결되어 있다. 눈·용암·적설·거미줄 발광이 별도로 합산된다. 정지 베이크만으로 Inferno를 동등하게 바꿀 수 없으므로 이 표현을 제거하는 교체는 하지 않는다. 후속 제작에서 정적 합성과 동적 합성을 구분한다.

기존 다수 미커밋·구매 원본·GUID/meta 보존. 이 단계에서 게임 에셋 변경 없음. 준비표: output/harness/20261113-144028-865fd8b4. 조회 로그는 다음 단계 입력·검증 근거로 보존한다.

## 2단계 — 대표 복제본 비교

프로젝트 소유 [복제 재질 폴더](../../Assets/11.Material/NonHumanoidToon)에서 Golem·Griffin Brown·Main Plant V1·Spider Minion V1 재질 4개를 제작했다. [원본 대응](../../output/non-humanoid-toon/material-map-representatives.tsv). [제작 스크립트](../../ArtSource/NonHumanoidToon/create-materials.csx)는 MCP 메서드 본문으로 사용하며 새 런타임 시스템을 추가하지 않는다.

[비교 촬영](../../ArtSource/NonHumanoidToon/render-comparison.csx)은 행동 컴포넌트 없는 시각 복제본을 Unity PreviewScene에서 렌더한다. Raon(오른쪽)·Jun(왼쪽)을 같은 조명·카메라에 배치했다. 55도 perspective, 1280×720, Golem/Plant/Spider는 거리7m, Griffin10.889m. 조명0=낮,1=밤,2=역광. 원본과 복제본을 각각 촬영해 총24장. 메인 씬·리그·Animator·MotionSet·액터 스크립트는 변경하지 않았다.

화면에서 실루엣·표면 무늬·발광 위치 유지, 과도한 흰 툰 반사 보정 후 일반군 확대의 기술적 표본으로 판단했다. Griffin 역광의 명도가 원본보다 높아지는 것은 툰 조명의 변화이며 최종 미술 인수는 사용자 테스트 대기다. 외곽선은 가는 다리/잎에 두껍게 겹치지 않도록 낮게 저작했다.

| 대표 | 낮 원본 / 교체 | 밤 원본 / 교체 | 역광 원본 / 교체 |
|---|---|---|---|
| Golem | [전](../../output/non-humanoid-toon/Golem_before_0.png) / [후](../../output/non-humanoid-toon/Golem_after_0.png) | [전](../../output/non-humanoid-toon/Golem_before_1.png) / [후](../../output/non-humanoid-toon/Golem_after_1.png) | [전](../../output/non-humanoid-toon/Golem_before_2.png) / [후](../../output/non-humanoid-toon/Golem_after_2.png) |
| Griffin | [전](../../output/non-humanoid-toon/Griffin_before_0.png) / [후](../../output/non-humanoid-toon/Griffin_after_0.png) | [전](../../output/non-humanoid-toon/Griffin_before_1.png) / [후](../../output/non-humanoid-toon/Griffin_after_1.png) | [전](../../output/non-humanoid-toon/Griffin_before_2.png) / [후](../../output/non-humanoid-toon/Griffin_after_2.png) |
| Plant | [전](../../output/non-humanoid-toon/Plant_before_0.png) / [후](../../output/non-humanoid-toon/Plant_after_0.png) | [전](../../output/non-humanoid-toon/Plant_before_1.png) / [후](../../output/non-humanoid-toon/Plant_after_1.png) | [전](../../output/non-humanoid-toon/Plant_before_2.png) / [후](../../output/non-humanoid-toon/Plant_after_2.png) |
| Spider | [전](../../output/non-humanoid-toon/Spider_before_0.png) / [후](../../output/non-humanoid-toon/Spider_after_0.png) | [전](../../output/non-humanoid-toon/Spider_before_1.png) / [후](../../output/non-humanoid-toon/Spider_after_1.png) | [전](../../output/non-humanoid-toon/Spider_before_2.png) / [후](../../output/non-humanoid-toon/Spider_after_2.png) |

이는 정지 자세의 Unity 비교이며 TPS 실제 조작·이동/공격·LOD 전환·실기 성능을 통과한 근거가 아니다. MCP 통신 오류 시에는 출력 파일 존재와 시각을 확인해 중복 생성을 피했다.

## 3단계 — 정규 프리팹 반영과 예외

[제작 원본과 재현 절차](../../ArtSource/NonHumanoidToon/README.md) · [원본/복제 GUID·사용 프리팹 manifest](../../output/non-humanoid-toon/manifest.json) · [최종 슬롯 조회](../../output/non-humanoid-toon/inventory-after.tsv).

| 범위 | 적용 | 대응·실제 슬롯 기록 |
|---|---|---|
| Plant/Child/Root9, Griffin2, Spider6, Lich2, Ent2, Skeleton3, 일반 Golem1, 허수아비1, 카메라 시험2 | 28개 / 복제 재질30개 / 903슬롯 | [원본→복제](../../output/non-humanoid-toon/material-map-all.tsv), [프리팹별 수](../../output/non-humanoid-toon/applied-material-map-all.tsv) |
| Stone Black·Normal | 2개 / 복제 재질2개 / 10슬롯 | [원본→복제](../../output/non-humanoid-toon/material-map-stone.tsv), [프리팹별 수](../../output/non-humanoid-toon/applied-material-map-stone.tsv) |
| Dryad | 기존 lilToonMulti 유지 | [동일 조명 비교](../../output/non-humanoid-toon/Dryad_before_0.png). 꽃/의상의 높은 명도는 원본 특성으로 남겼고 임의 교체하지 않았다 |
| Stone Inferno | 원본 Graph 및 5슬롯 유지 | 아래 동적 용암 예외. 표현 제거 없음 |

합계30개 프리팹·32개 복제 재질·913슬롯 교체. 현재 몸체/장비919슬롯은 lilToon계열914(Dryad1 포함), Inferno Graph5다. 고유 재질 수는 여전히34개다. VFX·AlwaysVisible·파티클은 교체 대상에서 제외했다. 중첩 카메라 시험 모델은 대상 액터에 재질 슬롯 override만 추가했다.

원본 BaseMap/색/UV, 노멀/강도, 발광/HDR RGB, metallic R, smoothness A→R, AO G→그림자 경계 마스크, Cull/큐를 이관했다. 추가 레이어/UV/렌더 모드가 조사 범위를 벗어나면 스크립트가 중단한다. 원본 발광 맵에 alpha 채널이 없음을 확인했고, URP에서 사용하지 않는 발광 색 alpha는1로 정규화했다. [BaseMap alpha 검사](../../output/non-humanoid-toon/base-alpha.tsv)에서는 Griffin·Plant 등의 실제 alpha0을 확인했다. 불투명 원본이 무시하던 alpha가 사망 Cutout 전환 때 구멍으로 되살아나지 않도록 복제본의 AlphaMaskMode=1/Scale=0/Value=1을 명시했다.

Golem·Stone Golem은 실루엣과 반사 표현을 유지하면서 외곽선 없는 lilToon으로 최종 조정했다. 나머지는 얇은 Outline 변형을 사용한다. 피부 MatCap/얼굴 그림자는 복사하지 않았다. Spider에만 낮은 림을 추가했고, 표면의 금속/거칠기 구분은 원본 마스크를 유지했다.

### Stone 정적 합성

Graph의 실제 슬롯을 따라 cavity R, dirt G, curvature B, 하체 마스크 G로 색을 순서대로 Lerp하고 원본 BaseColor를 곱했다. 매끄러움은 원본 Graph 그대로 ORM.G×_Roug이며 반전하지 않았다. 노멀은 원본을 공유한다. 눈동자 발광20, Black 거미줄0.06, Normal 적설1의 합산은 HDR EXR로 분리했다. 최종 해상도4096을 유지했다.

[Black 전](../../output/non-humanoid-toon/StoneBlack_before_0.png) / [후](../../output/non-humanoid-toon/StoneBlack_after_0.png), [Normal 전](../../output/non-humanoid-toon/StoneNormal_before_0.png) / [후](../../output/non-humanoid-toon/StoneNormal_after_0.png). 눈·적설·거미줄의 위치를 보존했고, 조명 반응의 명암은 툰으로 달라진다. Black/Normal 바탕·매끄러움 출력이 SHA256까지 같음을 확인해 Black 이름의 공유 파일로 합쳤다. 중복2파일 및 meta 75,380,903바이트를 정리했다.

### Inferno 예외 — 미전환

Graph는 Lava(UV0) + Snow(UV0×0.05×Time)×Lava.R을 발광에 합산한다. 시간에 따른 **타일링 배율**이므로 표준 lilToon의 이동/회전 설정으로 동등하게 보존할 수 없다. 정지 베이크나 스크롤로 바꾸면 용암의 변화가 달라진다. 따라서 원본 Graph를 유지했고 이1종의 시각적 이질감은 남는다.

대안은 정적 표면 베이크와 이 UV 수식만 추가한 lilToon 사용자 정의 발광 확장이다. 이 경우 사망 Cutout·디더 변형에서도 같은 수식을 유지해야 하므로 추가 셰이더 변형·실제 전투 검증이 필요하다. 이번에는 이름 기반 런타임 보정이나 표현 삭제를 추가하지 않았다.

## 4단계 — 검사와 화면 확인

| 검사 | 확인 주체·방식 | 결과·한계 |
|---|---|---|
| 원본 재질34 + 기존 meta66 | SHA256 자동 비교 | [100/100 보존](../../output/non-humanoid-toon/preservation-results.json). 구매 원본·대상 GUID 유지 |
| 대상32개 재질 외 직렬화 | 작업 전 사본 대조 | 32/32 보존. Unity 저장이 추가한6개 일반군과2개 Stone의 자동 필드만 분리 제거. 행동·리그·MotionSet·전투 데이터 유지 |
| 최종32개 프리팹 재로드 | 열린 Unity AssetDatabase | 몸체/장비919슬롯, 누락 참조·Missing Script0 |
| 셰이더 | Unity ShaderUtil | 복제본 lilToon/Outline 및 베이크 셰이더 오류0 |
| 기존 관련 EditMode | Test Runner | [18/18 통과](../../output/non-humanoid-toon/existing-tests.json): ActorColorChanger·ActorCameraProximityDither·ActorOutlineLod |
| 최종 복제 재질32개 | 실제 컴포넌트의 Editor 상태 주입/API | [32/32 통과](../../output/non-humanoid-toon/feedback-materials.tsv): 피격 MPB 적용/원복, 사망 Cutout/디더 재질 생성, 메인/발광 텍스처 유지. 실제 전투·풀 수명·시간 진행 화면을 대신하지 않는다 |
| 대표4종 낮/밤/역광 | Unity PreviewScene 정지 화면 | 전후24장, 최종 Golem 외곽선 제거 후 교체 화면 갱신. 이동·공격·LOD 전환 미수행 |
| 정규 월드 조명 | LakeVillageWorld 실제 카메라의 임시 구도·시각 복제본 | [전](../../output/non-humanoid-toon/Golem_world_before.png) / [후](../../output/non-humanoid-toon/Golem_world_after.png). Raon/Jun과 같은 프레임 조명으로 촬영. 이2장은 외곽선 제거 **전 단계**의 톤 확인 근거다. 최종 외곽선 제거 뒤에는 Play 카메라가 없어 월드 재촬영하지 못했으며 최종 Preview는 위에 별도 보존했다. 실제 조작 TPS 통과가 아니다 |
| 최종 미술·긴 플레이 | 사용자 | 사용자 테스트 대기 |

월드 최초 촬영의 수관 가림은 비교 근거로 채택하지 않고 열린 구도 촬영으로 교체했다. 화면 상단의 기존 배경 차폐 영역은 이번에 수정하지 않았다. 정규 프리팹 반영 전부터 ActorDatabase 준비 timeout3건이 Console에 조회됐고, 세션 중 반응형 환경 저장 호환 오류1건도 관찰했다. 재질 이외의 원인은 이번에 수정하지 않았으며, Console 전체 오류0·월드 초기화/실제 플레이 통과를 주장하지 않는다.

### 성능 — 제한된 Editor 비교

동일 Golem16개·4×4배치/간격4m·카메라32m/FOV55·1280×720·동일 조명, 5회 워밍업+40회 동기 Preview.Render 호출을 측정했다. CPU i5-12400F, GPU RTX3060Ti. **CPU의 호출 경과 시간이며 GPU 시간·게임 전체 프레임·실기 성능이 아니다.**

| 판본 | 중앙값 | p95 | 원본 기록 |
|---|---:|---:|---|
| URP 원본 | 2.8259ms | 3.6832ms | [40표본](../../output/non-humanoid-toon/Golem_cpu_before.tsv) |
| 초기 Outline 교체본 | 4.9398ms | 5.3517ms | [40표본](../../output/non-humanoid-toon/Golem_cpu_after-outline.tsv) |
| 최종 Golem 외곽선 제거 | 3.1164ms | 3.6512ms | [40표본](../../output/non-humanoid-toon/Golem_cpu_after.tsv) |

최종 중앙값은 원본보다 약0.29ms 높다. 단일 Editor 표본이므로 성능 개선이나 전체 프레임 예산 통과를 주장하지 않는다. 다른 종·다수 전투·GPU/메모리/실기 비교는 NH08 대기다.

## 사용자 직접 테스트 명세

현재 판정은 전 항목 **사용자 테스트 대기**다. Unity6000.3.21f1에서 기존 씬/작업을 보존하고 [Boot](../../Assets/01.Scenes/GameLogic/Boot.unity)의 정상 게임 시작 경로로 LakeVillageWorld에 진입한다. 별도 테스트 저장 슬롯을 사용하고 키보드/마우스 또는 게임패드를 기록한다. 개발 치트 패널의 스폰 탭에서 ActorDatabase의 대상 액터를 검색해 마리 수·전방 거리를 지정하고 '플레이어 전방에 스폰'을 사용한다. 데이터베이스에 없는 카메라 시험2개/허수아비는 프리팹 정지 검수와 실제 전투 대상을 구분한다. 원본 재질을 수정하거나 프리팹에 Play 변경을 Apply하지 않는다.

| ID | 절차 | 기대 결과·실패 기준 |
|---|---|---|
| NH01 | Golem/Skeleton, Griffin, Plant, Spider를 Raon/Jun과 같은 장소에서 낮·밤·역광 비교. 2/5/10/20m 거리 | 종·표면·눈/약점이 식별되고 새 분홍색/검은 덩어리·빛 번짐·과도한 외곽선 없음 |
| NH02 | 각 대표를360도 관찰하고 걷기/달리기/피격/공격을 각3회 확인 | 날개·가는 다리·잎/뿌리·장비의 선 끊김/팽창/겹침·스키닝 이상 없음 |
| NH03 | LODGroup이 있는 개체에서 전환 거리를 천천히 왕복하고 화면 밖으로 보낸 뒤 재진입 | 재질 복귀·외곽선·발광 위치 유지. LOD가 없는 개체는 거리/컬링만 기록 |
| NH04 | 연속 피격→플래시 종료, 피격 중 비활성/재등장 | 원래 색과 MPB로 복구. 눈/약점 발광이 손상되거나 빨강 잔류 없음 |
| NH05 | 사망→디졸브 완료→풀 복귀/재소환을5회 반복 | 사라지기 시작할 때 alpha 구멍 없음. 재사용 후 색·불투명도·발광·슬롯·렌더러 활성 복구. 동일 인스턴스 재사용 확인이 없으면 풀 검증은 미수행으로 기록 |
| NH06 | 카메라를 몸 표면~중심으로 접근한 뒤 충분히 이탈. 피격/사망과 겹쳐 확인 | 디더 전환/원복 정상, 원래 투명도·재질 복구, 외곽선만 남거나 실루엣이 잔류하지 않음 |
| NH07 | Black 거미줄·Normal 적설·Inferno 용암/눈동자를 낮/밤에20초 관찰 | Black/Normal 합성 위치 유지. Inferno는 원본처럼 시간에 따라 용암 밀도가 변함. Dryad 밝은 의상의 일관성도 사용자 판단 |
| NH08 | 동일 종16개, 같은 카메라/조명/해상도/후처리/AI 상태로 원본 대응표와 최종 재질을 Play 인스턴스에서만 비교 | Profiler CPU/GPU·메모리·Frame Debugger 패스를 기록. 원본 공유 재질 편집 금지. 워밍업 뒤 동일 구간을 측정하고 개체수/기기/값 없이 통과로 판정하지 않음 |

회신: ID / 개체·거리·조명·동작 / 통과·실패·미수행 / 실제 증상 / 화면·영상·Profiler 또는 Console. 전체32개 동시 소환은 요구하지 않는다. 빌드는 수행하지 않는다.

## 부산물·인계

원본 모델/텍스처·기존 미커밋·GUID/meta 보존. 커밋·Player·Addressables 빌드 없음. 새 런타임 분기·프레임워크 없음. 새 준비표는 output/harness/20261113-152338-bcf23f78이다.

최종 복제 재질32개·파생 마스크18PNG·Stone4EXR와 meta는 납품 에셋으로 총167,286,239바이트다. 캡처31장 약6MB·조회/검사 로그·제작 스크립트는 재개/검증 근거로 보존한다. 프리팹 상당수가 gitignore 대상이므로 [교체 전 사본](../../output/non-humanoid-toon/prefab-baseline)30개/9,207,964바이트는 원래 상태의 유일한 복구 근거일 수 있어 삭제하지 않는다. 이번에 만든 중복 베이크2개 및 meta만 제거했고 임시 Preview/월드 복제 객체는 매 촬영 후 정리했다. [관련 문서 로컬 링크](../../output/non-humanoid-toon/link-check.json)147개 누락0과 변경 diff의 공백 검사를 확인했다.
