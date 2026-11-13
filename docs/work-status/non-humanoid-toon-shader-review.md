# Non Humanoid 액터 툰 셰이더 전환 검토

이 문서는 적용 전 검토 기록이다. 이후 실제 변경·예외·검증은 [전환 작업 상태](non-humanoid-toon-migration.md)를 따른다.

기준일: 2026-11-13. 판정: **기술적으로 적용 가능. 일반 URP/Lit군 우선 전환을 권장하고 Stone Golem 3종은 별도 이식한다.**
요청 범위는 검토다. 코드·머티리얼·프리팹·씬을 변경하지 않았으며 실제 교체 렌더·미술 인수·성능은 미검증이다.

## 범위와 조사 방법

[분야 위키](../wiki/world-art.md), [공통 하네스](../../Tools/Harness/README.md), [테스트 지침](../../Assets/docs/guide/TEST_VALIDATION_GUIDE.md), [NPC 외형 지침](../../Assets/docs/guide/NPC_APPEARANCE_AUTHORING_GUIDE.md)을 읽고 열린 Unity Editor의 AssetDatabase로 실제 프리팹·Renderer.sharedMaterials·Shader 프로퍼티를 조회했다. 활성 씬은 `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity`, Play 상태는 false였다. 씬을 열거나 저장하지 않았다. 기존 다수 미커밋 변경은 보존했다.

Non Humanoid는 이번 검토에서 사람 캐릭터 외형이 아닌 몬스터군으로 해석했다. Unity Avatar.isHuman만으로 분류하면 Ent·Skeleton·Golem을 빠뜨린다. 이들은 실제 Humanoid Avatar를 사용하지만 이번 검토 대상에 포함했다.
`Assets/03.Prefabs/Actor/Monster/`에서 사람 캐릭터 5개와 Humanoid 하위 폴더를 제외한 **32개**를 조사했다. 이 중 카메라 시험용 2개와 훈련 허수아비 1개를 포함한다. 야생동물·환경 생물·모든 외부 구매 프리팹 전체를 조사한 결과는 아니다.

몸체·장비의 MeshRenderer/SkinnedMeshRenderer가 참조하는 고유 재질은 **34개: URP/Lit 30개, lilToonMulti 1개, Stone Golem 전용 Graph 3개**다. 비활성 자식과 LOD를 포함한 에셋 참조 집계이며 동시 드로우 수가 아니다. 28개 프리팹은 URP/Lit군, 1개는 기존 lilToon, 3개는 전용 Graph군이다.

## 기준 셰이더와 호환성

플레이어 모델과 ScenarioResidents NPC의 실제 재질에서 `lilToon`, `Hidden/lilToonOutline`, Cutout/Transparent/Outline 변형과 `_lil/lilToonMulti`를 확인했다. 기준은 단일 재질이 아니라 lilToon 셰이더 계열이다. 일반·Outline·CutoutOutline·Multi 모두 현재 Editor에서 Shader.Find 성공 및 isSupported=true를 확인했다. 이는 교체본의 렌더 통과나 Player variant 검증을 뜻하지 않는다.

이미 비인간형 리그의 `Monster_Dryad`가 lilToonMulti를 사용한다. 셰이더 전환 때문에 Animator·Avatar·MotionSet을 바꿀 필요는 없다. 다만 같은 셰이더만 지정한다고 플레이어·NPC와 동일한 톤이 되지는 않는다. 원본 텍스처에 그려진 음영, 노멀 강도, 그림자 경계·색, 림·반사·외곽선 폭을 종별로 보정해야 한다.

## 대상별 판단

| 대상 | 현재 몸체 | 판단과 필요한 보정 |
|---|---|---|
| Plant/ChildPlant/RootPlant 9개 | URP/Lit | 전환 가능. 발광 색과 노멀·반사 보정. 조사한 몸체 재질은 Opaque/AlphaClip=0이므로 식물 이름만으로 Cutout 처리하지 않는다 |
| Griffin 2개 | URP/Lit | 전환 가능. 깃털·날개 실루엣과 명암 보정. Dark의 Smoke VFX는 유지 |
| SpiderMinion/Queen 6개 | URP/Lit | 전환 가능. 갑각 반사와 눈/표식 발광 보정, 가는 다리 외곽선 검수 |
| Lich 2개, Ent 2개 | URP/Lit | 전환 가능. 발광·AO와 세부 음영 보정, 연기 재질 유지 |
| Skeleton 3개 | URP/Lit | 전환 가능. 뼈·장비·무기의 금속감 구분과 여러 재질 슬롯 보존 |
| MonsterActor_Golem 1개 | URP/Lit | 일반 전환군. 아래 Stone Golem 3종과 다른 구매 모델 |
| 훈련 허수아비 1개, CameraTestMap 2개 | URP/Lit | 전환 가능. 시험 모델의 코어 발광·돌/금속 구분 유지 |
| Dryad 1개 | lilToonMulti | 교체 불필요. 다른 대상의 리그 호환 선례이며 톤 보정만 별도 판단 |
| Monster_Golem_Black/Inferno/Normal 3개 | Stone_Golem_ShaderGraph | 단순 교체 부적합. 아래 전용 합성 이식 필요 |

### Stone Golem 예외

원본은 `Assets/ExternalAssets/Character/Stone_Golem/shaders/Stone_Golem_ShaderGraph.shadergraph`다.
표준 _BaseMap/_Color 대신 자동 생성 이름의 텍스처 프로퍼티와 _Base, _cavity, _dirt, _Curvature, _down_leg, _lava, _Snow, _web, _Color_emisive, _eye 등을 사용한다.
실제 값은 Inferno의 _lava=1, Normal의 _Snow=1, Black의 _web=0.06이며 세 재질 모두 _eye=20이다. 단순 셰이더 교체는 종류별 표면 합성과 눈 발광을 보존하지 못한다.

정적 표면 색 합성을 텍스처로 베이크하고 눈·용암을 lilToon 발광 마스크로 분리하는 방식을 우선 검토한다. 움직이는 용암/적설/거미줄 표현까지 같은 결과가 필요하면 원본 Graph 연결·시간 의존성 추가 조사 후 lilToon 레이어/스크롤 또는 전용 확장이 필요하다. 이번에는 Graph 전체 노드의 시간·공간 계산을 추적하거나 베이크하지 않았다. 기존 표현을 없애는 전환은 승인된 것으로 보지 않는다.

## 프로퍼티 이관과 기존 도구의 한계

| 원본 | 전환 시 처리 |
|---|---|
| _BaseMap / _BaseColor | lilToon _MainTex / _Color, UV scale/offset 보존 |
| _BumpMap / _BumpScale | 노멀 텍스처·강도와 _UseBumpMap 활성화 확인 |
| _EmissionMap / _EmissionColor | 텍스처·HDR 색·활성 상태 보존. 눈/약점이 밤에 과도하게 빛나지 않게 검수 |
| _MetallicGlossMap / _Smoothness | lilToon 반사 설정에 맞춰 채널·강도 확인. 동일 결과를 가정하지 않음 |
| _OcclusionMap | AO의 반영 방식 선택. 기존 AO 위에 강한 툰 그림자를 더하면 지나치게 어두워질 수 있음 |
| Surface/AlphaClip/Cull/Cutoff/Queue | 실제 원본 값을 기준으로 이관. LOD와 양면 여부도 검수 |
| 외곽선·툰 그림자 | 신규 저작 사항. NPC 재질 전체를 복사하지 않고 캐릭터 규모·실루엣에 맞춰 설정 |

[기존 lilToon 계층 변환기](../../Assets/02.Scripts/Tool/Editor/LilToonHierarchyConverter.cs)는 메인 텍스처/색, 노멀 텍스처, 발광, 렌더 모드의 기본 이관을 제공한다. 그러나 **sharedMaterials 원본을 직접 수정**하고 모든 Renderer를 스캔하므로 구매 원본·다른 프리팹·VFX까지 영향을 줄 수 있다. 노멀 강도, AO, metallic/smoothness, Cull/Cutoff와 특수 Graph 합성을 완전히 이관하는 도구가 아니다. 기본 대상 셰이더도 외곽선 없는 lilToon이며 NPC의 톤 설정을 자동 적용하지 않는다. 도구의 되돌리기 버튼은 원본 셰이더만 복구하므로 전체 상태의 영구 백업으로 보지 않는다.

[P09ToonLookProfileSO](../../Assets/02.Scripts/Editor/P09Builder/Model/P09ToonLookProfileSO.cs)는 원본과 교체 재질의 셰이더가 같아야 검증을 통과한다. 기존 lilToon의 톤 변형용이며 URP→lilToon 전환 프로파일로 그대로 사용할 수 없다.

후속 적용 시 프로젝트 소유 재질 복제본을 만들고 대상 프리팹의 명시된 몸체/장비 슬롯만 교체한다. 구매 원본·GUID·리그·모션과 기존 VFX는 보존한다. 같은 원본 재질을 쓰는 Plant/RootPlant·Lich 변형 등은 교체 재질 공유 범위를 명시한다. 이름 기반 런타임 분기나 매 프레임 셰이더 전환은 필요하지 않다.

## 전투 피드백과 미검증

[ActorColorChanger](../../Assets/02.Scripts/GameActor/Component/Common/ActorColorChanger.cs)는 lilToon의 _Color를 지원한다.
[DissolveController](../../Assets/02.Scripts/GameActor/Component/Common/DissolveController.cs)는 lilToon 디졸브·Cutout 전환을 지원하고 [카메라 근접 디더](../../Assets/02.Scripts/GameActor/Component/Common/ActorCameraProximityDither.cs)에도 lilToon 변형 경로가 있다. 소스상 연결 경로가 있다는 판단이며 대상별 런타임 통과는 아니다.

`AlwaysVisible_Material`과 `Particles/Standard Unlit` 연기/VFX를 몸체 교체 대상에서 제외해야 한다. 후자는 기존 프리팹에 실제 존재하는 사실만 기록하며 URP에서 해당 효과가 정상 출력되는지는 이번 검토에서 확인하지 않았다.

후속은 일반 Golem 또는 Skeleton, Griffin, 발광이 있는 Plant/Spider 대표를 독립 검수본으로 만들어 플레이어·NPC와 동일한 씬/조명에서 비교한 뒤 확대하는 순서를 권장한다. Stone Golem은 별도 패스로 진행한다.

| 항목 | 현재 상태 | 후속 확인 기준 |
|---|---|---|
| 프리팹·재질·셰이더 프로퍼티 조회 | 수행 | 본문 및 아래 목록 |
| 피격/사망/카메라 디더 코드 경로 검토 | 수행 | 위 소스 링크. 실제 동작과 구분 |
| 교체본 렌더·낮/밤·역광 | 미수행 | TPS 거리에서 무늬·눈·약점·종별 실루엣 식별 유지 |
| 공격/이동 중 외곽선·LOD | 미수행 | 날개·다리·뿌리의 깨짐/튀는 외곽선/전환 팝 없음 |
| 피격→사망→풀 재사용 | 미수행 | 플래시·디졸브·재등장 시 잔류 색/소실 없음 |
| 카메라 근접·안개/수중 | 미수행 | 디더/투명도·배경과의 명도 관계 확인 |
| 다수 배치 성능 | 미수행 | 동일 개체수·카메라에서 CPU/GPU와 패스 비용 비교. 빨라진다고 단정하지 않음 |
| 최종 미술 인수 | 사용자 테스트 대기 | 대표 교체본 제작 이후 사용자 판단 |
| 테스트/빌드 | 미수행 | 검토·문서만 변경하여 Unity 테스트/Player/Addressables 빌드 불필요 |

후속 사용자 결과는 `항목 / 통과·실패·미수행 / 실제 증상·개체·조명·거리 / 캡처`로 받는다. 현재는 적용 전 검토 완료이며 전환 구현·검증 완료가 아니다.

## 조회한 프리팹 목록

아래 링크는 실제 AssetDatabase 조회 대상이다. 전체 Renderer 재질에는 몸체 외 보조 효과도 포함되므로 변환 목록으로 그대로 쓰지 않는다.

- [MonsterActor_SkySentinel.prefab](../../Assets/03.Prefabs/Actor/Monster/CameraTestMap/MonsterActor_SkySentinel.prefab) — `Shader Graphs/AlwaysVisible_Particle`, `Universal Render Pipeline/Lit`
- [MonsterActor_StoneColossus.prefab](../../Assets/03.Prefabs/Actor/Monster/CameraTestMap/MonsterActor_StoneColossus.prefab) — `Shader Graphs/AlwaysVisible_Particle`, `Universal Render Pipeline/Lit`
- [Monster_ChildPlant_V1.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_ChildPlant_V1.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_ChildPlant_V2.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_ChildPlant_V2.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_ChildPlant_V3.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_ChildPlant_V3.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Dryad.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Dryad.prefab) — `_lil/lilToonMulti`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Golem_Black.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Golem_Black.prefab) — `Shader Graphs/Stone_Golem_ShaderGraph`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Golem_Inferno.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Golem_Inferno.prefab) — `Shader Graphs/Stone_Golem_ShaderGraph`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Golem_Normal.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Golem_Normal.prefab) — `Shader Graphs/Stone_Golem_ShaderGraph`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Griffin_Brown.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Griffin_Brown.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Griffin_Dark.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Griffin_Dark.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [Monster_Plant_V1.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Plant_V1.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Plant_V2.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Plant_V2.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_Plant_V3.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_Plant_V3.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [Monster_RootPlant_V1.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_RootPlant_V1.prefab) — `Shader Graphs/AlwaysVisible_Particle`, `Universal Render Pipeline/Lit`
- [Monster_RootPlant_V2.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_RootPlant_V2.prefab) — `Shader Graphs/AlwaysVisible_Particle`, `Universal Render Pipeline/Lit`
- [Monster_RootPlant_V3.prefab](../../Assets/03.Prefabs/Actor/Monster/Monster_RootPlant_V3.prefab) — `Shader Graphs/AlwaysVisible_Particle`, `Universal Render Pipeline/Lit`
- [MonsterActor_Ent_V1.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Ent_V1.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [MonsterActor_Ent_V2.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Ent_V2.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [MonsterActor_Golem.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Golem.prefab) — `Shader Graphs/AlwaysVisible_Particle`, `Universal Render Pipeline/Lit`
- [MonsterActor_Lich_V1.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Lich_V1.prefab) — `Universal Render Pipeline/Lit`, `Particles/Standard Unlit`, `Shader Graphs/AlwaysVisible_Particle`
- [MonsterActor_Lich_V2.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Lich_V2.prefab) — `Universal Render Pipeline/Lit`, `Particles/Standard Unlit`, `Shader Graphs/AlwaysVisible_Particle`
- [MonsterActor_Skeleton_Bow.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Skeleton_Bow.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [MonsterActor_Skeleton_Common.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Skeleton_Common.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [MonsterActor_Skeleton_Sword.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Skeleton_Sword.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [MonsterActor_SpiderMinion_V1.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_SpiderMinion_V1.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [MonsterActor_SpiderMinion_V2.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_SpiderMinion_V2.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [MonsterActor_SpiderMinion_V3.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_SpiderMinion_V3.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`, `Particles/Standard Unlit`
- [MonsterActor_SpiderQueen_V1.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_SpiderQueen_V1.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [MonsterActor_SpiderQueen_V2.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_SpiderQueen_V2.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [MonsterActor_SpiderQueen_V3.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_SpiderQueen_V3.prefab) — `Universal Render Pipeline/Lit`, `Shader Graphs/AlwaysVisible_Particle`
- [MonsterActor_Training_Dummy.prefab](../../Assets/03.Prefabs/Actor/Monster/MonsterActor_Training_Dummy.prefab) — `Shader Graphs/AlwaysVisible_Particle`, `Universal Render Pipeline/Lit`

## 변경과 부산물

변경은 이 검토 문서와 분야 위키 연결뿐이다. 별도 임시 스크립트·캡처·에셋을 만들지 않았다. 정리 대상 없음. 기존 사용자 미커밋 변경 보존.

