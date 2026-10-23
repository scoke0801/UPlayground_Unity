# E — 생활 확장 자산 조사·제작 판정 인계

> 기준일 2026-10-22 / **후보 실측·재사용 조사 완료. 신규 모델·텍스처 제작 확정 항목 없음. E 제작·본편 통합 완료 판정은 아님.**
> 기준: [A](A_BASELINE.md) 7~8절, [B](B_MAP_HANDOFF.md), [C](C_NPC_HANDOFF.md) 4절, [D](D_ENVIRONMENT_HANDOFF.md) 7절, [E 작업 지시](../LAKE_OF_LIFE_LIVING_WORLD_TASKS.md) 7절.
> “목록이 없으면 후보 조사·실제 부족 여부부터 정리” 조건을 적용했다. **미확인은 부족 확정도, 품질 통과도 아니다.**

## 1. 완료 범위와 남은 범위

A/B/C/D 최신 파일과 실제 Unity 자산을 대조했다. B는 기존 우물·집·밭·개울을 재사용했고 구체적인 신규 모델 발주가 없다. C는 조리·운반·수선·착석 모션과 손 접점 검증을 요구하되, 실제 부족이 확정되기 전 신규 모델 제작을 요구하지 않는다고 명시했다. D는 기존 흙·물결·FX 텍스처 우선 사용이며 근접 화면에서 부족이 확인될 때만 신규 제작하도록 했다.

따라서 이번 E는 **새 모델 0, 새 텍스처 0, 새 재질 0, 새 프리팹 0**이다. 기존 자산을 이유 없이 복제하거나 빈 프로젝트 폴더·임시 도형을 납품하지 않았다. 아래 파일/GUID가 재사용 전달 목록이다. 원본을 못 쓴 이유가 입증된 모델·텍스처는 현재 없다. 런타임 연결 변경은 하지 않았다.

| 요구 / 사용처 | 확인한 기존 해결 수단 | 현재 판정·남은 조건 |
|---|---|---|
| B / R04 마른 우물 | 현재 씬 `R04_DryWell`이 TGV_Well_01A 모델 사용. 원본 근접 렌더에서 내부 석벽과 목재 테두리 확인 | **내벽 부재 가정은 기각.** 바닥·수위 흔적·실제 지형 관통은 B/I 조사 카메라로 확인해야 함. 별도 내부 모델 제작 보류 |
| C / 조이 식사·그릇 정리 | TGV_Bowl_01A, TGV_Bread_01A. 그릇 실제 렌더 확인 | 기물 재사용 가능 후보. 조리/집기/내려놓기 클립·손 접점 미검증. 모델 생성으로 모션 부족을 해결할 수 없음 |
| C / 헤이즐 약 포장·처치 | TGV_Bottle_01A, TGV_Wood_Crate_01A, 기존 Humanoid_PackMedicine | 병/상자는 후보일 뿐 약상자 의미·운반 적합성 확정 아님. 상자 길이 약 1.05m여서 손 약상자로 곧바로 채택하지 않음. 전용 약상자 크기/손잡이 요구 미제공 |
| C / 모건 짐·수레 | TGV_Sack_01A, TGV_Wood_Crate_01A, 기존 Dungeon 수레, 소형 TGV_WheelBarrow 후보 | 적재·검사 기물 있음. 끈 당김/운반 접점 미검증. 기존 R09 수레를 수레바퀴 하나인 손수레로 임의 교체하지 않음 |
| C / 연목 수선 | FarmLand PF_Farm_Hammer_01, Stylized 3D Tools hammer01 | FarmLand 후보는 실제 URP 렌더 확인. 후자는 Standard (Specular setup)이므로 URP 적용 검수 전 채택하지 않음. Avatar/작업면/타격점 미확정 |
| C / 잔류 주민 물통·씨앗 | TGV_Bucket_01A, TGV_Sack_01A | 물통 손잡이가 별도 Transform이며 원본 BoxCollider 있음. 손잡이 회전·그립은 작업 모션 확정 후 조정 |
| C / 짐 주민 포장 | 기존 R04 before/ready 짐, TGV 자루/상자 | 서사 소유 기물을 유지. 반복 일과용 복제품을 만들어 인계 결과를 중복 표시하지 않음 |
| C / 휴식 | TGV_Bench_01A 높이 약 0.50m | 착석 전환과 무릎/발 접지 미검증. 현재 C의 서 있는 휴식을 임의 착석으로 변경하지 않음 |
| A 후보 / R09 수로 접합 | 현 씬 R09_ChannelLooseWood/ChannelWoodAside가 TGV_Wood_Fence_01A 사용. TGV_Wood_Plank_01A도 존재 | **펜스 사용만으로 결함 확정하지 않음.** 실제 접합 길이·전후 상태·카메라 불연속 증거 없음. 새 수문/접합부 제작 보류 |
| A 후보 / 시든 작물·붉은 뿌리 접합 | B가 기존 밭·개울을 재사용 | 구체 실패 화면·품종 실루엣·접합 치수 없음. 형태 부족 판정 미수행. 나무 고사 모델을 시든 작물 대체품으로 채택하지 않음 |
| D / 흙·물결·먼지·비 | 아래 5개 재질에 기존 텍스처 연결 확인 | 새 ImageGen 텍스처 필수 아님. 실제 환경 카메라에서의 반복/반사/오버드로 검수는 남음 |
| D / 바람·비 소리 | D 인계상 AudioSource clip 공란 | **확인된 별도 오디오 조달 과제.** 모델/텍스처 제작 범위와 다르며 이번에 제작·청취하지 않음 |

BlenderMCP 연결은 정상이다. 조회 당시 저장 경로 없는 기본 Scene, Cube/Camera/Light, dirty=false였으며 읽기 조회만 했다. 기존 Blender 작업을 저장·수정·초기화하지 않았다. 제작 필요가 확정되지 않아 Blender 모델 생성과 ImageGen 호출은 하지 않았다. 생성 이미지/PBR 세트·프롬프트도 없다.

## 2. 파일·GUID와 임포트 설정

신규 추적 대상은 이 문서와 `E_ASSET_HANDOFF.md.meta`뿐이다. 문서 GUID는 `58f764f4e0da4a8982be64315c984ba7`다. 새 Actor/Map/SceneEntity ID, Addressables 주소는 없다.

### 재사용 프리팹

경로를 그대로 로드한다. 원본 .meta를 유지하며 GUID를 재발급하지 않는다.

| 파일 | 기존 GUID |
|---|---|
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/TGV_Well_01A.prefab` | `fc7b668fd75bff14aa8124a2ab595675` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/TGV_Bucket_01A.prefab` | `8c0950d86645f7045a28896aaafc1b59` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Interior Props/TGV_Bench_01A.prefab` | `cf57add37fae8db4980f5946d91db26f` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Food/TGV_Bowl_01A.prefab` | `b2867439563de7b429679e5e41753b98` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Food/TGV_Bread_01A.prefab` | `958f52063bd0f6b46b92c84a338a49ea` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Food/TGV_Bottle_01A.prefab` | `3c779352b029c7c40a32fb7ff9f185db` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/TGV_Wood_Crate_01A.prefab` | `30ac00b6fee507e459067f6744e10d21` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/TGV_Sack_01A.prefab` | `32069977764b9854ca8051de610a42a4` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/TGV_Wood_Plank_01A.prefab` | `93f9bb19c7ab7b84a8fae6951fc52d3b` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Exterior Props/TGV_WheelBarrow_01A.prefab` | `d02bb6f26e100f049a11bdea4550f5f5` |
| `Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Props/FarmTools/PF_Farm_Hammer_01.prefab` | `a726b79cc7f1f75489e753e13ca4fab5` |
| `Assets/ExternalAssets/Prop/Weapon/Stylized 3D Tools/Prefabs/hammer01.prefab` | `9f64ed5ce0216394d91f836dbefecf5d` |
| `Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/PROPS/misc/P_PROP_cart_dungeon_01.prefab` | `eef1357fe1627e9478543b1ac2e1ffc5` |

### 연결 모델

프로젝트 임포트 상태를 실제 AssetDatabase에서 읽었다. 아래 기존 모델과 해당 .meta를 참조하며 복제하지 않았다.

| 파일 | 기존 GUID |
|---|---|
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Well_01A.fbx` | `25a4b702b45a787479a00c7ca3e87034` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Bucket_01A.fbx` | `faca49e350731f740ab4cc822b69238f` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Bench_01A.fbx` | `9e9d8ec1f03f8824f899edc1c5b6a18c` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Bowl_01A.fbx` | `2b5a3d141335b4a40b0377d3c6aa2f9b` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Bread_01A.fbx` | `74ce02df32fd39d4e9dfbf4e93b9a015` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Bottle_01A.fbx` | `ac9b24e73522ee348b0ca2df2eb0064a` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Wood_Crate_01A.fbx` | `a3e8a3ce7d285f84ba7df9dd372f4d64` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Sack_01A.fbx` | `519238ddf0edd2f468886c4093cd8876` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_Wood_Plank_01A.fbx` | `1a3e415d1f9ca8a4789c1bb185e2084e` |
| `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/TGV_WheelBarrow_01A.fbx` | `86d0aa70fb0786b458bb6c2a1bb7cc23` |
| `Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Meshes/Props/FarmTools/SM_Farm_Hammer_01.fbx` | `741f86c4adae66442b8f4190f6939057` |
| `Assets/ExternalAssets/Prop/Weapon/Stylized 3D Tools/Models/hammer01.obj` | `d93a0b6a65031ae45ad29ed7e096cfea` |
| `Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/3d/props/SM_PROP_cart_dungeon_01.fbx` | `4da606d33647e9c40904da822145911a` |

- TGV FBX 공통: globalScale=1, useFileScale=true, fileScale=0.01, Read/Write=false, normals=Import, tangents=CalculateMikk, meshCompression=Off, animationType=Generic. **Unity 프리팹 루트 배율 1의 실측 크기가 기준**이며 fileScale 숫자만 보고 추가 0.01 배율을 적용하지 않는다.
- FarmLand 망치: globalScale=1, fileScale=1, useFileScale=true, Read/Write=false, Import normals, CalculateMikk tangents, compression=Off, Generic.
- Dungeon 수레: globalScale=1, fileScale=0.01, useFileScale=true, Read/Write=false, Import normals, CalculateMikk tangents, compression=Off, animationType=None.
- hammer01 OBJ: globalScale/fileScale=1, Read/Write=true, Import normals, CalculateMikk tangents, compression=Off, animationType=None. URP 검수 전 제외 후보.
- 조사 모델의 모든 MeshFilter에서 UV0 개수=vertexCount. 이는 UV 펼침 왜곡·겹침·밀도 품질 통과를 의미하지 않는다. 아틀라스 UV를 임의 재배치하지 않는다.

TGV 공용 재질은 `Assets/ExternalAssets/Environment/Toon Golden Valley/Models/Materials/TGV_Atlas_1A.mat`, GUID `8a46e38b777bb7a4a8229df05167173b`, 셰이더 `Toon/TGV_CustomToonNormal`이다. 실제 렌더에 아래 원본 맵이 함께 사용됐다. 맵 단독 on/off 비교는 미수행이다.

| 프로퍼티 | Textures/ 아래 파일 / GUID | 현재 임포트 |
|---|---|---|
| _TextureSample | TGV_Atlas_1A.png / be4832b6011421544b6dd23cd9451ba9 | Default, sRGB ON, mip ON, Clamp, max4096, CompressedHQ |
| _NormalSample | TGV_Atlas_1N.png / 8f01fa6e12179b4428cc0a45543eb172 | NormalMap, sRGB OFF, mip ON, Repeat, max2048, Compressed |
| _TextureRamp | TGV_Toon_Ramp_1A.psd / dd684ab800b982e4b8f29f0458bc5c09 | Default, sRGB ON, mip ON, Repeat, max2048, Compressed |

Textures/는 `Assets/ExternalAssets/Environment/Toon Golden Valley/Textures/`다. 목재의 낮은 채도 갈색, 둥글게 단순화한 형태와 석재를 기존 재질 그대로 비교했다. 다른 팩의 메시를 TGV 아틀라스에 단순 교체하면 UV가 맞지 않으므로 금지한다.

### D 재질의 실제 연결

아래는 **참조·임포트 검사**이며 이번 E의 환경 화면 품질 통과가 아니다. 재질 경로는 `Assets/11.Material/LivingWorld/Environment/`다. D 소유 파일은 저장하지 않았다.

| 재질 / 입력 | 기존 텍스처 / GUID | 실제 임포트 |
|---|---|---|
| CreekGround_Outdoor.mat / `_BaseMap` | `Assets/04.Images/Environment/RedTreeCreek/CreekSilt_Albedo.png` / `60354f9821917d4499fed313eb622a0b` | 1024x1024; Default;sRGB=True;mipmap=True;wrap=Repeat;max=2048;compression=CompressedHQ |
| CreekGround_Sheltered.mat / `_BaseMap` | `Assets/04.Images/Environment/RedTreeCreek/CreekSilt_Albedo.png` / `60354f9821917d4499fed313eb622a0b` | 1024x1024; Default;sRGB=True;mipmap=True;wrap=Repeat;max=2048;compression=CompressedHQ |
| LakeWater_Weather.mat / `_NormalMap` | `Assets/ExternalAssets/Environment/PolyartStudio/SharedResources/Textures/Water/T_WaveLarge_N.png` / `c7eecd4c36cac8043b0fb53e64169d7e` | 512x512; NormalMap;sRGB=False;mipmap=True;wrap=Repeat;max=2048;compression=Compressed |
| Rain.mat / `_BaseMap` | `Assets/ExternalAssets/FX/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr stretch rain blur.png` / `a0e7dff5c9a656b4395f3cbc90ceac51` | 64x8; SingleChannel;sRGB=False;mipmap=True;wrap=Clamp;max=64;compression=Compressed |
| DroughtDust.mat / `_BaseMap` | `Assets/ExternalAssets/FX/Magic effects pack/Textures/SmokeFree1.png` / `bbc32c56102b32f4dafa718b4e782d9d` | 512x512; Default;sRGB=False;mipmap=True;wrap=Clamp;max=512;compression=Compressed |

흙의 젖음·둑 경계는 메시 vertex R/A 계약을 유지한다. 물결 노멀은 월드 XZ이며 NormalMap import를 유지한다. 생성 색상 이미지를 노멀·금속·러프니스·AO로 임의 복제하지 않는다. 후속 제작을 결정하면 D 7절의 경로/UV/색공간/프로퍼티를 따르고 각 맵의 실제 재질 적용 화면을 별도로 검수한다.

전체 재질/모델/텍스처 GUID·설정은 [기존 자산 검사 JSON](../../../../output/LivingWorldE/ExistingAssetAudit.json), [환경 검사 JSON](../../../../output/LivingWorldE/EnvironmentAssetAudit.json)에 보관했다.

## 3. 소비 계약과 치수·피벗·충돌

C의 실제 계약은 `NpcWorkPoint._heldPropPrefab`과 `NpcRoutine._propSocket`이다. 작업 도착 후 `Instantiate(prefab, socket, false)`하며 **이동 중 운반 기능이 아니다**. 소켓이 없으면 작업 예약을 거부한다. 기존 ItemActor/보상/서사 완료 소품을 장식 손 소품 슬롯에 연결하지 않는다.

| 후보 | 실측 크기 X×Y×Z (m), 루트 배율 1 | 피벗·부품·충돌과 연결 조건 |
|---|---|---|
| TGV_Bucket_01A | 0.6452×0.5246×0.6062 (접힌 손잡이 포함) | 원점은 바닥 부근. 손잡이 별도 Transform 약 (-0.01,0.43,0.01), 본체/손잡이 각각 LOD0/1. BoxCollider 1개. 손에 연결할 때는 원본을 수정하지 않고 별도 프로젝트 프리팹에서 그립 원점·손잡이 회전·장식 충돌 비활성을 저작해야 함 |
| TGV_Bowl_01A | 0.1947×0.0845×0.1997 | 바닥 원점. 단일 메시, 빈 그릇. 손가락 받침 위치는 캐릭터 손 소켓·집기 클립으로 확인 필요 |
| PF_Farm_Hammer_01 | 0.2716×0.5173×0.1637 | bounds 중심 (-0.0086,0.0609,0). 원점=접지점으로 간주하면 안 됨. LOD 3단. 손잡이 접점/타격면 미검증, 손용 기물에 물리 충돌을 임의 추가하지 않음 |
| TGV_Wood_Crate_01A | 0.6750×0.6520×1.0516 | 중심 (-0.0049,0.3159,-0.0063). 적재용 후보. 양손 운반·약상자용 크기로 확정하지 않음 |
| TGV_Bench_01A | 약 2.00×0.50×0.74 | 바닥 원점, BoxCollider 1개. 실제 착석 높이/앞 여유/발 접지는 C/I 확인 |
| TGV_Well_01A | 본체 메시 약 1.68×2.26×1.42, 휠 범위 별도 | 바닥 아래 약 0.20m 포함. 휠 Transform y≈1.65m, 본체/휠 분리, BoxCollider 2개. 본체 mesh bounds를 전체 휠 포함 치수로 쓰지 않음 |
| Dungeon 수레 | 본체 메시 약 3.36×1.58×5.18, 바퀴 별도 | 좌우 바퀴 분리, BoxCollider 5개. 좁은 통로 적합성·기존 인스턴스 배율은 I 현장 확인 |

새 모델이 없으므로 신규 단순 충돌 메시/UV/분리 부품을 납품한 것으로 기록하지 않는다. 원본의 기본 충돌체와 분리 구조를 조사했다. 필요한 변경은 **모션·접점이 확정된 뒤 프로젝트 전용 프리팹**에 적용하며 이 문서는 임의 그립 수치를 확정하지 않는다.

## 4. 배치·제작 결정 인계

I는 위 원본 참조를 우선 사용하고 B 씬과 기존 R04/R09 결과 소품을 보존한다. E가 본편이나 C/D 소유 프리팹을 저장하지 않았다. `_isPlacementReady`를 켜거나 null 작업 모션을 채우지 않았다.

제작 재개에 필요한 명세는 다음과 같다. 자료가 없는 상태를 도구 연결 실패로 표현하지 않는다.

| 담당 | 먼저 확정할 내용 | 그 뒤 E의 제작 범위 |
|---|---|---|
| B/I 우물·밭·수로 | 실제 배치 스케일, 조사 카메라 위치/거리, 빈 바닥·형태·접합 결함 화면, 전후 상태 | 기존 부품/재질로 해결되지 않는 내부/시든 묶음/접합부만 BlenderMCP |
| C 손 소품 | 확정 클립·Avatar, 좌/우 손 소켓, 프레임별 접점, 작업면 높이, 목표 크기·손잡이 방향 | 우선 기존 메시로 프로젝트 손 소품 프리팹. 메시 형태가 부족한 경우만 신규 모델 |
| D 텍스처 | 실제 근접/원경 비교에서 반복·UV·정상 방향 실패와 기존 조정으로 해결 못 한 이유 | D 지정 채널·색공간의 해당 텍스처만 ImageGen. 재질 적용 후 맵별 검수 |
| I/D 오디오 | 바람·비 루프 소스, Mixer, 실제 루프 경계·대사 비교 청취 | 별도 오디오 과제. E 모델/텍스처 확보로 완료 처리하지 않음 |

## 5. 수행 검사·실패·미수행

Unity **6000.3.21f1**, 프로젝트 경로 `C:/UsingProject/UnityProject/UPlayground`, Edit Mode에서 수행했다. 기존 본편 씬은 로드 상태 dirty=false였고 조사 후에도 보존했다. 임시 PreviewScene에서만 기물을 생성해 렌더하고 finally에서 닫았다. 본편 카메라·조명·소품은 편집하지 않았다.

- 13개 후보 프리팹 실제 로드: Missing Script **0**. MeshFilter UV0 개수 불일치 **0**. 연결 재질 ShaderHasError **0**. 원본 프리팹의 프로젝트/URP 적합성을 모두 통과했다는 뜻은 아니다.
- D 재질 5개: 셰이더 오류 false, 연결 텍스처 경로/GUID/색공간·mipmap 확인. 환경의 동적 화면 검수는 미수행.
- 일회성 검사 첫 실행은 중복 Newtonsoft 어셈블리로 컴파일 실패했다. 프로젝트 코드를 수정하지 않고 결과를 MCP 구조화 출력으로 받아 JSON 저장하는 방식으로 해소했다. 최종 자산 조회 성공과 구별한다.
- 첫 렌더 정리에서 Camera.targetTexture를 해제하기 전에 RenderTexture를 Release해 콘솔 오류 2건이 발생했다. 재현 코드의 finally에서 카메라 연결부터 해제하도록 수정하고 두 렌더 검사를 재실행했다. 기존 오류 로그는 지우지 않았다.
- 문서 링크 대상과 .meta GUID 임포트를 확인했다. 전체 git diff --check는 기존 사용자 씬 YAML의 후행 공백을 보고했으며 해당 씬은 수정하지 않았다. 외부 원본 diff는 없었다.
- BlenderMCP 읽기 조회 성공. ImageGen 미호출. 둘 다 제작 실패 사례는 없다.
- 새 런타임 코드/자산 변경이 없어 전체 EditMode 테스트·Play Mode·Windows Player 빌드는 실행하지 않았다. 실제 손 접점·KCC 충돌·배치 시야는 **사용자 테스트 대기**다. 프레임/VRAM/오버드로 미측정.

### 근접·원경 렌더 확인

원본 연결 재질 그대로, 단독 기물 PreviewScene, 고정 Directional Light 1.2, FOV 50°. 아래 거리는 **카메라~렌더 bounds 중심**이며 표면까지의 최소 거리나 본편 조사 카메라 거리와 다르다. 소품은 960×540, 우물은 960×960. 소품 LOD 설정은 원본 유지, 우물은 내부 확인을 위해 LOD0 강제. 실제 인물·지형·날씨·밤 조명이 없는 비교다.

| 후보 | 근접 / 원경 | AI 이미지 확인 결과 | 근거 |
|---|---|---|---|
| 우물 | 목표 (0,0.7,0) 기준 약 3.06m / 7.57m | 근접 내부 석벽·목재 테두리 확인. 원경 우물/휠 실루엣 식별. 바닥 마름·수위 흔적의 현장 가독성은 미확인 | [근접](../../../../output/LivingWorldE/Well_Source_0.png), [원경](../../../../output/LivingWorldE/Well_Source_1.png) |
| 양동이 | 1.25m / 8m | 근접 목재·띠·접힌 손잡이 확인, 원경 기물 유지. 근접에서는 목재 무늬가 크게 읽히며 손잡이는 몸 뒤로 접혀 있어 그대로 손 운반에 쓸 수 있다고 판정하지 않음 | [근접](../../../../output/LivingWorldE/TGV_Bucket_01A_near.png), [원경](../../../../output/LivingWorldE/TGV_Bucket_01A_far.png) |
| 그릇 | 0.5m / 8m | 빈 내부·단순 가장자리 확인. 원경 약 20cm 그릇은 작은 점 수준이며 독립 단서로 쓰기 어려움 | [근접](../../../../output/LivingWorldE/TGV_Bowl_01A_near.png), [원경](../../../../output/LivingWorldE/TGV_Bowl_01A_far.png) |
| FarmLand 망치 | 1.2m / 8m | 목재 손잡이·짙은 금속 머리 정상 표시. 원경 작은 실루엣 유지. TGV보다 금속 반사가 강해 실제 연목 주변 비교 필요 | [근접](../../../../output/LivingWorldE/PF_Farm_Hammer_01_near.png), [원경](../../../../output/LivingWorldE/PF_Farm_Hammer_01_far.png) |
| 상자 | 2m / 8m | 판재 간 틈·내부·적재 상자 실루엣 확인. 손 소품으로서의 크기/접점 품질은 미확인 | [근접](../../../../output/LivingWorldE/TGV_Wood_Crate_01A_near.png), [원경](../../../../output/LivingWorldE/TGV_Wood_Crate_01A_far.png) |

재현 코드: [소품](../../../../output/LivingWorldE/PreviewProps.cs.txt), [우물](../../../../output/LivingWorldE/PreviewWell.cs.txt). 실측 결과: [PreviewResults.json](../../../../output/LivingWorldE/PreviewResults.json). 출력 폴더는 git ignore 대상인 로컬 보조 증거이므로 외부 전달 때 문서와 함께 포함한다. 최종 프리팹 납품 폴더로 취급하지 않는다.

### 사용자/I 확인 절차

별도 검증 씬 또는 본편의 저장하지 않는 복사 작업에서 확인한다. 원본 저장 슬롯·본편 씬·외부 프리팹을 덮어쓰지 않는다.

| ID | 준비·절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| E-01 우물 | B1 또는 I 통합 버전의 실제 R04 조사 카메라와 플레이어 시점에서 안쪽·둘레 관찰. 거리/배율 기록 | 바닥/벽 틈·지형 관통·물처럼 보이는 면 때문에 마름이 오독되면 실패. 실패 위치와 사진을 신규 내부 제작 근거로 전달 |
| E-02 손 소품 | C 확정 클립·Avatar·소켓을 별도 배치에 연결, 시작/루프/끝·대화 취소 확인 | 손가락/도구 관통·소품 이탈·작업면 불일치·잔류 없음. 소켓/모션 없는 현재 상태는 미수행 |
| E-03 미술·카메라 | TGV 생활 공간에서 망치·상자·물통을 실제 크기로 낮/밤, 근접/3m/8m 비교 | 과도한 광택·목재 패턴·LOD 소실·통행/얼굴 가림 없음. 같은 좌표 전후 화면을 보고해야 함 |
| E-04 흙·수면 | D 프로필·실제 흙 메시 vertex R/A·물 재질로 근접/원경 및 날씨/서사 회복 전후 비교 | 반복/경계/노멀 반전·실내 젖음·서사 수위 오염 없음. 새 텍스처 요청은 실패 맵/프로퍼티를 특정 |

회신: `ID / 통과·실패·미수행 / 버전·카메라 거리·배율·시각 / 실제 결과 / 화면·로그`. 사용자 결과가 없으므로 위 4항목은 모두 대기다.

## 6. 보존과 완료 판정

본편 씬 SHA-256은 A와 동일한 `23F089199D37FB8FCD136574D0BAEE2A2A58CAD8660D8F0B222E4799B2D6FFD5`, 피해 마을 씬은 B1과 동일한 `C9D4D2BD04B2BC9A01F1D8EFE7D956613A30A92E9955CB7B0AD721B2D0EFFE88`를 확인했다. 외부 원본·Blender Scene·B/C/D 코드/자산·사용자 변경은 수정하거나 원복하지 않았다. 씬 저장, 전체 SaveAssets, 일괄 재직렬화, 커밋은 하지 않았다.

**인계 결과는 후보 실측과 제작 필요 판정까지다.** 신규 제작 필요가 입증되지 않았으므로 없는 모델/텍스처/재질/프리팹/.meta를 납품했다고 기록하지 않는다. 남은 모션·현장 검수와 오디오 과제를 완료로 승격하지 않는다.

