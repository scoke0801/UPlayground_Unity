---
name: organic-modeling-quality
description: "Blender(MCP·백그라운드 CLI)로 나무 줄기·고사목·바위·통나무·그루터기·석조 소품 같은 유기체/자연물 메시를 만들 때, 프리미티브 조합·플랫 셰이딩으로 각지고 로우폴리해 보이는 문제를 막는 레시피·폴리 예산·셰이딩 규칙·멀티앵글 자체 검수 기준. 다음 상황에서 사용한다: '나무 만들어줘', '고사목/쓰러진 나무/통나무/그루터기 모델', '바위·돌 만들어줘', 'Blender로 자연물 소품', '모델이 너무 각져/로우폴리야/막대기 같아', '바위가 감자 같아', '나무 줄기 퀄리티 올려줘'. 반대로 드롭 아이템 월드 모델(drop-item-world-model), 건물·모듈러 키트 그리드 설계(environment-artist), 배치·산포·묻기(TERRAIN_WATER_ART_LESSONS A.4, set-dressing), 구매 에셋 연결만 하는 작업, 잎·카드 텍스처 제작에는 사용하지 않는다."
---

# 유기체 모델링 품질

Blender 스크립트로 만든 자연물이 "직선 원기둥 + 막대기 가지 + 플랫 셰이딩"으로 끝나는 것을 막는다. 공용 함수와 실증 데모는 `templates/organic_lib.py`다(Blender 5.2.2에서 실행 확인). 새로 짜지 말고 이 함수를 불러 매개변수만 바꾼다.

```bash
# 비교 렌더 4구도 + 측정 + FBX 왕복을 한 번에(출력은 스크래치 폴더로)
"/c/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup \
  -P ".claude/skills/organic-modeling-quality/templates/organic_lib.py" -- "<출력폴더>" demo
```

대화형 Blender MCP가 연결돼 있으면 파일 내용을 `execute_blender_code`에 넣고 `make_*`·`measure`를 호출한다. 사용자 씬이 열려 있으면 새 컬렉션에서만 작업하거나 백그라운드 CLI를 쓴다(drop-item-world-model 1장과 같은 원칙).

## 1. 증상 → 원인 → 처방

| 증상 | 원인 | 처방 |
|---|---|---|
| 면마다 명암이 끊겨 각져 보임 | `shade_flat`(기본값) 또는 노멀 미설정 | 유기체는 전부 스무스 + 각도 샤프 + Weighted Normal(5장). 구매 팩은 로우폴리 팩까지 100% 스무스·커스텀 노멀이었다 |
| 줄기가 직선 원기둥 | 단일 실린더, 일정 반경 | 베지어 골격 + 점별 반경 테이퍼(1.25→0.45) + 노이즈 휨 |
| 가지·뿌리가 막대기, 접합부가 꽂힌 듯함 | 부품을 따로 만들어 겹쳐 둠 | 한 커브에 스플라인으로 넣고 시작점을 줄기 **안쪽**에 둔 뒤 복셀 리메시 + Smooth로 통합·필렛 |
| 노이즈를 줬는데 오히려 울퉁불퉁 각짐 | 성긴 링(6~8점)에 Displace | 리메시로 촘촘하게 만든 **뒤** 변위, 예산은 마지막 Decimate로 맞춤 |
| 바위가 감자·조약돌 | 구체 + 노이즈만, 평면 없음 | 비균등 스케일 + 평면 컷 5~7개(깊이 = 그 방향 돌출의 55~75%) + 약한 노이즈 |
| 뿌리가 별 모양, 가지가 등간격 | 각도·길이를 균등 분할 | 시드 난수로 각도·길이·굵기를 모두 흔든다 |
| 바위·그루터기가 떠 보임 | 피벗·바닥이 지면과 안 맞음 | 피벗을 바닥 중심(최저점 z=0)으로. 묻는 깊이는 배치 단계에서 정한다 |
| Unity에서 셰이딩이 Blender와 다름 | 모디파이어 미적용 익스포트, Unity 노멀 재계산 | 전부 Apply 후 FBX, Unity Normals = Import(7장) |

## 2. 파이프라인과 Apply 시점

1. **골격**: 베지어 커브(줄기·가지·뿌리 스플라인), 점별 `radius`로 테이퍼. 바위는 아이코스피어(subdiv 5) + 비균등 스케일 + 평면 컷.
2. **메시화** → **복셀 리메시**(줄기 `voxel = 밑동 반경/10`, 바위 `반경/24`) → **Smooth**(factor 0.8, 6회; 바위는 생략). 부품 통합과 접합부 필렛이 여기서 생긴다. 리메시는 UV를 지운다.
3. **다층 변위**: 큰 덩어리(Clouds, 크기 0.45~0.55, 세기 = 반경×0.15~0.2) → 중간 결(크기 0.1~0.18, 세기 = 반경×0.05). 고주파 잔결은 넣지 않는다(텍스처 몫).
4. **컷**: 지면 아래(z < -0.05) 제거, 통나무·그루터기 단면은 약간 비스듬히.
5. **Decimate(Collapse)** 로 예산까지 → 정리(병합·퇴화면 제거·가장 큰 섬만 유지·노멀 통일).
6. **피벗** 바닥 중심 → **노멀**(마지막. Decimate 뒤에 해야 한다).
7. **UV**(uv-workflow) → **LOD**(lod-pipeline) → **FBX**(unity-export). 익스포트 전 모든 모디파이어를 Apply하고 스케일 1을 확인한다.
   LOD는 LOD0 사본을 Decimate한 뒤 `finish_normals`와 `measure`를 LOD마다 다시 돌린다. 토폴로지가 바뀌면 LOD0의 커스텀 노멀은 맞지 않는다.

하드서피스(석조 소품)는 순서가 다르다: 형태 → Bevel(폭 0.01~0.03m, 2~3세그먼트, 각도 제한 30°) → (필요 시) Subdivision → Weighted Normal. Bevel을 Subdivision 뒤에 두면 모서리가 찌그러진다.

## 3. 카테고리 레시피 (`organic_lib.py` 함수)

| 대상 | 함수·핵심 매개변수 | 하한·주의 |
|---|---|---|
| 나무 줄기(잎 별도) | `make_tree_trunk(height=3.8, base_r=0.32, branches=3, roots=5, target_tris=2500)` | 뿌리 3~6개, 가지 시작은 줄기 안쪽, 최종 1,200 tris 미만이면 실루엣 재확인 |
| 고사목 | `make_tree_trunk(..., dead=True, branches=4)` | 가지 끝 반경 0.06 이하로 가늘게, 가지 길이 1.4배 |
| 바위 | `make_rock(size=(1.35,.95,.7), cut_count=6, cut_depth=(.55,.75), sharp_deg=30)` | 컷 깊이 0.9 이상은 스치기만 함. 바닥 컷 1개 필수 |
| 통나무 | `make_log(length=2.4, r=0.22, target_tris=800)` | 반경을 점마다 흔든다(일정 반경은 파이프). 양 끝 단면은 샤프 45° |
| 그루터기 | `make_log(stump=True, r=0.22, target_tris=900)` | 테이퍼를 약하게(나무처럼 주면 원뿔 덩어리), 높이 55% 지점 비스듬히 절단 |
| 석조 소품 | 박스·실린더 → 2장 하드서피스 순서 | **실증 안 됨.** 블록마다 크기·회전 편차, 완전 직각·동일 블록 반복 금지 |

변형은 `seed`만 바꿔 만든다. `random`과 `mathutils.noise` 모두 시드로 고정한다.

## 4. 폴리 예산 (이 프로젝트 구매 에셋 실측, LOD0 tris)

2026-11-15 FBX를 Blender로 가져와 실측(파일 단위 적용, 각 `.meta`는 `useFileScale: 1`). 크기 구분은 이 치수 기준이며 Unity 안에서 재지는 않았다. OpenWorld 배치는 주로 PolyartStudio FarmLand, RiverBasinWorld는 Idyllic Fantasy Nature를 쓴다.

| 대상 | 실측 | 신규 제작 목표 |
|---|---|---|
| 중경 활엽수 줄기 | Idyllic Broadleaf 줄기 1,382(+잎 2,000), Farm GenTree 줄기 3,072(+잎 17.5k) | 줄기·가지·뿌리 1,200~3,000 |
| 고사목 | TGV Dry 1,810 / Farm DriedTree(히어로) 24,826 | 중경 1,500~3,000, 히어로 5k~25k |
| 그루터기 | TGV Trunk 890 | 600~900 |
| 통나무 | Farm Log 860, TGV Log 744, LowPoly 286 | 500~900 |
| 바위 소(~1m) | 160~433 | 150~450 |
| 바위 중(2~7m) | 252~1,334 | 400~1,400 |
| 바위 대(수십 m) | TGV Large 5,150 | 2,000~5,000 |

LOD 비율 실측: LOD1 55~75%, LOD2 30~55%(TGV·Farm). lod-pipeline의 일반값(40~60/15~30%)보다 이 표를 우선한다. 예산은 대상 씬에서 측정하기 전까지 목표일 뿐 성능 판정이 아니다.

## 5. 셰이딩·노멀 (Blender 4.1+, 5.2.2 확인)

- `mesh.use_auto_smooth`는 4.1에서 제거됐다. 쓰면 AttributeError가 난다.
- 쓸 API: `me.shade_smooth()` → `me.set_sharp_from_angle(angle=라디안)` → `WEIGHTED_NORMAL` 모디파이어(`keep_sharp=True`, `weight=50`, `mode='FACE_AREA'`) → Apply(커스텀 노멀로 굳음).
- `bpy.ops.object.shade_auto_smooth`는 "Smooth by Angle" 지오메트리 노드 모디파이어를 붙인다. 모디파이어 타입 `SMOOTH_BY_ANGLE`은 없으므로 `modifiers.new`로 만들 수 없고, 쓰면 익스포트 전에 Apply해야 한다.
- 샤프 각도: 나무·고사목 60°, 통나무·그루터기 단면 45°, 바위 30°.
- 유기체에 `shade_flat` 금지. "로우폴리 스타일"도 구매 팩은 스무스 + 커스텀 노멀로 면을 표현했다.

## 6. 자체 검수 (통과 기준)

`measure(ob)`와 `render_views(...)`(Workbench 스튜디오 조명, 캐비티·그림자 끔) 결과로 판정한다. 하나라도 실패하면 고치고 다시 측정한다.

| 항목 | 통과 기준 | 근거 |
|---|---|---|
| `smooth_ratio` / `custom_normals` | 1.0 / true | 구매 팩 전부 |
| `median_edge_angle_deg` | 줄기·통나무·그루터기 ≤ 15, 고사목 ≤ 18, 바위 ≤ 16 | 구매 7.6~17.7, 레시피 6.5~14 |
| `p90_edge_angle_deg` | ≤ 45 (나이브 실린더는 90) | 레시피 24~41 |
| `non_manifold_edges` / `zero_area_faces` | 0 / 0 | |
| `loose_parts` | 1(의도한 부품 수와 같음) | 그루터기 컷 조각 실사례 |
| `local_min_z` | -0.15 ~ 0 (피벗이 바닥) | 바위 뜸 실사례 |
| `tris` | 4장 목표 범위 | |
| `scale` | 1,1,1 | |
| FBX 왕복 | 다시 가져와도 위 값이 같음. **노멀·스무딩·tris만** 비교한다. `bake_space_transform`로 Y-up이 굽혀져 Blender 재가져오기에서는 축·`local_min_z`가 바뀌어 보이는 것이 정상이며, Unity 크기·축은 이 검사로 검증되지 않는다 | 데모 `fbx_roundtrip` |
| UV(익스포트 전) | UV 레이어 ≥ 1, 0~1 밖 UV 없음. 리메시가 UV를 지우므로 uv-workflow로 편 뒤 확인 | `measure`는 UV를 보지 않는다 |

화면 4구도는 모두 본다(한 장만 보고 완료 판정 금지): ① 3/4 ② 측면(실루엣이 직선이면 실패) ③ 위 45~60°(별 모양·등간격이면 실패) ④ 1~3m 근접(접합부 이음새, 면 끊김, 떠 있음). 가능하면 같은 화면에 나이브 버전이나 구매 에셋을 나란히 둔다. 렌더는 실루엣·셰이딩 확인용이고 미술 품질 판정이 아니다.

### LLM 스크립트 모델링의 흔한 실패

- 프리미티브 몇 개를 겹쳐 놓고 끝냄, 기본값 플랫 셰이딩을 그대로 둠
- 성긴 메시에 노이즈, 균등 분할 배치, 시드 없는 난수
- 4.0 이전 API(`use_auto_smooth`), 백그라운드에서 컨텍스트 의존 `bpy.ops` 실패 → 데이터 API(`new_from_object`)로 Apply
- 스케일·모디파이어 미적용 익스포트, 화면 1장으로 완료 보고

## 7. Unity 임포트·질감

- Model Import: **Normals = Import**(기본). Calculate는 단일 각도로 하드 엣지를 정해 의도한 샤프가 깨진다(Unity 6 매뉴얼). Smoothness Source·Smoothing Angle은 바꾸지 않는다(Smoothing Angle은 Calculate 전용). 노멀맵을 쓰면 Tangents = Calculate Mikktspace.
- FBX 설정은 Unity에서 검증된 `ArtSource/DropItems/_lib/dropitem_lib.py`와 같게 둔다: `apply_scale_options='FBX_SCALE_ALL'`, `axis_forward='-Z'`, `axis_up='Y'`, `bake_space_transform=True`, `mesh_smooth_type='FACE'`, `use_mesh_modifiers=True`, `object_types={'MESH'}`(데모 `demo()`와 동일). 유기체 메시로 Unity 크기·축을 확인한 적은 아직 없으므로 첫 적용 때 1m 큐브 옆에서 확인한다(Unity 슬롯 필요).
- 노멀맵 없이 질감 살리기: 큰 형태와 부드러운 노멀이 먼저다. 그다음 버텍스 컬러(오목부 어둡게, 위쪽 밝게)나 AO를 컬러 속성에 굽고, 트림·아틀라스 텍스처(uv-workflow)를 쓴다. lilToon·URP 머티리얼 연결과 버텍스 컬러 사용 여부는 대상 셰이더에서 확인한다(이 스킬에서 실증하지 않음).

## 경계

- 배치·군락·묻기·크기 위계는 `Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md` A.4·A.5를 따른다.
- Blender 화면 확인과 Unity 적용 확인을 구분해 보고하고, 스타일·가독성 등 미술 품질은 `사용자 확인 필요`로 남긴다.
